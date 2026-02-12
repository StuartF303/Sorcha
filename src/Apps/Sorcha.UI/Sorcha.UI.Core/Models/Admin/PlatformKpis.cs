// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Aggregated platform Key Performance Indicators (KPIs) for the admin dashboard.
/// </summary>
public record PlatformKpis
{
    /// <summary>
    /// Total number of active organizations on the platform.
    /// </summary>
    public int TotalOrganizations { get; init; }

    /// <summary>
    /// Total number of users across all organizations.
    /// </summary>
    public int TotalUsers { get; init; }

    /// <summary>
    /// Number of services currently in healthy state.
    /// </summary>
    public int HealthyServices { get; init; }

    /// <summary>
    /// Total number of monitored services.
    /// </summary>
    public int TotalServices { get; init; }

    /// <summary>
    /// Timestamp when the KPIs were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Calculates the percentage of healthy services.
    /// </summary>
    public double HealthyServicesPercentage =>
        TotalServices > 0 ? (double)HealthyServices / TotalServices * 100 : 0;
}
