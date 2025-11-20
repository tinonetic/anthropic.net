namespace Anthropic.Net.Test;

using System.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using NSubstitute;
using Shouldly;
using Xunit;

public class ApiClientTests
{
    private readonly Uri _baseAddress;

    public ApiClientTests()
    {
        _baseAddress = new Uri("https://api.anthropic.com");
    }

    [Fact]
    public async Task TestPromptValidator_ValidPromptsAsync()
    {
        var mockJsonResponse = "{}";
        var messageHandler = new MockHttpMessageHandler(mockJsonResponse, HttpStatusCode.OK);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        _ = httpClientFactory.CreateClient().Returns(
            new HttpClient(messageHandler)
            {
                BaseAddress = _baseAddress,
            });

        var sut = new AnthropicApiClient("test-api-key", httpClientFactory);
        var userQuestion = "How do I make you print 'Hello World?'?";
        var response = await sut.CompletionAsync(new CompletionRequest(userQuestion, AnthropicModels.Claude_v1))
            .ConfigureAwait(true);

        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task TestMessageAsync_ValidRequestAsync()
    {
        // Arrange
        var mockResponse = /*lang=json,strict*/ @"{
            ""id"": ""msg_123"",
            ""type"": ""message"",
            ""role"": ""assistant"",
            ""content"": [
                {
                    ""type"": ""text"",
                    ""text"": ""Hello, world!""
                }
            ],
            ""model"": ""claude-3-sonnet-20240229"",
            ""stop_reason"": ""end_turn"",
            ""stop_sequence"": null,
            ""usage"": {
                ""input_tokens"": 10,
                ""output_tokens"": 5
            }
        }";

        var messageHandler = new MockHttpMessageHandler(mockResponse, HttpStatusCode.OK);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        _ = httpClientFactory.CreateClient().Returns(
            new HttpClient(messageHandler)
            {
                BaseAddress = _baseAddress,
            });

        var sut = new AnthropicApiClient("test-api-key", httpClientFactory);

        // Act
        var messages = new List<Message> { Message.FromUser("Hello, Claude!") };
        var response = await sut.MessageAsync(new MessageRequest(AnthropicModels.Claude3Sonnet, messages))
            .ConfigureAwait(true);

        // Assert
        response.ShouldNotBeNull();
        response.Id.ShouldBe("msg_123");
        response.Role.ShouldBe("assistant");
        response.Content.Count.ShouldBe(1);
        response.Content[0].Type.ShouldBe("text");
        ((TextContentBlock)response.Content[0]).Text.ShouldBe("Hello, world!");
    }
    [Fact]
    public async Task TestStreamMessageAsync_ValidRequestAsync()
    {
        // Arrange
        var mockSseResponse = """
            event: message_start
            data: {"type": "message_start", "message": {"id": "msg_1", "type": "message", "role": "assistant", "content": [], "model": "claude-3-sonnet-20240229", "stop_reason": null, "stop_sequence": null, "usage": {"input_tokens": 25, "output_tokens": 1}}}

            event: content_block_start
            data: {"type": "content_block_start", "index": 0, "content_block": {"type": "text", "text": ""}}

            event: content_block_delta
            data: {"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "Hello"}}

            event: content_block_stop
            data: {"type": "content_block_stop", "index": 0}

            event: message_delta
            data: {"type": "message_delta", "delta": {"stop_reason": "end_turn", "stop_sequence": null}, "usage": {"output_tokens": 15}}

            event: message_stop
            data: {"type": "message_stop"}
            """;

        var messageHandler = new MockHttpMessageHandler(mockSseResponse, HttpStatusCode.OK);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        _ = httpClientFactory.CreateClient().Returns(
            new HttpClient(messageHandler)
            {
                BaseAddress = _baseAddress,
            });

        var sut = new AnthropicApiClient("test-api-key", httpClientFactory);

        // Act
        var messages = new List<Message> { Message.FromUser("Hello") };
        var events = new List<MessageStreamEvent>();
        await foreach (var ev in sut.StreamMessageAsync(new MessageRequest(AnthropicModels.Claude3Sonnet, messages)))
        {
            events.Add(ev);
        }

        // Assert
        events.ShouldNotBeEmpty();
        events.Count.ShouldBe(6);
        events[0].ShouldBeOfType<MessageStartEvent>();
        events[2].ShouldBeOfType<ContentBlockDeltaEvent>();
        ((ContentBlockDeltaEvent)events[2]).Delta.Text.ShouldBe("Hello");
    }
}
