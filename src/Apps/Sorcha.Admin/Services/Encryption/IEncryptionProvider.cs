namespace Sorcha.Admin.Services.Encryption;

/// <summary>
/// Encryption provider interface for encrypting and decrypting sensitive data.
/// Implementations may use different encryption mechanisms based on the platform
/// (e.g., Web Crypto API for browsers, DPAPI for Windows, Keychain for macOS).
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Indicates whether encryption is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Encrypts plaintext data.
    /// </summary>
    /// <param name="plaintext">The plaintext string to encrypt.</param>
    /// <returns>Encrypted data as a byte array.</returns>
    /// <exception cref="NotSupportedException">Thrown if encryption is not available.</exception>
    Task<byte[]> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts encrypted data.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <returns>Decrypted plaintext string.</returns>
    /// <exception cref="NotSupportedException">Thrown if decryption is not available.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails.</exception>
    Task<string> DecryptAsync(byte[] ciphertext);
}
