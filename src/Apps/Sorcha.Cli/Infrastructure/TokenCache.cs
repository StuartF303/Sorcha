using System.Text.Json;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// Manages cached authentication tokens with OS-specific encryption.
/// Tokens are stored in ~/.sorcha/tokens/{profile}.token
/// </summary>
public class TokenCache
{
    private static string GetTokenCacheDirectory()
    {
        // Allow override via environment variable for testing
        var overrideDir = Environment.GetEnvironmentVariable("SORCHA_CONFIG_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return Path.Combine(overrideDir, "tokens");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sorcha",
            "tokens");
    }

    private readonly IEncryptionProvider _encryption;
    private readonly string _tokenCacheDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TokenCache(IEncryptionProvider encryption)
    {
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _tokenCacheDirectory = GetTokenCacheDirectory();
    }

    /// <summary>
    /// Gets the path to a token cache file for a specific profile.
    /// </summary>
    private string GetCacheFilePath(string profile)
    {
        return Path.Combine(_tokenCacheDirectory, $"{profile}.token");
    }

    /// <summary>
    /// Ensures the token cache directory exists.
    /// </summary>
    private void EnsureTokenCacheDirectory()
    {
        if (!Directory.Exists(_tokenCacheDirectory))
        {
            Directory.CreateDirectory(_tokenCacheDirectory);

            // Set restrictive permissions on Unix systems
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_tokenCacheDirectory,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    /// <summary>
    /// Stores a token in the cache with encryption.
    /// </summary>
    /// <param name="profile">Profile name</param>
    /// <param name="entry">Token cache entry to store</param>
    public async Task SetAsync(string profile, TokenCacheEntry entry)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("Profile name cannot be empty.", nameof(profile));
        }

        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        EnsureTokenCacheDirectory();

        // Serialize token entry to JSON
        var json = JsonSerializer.Serialize(entry, JsonOptions);

        // Encrypt the JSON
        var encrypted = await _encryption.EncryptAsync(json);

        // Write to cache file
        var cacheFile = GetCacheFilePath(profile);
        await File.WriteAllBytesAsync(cacheFile, encrypted);

        // Set restrictive permissions on Unix systems
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(cacheFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Retrieves a token from the cache.
    /// Returns null if the token doesn't exist, is expired, or cannot be decrypted.
    /// </summary>
    /// <param name="profile">Profile name</param>
    /// <returns>Token cache entry, or null if not found/expired</returns>
    public async Task<TokenCacheEntry?> GetAsync(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("Profile name cannot be empty.", nameof(profile));
        }

        var cacheFile = GetCacheFilePath(profile);

        if (!File.Exists(cacheFile))
        {
            return null;
        }

        try
        {
            // Read encrypted data
            var encrypted = await File.ReadAllBytesAsync(cacheFile);

            // Decrypt
            var json = await _encryption.DecryptAsync(encrypted);

            // Deserialize
            var entry = JsonSerializer.Deserialize<TokenCacheEntry>(json, JsonOptions);

            if (entry == null)
            {
                return null;
            }

            // Check if token is expired
            if (entry.IsExpired)
            {
                // Clean up expired token
                await ClearAsync(profile);
                return null;
            }

            return entry;
        }
        catch (Exception)
        {
            // If decryption or deserialization fails, clear the invalid cache entry
            await ClearAsync(profile);
            return null;
        }
    }

    /// <summary>
    /// Checks if a valid (non-expired) token exists in the cache.
    /// </summary>
    /// <param name="profile">Profile name</param>
    /// <returns>True if a valid token exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(string profile)
    {
        var entry = await GetAsync(profile);
        return entry != null && !entry.IsExpired;
    }

    /// <summary>
    /// Clears the cached token for a specific profile.
    /// </summary>
    /// <param name="profile">Profile name</param>
    public async Task ClearAsync(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("Profile name cannot be empty.", nameof(profile));
        }

        var cacheFile = GetCacheFilePath(profile);

        if (File.Exists(cacheFile))
        {
            await Task.Run(() => File.Delete(cacheFile));
        }
    }

    /// <summary>
    /// Clears all cached tokens.
    /// </summary>
    public async Task ClearAllAsync()
    {
        if (Directory.Exists(_tokenCacheDirectory))
        {
            var files = Directory.GetFiles(_tokenCacheDirectory, "*.token");
            foreach (var file in files)
            {
                await Task.Run(() => File.Delete(file));
            }
        }
    }

    /// <summary>
    /// Lists all profiles that have cached tokens.
    /// </summary>
    /// <returns>Collection of profile names with cached tokens</returns>
    public Task<IEnumerable<string>> ListCachedProfilesAsync()
    {
        if (!Directory.Exists(_tokenCacheDirectory))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = Directory.GetFiles(_tokenCacheDirectory, "*.token");
        var profiles = files.Select(f => Path.GetFileNameWithoutExtension(f));

        return Task.FromResult(profiles);
    }
}
