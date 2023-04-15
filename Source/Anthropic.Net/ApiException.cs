namespace Anthropic.Net;

/// <summary>
/// The generic Anthropic API exception.
/// </summary>
internal sealed class ApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="message">The relevant message.</param>
    public ApiException(string message)
        : base(message)
    {
    }
}
