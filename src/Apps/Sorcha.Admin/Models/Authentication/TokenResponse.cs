using System.Text.Json.Serialization;

namespace Sorcha.Admin.Models.Authentication;

/// <summary>
/// OAuth2 token response from the Tenant Service.
/// Conforms to RFC 6749 (OAuth 2.0).
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The access token (JWT) for API authentication.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new access tokens (optional).
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token type, typically "Bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token lifetime in seconds from issuance.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Space-separated list of granted scopes (optional).
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
