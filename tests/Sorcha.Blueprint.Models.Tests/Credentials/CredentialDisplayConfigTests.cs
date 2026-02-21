// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Models.Tests.Credentials;

public class CredentialDisplayConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new CredentialDisplayConfig();

        config.BackgroundColor.Should().Be("#1976D2");
        config.TextColor.Should().Be("#FFFFFF");
        config.Icon.Should().Be("Certificate");
        config.CardLayout.Should().Be("Standard");
        config.HighlightClaims.Should().BeNull();
    }

    [Fact]
    public void Serialize_WithHighlightClaims_RoundTrips()
    {
        var config = new CredentialDisplayConfig
        {
            BackgroundColor = "#FF5722",
            TextColor = "#000000",
            Icon = "Shield",
            CardLayout = "Ticket",
            HighlightClaims = new Dictionary<string, string>
            {
                ["licenseClass"] = "Class",
                ["holderName"] = "Name"
            }
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<CredentialDisplayConfig>(json)!;

        deserialized.BackgroundColor.Should().Be("#FF5722");
        deserialized.TextColor.Should().Be("#000000");
        deserialized.Icon.Should().Be("Shield");
        deserialized.CardLayout.Should().Be("Ticket");
        deserialized.HighlightClaims.Should().ContainKey("licenseClass")
            .WhoseValue.Should().Be("Class");
        deserialized.HighlightClaims.Should().ContainKey("holderName")
            .WhoseValue.Should().Be("Name");
    }

    [Fact]
    public void Serialize_WithNullHighlightClaims_OmitsField()
    {
        var config = new CredentialDisplayConfig();
        var json = JsonSerializer.Serialize(config);

        json.Should().NotContain("highlightClaims");
    }

    [Fact]
    public void Serialize_InIssuanceConfig_RoundTrips()
    {
        var config = new CredentialIssuanceConfig
        {
            CredentialType = "TestCredential",
            ClaimMappings = [new ClaimMapping { SourceField = "f1", ClaimName = "c1" }],
            RecipientParticipantId = "p1",
            DisplayConfig = new CredentialDisplayConfig
            {
                BackgroundColor = "#4CAF50",
                Icon = "Verified"
            }
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<CredentialIssuanceConfig>(json)!;

        deserialized.DisplayConfig.Should().NotBeNull();
        deserialized.DisplayConfig!.BackgroundColor.Should().Be("#4CAF50");
        deserialized.DisplayConfig.Icon.Should().Be("Verified");
    }
}
