// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering Blueprint Version Resolver services
/// </summary>
public static class BlueprintVersionResolverExtensions
{
    /// <summary>
    /// Adds the Blueprint Version Resolver to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBlueprintVersionResolver(this IServiceCollection services)
    {
        // Regular blueprint version resolver (singleton - no scoped dependencies)
        services.AddSingleton<IBlueprintVersionResolver, BlueprintVersionResolver>();

        return services;
    }

    /// <summary>
    /// Adds the Control Blueprint Version Resolver to the service collection.
    /// The Control Blueprint Version Resolver tracks governance configuration
    /// versions through the control transaction chain.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddControlBlueprintVersionResolver(this IServiceCollection services)
    {
        // Control blueprint version resolver (scoped - depends on scoped services)
        services.AddScoped<IControlBlueprintVersionResolver, ControlBlueprintVersionResolver>();

        return services;
    }

    /// <summary>
    /// Adds both Blueprint and Control Blueprint Version Resolvers
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAllVersionResolvers(this IServiceCollection services)
    {
        services.AddBlueprintVersionResolver();
        services.AddControlBlueprintVersionResolver();

        return services;
    }
}
