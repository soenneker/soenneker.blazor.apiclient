using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.ApiClient.Dtos;
using Soenneker.Blazor.LogJson.Abstract;
using Soenneker.Blazor.Utils.Session.Abstract;
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
using Soenneker.Dtos.HttpClientOptions;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Soenneker.Blazor.ApiClient;

///<inheritdoc cref="IApiClient"/>
public sealed class ApiClient : IApiClient
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogJsonInterop _logJsonInterop;
    private readonly IHttpClientCache _httpClientCache;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ISessionUtil _sessionUtil;

    private DateTime? _jwtExpiration;

    private string? _baseAddress;
    private bool _requestResponseLogging;

    private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);
    private const string _authScheme = "Bearer";
    private static readonly MediaTypeHeaderValue _applicationJsonMediaType = new("application/json");

    private const string _anonymous = $"{nameof(ApiClient)}-anonymous";
    private const string _authenticated = $"{nameof(ApiClient)}-authenticated";

    public ApiClient(IAccessTokenProvider accessTokenProvider, ISessionUtil sessionUtil, ILogJsonInterop logJsonInterop, IHttpClientCache httpClientCache,
        AuthenticationStateProvider authStateProvider)
    {
        _accessTokenProvider = accessTokenProvider;
        _sessionUtil = sessionUtil;
        _logJsonInterop = logJsonInterop;
        _httpClientCache = httpClientCache;
        _authStateProvider = authStateProvider;
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
            httpClientOptions.ModifyClient = ModifyClient;

        return await _httpClientCache.Get(clientName, httpClientOptions, cancellationToken).NoSync();
    }

    public async ValueTask<string> GetAccessToken()
    {
        AuthenticationState state = await _authStateProvider.GetAuthenticationStateAsync().NoSync();

        if (state.User.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("User is not authenticated");

        AccessTokenResult result = await _accessTokenProvider.RequestAccessToken().NoSync();

        if (!result.TryGetToken(out AccessToken? token) || token.Value.IsNullOrWhiteSpace())
        {
            await _sessionUtil.ExpireSession(false).NoSync();
            throw new InvalidOperationException("Access token could not be acquired or was empty.");
        }

        _jwtExpiration = token.Expires.UtcDateTime;
        await _sessionUtil.UpdateWithAccessToken(_jwtExpiration.Value).NoSync();

        return token.Value;
    }

    public ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, bool? allowAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, Object = obj, LogRequest = true, LogResponse = logResponse, AllowAnonymous = allowAnonymous};
        return Post(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default)
    {
        var httpContent = options.Object?.ToHttpContent();
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest.GetValueOrDefault())
            await LogRequest(new Uri(client.BaseAddress!, options.Uri).ToString(), httpContent, HttpMethod.Post, cancellationToken).NoSync();

        HttpResponseMessage response = await client.PostAsync(options.Uri, httpContent, cancellationToken).NoSync();

        if (options.LogResponse.GetValueOrDefault())
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    private async ValueTask ModifyClient(HttpClient httpClient)
    {
        string accessToken = await GetAccessToken().NoSync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_authScheme, accessToken);
    }

    public ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, AllowAnonymous = allowAnonymous, LogRequest = true, LogResponse = true};
        return Get(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default)
    {
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest.GetValueOrDefault())
            await LogRequest(new Uri(client.BaseAddress!, options.Uri).ToString(), null, HttpMethod.Get, cancellationToken).NoSync();

        HttpResponseMessage response = await client.GetAsync(options.Uri, cancellationToken).NoSync();

        if (options.LogResponse.GetValueOrDefault())
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public ValueTask<HttpResponseMessage> Put(string uri, object obj, bool? allowAnonymous = false, CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions {Uri = uri, Object = obj, LogRequest = true, LogResponse = true, AllowAnonymous = allowAnonymous};
        return Put(options, cancellationToken);
    }

    public async ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default)
    {
        var httpContent = options.Object.ToHttpContent();
        HttpClient client = await GetClient(options.AllowAnonymous, cancellationToken).NoSync();

        if (options.LogRequest.GetValueOrDefault())
            await LogRequest(new Uri(client.BaseAddress!, options.Uri).ToString(), httpContent, HttpMethod.Put, cancellationToken).NoSync();

        HttpResponseMessage response = await client.PutAsync(options.Uri, httpContent, cancellationToken).NoSync();

        if (options.LogResponse.GetValueOrDefault())
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

        if (options.LogRequest.GetValueOrDefault())
            await LogRequest(new Uri(client.BaseAddress!, options.Uri).ToString(), null, HttpMethod.Delete, cancellationToken).NoSync();

        HttpResponseMessage response = await client.DeleteAsync(options.Uri, cancellationToken).NoSync();

        if (options.LogResponse.GetValueOrDefault())
            await LogResponse(response, cancellationToken).NoSync();

        return response;
    }

    public async ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent
        {
            {
                new StreamContent(options.Stream) {Headers = {ContentType = new MediaTypeHeaderValue("application/octet-stream")}},
                "file", options.FileName
            }
        };

        if (options.Object != null)
        {
            string? json = JsonUtil.Serialize(options.Object);
            var jsonContent = new StringContent(json!, _utf8Encoding);
            jsonContent.Headers.ContentType = _applicationJsonMediaType;
            content.Add(jsonContent, "json");
        }

        HttpClient client = await GetClient(cancellationToken: cancellationToken).NoSync();

        if (options.LogRequest.GetValueOrDefault())
            await LogRequest(new Uri(client.BaseAddress!, options.Uri).ToString(), null, HttpMethod.Post, cancellationToken).NoSync();

        return await client.PostAsync(options.Uri, content, cancellationToken).NoSync();
    }

    private ValueTask LogRequest(string requestUri, HttpContent? httpContent, HttpMethod? httpMethod, CancellationToken cancellationToken)
    {
        return _requestResponseLogging ? _logJsonInterop.LogRequest(requestUri, httpContent, httpMethod, cancellationToken) : ValueTask.CompletedTask;
    }

    private ValueTask LogResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return _requestResponseLogging ? _logJsonInterop.LogResponse(response, cancellationToken) : ValueTask.CompletedTask;
    }
}