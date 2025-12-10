namespace Sorcha.Cli.Models;

/// <summary>
/// User login request.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client ID (optional, uses profile default if not specified).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth2 scope (optional).
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// Service principal (client credentials) login request.
/// </summary>
public class ServicePrincipalLoginRequest
{
    /// <summary>
    /// Client ID (service principal ID).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 scope (optional).
    /// </summary>
    public string? Scope { get; set; }
}
