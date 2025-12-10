using System.Security.Cryptography;
using System.Text;

namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// Windows Data Protection API (DPAPI) encryption provider.
/// Uses Windows user credentials to encrypt/decrypt data.
/// Only available on Windows platforms.
/// </summary>
#pragma warning disable CA1416 // Platform-specific API guarded by IsAvailable property
public class WindowsDpapiEncryption : IEncryptionProvider
{
    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc/>
    public Task<byte[]> EncryptAsync(string plaintext)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows platforms.");
        }

        if (string.IsNullOrEmpty(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        // Use ProtectedData.Protect with CurrentUser scope
        // This encrypts data using the current Windows user's credentials
        var encryptedBytes = ProtectedData.Protect(
            plaintextBytes,
            optionalEntropy: null, // No additional entropy
            scope: DataProtectionScope.CurrentUser);

        return Task.FromResult(encryptedBytes);
    }

    /// <inheritdoc/>
    public Task<string> DecryptAsync(byte[] ciphertext)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows platforms.");
        }

        if (ciphertext == null || ciphertext.Length == 0)
        {
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));
        }

        // Use ProtectedData.Unprotect with CurrentUser scope
        var decryptedBytes = ProtectedData.Unprotect(
            ciphertext,
            optionalEntropy: null, // No additional entropy
            scope: DataProtectionScope.CurrentUser);

        var plaintext = Encoding.UTF8.GetString(decryptedBytes);

        return Task.FromResult(plaintext);
    }
}
#pragma warning restore CA1416
