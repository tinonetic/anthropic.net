using Anthropic.Net;
using Anthropic.Net.Constants;
// using Anthropic.Net.Test; // This was an error in the previous attempt, removed.
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // For Task
using System.Threading; // For CancellationToken

public class Program
{
    public static async Task Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Anthropic.Net").Centered().Color(Color.Fuchsia));
        AnsiConsole.Write(new FigletText("Spectre Sample").Centered().Color(Color.Aqua));
        AnsiConsole.MarkupLine("Welcome to the Anthropic.Net SDK Spectre.Console Sample Application!");
        AnsiConsole.MarkupLine("This sample demonstrates the new Messages API.");
        AnsiConsole.WriteLine();

        // API Key Handling
        // Consider environment variables or user secrets for more secure API key management in real applications.
        // For this sample, we'll prompt directly.
        var apiKey = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your [green]Anthropic API Key[/]:")
                .PromptStyle("grey")
                .Secret());

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AnsiConsole.MarkupLine("[red]API Key cannot be empty. Exiting.[/]");
            return;
        }

        var client = new AnthropicApiClient(apiKey);

        // --- Non-Streaming Demo (CreateMessageAsync) ---
        AnsiConsole.MarkupLine("───────────────────────────────────");
        AnsiConsole.MarkupLine("[bold yellow]Demonstrating Non-Streaming Message (CreateMessageAsync)[/]");
        AnsiConsole.MarkupLine("───────────────────────────────────");

        var userMessageNonStreaming = AnsiConsole.Ask<string>("[cyan]Your message to Claude[/] (e.g., 'Tell me a short joke about programming.'):");

        var nonStreamingRequest = new MessagesRequest
        {
            Model = AnthropicModels.Claude_3_Sonnet_20240229, // Using Sonnet for non-streaming
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = userMessageNonStreaming }
            },
            MaxTokens = 250,
            System = "You are a helpful AI assistant." // Optional system prompt
        };

        try
        {
            await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("[grey]Claude is thinking...[/]", async ctx =>
            {
                var response = await client.CreateMessageAsync(nonStreamingRequest);

                AnsiConsole.WriteLine();
                var panel = new Panel(FormatMessageContent(response.Content))
                    .Header("[blue bold]Claude's Response[/]")
                    .Border(BoxBorder.Rounded)
                    .Padding(1, 1);
                AnsiConsole.Write(panel);

                AnsiConsole.MarkupLine($"[grey]Stop Reason:[/] {response.StopReason ?? "N/A"}");
                AnsiConsole.MarkupLine($"[grey]Token Usage:[/] Input: {response.Usage.InputTokens}, Output: {response.Usage.OutputTokens}");
            });
        }
        catch (AnthropicApiException ex)
        {
            AnsiConsole.MarkupLine("[bold red]API Error:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[bold red]An unexpected error occurred:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }

        AnsiConsole.WriteLine();

        // --- Streaming Demo (StreamMessageAsync) ---
        AnsiConsole.MarkupLine("───────────────────────────────────");
        AnsiConsole.MarkupLine("[bold yellow]Demonstrating Streaming Message (StreamMessageAsync)[/]");
        AnsiConsole.MarkupLine("───────────────────────────────────");
        var userMessageStreaming = AnsiConsole.Ask<string>("[cyan]Your message to Claude (streaming)[/] (e.g., 'Write a short poem about a curious cat.'):");

        var streamingRequest = new MessagesRequest
        {
            Model = AnthropicModels.Claude_3_Haiku_20240307, // Using Haiku for streaming for speed
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = userMessageStreaming }
            },
            MaxTokens = 300,
            System = "You are a creative poet AI."
        };

        try
        {
            AnsiConsole.MarkupLine("[blue bold]Claude's Streaming Response:[/]");
            var fullResponseText = new StringBuilder();

            await AnsiConsole.Live(new Markup(string.Empty)) 
                .StartAsync(async ctx =>
                {
                    ctx.UpdateTarget(new Markup(EscapeMarkup("Waiting for stream...")));
                    ctx.Refresh();
                    
                    await foreach (var streamEvent in client.StreamMessageAsync(streamingRequest, CancellationToken.None))
                    {
                        switch (streamEvent.Type)
                        {
                            case MessageStreamEventTypes.MessageStart:
                                var messageStart = streamEvent.Message;
                                // AnsiConsole.MarkupLine($"[grey dim]Stream started (ID: {messageStart?.Id}, Model: {messageStart?.Model})[/]");
                                // No text to add yet, but we can clear the "Waiting..."
                                fullResponseText.Clear(); 
                                ctx.UpdateTarget(new Markup(EscapeMarkup(fullResponseText.ToString())));
                                ctx.Refresh();
                                break;

                            case MessageStreamEventTypes.ContentBlockDelta:
                                if (streamEvent.Delta?.Text != null)
                                {
                                    fullResponseText.Append(streamEvent.Delta.Text);
                                    ctx.UpdateTarget(new Markup(EscapeMarkup(fullResponseText.ToString())));
                                    ctx.Refresh();
                                }
                                break;
                            
                            case MessageStreamEventTypes.MessageDelta:
                                if (streamEvent.Delta?.StopReason != null)
                                {
                                    // Append stop reason and usage at the end.
                                    // fullResponseText.Append($"\n[grey dim]Stop Reason: {streamEvent.Delta.StopReason}[/]");
                                }
                                if (streamEvent.Usage != null)
                                {
                                    // fullResponseText.Append($" [grey dim]Usage: In={streamEvent.Usage.InputTokens}, Out={streamEvent.Usage.OutputTokens}[/]");
                                }
                                ctx.UpdateTarget(new Markup(EscapeMarkup(fullResponseText.ToString())));
                                ctx.Refresh();
                                break;

                            case MessageStreamEventTypes.MessageStop:
                                // Final update, then a new line outside live display
                                ctx.UpdateTarget(new Markup(EscapeMarkup(fullResponseText.ToString())));
                                ctx.Refresh();
                                AnsiConsole.MarkupLine($"\n[grey dim]Stream ended.[/]");
                                break;
                                
                            case MessageStreamEventTypes.Error:
                                fullResponseText.Append($"\n[bold red]Error during stream: {streamEvent.Error?.ErrorDetails?.Message}[/]");
                                ctx.UpdateTarget(new Markup(EscapeMarkup(fullResponseText.ToString())));
                                ctx.Refresh();
                                break;

                            // Other events like ContentBlockStart, ContentBlockStop, Ping can be logged if desired
                            // but are not essential for displaying the primary text content.
                        }
                    }
                });
        }
        catch (AnthropicApiException ex)
        {
            AnsiConsole.MarkupLine("\n[bold red]API Error during stream setup:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("\n[bold red]An unexpected error occurred during stream:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }

        AnsiConsole.MarkupLine("───────────────────────────────────");
        AnsiConsole.MarkupLine("[bold green]Sample finished. Press any key to exit.[/]");
        Console.ReadKey();
    }

    private static string FormatMessageContent(List<ContentBlock> contentBlocks)
    {
        if (contentBlocks == null || !contentBlocks.Any())
        {
            return "[grey]No content received.[/]";
        }

        var sb = new StringBuilder();
        foreach (var block in contentBlocks.Where(b => b.Type == "text" && !string.IsNullOrEmpty(b.Text)))
        {
            sb.AppendLine(EscapeMarkup(block.Text));
        }
        
        if (sb.Length == 0) // Handle cases where there are content blocks but no text blocks
        {
            foreach (var block in contentBlocks)
            {
                sb.AppendLine($"[grey](Content type: {block.Type})[/]");
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static string EscapeMarkup(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
