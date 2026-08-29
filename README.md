[![](https://img.shields.io/nuget/v/soenneker.blazor.apiclient.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.apiclient/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.apiclient/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.apiclient/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.apiclient.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.apiclient/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.apiclient/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.apiclient/actions/workflows/codeql.yml)

# Soenneker.Blazor.ApiClient

Defines methods for configuring and interacting with the API, including HTTP operations, authentication, and optional request/response logging.

## Install

```bash
dotnet add package Soenneker.Blazor.ApiClient
```

## Quick start

```csharp
using Soenneker.Blazor.ApiClient.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddApiClientAsScoped();
```

Adds `IApiClient` as a scoped service.

## What you get

- `IApiClient` — Defines methods for configuring and interacting with the API, including HTTP operations, authentication, and optional request/response logging.
- `ApiClientRegistrar` — A lightweight and efficient API client wrapper for Blazor applications, simplifying HTTP communication with support for asynchronous calls, cancellation tokens, and JSON serialization.
- `RequestOptions` — Represents the options for making an API request, including the URI, request payload, and logging settings.
- `RequestUploadOptions` — Represents the options for uploading a file via an API request, extending `RequestOptions` with additional file upload properties.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IApiClient.Initialize(baseAddress, requestResponseLogging)` | Initializes the client with the specified base address and logging setting. Must be called before performing any HTTP operations. | Returns no value; the requested change is complete when the method returns. |
| `IApiClient.GetClient(allowAnonymous, cancellationToken)` | Retrieves or creates an `HttpClient` instance configured for authenticated or anonymous requests. | A task that returns the configured `HttpClient`. |
| `IApiClient.GetAccessToken(cancellationToken)` | Requests and returns a fresh access token using the configured authentication provider. | A task that returns the access token string. |
| `IApiClient.Get(uri, allowAnonymous, cancellationToken)` | Sends a GET request to the specified URI. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Get(options, cancellationToken)` | Sends a GET request using the specified `RequestOptions`. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Post(uri, obj, logResponse, allowAnonymous, cancellationToken)` | Sends a POST request with a JSON-serializable payload. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Post(options, cancellationToken)` | Sends a POST request using the specified `RequestOptions`. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Put(uri, obj, allowAnonymous, cancellationToken)` | Sends a PUT request with a JSON-serializable payload. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Put(options, cancellationToken)` | Sends a PUT request using the specified `RequestOptions`. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Delete(uri, cancellationToken)` | Sends a DELETE request to the specified URI. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Delete(options, cancellationToken)` | Sends a DELETE request using the specified `RequestOptions`. | A task that returns the `HttpResponseMessage`. |
| `IApiClient.Upload(options, cancellationToken)` | Uploads a file stream with optional JSON metadata. | A task that returns the `HttpResponseMessage`. |
| `ApiClientRegistrar.AddApiClientAsScoped(services)` | Adds `IApiClient` as a scoped service. | The same service collection, so additional registrations can be chained. |
| `RequestOptions.Uri` | Gets or sets the URI for the API request. | Gets or sets the URI for the API request. |
| `RequestOptions.Object` | Gets or sets an optional object that will be serialized and sent as the request body. | Gets or sets an optional object that will be serialized and sent as the request body. |
| `RequestOptions.AllowAnonymous` | Gets or sets a value indicating whether the request should allow anonymous access. Defaults to null (false). | Gets or sets a value indicating whether the request should allow anonymous access. Defaults to null (false). |
| `RequestOptions.LogResponse` | Gets or sets a value indicating whether the response should be logged. Defaults to null (false). | Gets or sets a value indicating whether the response should be logged. Defaults to null (false). |
| `RequestOptions.LogRequest` | Gets or sets a value indicating whether the request should be logged. Defaults to null (false). | Gets or sets a value indicating whether the request should be logged. Defaults to null (false). |

## Important behavior

- `IApiClient.GetAccessToken(cancellationToken)`: Thrown if the user is not authenticated or if the token could not be acquired.

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
