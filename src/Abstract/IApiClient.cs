using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.Blazor.ApiClient.Abstract;

/// <summary>
/// A lightweight and efficient API client wrapper for Blazor applications, simplifying HTTP communication with support for asynchronous calls, cancellation tokens, and JSON serialization.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Initializes the API client with a base URI and an optional flag for logging request and response details.
    /// </summary>
    /// <param name="baseUri">The base URI for the API.</param>
    /// <param name="requestResponseLogging">Indicates whether request and response logging should be enabled.</param>
    void Initialize(string baseUri, bool requestResponseLogging);

    /// <summary>
    /// Asynchronously retrieves the current access token for the API client.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing the access token as a string.</returns>
    ValueTask<string> GetAccessToken();

    /// <summary>
    /// Asynchronously retrieves a configured <see cref="HttpClient"/> instance with an access token already attached.
    /// Utilizes double-check locking to retrieve the access token, ensuring it is cached appropriately. <para/>
    /// The returned <see cref="HttpClient"/> exists for the lifetime of the user's scope.
    /// </summary>
    /// <param name="allowAnonymous">Optional flag to allow anonymous requests if set to true. Defaults to false.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing the configured <see cref="HttpClient"/>.</returns>
    ValueTask<HttpClient> GetClient(bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a POST request to the specified URI with an optional object payload.
    /// </summary>
    /// <param name="requestUri">The URI to send the POST request to.</param>
    /// <param name="obj">The optional object to be serialized and sent as the request body.</param>
    /// <param name="logResponse">Indicates whether the response should be logged. Defaults to true.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Post(string requestUri, object? obj, bool logResponse = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a GET request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the GET request to.</param>
    /// <param name="allowAnonymous">Optional flag to allow anonymous requests. Defaults to false.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Get(string requestUri, bool? allowAnonymous = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a PUT request to the specified URI with an object payload.
    /// </summary>
    /// <param name="requestUri">The URI to send the PUT request to.</param>
    /// <param name="obj">The object to be serialized and sent as the request body.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Put(string requestUri, object obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a DELETE request to the specified URI.
    /// </summary>
    /// <param name="requestUri">The URI to send the DELETE request to.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Delete(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads a file as a stream to the specified URI, with an optional object payload.
    /// </summary>
    /// <param name="requestUri">The URI to upload the file to.</param>
    /// <param name="stream">The file stream to be uploaded.</param>
    /// <param name="fileName">The name of the file being uploaded.</param>
    /// <param name="obj">An optional object to be serialized and sent as part of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, containing the <see cref="HttpResponseMessage"/>.</returns>
    ValueTask<HttpResponseMessage> Upload(string requestUri, Stream stream, string fileName, object? obj = null, CancellationToken cancellationToken = default);
}