// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class StaticFileSchemaProviderTests
{
    [Fact]
    public async Task NiemProvider_ReturnsCuratedSchemas()
    {
        var logger = new Mock<ILogger<StaticFileSchemaProvider>>();
        var provider = StaticFileSchemaProvider.CreateNiemProvider(logger.Object);

        provider.ProviderName.Should().Be("NIEM");

        var catalog = (await provider.GetCatalogAsync()).ToList();
        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name == "NIEM Person");
        catalog.Should().Contain(r => r.Name == "NIEM Case");
    }

    [Fact]
    public async Task IfcProvider_ReturnsCuratedSchemas()
    {
        var logger = new Mock<ILogger<StaticFileSchemaProvider>>();
        var provider = StaticFileSchemaProvider.CreateIfcProvider(logger.Object);

        provider.ProviderName.Should().Be("IFC");

        var catalog = (await provider.GetCatalogAsync()).ToList();
        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name == "IFC Building");
        catalog.Should().Contain(r => r.Name == "IFC Project");
    }

    [Fact]
    public async Task SearchAsync_NiemPerson_ReturnsResult()
    {
        var logger = new Mock<ILogger<StaticFileSchemaProvider>>();
        var provider = StaticFileSchemaProvider.CreateNiemProvider(logger.Object);

        var result = await provider.SearchAsync("Person");

        result.Results.Should().Contain(r => r.Name.Contains("Person"));
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        var logger = new Mock<ILogger<StaticFileSchemaProvider>>();
        var provider = StaticFileSchemaProvider.CreateNiemProvider(logger.Object);

        var result = await provider.IsAvailableAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AllSchemas_HaveValidContent()
    {
        var logger = new Mock<ILogger<StaticFileSchemaProvider>>();
        var provider = StaticFileSchemaProvider.CreateNiemProvider(logger.Object);

        var catalog = (await provider.GetCatalogAsync()).ToList();
        foreach (var entry in catalog)
        {
            entry.Content.Should().NotBeNull();
            entry.Content!.RootElement.GetProperty("type").GetString()
                .Should().Be("object");
        }
    }
}
