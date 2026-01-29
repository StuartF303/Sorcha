// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering genesis config cache services
/// </summary>
public static class GenesisConfigServiceExtensions
{
    /// <summary>
    /// Adds the genesis config service and related services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGenesisConfigService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<GenesisConfigCacheConfiguration>(
            configuration.GetSection(GenesisConfigCacheConfiguration.SectionName));

        // Register as scoped (depends on scoped IRegisterServiceClient)
        // L1 cache state is still maintained per-request, L2 cache is in Redis
        services.AddScoped<IGenesisConfigService, GenesisConfigService>();

        return services;
    }
}
