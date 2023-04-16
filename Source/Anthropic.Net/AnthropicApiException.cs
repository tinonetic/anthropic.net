namespace Anthropic.Net;

/// <summary>
/// The exception originating from the API itself, with the appropriate message.
/// </summary>
internal sealed class AnthropicApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiException"/> class.
    /// </summary>
    /// <param name="message">The relevant message.</param>
    public AnthropicApiException(string message)
        : base(message)
    {
    }
}
