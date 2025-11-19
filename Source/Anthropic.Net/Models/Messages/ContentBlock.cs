namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Base class for content blocks in messages.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContentBlock), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ImageContentBlock), typeDiscriminator: "image")]
public abstract class ContentBlock
{
    /// <summary>
    /// Gets or sets the type of content block.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Represents a text content block in a message.
/// </summary>
public class TextContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextContentBlock"/> class.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextContentBlock(string text)
    {
        Type = "text";
        Text = text;
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

/// <summary>
/// Represents an image content block in a message.
/// </summary>
public class ImageContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageContentBlock"/> class.
    /// </summary>
    /// <param name="source">The image source.</param>
    public ImageContentBlock(ImageSource source)
    {
        Type = "image";
        Source = source;
    }

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    [JsonPropertyName("source")]
    public ImageSource Source { get; set; }
}

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