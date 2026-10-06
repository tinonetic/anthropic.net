namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// A content block delta. <see cref="Type"/> is one of text_delta, input_json_delta, thinking_delta, signature_delta, citations_delta, compaction_delta.
/// </summary>
public class ContentBlockDelta
{
    /// <summary>Gets or sets the delta type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the text (text_delta).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Gets or sets a partial JSON fragment of tool input (input_json_delta).</summary>
    [JsonPropertyName("partial_json")]
    public string? PartialJson { get; set; }

    /// <summary>Gets or sets thinking text (thinking_delta).</summary>
    [JsonPropertyName("thinking")]
    public string? Thinking { get; set; }

    /// <summary>Gets or sets the thinking signature (signature_delta).</summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    /// <summary>Gets or sets a citation (citations_delta).</summary>
    [JsonPropertyName("citation")]
    public JsonElement? Citation { get; set; }

    /// <summary>Gets or sets compaction content (compaction_delta).</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
