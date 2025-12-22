// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for memory pool management
/// </summary>
public class MemPoolConfiguration
{
    /// <summary>
    /// Maximum transactions per memory pool
    /// </summary>
    public int MaxSize { get; set; } = 10_000;

    /// <summary>
    /// Default time-to-live for transactions
    /// </summary>
    public TimeSpan DefaultTTL { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum percentage of high-priority transactions (0.10 = 10%)
    /// </summary>
    public double HighPriorityQuota { get; set; } = 0.10;

    /// <summary>
    /// Interval for cleanup of expired transactions
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
