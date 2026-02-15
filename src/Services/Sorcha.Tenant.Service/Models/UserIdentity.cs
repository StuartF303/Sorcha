// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Authenticated user within an organization.
/// Stored in per-organization schema (org_{organization_id}).
/// </summary>
public class UserIdentity
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Organization membership (denormalized for queries).
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User ID from external IDP (sub claim from OIDC token).
    /// Must be unique within organization.
    /// Null for local authentication users.
    /// </summary>
    public string? ExternalIdpUserId { get; set; }

    /// <summary>
    /// Password hash for local authentication (BCrypt).
    /// Null for external IDP users (Azure AD/B2C).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// User email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User display name (friendly name shown in UI).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User roles within organization (Administrator, Auditor, Member).
    /// Organization creator automatically gets Administrator role.
    /// </summary>
    public UserRole[] Roles { get; set; } = new[] { UserRole.Member };

    /// <summary>
    /// User account status (Active, Suspended, Deleted).
    /// </summary>
    public IdentityStatus Status { get; set; } = IdentityStatus.Active;

    /// <summary>
    /// User creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last successful login timestamp (UTC). Null if never logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}

/// <summary>
/// User roles within an organization.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Full administrative access to organization settings, users, and permissions.
    /// </summary>
    Administrator,

    /// <summary>
    /// System administrator with elevated privileges across all organizations.
    /// </summary>
    SystemAdmin,

    /// <summary>
    /// Blueprint designer who can create and manage workflow definitions.
    /// </summary>
    Designer,

    /// <summary>
    /// Developer with access to API documentation and developer tools.
    /// </summary>
    Developer,

    /// <summary>
    /// Standard user who participates in workflows.
    /// </summary>
    User,

    /// <summary>
    /// Consumer of workflows, similar to User but with workflow execution focus.
    /// </summary>
    Consumer,

    /// <summary>
    /// Read-only access to audit logs and organization activity.
    /// </summary>
    Auditor,

    /// <summary>
    /// Standard member with permissions defined by organization policy.
    /// </summary>
    Member
}

/// <summary>
/// User account status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IdentityStatus
{
    /// <summary>
    /// User account is active and can authenticate.
    /// </summary>
    Active,

    /// <summary>
    /// User account is temporarily suspended (cannot authenticate).
    /// </summary>
    Suspended,

    /// <summary>
    /// User account is soft-deleted (can be restored within 30 days).
    /// </summary>
    Deleted
}
