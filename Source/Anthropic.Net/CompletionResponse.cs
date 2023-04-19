namespace Anthropic.Net;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a response from the Anthropic API for a text completion request.
/// </summary>
public class CompletionResponse
{
    /// <summary>
    /// Gets or sets the resulting completion up to and excluding the stop sequences.
    /// </summary>
    [JsonPropertyName("completion")]
    public string? Completion { get; set; }

    /// <summary>
    /// Gets or sets the reason why the text completion process stopped.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>
    /// Gets or sets the actual stop sequence that was seen, if the stop reason was "stop_sequence".
    /// </summary>
    [JsonPropertyName("stop")]
    public string? Stop { get; set; }
}
