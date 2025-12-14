using Blazored.LocalStorage;
using Sorcha.Admin.Models.Authentication;
using Sorcha.Admin.Services.Encryption;
using System.Text.Json;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Token cache using browser LocalStorage with encryption.
/// Stores JWT tokens encrypted at rest to protect against casual inspection.
///
/// Storage Key Format: sorcha:tokens:{profileName}
/// Storage Value Format: Base64-encoded encrypted JSON of TokenCacheEntry
/// </summary>
public class BrowserTokenCache
{
    private readonly ILocalStorageService _localStorage;
    private readonly IEncryptionProvider _encryption;
    private const string TOKEN_CACHE_PREFIX = "sorcha:tokens:";

    public BrowserTokenCache(
        ILocalStorageService localStorage,
        IEncryptionProvider encryption)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    }

    /// <summary>
    /// Stores a token entry encrypted in LocalStorage.
    /// </summary>
    /// <param name="profile">Profile name to associate this token with.</param>
    /// <param name="entry">Token cache entry to store.</param>
    public async Task SetAsync(string profile, TokenCacheEntry entry)
    {
        if (string.IsNullOrEmpty(profile))
            throw new ArgumentException("Profile cannot be null or empty.", nameof(profile));

        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var key = $"{TOKEN_CACHE_PREFIX}{profile}";

        try
        {
            // Serialize token entry to JSON
            var json = JsonSerializer.Serialize(entry);

            // Encrypt the JSON
            var encrypted = await _encryption.EncryptAsync(json);

            // Encode as Base64 for LocalStorage storage
            var base64 = Convert.ToBase64String(encrypted);

            // Store in LocalStorage
            await _localStorage.SetItemAsStringAsync(key, base64);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to store token for profile '{profile}'.", ex);
        }
    }

    /// <summary>
    /// Retrieves a token entry from LocalStorage and decrypts it.
    /// Returns null if the token doesn't exist or has expired.
    /// </summary>
    /// <param name="profile">Profile name to retrieve token for.</param>
    /// <returns>Token cache entry, or null if not found or expired.</returns>
    public async Task<TokenCacheEntry?> GetAsync(string profile)
    {
        if (string.IsNullOrEmpty(profile))
            throw new ArgumentException("Profile cannot be null or empty.", nameof(profile));

        var key = $"{TOKEN_CACHE_PREFIX}{profile}";

        try
        {
            // Retrieve from LocalStorage
            var base64 = await _localStorage.GetItemAsStringAsync(key);

            if (string.IsNullOrEmpty(base64))
                return null;

            // Decode from Base64
            var encrypted = Convert.FromBase64String(base64);

            // Decrypt
            var json = await _encryption.DecryptAsync(encrypted);

            // Deserialize
            var entry = JsonSerializer.Deserialize<TokenCacheEntry>(json);

            // Check if token has expired
            if (entry?.IsExpired == true)
            {
                // Auto-delete expired tokens
                await ClearAsync(profile);
                return null;
            }

            return entry;
        }
        catch (JsonException ex)
        {
            // Corrupted data - delete and return null
            await ClearAsync(profile);
            throw new InvalidOperationException(
                $"Token cache for profile '{profile}' is corrupted.", ex);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // Decryption failed - delete and return null
            await ClearAsync(profile);
            throw new InvalidOperationException(
                $"Failed to decrypt token for profile '{profile}'.", ex);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            // Other errors - delete to be safe
            await ClearAsync(profile);
            return null;
        }
    }

    /// <summary>
    /// Checks if a valid (non-expired) token exists for the specified profile.
    /// </summary>
    /// <param name="profile">Profile name to check.</param>
    /// <returns>True if a valid token exists, false otherwise.</returns>
    public async Task<bool> ExistsAsync(string profile)
    {
        var entry = await GetAsync(profile);
        return entry != null;
    }

    /// <summary>
    /// Removes the cached token for the specified profile.
    /// </summary>
    /// <param name="profile">Profile name to clear token for.</param>
    public async Task ClearAsync(string profile)
    {
        if (string.IsNullOrEmpty(profile))
            throw new ArgumentException("Profile cannot be null or empty.", nameof(profile));

        var key = $"{TOKEN_CACHE_PREFIX}{profile}";

        try
        {
            await _localStorage.RemoveItemAsync(key);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to clear token for profile '{profile}'.", ex);
        }
    }

    /// <summary>
    /// Removes all cached tokens for all profiles.
    /// Useful for logout-all or security cleanup scenarios.
    /// </summary>
    public async Task ClearAllAsync()
    {
        try
        {
            // Get all LocalStorage keys
            var keys = await _localStorage.KeysAsync();

            // Filter to token keys only
            var tokenKeys = keys.Where(k => k.StartsWith(TOKEN_CACHE_PREFIX)).ToList();

            // Remove all token keys
            foreach (var key in tokenKeys)
            {
                await _localStorage.RemoveItemAsync(key);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to clear all tokens.", ex);
        }
    }

    /// <summary>
    /// Gets a list of all profile names that have cached tokens.
    /// </summary>
    /// <returns>List of profile names with cached tokens.</returns>
    public async Task<IEnumerable<string>> ListCachedProfilesAsync()
    {
        try
        {
            // Get all LocalStorage keys
            var keys = await _localStorage.KeysAsync();

            // Extract profile names from token keys
            var profiles = keys
                .Where(k => k.StartsWith(TOKEN_CACHE_PREFIX))
                .Select(k => k.Substring(TOKEN_CACHE_PREFIX.Length))
                .ToList();

            return profiles;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to list cached profiles.", ex);
        }
    }
}
