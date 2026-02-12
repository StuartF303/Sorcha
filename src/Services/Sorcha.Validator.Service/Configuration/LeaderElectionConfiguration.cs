// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the local leader election service
/// </summary>
public class LeaderElectionConfiguration
{
    /// <summary>
    /// Default heartbeat interval if not specified in genesis config
    /// </summary>
    public TimeSpan DefaultHeartbeatInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Default leader timeout if not specified in genesis config
    /// </summary>
    public TimeSpan DefaultLeaderTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default term duration for rotating election
    /// </summary>
    public TimeSpan DefaultTermDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Grace period before triggering election on startup
    /// </summary>
    public TimeSpan StartupGracePeriod { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Number of missed heartbeats before considering leader failed
    /// </summary>
    public int MissedHeartbeatsThreshold { get; set; } = 3;

    /// <summary>
    /// Jitter range (ms) to add to heartbeat interval to prevent thundering herd
    /// </summary>
    public int HeartbeatJitterMs { get; set; } = 100;

    /// <summary>
    /// Whether to log detailed election state changes
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
