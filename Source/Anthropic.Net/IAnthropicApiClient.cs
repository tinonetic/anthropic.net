namespace Anthropic.Net;

using System.Collections.Generic; // For IAsyncEnumerable
using System.Threading; // For CancellationToken
using System.Threading.Tasks;

/// <summary>
/// The Anthropic API client interface.
/// </summary>
internal interface IAnthropicApiClient
{
    /// <summary>
    /// Sends a prompt to the Anthropic API for completion.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="CompletionResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    [Obsolete("This method uses the legacy Completions API. Prefer using CreateMessageAsync or StreamMessageAsync with the Messages API.")]
    Task<CompletionResponse> CompletionAsync(CompletionRequest request);

    /// <summary>
    /// Creates a message using the Anthropic Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessagesRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="MessagesResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    Task<MessagesResponse> CreateMessageAsync(MessagesRequest request);

    /// <summary>
    /// Streams a message using the Anthropic Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessagesRequest"/> object representing the request parameters. Ensure Stream property is set to true.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the streaming operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="MessagesStreamEvent"/> representing the stream of events from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails.</exception>
    IAsyncEnumerable<MessagesStreamEvent> StreamMessageAsync(
        MessagesRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default);
}
