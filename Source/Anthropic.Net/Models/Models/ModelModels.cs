namespace Anthropic.Net.Models.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>A model from the Models API.</summary>
public class ModelInfo
{
    /// <summary>Gets or sets the model id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the release time.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the context window.</summary>
    [JsonPropertyName("max_input_tokens")]
    public long? MaxInputTokens { get; set; }

    /// <summary>Gets or sets the maximum output tokens.</summary>
    [JsonPropertyName("max_tokens")]
    public long? MaxTokens { get; set; }

    /// <summary>Gets or sets the capability tree (supported flags at each leaf).</summary>
    [JsonPropertyName("capabilities")]
    public JsonElement? Capabilities { get; set; }
}

/// <summary>A page of a cursor-paginated list.</summary>
/// <typeparam name="T">Item type.</typeparam>
public class Page<T>
{
    /// <summary>Gets or sets the items.</summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether more pages exist.</summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    /// <summary>Gets or sets the first id (pass as before_id to page backwards).</summary>
    [JsonPropertyName("first_id")]
    public string? FirstId { get; set; }

    /// <summary>Gets or sets the last id (pass as after_id to page forwards).</summary>
    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }
}

/// <summary>Result of the token counting endpoint.</summary>
public class TokenCount
{
    /// <summary>Gets or sets the number of input tokens the request would use.</summary>
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
}
