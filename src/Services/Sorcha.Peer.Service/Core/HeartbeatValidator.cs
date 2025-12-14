// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validator for heartbeat timeout and failover logic
/// </summary>
public static class HeartbeatValidator
{
    /// <summary>
    /// Heartbeat interval in seconds (default: 30)
    /// </summary>
    public const int HeartbeatIntervalSeconds = PeerServiceConstants.HeartbeatIntervalSeconds;

    /// <summary>
    /// Heartbeat timeout in seconds (default: 30)
    /// </summary>
    public const int HeartbeatTimeoutSeconds = PeerServiceConstants.HeartbeatTimeoutSeconds;

    /// <summary>
    /// Maximum missed heartbeats before failover (default: 2 = 60 seconds total)
    /// </summary>
    public const int MaxMissedHeartbeats = PeerServiceConstants.MaxMissedHeartbeats;

    /// <summary>
    /// Checks if a heartbeat is timed out based on last heartbeat timestamp
    /// </summary>
    /// <param name="lastHeartbeat">Last heartbeat timestamp (UTC)</param>
    /// <returns>True if heartbeat is timed out (>30 seconds elapsed)</returns>
    public static bool IsHeartbeatTimedOut(DateTime lastHeartbeat)
    {
        var elapsed = DateTime.UtcNow - lastHeartbeat;
        return elapsed.TotalSeconds > HeartbeatTimeoutSeconds;
    }

    /// <summary>
    /// Checks if failover should be triggered based on missed heartbeat count
    /// </summary>
    /// <param name="missedHeartbeats">Number of consecutive missed heartbeats</param>
    /// <returns>True if failover should occur (>=2 missed)</returns>
    public static bool ShouldFailover(int missedHeartbeats)
    {
        return missedHeartbeats >= MaxMissedHeartbeats;
    }

    /// <summary>
    /// Calculates the next heartbeat time based on current time
    /// </summary>
    /// <returns>DateTime when next heartbeat should be sent</returns>
    public static DateTime CalculateNextHeartbeatTime()
    {
        return DateTime.UtcNow.AddSeconds(HeartbeatIntervalSeconds);
    }
}
