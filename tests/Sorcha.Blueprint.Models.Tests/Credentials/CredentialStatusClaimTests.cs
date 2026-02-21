// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Models.Tests.Credentials;

public class CredentialStatusClaimTests
{
    [Fact]
    public void DefaultType_IsBitstringStatusListEntry()
    {
        var claim = new CredentialStatusClaim
        {
            Id = "https://example.com/statuslist/1#42",
            StatusPurpose = "revocation",
            StatusListIndex = "42",
            StatusListCredential = "https://example.com/statuslist/1"
        };

        claim.Type.Should().Be("BitstringStatusListEntry");
    }

    [Theory]
    [InlineData("revocation")]
    [InlineData("suspension")]
    public void StatusPurpose_AcceptsValidValues(string purpose)
    {
        var claim = new CredentialStatusClaim
        {
            Id = $"https://example.com/list#0",
            StatusPurpose = purpose,
            StatusListIndex = "0",
            StatusListCredential = "https://example.com/list"
        };

        claim.StatusPurpose.Should().Be(purpose);
    }

    [Fact]
    public void Serialization_RoundTrips()
    {
        var claim = new CredentialStatusClaim
        {
            Id = "https://sorcha.example/api/v1/credentials/status-lists/issuer-reg-revocation-1#42",
            StatusPurpose = "revocation",
            StatusListIndex = "42",
            StatusListCredential = "https://sorcha.example/api/v1/credentials/status-lists/issuer-reg-revocation-1"
        };

        var json = JsonSerializer.Serialize(claim);
        var deserialized = JsonSerializer.Deserialize<CredentialStatusClaim>(json)!;

        deserialized.Id.Should().Be(claim.Id);
        deserialized.Type.Should().Be("BitstringStatusListEntry");
        deserialized.StatusPurpose.Should().Be("revocation");
        deserialized.StatusListIndex.Should().Be("42");
        deserialized.StatusListCredential.Should().Be(claim.StatusListCredential);
    }

    [Fact]
    public void Serialization_UsesJsonPropertyNames()
    {
        var claim = new CredentialStatusClaim
        {
            Id = "test#0",
            StatusPurpose = "revocation",
            StatusListIndex = "0",
            StatusListCredential = "test"
        };

        var json = JsonSerializer.Serialize(claim);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"type\":");
        json.Should().Contain("\"statusPurpose\":");
        json.Should().Contain("\"statusListIndex\":");
        json.Should().Contain("\"statusListCredential\":");
    }
}
