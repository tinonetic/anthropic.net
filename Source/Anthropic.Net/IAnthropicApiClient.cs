namespace Anthropic.Net;

using System.Threading.Tasks;
using Anthropic.Net.Models.Batches;
using Anthropic.Net.Models.Files;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Models;

/// <summary>
/// The Anthropic API client interface.
/// </summary>
public interface IAnthropicApiClient
{
    /// <summary>Gets the Managed Agents API (agents, environments, sessions, deployments, vaults, memory stores).</summary>
    ManagedAgents.ManagedAgentsApi ManagedAgents { get; }

    /// <summary>Gets the Skills API.</summary>
    ManagedAgents.SkillsApi Skills { get; }

    /// <summary>Gets the Admin API (organization management; needs an admin credential).</summary>
    Admin.AdminApi Admin { get; }

    /// <summary>
    /// Sends a prompt to the legacy Text Completions API.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>The completion response.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    [Obsolete("The Text Completions API is legacy. Use MessageAsync.")]
    Task<CompletionResponse> CompletionAsync(CompletionRequest request);

    /// <summary>
    /// Sends a message to the Anthropic API using the Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessageRequest"/> object representing the request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is the <see cref="MessageResponse"/>.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    Task<MessageResponse> MessageAsync(MessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the Anthropic API using the Messages API and streams the response.
    /// </summary>
    /// <param name="request">The <see cref="MessageRequest"/> object representing the request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of <see cref="MessageStreamEvent"/> objects.</returns>
    IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(MessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Counts the input tokens a request would use (POST /v1/messages/count_tokens). Free, rate limited.</summary>
    /// <param name="request">The request (max_tokens and sampling parameters are ignored).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token count.</returns>
    Task<TokenCount> CountTokensAsync(MessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists available models (GET /v1/models) with context window and capabilities.</summary>
    /// <param name="limit">Page size (1-1000).</param>
    /// <param name="afterId">Cursor: return the page after this id.</param>
    /// <param name="beforeId">Cursor: return the page before this id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of models.</returns>
    Task<Page<ModelInfo>> ListModelsAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a model by id or alias (GET /v1/models/{id}).</summary>
    /// <param name="modelId">The model id or alias.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model.</returns>
    Task<ModelInfo> GetModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>Creates a Message Batch (50% cost, asynchronous).</summary>
    /// <param name="requests">The batch requests.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch.</returns>
    Task<MessageBatch> CreateBatchAsync(IEnumerable<BatchRequestItem> requests, CancellationToken cancellationToken = default);

    /// <summary>Gets a batch; poll until <c>ProcessingStatus == "ended"</c>.</summary>
    /// <param name="batchId">The batch id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch.</returns>
    Task<MessageBatch> GetBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>Lists batches.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of batches.</returns>
    Task<Page<MessageBatch>> ListBatchesAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default);

    /// <summary>Cancels an in-progress batch.</summary>
    /// <param name="batchId">The batch id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch.</returns>
    Task<MessageBatch> CancelBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a finished batch.</summary>
    /// <param name="batchId">The batch id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>Streams the JSONL results of an ended batch. Results are in any order - match on <c>CustomId</c>.</summary>
    /// <param name="batchId">The batch id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results.</returns>
    IAsyncEnumerable<BatchResultItem> GetBatchResultsAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>Uploads a file (POST /v1/files) for reuse via file_id in image/document blocks.</summary>
    /// <param name="content">The file content.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="contentType">The MIME type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file metadata.</returns>
    Task<FileMetadata> UploadFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Lists files.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of files.</returns>
    Task<Page<FileMetadata>> ListFilesAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets file metadata.</summary>
    /// <param name="fileId">The file id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata.</returns>
    Task<FileMetadata> GetFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>Downloads a file's content (only files created by tools such as code execution are downloadable).</summary>
    /// <param name="fileId">The file id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bytes.</returns>
    Task<byte[]> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file.</summary>
    /// <param name="fileId">The file id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);
}
