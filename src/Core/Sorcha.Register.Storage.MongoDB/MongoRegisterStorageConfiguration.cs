// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Storage.MongoDB;

/// <summary>
/// Configuration for MongoDB Register storage.
/// </summary>
public class MongoRegisterStorageConfiguration
{
    /// <summary>
    /// MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Database name for register storage.
    /// </summary>
    public string DatabaseName { get; set; } = "sorcha_register";

    /// <summary>
    /// Collection name for registers.
    /// </summary>
    public string RegisterCollectionName { get; set; } = "registers";

    /// <summary>
    /// Collection name for transactions.
    /// </summary>
    public string TransactionCollectionName { get; set; } = "transactions";

    /// <summary>
    /// Collection name for dockets.
    /// </summary>
    public string DocketCollectionName { get; set; } = "dockets";

    /// <summary>
    /// Maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to create indexes on startup.
    /// </summary>
    public bool CreateIndexesOnStartup { get; set; } = true;
}
