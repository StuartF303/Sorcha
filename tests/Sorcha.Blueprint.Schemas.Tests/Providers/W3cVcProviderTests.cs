// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class W3cVcProviderTests
{
    private readonly W3cVcProvider _provider;

    public W3cVcProviderTests()
    {
        _provider = new W3cVcProvider(new Mock<ILogger<W3cVcProvider>>().Object);
    }

    [Fact]
    public void ProviderName_ReturnsW3cVc()
    {
        _provider.ProviderName.Should().Be("W3C VC");
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsVcAndVpSchemas()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        catalog.Should().HaveCount(3);
        catalog.Should().Contain(r => r.Name == "Verifiable Credential");
        catalog.Should().Contain(r => r.Name == "Verifiable Presentation");
        catalog.Should().Contain(r => r.Name == "Credential Subject");
    }

    [Fact]
    public async Task GetCatalogAsync_VcHasRequiredFields()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();
        var vc = catalog.First(r => r.Name == "Verifiable Credential");

        vc.Content.Should().NotBeNull();
        var required = vc.Content!.RootElement.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString()).Should()
            .Contain(["@context", "type", "issuer", "credentialSubject"]);
    }

    [Fact]
    public async Task SearchAsync_Credential_ReturnsResults()
    {
        var result = await _provider.SearchAsync("Credential");

        result.Results.Should().HaveCountGreaterThan(0);
        result.Results.Should().Contain(r => r.Name.Contains("Credential"));
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        var result = await _provider.IsAvailableAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsVc()
    {
        var result = await _provider.GetSchemaAsync("https://www.w3.org/ns/credentials/v2#VerifiableCredential");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Verifiable Credential");
    }
}
