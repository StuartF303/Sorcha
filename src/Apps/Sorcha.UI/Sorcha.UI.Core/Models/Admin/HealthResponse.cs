// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Health check response from Blueprint API
/// </summary>
public class BlueprintHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
    public BlueprintMetricsData? Metrics { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Metrics data from Blueprint API
/// </summary>
public class BlueprintMetricsData
{
    public int TotalBlueprints { get; set; }
    public int PublishedVersions { get; set; }
}

/// <summary>
/// Health check response from Peer Service
/// </summary>
public class PeerHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
    public PeerMetricsData? Metrics { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Metrics data from Peer Service
/// </summary>
public class PeerMetricsData
{
    public int ActivePeers { get; set; }
    public long TotalTransactions { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
}

/// <summary>
/// Peer information from Peer Service
/// </summary>
public class PeerNodeInfo
{
    public string PeerId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long RegisteredAt { get; set; }
}
