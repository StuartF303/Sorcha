// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class FhirSchemaProviderTests
{
    private readonly FhirSchemaProvider _provider;

    public FhirSchemaProviderTests()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://hl7.org") };
        var logger = new Mock<ILogger<FhirSchemaProvider>>();
        _provider = new FhirSchemaProvider(httpClient, logger.Object);
    }

    [Fact]
    public void ProviderName_ReturnsHl7Fhir()
    {
        _provider.ProviderName.Should().Be("HL7 FHIR");
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsCuratedResources()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name == "FHIR Patient");
        catalog.Should().Contain(r => r.Name == "FHIR Observation");
        catalog.Should().Contain(r => r.Name == "FHIR Encounter");
    }

    [Fact]
    public async Task GetCatalogAsync_AllHaveDraft202012()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        foreach (var entry in catalog)
        {
            entry.Content.Should().NotBeNull();
            entry.Content!.RootElement.GetProperty("$schema").GetString()
                .Should().Be("https://json-schema.org/draft/2020-12/schema");
        }
    }

    [Fact]
    public async Task SearchAsync_Patient_ReturnsResult()
    {
        var result = await _provider.SearchAsync("Patient");

        result.Results.Should().Contain(r => r.Name.Contains("Patient"));
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsResource()
    {
        var result = await _provider.GetSchemaAsync("https://hl7.org/fhir/StructureDefinition/Patient");

        result.Should().NotBeNull();
        result!.Name.Should().Be("FHIR Patient");
    }

    [Fact]
    public void BuildResourceSchema_HasResourceTypeRequired()
    {
        var schema = FhirSchemaProvider.BuildResourceSchema("Patient");

        var root = schema.RootElement;
        root.GetProperty("properties").GetProperty("resourceType")
            .GetProperty("const").GetString().Should().Be("Patient");

        var required = root.GetProperty("required");
        required.EnumerateArray().Should().Contain(e => e.GetString() == "resourceType");
    }
}
