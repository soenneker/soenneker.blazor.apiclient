using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Net.Http.Headers;
using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.ApiClient.Dtos;
using Soenneker.Blazor.LogJson.Abstract;
using Soenneker.Blazor.Utils.Session.Abstract;
using Soenneker.Extensions.DateTimeOffset;
using Soenneker.Extensions.Object;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.HttpClientCache.Dtos;
using Soenneker.Utils.Json;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Soenneker.Blazor.ApiClient;

///<inheritdoc cref="IApiClient"/>
public class ApiClient : IApiClient
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogJsonInterop _logJsonInterop;
    private readonly IHttpClientCache _httpClientCache;
    private readonly ISessionUtil _sessionUtil;

    private DateTime? _jwtExpiration;

    private string? _baseAddress;
    private bool _requestResponseLogging;

    private const string _anonymous = $"{nameof(ApiClient)}-anonymous";
    private const string _authenticated = $"{nameof(ApiClient)}-authenticated";

    public ApiClient(IAccessTokenProvider accessTokenProvider, ISessionUtil sessionUtil, ILogJsonInterop logJsonInterop, IHttpClientCache httpClientCache)
    {
        _accessTokenProvider = accessTokenProvider;
        _sessionUtil = sessionUtil;
        _logJsonInterop = logJsonInterop;
        _httpClientCache = httpClientCache;
    }

    public void Initialize(string baseAddress, bool requestResponseLogging)
    {
        _baseAddress = baseAddress;
        _requestResponseLogging = requestResponseLogging;
    }

    public async ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        string clientName = allowAnonymous.GetValueOrDefault() ? _anonymous : _authenticated;

        var httpClientOptions = new HttpClientOptions();

        if (!_baseAddress.IsNullOrEmpty())
            httpClientOptions.BaseAddress = _baseAddress;

        if (!allowAnonymous.GetValueOrDefault())
        {
            httpClientOptions.ModifyClient = ModifyClient;
        }

        return await _httpClientCache.Get(clientName, httpClientOptions, cancellationToken).NoSync();
    }

    /// <summary>
    /// Fairly heavy operation
    /// </summary>
    public async ValueTask<string> GetAccessToken()
    {
        AccessTokenResult accessTokenResult = await _accessTokenProvider.RequestAccessToken().NoSync();
        accessTokenResult.TryGetToken(out AccessToken? accessToken);

        if (accessToken == null || accessToken.Value.IsNullOrEmpty())
        {
            await _sessionUtil.ExpireSession(false).NoSync();
            throw new Exception("Access token was null or empty, expiring session");
        }

        _jwtExpiration = accessToken.Expires.ToUtcDateTime();
        await _sessionUtil.UpdateWithAccessToken(_jwtExpiration.Value).NoSync();

        return accessToken.Value;
    }

    public ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, Object = obj, LogRequest = true, LogResponse = logResponse};

        return Post(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpContent? httpContent = null;

        if (options.Object != null)
            httpContent = options.Object.ToHttpContent();

        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest)
            await LogRequest($"{client.BaseAddress}{options.Uri}", httpContent, HttpMethod.Post, cancellationToken).NoSync();

        HttpResponseMessage response = await client.PostAsync(options.Uri, httpContent, cancellationToken).NoSync();

        if (options.LogResponse)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    private async ValueTask ModifyClient(HttpClient httpClient)
    {
        string accessToken = await GetAccessToken().NoSync();
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"bearer {accessToken}");
    }

    public ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, AllowAnonymous = allowAnonymous, LogRequest = true, LogResponse = true};

        return Get(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest)
            await LogRequest($"{client.BaseAddress}{options.Uri}", null, HttpMethod.Get, cancellationToken).NoSync();

        HttpResponseMessage response = await client.GetAsync(options.Uri, cancellationToken).NoSync();

        if (options.LogResponse)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Put(string uri, object obj, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, Object = obj, LogRequest = true, LogResponse = true};

        return Put(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest)
            await LogRequest($"{client.BaseAddress}{options.Uri}", options.Object.ToHttpContent(), HttpMethod.Put, cancellationToken).NoSync();

        var httpContent = options.Object.ToHttpContent();

        HttpResponseMessage response = await client.PutAsync(options.Uri, httpContent, cancellationToken).NoSync();

        if (options.LogResponse)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, LogRequest = true, LogResponse = true};

        return Delete(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest)
            await LogRequest($"{client.BaseAddress}{options.Uri}", null, HttpMethod.Delete, cancellationToken).NoSync();

        HttpResponseMessage response = await client.DeleteAsync(options.Uri, cancellationToken).NoSync();

        if (options.LogResponse)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent
        {
            {new StreamContent(options.Stream) {Headers = {ContentType = new MediaTypeHeaderValue("application/octet-stream")}}, "file", options.FileName}
        };

        if (options.Object != null)
        {
            string? json = JsonUtil.Serialize(options.Object);
            var jsonContent = new StringContent(json!, Encoding.UTF8, "application/json");
            content.Add(jsonContent, "json");
        }

        HttpClient client = await GetClient(cancellationToken: cancellationToken).NoSync();

        if (options.LogRequest)
            await LogRequest($"{client.BaseAddress}{options.Uri}", null, HttpMethod.Post, cancellationToken).NoSync();

        return await client.PostAsync(options.Uri, content, cancellationToken).NoSync();
    }

    private ValueTask LogRequest(string requestUri, HttpContent? httpContent = null, HttpMethod? httpMethod = null, CancellationToken cancellationToken = default)
    {
        if (_requestResponseLogging)
            return _logJsonInterop.LogRequest(requestUri, httpContent, httpMethod, cancellationToken);

        return ValueTask.CompletedTask;
    }

    private ValueTask LogResponse(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (_requestResponseLogging)
            return _logJsonInterop.LogResponse(response, cancellationToken);

        return ValueTask.CompletedTask;
    }
}