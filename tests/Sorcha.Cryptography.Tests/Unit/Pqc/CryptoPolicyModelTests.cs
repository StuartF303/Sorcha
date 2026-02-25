// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.Register.Models;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class CryptoPolicyModelTests
{
    [Fact]
    public void CreateDefault_ShouldProduceValidPolicy()
    {
        var policy = CryptoPolicy.CreateDefault();

        policy.IsValid().Should().BeTrue();
        policy.Version.Should().Be(1);
        policy.AcceptedSignatureAlgorithms.Should().Contain("ED25519");
        policy.AcceptedSignatureAlgorithms.Should().Contain("ML-DSA-65");
        policy.AcceptedSignatureAlgorithms.Should().Contain("SLH-DSA-128s");
        policy.RequiredSignatureAlgorithms.Should().BeEmpty();
        policy.EnforcementMode.Should().Be(CryptoPolicyEnforcementMode.Permissive);
        policy.AcceptedEncryptionSchemes.Should().Contain("ML-KEM-768");
        policy.AcceptedHashFunctions.Should().Contain("SHA-256");
    }

    [Fact]
    public void IsValid_RequiredSubsetOfAccepted_ShouldReturnTrue()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.RequiredSignatureAlgorithms = ["ML-DSA-65"];

        policy.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_RequiredNotSubsetOfAccepted_ShouldReturnFalse()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.RequiredSignatureAlgorithms = ["UNKNOWN-ALGO"];

        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyAcceptedSignatureAlgorithms_ShouldReturnFalse()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.AcceptedSignatureAlgorithms = [];

        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyAcceptedEncryptionSchemes_ShouldReturnFalse()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.AcceptedEncryptionSchemes = [];

        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyAcceptedHashFunctions_ShouldReturnFalse()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.AcceptedHashFunctions = [];

        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ZeroVersion_ShouldReturnFalse()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.Version = 0;

        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void JsonSerialization_ShouldRoundTrip()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.RequiredSignatureAlgorithms = ["ML-DSA-65"];
        policy.DeprecatedAlgorithms = ["RSA4096"];
        policy.EnforcementMode = CryptoPolicyEnforcementMode.Strict;

        var json = JsonSerializer.Serialize(policy);
        var deserialized = JsonSerializer.Deserialize<CryptoPolicy>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(policy.Version);
        deserialized.AcceptedSignatureAlgorithms.Should().BeEquivalentTo(policy.AcceptedSignatureAlgorithms);
        deserialized.RequiredSignatureAlgorithms.Should().BeEquivalentTo(policy.RequiredSignatureAlgorithms);
        deserialized.EnforcementMode.Should().Be(CryptoPolicyEnforcementMode.Strict);
        deserialized.DeprecatedAlgorithms.Should().BeEquivalentTo(policy.DeprecatedAlgorithms);
    }

    [Fact]
    public void EnforcementMode_ShouldSerializeAsString()
    {
        var policy = CryptoPolicy.CreateDefault();
        policy.EnforcementMode = CryptoPolicyEnforcementMode.Strict;

        var json = JsonSerializer.Serialize(policy);

        json.Should().Contain("\"Strict\"");
    }

    [Fact]
    public void KnownAlgorithms_ShouldContainAllExpected()
    {
        CryptoPolicy.KnownAlgorithms.All.Should().Contain("ED25519");
        CryptoPolicy.KnownAlgorithms.All.Should().Contain("ML-DSA-65");
        CryptoPolicy.KnownAlgorithms.All.Should().Contain("SLH-DSA-128s");
        CryptoPolicy.KnownAlgorithms.All.Should().Contain("ML-KEM-768");
        CryptoPolicy.KnownAlgorithms.All.Should().Contain("XCHACHA20-POLY1305");
        CryptoPolicy.KnownAlgorithms.All.Should().HaveCount(11);
    }
}
