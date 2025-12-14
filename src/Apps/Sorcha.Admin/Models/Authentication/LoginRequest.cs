namespace Sorcha.Admin.Models.Authentication;

/// <summary>
/// Request model for user login with username and password.
/// Supports OAuth2 Password Grant flow.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email address.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client identifier (optional).
    /// Defaults to "sorcha-admin" if not specified.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth2 scope (optional).
    /// Space-separated list of requested scopes.
    /// </summary>
    public string? Scope { get; set; }
}
