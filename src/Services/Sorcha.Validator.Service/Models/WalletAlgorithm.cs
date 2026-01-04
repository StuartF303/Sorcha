// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Represents supported cryptographic algorithms for wallet operations.
/// </summary>
/// <remarks>
/// These algorithms are used for signing dockets, consensus votes, and
/// signature verification. The enum values map directly to the Wallet Service
/// proto WalletAlgorithm enum and Sorcha.Cryptography.WalletNetworks enum.
/// </remarks>
public enum WalletAlgorithm : byte
{
    /// <summary>
    /// Edwards-curve Digital Signature Algorithm.
    /// Fast, secure, 32-byte keys. Default algorithm.
    /// </summary>
    ED25519 = 1,

    /// <summary>
    /// NIST P-256 (secp256r1) elliptic curve.
    /// FIPS 186-4 compliant, 32-byte keys.
    /// </summary>
    NISTP256 = 2,

    /// <summary>
    /// RSA with 4096-bit keys.
    /// Traditional RSA algorithm, 512-byte keys.
    /// </summary>
    RSA4096 = 3
}
