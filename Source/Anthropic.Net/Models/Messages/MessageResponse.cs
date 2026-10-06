namespace Anthropic.Net.Models.Messages;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a response from the Anthropic Messages API.
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// Gets or sets the ID of the message.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the response.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = [];

    /// <summary>
    /// Gets or sets the model used to generate the response.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stop reason: end_turn, max_tokens, stop_sequence, tool_use, pause_turn, refusal, model_context_window_exceeded.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>
    /// Gets or sets the stop sequence.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    /// <summary>
    /// Gets or sets details on a refusal (only when <see cref="StopReason"/> is "refusal"): type, category, explanation.
    /// </summary>
    [JsonPropertyName("stop_details")]
    public StopDetails? StopDetails { get; set; }

    /// <summary>
    /// Gets or sets the code execution container (id, expires_at), if one was used.
    /// </summary>
    [JsonPropertyName("container")]
    public JsonElement? Container { get; set; }

    /// <summary>
    /// Gets or sets the usage information.
    /// </summary>
    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = new Usage();

    /// <summary>
    /// Gets the concatenated text content of the message.
    /// </summary>
    [JsonIgnore]
    public string Text => string.Concat(Content.OfType<TextContentBlock>().Select(b => b.Text));

    /// <summary>
    /// Gets the tool_use blocks the model wants executed.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<ToolUseContentBlock> ToolUses => Content.OfType<ToolUseContentBlock>();

    /// <summary>
    /// Converts this response into an assistant <see cref="Message"/> that can be appended to the conversation
    /// (keeps thinking, tool_use, server tool and compaction blocks intact, as the API requires).
    /// </summary>
    /// <returns>An assistant message.</returns>
    public Message ToAssistantMessage() => new("assistant", Content);
}

/// <summary>
/// Details about why the model refused (stop_reason "refusal").
/// </summary>
public class StopDetails
{
    /// <summary>
    /// Gets or sets the type; "refusal".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the category (open set: cyber, bio, reasoning_extraction, frontier_llm, general_harms, ...).
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets a human-readable explanation.
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}
