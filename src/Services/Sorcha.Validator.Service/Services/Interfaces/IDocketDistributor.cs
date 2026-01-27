// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Distributes dockets to peer validators and submits confirmed dockets to Register Service.
/// Handles:
/// - Broadcasting proposed dockets to peers for consensus voting
/// - Broadcasting confirmed dockets after consensus achievement
/// - Submitting confirmed dockets to Register Service for persistence
/// </summary>
public interface IDocketDistributor
{
    /// <summary>
    /// Broadcasts a proposed docket to peer validators for consensus voting.
    /// </summary>
    /// <param name="docket">The proposed docket</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of peers the docket was broadcast to</returns>
    Task<int> BroadcastProposedDocketAsync(
        Docket docket,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a confirmed docket to all peer validators.
    /// </summary>
    /// <param name="docket">The confirmed docket with consensus signatures</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of peers the docket was broadcast to</returns>
    Task<int> BroadcastConfirmedDocketAsync(
        Docket docket,
        CancellationToken ct = default);

    /// <summary>
    /// Submits a confirmed docket to the Register Service for persistence.
    /// </summary>
    /// <param name="docket">The confirmed docket</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitToRegisterServiceAsync(
        Docket docket,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics about docket distribution.
    /// </summary>
    /// <returns>Distribution statistics</returns>
    DocketDistributorStats GetStats();
}

/// <summary>
/// Statistics for docket distribution operations.
/// </summary>
public record DocketDistributorStats
{
    /// <summary>Total proposed dockets broadcast</summary>
    public long TotalProposedBroadcasts { get; init; }

    /// <summary>Total confirmed dockets broadcast</summary>
    public long TotalConfirmedBroadcasts { get; init; }

    /// <summary>Total dockets submitted to Register Service</summary>
    public long TotalRegisterSubmissions { get; init; }

    /// <summary>Total failed Register Service submissions</summary>
    public long FailedRegisterSubmissions { get; init; }

    /// <summary>Average broadcast time in milliseconds</summary>
    public double AverageBroadcastTimeMs { get; init; }

    /// <summary>Last broadcast timestamp</summary>
    public DateTimeOffset? LastBroadcastAt { get; init; }
}
