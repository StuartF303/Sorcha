using System;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Extensions;

/// <summary>
/// Extension methods for cryptography enumerations.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the public key size in bytes for the network type.
    /// </summary>
    /// <param name="network">The wallet network type.</param>
    /// <returns>The public key size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when network type is unknown.</exception>
    public static int GetPublicKeySize(this WalletNetworks network) => network switch
    {
        WalletNetworks.ED25519 => 32,
        WalletNetworks.NISTP256 => 64,
        WalletNetworks.RSA4096 => 550, // Approximate DER-encoded size
        _ => throw new ArgumentException($"Unknown network type: {network}", nameof(network))
    };

    /// <summary>
    /// Gets the private key size in bytes for the network type.
    /// </summary>
    /// <param name="network">The wallet network type.</param>
    /// <returns>The private key size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when network type is unknown.</exception>
    public static int GetPrivateKeySize(this WalletNetworks network) => network switch
    {
        WalletNetworks.ED25519 => 64,
        WalletNetworks.NISTP256 => 32,
        WalletNetworks.RSA4096 => 1193, // Approximate DER-encoded size
        _ => throw new ArgumentException($"Unknown network type: {network}", nameof(network))
    };

    /// <summary>
    /// Gets the symmetric key size in bytes for the encryption type.
    /// </summary>
    /// <param name="type">The encryption type.</param>
    /// <returns>The key size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when encryption type is unknown.</exception>
    public static int GetSymmetricKeySize(this EncryptionType type) => type switch
    {
        EncryptionType.AES_128 => 16,
        EncryptionType.AES_256 => 32,
        EncryptionType.AES_GCM => 32,
        EncryptionType.CHACHA20_POLY1305 => 32,
        EncryptionType.XCHACHA20_POLY1305 => 32,
        _ => throw new ArgumentException($"Unknown encryption type: {type}", nameof(type))
    };

    /// <summary>
    /// Gets the initialization vector size in bytes for the encryption type.
    /// </summary>
    /// <param name="type">The encryption type.</param>
    /// <returns>The IV/nonce size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when encryption type is unknown.</exception>
    public static int GetIVSize(this EncryptionType type) => type switch
    {
        EncryptionType.AES_128 => 16,
        EncryptionType.AES_256 => 16,
        EncryptionType.AES_GCM => 12,
        EncryptionType.CHACHA20_POLY1305 => 12,
        EncryptionType.XCHACHA20_POLY1305 => 24,
        _ => throw new ArgumentException($"Unknown encryption type: {type}", nameof(type))
    };

    /// <summary>
    /// Gets the hash output size in bytes for the hash type.
    /// </summary>
    /// <param name="type">The hash type.</param>
    /// <returns>The hash size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when hash type is unknown.</exception>
    public static int GetHashSize(this HashType type) => type switch
    {
        HashType.SHA256 => 32,
        HashType.SHA384 => 48,
        HashType.SHA512 => 64,
        HashType.Blake2b256 => 32,
        HashType.Blake2b512 => 64,
        _ => throw new ArgumentException($"Unknown hash type: {type}", nameof(type))
    };
}
