// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Response model for a wallet link challenge.
/// </summary>
public record WalletLinkChallengeResponse
{
    /// <summary>
    /// Challenge identifier.
    /// </summary>
    public Guid ChallengeId { get; init; }

    /// <summary>
    /// Challenge message that must be signed by the wallet.
    /// </summary>
    public string Challenge { get; init; } = string.Empty;

    /// <summary>
    /// Wallet address being linked.
    /// </summary>
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>
    /// Expected signing algorithm.
    /// </summary>
    public string Algorithm { get; init; } = string.Empty;

    /// <summary>
    /// Challenge expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Challenge status.
    /// </summary>
    public ChallengeStatus Status { get; init; }
}
