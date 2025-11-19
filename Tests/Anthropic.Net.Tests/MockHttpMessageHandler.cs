namespace Anthropic.Net.Test;

using System.Net;
using System.Threading.Tasks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
    {
        _response = response;
        _statusCode = statusCode;
    }

    public string? Input { get; private set; }

    public int NumberOfCalls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        NumberOfCalls++;

        if (request.Content != null)
        {
            Input = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
        }

        return new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response),
        };
    }
}
