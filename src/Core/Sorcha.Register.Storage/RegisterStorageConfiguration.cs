// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Storage.Abstractions.Caching;

namespace Sorcha.Register.Storage;

/// <summary>
/// Configuration for Register Service storage layer.
/// </summary>
public class RegisterStorageConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RegisterStorage";

    /// <summary>
    /// Redis connection string for hot-tier cache.
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// MongoDB connection string for WORM store.
    /// </summary>
    public string MongoConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// MongoDB database name.
    /// </summary>
    public string MongoDatabaseName { get; set; } = "sorcha_register";

    /// <summary>
    /// Collection name for dockets in MongoDB.
    /// </summary>
    public string DocketCollectionName { get; set; } = "dockets";

    /// <summary>
    /// Collection name for transactions in MongoDB.
    /// </summary>
    public string TransactionCollectionName { get; set; } = "transactions";

    /// <summary>
    /// Collection name for registers in MongoDB.
    /// </summary>
    public string RegisterCollectionName { get; set; } = "registers";

    /// <summary>
    /// Verified cache configuration for dockets.
    /// </summary>
    public VerifiedCacheConfiguration DocketCacheConfiguration { get; set; } = new()
    {
        KeyPrefix = "register:docket:",
        CacheTtlSeconds = 86400, // 24 hours - dockets are immutable
        EnableHashVerification = true,
        WarmingBatchSize = 1000,
        StartupStrategy = CacheStartupStrategy.Progressive,
        BlockingThreshold = 100
    };

    /// <summary>
    /// Verified cache configuration for transactions.
    /// </summary>
    public VerifiedCacheConfiguration TransactionCacheConfiguration { get; set; } = new()
    {
        KeyPrefix = "register:tx:",
        CacheTtlSeconds = 3600, // 1 hour
        EnableHashVerification = true,
        WarmingBatchSize = 500,
        StartupStrategy = CacheStartupStrategy.Progressive,
        BlockingThreshold = 50
    };

    /// <summary>
    /// Whether to use in-memory storage (for testing/development).
    /// </summary>
    public bool UseInMemoryStorage { get; set; } = false;

    /// <summary>
    /// Whether to enable cache warming on startup.
    /// </summary>
    public bool EnableCacheWarming { get; set; } = true;
}
