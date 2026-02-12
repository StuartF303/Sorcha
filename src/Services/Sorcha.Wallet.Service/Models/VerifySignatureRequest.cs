// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for verifying a cryptographic signature
/// </summary>
public class VerifySignatureRequest
{
    /// <summary>
    /// Public key used for verification (base64 encoded)
    /// </summary>
    [Required]
    public required string PublicKey { get; set; }

    /// <summary>
    /// Original data that was signed (UTF-8 string, will be SHA-256 hashed for verification)
    /// </summary>
    [Required]
    public required string Data { get; set; }

    /// <summary>
    /// Signature to verify (base64 encoded)
    /// </summary>
    [Required]
    public required string Signature { get; set; }

    /// <summary>
    /// Algorithm used for signing (ED25519, NISTP256, RSA4096)
    /// </summary>
    [Required]
    public required string Algorithm { get; set; }
}
