// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// In-memory cache for system register blueprints on peer nodes
/// </summary>
/// <remarks>
/// Provides thread-safe caching of blueprints synchronized from central nodes.
/// Used to serve local queries without requiring constant connection to central node.
/// Supports full cache updates (initial sync) and incremental updates (periodic sync).
/// </remarks>
public class SystemRegisterCache
{
    private readonly ILogger<SystemRegisterCache> _logger;
    private readonly ConcurrentDictionary<string, CachedBlueprint> _blueprints;
    private readonly object _cacheLock = new();
    private long _currentVersion = 0;
    private DateTime _lastUpdateTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRegisterCache"/> class
    /// </summary>
    public SystemRegisterCache(ILogger<SystemRegisterCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blueprints = new ConcurrentDictionary<string, CachedBlueprint>();
    }

    /// <summary>
    /// Updates the cache with a collection of blueprints (used for full sync)
    /// </summary>
    /// <param name="blueprints">Blueprints to store in cache</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateCacheAsync(IEnumerable<CachedBlueprint> blueprints, CancellationToken cancellationToken = default)
    {
        try
        {
            var blueprintList = blueprints.ToList();
            _logger.LogInformation("Updating system register cache with {Count} blueprints", blueprintList.Count);

            lock (_cacheLock)
            {
                // Clear existing cache
                _blueprints.Clear();

                // Add all blueprints
                foreach (var blueprint in blueprintList)
                {
                    _blueprints[blueprint.BlueprintId] = blueprint;

                    // Track highest version
                    if (blueprint.Version > _currentVersion)
                    {
                        _currentVersion = blueprint.Version;
                    }
                }

                _lastUpdateTime = DateTime.UtcNow;
            }

            _logger.LogInformation("Cache updated successfully. Current version: {Version}, Total blueprints: {Count}",
                _currentVersion, _blueprints.Count);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update system register cache");
            throw;
        }
    }

    /// <summary>
    /// Adds or updates a single blueprint in the cache (used for incremental sync)
    /// </summary>
    /// <param name="blueprint">Blueprint to add or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddOrUpdateBlueprintAsync(CachedBlueprint blueprint, CancellationToken cancellationToken = default)
    {
        try
        {
            _blueprints[blueprint.BlueprintId] = blueprint;

            // Update version tracking
            lock (_cacheLock)
            {
                if (blueprint.Version > _currentVersion)
                {
                    _currentVersion = blueprint.Version;
                }
                _lastUpdateTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Blueprint {BlueprintId} added/updated in cache (version {Version})",
                blueprint.BlueprintId, blueprint.Version);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update blueprint {BlueprintId} in cache", blueprint.BlueprintId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a blueprint by ID from the cache
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached blueprint or null if not found</returns>
    public Task<CachedBlueprint?> GetBlueprintByIdAsync(string blueprintId, CancellationToken cancellationToken = default)
    {
        if (_blueprints.TryGetValue(blueprintId, out var blueprint))
        {
            _logger.LogDebug("Retrieved blueprint {BlueprintId} from cache", blueprintId);
            return Task.FromResult<CachedBlueprint?>(blueprint);
        }

        _logger.LogDebug("Blueprint {BlueprintId} not found in cache", blueprintId);
        return Task.FromResult<CachedBlueprint?>(null);
    }

    /// <summary>
    /// Retrieves all active blueprints from the cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all cached blueprints</returns>
    public Task<List<CachedBlueprint>> GetAllBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        var blueprints = _blueprints.Values
            .Where(b => b.IsActive)
            .OrderBy(b => b.Version)
            .ToList();

        _logger.LogDebug("Retrieved {Count} active blueprints from cache", blueprints.Count);
        return Task.FromResult(blueprints);
    }

    /// <summary>
    /// Clears all blueprints from the cache
    /// </summary>
    public void Clear()
    {
        lock (_cacheLock)
        {
            _blueprints.Clear();
            _currentVersion = 0;
            _lastUpdateTime = DateTime.MinValue;
        }

        _logger.LogInformation("System register cache cleared");
    }

    /// <summary>
    /// Gets the current version of the cached system register
    /// </summary>
    public long GetCurrentVersion()
    {
        lock (_cacheLock)
        {
            return _currentVersion;
        }
    }

    /// <summary>
    /// Gets the total count of cached blueprints
    /// </summary>
    public int GetBlueprintCount()
    {
        return _blueprints.Count;
    }

    /// <summary>
    /// Gets the timestamp of the last cache update
    /// </summary>
    public DateTime GetLastUpdateTime()
    {
        lock (_cacheLock)
        {
            return _lastUpdateTime;
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_cacheLock)
        {
            return new CacheStatistics
            {
                TotalBlueprints = _blueprints.Count,
                ActiveBlueprints = _blueprints.Values.Count(b => b.IsActive),
                CurrentVersion = _currentVersion,
                LastUpdateTime = _lastUpdateTime,
                CacheSizeBytes = EstimateCacheSize()
            };
        }
    }

    /// <summary>
    /// Estimates the total size of cached data in bytes
    /// </summary>
    private long EstimateCacheSize()
    {
        // Rough estimation: sum of all document sizes
        return _blueprints.Values.Sum(b => b.Document?.Length ?? 0);
    }
}

/// <summary>
/// Represents a cached blueprint entry
/// </summary>
public class CachedBlueprint
{
    /// <summary>
    /// Unique blueprint identifier
    /// </summary>
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// System register version number
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Blueprint JSON document (as byte array for efficient storage)
    /// </summary>
    public byte[]? Document { get; set; }

    /// <summary>
    /// Timestamp when blueprint was published
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Identity of publisher
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Whether blueprint is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// SHA-256 checksum for integrity verification
    /// </summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Statistics about the cache
/// </summary>
public class CacheStatistics
{
    public int TotalBlueprints { get; set; }
    public int ActiveBlueprints { get; set; }
    public long CurrentVersion { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public long CacheSizeBytes { get; set; }
}
