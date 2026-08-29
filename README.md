[![](https://img.shields.io/nuget/v/soenneker.blazor.apiclient.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.apiclient/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.apiclient/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.apiclient/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.apiclient.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.apiclient/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.apiclient/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.apiclient/actions/workflows/codeql.yml)

# Soenneker.Blazor.ApiClient

A scoped Blazor API client for JSON requests, authenticated session tokens, optional browser-console logging, and multipart uploads.

## Installation and registration

```bash
dotnet add package Soenneker.Blazor.ApiClient
```

```csharp
using Soenneker.Blazor.ApiClient.Registrars;

builder.Services.AddApiClientAsScoped();
```

The registrar also adds the session, JSON logging, and shared `HttpClient` cache dependencies.

## Initialize the scoped client

`Initialize()` must run before any request:

```csharp
using Soenneker.Blazor.ApiClient.Abstract;

public sealed class WeatherApi
{
    private readonly IApiClient _api;

    public WeatherApi(IApiClient api, IConfiguration configuration)
    {
        _api = api;
        _api.Initialize(
            configuration["Api:BaseUrl"]!,
            requestResponseLogging: false);
    }
}
```

The base address must be absolute and HTTPS. Plain HTTP is accepted only for loopback development addresses.

## Authenticated JSON request

```csharp
public async Task<WeatherForecast?> GetForecast(
    CancellationToken cancellationToken)
{
    using HttpResponseMessage response = await _api.Get(
        "weather/forecast",
        cancellationToken: cancellationToken);

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<WeatherForecast>(
        cancellationToken);
}
```

Authenticated wrapper methods request the current access token from `ISessionUtil` and attach it as a bearer header to that request. Token headers are reused only while the token string remains unchanged.

Use `allowAnonymous: true` for a public endpoint:

```csharp
using HttpResponseMessage response = await _api.Get(
    "health",
    allowAnonymous: true,
    cancellationToken: cancellationToken);
```

Authenticated requests cannot use an absolute URL on a different scheme, host, or port than the configured base address. This prevents forwarding a session token to another origin. Anonymous requests may use absolute URLs.

## Request options

```csharp
using Soenneker.Blazor.ApiClient.Dtos;

using HttpResponseMessage response = await _api.Post(
    new RequestOptions
    {
        Uri = "orders",
        Object = new { productId, quantity = 1 },
        LogRequest = true,
        LogResponse = false
    },
    cancellationToken);
```

`LogRequest` and `LogResponse` take effect only when `requestResponseLogging` was enabled during initialization. Logging may expose request or response data in the browser console; keep it disabled for sensitive production traffic.

## File upload

```csharp
await using FileStream stream = File.OpenRead(path);

using HttpResponseMessage response = await _api.Upload(
    new RequestUploadOptions
    {
        Uri = "documents",
        Stream = stream,
        FileName = Path.GetFileName(path),
        Object = new { category = "invoice" },
        LogRequest = true
    },
    cancellationToken);
```

Uploads are always authenticated. The multipart names are `file` for the binary stream and `json` for optional serialized metadata. The upload disposes its multipart content, which also disposes the supplied stream; do not reuse it afterward.

All methods return the raw `HttpResponseMessage`. The caller owns that response and is responsible for disposal, status handling, and response deserialization. `GetClient()` returns the cached transport client but does not attach authorization automatically; prefer the wrapper methods for authenticated calls.
