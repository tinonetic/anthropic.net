namespace Anthropic.Net.Models.Messages;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a request to the Anthropic Messages API.
/// </summary>
public class MessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRequest"/> class.
    /// </summary>
    /// <param name="model">The model to use for generating a response.</param>
    /// <param name="messages">The conversation messages to generate a response for.</param>
    /// <param name="maxTokens">The maximum number of tokens to generate before stopping.</param>
    public MessageRequest(string model, IList<Message> messages, int maxTokens = 1024)
    {
        Model = model;
        Messages = messages;
        MaxTokens = maxTokens;
    }

    /// <summary>
    /// Gets or sets the model to use for generating a response.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the conversation messages to generate a response for.
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<Message> Messages { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate before stopping.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the system prompt: a string, or a list of <see cref="TextContentBlock"/> (use blocks to set cache_control).
    /// </summary>
    [JsonPropertyName("system")]
    public object? System { get; set; }

    /// <summary>
    /// Gets or sets the temperature (rejected by Fable 5.x / Opus 5.5 / 5 / 4.8 / 4.7 / Sonnet 5.x).
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the top_p (rejected by the newest models).
    /// </summary>
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    /// <summary>
    /// Gets or sets the top_k (rejected by the newest models).
    /// </summary>
    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    /// <summary>
    /// Gets or sets the stop sequences.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public IList<string>? StopSequences { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the tools available to the model (user-defined and server tools).
    /// </summary>
    [JsonPropertyName("tools")]
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets the tool choice: a <see cref="ToolChoice"/> or any serializable object.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    /// <summary>
    /// Gets or sets the legacy response format. Not part of the Messages API any more; ignored on the wire.
    /// Use <see cref="OutputConfig"/> with <see cref="OutputFormat.JsonSchema"/> for structured output.
    /// </summary>
    [Obsolete("Not an API parameter. Use OutputConfig.Format (structured outputs) instead.")]
    [JsonIgnore]
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the thinking configuration.
    /// </summary>
    [JsonPropertyName("thinking")]
    public ThinkingConfig? Thinking { get; set; }

    /// <summary>
    /// Gets or sets the output configuration (effort, structured outputs, task budget).
    /// </summary>
    [JsonPropertyName("output_config")]
    public OutputConfig? OutputConfig { get; set; }

    /// <summary>
    /// Gets or sets request metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public RequestMetadata? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the service tier: "auto" or "standard_only".
    /// </summary>
    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }

    /// <summary>
    /// Gets or sets where inference runs, e.g. "us" or "global".
    /// </summary>
    [JsonPropertyName("inference_geo")]
    public string? InferenceGeo { get; set; }

    /// <summary>
    /// Gets or sets the speed: "fast" (Opus fast mode; needs beta fast-mode-2026-02-01).
    /// </summary>
    [JsonPropertyName("speed")]
    public string? Speed { get; set; }

    /// <summary>
    /// Gets or sets top-level automatic prompt caching.
    /// </summary>
    [JsonPropertyName("cache_control")]
    public CacheControl? CacheControl { get; set; }

    /// <summary>
    /// Gets or sets the container id or object (code execution / skills).
    /// </summary>
    [JsonPropertyName("container")]
    public object? Container { get; set; }

    /// <summary>
    /// Gets or sets MCP connector servers (beta mcp-client-2025-11-20).
    /// </summary>
    [JsonPropertyName("mcp_servers")]
    public List<McpServer>? McpServers { get; set; }

    /// <summary>
    /// Gets or sets server-side context management (beta context-management-2025-06-27), e.g. { edits = [ { type = "clear_tool_uses_20250919" } ] }.
    /// </summary>
    [JsonPropertyName("context_management")]
    public object? ContextManagement { get; set; }

    /// <summary>
    /// Gets or sets server-side fallbacks (beta server-side-fallback-2026-07-01): "default", or a list like [ { model = "..." } ] (beta server-side-fallback-2026-06-01).
    /// </summary>
    [JsonPropertyName("fallbacks")]
    public object? Fallbacks { get; set; }

    /// <summary>
    /// Gets or sets beta feature ids sent in the <c>anthropic-beta</c> header (not part of the body).
    /// </summary>
    [JsonIgnore]
    public IList<string>? Betas { get; set; }
}
