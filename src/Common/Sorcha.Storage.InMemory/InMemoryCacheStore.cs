// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.InMemory;

/// <summary>
/// In-memory implementation of ICacheStore for development and testing.
/// </summary>
public class InMemoryCacheStore : ICacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiration;
    private long _totalRequests;
    private long _hits;
    private long _misses;
    private long _evictions;

    /// <summary>
    /// Initializes a new instance of the InMemoryCacheStore.
    /// </summary>
    /// <param name="defaultExpiration">Default expiration for cache entries.</param>
    public InMemoryCacheStore(TimeSpan? defaultExpiration = null)
    {
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(15);
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalRequests);

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                Interlocked.Increment(ref _evictions);
                Interlocked.Increment(ref _misses);
                return Task.FromResult<T?>(default);
            }

            Interlocked.Increment(ref _hits);
            return Task.FromResult(JsonSerializer.Deserialize<T>(entry.SerializedValue));
        }

        Interlocked.Increment(ref _misses);
        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new CacheEntry(
            JsonSerializer.Serialize(value),
            DateTime.UtcNow,
            expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null);

        _cache.AddOrUpdate(key, entry, (_, _) => entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                Interlocked.Increment(ref _evictions);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration ?? _defaultExpiration, cancellationToken);
        return value;
    }

    /// <inheritdoc/>
    public Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");
        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();

        long removed = 0;
        foreach (var key in keysToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                removed++;
            }
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc/>
    public Task<long> IncrementAsync(
        string key,
        long delta = 1,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var entry = _cache.AddOrUpdate(
            key,
            _ => new CacheEntry(
                JsonSerializer.Serialize(delta),
                DateTime.UtcNow,
                expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null),
            (_, existing) =>
            {
                var currentValue = JsonSerializer.Deserialize<long>(existing.SerializedValue);
                return new CacheEntry(
                    JsonSerializer.Serialize(currentValue + delta),
                    existing.CreatedAt,
                    existing.ExpiresAt);
            });

        return Task.FromResult(JsonSerializer.Deserialize<long>(entry.SerializedValue));
    }

    /// <inheritdoc/>
    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new CacheStatistics(
            TotalRequests: Interlocked.Read(ref _totalRequests),
            Hits: Interlocked.Read(ref _hits),
            Misses: Interlocked.Read(ref _misses),
            AverageLatencyMs: 0.1, // In-memory is essentially instant
            P99LatencyMs: 0.5,
            CurrentEntryCount: _cache.Count,
            EvictionCount: Interlocked.Read(ref _evictions));

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        Interlocked.Exchange(ref _evictions, 0);
    }

    private record CacheEntry(string SerializedValue, DateTime CreatedAt, DateTime? ExpiresAt);
}
