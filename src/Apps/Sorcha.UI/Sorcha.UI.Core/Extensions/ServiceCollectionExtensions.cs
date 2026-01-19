using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.UI.Core.Models.Admin;
using Sorcha.UI.Core.Services;
using Sorcha.UI.Core.Services.Authentication;
using Sorcha.UI.Core.Services.Configuration;
using Sorcha.UI.Core.Services.Encryption;
using Sorcha.UI.Core.Services.Http;
using Sorcha.UI.Core.Services.Wallet;

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

        // Wallet API Service with authenticated HttpClient
        services.AddScoped<IWalletApiService>(sp =>
        {
            var handler = sp.GetRequiredService<AuthenticatedHttpMessageHandler>();
            handler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };

            return new WalletApiService(httpClient);
        });

        // Admin Services
        services.AddAdminServices(baseAddress);

        return services;
    }

    /// <summary>
    /// Registers admin dashboard services (health check, organization admin, audit).
    /// </summary>
    public static IServiceCollection AddAdminServices(this IServiceCollection services, string baseAddress)
    {
        // Configure health check options with default services
        services.Configure<HealthCheckOptions>(options =>
        {
            options.PollingIntervalMs = 30_000; // 30 seconds
            options.TimeoutMs = 5_000; // 5 seconds
            options.Services =
            [
                new ServiceEndpointConfig { ServiceName = "Blueprint Service", ServiceKey = "blueprint", HealthEndpoint = "/blueprint/health" },
                new ServiceEndpointConfig { ServiceName = "Register Service", ServiceKey = "register", HealthEndpoint = "/register/health" },
                new ServiceEndpointConfig { ServiceName = "Wallet Service", ServiceKey = "wallet", HealthEndpoint = "/wallet/health" },
                new ServiceEndpointConfig { ServiceName = "Tenant Service", ServiceKey = "tenant", HealthEndpoint = "/tenant/health" },
                new ServiceEndpointConfig { ServiceName = "Validator Service", ServiceKey = "validator", HealthEndpoint = "/validator/health" },
                new ServiceEndpointConfig { ServiceName = "Peer Service", ServiceKey = "peer", HealthEndpoint = "/peer/health" },
                new ServiceEndpointConfig { ServiceName = "API Gateway", ServiceKey = "gateway", HealthEndpoint = "/health" }
            ];
        });

        // Audit Service (used by organization admin service, so register first)
        services.AddScoped<IAuditService>(sp =>
        {
            var handler = sp.GetRequiredService<AuthenticatedHttpMessageHandler>();
            handler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };

            var logger = sp.GetRequiredService<ILogger<AuditService>>();
            return new AuditService(httpClient, logger);
        });

        // Health Check Service
        services.AddScoped<IHealthCheckService>(sp =>
        {
            var handler = sp.GetRequiredService<AuthenticatedHttpMessageHandler>();
            handler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };

            var options = sp.GetRequiredService<IOptions<HealthCheckOptions>>();
            var logger = sp.GetRequiredService<ILogger<HealthCheckService>>();
            return new HealthCheckService(httpClient, options, logger);
        });

        // Organization Admin Service
        services.AddScoped<IOrganizationAdminService>(sp =>
        {
            var handler = sp.GetRequiredService<AuthenticatedHttpMessageHandler>();
            handler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };

            var auditService = sp.GetRequiredService<IAuditService>();
            var logger = sp.GetRequiredService<ILogger<OrganizationAdminService>>();
            return new OrganizationAdminService(httpClient, auditService, logger);
        });

        return services;
    }
}
