// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request to login with email and password.
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// User email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User password.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Optional organization subdomain.
    /// If not provided, will look up by email domain or use default organization.
    /// </summary>
    public string? OrganizationSubdomain { get; init; }
}

/// <summary>
/// Request to refresh an access token.
/// </summary>
public record TokenRefreshRequest
{
    /// <summary>
    /// The refresh token.
    /// </summary>
    public required string RefreshToken { get; init; }
}

/// <summary>
/// Response containing access and refresh tokens.
/// </summary>
public record TokenResponse
{
    /// <summary>
    /// JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Access token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Token scope.
    /// </summary>
    public string? Scope { get; init; }
}

/// <summary>
/// Request to revoke a token.
/// </summary>
public record TokenRevocationRequest
{
    /// <summary>
    /// The token to revoke.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Type of token being revoked (access_token or refresh_token).
    /// </summary>
    public string TokenTypeHint { get; init; } = "refresh_token";
}

/// <summary>
/// Request to introspect a token.
/// </summary>
public record TokenIntrospectionRequest
{
    /// <summary>
    /// The token to introspect.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Type hint for the token.
    /// </summary>
    public string? TokenTypeHint { get; init; }
}

/// <summary>
/// Token introspection response (RFC 7662).
/// </summary>
public record TokenIntrospectionResponse
{
    /// <summary>
    /// Whether the token is active.
    /// </summary>
    public bool Active { get; init; }

    /// <summary>
    /// Token scope.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Client ID that requested the token.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Subject (user ID) of the token.
    /// </summary>
    public string? Sub { get; init; }

    /// <summary>
    /// Token expiration time (Unix timestamp).
    /// </summary>
    public long? Exp { get; init; }

    /// <summary>
    /// Token issued at time (Unix timestamp).
    /// </summary>
    public long? Iat { get; init; }

    /// <summary>
    /// Token issuer.
    /// </summary>
    public string? Iss { get; init; }

    /// <summary>
    /// Token audience.
    /// </summary>
    public string? Aud { get; init; }

    /// <summary>
    /// Token type (Bearer).
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// JWT ID.
    /// </summary>
    public string? Jti { get; init; }

    /// <summary>
    /// Organization ID claim.
    /// </summary>
    public string? OrgId { get; init; }

    /// <summary>
    /// User roles.
    /// </summary>
    public string[]? Roles { get; init; }
}

/// <summary>
/// Request to revoke all tokens for a user.
/// </summary>
public record RevokeUserTokensRequest
{
    /// <summary>
    /// User ID whose tokens should be revoked.
    /// </summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// Request to revoke all tokens for an organization.
/// </summary>
public record RevokeOrganizationTokensRequest
{
    /// <summary>
    /// Organization ID whose tokens should be revoked.
    /// </summary>
    public required Guid OrganizationId { get; init; }
}

/// <summary>
/// Current authenticated user information.
/// </summary>
public record CurrentUserResponse
{
    /// <summary>
    /// User ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// User email.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Organization ID.
    /// </summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public string? OrganizationName { get; init; }

    /// <summary>
    /// User roles.
    /// </summary>
    public string[] Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Token type (user, service, etc.).
    /// </summary>
    public string? TokenType { get; init; }
}

/// <summary>
/// Generic success response.
/// </summary>
public record SuccessResponse
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Optional message.
    /// </summary>
    public string? Message { get; init; }
}
