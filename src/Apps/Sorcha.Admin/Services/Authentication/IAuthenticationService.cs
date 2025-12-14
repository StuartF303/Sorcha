using Sorcha.Admin.Models.Authentication;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Authentication service interface for managing user authentication with the Tenant Service.
/// Supports OAuth2 Password Grant flow with extensibility for future auth methods.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password (OAuth2 Password Grant).
    /// </summary>
    /// <param name="request">Login request containing username and password.</param>
    /// <param name="profileName">Profile name to authenticate against.</param>
    /// <returns>Token response with access token and optional refresh token.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if authentication fails.</exception>
    Task<TokenResponse> LoginAsync(LoginRequest request, string profileName);

    /// <summary>
    /// Gets the current access token for the specified profile.
    /// Automatically refreshes if token is expiring soon.
    /// </summary>
    /// <param name="profileName">Profile name to get token for.</param>
    /// <returns>Access token string, or null if not authenticated.</returns>
    Task<string?> GetAccessTokenAsync(string profileName);

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    /// <param name="profileName">Profile name to refresh token for.</param>
    /// <returns>New token response.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if refresh fails.</exception>
    Task<TokenResponse?> RefreshTokenAsync(string profileName);

    /// <summary>
    /// Checks if the user is authenticated for the specified profile.
    /// </summary>
    /// <param name="profileName">Profile name to check.</param>
    /// <returns>True if authenticated with a valid token.</returns>
    Task<bool> IsAuthenticatedAsync(string profileName);

    /// <summary>
    /// Logs out the user by clearing the cached token for the specified profile.
    /// </summary>
    /// <param name="profileName">Profile name to logout from.</param>
    Task LogoutAsync(string profileName);

    /// <summary>
    /// Logs out from all profiles by clearing all cached tokens.
    /// </summary>
    Task LogoutAllAsync();

    // Future extensibility methods (not implemented in Phase 1)

    /// <summary>
    /// Initiates OAuth2 Device Code flow authentication.
    /// </summary>
    /// <param name="profileName">Profile name to authenticate against.</param>
    /// <returns>Device code response with user code and verification URL.</returns>
    Task<DeviceCodeResponse> StartDeviceFlowAsync(string profileName);

    /// <summary>
    /// Polls the token endpoint for OAuth2 Device Code flow completion.
    /// </summary>
    /// <param name="deviceCode">Device code response from StartDeviceFlowAsync.</param>
    /// <param name="profileName">Profile name to authenticate against.</param>
    /// <returns>Token response when user completes authorization.</returns>
    Task<TokenResponse> PollDeviceFlowAsync(DeviceCodeResponse deviceCode, string profileName);
}

/// <summary>
/// Device code response for OAuth2 Device Code flow.
/// Placeholder for future implementation.
/// </summary>
public class DeviceCodeResponse
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUri { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int Interval { get; set; } = 5;
}
