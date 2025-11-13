// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.ApiGateway.Models;

/// <summary>
/// Aggregated health response from all services
/// </summary>
public class AggregatedHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, ServiceHealth> Services { get; set; } = new();
}

/// <summary>
/// Health status of an individual service
/// </summary>
public class ServiceHealth
{
    public string Status { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// System-wide statistics
/// </summary>
public class SystemStatistics
{
    public int TotalServices { get; set; }
    public int HealthyServices { get; set; }
    public int UnhealthyServices { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> ServiceMetrics { get; set; } = new();
}
