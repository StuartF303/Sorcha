// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services;

/// <summary>
/// In-memory implementation of sector filter preferences.
/// Default: all sectors enabled when no preferences exist.
/// </summary>
public class SectorFilterService : ISectorFilterService
{
    private readonly ConcurrentDictionary<string, OrganisationSectorPreferencesDto> _preferences = new();
    private readonly ILogger<SectorFilterService> _logger;

    public SectorFilterService(ILogger<SectorFilterService> logger)
    {
        _logger = logger;
    }

    public Task<OrganisationSectorPreferencesDto> GetPreferencesAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        if (_preferences.TryGetValue(organizationId, out var prefs))
        {
            return Task.FromResult(prefs);
        }

        // Default: all sectors enabled
        return Task.FromResult(new OrganisationSectorPreferencesDto(
            organizationId,
            null,
            AllSectorsEnabled: true,
            LastModifiedAt: null));
    }

    public Task<OrganisationSectorPreferencesDto> UpdatePreferencesAsync(
        string organizationId,
        string[]? enabledSectors,
        CancellationToken cancellationToken = default)
    {
        // Validate sector IDs
        if (enabledSectors is not null)
        {
            var invalidSectors = enabledSectors.Where(s => !SchemaSector.IsValid(s)).ToArray();
            if (invalidSectors.Length > 0)
            {
                throw new ArgumentException($"Invalid sector IDs: {string.Join(", ", invalidSectors)}");
            }
        }

        var allEnabled = enabledSectors is null;
        var dto = new OrganisationSectorPreferencesDto(
            organizationId,
            enabledSectors,
            allEnabled,
            DateTimeOffset.UtcNow);

        _preferences[organizationId] = dto;
        _logger.LogInformation("Updated sector preferences for org {OrgId}: {Sectors}",
            organizationId,
            allEnabled ? "all" : string.Join(", ", enabledSectors!));

        return Task.FromResult(dto);
    }

    public Task<string[]?> GetEnabledSectorsAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        if (_preferences.TryGetValue(organizationId, out var prefs) && !prefs.AllSectorsEnabled)
        {
            return Task.FromResult(prefs.EnabledSectors);
        }

        return Task.FromResult<string[]?>(null); // All enabled
    }

    public Task<SchemaIndexSearchResponse> FilterBySectorsAsync(
        string organizationId,
        SchemaIndexSearchResponse searchResponse,
        CancellationToken cancellationToken = default)
    {
        if (!_preferences.TryGetValue(organizationId, out var prefs) || prefs.AllSectorsEnabled)
        {
            return Task.FromResult(searchResponse); // No filtering needed
        }

        var enabledSet = new HashSet<string>(prefs.EnabledSectors ?? [], StringComparer.OrdinalIgnoreCase);

        var filtered = searchResponse.Results
            .Where(r => r.SectorTags.Any(t => enabledSet.Contains(t)))
            .ToList();

        return Task.FromResult(new SchemaIndexSearchResponse(
            filtered,
            filtered.Count,
            searchResponse.NextCursor,
            searchResponse.LoadingProviders));
    }
}
