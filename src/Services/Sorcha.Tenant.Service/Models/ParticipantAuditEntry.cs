// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Audit log entry for participant identity changes.
/// Stored in per-organization schema (org_{organization_id}).
/// Provides tamper-evident logging of all participant-related events.
/// </summary>
public class ParticipantAuditEntry
{
    /// <summary>
    /// Unique audit entry identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Affected participant identifier.
    /// </summary>
    public Guid ParticipantId { get; set; }

    /// <summary>
    /// Action type performed (Created, Updated, WalletLinked, WalletRevoked, etc.).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// User or service ID that performed the action.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Type of actor (User, Admin, System).
    /// </summary>
    public string ActorType { get; set; } = string.Empty;

    /// <summary>
    /// Action timestamp (UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Previous state as JSON document. Null for creation events.
    /// </summary>
    public JsonDocument? OldValues { get; set; }

    /// <summary>
    /// New state as JSON document. Null for deletion events.
    /// </summary>
    public JsonDocument? NewValues { get; set; }

    /// <summary>
    /// Client IP address (IPv4 or IPv6). Null for system-initiated actions.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Navigation property to the affected participant.
    /// </summary>
    public ParticipantIdentity? Participant { get; set; }
}

/// <summary>
/// Audit action types for participant identity changes.
/// </summary>
public static class ParticipantAuditAction
{
    /// <summary>
    /// Participant was registered.
    /// </summary>
    public const string Created = "Created";

    /// <summary>
    /// Participant display name or email was updated.
    /// </summary>
    public const string Updated = "Updated";

    /// <summary>
    /// Wallet address was linked to participant.
    /// </summary>
    public const string WalletLinked = "WalletLinked";

    /// <summary>
    /// Wallet address link was revoked.
    /// </summary>
    public const string WalletRevoked = "WalletRevoked";

    /// <summary>
    /// Participant status changed to Active.
    /// </summary>
    public const string Activated = "Activated";

    /// <summary>
    /// Participant status changed to Inactive (soft-delete).
    /// </summary>
    public const string Deactivated = "Deactivated";

    /// <summary>
    /// Participant status changed to Suspended.
    /// </summary>
    public const string Suspended = "Suspended";
}
