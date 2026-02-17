// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Repositories;
using Sorcha.Blueprint.Schemas.Services;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Services;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Tests;

public class SchemaIndexServiceTests
{
    private readonly Mock<ISchemaIndexRepository> _repoMock;
    private readonly Mock<IExternalSchemaProvider> _providerMock;
    private readonly SchemaIndexService _service;

    public SchemaIndexServiceTests()
    {
        _repoMock = new Mock<ISchemaIndexRepository>();
        _providerMock = new Mock<IExternalSchemaProvider>();
        _providerMock.Setup(p => p.ProviderName).Returns("TestProvider");

        var logger = new Mock<ILogger<SchemaIndexService>>();
        _service = new SchemaIndexService(
            _repoMock.Object,
            [_providerMock.Object],
            logger.Object);
    }

    [Fact]
    public async Task SearchAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.SearchAsync(
            It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<string?>(),
            It.IsAny<SchemaIndexStatus?>(), It.IsAny<int>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaIndexSearchResult([], 0, null));

        var result = await _service.SearchAsync(search: "test");

        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_IncludesLoadingProviders()
    {
        _repoMock.Setup(r => r.SearchAsync(
            It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<string?>(),
            It.IsAny<SchemaIndexStatus?>(), It.IsAny<int>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaIndexSearchResult([], 0, null));

        _service.SetProviderLoading("TestProvider", true);

        var result = await _service.SearchAsync();

        result.LoadingProviders.Should().Contain("TestProvider");
    }

    [Fact]
    public async Task GetByProviderAndUriAsync_ReturnsDetailWithContent()
    {
        var doc = CreateTestDocument();
        _repoMock.Setup(r => r.GetByProviderAndUriAsync("TestProvider", "http://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var content = JsonDocument.Parse("""{ "type": "object" }""");
        _providerMock.Setup(p => p.GetSchemaAsync("http://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sorcha.Blueprint.Schemas.DTOs.ExternalSchemaResult(
                "Test", "Desc", "http://example.com", "TestProvider", Content: content));

        var result = await _service.GetByProviderAndUriAsync("TestProvider", "http://example.com");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Schema");
        result.SourceProvider.Should().Be("TestProvider");
    }

    [Fact]
    public async Task GetByProviderAndUriAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByProviderAndUriAsync("Unknown", "uri", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaIndexEntryDocument?)null);

        var result = await _service.GetByProviderAndUriAsync("Unknown", "uri");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertFromProviderAsync_NormalisesAndIndexes()
    {
        var schema = JsonDocument.Parse("""
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                },
                "required": ["name"]
            }
            """);

        _repoMock.Setup(r => r.BatchUpsertAsync(It.IsAny<IEnumerable<SchemaIndexEntryDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.GetCountByProviderAsync("TestProvider", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var entries = new List<ProviderSchemaEntry>
        {
            new("http://example.com/schema", "Test Schema", "A test schema",
                ["general"], "1.0.0", schema)
        };

        var result = await _service.UpsertFromProviderAsync("TestProvider", entries);

        result.Should().Be(1);
        _repoMock.Verify(r => r.BatchUpsertAsync(
            It.Is<IEnumerable<SchemaIndexEntryDocument>>(docs =>
                docs.Any(d => d.FieldCount == 2 && d.ContentHash != null)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertFromProviderAsync_UpdatesProviderStatus()
    {
        _repoMock.Setup(r => r.BatchUpsertAsync(It.IsAny<IEnumerable<SchemaIndexEntryDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _repoMock.Setup(r => r.GetCountByProviderAsync("TestProvider", It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var entries = new List<ProviderSchemaEntry>
        {
            new("uri", "Title", null, ["general"], "1.0", JsonDocument.Parse("""{ "type": "object" }"""))
        };

        await _service.UpsertFromProviderAsync("TestProvider", entries);

        var statuses = _service.GetProviderStatuses();
        var testStatus = statuses.FirstOrDefault(s => s.ProviderName == "TestProvider");
        testStatus.Should().NotBeNull();
        testStatus!.SchemaCount.Should().Be(10);
        testStatus.HealthStatus.Should().Be("Healthy");
    }

    [Fact]
    public void RecordProviderFailure_IncreasesConsecutiveFailures()
    {
        _service.RecordProviderFailure("TestProvider", "Connection refused");

        var statuses = _service.GetProviderStatuses();
        var status = statuses.First(s => s.ProviderName == "TestProvider");
        status.LastError.Should().Be("Connection refused");
        status.HealthStatus.Should().Be("Degraded");
    }

    [Fact]
    public void RecordProviderFailure_ThreeFailures_MarkUnavailable()
    {
        _service.RecordProviderFailure("TestProvider", "Error 1");
        _service.RecordProviderFailure("TestProvider", "Error 2");
        _service.RecordProviderFailure("TestProvider", "Error 3");

        var statuses = _service.GetProviderStatuses();
        var status = statuses.First(s => s.ProviderName == "TestProvider");
        status.HealthStatus.Should().Be("Unavailable");
    }

    [Fact]
    public void IsProviderInBackoff_ReturnsTrueAfterFailure()
    {
        _service.RecordProviderFailure("TestProvider", "Error");

        _service.IsProviderInBackoff("TestProvider").Should().BeTrue();
    }

    [Fact]
    public void GetLoadingProviders_ReturnsEmptyByDefault()
    {
        _service.GetLoadingProviders().Should().BeEmpty();
    }

    [Fact]
    public void RegisterProvider_AddsNewProvider()
    {
        _service.RegisterProvider("NewProvider", ProviderType.StaticFile, 1.0);

        var statuses = _service.GetProviderStatuses();
        statuses.Should().Contain(s => s.ProviderName == "NewProvider");
    }

    private static SchemaIndexEntryDocument CreateTestDocument() => new()
    {
        SourceProvider = "TestProvider",
        SourceUri = "http://example.com",
        Title = "Test Schema",
        Description = "A test schema",
        SectorTags = ["general"],
        FieldCount = 3,
        RequiredFields = ["name"],
        SchemaVersion = "1.0.0"
    };
}
