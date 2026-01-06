// API Contract: ITokenCache
// Purpose: Encrypted token storage in LocalStorage
// Location: Sorcha.UI.Core/Services/Authentication/ITokenCache.cs

using Sorcha.UI.Core.Models.Authentication;

namespace Sorcha.UI.Core.Services.Authentication;

/// <summary>
/// Token cache for encrypted storage of JWT tokens
/// </summary>
public interface ITokenCache
{
    /// <summary>
    /// Stores authentication tokens for a profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <param name="entry">Token cache entry with access/refresh tokens</param>
    /// <exception cref="InvalidOperationException">Encryption failed</exception>
    Task StoreTokenAsync(string profileName, TokenCacheEntry entry);

    /// <summary>
    /// Retrieves cached tokens for a profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>Token cache entry or null if not found</returns>
    Task<TokenCacheEntry?> GetTokenAsync(string profileName);

    /// <summary>
    /// Removes cached tokens for a profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    Task RemoveTokenAsync(string profileName);

    /// <summary>
    /// Checks if tokens exist for a profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>True if tokens are cached</returns>
    Task<bool> HasTokenAsync(string profileName);

    /// <summary>
    /// Clears all cached tokens (all profiles)
    /// </summary>
    Task ClearAllTokensAsync();
}
