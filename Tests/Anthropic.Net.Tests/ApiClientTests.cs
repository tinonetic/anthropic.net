namespace Anthropic.Net.Test;

using System.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;
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
}
