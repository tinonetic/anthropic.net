namespace Anthropic.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Anthropic.Net.Constants;

/// <summary>
/// Represents a request to complete text using the Anthropic API.
/// </summary>
public class CompletionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionRequest"/> class with the specified prompt and model identifier.
    /// </summary>
    /// <param name="userQuestion">The Question or Instruction that forms part of the prompt you want Claude to complete.</param>
    /// <param name="model">The version of Claude to use for the request. See consants from <see cref="AnthropicModels"/> and API documentation.</param>
    /// <param name="maxTokensToSample">A maximum number of tokens to generate before stopping.</param>
    public CompletionRequest(string userQuestion, string model, int maxTokensToSample = 300)
    {
        Model = model;
        MaxTokensToSample = maxTokensToSample;
        StopSequences = new List<string>() { "\\n\\nHuman:" };
        Stream = false;
        Temperature = 1;
        TopK = -1;
        TopP = -1;
        UserQuestion = string.IsNullOrWhiteSpace(userQuestion) ? "Hi" : userQuestion;
    }

    /// <summary>
    /// Gets or sets the question or instruction part of the prompt.
    /// </summary>
    [JsonIgnore]
    public string UserQuestion { get; set; }

    /// <summary>
    /// Gets the prompt you want Claude to complete.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt => $"\\n\\n{AnthropicSignals.HumanSignal}{UserQuestion}{AnthropicSignals.HumanSignal}";

    /// <summary>
    /// Gets or sets the version of Claude to use for the request.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate before stopping.
    /// </summary>
    [JsonPropertyName("max_tokens_to_sample")]
    public int MaxTokensToSample { get; set; }

    /// <summary>
    /// Gets or sets a list of strings upon which to stop generating.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public List<string> StopSequences { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to incrementally stream the response using SSE.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the amount of randomness injected into the response.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of options to sample for each subsequent token.
    /// </summary>
    [JsonPropertyName("top_k")]
    public int TopK { get; set; }

    /// <summary>
    /// Gets or sets the probability threshold for nucleus sampling.
    /// </summary>
    [JsonPropertyName("top_p")]
    public float TopP { get; set; }
}
