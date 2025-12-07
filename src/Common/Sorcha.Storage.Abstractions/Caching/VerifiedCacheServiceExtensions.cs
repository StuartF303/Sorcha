// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sorcha.Storage.Abstractions.Caching;

/// <summary>
/// Extension methods for registering verified cache services.
/// </summary>
public static class VerifiedCacheServiceExtensions
{
    /// <summary>
    /// Adds verified cache configuration from appsettings.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddVerifiedCacheConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<VerifiedCacheConfiguration>(
            configuration.GetSection(VerifiedCacheConfiguration.SectionName));

        return services;
    }

    /// <summary>
    /// Adds a verified cache for a specific document type.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="idSelector">Function to extract ID from document.</param>
    /// <param name="hashSelector">Optional function to extract hash for verification.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddVerifiedCache<TDocument, TId>(
        this IServiceCollection services,
        Func<TDocument, TId> idSelector,
        Func<TDocument, string>? hashSelector = null)
        where TDocument : class
        where TId : notnull
    {
        services.AddSingleton<IVerifiedCache<TDocument, TId>>(sp =>
        {
            var cacheStore = sp.GetRequiredService<ICacheStore>();
            var wormStore = sp.GetRequiredService<IWormStore<TDocument, TId>>();
            var options = sp.GetService<IOptions<VerifiedCacheConfiguration>>();
            var logger = sp.GetService<ILogger<VerifiedCache<TDocument, TId>>>();

            return new VerifiedCache<TDocument, TId>(
                cacheStore,
                wormStore,
                idSelector,
                options,
                hashSelector,
                logger);
        });

        return services;
    }

    /// <summary>
    /// Adds a hosted service that warms the cache on startup.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddCacheWarmingService<TDocument, TId>(
        this IServiceCollection services)
        where TDocument : class
        where TId : notnull
    {
        services.AddHostedService<CacheWarmingHostedService<TDocument, TId>>();
        return services;
    }
}

/// <summary>
/// Hosted service that warms the verified cache on startup.
/// </summary>
/// <typeparam name="TDocument">Document type.</typeparam>
/// <typeparam name="TId">Document identifier type.</typeparam>
public class CacheWarmingHostedService<TDocument, TId> : IHostedService
    where TDocument : class
    where TId : notnull
{
    private readonly IVerifiedCache<TDocument, TId> _cache;
    private readonly IOptions<VerifiedCacheConfiguration> _options;
    private readonly ILogger<CacheWarmingHostedService<TDocument, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the CacheWarmingHostedService.
    /// </summary>
    public CacheWarmingHostedService(
        IVerifiedCache<TDocument, TId> cache,
        IOptions<VerifiedCacheConfiguration> options,
        ILogger<CacheWarmingHostedService<TDocument, TId>> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = _options.Value;

        _logger.LogInformation(
            "Cache warming service starting with strategy: {Strategy}",
            config.StartupStrategy);

        try
        {
            var currentSequence = await _cache.GetCurrentSequenceAsync(cancellationToken);

            if (currentSequence == 0)
            {
                _logger.LogInformation("No documents in WORM store, skipping cache warming");
                return;
            }

            var targetSequence = config.StartupStrategy == CacheStartupStrategy.Blocking
                ? Math.Min(currentSequence, (ulong)config.BlockingThreshold)
                : currentSequence;

            var progress = new Progress<CacheWarmingProgress>(p =>
            {
                _logger.LogDebug(
                    "Cache warming progress: {Loaded}/{Total} ({Percent:F1}%)",
                    p.DocumentsLoaded, p.TotalDocuments, p.PercentComplete);
            });

            if (config.StartupStrategy == CacheStartupStrategy.Blocking)
            {
                // Block until cache is warmed
                await _cache.WarmCacheAsync(targetSequence, progress, cancellationToken);
                _logger.LogInformation(
                    "Blocking cache warming completed for {Count} documents",
                    targetSequence);
            }
            else
            {
                // Start progressive warming in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _cache.WarmCacheAsync(targetSequence, progress, CancellationToken.None);
                        _logger.LogInformation(
                            "Progressive cache warming completed for {Count} documents",
                            targetSequence);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Progressive cache warming failed");
                    }
                }, CancellationToken.None);

                _logger.LogInformation(
                    "Progressive cache warming started for {Count} documents",
                    targetSequence);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed during startup");

            if (config.StartupStrategy == CacheStartupStrategy.Blocking)
            {
                throw; // Re-throw for blocking strategy
            }
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache warming service stopping");
        return Task.CompletedTask;
    }
}
