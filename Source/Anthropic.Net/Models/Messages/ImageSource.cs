namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an image source in an image content block (base64, url or Files API file).
/// </summary>
public class ImageSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageSource"/> class (base64).
    /// </summary>
    /// <param name="type">The type of image source.</param>
    /// <param name="mediaType">The media type of the image.</param>
    /// <param name="data">The base64-encoded image data.</param>
    public ImageSource(string type, string mediaType, string data)
    {
        Type = type;
        MediaType = mediaType;
        Data = data;
    }

    /// <summary>Initializes a new instance of the <see cref="ImageSource"/> class for deserialization.</summary>
    [JsonConstructor]
    public ImageSource()
    {
        Type = string.Empty;
    }

    /// <summary>
    /// Gets or sets the type of image source: base64, url or file.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the media type of the image.
    /// </summary>
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded image data.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>Gets or sets the image URL (type "url").</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the Files API id (type "file").</summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    /// <summary>Creates a URL image source.</summary>
    /// <param name="url">The image URL.</param>
    /// <returns>The source.</returns>
    public static ImageSource FromUrl(string url) => new() { Type = "url", Url = url };

    /// <summary>Creates a Files API image source.</summary>
    /// <param name="fileId">The file id.</param>
    /// <returns>The source.</returns>
    public static ImageSource FromFileId(string fileId) => new() { Type = "file", FileId = fileId };
}
