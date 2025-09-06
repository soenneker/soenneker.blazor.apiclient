using Soenneker.Blazor.ApiClient.Dtos;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.ApiClient.Abstract;

/// <summary>
/// Defines methods for configuring and interacting with the API,
/// including HTTP operations, authentication, and optional request/response logging.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Initializes the client with the specified base address and logging setting.
    /// Must be called before performing any HTTP operations.
    /// </summary>
    /// <param name="baseAddress">The base URI of the API endpoints.</param>
    /// <param name="requestResponseLogging">Whether to enable detailed request and response logging.</param>
    void Initialize(string baseAddress, bool requestResponseLogging);

    /// <summary>
    /// Retrieves or creates an <see cref="HttpClient"/> instance configured for authenticated or anonymous requests.
    /// </summary>
    /// <param name="allowAnonymous">If true, allows anonymous requests (no bearer token); otherwise requires authentication.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the configured <see cref="HttpClient"/>.</returns>
    ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests and returns a fresh access token using the configured authentication provider.
    /// </summary>
    /// <returns>A task that returns the access token string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the user is not authenticated or if the token could not be acquired.
    /// </exception>
    ValueTask<string> GetAccessToken();

    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="uri">The relative URI of the resource.</param>
    /// <param name="allowAnonymous">If true, allows anonymous requests; otherwise uses authentication.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a GET request using the specified <see cref="RequestOptions"/>.
    /// </summary>
    /// <param name="options">Options including URI, logging flags, and anonymity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a JSON-serializable payload.
    /// </summary>
    /// <param name="uri">The relative URI of the endpoint.</param>
    /// <param name="obj">The object to serialize as JSON in the request body.</param>
    /// <param name="logResponse">Whether to log the response.</param>
    /// <param name="allowAnonymous">If true, allows anonymous requests; otherwise uses authentication.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, bool? allowAnonymous = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request using the specified <see cref="RequestOptions"/>.
    /// </summary>
    /// <param name="options">Options including URI, payload, and logging flags.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with a JSON-serializable payload.
    /// </summary>
    /// <param name="uri">The relative URI of the resource.</param>
    /// <param name="obj">The object to serialize as JSON in the request body.</param>
    /// <param name="allowAnonymous">If true, allows anonymous requests; otherwise uses authentication.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Put(string uri, object obj, bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request using the specified <see cref="RequestOptions"/>.
    /// </summary>
    /// <param name="options">Options including URI, payload, and logging flags.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request to the specified URI.
    /// </summary>
    /// <param name="uri">The relative URI of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request using the specified <see cref="RequestOptions"/>.
    /// </summary>
    /// <param name="options">Options including URI and logging flags.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file stream with optional JSON metadata.
    /// </summary>
    /// <param name="options">Options including target URI, file stream, filename, and metadata object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default);
}
