// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Services;

/// <summary>
/// Service for managing token revocation using Redis blacklist.
/// </summary>
public interface ITokenRevocationService
{
    /// <summary>
    /// Revokes a token by adding its JTI to the blacklist.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) of the token to revoke.</param>
    /// <param name="expiresAt">When the token would have expired (for TTL calculation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeTokenAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token has been revoked.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the token is revoked, false otherwise.</returns>
    Task<bool> IsTokenRevokedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllOrganizationTokensAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a token issuance for potential bulk revocation.
    /// </summary>
    /// <param name="jti">The JWT ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="organizationId">The organization ID (optional for public users).</param>
    /// <param name="expiresAt">Token expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackTokenAsync(string jti, string userId, string? organizationId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the failed authentication attempt counter for rate limiting.
    /// </summary>
    /// <param name="identifier">User identifier or IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current number of failed attempts.</returns>
    Task<int> IncrementFailedAuthAttemptsAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the identifier is rate-limited due to failed attempts.
    /// </summary>
    /// <param name="identifier">User identifier or IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rate-limited, false otherwise.</returns>
    Task<bool> IsRateLimitedAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed authentication counter (after successful login).
    /// </summary>
    /// <param name="identifier">User identifier or IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetFailedAuthAttemptsAsync(string identifier, CancellationToken cancellationToken = default);
}
