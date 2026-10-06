namespace Anthropic.Net.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Anthropic.Net services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AnthropicServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="AnthropicApiClient"/> (also resolvable as <see cref="IAnthropicApiClient"/>) to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAnthropicClient(this IServiceCollection services, string apiKey)
        => services.AddAnthropicClient(o =>
        {
            o.ApiKey = apiKey;
            o.AuthToken = null;
        });

    /// <summary>
    /// Adds the <see cref="AnthropicApiClient"/> configured through <see cref="AnthropicClientOptions"/>.
    /// Without a configure action the API key is read from ANTHROPIC_API_KEY.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">Configures the options (key, base URL, retries, timeout, default betas).</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAnthropicClient(this IServiceCollection services, Action<AnthropicClientOptions>? configure = null)
    {
        var options = new AnthropicClientOptions();
        configure?.Invoke(options);

        services.AddHttpClient();
        services.AddSingleton(options);
        services.AddTransient<AnthropicApiClient>(sp =>
            new AnthropicApiClient(sp.GetRequiredService<AnthropicClientOptions>(), sp.GetRequiredService<IHttpClientFactory>()));
        services.AddTransient<IAnthropicApiClient>(sp => sp.GetRequiredService<AnthropicApiClient>());

        return services;
    }
}
