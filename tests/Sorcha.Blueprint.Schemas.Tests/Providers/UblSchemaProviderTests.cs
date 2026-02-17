// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class UblSchemaProviderTests
{
    private readonly UblSchemaProvider _provider;

    public UblSchemaProviderTests()
    {
        _provider = new UblSchemaProvider(new Mock<ILogger<UblSchemaProvider>>().Object);
    }

    [Fact]
    public void ProviderName_ReturnsOasisUbl()
    {
        _provider.ProviderName.Should().Be("OASIS UBL");
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsCuratedDocuments()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name == "UBL Invoice");
        catalog.Should().Contain(r => r.Name == "UBL Order");
        catalog.Should().Contain(r => r.Name == "UBL CreditNote");
    }

    [Fact]
    public async Task SearchAsync_Invoice_ReturnsResult()
    {
        var result = await _provider.SearchAsync("Invoice");

        result.Results.Should().Contain(r => r.Name.Contains("Invoice"));
    }

    [Fact]
    public void BuildDocumentSchema_HasRequiredFields()
    {
        var schema = UblSchemaProvider.BuildDocumentSchema("Invoice", "A commercial invoice");

        var root = schema.RootElement;
        root.GetProperty("title").GetString().Should().Be("UBL Invoice");

        var required = root.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString()).Should()
            .Contain(["ID", "IssueDate"]);
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsDocument()
    {
        var result = await _provider.GetSchemaAsync("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");

        result.Should().NotBeNull();
        result!.Name.Should().Be("UBL Invoice");
    }
}
