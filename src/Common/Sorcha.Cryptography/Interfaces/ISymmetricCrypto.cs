using System.Threading;
using System.Threading.Tasks;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;

namespace Sorcha.Cryptography.Interfaces;

/// <summary>
/// Provides symmetric encryption and decryption operations.
/// </summary>
public interface ISymmetricCrypto
{
    /// <summary>
    /// Encrypts data with symmetric encryption.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="encryptionType">The encryption algorithm to use.</param>
    /// <param name="key">Optional encryption key. If null, a new key is generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the ciphertext with metadata or error status.</returns>
    Task<CryptoResult<SymmetricCiphertext>> EncryptAsync(
        byte[] plaintext,
        EncryptionType encryptionType = EncryptionType.XCHACHA20_POLY1305,
        byte[]? key = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data with symmetric encryption.
    /// </summary>
    /// <param name="ciphertext">The encrypted data with metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the plaintext or error status.</returns>
    Task<CryptoResult<byte[]>> DecryptAsync(
        SymmetricCiphertext ciphertext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a random encryption key for the specified encryption type.
    /// </summary>
    /// <param name="encryptionType">The encryption algorithm.</param>
    /// <returns>A cryptographically secure random key.</returns>
    byte[] GenerateKey(EncryptionType encryptionType);

    /// <summary>
    /// Generates a random IV/nonce for the specified encryption type.
    /// </summary>
    /// <param name="encryptionType">The encryption algorithm.</param>
    /// <returns>A cryptographically secure random IV/nonce.</returns>
    byte[] GenerateIV(EncryptionType encryptionType);
}
