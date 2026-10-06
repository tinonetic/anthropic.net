namespace Anthropic.Net.Models.Messages;

using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Gets or sets the prompt-caching breakpoint.
    /// </summary>
    [JsonPropertyName("cache_control")]
    public CacheControl? CacheControl { get; set; }

    /// <summary>
    /// Gets or sets citations attached to this text (response only; raw because location shapes vary by source type).
    /// </summary>
    [JsonPropertyName("citations")]
    public List<JsonElement>? Citations { get; set; }
}
