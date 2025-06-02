using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.Net;

public class MessagesResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } // e.g., "message"

    [JsonPropertyName("role")]
    public string Role { get; set; } // e.g., "assistant"

    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("stop_reason")]
    public string StopReason { get; set; } // e.g., "end_turn", "max_tokens", "tool_use"

    [JsonPropertyName("stop_sequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopSequence { get; set; }

    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}
