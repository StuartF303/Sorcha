using System;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Container for symmetrically encrypted data with metadata.
/// </summary>
public class SymmetricCiphertext : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets or initializes the encrypted data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets or initializes the encryption key.
    /// </summary>
    public required byte[] Key { get; init; }

    /// <summary>
    /// Gets or initializes the initialization vector or nonce.
    /// </summary>
    public required byte[] IV { get; init; }

    /// <summary>
    /// Gets or initializes the encryption type used.
    /// </summary>
    public required EncryptionType Type { get; init; }

    /// <summary>
    /// Zeroes out sensitive key material.
    /// </summary>
    public void Zeroize()
    {
        Array.Clear(Key, 0, Key.Length);
        Array.Clear(IV, 0, IV.Length);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            Zeroize();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Destructor to ensure key material is zeroized.
    /// </summary>
    ~SymmetricCiphertext() => Dispose();
}
