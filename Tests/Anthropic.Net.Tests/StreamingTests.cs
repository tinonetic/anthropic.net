namespace Anthropic.Net.Test;

using System.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using NSubstitute;
using Shouldly;
using Xunit;

public class StreamingTests
{
    private readonly Uri _baseAddress = new("https://api.anthropic.com");

    [Fact]
    public async Task TestStreamMessageAsync_ValidStream_YieldsEvents()
    {
        // Arrange
        var sseResponse = @"event: message_start
data: {""type"": ""message_start"", ""message"": {""id"": ""msg_1"", ""type"": ""message"", ""role"": ""assistant"", ""content"": [], ""model"": ""claude-3-sonnet-20240229"", ""stop_reason"": null, ""stop_sequence"": null, ""usage"": {""input_tokens"": 10, ""output_tokens"": 1}}}

event: content_block_start
data: {""type"": ""content_block_start"", ""index"": 0, ""content_block"": {""type"": ""text"", ""text"": """" }}

event: content_block_delta
data: {""type"": ""content_block_delta"", ""index"": 0, ""delta"": {""type"": ""text_delta"", ""text"": ""Hello""}}

event: content_block_delta
data: {""type"": ""content_block_delta"", ""index"": 0, ""delta"": {""type"": ""text_delta"", ""text"": "" World""}}

event: content_block_stop
data: {""type"": ""content_block_stop"", ""index"": 0}

event: message_delta
data: {""type"": ""message_delta"", ""delta"": {""stop_reason"": ""end_turn"", ""stop_sequence"": null}, ""usage"": {""output_tokens"": 5}}

event: message_stop
data: {""type"": ""message_stop""}
";

        var messageHandler = new MockHttpMessageHandler(sseResponse, HttpStatusCode.OK);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        _ = httpClientFactory.CreateClient().Returns(
            new HttpClient(messageHandler)
            {
                BaseAddress = _baseAddress,
            });

        var sut = new AnthropicApiClient("test-api-key", httpClientFactory);
        var request = new MessageRequest(AnthropicModels.Claude3Sonnet, [Message.FromUser("Hi")]);

        // Act
        var events = new List<MessageStreamEvent>();
        await foreach (var evt in sut.StreamMessageAsync(request))
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(7);
        events[0].ShouldBeOfType<MessageStartEvent>();
        events[1].ShouldBeOfType<ContentBlockStartEvent>();
        events[2].ShouldBeOfType<ContentBlockDeltaEvent>().Delta.Text.ShouldBe("Hello");
        events[3].ShouldBeOfType<ContentBlockDeltaEvent>().Delta.Text.ShouldBe(" World");
        events[4].ShouldBeOfType<ContentBlockStopEvent>();
        events[5].ShouldBeOfType<MessageDeltaEvent>();
        events[6].ShouldBeOfType<MessageStopEvent>();
    }
}
