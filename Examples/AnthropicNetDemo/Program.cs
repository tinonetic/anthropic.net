namespace AnthropicNetDemo;

using Anthropic.Net;

/// <summary>
/// Basic Demo console program that shows library usage.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry method.
    /// </summary>
    public static void Main()
    {
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
        //    {
        //      "Anthropic": {
        //          "ApiKey": "YOUR-ANTHROPIC-API-KEY-FROM-ABOVE"
        //      }
        //    }
        //
        // 7. Run the demo

        //var apiClient = new AnthropicApiClient() 
        Console.WriteLine("Hello, World!");
    }
}
