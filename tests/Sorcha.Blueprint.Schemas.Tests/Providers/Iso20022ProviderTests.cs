// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests.Providers;

public class Iso20022ProviderTests
{
    private readonly Iso20022Provider _provider;

    public Iso20022ProviderTests()
    {
        _provider = new Iso20022Provider(new Mock<ILogger<Iso20022Provider>>().Object);
    }

    [Fact]
    public void ProviderName_ReturnsIso20022()
    {
        _provider.ProviderName.Should().Be("ISO 20022");
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsCuratedMessages()
    {
        var catalog = (await _provider.GetCatalogAsync()).ToList();

        catalog.Should().NotBeEmpty();
        catalog.Should().Contain(r => r.Name.Contains("pacs.008"));
        catalog.Should().Contain(r => r.Name.Contains("pain.001"));
        catalog.Should().Contain(r => r.Name.Contains("camt.053"));
    }

    [Fact]
    public async Task SearchAsync_Payment_ReturnsResults()
    {
        var result = await _provider.SearchAsync("payment");

        result.Results.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SearchAsync_Pacs_ReturnsResults()
    {
        var result = await _provider.SearchAsync("pacs");

        result.Results.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void BuildMessageSchema_PaymentMessage_HasAccountProperties()
    {
        var schema = Iso20022Provider.BuildMessageSchema("pacs.008", "Credit transfer");

        var root = schema.RootElement;
        var props = root.GetProperty("properties");
        props.TryGetProperty("debtorAccount", out _).Should().BeTrue();
        props.TryGetProperty("creditorAccount", out _).Should().BeTrue();
        props.TryGetProperty("amount", out _).Should().BeTrue();
    }

    [Fact]
    public void BuildMessageSchema_StatementMessage_HasBalanceProperties()
    {
        var schema = Iso20022Provider.BuildMessageSchema("camt.053", "Account statement");

        var root = schema.RootElement;
        var props = root.GetProperty("properties");
        props.TryGetProperty("account", out _).Should().BeTrue();
        props.TryGetProperty("balance", out _).Should().BeTrue();
        props.TryGetProperty("entries", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsMessage()
    {
        var result = await _provider.GetSchemaAsync("urn:iso:std:iso:20022:pacs.008");

        result.Should().NotBeNull();
        result!.Name.Should().Contain("pacs.008");
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        var result = await _provider.IsAvailableAsync();
        result.Should().BeTrue();
    }
}
