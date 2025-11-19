# Anthropic.NET

![Tinonetic.Anthropic Logo](assets/logo.png)

[![anthropic_net NuGet Package](https://img.shields.io/nuget/v/anthropic.net.svg)](https://www.nuget.org/packages/anthropic.net/) [![anthropic_net NuGet Package Downloads](https://img.shields.io/nuget/dt/anthropic.net)](https://www.nuget.org/packages/anthropic.net) [![GitHub Actions Status](https://github.com/tinonetic/anthropic.net/workflows/Build/badge.svg?branch=main)](https://github.com/tinonetic/anthropic.net/actions)

Anthropic.NET is a community-written .NET SDK that gives you access to Anthropic's safety-first language model APIs (Claude).

## Features (v2.0)

-   **Modern API**: Designed for .NET 8.0+, fully asynchronous.
-   **Streaming Support**: Real-time response streaming with Server-Sent Events (SSE).
-   **Tools / Function Calling**: Define tools and handle tool use requests from Claude.
-   **Vision Support**: Easily send images for analysis.
-   **Dependency Injection**: Built-in support for `IHttpClientFactory` and `Microsoft.Extensions.DependencyInjection`.

## Installation

Install the package via NuGet:

```bash
dotnet add package Anthropic.Net
```

## Quick Start

### 1. Initialize the Client

You can instantiate `AnthropicApiClient` directly. It manages its own `HttpClient` internally to prevent socket exhaustion.

```csharp
using Anthropic.Net;

var client = new AnthropicApiClient("YOUR_API_KEY");
```

### 2. Chat Completion

```csharp
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;

var messages = new List<Message> { Message.FromUser("Hello, Claude!") };
var request = new MessageRequest(AnthropicModels.Claude3Sonnet, messages);

var response = await client.MessageAsync(request);
Console.WriteLine(response.Content.OfType<TextContentBlock>().First().Text);
```

### 3. Streaming

```csharp
using Anthropic.Net.Models.Messages.Streaming;

await foreach (var evt in client.StreamMessageAsync(request))
{
    if (evt is ContentBlockDeltaEvent delta && delta.Delta.Type == "text_delta")
    {
        Console.Write(delta.Delta.Text);
    }
}
```

### 4. Tools (Function Calling)

```csharp
var tool = new Tool("get_weather", "Get weather", new
{
    type = "object",
    properties = new { location = new { type = "string" } },
    required = new[] { "location" }
});

var request = new MessageRequest(AnthropicModels.Claude3Sonnet, messages)
{
    Tools = [tool]
};

var response = await client.MessageAsync(request);

if (response.StopReason == "tool_use")
{
    var toolUse = response.Content.OfType<ToolUseContentBlock>().First();
    Console.WriteLine($"Tool requested: {toolUse.Name}");
}
```

### 5. Vision

```csharp
var imageBlock = await ImageContentBlock.FromFileAsync("path/to/image.png");
var message = new Message("user", new List<ContentBlock> 
{ 
    new TextContentBlock("Describe this image."), 
    imageBlock 
});
```

## Dependency Injection

For ASP.NET Core or other DI-based applications, use the extension method to register the client.

```csharp
using Anthropic.Net.Extensions;

// In Program.cs or Startup.cs
builder.Services.AddAnthropicClient("YOUR_API_KEY");
```

You can then inject `AnthropicApiClient` into your controllers or services.

## Demo Application

Check out the `Examples/AnthropicNetDemo` project for a full interactive CLI demo.

```bash
cd Examples/AnthropicNetDemo
dotnet run
```

## License

MIT
