using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.Net;

// Main container for all stream events
public class MessagesStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MessageStartDetails? Message { get; set; } // For message_start

    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; set; } // For content_block_start, content_block_delta

    [JsonPropertyName("content_block")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentBlock? ContentBlock { get; set; } // For content_block_start

    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeltaDetails? Delta { get; set; } // For content_block_delta, message_delta

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UsageInfo? Usage { get; set; } // For message_delta (when stop_reason is present)

    // Error event is simpler
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamError? Error { get; set; } // For error type

    // No specific data for ping or message_stop (message_stop is indicated by Type)
}

public class MessageStartDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } // "message"

    [JsonPropertyName("role")]
    public string Role { get; set; }

    // Content will be built from content_block_* events
    // Model is part of the top-level request/response not usually in this part of stream
    [JsonPropertyName("model")]
    public string Model { get; set; } // Added based on typical message_start structure

    [JsonPropertyName("usage")] // Often included in message_start
    public UsageInfo Usage { get; set; }
}

public class DeltaDetails
{
    // For content_block_delta
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentBlockDeltaType { get; set; } // "text_delta", "input_json_delta" (for tool use)

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("partial_json")] // For tool_use input json streaming
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartialJson { get; set; }


    // For message_delta
    [JsonPropertyName("stop_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopSequence { get; set; }

    // Usage is typically at the top level of MessagesStreamEvent when part of message_delta
}

public class StreamError
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // e.g. "error"

    [JsonPropertyName("error")]
    public ApiError ErrorDetails { get; set; }
}

public class ApiError
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // e.g. "overloaded_error"
    [JsonPropertyName("message")]
    public string Message { get; set; }
}

// Constants for event types (optional, but good for comparison)
public static class MessageStreamEventTypes
{
    public const string MessageStart = "message_start";
    public const string ContentBlockStart = "content_block_start";
    public const string ContentBlockDelta = "content_block_delta";
    public const string ContentBlockStop = "content_block_stop";
    public const string MessageDelta = "message_delta";
    public const string MessageStop = "message_stop";
    public const string Ping = "ping";
    public const string Error = "error";
}
