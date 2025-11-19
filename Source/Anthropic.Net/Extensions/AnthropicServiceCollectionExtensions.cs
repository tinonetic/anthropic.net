namespace Anthropic.Net.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Anthropic.Net services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AnthropicServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="AnthropicApiClient"/> to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAnthropicClient(this IServiceCollection services, string apiKey)
    {
        services.AddHttpClient();
        services.AddTransient<AnthropicApiClient>(sp =>
            new AnthropicApiClient(apiKey, sp.GetRequiredService<IHttpClientFactory>()));

        return services;
    }
}
