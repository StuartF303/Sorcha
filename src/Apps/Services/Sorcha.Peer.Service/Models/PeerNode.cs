// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Models;

/// <summary>
/// Represents a peer node in the network
/// </summary>
public class PeerNode
{
    public required string PeerId { get; set; }
    public required string Endpoint { get; set; }
    public string Status { get; set; } = "active";
    public long RegisteredAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Current metrics snapshot
/// </summary>
public class ServiceMetrics
{
    public int ActivePeers { get; set; }
    public long TotalTransactions { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long UptimeSeconds { get; set; }
}
