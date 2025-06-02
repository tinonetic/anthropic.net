using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic.Net.Constants; // For AnthropicVersion if needed, or model names
using NSubstitute;
using Shouldly;
using Xunit;

namespace Anthropic.Net.Test;

public class MessagesApiTests
{
    private const string TestApiKey = "test-api-key";
    private readonly string _apiBaseUrl = "https://api.anthropic.com/v2/messages"; // More specific for messages
    private readonly string _anthropicVersion = "2023-06-01";


    [Fact]
    public async Task CreateMessageAsync_SuccessfulResponse_ReturnsParsedResponse()
    {
        // Arrange
        var request = new MessagesRequest
        {
            Model = "claude-3-opus-20240229",
            Messages = new List<Message> { new Message { Role = "user", Content = "Hello, Claude!" } },
            MaxTokens = 100
        };
        var expectedResponsePayload = new MessagesResponse
        {
            Id = "msg_123",
            Type = "message",
            Role = "assistant",
            Content = new List<ContentBlock> { new ContentBlock { Type = "text", Text = "Hello! How can I help you today?" } },
            Model = "claude-3-opus-20240229",
            StopReason = "end_turn",
            Usage = new UsageInfo { InputTokens = 10, OutputTokens = 20 }
        };
        var serializedResponse = JsonSerializer.Serialize(expectedResponsePayload);

        var mockHandler = new MockHttpMessageHandler(serializedResponse, HttpStatusCode.OK);
        var apiClient = new AnthropicApiClient(TestApiKey, mockHandler, "https://api.anthropic.com"); // Base for client

        // Act
        var response = await apiClient.CreateMessageAsync(request);

        // Assert
        response.ShouldNotBeNull();
        response.Id.ShouldBe(expectedResponsePayload.Id);
        response.Role.ShouldBe("assistant");
        response.Content.ShouldHaveSingleItem();
        response.Content[0].Type.ShouldBe("text");
        response.Content[0].Text.ShouldBe(expectedResponsePayload.Content[0].Text);
        response.Usage.InputTokens.ShouldBe(expectedResponsePayload.Usage.InputTokens);

        // Verify request details
        mockHandler.NumberOfCalls.ShouldBe(1);
        mockHandler.Input.ShouldNotBeNull();
        var sentRequest = JsonSerializer.Deserialize<MessagesRequest>(mockHandler.Input);
        sentRequest.Model.ShouldBe(request.Model);
        sentRequest.Messages[0].Content.ShouldBe(request.Messages[0].Content); // Simple string content comparison
        // Further request header checks can be added to MockHttpMessageHandler if needed
    }

    [Fact]
    public async Task CreateMessageAsync_ApiError_ThrowsAnthropicApiException()
    {
        // Arrange
        var request = new MessagesRequest
        {
            Model = "claude-3-opus-20240229",
            Messages = new List<Message> { new Message { Role = "user", Content = "Hello, Claude!" } },
            MaxTokens = 100
        };
        var errorPayload = new { type = "error", error = new { type = "invalid_request_error", message = "Bad request" } };
        var serializedErrorResponse = JsonSerializer.Serialize(errorPayload);

        var mockHandler = new MockHttpMessageHandler(serializedErrorResponse, HttpStatusCode.BadRequest);
        var apiClient = new AnthropicApiClient(TestApiKey, mockHandler, "https://api.anthropic.com");

        // Act & Assert
        var exception = await Should.ThrowAsync<AnthropicApiException>(async () => await apiClient.CreateMessageAsync(request));
        exception.Message.ShouldContain("Request failed with status code BadRequest");
        exception.Message.ShouldContain("Bad request");
    }

    [Fact]
    public async Task StreamMessageAsync_SuccessfulStream_YieldsCorrectEvents()
    {
        // Arrange
        var request = new MessagesRequest
        {
            Model = "claude-3-opus-20240229",
            Messages = new List<Message> { new Message { Role = "user", Content = "Hello stream!" } },
            MaxTokens = 100,
            Stream = true
        };

        var sseStreamBuilder = new StringBuilder();
        // Event 1: message_start
        sseStreamBuilder.AppendLine("event: message_start");
        var messageStartData = new { message = new { id = "msg_stream_123", type = "message", role = "assistant", model = "claude-3-opus-20240229", usage = new { input_tokens = 10, output_tokens = 0 } } };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(messageStartData)}");
        sseStreamBuilder.AppendLine();

        // Event 2: content_block_start
        sseStreamBuilder.AppendLine("event: content_block_start");
        var contentBlockStartData = new { index = 0, content_block = new { type = "text", text = "" } };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(contentBlockStartData)}");
        sseStreamBuilder.AppendLine();

        // Event 3: content_block_delta (text)
        sseStreamBuilder.AppendLine("event: content_block_delta");
        var contentBlockDeltaData1 = new { index = 0, delta = new { type = "text_delta", text = "Hello" } };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(contentBlockDeltaData1)}");
        sseStreamBuilder.AppendLine();

        // Event 4: content_block_delta (text)
        sseStreamBuilder.AppendLine("event: content_block_delta");
        var contentBlockDeltaData2 = new { index = 0, delta = new { type = "text_delta", text = " World!" } };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(contentBlockDeltaData2)}");
        sseStreamBuilder.AppendLine();
        
        // Event 5: content_block_stop
        sseStreamBuilder.AppendLine("event: content_block_stop");
        var contentBlockStopData = new { index = 0 };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(contentBlockStopData)}");
        sseStreamBuilder.AppendLine();

        // Event 6: message_delta
        sseStreamBuilder.AppendLine("event: message_delta");
        var messageDeltaData = new { delta = new { stop_reason = "end_turn", stop_sequence = (string)null }, usage = new { output_tokens = 25 } };
        sseStreamBuilder.AppendLine($"data: {JsonSerializer.Serialize(messageDeltaData)}");
        sseStreamBuilder.AppendLine();

        // Event 7: message_stop
        sseStreamBuilder.AppendLine("event: message_stop");
        sseStreamBuilder.AppendLine("data: {}"); // Empty data for message_stop often, or specific SDK model for it
        sseStreamBuilder.AppendLine();

        var mockHandler = new MockHttpMessageHandler(sseStreamBuilder.ToString(), HttpStatusCode.OK, "text/event-stream");
        var apiClient = new AnthropicApiClient(TestApiKey, mockHandler, "https://api.anthropic.com");

        var events = new List<MessagesStreamEvent>();

        // Act
        await foreach (var evt in apiClient.StreamMessageAsync(request, CancellationToken.None))
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(7);

        events[0].Type.ShouldBe(MessageStreamEventTypes.MessageStart);
        events[0].Message.ShouldNotBeNull();
        events[0].Message.Id.ShouldBe("msg_stream_123");
        events[0].Message.Usage.InputTokens.ShouldBe(10);

        events[1].Type.ShouldBe(MessageStreamEventTypes.ContentBlockStart);
        events[1].Index.ShouldBe(0);
        events[1].ContentBlock.ShouldNotBeNull();
        events[1].ContentBlock.Type.ShouldBe("text");

        events[2].Type.ShouldBe(MessageStreamEventTypes.ContentBlockDelta);
        events[2].Index.ShouldBe(0);
        events[2].Delta.ShouldNotBeNull();
        events[2].Delta.Text.ShouldBe("Hello");

        events[3].Type.ShouldBe(MessageStreamEventTypes.ContentBlockDelta);
        events[3].Index.ShouldBe(0);
        events[3].Delta.ShouldNotBeNull();
        events[3].Delta.Text.ShouldBe(" World!");
        
        events[4].Type.ShouldBe(MessageStreamEventTypes.ContentBlockStop);
        events[4].Index.ShouldBe(0);

        events[5].Type.ShouldBe(MessageStreamEventTypes.MessageDelta);
        events[5].Delta.ShouldNotBeNull();
        events[5].Delta.StopReason.ShouldBe("end_turn");
        events[5].Usage.ShouldNotBeNull();
        events[5].Usage.OutputTokens.ShouldBe(25);
        
        events[6].Type.ShouldBe(MessageStreamEventTypes.MessageStop);
    }

    [Fact]
    public async Task StreamMessageAsync_ApiErrorBeforeStream_ThrowsAnthropicApiException()
    {
        // Arrange
        var request = new MessagesRequest
        {
            Model = "claude-3-opus-20240229",
            Messages = new List<Message> { new Message { Role = "user", Content = "Hello stream!" } },
            MaxTokens = 100,
            Stream = true
        };
        var errorPayload = new { type = "error", error = new { type = "authentication_error", message = "Invalid API Key" } };
        var serializedErrorResponse = JsonSerializer.Serialize(errorPayload);

        var mockHandler = new MockHttpMessageHandler(serializedErrorResponse, HttpStatusCode.Unauthorized);
        var apiClient = new AnthropicApiClient(TestApiKey, mockHandler, "https://api.anthropic.com");

        // Act & Assert
        var exception = await Should.ThrowAsync<AnthropicApiException>(async () =>
        {
            await foreach (var _ in apiClient.StreamMessageAsync(request, CancellationToken.None))
            {
                // Should not reach here
            }
        });
        exception.Message.ShouldContain("Request failed with status code Unauthorized");
        exception.Message.ShouldContain("Invalid API Key");
    }
}
