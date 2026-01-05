using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.ApiClient.Dtos;
using Soenneker.Blazor.LogJson.Abstract;
using Soenneker.Blazor.Utils.Session.Abstract;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.Object;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Soenneker.Blazor.ApiClient;

/// <inheritdoc cref="IApiClient"/>
public sealed class ApiClient : IApiClient
{
    private readonly ILogJsonInterop _logJsonInterop;
    private readonly IHttpClientCache _httpClientCache;
    private readonly ISessionUtil _sessionUtil;

    private string? _baseAddressTrimmed; // cached for log URI building
    private Uri _baseUri;
    private bool _requestResponseLogging;

    // Header cache (NOT token cache for retrieval; SessionUtil already caches retrieval)
    private string? _cachedAccessToken;
    private AuthenticationHeaderValue? _cachedAuthHeader;

    private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);
    private const string _authScheme = "Bearer";

    private static readonly MediaTypeHeaderValue _octetStreamMediaType = new("application/octet-stream");

    private const string _anonymous = $"{nameof(ApiClient)}-anonymous";
    private const string _authenticated = $"{nameof(ApiClient)}-authenticated";

    public ApiClient(ISessionUtil sessionUtil, ILogJsonInterop logJsonInterop, IHttpClientCache httpClientCache)
    {
        _sessionUtil = sessionUtil;
        _logJsonInterop = logJsonInterop;
        _httpClientCache = httpClientCache;
    }

    public void Initialize(string baseAddress, bool requestResponseLogging)
    {
        _baseAddressTrimmed = baseAddress.HasContent() ? baseAddress.TrimEnd('/') : null;
        _baseUri = baseAddress.HasContent() ? new Uri(baseAddress, UriKind.Absolute) : throw new Exception("BaseAddress must be set");
        _requestResponseLogging = requestResponseLogging;
    }

    public ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        // No closure: state passed explicitly + static lambda
        if (allowAnonymous.GetValueOrDefault())
        {
            return _httpClientCache.Get(_anonymous, _baseUri, static baseUri =>
            {
                var httpClientOptions = new HttpClientOptions
                {
                    BaseAddress = baseUri
                };

                return httpClientOptions;
            }, cancellationToken);
        }

        // For authenticated, we need ModifyClient which captures instance state, but we avoid capturing _baseUri
        Func<HttpClient, ValueTask> modifyClient = ModifyClient;
        return _httpClientCache.Get(_authenticated, (baseUri: _baseUri, modifyClient: modifyClient), static state =>
        {
            var httpClientOptions = new HttpClientOptions
            {
                BaseAddress = state.baseUri,
                // Only sets an initial header when the HttpClient is first created.
                // We still ensure freshness on each request.
                ModifyClient = state.modifyClient
            };

            return httpClientOptions;
        }, cancellationToken);
    }

    public ValueTask<string> GetAccessToken(CancellationToken cancellationToken = default) =>
        _sessionUtil.GetAccessToken(cancellationToken);

    public ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, bool? allowAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions
        {
            Uri = uri,
            Object = obj,
            LogRequest = true,
            LogResponse = logResponse,
            AllowAnonymous = allowAnonymous
        };

        return Post(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default)
    {
        bool anonymous = options.AllowAnonymous.GetValueOrDefault();
        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        HttpClient client = await GetClient(anonymous, cancellationToken)
            .NoSync();

        if (!anonymous)
            await EnsureAuthHeader(client, cancellationToken)
                .NoSync();

        using var content = options.Object?.ToHttpContent();

        if (logReq)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await LogRequest(requestUri, content, HttpMethod.Post, cancellationToken)
                .NoSync();
        }

        HttpCompletionOption completion = logRes ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Uri);
        request.Content = content;

        HttpResponseMessage response = await client.SendAsync(request, completion, cancellationToken)
                                                   .NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken)
                .NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions
        {
            Uri = uri,
            AllowAnonymous = allowAnonymous,
            LogRequest = true,
            LogResponse = true
        };

        return Get(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default)
    {
        bool anonymous = options.AllowAnonymous.GetValueOrDefault();
        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        HttpClient client = await GetClient(anonymous, cancellationToken)
            .NoSync();

        if (!anonymous)
            await EnsureAuthHeader(client, cancellationToken)
                .NoSync();

        if (logReq)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await LogRequest(requestUri, null, HttpMethod.Get, cancellationToken)
                .NoSync();
        }

        HttpCompletionOption completion = logRes ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

        HttpResponseMessage response = await client.GetAsync(options.Uri, completion, cancellationToken)
                                                   .NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken)
                .NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Put(string uri, object obj, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions
        {
            Uri = uri,
            Object = obj,
            LogRequest = true,
            LogResponse = true,
            AllowAnonymous = allowAnonymous
        };

        return Put(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default)
    {
        bool anonymous = options.AllowAnonymous.GetValueOrDefault();
        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        HttpClient client = await GetClient(anonymous, cancellationToken)
            .NoSync();

        if (!anonymous)
            await EnsureAuthHeader(client, cancellationToken)
                .NoSync();

        using var content = options.Object?.ToHttpContent();

        if (logReq)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await LogRequest(requestUri, content, HttpMethod.Put, cancellationToken)
                .NoSync();
        }

        HttpCompletionOption completion = logRes ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

        using var request = new HttpRequestMessage(HttpMethod.Put, options.Uri);
        request.Content = content;

        HttpResponseMessage response = await client.SendAsync(request, completion, cancellationToken)
                                                   .NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken)
                .NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions
        {
            Uri = uri,
            LogRequest = true,
            LogResponse = true
        };

        return Delete(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default)
    {
        bool anonymous = options.AllowAnonymous.GetValueOrDefault();
        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        HttpClient client = await GetClient(anonymous, cancellationToken)
            .NoSync();

        if (!anonymous)
            await EnsureAuthHeader(client, cancellationToken)
                .NoSync();

        if (logReq)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await LogRequest(requestUri, null, HttpMethod.Delete, cancellationToken)
                .NoSync();
        }

        HttpCompletionOption completion = logRes ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

        using var request = new HttpRequestMessage(HttpMethod.Delete, options.Uri);

        HttpResponseMessage response = await client.SendAsync(request, completion, cancellationToken)
                                                   .NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken)
                .NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default)
    {
        bool logReq = options.LogRequest.GetValueOrDefault();

        HttpClient client = await GetClient(allowAnonymous: false, cancellationToken)
            .NoSync();

        await EnsureAuthHeader(client, cancellationToken)
            .NoSync();

        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(options.Stream);
        fileContent.Headers.ContentType = _octetStreamMediaType;

        content.Add(fileContent, "file", options.FileName);

        if (options.Object is not null)
        {
            string? json = JsonUtil.Serialize(options.Object);
            var jsonContent = new StringContent(json ?? "null", _utf8Encoding, "application/json");
            content.Add(jsonContent, "json");
        }

        if (logReq)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await LogRequest(requestUri, null, HttpMethod.Post, cancellationToken)
                .NoSync();
        }

        HttpResponseMessage response = await client.PostAsync(options.Uri, content, cancellationToken)
                                                   .NoSync();
        return response;
    }

    private async ValueTask ModifyClient(HttpClient httpClient)
    {
        // Called only during HttpClient creation via cache.
        // Still do the same "header cache" logic to avoid an extra header allocation if possible.
        string accessToken = await _sessionUtil.GetAccessToken()
                                               .NoSync();

        if (!string.Equals(_cachedAccessToken, accessToken, StringComparison.Ordinal))
        {
            _cachedAccessToken = accessToken;
            _cachedAuthHeader = new AuthenticationHeaderValue(_authScheme, accessToken);
        }

        httpClient.DefaultRequestHeaders.Authorization = _cachedAuthHeader;
    }

    private async ValueTask EnsureAuthHeader(HttpClient client, CancellationToken cancellationToken)
    {
        string accessToken = await _sessionUtil.GetAccessToken(cancellationToken)
                                               .NoSync();

        if (string.Equals(_cachedAccessToken, accessToken, StringComparison.Ordinal))
        {
            AuthenticationHeaderValue? cached = _cachedAuthHeader;
            if (!ReferenceEquals(client.DefaultRequestHeaders.Authorization, cached))
                client.DefaultRequestHeaders.Authorization = cached;

            return;
        }

        _cachedAccessToken = accessToken;
        _cachedAuthHeader = new AuthenticationHeaderValue(_authScheme, accessToken);
        client.DefaultRequestHeaders.Authorization = _cachedAuthHeader;
    }

    private string BuildRequestUri(string uri)
    {
        if (_baseAddressTrimmed is null || uri.IsNullOrEmpty())
            return uri;

        if (Uri.TryCreate(uri, UriKind.Absolute, out _))
            return uri;

        if (uri[0] == '/')
            return string.Concat(_baseAddressTrimmed, uri);

        return string.Concat(_baseAddressTrimmed, "/", uri);
    }

    private ValueTask LogRequest(string requestUri, HttpContent? httpContent, HttpMethod? httpMethod, CancellationToken cancellationToken) =>
        _requestResponseLogging ? _logJsonInterop.LogRequest(requestUri, httpContent, httpMethod, cancellationToken) : ValueTask.CompletedTask;

    private ValueTask LogResponse(HttpResponseMessage response, CancellationToken cancellationToken) =>
        _requestResponseLogging ? _logJsonInterop.LogResponse(response, cancellationToken) : ValueTask.CompletedTask;
}