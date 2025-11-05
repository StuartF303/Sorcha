// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Represents a peer node in the network
/// </summary>
public class PeerNode : IEquatable<PeerNode>
{
    /// <summary>
    /// Unique identifier for the peer
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Network address of the peer (IP or hostname)
    /// </summary>
    [Required]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Port number for peer communication
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary>
    /// Supported communication protocols
    /// </summary>
    public List<string> SupportedProtocols { get; set; } = new();

    /// <summary>
    /// When this peer was first discovered
    /// </summary>
    public DateTimeOffset FirstSeen { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this peer was last seen online
    /// </summary>
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of consecutive failed connection attempts
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Whether this is a bootstrap node
    /// </summary>
    public bool IsBootstrapNode { get; set; } = false;

    /// <summary>
    /// Peer capabilities
    /// </summary>
    public PeerCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Average latency to this peer in milliseconds
    /// </summary>
    public int AverageLatencyMs { get; set; } = 0;

    public bool Equals(PeerNode? other)
    {
        return other != null && PeerId == other.PeerId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PeerNode);
    }

    public override int GetHashCode()
    {
        return PeerId.GetHashCode();
    }

    public override string ToString()
    {
        return $"Peer {PeerId} at {Address}:{Port}";
    }
}

/// <summary>
/// Capabilities of a peer node
/// </summary>
public class PeerCapabilities
{
    /// <summary>
    /// Supports gRPC streaming connections
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// Supports transaction distribution
    /// </summary>
    public bool SupportsTransactionDistribution { get; set; } = true;

    /// <summary>
    /// Maximum transaction size in bytes
    /// </summary>
    public int MaxTransactionSize { get; set; } = 10 * 1024 * 1024; // 10 MB
}
