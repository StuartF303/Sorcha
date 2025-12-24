// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Synchronization checkpoint for tracking incremental sync progress
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
    /// Last synchronized system register version
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
    /// Total number of blueprints in local replica
    /// </summary>
    [Required]
    [JsonPropertyName("totalBlueprints")]
    public int TotalBlueprints { get; set; } = 0;

    /// <summary>
    /// Hub node this checkpoint is for
    /// </summary>
    [Required]
    [MaxLength(64)]
    [JsonPropertyName("centralNodeId")]
    public string HubNodeId { get; set; } = string.Empty;

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
    /// <param name="blueprintCount">Total number of blueprints now in local replica</param>
    public void UpdateAfterSync(long newVersion, int blueprintCount)
    {
        CurrentVersion = newVersion;
        TotalBlueprints = blueprintCount;
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
