// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Supported cryptographic hash algorithms.
/// </summary>
public enum HashType
{
    /// <summary>
    /// SHA-256 hash algorithm (256-bit output).
    /// </summary>
    SHA256,

    /// <summary>
    /// SHA-384 hash algorithm (384-bit output).
    /// </summary>
    SHA384,

    /// <summary>
    /// SHA-512 hash algorithm (512-bit output).
    /// </summary>
    SHA512,

    /// <summary>
    /// Blake2b hash algorithm with 256-bit output.
    /// </summary>
    Blake2b256,

    /// <summary>
    /// Blake2b hash algorithm with 512-bit output.
    /// </summary>
    Blake2b512
}
