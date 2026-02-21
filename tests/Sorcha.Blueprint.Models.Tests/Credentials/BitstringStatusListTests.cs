// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Models.Tests.Credentials;

public class BitstringStatusListTests
{
    // ===== Create Factory Tests =====

    [Fact]
    public void Create_ValidParams_ReturnsInitializedList()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");

        list.Id.Should().Be("issuer-1-register-1-revocation-1");
        list.IssuerWallet.Should().Be("issuer-1");
        list.RegisterId.Should().Be("register-1");
        list.Purpose.Should().Be("revocation");
        list.Size.Should().Be(BitstringStatusList.MinimumSize);
        list.NextAvailableIndex.Should().Be(0);
        list.Version.Should().Be(1);
        list.EncodedList.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_SuspensionPurpose_Succeeds()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "suspension");
        list.Purpose.Should().Be("suspension");
    }

    [Fact]
    public void Create_InvalidPurpose_ThrowsArgumentException()
    {
        var act = () => BitstringStatusList.Create("issuer-1", "register-1", "invalid");
        act.Should().Throw<ArgumentException>().WithMessage("*'revocation' or 'suspension'*");
    }

    [Fact]
    public void Create_SizeBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        var act = () => BitstringStatusList.Create("issuer-1", "register-1", "revocation", size: 100);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_CustomSize_UsesSpecifiedSize()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation", size: 262144);
        list.Size.Should().Be(262144);
    }

    [Fact]
    public void MinimumSize_IsW3CCompliant()
    {
        BitstringStatusList.MinimumSize.Should().Be(131072);
    }

    // ===== GZip + Base64 Round-Trip Tests =====

    [Fact]
    public void CompressDecompress_RoundTrips()
    {
        var original = new byte[131072 / 8]; // All zeros
        var encoded = BitstringStatusList.CompressBitstring(original);
        var decompressed = BitstringStatusList.DecompressBitstring(encoded);

        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CompressDecompress_WithSetBits_RoundTrips()
    {
        var original = new byte[131072 / 8];
        original[0] = 0x80; // First bit set (MSB)
        original[42] = 0xFF; // All bits set in byte 42

        var encoded = BitstringStatusList.CompressBitstring(original);
        var decompressed = BitstringStatusList.DecompressBitstring(encoded);

        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void EncodedList_IsBase64String()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var act = () => Convert.FromBase64String(list.EncodedList);
        act.Should().NotThrow();
    }

    // ===== AllocateIndex Tests =====

    [Fact]
    public void AllocateIndex_ReturnsSequentialIndices()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");

        list.AllocateIndex().Should().Be(0);
        list.AllocateIndex().Should().Be(1);
        list.AllocateIndex().Should().Be(2);
        list.NextAvailableIndex.Should().Be(3);
    }

    [Fact]
    public void AllocateIndex_WhenFull_ReturnsMinusOne()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        list.NextAvailableIndex = list.Size; // Simulate full

        list.AllocateIndex().Should().Be(-1);
    }

    // ===== GetBit / SetBit Tests =====

    [Fact]
    public void GetBit_AllZeros_ReturnsFalse()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        list.GetBit(0).Should().BeFalse();
        list.GetBit(42).Should().BeFalse();
        list.GetBit(131071).Should().BeFalse();
    }

    [Fact]
    public void SetBit_True_SetsCorrectBit()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");

        list.SetBit(42, true);

        list.GetBit(42).Should().BeTrue();
        list.GetBit(41).Should().BeFalse();
        list.GetBit(43).Should().BeFalse();
    }

    [Fact]
    public void SetBit_ThenClear_RestoredToFalse()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");

        list.SetBit(100, true);
        list.GetBit(100).Should().BeTrue();

        list.SetBit(100, false);
        list.GetBit(100).Should().BeFalse();
    }

    [Fact]
    public void SetBit_IncrementsVersion()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        list.Version.Should().Be(1);

        list.SetBit(0, true);
        list.Version.Should().Be(2);

        list.SetBit(0, false);
        list.Version.Should().Be(3);
    }

    [Fact]
    public void SetBit_UpdatesLastUpdated()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var originalTime = list.LastUpdated;

        // Small delay to ensure timestamp changes
        list.SetBit(0, true);

        list.LastUpdated.Should().BeOnOrAfter(originalTime);
    }

    [Fact]
    public void SetBit_NegativeIndex_ThrowsArgumentOutOfRange()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var act = () => list.SetBit(-1, true);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetBit_IndexBeyondSize_ThrowsArgumentOutOfRange()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var act = () => list.SetBit(131072, true);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetBit_NegativeIndex_ThrowsArgumentOutOfRange()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var act = () => list.GetBit(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ===== MSB-First Bit Order Tests =====

    [Fact]
    public void SetBit_MsbFirstOrder_Bit0IsHighBitOfByte0()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        list.SetBit(0, true);

        // Bit 0 → byte 0, bit index 7 (MSB) per W3C spec
        var raw = BitstringStatusList.DecompressBitstring(list.EncodedList);
        (raw[0] & 0x80).Should().NotBe(0, "bit 0 should be MSB of byte 0");
    }

    [Fact]
    public void SetBit_Bit7_IsLsbOfByte0()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        list.SetBit(7, true);

        var raw = BitstringStatusList.DecompressBitstring(list.EncodedList);
        (raw[0] & 0x01).Should().NotBe(0, "bit 7 should be LSB of byte 0");
    }

    [Fact]
    public void SetBit_MultipleBits_AllRetrievable()
    {
        var list = BitstringStatusList.Create("issuer-1", "register-1", "revocation");
        var indices = new[] { 0, 7, 8, 42, 1000, 131071 };

        foreach (var idx in indices)
            list.SetBit(idx, true);

        foreach (var idx in indices)
            list.GetBit(idx).Should().BeTrue($"bit {idx} should be set");
    }
}
