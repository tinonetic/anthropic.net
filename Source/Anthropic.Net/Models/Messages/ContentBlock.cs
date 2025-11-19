namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

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
