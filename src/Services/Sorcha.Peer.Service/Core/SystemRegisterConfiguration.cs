// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration for system register synchronization
/// </summary>
public class SystemRegisterConfiguration
{
    /// <summary>
    /// Periodic sync interval in minutes (default: 5 minutes per FR-032)
    /// </summary>
    [Range(1, 60)]
    public int PeriodicSyncIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Heartbeat interval in seconds (default: 30 seconds per FR-036)
    /// </summary>
    [Range(10, 120)]
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Heartbeat timeout in seconds (default: 30 seconds per FR-036)
    /// </summary>
    [Range(10, 120)]
    public int HeartbeatTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts before failover to next hub node
    /// </summary>
    [Range(1, 20)]
    public int MaxRetryAttempts { get; set; } = 10;

    /// <summary>
    /// Maximum missed heartbeats before triggering failover (default: 2 = 60 seconds total)
    /// </summary>
    [Range(1, 10)]
    public int MaxMissedHeartbeats { get; set; } = 2;

    /// <summary>
    /// Connection timeout per attempt in seconds
    /// </summary>
    [Range(10, 120)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
