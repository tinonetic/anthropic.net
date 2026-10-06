using System.Text.Json;
using Anthropic.Net;
using Anthropic.Net.Constants;
using Anthropic.Net.Extensions;
using Anthropic.Net.ManagedAgents;
using Anthropic.Net.Models.Batches;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using Anthropic.Net.Platforms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace AnthropicNetDemo;

/// <summary>
/// Interactive demo of Anthropic.Net. Each menu entry exercises one area of the SDK.
/// Some demos cost a little (they call the real API); the menu says which ones need extra credentials.
/// </summary>
class Program
{
    const string Fast = AnthropicModels.ClaudeSonnet55;
    const string Smart = AnthropicModels.ClaudeOpus55;

    static readonly (string Title, Func<AnthropicApiClient, Task> Run)[] Demos =
    [
        ("Chat (interactive)", RunChatAsync),
        ("Streaming (text + final message + usage)", RunStreamingAsync),
        ("Tools: automatic tool loop", RunToolsAsync),
        ("Vision (image analysis)", RunVisionAsync),
        ("Thinking + effort (streamed reasoning)", RunThinkingAsync),
        ("Server tool: web search with sources", RunWebSearchAsync),
        ("Structured output (JSON schema)", RunStructuredOutputAsync),
        ("Prompt caching (cache write, then read)", RunCachingAsync),
        ("Documents with citations", RunDocumentCitationsAsync),
        ("Token counting + model discovery", RunModelsAndTokensAsync),
        ("Message Batches (create, poll, results)", RunBatchesAsync),
        ("Files API (upload, list, delete)", RunFilesAsync),
        ("Managed Agents (agent, environment, session)", RunManagedAgentsAsync),
        ("Admin API (needs an admin key)", RunAdminAsync),
        ("Cloud platforms (Bedrock / AWS / Vertex / Foundry)", RunPlatformsAsync),
    ];

    static async Task Main(string[] args)
    {
        // 1. Setup Configuration (User Secrets)
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();
        var apiKey = builder.Configuration["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        // 2. Banner
        Banner();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>("Please enter your [green]Anthropic API Key[/]:")
                    .Secret());
        }

        // 3. Initialize client: retries (429/5xx/529) are on by default; tune them here.
        using var client = new AnthropicApiClient(new AnthropicClientOptions
        {
            ApiKey = apiKey,
            AuthToken = null,
            MaxRetries = 3,
            Timeout = TimeSpan.FromMinutes(5),
        });

        // 4. Main menu loop
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a demo:")
                    .PageSize(20)
                    .AddChoices(Demos.Select(d => d.Title).Append("Exit")));

            if (choice == "Exit")
            {
                return;
            }

            try
            {
                await Demos.First(d => d.Title == choice).Run(client);
            }
            catch (AnthropicApiException ex)
            {
                // Typed errors: status, API error type, and the request id to quote to support.
                AnsiConsole.MarkupLine($"[red]API error {ex.StatusCode}[/] [grey]({Markup.Escape(ex.ErrorType ?? "n/a")}, request {Markup.Escape(ex.RequestId ?? "n/a")}, retryable: {ex.IsRetryable})[/]");
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
            catch (Exception ex) when (ex is NotSupportedException or ArgumentException or InvalidOperationException)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            Banner();
        }
    }

    static void Banner()
    {
        AnsiConsole.Write(new FigletText("tinonetic.Anthropic").LeftJustified().Color(Color.Teal));
        AnsiConsole.MarkupLine("[bold teal]v3.0 Demo Application[/]");
        AnsiConsole.WriteLine();
    }

    static void Say(string who, string text, string color = "blue") => AnsiConsole.MarkupLine($"[{color}]{who}:[/] {Markup.Escape(text)}");

    static void Title(string text) => AnsiConsole.MarkupLine($"[bold yellow]--- {Markup.Escape(text)} ---[/]");

    static void ShowUsage(MessageResponse r)
    {
        var u = r.Usage;
        AnsiConsole.MarkupLine($"[grey]tokens in/out: {u.InputTokens}/{u.OutputTokens}, cache write/read: {u.CacheCreationInputTokens ?? 0}/{u.CacheReadInputTokens ?? 0}, stop: {Markup.Escape(r.StopReason ?? "n/a")}[/]");
        if (r.StopReason == "refusal")
        {
            AnsiConsole.MarkupLine($"[red]Refused ({Markup.Escape(r.StopDetails?.Category ?? "unknown")}): {Markup.Escape(r.StopDetails?.Explanation ?? string.Empty)}[/]");
        }
    }

    static async Task RunChatAsync(AnthropicApiClient client)
    {
        Title("Interactive Chat (type 'exit' to stop)");
        var messages = new List<Message>();

        while (true)
        {
            var input = AnsiConsole.Ask<string>("[green]You:[/]");
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            messages.Add(Message.FromUser(input));
            var response = await AnsiConsole.Status().StartAsync("Thinking...", _ => client.MessageAsync(new MessageRequest(Fast, messages)));
            Say("Claude", response.Text);
            messages.Add(response.ToAssistantMessage());
        }
    }

    static async Task RunStreamingAsync(AnthropicApiClient client)
    {
        Title("Streaming");
        var input = AnsiConsole.Ask<string>("[green]Enter a prompt for streaming:[/]");
        var request = new MessageRequest(Fast, [Message.FromUser(input)]);

        AnsiConsole.Markup("[blue]Claude:[/] ");

        // StreamMessageToFinalAsync accumulates the stream into a full MessageResponse; the callback sees every event.
        var final = await client.StreamMessageToFinalAsync(request, e =>
        {
            if (e is ContentBlockDeltaEvent { Delta: { Type: "text_delta", Text: { } text } })
            {
                Console.Write(text);
            }
        });

        AnsiConsole.WriteLine();
        ShowUsage(final);
    }

    static async Task RunToolsAsync(AnthropicApiClient client)
    {
        Title("Tools: automatic tool loop");
        var tools = new List<Tool>
        {
            new("get_weather", "Get the current weather for a city", new
            {
                type = "object",
                properties = new { city = new { type = "string", description = "City name" } },
                required = new[] { "city" },
                additionalProperties = false,
            })
            { Strict = true },
        };

        var request = new MessageRequest(Fast, [Message.FromUser("What is the weather in Paris and in Tokyo? Compare them.")])
        {
            Tools = tools,
        };

        // RunToolsAsync calls the model, runs every requested tool concurrently, returns all results in ONE user message, and repeats.
        var final = await client.RunToolsAsync(request, (use, _) =>
        {
            var city = ((JsonElement)use.Input).GetProperty("city").GetString();
            AnsiConsole.MarkupLine($"[yellow]Tool call:[/] {use.Name}({Markup.Escape(city ?? string.Empty)})");
            return Task.FromResult<object>($"It is 18 degrees and sunny in {city}.");
        });

        Say("Claude", final.Text);
        ShowUsage(final);
    }

    static async Task RunVisionAsync(AnthropicApiClient client)
    {
        Title("Vision");
        AnsiConsole.MarkupLine("Analyzing a sample image (1x1 pixel red dot for demo purposes)...");

        var redDotBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var imageBlock = ImageContentBlock.FromBytes(Convert.FromBase64String(redDotBase64), "image/png");
        var message = new Message("user", new List<ContentBlock> { new TextContentBlock("What color is this image?"), imageBlock });

        var response = await AnsiConsole.Status().StartAsync("Analyzing image...", _ => client.MessageAsync(new MessageRequest(Fast, [message])));
        Say("Claude", response.Text);
    }

    static async Task RunThinkingAsync(AnthropicApiClient client)
    {
        Title("Thinking + effort");
        var effort = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Effort level:").AddChoices("low", "medium", "high", "xhigh", "max"));
        var question = AnsiConsole.Ask("[green]Question:[/]", "A bat and a ball cost $1.10 together; the bat costs $1.00 more than the ball. What does the ball cost?");

        // Adaptive thinking is the default on current models; display "summarized" makes the reasoning readable.
        var request = new MessageRequest(Smart, [Message.FromUser(question)], maxTokens: 16000)
        {
            Thinking = ThinkingConfig.Adaptive(display: "summarized"),
            OutputConfig = new OutputConfig { Effort = effort },
        };

        await foreach (var e in client.StreamMessageAsync(request))
        {
            if (e is not ContentBlockDeltaEvent { Delta: var d })
            {
                continue;
            }

            if (d is { Type: "thinking_delta", Thinking: { } thought })
            {
                AnsiConsole.Markup($"[grey]{Markup.Escape(thought)}[/]");
            }
            else if (d is { Type: "text_delta", Text: { } text })
            {
                Console.Write(text);
            }
        }

        AnsiConsole.WriteLine();
    }

    static async Task RunWebSearchAsync(AnthropicApiClient client)
    {
        Title("Server tool: web search");
        var query = AnsiConsole.Ask("[green]Ask something current:[/]", "What are the latest Claude model releases?");

        // Server tools run on Anthropic's side - one call, no loop. Results arrive as typed blocks.
        var request = new MessageRequest(Smart, [Message.FromUser(query)], maxTokens: 8000)
        {
            Tools = [Tool.WebSearch(maxUses: 2)],
        };
        var response = await AnsiConsole.Status().StartAsync("Searching...", _ => client.MessageAsync(request));

        foreach (var block in response.Content)
        {
            switch (block)
            {
                case ServerToolUseContentBlock use:
                    AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(use.Name)}:[/] {Markup.Escape(use.Input.ToString())}");
                    break;
                case WebSearchToolResultContentBlock { Content.ValueKind: JsonValueKind.Array } result:
                    foreach (var hit in result.Content.EnumerateArray())
                    {
                        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(hit.GetProperty("title").GetString() ?? string.Empty)} - {Markup.Escape(hit.GetProperty("url").GetString() ?? string.Empty)}[/]");
                    }

                    break;
                case WebSearchToolResultContentBlock error:
                    // Server tool errors come back with HTTP 200, not as exceptions.
                    AnsiConsole.MarkupLine($"[red]Search failed: {Markup.Escape(error.Content.ToString())}[/]");
                    break;
            }
        }

        Say("Claude", response.Text);
        ShowUsage(response);
    }

    static async Task RunStructuredOutputAsync(AnthropicApiClient client)
    {
        Title("Structured output");
        var schema = new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                year = new { type = "integer" },
                tags = new { type = "array", items = new { type = "string" } },
            },
            required = new[] { "name", "year", "tags" },
            additionalProperties = false,
        };

        var request = new MessageRequest(Fast, [Message.FromUser("Describe the C# language as a record.")])
        {
            OutputConfig = new OutputConfig { Format = OutputFormat.JsonSchema(schema) },
        };
        var response = await client.MessageAsync(request);

        using var doc = JsonDocument.Parse(response.Text);
        AnsiConsole.WriteLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
    }

    static async Task RunCachingAsync(AnthropicApiClient client)
    {
        Title("Prompt caching");
        AnsiConsole.MarkupLine("[grey]Two identical requests: the first writes the cache, the second reads it.[/]");

        // The cached prefix must be large enough (model dependent, up to ~4k tokens).
        var longContext = string.Join("\n", Enumerable.Range(1, 400).Select(i => $"Reference fact #{i}: the number {i} squared is {i * i}."));
        MessageRequest Build() => new(Fast, [Message.FromUser("What is 37 squared, according to the reference?")], 200)
        {
            System = new List<TextContentBlock>
            {
                new("Answer using only the reference below.\n" + longContext) { CacheControl = CacheControl.Ephemeral() },
            },
        };

        for (var i = 1; i <= 2; i++)
        {
            var response = await client.MessageAsync(Build());
            AnsiConsole.MarkupLine($"[blue]Request {i}:[/] {Markup.Escape(response.Text)}");
            ShowUsage(response);
        }
    }

    static async Task RunDocumentCitationsAsync(AnthropicApiClient client)
    {
        Title("Documents with citations");
        var doc = DocumentContentBlock.FromText(
            "The Eiffel Tower was completed in 1889. It is 330 metres tall. It was designed by the engineering firm of Gustave Eiffel.",
            title: "Eiffel Tower facts",
            citations: true);
        var message = new Message("user", new List<ContentBlock> { doc, new TextContentBlock("When was it completed and how tall is it?") });

        var response = await client.MessageAsync(new MessageRequest(Fast, [message]));
        foreach (var block in response.Content.OfType<TextContentBlock>())
        {
            AnsiConsole.MarkupLine($"{Markup.Escape(block.Text)} [grey]{(block.Citations?.Count > 0 ? $"[[{block.Citations.Count} citation(s): {Markup.Escape(block.Citations[0].GetProperty("cited_text").GetString() ?? string.Empty)}]]" : string.Empty)}[/]");
        }
    }

    static async Task RunModelsAndTokensAsync(AnthropicApiClient client)
    {
        Title("Token counting + model discovery");

        var request = new MessageRequest(Fast, [Message.FromUser("Count the tokens in this sentence, please.")]);
        var count = await client.CountTokensAsync(request);
        AnsiConsole.MarkupLine($"Input tokens: [green]{count.InputTokens}[/] (free to count)");

        var page = await client.ListModelsAsync(limit: 50);
        var table = new Table().AddColumns("Id", "Name", "Context", "Max output");
        foreach (var m in page.Data)
        {
            table.AddRow(Markup.Escape(m.Id), Markup.Escape(m.DisplayName), m.MaxInputTokens?.ToString("N0") ?? "?", m.MaxTokens?.ToString("N0") ?? "?");
        }

        AnsiConsole.Write(table);
    }

    static async Task RunBatchesAsync(AnthropicApiClient client)
    {
        Title("Message Batches (50% cost, asynchronous)");
        var requests = new[] { "France", "Japan", "Brazil" }
            .Select(c => new BatchRequestItem($"capital-{c}", new MessageRequest(AnthropicModels.ClaudeHaiku45, [Message.FromUser($"Capital of {c}? One word.")], 50)))
            .ToList();

        var batch = await client.CreateBatchAsync(requests);
        AnsiConsole.MarkupLine($"Created [green]{batch.Id}[/]");

        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (batch.ProcessingStatus != "ended" && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            batch = await client.GetBatchAsync(batch.Id);
            AnsiConsole.MarkupLine($"[grey]{batch.ProcessingStatus}: {batch.RequestCounts.Succeeded} ok, {batch.RequestCounts.Processing} processing[/]");
        }

        if (batch.ProcessingStatus != "ended")
        {
            AnsiConsole.MarkupLine("[yellow]Still running - cancelling.[/]");
            await client.CancelBatchAsync(batch.Id);
            return;
        }

        // Results arrive in any order: match on CustomId.
        await foreach (var item in client.GetBatchResultsAsync(batch.Id))
        {
            Say(item.CustomId, item.Result.Type == "succeeded" ? item.Result.Message!.Text : $"({item.Result.Type})", "green");
        }
    }

    static async Task RunFilesAsync(AnthropicApiClient client)
    {
        Title("Files API");
        var bytes = System.Text.Encoding.UTF8.GetBytes("Project codename: Teal Harbor. Launch date: 2027-03-01.");
        using var stream = new MemoryStream(bytes);
        var file = await client.UploadFileAsync(stream, "notes.txt", "text/plain");
        AnsiConsole.MarkupLine($"Uploaded [green]{file.Id}[/] ({file.SizeBytes} bytes)");

        try
        {
            // Reference the upload by id instead of re-sending the content.
            var doc = new DocumentContentBlock { Source = new DocumentSource { Type = "file", FileId = file.Id } };
            var message = new Message("user", new List<ContentBlock> { doc, new TextContentBlock("What is the launch date?") });
            var response = await client.MessageAsync(new MessageRequest(Fast, [message]));
            Say("Claude", response.Text);

            var files = await client.ListFilesAsync(limit: 5);
            AnsiConsole.MarkupLine($"[grey]{files.Data.Count} file(s) listed[/]");
        }
        finally
        {
            await client.DeleteFileAsync(file.Id);
            AnsiConsole.MarkupLine("[grey]Deleted the upload.[/]");
        }
    }

    static async Task RunManagedAgentsAsync(AnthropicApiClient client)
    {
        Title("Managed Agents (beta)");
        AnsiConsole.MarkupLine("[grey]Creates an agent + cloud environment + session. The session and environment are deleted at the end; archiving the agent is permanent, so you are asked first.[/]");
        if (!AnsiConsole.Confirm("Continue?"))
        {
            return;
        }

        var api = client.ManagedAgents;
        var agent = await api.Agents.CreateAsync(new CreateAgentRequest("Demo helper", Smart)
        {
            System = "You are a concise assistant. You may use your sandbox to compute things.",
            Tools = [new { type = "agent_toolset_20260401" }],
        });
        var env = await api.Environments.CreateAsync(new CreateEnvironmentRequest("demo-sandbox", new { type = "cloud", networking = new { type = "unrestricted" } }));
        var session = await api.Sessions.CreateAsync(new CreateSessionRequest(agent.Id, env.Id) { Title = "Demo session" });
        AnsiConsole.MarkupLine($"Agent [green]{agent.Id}[/], environment [green]{env.Id}[/], session [green]{session.Id}[/]");

        try
        {
            // Open the stream BEFORE sending, so no events are missed.
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var done = new TaskCompletionSource();
            var reader = Task.Run(async () =>
            {
                await foreach (var e in api.Sessions.Events.StreamAsync(session.Id, ct: cts.Token))
                {
                    if (e.Type == "agent.message")
                    {
                        Say("Agent", e.Text);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(e.Type)}[/]");
                    }

                    if (e.Type.Contains("idle", StringComparison.Ordinal) || e.Type.Contains("terminated", StringComparison.Ordinal))
                    {
                        done.TrySetResult();
                        return;
                    }
                }
            }, cts.Token);

            await Task.Delay(1000);
            await api.Sessions.Events.SendAsync(session.Id, [SessionEvents.UserMessage("Compute the 20th Fibonacci number and tell me the result.")]);
            await Task.WhenAny(done.Task, reader);
            cts.Cancel();
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Timed out waiting for the agent.[/]");
        }
        finally
        {
            await api.Sessions.DeleteAsync(session.Id);
            await api.Environments.DeleteAsync(env.Id);
            AnsiConsole.MarkupLine("[grey]Deleted the session and environment.[/]");
        }

        if (AnsiConsole.Confirm($"Archive agent {agent.Id}? This cannot be undone.", false))
        {
            await api.Agents.ArchiveAsync(agent.Id);
        }
    }

    static async Task RunAdminAsync(AnthropicApiClient _)
    {
        Title("Admin API");
        AnsiConsole.MarkupLine("[grey]Needs an Admin API key (sk-ant-admin...). Regular keys are rejected. Read-only calls only.[/]");
        var key = AnsiConsole.Prompt(new TextPrompt<string>("Admin API key:").Secret().AllowEmpty());
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        using var admin = new AnthropicApiClient(new AnthropicClientOptions { ApiKey = key, AuthToken = null });
        var org = await admin.Admin.GetOrganizationAsync();
        AnsiConsole.MarkupLine($"Organization: [green]{Markup.Escape(org.Name ?? org.Id)}[/]");

        var users = await admin.Admin.Users.ListAsync(limit: 10);
        foreach (var u in users.Data)
        {
            AnsiConsole.MarkupLine($"  {Markup.Escape(u.Email ?? u.Id)} [grey]({Markup.Escape(u.Role ?? "?")})[/]");
        }

        var workspaces = await admin.Admin.Workspaces.ListAsync(limit: 10);
        AnsiConsole.MarkupLine($"{workspaces.Data.Count} workspace(s): {Markup.Escape(string.Join(", ", workspaces.Data.Select(w => w.Name)))}");
    }

    static async Task RunPlatformsAsync(AnthropicApiClient _)
    {
        Title("Cloud platforms");
        var platform = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Platform:").AddChoices("Amazon Bedrock", "Claude Platform on AWS", "Google Vertex AI", "Microsoft Foundry"));

        AnthropicApiClient client;
        string model;
        switch (platform)
        {
            case "Amazon Bedrock":
                var bedrockKey = AnsiConsole.Prompt(new TextPrompt<string>("Bedrock API key (blank = SigV4 from AWS_* env vars):").Secret().AllowEmpty());
                client = AnthropicBedrockMantle.Create(AnsiConsole.Ask("Region:", "us-east-1"), string.IsNullOrWhiteSpace(bedrockKey) ? null : bedrockKey);
                model = AnthropicBedrockMantle.ModelId(AnthropicModels.ClaudeSonnet55);
                break;
            case "Claude Platform on AWS":
                client = AnthropicAws.Create(AnsiConsole.Ask("Region:", "us-east-1"), AnsiConsole.Ask<string>("Workspace id:"));
                model = AnthropicModels.ClaudeSonnet55;
                break;
            case "Google Vertex AI":
                var token = AnsiConsole.Prompt(new TextPrompt<string>("Access token (gcloud auth print-access-token):").Secret());
                client = AnthropicVertex.Create(AnsiConsole.Ask<string>("GCP project id:"), AnsiConsole.Ask("Region:", "global"), _ => Task.FromResult(token));
                model = AnthropicModels.ClaudeSonnet55;
                break;
            default:
                client = AnthropicFoundry.Create(AnsiConsole.Ask<string>("Foundry resource name:"), AnsiConsole.Prompt(new TextPrompt<string>("API key:").Secret()));
                model = AnthropicModels.ClaudeSonnet55;
                break;
        }

        using (client)
        {
            var response = await client.MessageAsync(new MessageRequest(model, [Message.FromUser("Say hello in five words.")], 100));
            Say(platform, response.Text);
            ShowUsage(response);
        }
    }
}
