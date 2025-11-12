using System;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Represents a cryptographic key with its associated network type.
/// </summary>
public readonly struct CryptoKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoKey"/> struct.
    /// </summary>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="key">The key bytes.</param>
    public CryptoKey(WalletNetworks network, byte[]? key)
    {
        Network = network;
        Key = key;
    }

    /// <summary>
    /// Gets the wallet network/algorithm type.
    /// </summary>
    public WalletNetworks Network { get; init; }

    /// <summary>
    /// Gets the key bytes.
    /// </summary>
    public byte[]? Key { get; init; }

    /// <summary>
    /// Zeroes out the key data for security.
    /// </summary>
    public void Zeroize()
    {
        if (Key != null)
        {
            Array.Clear(Key, 0, Key.Length);
        }
    }
}
