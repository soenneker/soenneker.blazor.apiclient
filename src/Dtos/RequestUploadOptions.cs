using System.IO;

namespace Soenneker.Blazor.ApiClient.Dtos;

/// <summary>
/// Represents the options for uploading a file via an API request, extending <see cref="RequestOptions"/> with additional file upload properties.
/// </summary>
public record RequestUploadOptions : RequestOptions
{
    /// <summary>
    /// Gets or sets the stream representing the file to be uploaded.
    /// </summary>
    public Stream Stream { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the file being uploaded.
    /// </summary>
    public string FileName { get; set; } = null!;
}