namespace Soenneker.Blazor.ApiClient.Dtos;

/// <summary>
/// Represents the options for making an API request, including the URI, request payload, and logging settings.
/// </summary>
public record RequestOptions
{
    /// <summary>
    /// Gets or sets the URI for the API request.
    /// </summary>
    public string Uri { get; set; } = default!;

    /// <summary>
    /// Gets or sets an optional object that will be serialized and sent as the request body.
    /// </summary>
    public object? Object { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request should allow anonymous access. Defaults to false.
    /// </summary>
    public bool? AllowAnonymous { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the response should be logged. Defaults to false.
    /// </summary>
    public bool LogResponse { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the request should be logged. Defaults to false.
    /// </summary>
    public bool LogRequest { get; set; } = false;
}