using System;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Represents a complete key ring with mnemonic recovery phrase.
/// </summary>
public class KeyRing
{
    /// <summary>
    /// Gets or sets the network type for this key ring.
    /// </summary>
    public required WalletNetworks Network { get; init; }

    /// <summary>
    /// Gets or sets the mnemonic recovery phrase (12 words).
    /// </summary>
    public string? Mnemonic { get; init; }

    /// <summary>
    /// Gets or sets the master key set.
    /// </summary>
    public required KeySet MasterKeySet { get; init; }

    /// <summary>
    /// Gets or sets the optional password hint.
    /// </summary>
    public string? PasswordHint { get; init; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Zeroes out sensitive key material.
    /// </summary>
    public void Zeroize()
    {
        MasterKeySet.Zeroize();
    }
}
