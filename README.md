# Anthropic.NET

[![anthropic_net NuGet Package](https://img.shields.io/nuget/v/anthropic.net.svg)](https://www.nuget.org/packages/anthropic.net/) [![anthropic_net NuGet Package Downloads](https://img.shields.io/nuget/dt/anthropic.net)](https://www.nuget.org/packages/anthropic.net) [![GitHub Actions Status](https://github.com/tinonetic/anthropic.net/workflows/Build/badge.svg?branch=main)](https://github.com/tinonetic/anthropic.net/actions) [![CodeQL](https://github.com/marcominerva/ChatGptNet/actions/workflows/codeql.yml/badge.svg)](https://github.com/marcominerva/ChatGptNet/actions/workflows/codeql.yml)

[![GitHub Actions Build History](https://buildstats.info/github/chart/tinonetic/anthropic.net?branch=main&includeBuildsFromPullRequest=false)](https://github.com/tinonetic/anthropic.net/actions)

Anthropic.NET is a community-written .NET SDK that provides access to Anthropic's language models, including the powerful Claude family. This SDK simplifies interaction with Anthropic's APIs from your .NET applications.

## Features

*   Easy-to-use client for Anthropic APIs.
*   Supports the new **Messages API** for rich, conversational AI experiences.
*   Provides methods for both non-streaming and streaming responses.
*   Typed request and response objects.
*   Targets modern .NET versions.

## Getting Started

### Installation

Add the Anthropic.Net NuGet package to your project:

```bash
dotnet add package Anthropic.Net
```

### API Key

You'll need an API key from Anthropic. You can get one by signing up at [Anthropic's website](https://www.anthropic.com/). It's recommended to set your API key via an environment variable for security:

```bash
export ANTHROPIC_API_KEY="your-api-key-here"
```

Or, you can pass it directly when instantiating the client.

### Client Instantiation

The `AnthropicApiClient` has been updated for simpler instantiation. It no longer requires `IHttpClientFactory` to be injected for basic use, as it manages its HTTP client internally.

```csharp
using Anthropic.Net;

// Option 1: API key from environment variable (recommended)
// Ensure ANTHROPIC_API_KEY environment variable is set.
// var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
// var client = new AnthropicApiClient(apiKey);

// Option 2: API key directly in code (use with caution)
var client = new AnthropicApiClient("YOUR_ANTHROPIC_API_KEY");
```

## Using the Messages API

The SDK now focuses on the new Anthropic **Messages API** (`/v2/messages`), which is the preferred way to interact with Claude models. This API is more flexible and supports multi-turn conversations, and is designed for future capabilities like tool use.

The older Completions API (`/v1/complete` via the `CompletionAsync` method) is now marked as **obsolete**.

### Non-Streaming Message (CreateMessageAsync)

To send a single message and receive a complete response:

```csharp
using Anthropic.Net;
using Anthropic.Net.Constants; // For model constants like AnthropicModels.Claude_3_Opus_20240229
using System;
using System.Threading.Tasks;

// ... inside an async method ...
var client = new AnthropicApiClient("YOUR_ANTHROPIC_API_KEY");

var request = new MessagesRequest
{
    Model = AnthropicModels.Claude_3_Sonnet_20240229,
    Messages = new List<Message>
    {
        new Message { Role = "user", Content = "Hello, Claude! Tell me a fun fact about space." }
    },
    MaxTokens = 250,
    System = "You are a friendly and knowledgeable assistant." // Optional system prompt
};

try
{
    MessagesResponse response = await client.CreateMessageAsync(request);
    Console.WriteLine("Claude's response:");
    foreach (var contentBlock in response.Content.Where(c => c.Type == "text"))
    {
        Console.WriteLine(contentBlock.Text);
    }
    Console.WriteLine($"\nUsage: Input Tokens: {response.Usage.InputTokens}, Output Tokens: {response.Usage.OutputTokens}");
}
catch (AnthropicApiException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    // Inspect ex.StatusCode, ex.ErrorResponse, etc. for more details
}
```

### Streaming Message (StreamMessageAsync)

To receive the response as a stream of events (useful for real-time output):

```csharp
using Anthropic.Net;
using Anthropic.Net.Constants;
using System;
using System.Text;
using System.Threading.Tasks;

// ... inside an async method ...
var client = new AnthropicApiClient("YOUR_ANTHROPIC_API_KEY");

var streamRequest = new MessagesRequest
{
    Model = AnthropicModels.Claude_3_Haiku_20240307, // Haiku is great for streaming
    Messages = new List<Message>
    {
        new Message { Role = "user", Content = "Write a short story about a brave little robot." }
    },
    MaxTokens = 500,
    // Stream = true; // This is automatically handled by StreamMessageAsync
};

try
{
    Console.WriteLine("\nClaude's streaming response:");
    var completeResponse = new StringBuilder();
    await foreach (var streamEvent in client.StreamMessageAsync(streamRequest, CancellationToken.None))
    {
        switch (streamEvent.Type)
        {
            case MessageStreamEventTypes.ContentBlockDelta:
                if (streamEvent.Delta?.Text != null)
                {
                    Console.Write(streamEvent.Delta.Text);
                    completeResponse.Append(streamEvent.Delta.Text);
                }
                break;
            case MessageStreamEventTypes.MessageStop:
                Console.WriteLine("\n--- Stream Ended ---");
                break;
            // You can handle other event types like MessageStart, MessageDelta, ContentBlockStart, etc.
        }
    }
    // 'completeResponse' StringBuilder now holds the full text if you need it.
}
catch (AnthropicApiException ex)
{
    Console.WriteLine($"\nAPI Error during stream: {ex.Message}");
}
```

## Examples

For a more interactive demonstration of the SDK's capabilities, including the Messages API with streaming, check out the sample application in the `Examples` folder:

*   **`Anthropic.Net.SpectreSample`**: A console application using the [Spectre.Console](https://spectreconsole.net/) library for a rich user experience.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This SDK is licensed under the MIT License. See the LICENSE file for details.
