// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Represents a user's participant status within an organization.
/// Stored in per-organization schema (org_{organization_id}).
/// Links User (Tenant) + Organization for workflow participation.
/// </summary>
public class ParticipantIdentity
{
    /// <summary>
    /// Unique participant identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to Tenant Service user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Organization this participant belongs to (denormalized for queries).
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Display name for participant contexts.
    /// May differ from the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Email address (copied from user for search optimization).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Participant status (Active, Inactive, Suspended).
    /// </summary>
    public ParticipantIdentityStatus Status { get; set; } = ParticipantIdentityStatus.Active;

    /// <summary>
    /// Registration timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last modification timestamp (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Deactivation timestamp (UTC). Null if active.
    /// Used for soft-delete to preserve audit trail.
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// Navigation property to linked wallet addresses.
    /// </summary>
    public ICollection<LinkedWalletAddress> LinkedWalletAddresses { get; set; } = new List<LinkedWalletAddress>();

    /// <summary>
    /// Navigation property to audit entries.
    /// </summary>
    public ICollection<ParticipantAuditEntry> AuditEntries { get; set; } = new List<ParticipantAuditEntry>();

    /// <summary>
    /// Navigation property to wallet link challenges.
    /// </summary>
    public ICollection<WalletLinkChallenge> WalletLinkChallenges { get; set; } = new List<WalletLinkChallenge>();
}
