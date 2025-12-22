// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for docket building triggers and constraints
/// </summary>
public class DocketBuildConfiguration
{
    /// <summary>
    /// Time threshold for building a docket (hybrid trigger)
    /// </summary>
    public TimeSpan TimeThreshold { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Size threshold (transaction count) for building a docket (hybrid trigger)
    /// </summary>
    public int SizeThreshold { get; set; } = 50;

    /// <summary>
    /// Maximum transactions per docket
    /// </summary>
    public int MaxTransactionsPerDocket { get; set; } = 100;

    /// <summary>
    /// Whether to allow dockets with zero transactions
    /// </summary>
    public bool AllowEmptyDockets { get; set; } = false;
}
