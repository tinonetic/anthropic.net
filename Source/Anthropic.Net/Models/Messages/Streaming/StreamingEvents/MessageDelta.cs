namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;

/// <summary>
/// The delta of a message_delta event.
/// </summary>
public class MessageDelta
{
    /// <summary>Gets or sets the stop reason.</summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>Gets or sets the stop sequence.</summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    /// <summary>Gets or sets refusal details.</summary>
    [JsonPropertyName("stop_details")]
    public StopDetails? StopDetails { get; set; }
}
