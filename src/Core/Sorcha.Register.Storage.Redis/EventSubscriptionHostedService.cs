// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sorcha.Register.Core.Events;

namespace Sorcha.Register.Storage.Redis;

/// <summary>
/// Background service that starts the Redis Streams event processing loop
/// </summary>
public class EventSubscriptionHostedService : BackgroundService
{
    private readonly IEventSubscriber _subscriber;
    private readonly ILogger<EventSubscriptionHostedService> _logger;

    public EventSubscriptionHostedService(
        IEventSubscriber subscriber,
        ILogger<EventSubscriptionHostedService> logger)
    {
        _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Brief startup delay to allow subscriptions to be registered
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("EventSubscriptionHostedService starting processing loop");

        if (_subscriber is RedisStreamEventSubscriber redisSubscriber)
        {
            await redisSubscriber.StartProcessingAsync(stoppingToken);
        }
        else
        {
            _logger.LogInformation(
                "Event subscriber is {Type}, not RedisStreamEventSubscriber — no processing loop needed",
                _subscriber.GetType().Name);
        }
    }
}
