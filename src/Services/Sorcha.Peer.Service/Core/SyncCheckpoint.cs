// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Synchronization checkpoint for tracking incremental sync progress per register
/// </summary>
public class SyncCheckpoint
{
    /// <summary>
    /// Peer that owns this checkpoint
    /// </summary>
    [Required]
    [MaxLength(64)]
    [JsonPropertyName("peerId")]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Register this checkpoint tracks
    /// </summary>
    [Required]
    [MaxLength(255)]
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Last synchronized version for this register
    /// </summary>
    [Required]
    [JsonPropertyName("currentVersion")]
    public long CurrentVersion { get; set; } = 0;

    /// <summary>
    /// Unix timestamp (milliseconds) of last successful sync
    /// </summary>
    [Required]
    [JsonPropertyName("lastSyncTime")]
    public long LastSyncTime { get; set; }

    /// <summary>
    /// Total number of items in local replica for this register
    /// </summary>
    [Required]
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; } = 0;

    /// <summary>
    /// Peer ID of the source peer used for syncing (can change during failover)
    /// </summary>
    [MaxLength(64)]
    [JsonPropertyName("sourcePeerId")]
    public string? SourcePeerId { get; set; }

    /// <summary>
    /// When next periodic sync is due (UTC)
    /// </summary>
    [Required]
    [JsonPropertyName("nextSyncDue")]
    public DateTime NextSyncDue { get; set; } = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);

    /// <summary>
    /// Updates checkpoint after successful sync
    /// </summary>
    /// <param name="newVersion">New version number synchronized</param>
    /// <param name="itemCount">Total number of items now in local replica</param>
    public void UpdateAfterSync(long newVersion, int itemCount)
    {
        CurrentVersion = newVersion;
        TotalItems = itemCount;
        LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);
    }

    /// <summary>
    /// Checks if periodic sync is due
    /// </summary>
    /// <returns>True if current time is past NextSyncDue</returns>
    public bool IsSyncDue()
    {
        return DateTime.UtcNow >= NextSyncDue;
    }
}
