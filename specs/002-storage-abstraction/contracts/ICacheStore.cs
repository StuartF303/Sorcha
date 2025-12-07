// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
// CONTRACT: This file defines the interface specification for implementation

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Interface for hot-tier cache operations.
/// Implementations: Redis, InMemory
/// </summary>
/// <remarks>
/// The cache store provides ephemeral key-value storage with TTL support.
/// All operations are designed for high throughput and low latency.
/// Cache failures should be handled gracefully - data can be recomputed.
/// </remarks>
public interface ICacheStore
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value or null if not found/expired</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value with optional expiration.
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Optional absolute expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key existed and was removed</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists and is not expired</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value or creates it if not present (cache-aside pattern).
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create value if not cached</param>
    /// <param name="expiration">Optional expiration for newly created entry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or newly created value</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all keys matching a pattern.
    /// </summary>
    /// <param name="pattern">Key pattern (e.g., "user:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys removed</returns>
    Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments a counter (for rate limiting).
    /// </summary>
    /// <param name="key">Counter key</param>
    /// <param name="delta">Amount to increment (default 1)</param>
    /// <param name="expiration">Optional expiration (set on first increment)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New counter value after increment</returns>
    Task<long> IncrementAsync(
        string key,
        long delta = 1,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for cache operations.
/// </summary>
public record CacheStatistics(
    long TotalRequests,
    long Hits,
    long Misses,
    double AverageLatencyMs,
    double P99LatencyMs,
    long CurrentEntryCount,
    long EvictionCount)
{
    /// <summary>
    /// Cache hit rate (0.0 to 1.0).
    /// </summary>
    public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests : 0.0;
}
