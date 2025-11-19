using Anthropic.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace AnthropicNetDemo;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Setup Configuration (User Secrets)
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();
        var apiKey = builder.Configuration["Anthropic:ApiKey"]; // Changed to match typical user secrets structure

        // 2. Setup Spectre.Console
        AnsiConsole.Write(
            new FigletText("Anthropic.Net")
                .LeftJustified()
                .Color(Color.Teal));
        AnsiConsole.MarkupLine("[bold teal]v2.0 Demo Application[/]");
        AnsiConsole.WriteLine();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>("Please enter your [green]Anthropic API Key[/]:")
                    .Secret());
        }

        // 3. Initialize Client
        using var client = new AnthropicApiClient(apiKey);

        // 4. Main Menu Loop
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a demo:")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "Chat (Interactive)",
                        "Streaming Demo",
                        "Tools Demo (Weather)",
                        "Vision Demo (Image Analysis)",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Chat (Interactive)":
                    await RunChatAsync(client);
                    break;
                case "Streaming Demo":
                    await RunStreamingAsync(client);
                    break;
                case "Tools Demo (Weather)":
                    await RunToolsAsync(client);
                    break;
                case "Vision Demo (Image Analysis)":
                    await RunVisionAsync(client);
                    break;
                case "Exit":
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("Anthropic.Net")
                    .LeftJustified()
                    .Color(Color.Teal));
        }
    }

    static async Task RunChatAsync(AnthropicApiClient client)
    {
        AnsiConsole.MarkupLine("[bold yellow]--- Interactive Chat ---[/]");
        var messages = new List<Message>();

        while (true)
        {
            var input = AnsiConsole.Ask<string>("[green]You:[/]");
            if (input.ToLower() == "exit")
                break;

            messages.Add(Message.FromUser(input));

            await AnsiConsole.Status()
                .StartAsync("Thinking...", async ctx =>
                {
                    var request = new MessageRequest(AnthropicModels.Claude3Sonnet, messages);
                    var response = await client.MessageAsync(request);

                    var responseText = response.Content.OfType<TextContentBlock>().First().Text;
                    AnsiConsole.MarkupLine($"[blue]Claude:[/] {responseText}");
                    messages.Add(Message.FromAssistant(responseText));
                });
        }
    }

    static async Task RunStreamingAsync(AnthropicApiClient client)
    {
        AnsiConsole.MarkupLine("[bold yellow]--- Streaming Demo ---[/]");
        var input = AnsiConsole.Ask<string>("[green]Enter a prompt for streaming:[/]");

        var request = new MessageRequest(AnthropicModels.Claude3Sonnet, [Message.FromUser(input)]);

        AnsiConsole.Markup("[blue]Claude:[/] ");
        await foreach (var evt in client.StreamMessageAsync(request))
        {
            if (evt is ContentBlockDeltaEvent delta && delta.Delta.Type == "text_delta" && !string.IsNullOrEmpty(delta.Delta.Text))
            {
                AnsiConsole.Write(delta.Delta.Text);
            }
        }
        AnsiConsole.WriteLine();
    }

    static async Task RunToolsAsync(AnthropicApiClient client)
    {
        AnsiConsole.MarkupLine("[bold yellow]--- Tools Demo ---[/]");

        var tool = new Tool("get_weather", "Get weather for a location", new
        {
            type = "object",
            properties = new
            {
                location = new { type = "string", description = "City name" }
            },
            required = new[] { "location" }
        });

        var messages = new List<Message> { Message.FromUser("What is the weather in San Francisco?") };
        var request = new MessageRequest(AnthropicModels.Claude3Sonnet, messages)
        {
            Tools = [tool]
        };

        var userContent = (List<ContentBlock>)messages[0].Content;
        AnsiConsole.MarkupLine($"[green]User:[/] {userContent.OfType<TextContentBlock>().First().Text}");

        var response = await client.MessageAsync(request);

        if (response.StopReason == "tool_use")
        {
            var toolUse = response.Content.OfType<ToolUseContentBlock>().First();
            AnsiConsole.MarkupLine($"[yellow]Tool Use Requested:[/] {toolUse.Name}");
            AnsiConsole.MarkupLine($"[yellow]Input:[/] {System.Text.Json.JsonSerializer.Serialize(toolUse.Input)}");

            // Simulate tool execution
            var result = "The weather in San Francisco is 65 degrees and sunny.";
            AnsiConsole.MarkupLine($"[cyan]Tool Result:[/] {result}");

            messages.Add(new Message("assistant", response.Content));
            messages.Add(new Message("user", new List<ContentBlock> { new ToolResultContentBlock(toolUse.Id, result) }));

            var followUpRequest = new MessageRequest(AnthropicModels.Claude3Sonnet, messages) { Tools = [tool] };
            var followUpResponse = await client.MessageAsync(followUpRequest);

            var finalText = followUpResponse.Content.OfType<TextContentBlock>().First().Text;
            AnsiConsole.MarkupLine($"[blue]Claude:[/] {finalText}");
        }
    }

    static async Task RunVisionAsync(AnthropicApiClient client)
    {
        AnsiConsole.MarkupLine("[bold yellow]--- Vision Demo ---[/]");
        AnsiConsole.MarkupLine("Analyzing a sample image (1x1 pixel red dot for demo purposes)...");

        // Create a simple 1x1 red pixel PNG base64
        var redDotBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var imageBytes = Convert.FromBase64String(redDotBase64);

        var imageBlock = ImageContentBlock.FromBytes(imageBytes, "image/png");
        var message = new Message("user", new List<ContentBlock> {
            new TextContentBlock("What color is this image?"),
            imageBlock
        });

        var request = new MessageRequest(AnthropicModels.Claude3Sonnet, [message]);

        await AnsiConsole.Status()
            .StartAsync("Analyzing image...", async ctx =>
            {
                var response = await client.MessageAsync(request);
                var responseText = response.Content.OfType<TextContentBlock>().First().Text;
                AnsiConsole.MarkupLine($"[blue]Claude:[/] {responseText}");
            });
    }
}
