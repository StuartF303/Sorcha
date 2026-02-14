using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// macOS Keychain encryption provider.
/// Uses macOS Keychain to securely store encrypted data.
/// Only available on macOS platforms.
/// </summary>
public class MacOsKeychainEncryption : IEncryptionProvider
{
    private const string ServiceName = "sorcha-cli";
    private static readonly Regex SafeAccountNamePattern = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsMacOS();

    /// <inheritdoc/>
    public async Task<byte[]> EncryptAsync(string plaintext)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS platforms.");
        }

        if (string.IsNullOrEmpty(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));
        }

        // Generate a unique account name based on GUID (safe characters only)
        var accountName = $"{ServiceName}-{Guid.NewGuid():N}";
        ValidateAccountName(accountName);

        // Store the plaintext in Keychain via stdin to avoid shell injection.
        // The macOS `security` command with `-w -` is not supported,
        // so we encode the plaintext as hex and pass via argument after validation.
        var plaintextHex = Convert.ToHexString(Encoding.UTF8.GetBytes(plaintext));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            }
        };

        // Build arguments as a list to avoid shell interpretation
        process.StartInfo.ArgumentList.Add("add-generic-password");
        process.StartInfo.ArgumentList.Add("-s");
        process.StartInfo.ArgumentList.Add(ServiceName);
        process.StartInfo.ArgumentList.Add("-a");
        process.StartInfo.ArgumentList.Add(accountName);
        process.StartInfo.ArgumentList.Add("-w");
        process.StartInfo.ArgumentList.Add(plaintextHex);
        process.StartInfo.ArgumentList.Add("-U");

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to store data in Keychain: {error}");
        }

        // Return the account name as a reference (we'll use this to retrieve the data later)
        return Encoding.UTF8.GetBytes($"keychain:{accountName}");
    }

    /// <inheritdoc/>
    public async Task<string> DecryptAsync(byte[] ciphertext)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS platforms.");
        }

        if (ciphertext == null || ciphertext.Length == 0)
        {
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));
        }

        // Ciphertext is the account name reference
        var reference = Encoding.UTF8.GetString(ciphertext);
        if (!reference.StartsWith("keychain:"))
        {
            throw new InvalidOperationException("Invalid Keychain reference format.");
        }

        var accountName = reference.Substring("keychain:".Length);
        ValidateAccountName(accountName);

        // Retrieve the plaintext from Keychain using ArgumentList to avoid shell injection
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.StartInfo.ArgumentList.Add("find-generic-password");
        process.StartInfo.ArgumentList.Add("-s");
        process.StartInfo.ArgumentList.Add(ServiceName);
        process.StartInfo.ArgumentList.Add("-a");
        process.StartInfo.ArgumentList.Add(accountName);
        process.StartInfo.ArgumentList.Add("-w");

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to retrieve data from Keychain: {error}");
        }

        // Decode the hex-encoded plaintext
        var hexValue = output.TrimEnd('\n', '\r');
        var plaintextBytes = Convert.FromHexString(hexValue);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static void ValidateAccountName(string accountName)
    {
        if (!SafeAccountNamePattern.IsMatch(accountName))
            throw new ArgumentException("Invalid account name format. Only alphanumeric, dash, and underscore are allowed.", nameof(accountName));
    }
}
