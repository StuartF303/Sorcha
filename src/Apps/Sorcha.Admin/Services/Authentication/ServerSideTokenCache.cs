using Microsoft.AspNetCore.DataProtection;
using Sorcha.Admin.Models.Authentication;
using System.Text.Json;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Server-side session-based token cache implementation.
/// Stores encrypted authentication tokens in ASP.NET Core Session.
/// This implementation is immune to Blazor Server circuit recreation issues.
/// </summary>
public class ServerSideTokenCache : ITokenCache
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<ServerSideTokenCache> _logger;

    private const string SESSION_KEY_PREFIX = "sorcha:tokens:";

    public ServerSideTokenCache(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ServerSideTokenCache> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dataProtector = dataProtectionProvider?.CreateProtector("Sorcha.TokenCache")
            ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the session instance, ensuring session is available.
    /// </summary>
    private ISession GetSession()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available. Ensure this is called within an HTTP request.");
        }

        return httpContext.Session;
    }

    /// <summary>
    /// Stores a token cache entry for the specified profile.
    /// </summary>
    public async Task SetAsync(string profileName, TokenCacheEntry entry)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentNullException(nameof(profileName));
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        try
        {
            var session = GetSession();

            // Ensure session is loaded
            await session.LoadAsync();

            // Serialize token cache entry to JSON
            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Encrypt JSON using Data Protection API
            var encryptedBytes = _dataProtector.Protect(System.Text.Encoding.UTF8.GetBytes(json));

            // Store in session with profile-specific key
            var sessionKey = $"{SESSION_KEY_PREFIX}{profileName}";
            session.Set(sessionKey, encryptedBytes);

            // Commit session changes
            await session.CommitAsync();

            _logger.LogInformation("Token cached for profile '{Profile}' (Subject: {Subject}, Expires: {Expiry})",
                profileName, entry.Subject, entry.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store token for profile '{Profile}'", profileName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a token cache entry for the specified profile.
    /// </summary>
    public async Task<TokenCacheEntry?> GetAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentNullException(nameof(profileName));

        try
        {
            var session = GetSession();

            // Ensure session is loaded
            await session.LoadAsync();

            var sessionKey = $"{SESSION_KEY_PREFIX}{profileName}";
            if (!session.TryGetValue(sessionKey, out var encryptedBytes))
            {
                _logger.LogDebug("No cached token found for profile '{Profile}'", profileName);
                return null;
            }

            // Decrypt using Data Protection API
            var json = System.Text.Encoding.UTF8.GetString(_dataProtector.Unprotect(encryptedBytes));

            // Deserialize from JSON
            var entry = JsonSerializer.Deserialize<TokenCacheEntry>(json);

            if (entry == null)
            {
                _logger.LogWarning("Failed to deserialize token for profile '{Profile}'", profileName);
                return null;
            }

            // Check if token is expired
            if (entry.IsExpired)
            {
                _logger.LogInformation("Cached token for profile '{Profile}' has expired", profileName);
                await ClearAsync(profileName);
                return null;
            }

            _logger.LogDebug("Retrieved cached token for profile '{Profile}' (Subject: {Subject}, Expires: {Expiry})",
                profileName, entry.Subject, entry.ExpiresAt);

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve token for profile '{Profile}'", profileName);
            return null;
        }
    }

    /// <summary>
    /// Checks if a token exists and is valid for the specified profile.
    /// </summary>
    public async Task<bool> HasValidTokenAsync(string profileName)
    {
        var entry = await GetAsync(profileName);
        return entry != null && !entry.IsExpired;
    }

    /// <summary>
    /// Removes the cached token for the specified profile.
    /// </summary>
    public async Task ClearAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentNullException(nameof(profileName));

        try
        {
            var session = GetSession();

            // Ensure session is loaded
            await session.LoadAsync();

            var sessionKey = $"{SESSION_KEY_PREFIX}{profileName}";
            session.Remove(sessionKey);

            // Commit session changes
            await session.CommitAsync();

            _logger.LogInformation("Cleared cached token for profile '{Profile}'", profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear token for profile '{Profile}'", profileName);
            throw;
        }
    }

    /// <summary>
    /// Removes all cached tokens for all profiles.
    /// </summary>
    public async Task ClearAllAsync()
    {
        try
        {
            var session = GetSession();

            // Ensure session is loaded
            await session.LoadAsync();

            // Find all token-related session keys
            var tokenKeys = session.Keys
                .Where(key => key.StartsWith(SESSION_KEY_PREFIX))
                .ToList();

            foreach (var key in tokenKeys)
            {
                session.Remove(key);
            }

            // Commit session changes
            await session.CommitAsync();

            _logger.LogInformation("Cleared all cached tokens ({Count} profiles)", tokenKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all tokens");
            throw;
        }
    }

    /// <summary>
    /// Lists all profile names that have cached tokens.
    /// </summary>
    public async Task<IEnumerable<string>> ListCachedProfilesAsync()
    {
        try
        {
            var session = GetSession();

            // Ensure session is loaded
            await session.LoadAsync();

            var profileNames = session.Keys
                .Where(key => key.StartsWith(SESSION_KEY_PREFIX))
                .Select(key => key.Substring(SESSION_KEY_PREFIX.Length))
                .ToList();

            _logger.LogDebug("Found {Count} cached profiles", profileNames.Count);

            return profileNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list cached profiles");
            return Enumerable.Empty<string>();
        }
    }
}
