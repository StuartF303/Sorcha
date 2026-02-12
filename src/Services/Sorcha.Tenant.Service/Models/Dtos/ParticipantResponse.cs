// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Response model for participant identity summary.
/// Used in list views and search results.
/// </summary>
public record ParticipantResponse
{
    /// <summary>
    /// Unique participant identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User ID from Tenant Service.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Organization identifier.
    /// </summary>
    public Guid OrganizationId { get; init; }

    /// <summary>
    /// Display name for the participant.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Participant status.
    /// </summary>
    public ParticipantIdentityStatus Status { get; init; }

    /// <summary>
    /// Whether the participant has at least one active linked wallet.
    /// </summary>
    public bool HasLinkedWallet { get; init; }

    /// <summary>
    /// Registration timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
