// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Models.Tests.Credentials;

public class UsagePolicyTests
{
    [Theory]
    [InlineData(UsagePolicy.Reusable, 0)]
    [InlineData(UsagePolicy.SingleUse, 1)]
    [InlineData(UsagePolicy.LimitedUse, 2)]
    public void UsagePolicy_HasCorrectIntegerValues(UsagePolicy policy, int expected)
    {
        ((int)policy).Should().Be(expected);
    }

    [Theory]
    [InlineData(UsagePolicy.Reusable, "\"Reusable\"")]
    [InlineData(UsagePolicy.SingleUse, "\"SingleUse\"")]
    [InlineData(UsagePolicy.LimitedUse, "\"LimitedUse\"")]
    public void UsagePolicy_SerializesAsString(UsagePolicy policy, string expectedJson)
    {
        var json = JsonSerializer.Serialize(policy);
        json.Should().Be(expectedJson);
    }

    [Theory]
    [InlineData("\"Reusable\"", UsagePolicy.Reusable)]
    [InlineData("\"SingleUse\"", UsagePolicy.SingleUse)]
    [InlineData("\"LimitedUse\"", UsagePolicy.LimitedUse)]
    public void UsagePolicy_DeserializesFromString(string json, UsagePolicy expected)
    {
        var result = JsonSerializer.Deserialize<UsagePolicy>(json);
        result.Should().Be(expected);
    }

    [Fact]
    public void UsagePolicy_RoundTrips_InIssuanceConfig()
    {
        var config = new CredentialIssuanceConfig
        {
            CredentialType = "TestCredential",
            ClaimMappings = [new ClaimMapping { SourceField = "f1", ClaimName = "c1" }],
            RecipientParticipantId = "p1",
            UsagePolicy = UsagePolicy.LimitedUse,
            MaxPresentations = 5
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<CredentialIssuanceConfig>(json)!;

        deserialized.UsagePolicy.Should().Be(UsagePolicy.LimitedUse);
        deserialized.MaxPresentations.Should().Be(5);
    }
}
