// API Contract: IAuthenticationService
// Purpose: OAuth2 Password Grant authentication and token management
// Location: Sorcha.UI.Core/Services/Authentication/IAuthenticationService.cs

using Sorcha.UI.Core.Models.Authentication;

namespace Sorcha.UI.Core.Services.Authentication;

/// <summary>
/// Authentication service for OAuth2 Password Grant flow
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates user with username/password (OAuth2 Password Grant)
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="profileName">Profile to authenticate against</param>
    /// <returns>OAuth2 token response</returns>
    /// <exception cref="HttpRequestException">Authentication failed (401/403)</exception>
    /// <exception cref="InvalidOperationException">Profile not found</exception>
    Task<TokenResponse> LoginAsync(LoginRequest request, string profileName);

    /// <summary>
    /// Retrieves cached access token for profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>Access token or null if not authenticated</returns>
    Task<string?> GetAccessTokenAsync(string profileName);

    /// <summary>
    /// Retrieves cached refresh token for profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>Refresh token or null if not available</returns>
    Task<string?> GetRefreshTokenAsync(string profileName);

    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>True if refresh succeeded, false if refresh failed (requires re-login)</returns>
    Task<bool> RefreshTokenAsync(string profileName);

    /// <summary>
    /// Logs out user (clears cached tokens)
    /// </summary>
    /// <param name="profileName">Profile name</param>
    Task LogoutAsync(string profileName);

    /// <summary>
    /// Checks if user is authenticated for profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <returns>True if authenticated (valid non-expired token cached)</returns>
    bool IsAuthenticated(string profileName);

    /// <summary>
    /// Gets current authentication state information
    /// </summary>
    /// <returns>Authentication state with claims</returns>
    Task<AuthenticationStateInfo> GetAuthenticationInfoAsync();
}
