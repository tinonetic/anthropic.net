namespace Anthropic.Net.Models.Messages;

using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
