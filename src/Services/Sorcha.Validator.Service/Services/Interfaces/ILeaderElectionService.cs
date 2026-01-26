// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Manages leader election for docket building in a multi-validator network.
/// Only the leader initiates docket builds; other validators act as confirmers.
/// </summary>
public interface ILeaderElectionService
{
    /// <summary>
    /// Current leader validator ID (null if no leader elected)
    /// </summary>
    string? CurrentLeaderId { get; }

    /// <summary>
    /// Whether this validator is the current leader
    /// </summary>
    bool IsLeader { get; }

    /// <summary>
    /// Current election term number
    /// </summary>
    long CurrentTerm { get; }

    /// <summary>
    /// Timestamp of last heartbeat received from leader (for followers)
    /// </summary>
    DateTimeOffset? LastHeartbeatReceived { get; }

    /// <summary>
    /// Event raised when leadership changes
    /// </summary>
    event EventHandler<LeaderChangedEventArgs>? LeaderChanged;

    /// <summary>
    /// Start participating in leader election for a register
    /// </summary>
    /// <param name="registerId">Register to participate in</param>
    /// <param name="ct">Cancellation token</param>
    Task StartAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Stop participating in leader election
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Send heartbeat to followers (leader only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task SendHeartbeatAsync(CancellationToken ct = default);

    /// <summary>
    /// Process heartbeat received from leader (followers)
    /// </summary>
    /// <param name="leaderId">Leader validator ID</param>
    /// <param name="term">Leader's term number</param>
    /// <param name="latestDocketNumber">Leader's latest docket number</param>
    /// <param name="ct">Cancellation token</param>
    Task ProcessHeartbeatAsync(
        string leaderId,
        long term,
        long latestDocketNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Trigger a new election (on leader failure)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task TriggerElectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the next leader in rotation order
    /// </summary>
    /// <param name="currentLeaderId">Current leader ID</param>
    /// <returns>Next leader ID in rotation</returns>
    Task<string?> GetNextLeaderAsync(string? currentLeaderId);
}

/// <summary>
/// Event arguments for leader change events
/// </summary>
public class LeaderChangedEventArgs : EventArgs
{
    /// <summary>Previous leader ID (null if none)</summary>
    public required string? PreviousLeaderId { get; init; }

    /// <summary>New leader ID (null if election in progress)</summary>
    public required string? NewLeaderId { get; init; }

    /// <summary>New term number</summary>
    public required long Term { get; init; }

    /// <summary>Reason for the leadership change</summary>
    public required LeaderChangeReason Reason { get; init; }

    /// <summary>Timestamp of the change</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Reasons for leadership change
/// </summary>
public enum LeaderChangeReason
{
    /// <summary>Initial election when service starts</summary>
    InitialElection,

    /// <summary>Term expired (time-based rotation)</summary>
    TermExpired,

    /// <summary>Leader failed to send heartbeats</summary>
    LeaderTimeout,

    /// <summary>Leader voluntarily resigned</summary>
    LeaderResigned,

    /// <summary>Received heartbeat with higher term</summary>
    HigherTermReceived,

    /// <summary>Validator list changed</summary>
    ValidatorListChanged
}
