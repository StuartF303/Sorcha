// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request model for verifying a wallet link challenge.
/// </summary>
public record VerifyWalletLinkRequest
{
    /// <summary>
    /// Signature of the challenge message, encoded as base64.
    /// The challenge message must be signed with the wallet's private key.
    /// </summary>
    [Required]
    public string Signature { get; init; } = string.Empty;

    /// <summary>
    /// Public key bytes, encoded as base64.
    /// Used to verify the signature and stored for future verification.
    /// </summary>
    [Required]
    public string PublicKey { get; init; } = string.Empty;
}
