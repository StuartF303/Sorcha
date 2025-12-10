using System.Diagnostics;
using System.Text;

namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// macOS Keychain encryption provider.
/// Uses macOS Keychain to securely store encrypted data.
/// Only available on macOS platforms.
/// </summary>
public class MacOsKeychainEncryption : IEncryptionProvider
{
    private const string ServiceName = "sorcha-cli";

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

        // Generate a unique account name based on timestamp to avoid collisions
        var accountName = $"{ServiceName}-{Guid.NewGuid():N}";

        // Store the plaintext in Keychain using the `security` command
        // -s: service name
        // -a: account name
        // -w: password (plaintext data)
        // -U: update if exists
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-generic-password -s {ServiceName} -a {accountName} -w \"{EscapeForShell(plaintext)}\" -U",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

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

        // Retrieve the plaintext from Keychain using the `security` command
        // find-generic-password: find a generic password item
        // -s: service name
        // -a: account name
        // -w: print password only
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"find-generic-password -s {ServiceName} -a {accountName} -w",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to retrieve data from Keychain: {error}");
        }

        // Remove trailing newline
        return output.TrimEnd('\n', '\r');
    }

    /// <summary>
    /// Escapes a string for use in a shell command argument.
    /// </summary>
    private static string EscapeForShell(string input)
    {
        // Replace backslashes and quotes
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
