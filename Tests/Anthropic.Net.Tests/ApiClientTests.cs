namespace Anthropic.Net.Test;

using System.Net;
using Anthropic.Net.Constants;
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
}
