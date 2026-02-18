// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Services;

namespace Sorcha.Blueprint.Service.Tests;

public class SectorFilterServiceTests
{
    private readonly SectorFilterService _service;

    public SectorFilterServiceTests()
    {
        var logger = new Mock<ILogger<SectorFilterService>>();
        _service = new SectorFilterService(logger.Object);
    }

    [Fact]
    public async Task GetPreferencesAsync_NoPreferences_ReturnsAllEnabled()
    {
        var result = await _service.GetPreferencesAsync("org-1");

        result.Should().NotBeNull();
        result.OrganizationId.Should().Be("org-1");
        result.AllSectorsEnabled.Should().BeTrue();
        result.EnabledSectors.Should().BeNull();
        result.LastModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithSectors_StoresPreferences()
    {
        var sectors = new[] { "finance", "healthcare" };

        var result = await _service.UpdatePreferencesAsync("org-1", sectors);

        result.OrganizationId.Should().Be("org-1");
        result.EnabledSectors.Should().BeEquivalentTo(sectors);
        result.AllSectorsEnabled.Should().BeFalse();
        result.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithNull_SetsAllEnabled()
    {
        await _service.UpdatePreferencesAsync("org-1", new[] { "finance" });

        var result = await _service.UpdatePreferencesAsync("org-1", null);

        result.AllSectorsEnabled.Should().BeTrue();
        result.EnabledSectors.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_InvalidSector_ThrowsArgument()
    {
        var act = () => _service.UpdatePreferencesAsync("org-1", new[] { "invalid-sector" });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalid-sector*");
    }

    [Fact]
    public async Task GetPreferencesAsync_AfterUpdate_ReturnsSavedPreferences()
    {
        var sectors = new[] { "commerce", "technology" };
        await _service.UpdatePreferencesAsync("org-1", sectors);

        var result = await _service.GetPreferencesAsync("org-1");

        result.EnabledSectors.Should().BeEquivalentTo(sectors);
        result.AllSectorsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetEnabledSectorsAsync_NoPreferences_ReturnsNull()
    {
        var result = await _service.GetEnabledSectorsAsync("org-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEnabledSectorsAsync_WithPreferences_ReturnsSectors()
    {
        var sectors = new[] { "finance", "identity" };
        await _service.UpdatePreferencesAsync("org-1", sectors);

        var result = await _service.GetEnabledSectorsAsync("org-1");

        result.Should().BeEquivalentTo(sectors);
    }

    [Fact]
    public async Task GetEnabledSectorsAsync_AllEnabled_ReturnsNull()
    {
        await _service.UpdatePreferencesAsync("org-1", null);

        var result = await _service.GetEnabledSectorsAsync("org-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FilterBySectorsAsync_NoPreferences_ReturnsUnchanged()
    {
        var response = CreateSearchResponse("finance", "healthcare");

        var result = await _service.FilterBySectorsAsync("org-1", response);

        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterBySectorsAsync_WithFilter_FiltersResults()
    {
        await _service.UpdatePreferencesAsync("org-1", new[] { "finance" });

        var response = CreateSearchResponse("finance", "healthcare");

        var result = await _service.FilterBySectorsAsync("org-1", response);

        result.Results.Should().HaveCount(1);
        result.Results[0].SectorTags.Should().Contain("finance");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task FilterBySectorsAsync_EmptyEnabled_FiltersEverything()
    {
        await _service.UpdatePreferencesAsync("org-1", Array.Empty<string>());

        var response = CreateSearchResponse("finance", "healthcare");

        var result = await _service.FilterBySectorsAsync("org-1", response);

        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task FilterBySectorsAsync_MultiSectorEntry_MatchesAny()
    {
        await _service.UpdatePreferencesAsync("org-1", new[] { "commerce" });

        var entry = new SchemaIndexEntryDto(
            "sc001", "TestProvider", "urn:test:1", "Multi", null,
            new[] { "finance", "commerce" }, 5, 2, "2020-12", "Active", DateTimeOffset.UtcNow);

        var response = new SchemaIndexSearchResponse(new[] { entry }, 1, null, null);

        var result = await _service.FilterBySectorsAsync("org-1", response);

        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task FilterBySectorsAsync_CaseInsensitive_MatchesSectors()
    {
        await _service.UpdatePreferencesAsync("org-1", new[] { "Finance" });

        var entry = new SchemaIndexEntryDto(
            "sc002", "TestProvider", "urn:test:1", "Test", null,
            new[] { "finance" }, 5, 2, "2020-12", "Active", DateTimeOffset.UtcNow);

        var response = new SchemaIndexSearchResponse(new[] { entry }, 1, null, null);

        var result = await _service.FilterBySectorsAsync("org-1", response);

        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task MultipleOrgs_HaveIndependentPreferences()
    {
        await _service.UpdatePreferencesAsync("org-1", new[] { "finance" });
        await _service.UpdatePreferencesAsync("org-2", new[] { "healthcare" });

        var org1 = await _service.GetEnabledSectorsAsync("org-1");
        var org2 = await _service.GetEnabledSectorsAsync("org-2");

        org1.Should().BeEquivalentTo(new[] { "finance" });
        org2.Should().BeEquivalentTo(new[] { "healthcare" });
    }

    private static SchemaIndexSearchResponse CreateSearchResponse(params string[] sectors)
    {
        var results = sectors.Select((s, i) => new SchemaIndexEntryDto(
            $"sc{i:D3}", "TestProvider", $"urn:test:{i}", $"Test {s}", null,
            new[] { s }, 5, 2, "2020-12", "Active", DateTimeOffset.UtcNow))
            .ToList();

        return new SchemaIndexSearchResponse(results, results.Count, null, null);
    }
}
