namespace Anthropic.Net;

using System.Threading.Tasks;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;

/// <summary>
/// The Anthropic API client interface.
/// </summary>
public interface IAnthropicApiClient
{
    /// <summary>
    /// Sends a prompt to the Anthropic API for completion.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="CompletionResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    Task<CompletionResponse> CompletionAsync(CompletionRequest request);

    /// <summary>
    /// Sends a message to the Anthropic API using the Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessageRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="MessageResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    Task<MessageResponse> MessageAsync(MessageRequest request);

    /// <summary>
    /// Sends a message to the Anthropic API using the Messages API and streams the response.
    /// </summary>
    /// <param name="request">The <see cref="MessageRequest"/> object representing the request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of <see cref="MessageStreamEvent"/> objects.</returns>
    IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(MessageRequest request, CancellationToken cancellationToken = default);
}
