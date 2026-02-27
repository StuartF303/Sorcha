// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a cached authentication token entry.
/// </summary>
public class TokenCacheEntry
{
    /// <summary>
    /// Access token (JWT).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token (if available).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration timestamp (UTC).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Profile name this token is associated with.
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Username or client ID that obtained this token.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Checks if the token is expiring soon (within specified buffer).
    /// </summary>
    /// <param name="bufferMinutes">Buffer time in minutes</param>
    public bool IsExpiringSoon(int bufferMinutes = 5)
    {
        return DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-bufferMinutes);
    }
}
