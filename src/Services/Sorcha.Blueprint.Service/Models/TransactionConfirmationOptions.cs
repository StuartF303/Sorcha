// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Configuration for transaction confirmation polling behavior.
/// Bind from appsettings.json section "TransactionConfirmation".
/// </summary>
public class TransactionConfirmationOptions
{
    public const string SectionName = "TransactionConfirmation";

    /// <summary>
    /// Maximum time to wait for transaction confirmation (seconds). Default: 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Interval between polling attempts (seconds). Default: 1.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 1;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);
}
