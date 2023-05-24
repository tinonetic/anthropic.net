namespace Anthropic.Net;
using System.Threading.Tasks;

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
}
