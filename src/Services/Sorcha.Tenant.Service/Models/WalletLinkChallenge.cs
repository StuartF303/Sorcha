// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Temporary record for wallet verification flow.
/// Stored in public schema for cross-org challenge lookup.
/// Challenges expire after 5 minutes.
/// </summary>
public class WalletLinkChallenge
{
    /// <summary>
    /// Unique challenge identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Participant initiating the wallet link.
    /// </summary>
    public Guid ParticipantId { get; set; }

    /// <summary>
    /// Wallet address being linked.
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Challenge message that must be signed by the wallet.
    /// Contains participant ID, address, timestamp, and nonce.
    /// </summary>
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// Challenge expiration timestamp (UTC).
    /// Default is 5 minutes from creation.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Challenge status (Pending, Completed, Expired, Failed).
    /// </summary>
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;

    /// <summary>
    /// Challenge creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Verification completion timestamp (UTC). Null if not completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property to the participant.
    /// </summary>
    public ParticipantIdentity? Participant { get; set; }
}
