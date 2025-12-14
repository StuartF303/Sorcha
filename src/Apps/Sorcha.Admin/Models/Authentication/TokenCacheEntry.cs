using System.Text.Json.Serialization;

namespace Sorcha.Admin.Models.Authentication;

/// <summary>
/// Cached token entry stored in browser LocalStorage (encrypted).
/// Contains both access and refresh tokens with expiration metadata.
/// </summary>
public class TokenCacheEntry
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new access tokens (optional).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Absolute expiration time of the access token (UTC).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Profile name this token is associated with.
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Subject (username or client ID) extracted from the token.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Indicates whether the token has expired.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Indicates whether the token will expire soon (within the specified buffer).
    /// </summary>
    /// <param name="bufferMinutes">Buffer time in minutes before expiration. Default is 5 minutes.</param>
    /// <returns>True if the token expires within the buffer time.</returns>
    public bool IsExpiringSoon(int bufferMinutes = 5) =>
        DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-bufferMinutes);
}
