using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.ApiClient.Dtos;
using Soenneker.Blazor.LogJson.Abstract;
using Soenneker.Blazor.Utils.Session.Abstract;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.Object;
using Soenneker.Extensions.String;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.ApiClient;

/// <inheritdoc cref="IApiClient"/>
public sealed class ApiClient : IApiClient
{
    private readonly ILogJsonInterop _logJsonInterop;
    private readonly IHttpClientCache _httpClientCache;
    private readonly ISessionUtil _sessionUtil;

    private string? _baseAddressTrimmed;
    private Uri _baseUri = null!;
    private bool _requestResponseLogging;

    // Header cache only. Token retrieval is still delegated to SessionUtil.
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
        if (!baseAddress.HasContent())
            throw new InvalidOperationException("BaseAddress must be set");

        _baseAddressTrimmed = baseAddress.TrimEnd('/');
        _baseUri = new Uri(baseAddress, UriKind.Absolute);
        _requestResponseLogging = requestResponseLogging;
    }

    public ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        if (allowAnonymous.GetValueOrDefault())
        {
            return _httpClientCache.Get(_anonymous, _baseUri, static baseUri =>
            {
                return new HttpClientOptions
                {
                    BaseAddress = baseUri
                };
            }, cancellationToken);
        }

        // Important for Blazor WASM:
        // Do NOT fetch/access tokens during HttpClient creation. Token acquisition can require
        // the auth/JS pipeline to be ready. Apply Authorization per request instead.
        return _httpClientCache.Get(_authenticated, _baseUri, static baseUri =>
        {
            return new HttpClientOptions
            {
                BaseAddress = baseUri
            };
        }, cancellationToken);
    }

    public ValueTask<string> GetAccessToken(CancellationToken cancellationToken = default) =>
        _sessionUtil.GetAccessToken(cancellationToken);

    public ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true,
        bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Post, uri, obj, allowAnonymous.GetValueOrDefault(), logRequest: true, logResponse,
            cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Post, options.Uri, options.Object, options.AllowAnonymous.GetValueOrDefault(),
            options.LogRequest.GetValueOrDefault(), options.LogResponse.GetValueOrDefault(), cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Get, uri, body: null, allowAnonymous.GetValueOrDefault(), logRequest: true,
            logResponse: true, cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Get, options.Uri, body: null, options.AllowAnonymous.GetValueOrDefault(),
            options.LogRequest.GetValueOrDefault(), options.LogResponse.GetValueOrDefault(), cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Put(string uri, object obj, bool? allowAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Put, uri, obj, allowAnonymous.GetValueOrDefault(), logRequest: true,
            logResponse: true, cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Put, options.Uri, options.Object, options.AllowAnonymous.GetValueOrDefault(),
            options.LogRequest.GetValueOrDefault(), options.LogResponse.GetValueOrDefault(), cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Delete, uri, body: null, allowAnonymous: false, logRequest: true, logResponse: true,
            cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default)
    {
        return SendCore(HttpMethod.Delete, options.Uri, body: null, options.AllowAnonymous.GetValueOrDefault(),
            options.LogRequest.GetValueOrDefault(), options.LogResponse.GetValueOrDefault(), cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(allowAnonymous: false, cancellationToken).ConfigureAwait(false);

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

        bool effectiveLogRequest = _requestResponseLogging && options.LogRequest.GetValueOrDefault();

        if (effectiveLogRequest)
        {
            string requestUri = BuildRequestUri(options.Uri);
            await _logJsonInterop.LogRequest(requestUri, null, HttpMethod.Post, cancellationToken)
                                 .ConfigureAwait(false);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Uri);
        request.Content = content;

        request.Headers.Authorization = await GetAuthHeader(cancellationToken).ConfigureAwait(false);

        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                           .ConfigureAwait(false);
    }

    private async ValueTask<HttpResponseMessage> SendCore(HttpMethod method, string uri, object? body,
        bool allowAnonymous, bool logRequest, bool logResponse, CancellationToken cancellationToken)
    {
        HttpClient client = await GetClient(allowAnonymous, cancellationToken).ConfigureAwait(false);

        using var content = body?.ToHttpContent();

        bool effectiveLogRequest = _requestResponseLogging && logRequest;
        bool effectiveLogResponse = _requestResponseLogging && logResponse;

        if (effectiveLogRequest)
        {
            string requestUri = BuildRequestUri(uri);
            await _logJsonInterop.LogRequest(requestUri, content, method, cancellationToken).ConfigureAwait(false);
        }

        HttpCompletionOption completion = effectiveLogResponse
            ? HttpCompletionOption.ResponseContentRead
            : HttpCompletionOption.ResponseHeadersRead;

        using var request = new HttpRequestMessage(method, uri);

        if (content is not null)
            request.Content = content;

        if (!allowAnonymous)
            request.Headers.Authorization = await GetAuthHeader(cancellationToken).ConfigureAwait(false);

        HttpResponseMessage response =
            await client.SendAsync(request, completion, cancellationToken).ConfigureAwait(false);

        if (effectiveLogResponse)
            await _logJsonInterop.LogResponse(response, cancellationToken).ConfigureAwait(false);

        return response;
    }

    private async ValueTask<AuthenticationHeaderValue> GetAuthHeader(CancellationToken cancellationToken)
    {
        string accessToken = await _sessionUtil.GetAccessToken(cancellationToken).ConfigureAwait(false);

        AuthenticationHeaderValue? cached = _cachedAuthHeader;

        if (cached is not null && string.Equals(_cachedAccessToken, accessToken, StringComparison.Ordinal))
        {
            return cached;
        }

        var header = new AuthenticationHeaderValue(_authScheme, accessToken);

        _cachedAccessToken = accessToken;
        _cachedAuthHeader = header;

        return header;
    }

    private string BuildRequestUri(string uri)
    {
        if (_baseAddressTrimmed is null || uri.IsNullOrEmpty())
            return uri;

        if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        return uri[0] == '/' ? string.Concat(_baseAddressTrimmed, uri) : string.Concat(_baseAddressTrimmed, "/", uri);
    }
}