// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Comprehensive audit trail entry for authentication and authorization events.
/// Stored in per-organization schema (org_{organization_id}) for data isolation.
/// Provides tamper-evident logging of all security-relevant events.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Auto-incrementing log entry ID (per organization schema).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Event timestamp (UTC).
    /// Indexed for efficient time-range queries.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Type of audit event (Login, TokenIssued, PermissionDenied, etc.).
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// User or service identity ID associated with this event.
    /// Null for failed authentication attempts (unknown identity).
    /// </summary>
    public Guid? IdentityId { get; set; }

    /// <summary>
    /// Organization context for this event.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Client IP address (IPv4 or IPv6).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Client user agent string (browser, app, service name).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the event succeeded or failed.
    /// True for successful operations, false for denied/failed operations.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Additional event-specific details (JSONB in PostgreSQL).
    /// Examples: token JTI, error messages, requested resource, IDP name.
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Types of auditable events in the Tenant Service.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// User successfully logged in via external IDP or PassKey.
    /// </summary>
    Login,

    /// <summary>
    /// User explicitly logged out.
    /// </summary>
    Logout,

    /// <summary>
    /// New JWT access token was issued.
    /// </summary>
    TokenIssued,

    /// <summary>
    /// JWT access token was refreshed using refresh token.
    /// </summary>
    TokenRefreshed,

    /// <summary>
    /// JWT token was explicitly revoked (logout, security incident).
    /// </summary>
    TokenRevoked,

    /// <summary>
    /// JWT token was validated by another service.
    /// </summary>
    TokenValidated,

    /// <summary>
    /// User was denied access to a resource due to insufficient permissions.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Organization's IDP configuration was created or updated.
    /// </summary>
    IdpConfigurationUpdated,

    /// <summary>
    /// Organization's permission configuration was updated.
    /// </summary>
    OrganizationPermissionsUpdated,

    /// <summary>
    /// New PassKey was registered for a user.
    /// </summary>
    PassKeyRegistered,

    /// <summary>
    /// User authenticated using PassKey.
    /// </summary>
    PassKeyAuthentication,

    /// <summary>
    /// Organization was created by an administrator.
    /// </summary>
    OrganizationCreated,

    /// <summary>
    /// Organization details were updated by an administrator.
    /// </summary>
    OrganizationUpdated,

    /// <summary>
    /// Organization was deactivated (soft-deleted) by an administrator.
    /// </summary>
    OrganizationDeactivated,

    /// <summary>
    /// User was added to an organization by an administrator.
    /// </summary>
    UserAddedToOrganization,

    /// <summary>
    /// User details or roles were updated by an administrator.
    /// </summary>
    UserUpdatedInOrganization,

    /// <summary>
    /// User was removed from an organization by an administrator.
    /// </summary>
    UserRemovedFromOrganization
}
