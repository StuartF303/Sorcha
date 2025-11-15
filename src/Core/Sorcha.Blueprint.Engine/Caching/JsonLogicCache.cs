// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;

namespace Sorcha.Blueprint.Engine.Caching;

/// <summary>
/// Cache for compiled/parsed JSON Logic expressions
/// </summary>
/// <remarks>
/// Caches expressions by their hash to avoid re-parsing the same expression multiple times.
/// Significant performance improvement for frequently-used expressions.
/// </remarks>
public class JsonLogicCache
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions;

    public JsonLogicCache(IMemoryCache cache)
    {
        _cache = cache;
        _defaultOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Size = 1 // Each entry counts as 1 unit
        };
    }

    public JsonLogicCache() : this(new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = 1000 // Max 1000 cached expressions
    }))
    {
    }

    /// <summary>
    /// Get or add an expression to the cache
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="expression">The JSON Logic expression</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <returns>The cached or newly created value</returns>
    public T GetOrAdd<T>(JsonNode expression, Func<JsonNode, T> factory)
    {
        var key = ComputeHash(expression);

        return _cache.GetOrCreate(key, entry =>
        {
            entry.SetOptions(_defaultOptions);
            return factory(expression);
        })!;
    }

    /// <summary>
    /// Get or add an expression to the cache asynchronously
    /// </summary>
    public async Task<T> GetOrAddAsync<T>(JsonNode expression, Func<JsonNode, Task<T>> factory)
    {
        var key = ComputeHash(expression);

        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var value = await factory(expression);

        _cache.Set(key, value, _defaultOptions);

        return value;
    }

    /// <summary>
    /// Try to get a cached value
    /// </summary>
    public bool TryGet<T>(JsonNode expression, out T? value)
    {
        var key = ComputeHash(expression);
        return _cache.TryGetValue(key, out value);
    }

    /// <summary>
    /// Clear the cache
    /// </summary>
    public void Clear()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0); // Remove all entries
        }
    }

    /// <summary>
    /// Remove a specific expression from the cache
    /// </summary>
    public void Remove(JsonNode expression)
    {
        var key = ComputeHash(expression);
        _cache.Remove(key);
    }

    /// <summary>
    /// Compute a hash for the expression to use as cache key
    /// </summary>
    private string ComputeHash(JsonNode expression)
    {
        var json = expression.ToJsonString();
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        // Note: MemoryCache doesn't expose hit/miss counters directly
        // For production, consider using a cache with built-in metrics
        return new CacheStatistics
        {
            Message = "Cache statistics require instrumentation. Consider using distributed cache with metrics."
        };
    }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long TotalRequests => Hits + Misses;
    public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests : 0;
    public string? Message { get; set; }
}

/// <summary>
/// Cached expression wrapper
/// </summary>
/// <typeparam name="T">Type of the cached expression data</typeparam>
public class CachedExpression<T>
{
    /// <summary>
    /// The original expression
    /// </summary>
    public JsonNode Expression { get; set; } = null!;

    /// <summary>
    /// The cached data
    /// </summary>
    public T Data { get; set; } = default!;

    /// <summary>
    /// When this was cached
    /// </summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of times this has been accessed
    /// </summary>
    public int AccessCount { get; set; }
}
