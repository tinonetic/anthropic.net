namespace Anthropic.Net.Test;

using System.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

public class ToolsTests
{
    private readonly Uri _baseAddress = new("https://api.anthropic.com");

    [Fact]
    public async Task TestMessageAsync_WithTools_ReturnsToolUse()
    {
        // Arrange
        var mockResponse = /*lang=json,strict*/ @"{
            ""id"": ""msg_tool"",
            ""type"": ""message"",
            ""role"": ""assistant"",
            ""content"": [
                {
                    ""type"": ""text"",
                    ""text"": ""I will check the weather.""
                },
                {
                    ""type"": ""tool_use"",
                    ""id"": ""tool_1"",
                    ""name"": ""get_weather"",
                    ""input"": { ""location"": ""San Francisco"" }
                }
            ],
            ""model"": ""claude-3-sonnet-20240229"",
            ""stop_reason"": ""tool_use"",
            ""stop_sequence"": null,
            ""usage"": { ""input_tokens"": 20, ""output_tokens"": 15 }
        }";

        var messageHandler = new MockHttpMessageHandler(mockResponse, HttpStatusCode.OK);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        _ = httpClientFactory.CreateClient().Returns(
            new HttpClient(messageHandler)
            {
                BaseAddress = _baseAddress,
            });

        var sut = new AnthropicApiClient("test-api-key", httpClientFactory);
        var tool = new Tool("get_weather", "Get weather", new { type = "object", properties = new { location = new { type = "string" } } });
        var request = new MessageRequest(AnthropicModels.Claude3Sonnet, [Message.FromUser("Weather in SF?")])
        {
            Tools = [tool]
        };

        // Act
        var response = await sut.MessageAsync(request);

        // Assert
        response.ShouldNotBeNull();
        response.StopReason.ShouldBe("tool_use");
        response.Content.Count.ShouldBe(2);
        response.Content[1].ShouldBeOfType<ToolUseContentBlock>();
        var toolUse = (ToolUseContentBlock)response.Content[1];
        toolUse.Name.ShouldBe("get_weather");
        toolUse.Id.ShouldBe("tool_1");
    }
}
