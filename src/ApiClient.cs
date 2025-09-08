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

///<inheritdoc cref="IApiClient"/>
public sealed class ApiClient : IApiClient
{
    private readonly ILogJsonInterop _logJsonInterop;
    private readonly IHttpClientCache _httpClientCache;
    private readonly ISessionUtil _sessionUtil;

    private string? _baseAddress;
    private bool _requestResponseLogging;

    private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);
    private const string _authScheme = "Bearer";
    private static readonly MediaTypeHeaderValue _applicationJsonMediaType = new("application/json");

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
        _baseAddress = baseAddress;
        _requestResponseLogging = requestResponseLogging;
    }

    public ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        string clientName = allowAnonymous.GetValueOrDefault() ? _anonymous : _authenticated;

        var httpClientOptions = new HttpClientOptions();

        if (!_baseAddress.IsNullOrEmpty())
            httpClientOptions.BaseAddress = _baseAddress;

        // Keep this for initial header on first create; we still ensure freshness per-request.
        if (!allowAnonymous.GetValueOrDefault())
            httpClientOptions.ModifyClient = ModifyClient;

        return _httpClientCache.Get(clientName, httpClientOptions, cancellationToken);
    }

    public ValueTask<string> GetAccessToken(CancellationToken cancellationToken = default)
    {
        return _sessionUtil.GetAccessToken(cancellationToken);
    }

    public ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions { Uri = uri, Object = obj, LogRequest = true, LogResponse = logResponse, AllowAnonymous = allowAnonymous };
        return Post(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (!options.AllowAnonymous.GetValueOrDefault())
            await EnsureAuthHeader(client, cancellationToken).NoSync();

        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        using var content = options.Object?.ToHttpContent();

        if (logReq)
        {
            string requestUri = client.BaseAddress is not null ? new Uri(client.BaseAddress, options.Uri).ToString() : options.Uri;

            await LogRequest(requestUri, content, HttpMethod.Post, cancellationToken).NoSync();
        }

        HttpResponseMessage response = await client.PostAsync(options.Uri, content, cancellationToken).NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    private async ValueTask ModifyClient(HttpClient httpClient)
    {
        string accessToken = await GetAccessToken().NoSync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_authScheme, accessToken);
    }

    private async ValueTask EnsureAuthHeader(HttpClient client, CancellationToken cancellationToken)
    {
        string accessToken = await GetAccessToken(cancellationToken).NoSync();

        AuthenticationHeaderValue? current = client.DefaultRequestHeaders.Authorization;
        // Compare parameter; avoid header churn
        if (current is null || !string.Equals(current.Parameter, accessToken, StringComparison.Ordinal))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_authScheme, accessToken);
    }

    public ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions { Uri = uri, AllowAnonymous = allowAnonymous, LogRequest = true, LogResponse = true };
        return Get(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (!options.AllowAnonymous.GetValueOrDefault())
            await EnsureAuthHeader(client, cancellationToken).NoSync();

        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        if (logReq)
        {
            string requestUri = client.BaseAddress is not null ? new Uri(client.BaseAddress, options.Uri).ToString() : options.Uri;

            await LogRequest(requestUri, null, HttpMethod.Get, cancellationToken);
        }

        HttpCompletionOption completion = logRes ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;

        HttpResponseMessage response = await client.GetAsync(options.Uri, completion, cancellationToken).NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Put(string uri, object obj, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions { Uri = uri, Object = obj, LogRequest = true, LogResponse = true, AllowAnonymous = allowAnonymous };
        return Put(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (!options.AllowAnonymous.GetValueOrDefault())
            await EnsureAuthHeader(client, cancellationToken).NoSync();

        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        using var content = options.Object?.ToHttpContent();

        if (logReq)
        {
            string requestUri = client.BaseAddress is not null ? new Uri(client.BaseAddress, options.Uri).ToString() : options.Uri;

            await LogRequest(requestUri, content, HttpMethod.Put, cancellationToken).NoSync();
        }

        HttpResponseMessage response = await client.PutAsync(options.Uri, content, cancellationToken).NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions { Uri = uri, LogRequest = true, LogResponse = true };
        return Delete(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (!options.AllowAnonymous.GetValueOrDefault())
            await EnsureAuthHeader(client, cancellationToken).NoSync();

        bool logReq = options.LogRequest.GetValueOrDefault();
        bool logRes = options.LogResponse.GetValueOrDefault();

        if (logReq)
        {
            string requestUri = client.BaseAddress is not null ? new Uri(client.BaseAddress, options.Uri).ToString() : options.Uri;

            await LogRequest(requestUri, null, HttpMethod.Delete, cancellationToken).NoSync();
        }

        HttpResponseMessage response = await client.DeleteAsync(options.Uri, cancellationToken).NoSync();

        if (logRes)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(cancellationToken: cancellationToken).NoSync();

        await EnsureAuthHeader(client, cancellationToken).NoSync();

        bool logReq = options.LogRequest.GetValueOrDefault();

        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(options.Stream)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
        }, "file", options.FileName);

        if (options.Object is not null)
        {
            // Keep JsonUtil path consistent with the rest of the app
            string? json = JsonUtil.Serialize(options.Object);
            var jsonContent = new StringContent(json, _utf8Encoding);
            jsonContent.Headers.ContentType = _applicationJsonMediaType;
            content.Add(jsonContent, "json");
        }

        if (logReq)
        {
            string requestUri = client.BaseAddress is not null ? new Uri(client.BaseAddress, options.Uri).ToString() : options.Uri;

            await LogRequest(requestUri, null, HttpMethod.Post, cancellationToken).NoSync();
        }

        HttpResponseMessage response = await client.PostAsync(options.Uri, content, cancellationToken).NoSync();
        return response;
    }

    private ValueTask LogRequest(string requestUri, HttpContent? httpContent, HttpMethod? httpMethod, CancellationToken cancellationToken) =>
        _requestResponseLogging ? _logJsonInterop.LogRequest(requestUri, httpContent, httpMethod, cancellationToken) : ValueTask.CompletedTask;

    private ValueTask LogResponse(HttpResponseMessage response, CancellationToken cancellationToken) =>
        _requestResponseLogging ? _logJsonInterop.LogResponse(response, cancellationToken) : ValueTask.CompletedTask;
}