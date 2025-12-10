namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// Provides OS-specific encryption for sensitive data (tokens, credentials).
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Encrypts plaintext data.
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <returns>Encrypted data as byte array</returns>
    Task<byte[]> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts encrypted data.
    /// </summary>
    /// <param name="ciphertext">Encrypted data</param>
    /// <returns>Decrypted plaintext</returns>
    Task<string> DecryptAsync(byte[] ciphertext);

    /// <summary>
    /// Gets a value indicating whether this encryption provider is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }
}
