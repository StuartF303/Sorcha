// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request model for initiating a wallet link challenge.
/// </summary>
public record InitiateWalletLinkRequest
{
    /// <summary>
    /// Wallet address to link (e.g., base58 encoded, hex).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>
    /// Signing algorithm used by the wallet.
    /// Supported values: ED25519, P-256, RSA-4096.
    /// </summary>
    [Required]
    [RegularExpression("^(ED25519|P-256|RSA-4096)$", ErrorMessage = "Algorithm must be ED25519, P-256, or RSA-4096")]
    public string Algorithm { get; init; } = string.Empty;
}
