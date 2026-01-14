using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Sorcha.UI.Core.Services.Authentication;
using Sorcha.UI.Core.Services.Configuration;
using Sorcha.UI.Core.Services.Encryption;
using Sorcha.UI.Core.Services.Http;

namespace Sorcha.UI.Core.Extensions;

/// <summary>
/// Extension methods for registering Sorcha.UI.Core services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core services (authentication, configuration, encryption)
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, string baseAddress)
    {
        // Encryption
        services.AddScoped<IEncryptionProvider, BrowserEncryptionProvider>();

        // Token cache
        services.AddScoped<ITokenCache, BrowserTokenCache>();

        // Configuration
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // Register a plain HttpClient for AuthenticationService (no message handler to avoid circular dependency)
        services.AddScoped<HttpClient>(sp => new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        });

        // Authentication
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CustomAuthenticationStateProvider>());

        // Auth state sync service (for SSR -> WASM token handoff)
        services.AddScoped<AuthStateSync>();

        // HTTP message handler for authenticated API calls (registered but not used by AuthenticationService)
        services.AddTransient<AuthenticatedHttpMessageHandler>();

        return services;
    }
}
