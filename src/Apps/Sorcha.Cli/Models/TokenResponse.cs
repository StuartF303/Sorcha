using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// OAuth2 token response from authentication endpoint.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Access token (JWT).
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token (optional).
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token type (usually "Bearer").
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token scope (optional).
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
