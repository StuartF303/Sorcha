// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Cryptographic signature with algorithm information
/// </summary>
public class Signature
{
    /// <summary>
    /// Signer's public key
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// Base64-encoded signature value
    /// </summary>
    public required string SignatureValue { get; init; }

    /// <summary>
    /// Signature algorithm (ED25519, NIST-P256, RSA-4096)
    /// </summary>
    public required string Algorithm { get; init; }
}
