// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Represents the health status of a monitored service.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health check has not been performed or timed out.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Service is fully operational.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Service is partially operational with some issues.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Service is not operational or unreachable.
    /// </summary>
    Unhealthy = 3
}
