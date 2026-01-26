// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the in-memory verified transaction queue
/// </summary>
public class VerifiedQueueConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "VerifiedQueue";

    /// <summary>
    /// Maximum transactions per register queue (memory limit)
    /// </summary>
    public int MaxTransactionsPerRegister { get; set; } = 10000;

    /// <summary>
    /// Maximum total transactions across all registers (global memory limit)
    /// </summary>
    public int MaxTotalTransactions { get; set; } = 100000;

    /// <summary>
    /// Maximum time a transaction can remain in the queue before expiry
    /// </summary>
    public TimeSpan TransactionTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Interval for running queue cleanup (removing expired transactions)
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Batch size for docket building (max transactions per docket)
    /// </summary>
    public int DefaultBatchSize { get; set; } = 1000;

    /// <summary>
    /// Enable priority ordering (higher priority transactions first)
    /// </summary>
    public bool EnablePriorityOrdering { get; set; } = true;

    /// <summary>
    /// Maximum registers to track in memory
    /// </summary>
    public int MaxRegisters { get; set; } = 1000;

    /// <summary>
    /// Whether to enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
}
