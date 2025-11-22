// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Standard JWT claims included in access tokens issued by the Tenant Service.
/// Used by all Sorcha services for authorization decisions.
/// </summary>
public class TokenClaims
{
    /// <summary>
    /// Subject (user or service principal ID).
    /// Standard JWT "sub" claim.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Organization ID (null for public identities and service principals).
    /// Custom claim: "org_id".
    /// </summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>
    /// User email (null for service principals).
    /// Standard JWT "email" claim.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// User display name (null for service principals).
    /// Standard JWT "name" claim.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// User roles within organization (e.g., "Administrator", "Auditor", "Member").
    /// Empty array for public identities.
    /// Custom claim: "roles".
    /// </summary>
    public string[] Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Blockchain IDs this user/organization can access.
    /// Empty array if no blockchain access.
    /// Custom claim: "permitted_blockchains".
    /// </summary>
    public Guid[] PermittedBlockchains { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Whether this identity can create new blockchains.
    /// Custom claim: "can_create_blockchain".
    /// </summary>
    public bool CanCreateBlockchain { get; init; }

    /// <summary>
    /// Whether this identity can publish blueprints.
    /// Custom claim: "can_publish_blueprint".
    /// </summary>
    public bool CanPublishBlueprint { get; init; }

    /// <summary>
    /// Token type ("user", "service", "public").
    /// Custom claim: "token_type".
    /// </summary>
    public required string TokenType { get; init; }

    /// <summary>
    /// Issuer (Tenant Service URL).
    /// Standard JWT "iss" claim.
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// Audience (which services can consume this token).
    /// Standard JWT "aud" claim.
    /// </summary>
    public string[] Audience { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Token expiration time (Unix timestamp).
    /// Standard JWT "exp" claim.
    /// </summary>
    public long ExpiresAt { get; init; }

    /// <summary>
    /// Token issued at time (Unix timestamp).
    /// Standard JWT "iat" claim.
    /// </summary>
    public long IssuedAt { get; init; }

    /// <summary>
    /// JWT ID (unique token identifier for revocation tracking).
    /// Standard JWT "jti" claim.
    /// </summary>
    public required string TokenId { get; init; }
}
