// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.Redis;

/// <summary>
/// Extension methods for registering Redis storage services.
/// </summary>
public static class RedisServiceExtensions
{
    /// <summary>
    /// Adds Redis cache store as the ICacheStore implementation.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration for Redis connection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddRedisCacheStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HotTierConfiguration>(
            configuration.GetSection("Storage:Hot"));

        services.TryAddSingleton<ICacheStore, RedisCacheStore>();

        return services;
    }

    /// <summary>
    /// Adds Redis cache store with explicit configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Action to configure hot tier settings.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddRedisCacheStore(
        this IServiceCollection services,
        Action<HotTierConfiguration> configure)
    {
        services.Configure(configure);
        services.TryAddSingleton<ICacheStore, RedisCacheStore>();

        return services;
    }

    /// <summary>
    /// Adds Redis cache store with connection string.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Redis connection string.</param>
    /// <param name="keyPrefix">Key prefix for cache entries.</param>
    /// <param name="defaultTtlSeconds">Default TTL in seconds.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddRedisCacheStore(
        this IServiceCollection services,
        string connectionString,
        string keyPrefix = "sorcha:",
        int defaultTtlSeconds = 900)
    {
        services.Configure<HotTierConfiguration>(config =>
        {
            config.Provider = "Redis";
            config.DefaultTtlSeconds = defaultTtlSeconds;
            config.Redis = new RedisConfiguration
            {
                ConnectionString = connectionString,
                InstanceName = keyPrefix
            };
        });

        services.TryAddSingleton<ICacheStore, RedisCacheStore>();

        return services;
    }
}
