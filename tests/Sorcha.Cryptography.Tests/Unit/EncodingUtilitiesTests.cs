using FluentAssertions;
using Sorcha.Cryptography.Utilities;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

public class EncodingUtilitiesTests
{
    [Fact]
    public void Base58_EncodeAndDecode_ShouldRoundTrip()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        string encoded = Base58.Encode(data);
        byte[]? decoded = Base58.Decode(encoded);

        // Assert
        decoded.Should().Equal(data);
    }

    [Fact]
    public void Base58Check_EncodeAndDecode_ShouldRoundTrip()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test Data");

        // Act
        string encoded = Base58.EncodeCheck(data);
        byte[]? decoded = Base58.DecodeCheck(encoded);

        // Assert
        decoded.Should().Equal(data);
    }

    [Fact]
    public void HexConverter_ToHexAndFromHex_ShouldRoundTrip()
    {
        // Arrange
        byte[] data = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

        // Act
        string hex = HexConverter.ToHex(data);
        byte[]? decoded = HexConverter.FromHex(hex);

        // Assert
        decoded.Should().Equal(data);
    }

    [Fact]
    public void HexConverter_IsValidHex_ShouldValidateCorrectly()
    {
        // Act & Assert
        HexConverter.IsValidHex("0123456789ABCDEF").Should().BeTrue();
        HexConverter.IsValidHex("0x123ABC").Should().BeTrue();
        HexConverter.IsValidHex("123 ABC").Should().BeTrue();
        HexConverter.IsValidHex("GHIJK").Should().BeFalse();
        HexConverter.IsValidHex("123").Should().BeFalse(); // Odd length
    }

    [Fact]
    public void VariableLengthInteger_EncodeAndDecode_ShouldRoundTrip()
    {
        // Arrange
        ulong[] testValues = { 0, 100, 0xFC, 0xFD, 0xFFFF, 0x10000, 0xFFFFFFFF, 0x100000000 };

        foreach (ulong value in testValues)
        {
            // Act
            byte[] encoded = VariableLengthInteger.Encode(value);
            ulong decoded = VariableLengthInteger.Decode(encoded, 0, out int bytesRead);

            // Assert
            decoded.Should().Be(value);
            bytesRead.Should().Be(encoded.Length);
        }
    }

    [Fact]
    public void Bech32_EncodeAndDecode_ShouldRoundTrip()
    {
        // Arrange
        byte[] data = new byte[] { 0x00, 0x14, 0x75, 0x1e, 0x76, 0xe8, 0x19, 0x91 };

        // Act
        string encoded = Bech32.Encode("ws1", data);
        var decoded = Bech32.Decode(encoded);

        // Assert
        decoded.Should().NotBeNull();
        decoded!.Value.Hrp.Should().Be("ws1");
        decoded.Value.Data.Should().Equal(data);
    }
}
