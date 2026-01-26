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
        services.AddSingleton<IBlueprintVersionResolver, BlueprintVersionResolver>();

        return services;
    }
}
