// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for ML-KEM-768 key encapsulation + symmetric encryption.
/// </summary>
public class EncapsulateRequest
{
    /// <summary>
    /// Recipient's ML-KEM-768 public key (base64 encoded).
    /// </summary>
    public required string RecipientPublicKey { get; set; }

    /// <summary>
    /// Plaintext data to encrypt (base64 encoded). Optional — if omitted, only the shared secret is returned.
    /// </summary>
    public string? Plaintext { get; set; }
}
