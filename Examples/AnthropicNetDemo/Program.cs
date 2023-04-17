namespace AnthropicNetDemo;

using System.Threading.Tasks;
using Anthropic.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// To run the demo
// 1. Go to https://console.anthropic.com/account/keys
// 2. Click "Create Key"
// 3. Copy API key to somewhere temporary
// 4. Go to your project's root directory
// 5. Open your CLI/terminal to save as dotnet secret - RECOMMENDED
// 6. Enable secret storage by running the following in the root directory:
//
//      dotnet user-secrets init
//
// 5. EDIT and PASTE the following with your copied API Key. Press ENTER to save it. :
//
//      dotnet user-secrets set "Anthropic:ApiKey" "YOUR-ANTHROPIC-API-KEY-FROM-ABOVE"
//
// 6. ALTERNATIVELY, have the key in your appsettings.json file with the following structure
//
//    {
//      "Anthropic": {
//          "ApiKey": "YOUR-ANTHROPIC-API-KEY-FROM-ABOVE"
//      }
//    }
//
//    THEN modify Config builder to use appsettings.json:
//
//      var config = new ConfigurationBuilder()
//           .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
//           .AddJsonFile("appsettings.json")
//           .AddUserSecrets<Program>()
//           .Build();
//
// 7. Run the demo

/// <summary>
/// Basic Demo console program that shows library usage.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry method.
    /// </summary>
    public static async Task Main()
    {
        // Boiler plate code
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddUserSecrets<Program>()
            .Build();

        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();

                services.AddTransient(cxt =>
                    {
                        // Anthropic-related code
                        var apiKey = config.GetSection("Anthropic:ApiKey").Value ?? "MISSING KEY";
                        var clientFactory = cxt.GetRequiredService<IHttpClientFactory>();
                        return new AnthropicApiClient(apiKey, clientFactory);
                    });
            }).UseConsoleLifetime().Build();

        var anthropicApiClient = host.Services.GetRequiredService<AnthropicApiClient>();
        var respost = await anthropicApiClient.CompletionAsync(new Dictionary<string, object> { { "prompt", "What is the history behind Hello World?" } });
        Console.WriteLine("Hello, World!");
    }
}
