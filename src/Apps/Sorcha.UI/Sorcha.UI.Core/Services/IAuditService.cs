// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Client-side audit service for logging administrative actions.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="eventType">Type of audit event.</param>
    /// <param name="details">Event-specific details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(
        AuditEventType eventType,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an organization-related audit event.
    /// </summary>
    /// <param name="eventType">Type of audit event.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <param name="details">Event-specific details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogOrganizationEventAsync(
        AuditEventType eventType,
        Guid organizationId,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a user-related audit event.
    /// </summary>
    /// <param name="eventType">Type of audit event.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="details">Event-specific details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUserEventAsync(
        AuditEventType eventType,
        Guid organizationId,
        Guid userId,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of audit events for admin operations.
/// Mirrors the backend AuditEventType enum with admin-specific additions.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// Organization was created.
    /// </summary>
    OrganizationCreated,

    /// <summary>
    /// Organization details were updated.
    /// </summary>
    OrganizationUpdated,

    /// <summary>
    /// Organization was deactivated.
    /// </summary>
    OrganizationDeactivated,

    /// <summary>
    /// User was added to an organization.
    /// </summary>
    UserAddedToOrganization,

    /// <summary>
    /// User details or roles were updated.
    /// </summary>
    UserUpdatedInOrganization,

    /// <summary>
    /// User was removed from an organization.
    /// </summary>
    UserRemovedFromOrganization,

    /// <summary>
    /// Admin dashboard was accessed.
    /// </summary>
    AdminDashboardAccessed,

    /// <summary>
    /// Health dashboard was refreshed.
    /// </summary>
    HealthCheckRefreshed
}
