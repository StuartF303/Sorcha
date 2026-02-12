// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering blueprint cache services
/// </summary>
public static class BlueprintCacheExtensions
{
    /// <summary>
    /// Adds the blueprint cache and related services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlueprintCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<BlueprintCacheConfiguration>(
            configuration.GetSection(BlueprintCacheConfiguration.SectionName));

        // Register the cache as singleton (maintains L1 cache state)
        services.AddSingleton<IBlueprintCache, BlueprintCache>();

        return services;
    }
}
