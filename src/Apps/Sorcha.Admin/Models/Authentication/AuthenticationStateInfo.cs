namespace Sorcha.Admin.Models.Authentication;

/// <summary>
/// UI-friendly authentication state information.
/// Used for displaying authentication status in components.
/// </summary>
public class AuthenticationStateInfo
{
    /// <summary>
    /// Indicates whether the user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Username of the authenticated user (if authenticated).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Name of the active profile (environment) the user is authenticated against.
    /// </summary>
    public string? CurrentProfile { get; set; }

    /// <summary>
    /// Absolute expiration time of the current access token (UTC).
    /// </summary>
    public DateTimeOffset? TokenExpiresAt { get; set; }
}
