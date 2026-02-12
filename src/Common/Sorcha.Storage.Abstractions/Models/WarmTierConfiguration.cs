// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Configuration for warm tier (operational) storage.
/// </summary>
public class WarmTierConfiguration
{
    /// <summary>
    /// Relational database configuration (PostgreSQL via EF Core).
    /// </summary>
    public RelationalConfiguration? Relational { get; set; }

    /// <summary>
    /// Document database configuration (MongoDB).
    /// </summary>
    public DocumentConfiguration? Documents { get; set; }

    /// <summary>
    /// Observability settings for this tier.
    /// </summary>
    public ObservabilityConfiguration? Observability { get; set; }
}

/// <summary>
/// Relational database configuration.
/// </summary>
public class RelationalConfiguration
{
    /// <summary>
    /// Provider name: "PostgreSQL", "InMemory".
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Enable sensitive data logging (development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
}

/// <summary>
/// Document database configuration.
/// </summary>
public class DocumentConfiguration
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
    /// Database name.
    /// </summary>
    public string DatabaseName { get; set; } = "sorcha";
}
