// API Contract: IEncryptionProvider
// Purpose: AES-256-GCM encryption for LocalStorage data
// Location: Sorcha.UI.Core/Services/Encryption/IEncryptionProvider.cs

namespace Sorcha.UI.Core.Services.Encryption;

/// <summary>
/// Encryption provider for sensitive data (Web Crypto API or MAUI secure storage)
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <returns>Encrypted data (Base64-encoded ciphertext + IV)</returns>
    /// <exception cref="NotSupportedException">Encryption not available (HTTP non-localhost)</exception>
    Task<string> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts ciphertext data using AES-256-GCM
    /// </summary>
    /// <param name="ciphertext">Encrypted data (Base64-encoded ciphertext + IV)</param>
    /// <returns>Decrypted plaintext</returns>
    /// <exception cref="CryptographicException">Decryption failed (invalid key/IV/data)</exception>
    Task<string> DecryptAsync(string ciphertext);

    /// <summary>
    /// Checks if encryption is available (Web Crypto API supported)
    /// </summary>
    /// <returns>True if encryption is available</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets encryption status message (for UI display)
    /// </summary>
    /// <returns>Status message (e.g., "Encrypted (HTTPS)", "Plaintext (HTTP)")</returns>
    Task<string> GetStatusAsync();
}
