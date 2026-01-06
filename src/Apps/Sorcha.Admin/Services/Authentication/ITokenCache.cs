using Sorcha.Admin.Models.Authentication;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Interface for token cache implementations.
/// Provides abstraction for storing and retrieving authentication tokens.
/// </summary>
public interface ITokenCache
{
    /// <summary>
    /// Stores a token cache entry for the specified profile.
    /// </summary>
    Task SetAsync(string profileName, TokenCacheEntry entry);

    /// <summary>
    /// Retrieves a token cache entry for the specified profile.
    /// </summary>
    Task<TokenCacheEntry?> GetAsync(string profileName);

    /// <summary>
    /// Checks if a token exists and is valid for the specified profile.
    /// </summary>
    Task<bool> HasValidTokenAsync(string profileName);

    /// <summary>
    /// Removes the cached token for the specified profile.
    /// </summary>
    Task ClearAsync(string profileName);

    /// <summary>
    /// Removes all cached tokens for all profiles.
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Lists all profile names that have cached tokens.
    /// </summary>
    Task<IEnumerable<string>> ListCachedProfilesAsync();
}
