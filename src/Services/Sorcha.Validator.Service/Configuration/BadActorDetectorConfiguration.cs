// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the bad actor detector service.
/// </summary>
public class BadActorDetectorConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "BadActorDetector";

    /// <summary>
    /// Time window for counting rejections (default: 1 hour)
    /// </summary>
    public TimeSpan RejectionCountWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Number of rejections in window to trigger warning (default: 5)
    /// </summary>
    public int WarningThreshold { get; set; } = 5;

    /// <summary>
    /// Number of rejections in window to trigger high severity (default: 10)
    /// </summary>
    public int HighSeverityThreshold { get; set; } = 10;

    /// <summary>
    /// Number of rejections in window to trigger critical severity (default: 20)
    /// </summary>
    public int CriticalThreshold { get; set; } = 20;

    /// <summary>
    /// How long to retain incident records (default: 7 days)
    /// </summary>
    public TimeSpan IncidentRetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Maximum incidents to store per validator (default: 1000)
    /// </summary>
    public int MaxIncidentsPerValidator { get; set; } = 1000;

    /// <summary>
    /// Whether to enable real-time alerting for critical incidents (default: true)
    /// </summary>
    public bool EnableCriticalAlerts { get; set; } = true;

    /// <summary>
    /// Cleanup interval for expired incidents (default: 1 hour)
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
