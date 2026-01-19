// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Represents the health status of an individual platform service.
/// </summary>
public record ServiceHealthStatus
{
    /// <summary>
    /// Display name of the service (e.g., "Blueprint Service").
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Internal identifier key (e.g., "blueprint").
    /// </summary>
    public required string ServiceKey { get; init; }

    /// <summary>
    /// Health check endpoint URL.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Current health status.
    /// </summary>
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;

    /// <summary>
    /// Timestamp of the last successful health check.
    /// </summary>
    public DateTimeOffset? LastCheckTime { get; set; }

    /// <summary>
    /// Duration of the last health check request.
    /// </summary>
    public TimeSpan? LastCheckDuration { get; set; }

    /// <summary>
    /// Error message if the service is unhealthy.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Service version reported by the health endpoint.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Service uptime as reported by the health endpoint.
    /// </summary>
    public string? Uptime { get; set; }
}
