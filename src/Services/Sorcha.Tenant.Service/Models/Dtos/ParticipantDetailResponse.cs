// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Response model for detailed participant identity information.
/// Includes linked wallet addresses.
/// </summary>
public record ParticipantDetailResponse
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
    /// Registration timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Deactivation timestamp (if applicable).
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; init; }

    /// <summary>
    /// Active linked wallet addresses.
    /// </summary>
    public List<LinkedWalletAddressResponse> LinkedWallets { get; init; } = new();
}
