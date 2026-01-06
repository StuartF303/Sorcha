using Microsoft.JSInterop;
using Sorcha.Admin.Models.Authentication;
using Sorcha.Admin.Services.Encryption;
using System.Text.Json;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Token cache using browser LocalStorage with encryption.
/// Stores JWT tokens encrypted at rest to protect against casual inspection.
///
/// Uses direct JSRuntime calls to localStorage for synchronous, reliable writes
/// that complete before navigation events can terminate the JavaScript context.
///
/// Storage Key Format: sorcha:tokens:{profileName}
/// Storage Value Format: Base64-encoded encrypted JSON of TokenCacheEntry
/// </summary>
public class BrowserTokenCache : ITokenCache
{
    private readonly IEncryptionProvider _encryption;
    private readonly IJSRuntime _jsRuntime;
    private const string TOKEN_CACHE_PREFIX = "sorcha:tokens:";

    public BrowserTokenCache(
        IEncryptionProvider encryption,
        IJSRuntime jsRuntime)
    {
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    private async Task LogAsync(string level, string message, object? data = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync($"console.{level}", $"[BrowserTokenCache] {message}", data ?? "");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Stores a token entry encrypted in LocalStorage.
    /// </summary>
    /// <param name="profile">Profile name to associate this token with.</param>
    /// <param name="entry">Token cache entry to store.</param>
    public async Task SetAsync(string profile, TokenCacheEntry entry)
    {
        await LogAsync("info", $"SetAsync called for profile: '{profile}'");

        if (string.IsNullOrEmpty(profile))
            throw new ArgumentException("Profile cannot be null or empty.", nameof(profile));

        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var key = $"{TOKEN_CACHE_PREFIX}{profile}";

        try
        {
            await LogAsync("debug", $"Starting token storage for profile '{profile}'", new {
                Profile = profile,
                Subject = entry.Subject,
                ExpiresAt = entry.ExpiresAt.ToString("O")
            });

            // Serialize token entry to JSON
            await LogAsync("debug", "Step 1: Serializing token entry to JSON...");
            var json = JsonSerializer.Serialize(entry);
            await LogAsync("debug", $"JSON serialized: {json.Length} characters");

            // Encrypt the JSON
            await LogAsync("debug", "Step 2: Encrypting JSON...");
            var encrypted = await _encryption.EncryptAsync(json);
            await LogAsync("debug", $"Encryption completed: {encrypted.Length} bytes");

            // Encode as Base64 for LocalStorage storage
            await LogAsync("debug", "Step 3: Encoding as Base64...");
            var base64 = Convert.ToBase64String(encrypted);
            await LogAsync("debug", $"Base64 encoding completed: {base64.Length} characters");

            // Store in LocalStorage using synchronous JSRuntime to ensure write completes
            // before any navigation that might terminate the JavaScript context
            await LogAsync("debug", $"Step 4: Storing in LocalStorage with key: '{key}'...");
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, base64);

            // Verify write succeeded by reading back
            var verified = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (verified != base64)
                throw new InvalidOperationException("LocalStorage write verification failed");

            await LogAsync("info", $"✓ Token successfully stored for profile '{profile}'");
        }
        catch (Exception ex)
        {
            await LogAsync("error", $"✗ Failed to store token for profile '{profile}': {ex.Message}", new {
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });
            throw new InvalidOperationException(
                $"Failed to store token for profile '{profile}'. Error: {ex.Message}", ex);
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
            // Retrieve from LocalStorage using synchronous JSRuntime
            var base64 = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);

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
    /// Checks if a valid (non-expired) token exists for the specified profile.
    /// Implements ITokenCache interface method.
    /// </summary>
    /// <param name="profileName">Profile name to check.</param>
    /// <returns>True if a valid token exists, false otherwise.</returns>
    public async Task<bool> HasValidTokenAsync(string profileName)
    {
        return await ExistsAsync(profileName);
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
            // Remove from LocalStorage using synchronous JSRuntime
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
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
            // Get all LocalStorage keys using JSRuntime
            var length = await _jsRuntime.InvokeAsync<int>("eval", "localStorage.length");
            var tokenKeys = new List<string>();

            for (int i = 0; i < length; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string>("localStorage.key", i);
                if (!string.IsNullOrEmpty(key) && key.StartsWith(TOKEN_CACHE_PREFIX))
                {
                    tokenKeys.Add(key);
                }
            }

            // Remove all token keys
            foreach (var key in tokenKeys)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
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
            // Get all LocalStorage keys using JSRuntime
            var length = await _jsRuntime.InvokeAsync<int>("eval", "localStorage.length");
            var profiles = new List<string>();

            for (int i = 0; i < length; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string>("localStorage.key", i);
                if (!string.IsNullOrEmpty(key) && key.StartsWith(TOKEN_CACHE_PREFIX))
                {
                    var profileName = key.Substring(TOKEN_CACHE_PREFIX.Length);
                    profiles.Add(profileName);
                }
            }

            return profiles;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to list cached profiles.", ex);
        }
    }
}
