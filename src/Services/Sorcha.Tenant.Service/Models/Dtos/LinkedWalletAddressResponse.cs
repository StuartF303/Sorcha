// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Response model for a linked wallet address.
/// </summary>
public record LinkedWalletAddressResponse
{
    /// <summary>
    /// Link identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Wallet address string.
    /// </summary>
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>
    /// Signing algorithm (ED25519, P-256, RSA-4096).
    /// </summary>
    public string Algorithm { get; init; } = string.Empty;

    /// <summary>
    /// Link status (Active, Revoked).
    /// </summary>
    public WalletLinkStatus Status { get; init; }

    /// <summary>
    /// Timestamp when the wallet was linked.
    /// </summary>
    public DateTimeOffset LinkedAt { get; init; }

    /// <summary>
    /// Timestamp when the link was revoked (if applicable).
    /// </summary>
    public DateTimeOffset? RevokedAt { get; init; }
}
