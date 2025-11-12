using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;

namespace Sorcha.Cryptography.Interfaces;

/// <summary>
/// Provides cryptographic operations for key management, signing, and encryption.
/// </summary>
public interface ICryptoModule
{
    /// <summary>
    /// Generates a new cryptographic key pair.
    /// </summary>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="seed">Optional seed for deterministic generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the generated key set or error status.</returns>
    Task<CryptoResult<KeySet>> GenerateKeySetAsync(
        WalletNetworks network,
        byte[]? seed = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recovers a key set from key data.
    /// </summary>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="keyData">The key data to recover from.</param>
    /// <param name="password">Optional password for encrypted keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the recovered key set or error status.</returns>
    Task<CryptoResult<KeySet>> RecoverKeySetAsync(
        WalletNetworks network,
        byte[] keyData,
        string? password = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a hash with a private key.
    /// </summary>
    /// <param name="hash">The hash to sign.</param>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="privateKey">The private key to sign with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the signature or error status.</returns>
    Task<CryptoResult<byte[]>> SignAsync(
        byte[] hash,
        byte network,
        byte[] privateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a signature against a hash and public key.
    /// </summary>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="hash">The hash that was signed.</param>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="publicKey">The public key to verify with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification status.</returns>
    Task<CryptoStatus> VerifyAsync(
        byte[] signature,
        byte[] hash,
        byte network,
        byte[] publicKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts data with a public key.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="publicKey">The public key to encrypt with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the ciphertext or error status.</returns>
    Task<CryptoResult<byte[]>> EncryptAsync(
        byte[] data,
        byte network,
        byte[] publicKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data with a private key.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt.</param>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="privateKey">The private key to decrypt with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the plaintext or error status.</returns>
    Task<CryptoResult<byte[]>> DecryptAsync(
        byte[] ciphertext,
        byte network,
        byte[] privateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates public key from private key.
    /// </summary>
    /// <param name="network">The wallet network/algorithm type.</param>
    /// <param name="privateKey">The private key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the public key or error status.</returns>
    Task<CryptoResult<byte[]>> CalculatePublicKeyAsync(
        byte network,
        byte[] privateKey,
        CancellationToken cancellationToken = default);
}
