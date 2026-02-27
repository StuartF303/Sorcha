// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Interfaces;

/// <summary>
/// Provides cryptographic hash functions.
/// </summary>
public interface IHashProvider
{
    /// <summary>
    /// Computes hash of data using specified algorithm.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="hashType">The hash algorithm to use.</param>
    /// <returns>The hash bytes.</returns>
    byte[] ComputeHash(byte[] data, HashType hashType = HashType.SHA256);

    /// <summary>
    /// Computes hash of data stream using specified algorithm.
    /// </summary>
    /// <param name="dataStream">The data stream to hash.</param>
    /// <param name="hashType">The hash algorithm to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hash bytes.</returns>
    Task<byte[]> ComputeHashAsync(
        Stream dataStream,
        HashType hashType = HashType.SHA256,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes HMAC of data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="key">The HMAC key.</param>
    /// <param name="hashType">The hash algorithm to use.</param>
    /// <returns>The HMAC bytes.</returns>
    byte[] ComputeHMAC(byte[] data, byte[] key, HashType hashType = HashType.SHA256);

    /// <summary>
    /// Verifies that data matches a given hash.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="hash">The expected hash.</param>
    /// <param name="hashType">The hash algorithm used.</param>
    /// <returns>True if the hash matches.</returns>
    bool VerifyHash(byte[] data, byte[] hash, HashType hashType = HashType.SHA256);
}
