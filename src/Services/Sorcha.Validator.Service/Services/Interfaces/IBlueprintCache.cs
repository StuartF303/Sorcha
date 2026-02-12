// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Caches blueprints for fast lookup during transaction validation.
/// Uses Redis (L2) with optional local in-memory cache (L1).
/// </summary>
public interface IBlueprintCache
{
    /// <summary>
    /// Get a blueprint by ID, fetching from source if not cached
    /// </summary>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Blueprint or null if not found</returns>
    Task<BlueprintModel?> GetBlueprintAsync(
        string blueprintId,
        CancellationToken ct = default);

    /// <summary>
    /// Get a blueprint, using the provided factory to fetch if not cached
    /// </summary>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="factory">Factory to fetch blueprint if not cached</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Blueprint or null if not found</returns>
    Task<BlueprintModel?> GetOrFetchAsync(
        string blueprintId,
        Func<string, CancellationToken, Task<BlueprintModel?>> factory,
        CancellationToken ct = default);

    /// <summary>
    /// Cache a blueprint
    /// </summary>
    /// <param name="blueprint">Blueprint to cache</param>
    /// <param name="ttl">Optional custom TTL</param>
    /// <param name="ct">Cancellation token</param>
    Task SetBlueprintAsync(
        BlueprintModel blueprint,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific action from a blueprint
    /// </summary>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="actionId">Action ID (numeric)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Action or null if not found</returns>
    Task<ActionModel?> GetActionAsync(
        string blueprintId,
        int actionId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a blueprint is cached
    /// </summary>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if cached</returns>
    Task<bool> ExistsAsync(
        string blueprintId,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a blueprint from cache
    /// </summary>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if removed</returns>
    Task<bool> RemoveAsync(
        string blueprintId,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cached blueprints for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of entries invalidated</returns>
    Task<long> InvalidateByRegisterAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    Task<BlueprintCacheStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Warm up the cache with blueprints for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="blueprintIds">Blueprint IDs to warm up</param>
    /// <param name="factory">Factory to fetch blueprints</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of blueprints cached</returns>
    Task<int> WarmupAsync(
        string registerId,
        IEnumerable<string> blueprintIds,
        Func<string, CancellationToken, Task<BlueprintModel?>> factory,
        CancellationToken ct = default);
}

/// <summary>
/// Statistics for the blueprint cache
/// </summary>
public record BlueprintCacheStats
{
    /// <summary>Total cache hits</summary>
    public long TotalHits { get; init; }

    /// <summary>Total cache misses</summary>
    public long TotalMisses { get; init; }

    /// <summary>Total entries in Redis cache (L2)</summary>
    public long RedisCacheEntries { get; init; }

    /// <summary>Total entries in local cache (L1)</summary>
    public int LocalCacheEntries { get; init; }

    /// <summary>L1 cache hits</summary>
    public long LocalCacheHits { get; init; }

    /// <summary>L2 cache hits</summary>
    public long RedisCacheHits { get; init; }

    /// <summary>Cache hit ratio</summary>
    public double HitRatio => TotalHits + TotalMisses > 0
        ? (double)TotalHits / (TotalHits + TotalMisses)
        : 0;

    /// <summary>Average lookup latency in milliseconds</summary>
    public double AverageLatencyMs { get; init; }
}
