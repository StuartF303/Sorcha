// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sorcha.Register.Core.Events;

namespace Sorcha.Register.Storage.Redis;

/// <summary>
/// Extension methods for registering Redis Streams event infrastructure
/// </summary>
public static class RedisEventStreamServiceExtensions
{
    /// <summary>
    /// Adds Redis Streams event publishing and subscribing from configuration
    /// </summary>
    public static IServiceCollection AddRedisEventStreams(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisEventStreamConfiguration>(
            configuration.GetSection("EventStreams:Redis"));

        services.TryAddSingleton<IEventPublisher, RedisStreamEventPublisher>();
        services.TryAddSingleton<IEventSubscriber, RedisStreamEventSubscriber>();
        services.AddHostedService<EventSubscriptionHostedService>();

        return services;
    }

    /// <summary>
    /// Adds Redis Streams event publishing and subscribing with programmatic configuration
    /// </summary>
    public static IServiceCollection AddRedisEventStreams(
        this IServiceCollection services,
        Action<RedisEventStreamConfiguration> configure)
    {
        services.Configure(configure);

        services.TryAddSingleton<IEventPublisher, RedisStreamEventPublisher>();
        services.TryAddSingleton<IEventSubscriber, RedisStreamEventSubscriber>();
        services.AddHostedService<EventSubscriptionHostedService>();

        return services;
    }
}
