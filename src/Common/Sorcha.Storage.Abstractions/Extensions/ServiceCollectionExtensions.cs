// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Extension methods for registering storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds storage configuration from configuration section.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddStorageConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageConfiguration>(
            configuration.GetSection(StorageConfiguration.SectionName));

        return services;
    }

    /// <summary>
    /// Adds storage configuration with options action.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddStorageConfiguration(
        this IServiceCollection services,
        Action<StorageConfiguration> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// Adds the storage provider factory.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddStorageProviderFactory(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IStorageProviderFactory, StorageProviderFactory>();
        return services;
    }

    /// <summary>
    /// Adds the full storage layer with configuration and factory.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddSorchaStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStorageConfiguration(configuration);
        services.AddStorageProviderFactory();
        return services;
    }
}
