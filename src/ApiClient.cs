using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Net.Http.Headers;
using Soenneker.Blazor.ApiClient.Abstract;
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
        string clientName = GetClientName(allowAnonymous);

        HttpClient client;

        if (allowAnonymous.GetValueOrDefault())
        {
            client = await _httpClientCache.Get(clientName, cancellationToken: cancellationToken);
        }
        else
        {
            client = await _httpClientCache.Get(clientName, new HttpClientOptions {ModifyClient = ModifyClient}, cancellationToken);
        }

        if (!_baseAddress.IsNullOrEmpty())
            client.BaseAddress = new Uri(_baseAddress);

        return client;
    }

    private static string GetClientName(bool? allowAnonymous = false)
    {
        return $"{nameof(ApiClient)}-{allowAnonymous}";
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

    public async ValueTask<HttpResponseMessage> Post(string requestUri, object? obj, bool logResponse = true, CancellationToken cancellationToken = default)
    {
        HttpContent? httpContent = null;

        if (obj != null)
            httpContent = obj.ToHttpContent();

        HttpClient client = await GetClient(false, cancellationToken).NoSync();

        await LogRequest($"{client.BaseAddress}{requestUri}", httpContent, HttpMethod.Post, cancellationToken).NoSync();

        HttpResponseMessage response = await client.PostAsync(requestUri, httpContent, cancellationToken).NoSync();

        if (logResponse)
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    private async ValueTask ModifyClient(HttpClient httpClient)
    {
        string accessToken = await GetAccessToken().NoSync();
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"bearer {accessToken}");
    }

    public async ValueTask<HttpResponseMessage> Get(string requestUri, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        await LogRequest(requestUri, null, HttpMethod.Get, cancellationToken).NoSync();

        HttpClient client = await GetClient(allowAnonymous, cancellationToken).NoSync();

        HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken).NoSync();

        await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Put(string requestUri, object obj, CancellationToken cancellationToken = default)
    {
        var httpContent = obj.ToHttpContent();

        await LogRequest(requestUri, httpContent, HttpMethod.Put, cancellationToken).NoSync();

        HttpClient client = await GetClient(false, cancellationToken).NoSync();
        HttpResponseMessage response = await client.PutAsync(requestUri, httpContent, cancellationToken).NoSync();

        await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Delete(string requestUri, CancellationToken cancellationToken = default)
    {
        await LogRequest(requestUri, null, HttpMethod.Delete, cancellationToken).NoSync();

        HttpClient client = await GetClient(false, cancellationToken).NoSync();
        HttpResponseMessage response = await client.DeleteAsync(requestUri, cancellationToken).NoSync();

        await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Upload(string requestUri, Stream stream, string fileName, object? obj = null, CancellationToken cancellationToken = default)
    {
        await LogRequest(requestUri, null, HttpMethod.Post, cancellationToken).NoSync();

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        if (obj != null)
        {
            string? json = JsonUtil.Serialize(obj);
            var jsonContent = new StringContent(json!, Encoding.UTF8, "application/json");
            content.Add(jsonContent, "json");
        }

        HttpClient client = await GetClient(cancellationToken: cancellationToken).NoSync();

        HttpResponseMessage response = await client.PostAsync(requestUri, content, cancellationToken).NoSync();
        response.EnsureSuccessStatusCode();
        return response;
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