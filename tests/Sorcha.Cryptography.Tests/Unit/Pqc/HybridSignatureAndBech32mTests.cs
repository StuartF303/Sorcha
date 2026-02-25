// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Models;
using Sorcha.Cryptography.Utilities;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class HybridSignatureTests
{
    [Fact]
    public void ToJson_FromJson_ShouldRoundTrip()
    {
        var signature = new HybridSignature
        {
            Classical = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            ClassicalAlgorithm = "ED25519",
            Pqc = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(new byte[] { 7, 8, 9 })
        };

        var json = signature.ToJson();
        var deserialized = HybridSignature.FromJson(json);

        deserialized.Should().NotBeNull();
        deserialized!.Classical.Should().Be(signature.Classical);
        deserialized.ClassicalAlgorithm.Should().Be(signature.ClassicalAlgorithm);
        deserialized.Pqc.Should().Be(signature.Pqc);
        deserialized.PqcAlgorithm.Should().Be(signature.PqcAlgorithm);
        deserialized.WitnessPublicKey.Should().Be(signature.WitnessPublicKey);
    }

    [Fact]
    public void IsValid_BothSignatures_ShouldReturnTrue()
    {
        var signature = new HybridSignature
        {
            Classical = "base64data",
            ClassicalAlgorithm = "ED25519",
            Pqc = "base64pqc",
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = "base64key"
        };

        signature.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ClassicalOnly_ShouldReturnTrue()
    {
        var signature = new HybridSignature
        {
            Classical = "base64data",
            ClassicalAlgorithm = "ED25519"
        };

        signature.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_PqcOnly_ShouldReturnTrue()
    {
        var signature = new HybridSignature
        {
            Pqc = "base64pqc",
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = "base64key"
        };

        signature.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_Empty_ShouldReturnFalse()
    {
        var signature = new HybridSignature();

        signature.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_PqcWithoutWitnessKey_ShouldReturnFalse()
    {
        var signature = new HybridSignature
        {
            Pqc = "base64pqc",
            PqcAlgorithm = "ML-DSA-65"
            // Missing WitnessPublicKey
        };

        signature.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ClassicalWithoutAlgorithm_ShouldReturnFalse()
    {
        var signature = new HybridSignature
        {
            Classical = "base64data"
            // Missing ClassicalAlgorithm
        };

        signature.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsHybridFormat_JsonObject_ShouldReturnTrue()
    {
        HybridSignature.IsHybridFormat("{\"classical\":\"abc\"}").Should().BeTrue();
    }

    [Fact]
    public void IsHybridFormat_PlainBase64_ShouldReturnFalse()
    {
        HybridSignature.IsHybridFormat("SGVsbG8gV29ybGQ=").Should().BeFalse();
    }

    [Fact]
    public void FromJson_InvalidJson_ShouldReturnNull()
    {
        HybridSignature.FromJson("not json").Should().BeNull();
    }

    [Fact]
    public void FromJson_EmptyString_ShouldReturnNull()
    {
        HybridSignature.FromJson("").Should().BeNull();
    }

    [Fact]
    public void ToJson_OmitsNullFields()
    {
        var signature = new HybridSignature
        {
            Classical = "base64data",
            ClassicalAlgorithm = "ED25519"
        };

        var json = signature.ToJson();

        json.Should().NotContain("pqc");
        json.Should().NotContain("witnessPublicKey");
    }
}

public class Bech32mTests
{
    [Fact]
    public void Encode_Decode_ShouldRoundTrip()
    {
        var data = new byte[] { 0x10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        var encoded = Bech32m.Encode("ws2", data);
        var decoded = Bech32m.Decode(encoded);

        decoded.Should().NotBeNull();
        decoded!.Value.Hrp.Should().Be("ws2");
        decoded.Value.Data.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Encode_ShouldProduceWs2Prefix()
    {
        var data = new byte[] { 0x10, 1, 2, 3 };

        var encoded = Bech32m.Encode("ws2", data);

        encoded.Should().StartWith("ws21");
    }

    [Fact]
    public void Decode_InvalidChecksum_ShouldReturnNull()
    {
        var data = new byte[] { 0x10, 1, 2, 3 };
        var encoded = Bech32m.Encode("ws2", data);

        // Tamper with last character
        var tampered = encoded[..^1] + (encoded[^1] == 'q' ? 'p' : 'q');
        Bech32m.Decode(tampered).Should().BeNull();
    }

    [Fact]
    public void Decode_Bech32Original_ShouldFailOnBech32m()
    {
        // Bech32 (not Bech32m) encoded string should fail Bech32m checksum verification
        var bech32Encoded = Bech32.Encode("ws1", new byte[] { 0x00, 1, 2, 3 });
        Bech32m.Decode(bech32Encoded).Should().BeNull();
    }

    [Fact]
    public void Encode_EmptyData_ShouldThrow()
    {
        var act = () => Bech32m.Encode("ws2", Array.Empty<byte>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encode_NullHrp_ShouldThrow()
    {
        var act = () => Bech32m.Encode(null!, new byte[] { 1 });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decode_EmptyString_ShouldReturnNull()
    {
        Bech32m.Decode("").Should().BeNull();
    }

    [Fact]
    public void Encode_LargeData_ShouldRoundTrip()
    {
        // Simulate a PQC address: network byte + 32-byte SHA-256 hash
        var data = new byte[33];
        data[0] = 0x10; // ML_DSA_65 network byte
        new Random(42).NextBytes(data.AsSpan(1));

        var encoded = Bech32m.Encode("ws2", data);
        encoded.Length.Should().BeLessThan(100, "ws2 addresses must be under 100 characters");

        var decoded = Bech32m.Decode(encoded);
        decoded.Should().NotBeNull();
        decoded!.Value.Data.Should().BeEquivalentTo(data);
    }
}
