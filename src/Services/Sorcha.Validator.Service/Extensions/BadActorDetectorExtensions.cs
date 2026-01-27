// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering BadActorDetector services.
/// </summary>
public static class BadActorDetectorExtensions
{
    /// <summary>
    /// Adds the BadActorDetector service to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBadActorDetector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register configuration
        services.Configure<BadActorDetectorConfiguration>(
            configuration.GetSection(BadActorDetectorConfiguration.SectionName));

        // Register as singleton for shared state across requests
        services.AddSingleton<IBadActorDetector, BadActorDetector>();

        // Register cleanup service
        services.AddHostedService<BadActorCleanupService>();

        return services;
    }

    /// <summary>
    /// Adds the BadActorDetector service with custom configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBadActorDetector(
        this IServiceCollection services,
        Action<BadActorDetectorConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Register configuration
        services.Configure(configure);

        // Register as singleton for shared state across requests
        services.AddSingleton<IBadActorDetector, BadActorDetector>();

        // Register cleanup service
        services.AddHostedService<BadActorCleanupService>();

        return services;
    }
}

/// <summary>
/// Background service for cleaning up expired bad actor incidents.
/// </summary>
public class BadActorCleanupService : BackgroundService
{
    private readonly IBadActorDetector _badActorDetector;
    private readonly BadActorDetectorConfiguration _config;
    private readonly ILogger<BadActorCleanupService> _logger;

    public BadActorCleanupService(
        IBadActorDetector badActorDetector,
        IOptions<BadActorDetectorConfiguration> config,
        ILogger<BadActorCleanupService> logger)
    {
        _badActorDetector = badActorDetector ?? throw new ArgumentNullException(nameof(badActorDetector));
        _config = config?.Value ?? new BadActorDetectorConfiguration();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Bad actor cleanup service started. Cleanup interval: {Interval}",
            _config.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.CleanupInterval, stoppingToken);

                if (_badActorDetector is BadActorDetector detector)
                {
                    detector.CleanupExpiredIncidents();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bad actor incident cleanup");
            }
        }

        _logger.LogInformation("Bad actor cleanup service stopped");
    }
}
