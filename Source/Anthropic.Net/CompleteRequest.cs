namespace Anthropic.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a request to complete text using the Anthropic API.
/// </summary>
public class CompleteRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteRequest"/> class with the specified prompt and model identifier.
    /// </summary>
    /// <param name="prompt">The prompt text to complete.</param>
    /// <param name="model">The identifier of the language model to use for text completion.</param>s
    public CompleteRequest(string prompt, string model)
    {
        Prompt = prompt;
        Model = model;
        MaxTokensToSample = 300;
        StopSequences = new[] { "\n\nHuman:" };
    }

    /// <summary>
    /// Gets or sets the prompt text to complete.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the language model to use for text completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to sample when generating the completed text.
    /// </summary>
    [JsonPropertyName("max_tokens_to_sample")]
    public int MaxTokensToSample { get; set; }

    /// <summary>
    /// Gets or sets the stop sequences to use when generating the completed text.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public IEnumerable<string> StopSequences { get; set; }
}
