namespace Anthropic.Net.Models.Messages;

using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/// <summary>
/// Base class for content blocks in messages.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContentBlock), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ImageContentBlock), typeDiscriminator: "image")]
[JsonDerivedType(typeof(ToolUseContentBlock), typeDiscriminator: "tool_use")]
[JsonDerivedType(typeof(ToolResultContentBlock), typeDiscriminator: "tool_result")]
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

    /// <summary>
    /// Creates an <see cref="ImageContentBlock"/> from a byte array.
    /// </summary>
    /// <param name="bytes">The image data.</param>
    /// <param name="mediaType">The media type (e.g., "image/jpeg").</param>
    /// <returns>A new <see cref="ImageContentBlock"/> instance.</returns>
    public static ImageContentBlock FromBytes(byte[] bytes, string mediaType)
    {
        var base64Data = Convert.ToBase64String(bytes);
        return new ImageContentBlock(new ImageSource("base64", mediaType, base64Data));
    }

    /// <summary>
    /// Creates an <see cref="ImageContentBlock"/> from a file path.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="mediaType">The media type (e.g., "image/jpeg"). If null, it will be inferred from the extension.</param>
    /// <returns>A new <see cref="ImageContentBlock"/> instance.</returns>
    public static async Task<ImageContentBlock> FromFileAsync(string filePath, string? mediaType = null)
    {
        var bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            mediaType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        return FromBytes(bytes, mediaType);
    }
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

/// <summary>
/// Represents a tool use content block in a message.
/// </summary>
public class ToolUseContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolUseContentBlock"/> class.
    /// </summary>
    public ToolUseContentBlock()
    {
        Type = "tool_use";
    }

    /// <summary>
    /// Gets or sets the ID of the tool use.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input to the tool.
    /// </summary>
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
}

/// <summary>
/// Represents a tool result content block in a message.
/// </summary>
public class ToolResultContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolResultContentBlock"/> class.
    /// </summary>
    /// <param name="toolUseId">The ID of the tool use this result corresponds to.</param>
    /// <param name="content">The content of the result.</param>
    public ToolResultContentBlock(string toolUseId, string content)
    {
        Type = "tool_result";
        ToolUseId = toolUseId;
        Content = content;
    }

    /// <summary>
    /// Gets or sets the ID of the tool use this result corresponds to.
    /// </summary>
    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; }

    /// <summary>
    /// Gets or sets the content of the result.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result is an error.
    /// </summary>
    [JsonPropertyName("is_error")]
    public bool? IsError { get; set; }
}