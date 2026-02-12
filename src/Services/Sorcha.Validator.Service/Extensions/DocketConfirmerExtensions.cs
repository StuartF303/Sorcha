// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering DocketConfirmer services.
/// </summary>
public static class DocketConfirmerExtensions
{
    /// <summary>
    /// Adds the DocketConfirmer service to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDocketConfirmer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register configuration
        services.Configure<DocketConfirmerConfiguration>(
            configuration.GetSection(DocketConfirmerConfiguration.SectionName));

        // Register the confirmer
        services.AddScoped<IDocketConfirmer, DocketConfirmer>();

        return services;
    }

    /// <summary>
    /// Adds the DocketConfirmer service with custom configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDocketConfirmer(
        this IServiceCollection services,
        Action<DocketConfirmerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Register configuration
        services.Configure(configure);

        // Register the confirmer
        services.AddScoped<IDocketConfirmer, DocketConfirmer>();

        return services;
    }
}
