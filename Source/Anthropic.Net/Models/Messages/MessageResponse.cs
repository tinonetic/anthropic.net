namespace Anthropic.Net.Models.Messages;

using System.Collections.Generic;
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
    /// Gets or sets the stop reason.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>
    /// Gets or sets the stop sequence.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    /// <summary>
    /// Gets or sets the usage information.
    /// </summary>
    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = new Usage();

    /// <summary>
    /// Gets the text content of the message.
    /// </summary>
    [JsonIgnore]
    public string Text
    {
        get
        {
            var textBlocks = Content.FindAll(c => c.Type == "text");
            if (textBlocks.Count > 0)
            {
                return string.Join(string.Empty, textBlocks.ConvertAll(b => ((TextContentBlock)b).Text));
            }

            return string.Empty;
        }
    }
}
