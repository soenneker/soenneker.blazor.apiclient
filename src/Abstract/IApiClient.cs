using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.ApiClient.Dtos;

namespace Soenneker.Blazor.ApiClient.Abstract;

/// <summary>
/// A lightweight and efficient API client wrapper for Blazor applications. Simplifies HTTP communication with support for asynchronous calls, cancellation tokens, and JSON serialization.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Initializes the API client with a base URI and an optional flag to enable logging of request and response details.
    /// </summary>
    /// <param name="baseAddress">The base URI for the API.</param>
    /// <param name="requestResponseLogging">A flag to indicate whether request and response logging should be enabled.</param>
    void Initialize(string baseAddress, bool requestResponseLogging);

    /// <summary>
    /// Asynchronously retrieves the current access token for the API client.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the access token as a string.</returns>
    ValueTask<string> GetAccessToken();

    /// <summary>
    /// Asynchronously retrieves a configured <see cref="HttpClient"/> instance with an access token attached.
    /// Uses double-check locking to ensure the access token is cached appropriately. <para/>
    /// The returned <see cref="HttpClient"/> exists for the duration of the user's session.
    /// </summary>
    /// <param name="allowAnonymous">An optional flag to allow anonymous requests. Defaults to false.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the configured <see cref="HttpClient"/>.</returns>
    ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a POST request to the specified URI with an optional object payload.
    /// </summary>
    /// <param name="options">Options for configuring the POST request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Post(RequestOptions options, CancellationToken cancellationToken = default);

    ValueTask<HttpResponseMessage> Post(string uri, object? obj, bool logResponse = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a GET request to the specified URI.
    /// </summary>
    /// <param name="options">Options for configuring the GET request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Get(RequestOptions options, CancellationToken cancellationToken = default);

    ValueTask<HttpResponseMessage> Get(string uri, bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a PUT request to the specified URI with an object payload.
    /// </summary>
    /// <param name="options">Options for configuring the PUT request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Put(RequestOptions options, CancellationToken cancellationToken = default);

    ValueTask<HttpResponseMessage> Put(string uri, object obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a DELETE request to the specified URI.
    /// </summary>
    /// <param name="options">Options for configuring the DELETE request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Delete(RequestOptions options, CancellationToken cancellationToken = default);

    ValueTask<HttpResponseMessage> Delete(string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads a file stream to the specified URI, with an optional object payload.
    /// </summary>
    /// <param name="options">Options for configuring the file upload request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, which contains the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Upload(RequestUploadOptions options, CancellationToken cancellationToken = default);
}