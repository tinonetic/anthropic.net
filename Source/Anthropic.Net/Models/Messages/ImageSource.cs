namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an image source in an image content block.
/// </summary>
public class ImageSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageSource"/> class.
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

    /// <summary>
    /// Gets or sets the type of image source.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the media type of the image.
    /// </summary>
    [JsonPropertyName("media_type")]
    public string MediaType { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded image data.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }
}
