// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for genesis config caching
/// </summary>
public class GenesisConfigCacheConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "GenesisConfigCache";

    /// <summary>
    /// Redis key prefix for cached genesis configurations
    /// </summary>
    public string KeyPrefix { get; set; } = "sorcha:validator:genesis:";

    /// <summary>
    /// Default TTL for cached genesis configurations
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How often to check if cached config is stale
    /// </summary>
    public TimeSpan StaleCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum retries for cache operations
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Whether to enable local in-memory cache as L1
    /// </summary>
    public bool EnableLocalCache { get; set; } = true;

    /// <summary>
    /// TTL for local in-memory cache (L1)
    /// </summary>
    public TimeSpan LocalCacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum entries in local cache
    /// </summary>
    public int LocalCacheMaxEntries { get; set; } = 50;

    /// <summary>
    /// Register service base URL for fetching genesis data
    /// </summary>
    public string? RegisterServiceUrl { get; set; }

    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
