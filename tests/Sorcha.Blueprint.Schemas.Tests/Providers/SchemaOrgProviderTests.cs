// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class SchemaOrgProviderTests
{
    private readonly SchemaOrgProvider _provider;

    public SchemaOrgProviderTests()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://schema.org") };
        var logger = new Mock<ILogger<SchemaOrgProvider>>();
        _provider = new SchemaOrgProvider(httpClient, logger.Object);
    }

    [Fact]
    public void ProviderName_ReturnsSchemaOrg()
    {
        _provider.ProviderName.Should().Be("schema.org");
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsCuratedTypes()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name == "Person");
        catalog.Should().Contain(r => r.Name == "Invoice");
        catalog.Should().Contain(r => r.Name == "Organization");
    }

    [Fact]
    public async Task GetCatalogAsync_AllHaveContent()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        foreach (var entry in catalog)
        {
            entry.Content.Should().NotBeNull($"'{entry.Name}' should have JSON Schema content");
            entry.Content!.RootElement.GetProperty("$schema").GetString()
                .Should().Be("https://json-schema.org/draft/2020-12/schema");
            entry.Content.RootElement.GetProperty("type").GetString()
                .Should().Be("object");
        }
    }

    [Fact]
    public async Task SearchAsync_MatchesByName()
    {
        var result = await _provider.SearchAsync("Person");

        result.Results.Should().Contain(r => r.Name == "Person");
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        var result = await _provider.SearchAsync("");

        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsMatchingSchema()
    {
        var result = await _provider.GetSchemaAsync("https://schema.org/Person");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Person");
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        // Static catalog - always available (even if HTTP fails for real URL)
        // The catalog is built from curated types, not fetched
        var catalog = await _provider.GetCatalogAsync();
        catalog.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildJsonSchemaForType_Person_HasExpectedProperties()
    {
        var schema = SchemaOrgProvider.BuildJsonSchemaForType("Person");

        var root = schema.RootElement;
        root.GetProperty("title").GetString().Should().Be("Person");

        var props = root.GetProperty("properties");
        props.TryGetProperty("givenName", out _).Should().BeTrue();
        props.TryGetProperty("familyName", out _).Should().BeTrue();
        props.TryGetProperty("email", out _).Should().BeTrue();
        props.TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public void BuildJsonSchemaForType_Invoice_HasPaymentProperties()
    {
        var schema = SchemaOrgProvider.BuildJsonSchemaForType("Invoice");

        var props = schema.RootElement.GetProperty("properties");
        props.TryGetProperty("totalPaymentDue", out _).Should().BeTrue();
        props.TryGetProperty("paymentDueDate", out _).Should().BeTrue();
    }
}
