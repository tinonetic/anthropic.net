namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the format of the response from Claude.
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseFormat"/> class.
    /// </summary>
    /// <param name="type">The type of response format.</param>
    public ResponseFormat(string type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the type of response format.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Creates a JSON response format.
    /// </summary>
    /// <returns>A new ResponseFormat configured for JSON output.</returns>
    public static ResponseFormat Json() => new("json");
}
