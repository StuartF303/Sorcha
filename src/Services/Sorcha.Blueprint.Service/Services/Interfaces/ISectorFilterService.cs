// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for managing organisation-specific sector visibility preferences.
/// </summary>
public interface ISectorFilterService
{
    /// <summary>
    /// Gets the sector preferences for an organisation.
    /// Returns all-enabled by default if no preferences exist.
    /// </summary>
    Task<OrganisationSectorPreferencesDto> GetPreferencesAsync(
        string organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the sector preferences for an organisation.
    /// Pass null to enable all sectors.
    /// </summary>
    Task<OrganisationSectorPreferencesDto> UpdatePreferencesAsync(
        string organizationId,
        string[]? enabledSectors,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of enabled sector IDs for an organisation.
    /// Returns null if all sectors are enabled.
    /// </summary>
    Task<string[]?> GetEnabledSectorsAsync(
        string organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters a search response by the organisation's enabled sectors.
    /// </summary>
    Task<SchemaIndexSearchResponse> FilterBySectorsAsync(
        string organizationId,
        SchemaIndexSearchResponse searchResponse,
        CancellationToken cancellationToken = default);
}
