// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Engine.Credentials;
using Sorcha.Blueprint.Engine.Models;
using Sorcha.Blueprint.Models.Credentials;
using Sorcha.Cryptography.SdJwt;

namespace Sorcha.Blueprint.Engine.Tests.Credentials;

/// <summary>
/// Tests that CredentialIssuer correctly propagates UsagePolicy, MaxPresentations,
/// and DisplayConfig from CredentialIssuanceConfig to IssuedCredentialInfo.
/// </summary>
public class UsagePolicyIssuanceTests
{
    private readonly Mock<ISdJwtService> _sdJwtServiceMock;
    private readonly CredentialIssuer _issuer;

    public UsagePolicyIssuanceTests()
    {
        _sdJwtServiceMock = new Mock<ISdJwtService>();
        _sdJwtServiceMock
            .Setup(s => s.CreateTokenAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>?>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdJwtToken { RawToken = "test.token.sig~" });

        _issuer = new CredentialIssuer(_sdJwtServiceMock.Object);
    }

    [Fact]
    public async Task IssueAsync_DefaultUsagePolicy_IsReusable()
    {
        var config = CreateConfig();

        var result = await IssueAsync(config);

        result.UsagePolicy.Should().Be("Reusable");
        result.MaxPresentations.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_SingleUsePolicy_SetsCorrectly()
    {
        var config = CreateConfig();
        config.UsagePolicy = UsagePolicy.SingleUse;

        var result = await IssueAsync(config);

        result.UsagePolicy.Should().Be("SingleUse");
        result.MaxPresentations.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_LimitedUsePolicy_IncludesMaxPresentations()
    {
        var config = CreateConfig();
        config.UsagePolicy = UsagePolicy.LimitedUse;
        config.MaxPresentations = 5;

        var result = await IssueAsync(config);

        result.UsagePolicy.Should().Be("LimitedUse");
        result.MaxPresentations.Should().Be(5);
    }

    [Fact]
    public async Task IssueAsync_WithDisplayConfig_SerializesToJson()
    {
        var config = CreateConfig();
        config.DisplayConfig = new CredentialDisplayConfig
        {
            BackgroundColor = "#FF5722",
            TextColor = "#000000",
            Icon = "Shield",
            CardLayout = "Compact"
        };

        var result = await IssueAsync(config);

        result.DisplayConfigJson.Should().NotBeNullOrEmpty();

        var displayConfig = JsonSerializer.Deserialize<CredentialDisplayConfig>(result.DisplayConfigJson!);
        displayConfig.Should().NotBeNull();
        displayConfig!.BackgroundColor.Should().Be("#FF5722");
        displayConfig.TextColor.Should().Be("#000000");
        displayConfig.Icon.Should().Be("Shield");
        displayConfig.CardLayout.Should().Be("Compact");
    }

    [Fact]
    public async Task IssueAsync_NullDisplayConfig_DisplayConfigJsonIsNull()
    {
        var config = CreateConfig();
        config.DisplayConfig = null;

        var result = await IssueAsync(config);

        result.DisplayConfigJson.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_DefaultDisplayConfig_SerializesDefaults()
    {
        var config = CreateConfig();
        config.DisplayConfig = new CredentialDisplayConfig(); // all defaults

        var result = await IssueAsync(config);

        result.DisplayConfigJson.Should().NotBeNullOrEmpty();

        var displayConfig = JsonSerializer.Deserialize<CredentialDisplayConfig>(result.DisplayConfigJson!);
        displayConfig!.BackgroundColor.Should().Be("#1976D2");
        displayConfig.TextColor.Should().Be("#FFFFFF");
        displayConfig.Icon.Should().Be("Certificate");
        displayConfig.CardLayout.Should().Be("Standard");
    }

    private static CredentialIssuanceConfig CreateConfig()
    {
        return new CredentialIssuanceConfig
        {
            CredentialType = "TestCredential",
            ClaimMappings = [],
            RecipientParticipantId = "recipient"
        };
    }

    private async Task<IssuedCredentialInfo> IssueAsync(CredentialIssuanceConfig config)
    {
        return await _issuer.IssueAsync(
            config,
            new Dictionary<string, object>(),
            "did:issuer:test",
            "did:recipient:test",
            new byte[] { 1, 2, 3 },
            "EdDSA");
    }
}
