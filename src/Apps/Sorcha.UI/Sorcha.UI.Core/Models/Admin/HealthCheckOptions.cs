// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Configuration options for the health check polling service.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Polling interval in milliseconds. Default: 30000 (30 seconds).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 30_000;

    /// <summary>
    /// Timeout for individual health checks in milliseconds. Default: 5000 (5 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// Services to monitor.
    /// </summary>
    public List<ServiceEndpointConfig> Services { get; set; } = [];
}

/// <summary>
/// Configuration for a single service endpoint to monitor.
/// </summary>
public record ServiceEndpointConfig
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
    /// Relative health endpoint path (e.g., "/blueprint/health").
    /// </summary>
    public required string HealthEndpoint { get; init; }
}
