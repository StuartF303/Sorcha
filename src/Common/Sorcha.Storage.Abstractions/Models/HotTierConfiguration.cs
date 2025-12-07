// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Configuration for hot tier (cache) storage.
/// </summary>
public class HotTierConfiguration
{
    /// <summary>
    /// Provider name: "Redis", "InMemory".
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Redis-specific configuration.
    /// </summary>
    public RedisConfiguration? Redis { get; set; }

    /// <summary>
    /// Default TTL for cache entries in seconds.
    /// Default: 900 (15 minutes).
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 900;

    /// <summary>
    /// Observability settings for this tier.
    /// </summary>
    public ObservabilityConfiguration? Observability { get; set; }

    /// <summary>
    /// Gets the default TTL as a TimeSpan.
    /// </summary>
    public TimeSpan DefaultTtl => TimeSpan.FromSeconds(DefaultTtlSeconds);
}

/// <summary>
/// Redis-specific configuration.
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for all cache entries.
    /// Default: "sorcha:".
    /// </summary>
    public string InstanceName { get; set; } = "sorcha:";

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Synchronous operation timeout in milliseconds.
    /// </summary>
    public int SyncTimeout { get; set; } = 1000;

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerConfiguration? CircuitBreaker { get; set; }
}

/// <summary>
/// Circuit breaker configuration for resilience.
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of failures allowed before breaking.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Sampling duration for failure threshold.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Duration to keep circuit open.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}
