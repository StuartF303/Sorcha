// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Configuration for cold tier (WORM) storage.
/// </summary>
public class ColdTierConfiguration
{
    /// <summary>
    /// Provider name: "MongoDB", "InMemory".
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database name for ledger data.
    /// </summary>
    public string DatabaseName { get; set; } = "sorcha_ledger";

    /// <summary>
    /// Collection name for dockets.
    /// </summary>
    public string CollectionName { get; set; } = "dockets";

    /// <summary>
    /// Observability settings for this tier.
    /// </summary>
    public ObservabilityConfiguration? Observability { get; set; }
}
