// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Constants for peer service central node connection and system register
/// </summary>
public static class PeerServiceConstants
{
    /// <summary>
    /// Well-known system register identifier (00000000-0000-0000-0000-000000000000)
    /// </summary>
    public static readonly Guid SystemRegisterId = Guid.Empty;

    /// <summary>
    /// MongoDB collection name for system register blueprints
    /// </summary>
    public const string SystemRegisterCollectionName = "sorcha_system_register_blueprints";

    /// <summary>
    /// Valid central node hostnames
    /// </summary>
    public static readonly string[] CentralNodeHostnames = new[]
    {
        "n0.sorcha.dev",
        "n1.sorcha.dev",
        "n2.sorcha.dev"
    };

    /// <summary>
    /// Heartbeat interval in seconds (FR-036)
    /// </summary>
    public const int HeartbeatIntervalSeconds = 30;

    /// <summary>
    /// Heartbeat timeout in seconds (FR-036)
    /// </summary>
    public const int HeartbeatTimeoutSeconds = 30;

    /// <summary>
    /// Maximum missed heartbeats before failover (2 = 60 seconds total)
    /// </summary>
    public const int MaxMissedHeartbeats = 2;

    /// <summary>
    /// Periodic sync interval in minutes (FR-032)
    /// </summary>
    public const int PeriodicSyncIntervalMinutes = 5;

    /// <summary>
    /// Connection retry initial delay in seconds
    /// </summary>
    public const int RetryInitialDelaySeconds = 1;

    /// <summary>
    /// Connection retry multiplier (exponential backoff)
    /// </summary>
    public const double RetryMultiplier = 2.0;

    /// <summary>
    /// Connection retry max delay in seconds (cap at 60s)
    /// </summary>
    public const int RetryMaxDelaySeconds = 60;

    /// <summary>
    /// Maximum retry attempts before giving up and trying next central node
    /// </summary>
    public const int MaxRetryAttempts = 10;

    /// <summary>
    /// Connection timeout per attempt in seconds
    /// </summary>
    public const int ConnectionTimeoutSeconds = 30;

    /// <summary>
    /// Push notification delivery target (80% of peers within 30s, SC-016)
    /// </summary>
    public const double PushNotificationTargetPercent = 0.80;

    /// <summary>
    /// Push notification delivery timeout in seconds (SC-016)
    /// </summary>
    public const int PushNotificationTimeoutSeconds = 30;

    /// <summary>
    /// Expected retry backoff sequence (in seconds): 1, 2, 4, 8, 16, 32, 60, 60, 60, 60
    /// </summary>
    public static readonly TimeSpan[] RetryBackoffSequence = new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16),
        TimeSpan.FromSeconds(32),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60)
    };
}
