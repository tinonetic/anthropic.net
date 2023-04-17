namespace Anthropic.Net.Test;

using System.Net;
using System.Text.Json;
using Anthropic.Net.Test.Constants;
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
        var response = "{}";
        var messageHandler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = _baseAddress,
        };
        var sut = new AnthropicApiClient(httpClient, "test-api-key");

        var validRequest1 = await sut.CompletionAsync(new Dictionary<string, object>
            {
                { "max_tokens_to_sample", 1 },
                { "prompt", $"{AnthropicPrompts.HumanPrompt} Hello{AnthropicPrompts.AssistantPrompt}" },
            }).ConfigureAwait(true);

        var validRequest2 = await sut.CompletionAsync(new Dictionary<string, object>
            {
                { "max_tokens_to_sample", 1 },
                { "prompt", $"{AnthropicPrompts.HumanPrompt} Hello{AnthropicPrompts.AssistantPrompt} First answer{AnthropicPrompts.HumanPrompt} Try again{AnthropicPrompts.AssistantPrompt}" },
            }).ConfigureAwait(true);

        validRequest1.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
        validRequest2.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
    }
}
