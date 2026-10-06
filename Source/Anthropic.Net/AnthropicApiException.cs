namespace Anthropic.Net;

/// <summary>
/// Represents an exception that originates from the Anthropic API.
/// </summary>
public class AnthropicApiException : Exception
{
    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public AnthropicApiException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AnthropicApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance describing an HTTP error response.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorType">The API error type, e.g. rate_limit_error.</param>
    /// <param name="requestId">The request-id response header.</param>
    public AnthropicApiException(string message, int statusCode, string? errorType, string? requestId)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        RequestId = requestId;
    }

    /// <summary>Gets the HTTP status code, when the error came from an HTTP response.</summary>
    public int? StatusCode { get; }

    /// <summary>Gets the API error type (invalid_request_error, authentication_error, billing_error, permission_error, not_found_error, request_too_large, rate_limit_error, api_error, overloaded_error).</summary>
    public string? ErrorType { get; }

    /// <summary>Gets the request id, useful when contacting support.</summary>
    public string? RequestId { get; }

    /// <summary>Gets a value indicating whether retrying may succeed (408, 409, 429, 5xx).</summary>
    public bool IsRetryable => StatusCode is 408 or 409 or 429 or >= 500;
}
