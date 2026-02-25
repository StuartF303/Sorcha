// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for ML-KEM-768 key decapsulation + symmetric decryption.
/// </summary>
public class DecapsulateRequest
{
    /// <summary>
    /// The packed ciphertext from encapsulation (base64 encoded).
    /// Contains KEM ciphertext + nonce + symmetric ciphertext.
    /// </summary>
    public required string Ciphertext { get; set; }
}
