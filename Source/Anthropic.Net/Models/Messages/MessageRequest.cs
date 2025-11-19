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
    /// Gets or sets the system prompt that helps set the behavior of the assistant.
    /// </summary>
    [JsonPropertyName("system")]
    public string? System { get; set; }

    /// <summary>
    /// Gets or sets the temperature. Controls the randomness of the output.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the top_p. Controls the diversity of the output.
    /// </summary>
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    /// <summary>
    /// Gets or sets the top_k. Controls the diversity of the output.
    /// </summary>
    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    /// <summary>
    /// Gets or sets the stop sequences. The model will stop generating text after it produces a stop sequence.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public IList<string>? StopSequences { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the tools available to the model.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets the tool choice.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to return JSON.
    /// </summary>
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }
}
