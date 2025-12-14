// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validator for periodic sync interval and timing
/// </summary>
public static class SyncValidator
{
    /// <summary>
    /// Periodic sync interval in minutes (default: 5)
    /// </summary>
    public const int PeriodicSyncIntervalMinutes = PeerServiceConstants.PeriodicSyncIntervalMinutes;

    /// <summary>
    /// Periodic sync interval in seconds
    /// </summary>
    public const int PeriodicSyncIntervalSeconds = PeriodicSyncIntervalMinutes * 60;

    /// <summary>
    /// Calculates the next sync time based on last sync time
    /// </summary>
    /// <param name="lastSyncTime">Last sync timestamp (UTC)</param>
    /// <returns>DateTime when next sync should occur</returns>
    public static DateTime CalculateNextSyncTime(DateTime lastSyncTime)
    {
        return lastSyncTime.AddMinutes(PeriodicSyncIntervalMinutes);
    }

    /// <summary>
    /// Checks if sync is due based on next sync timestamp
    /// </summary>
    /// <param name="nextSyncDue">Next sync due timestamp (UTC)</param>
    /// <returns>True if current time is past next sync due time</returns>
    public static bool IsSyncDue(DateTime nextSyncDue)
    {
        return DateTime.UtcNow >= nextSyncDue;
    }

    /// <summary>
    /// Calculates time remaining until next sync
    /// </summary>
    /// <param name="nextSyncDue">Next sync due timestamp (UTC)</param>
    /// <returns>TimeSpan until next sync, or TimeSpan.Zero if overdue</returns>
    public static TimeSpan TimeUntilNextSync(DateTime nextSyncDue)
    {
        var remaining = nextSyncDue - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
