namespace Anthropic.Net.Models.Messages;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents usage information in a message response.
/// </summary>
public class Usage
{
    /// <summary>
    /// Gets or sets the number of input tokens.
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens.
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens written to the prompt cache.
    /// </summary>
    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens read from the prompt cache.
    /// </summary>
    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the service tier used (standard, priority, batch).
    /// </summary>
    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }

    /// <summary>
    /// Gets or sets where inference ran.
    /// </summary>
    [JsonPropertyName("inference_geo")]
    public string? InferenceGeo { get; set; }

    /// <summary>
    /// Gets or sets the speed used ("standard" or "fast").
    /// </summary>
    [JsonPropertyName("speed")]
    public string? Speed { get; set; }

    /// <summary>
    /// Gets or sets server tool usage counters (e.g. web_search_requests).
    /// </summary>
    [JsonPropertyName("server_tool_use")]
    public JsonElement? ServerToolUse { get; set; }

    /// <summary>
    /// Gets or sets any other usage fields (cache_creation breakdown, iterations, ...).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
