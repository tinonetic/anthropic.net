namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a delta event in a streaming message, containing information about stop reason and stop sequence.
/// </summary>
public class MessageDelta
{
    /// <summary>
    /// Gets or sets the reason why the message stopped.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>
    /// Gets or sets the sequence that caused the message to stop.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}
