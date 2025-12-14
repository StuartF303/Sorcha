// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Heartbeat message sent from peer to central node
/// </summary>
public class HeartbeatMessage
{
    /// <summary>
    /// Unique identifier of peer sending heartbeat
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp (milliseconds) when heartbeat was sent
    /// </summary>
    [Required]
    public long Timestamp { get; set; }

    /// <summary>
    /// Monotonically increasing sequence number
    /// </summary>
    [Required]
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Last known system register version
    /// </summary>
    [Required]
    public long LastSyncVersion { get; set; }

    /// <summary>
    /// Number of active peer connections (optional metric)
    /// </summary>
    public int? ActiveConnections { get; set; }

    /// <summary>
    /// CPU usage percentage (optional metric)
    /// </summary>
    [Range(0, 100)]
    public double? CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage in megabytes (optional metric)
    /// </summary>
    public double? MemoryUsageMb { get; set; }

    /// <summary>
    /// Type of node sending heartbeat
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NodeType { get; set; } = "Peer";

    /// <summary>
    /// Creates a heartbeat message with current timestamp
    /// </summary>
    /// <param name="peerId">Unique identifier of the peer</param>
    /// <param name="sequenceNumber">Sequence number for this heartbeat</param>
    /// <param name="lastSyncVersion">Last synchronized system register version</param>
    /// <returns>A new HeartbeatMessage instance</returns>
    public static HeartbeatMessage Create(string peerId, long sequenceNumber, long lastSyncVersion)
    {
        if (string.IsNullOrWhiteSpace(peerId))
            throw new ArgumentException("Peer ID cannot be null or empty", nameof(peerId));

        return new HeartbeatMessage
        {
            PeerId = peerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SequenceNumber = sequenceNumber,
            LastSyncVersion = lastSyncVersion,
            NodeType = "Peer"
        };
    }
}
