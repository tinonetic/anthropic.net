namespace Anthropic.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// The Anthropic API client interface.
/// </summary>
internal interface IApiClient
{
    /// <summary>
    /// Base method for sending a prompt to Claude for completion.
    /// </summary>
    /// <param name="parameters">A dictionary of parameters to include in the request, according to the reference.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    /// <remarks>
    /// The <paramref name="parameters"/> parameter should be a relative URL that will be combined with the base API URL.
    /// For more information, see the <a href="https://console.anthropic.com/docs/api/reference">Anthropic API Reference</a>.
    /// </remarks>
    Task<JsonElement> CompletionAsync(Dictionary<string, object> parameters);
}
