using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Extensions;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

public class HashProviderTests
{
    private readonly HashProvider _hashProvider;

    public HashProviderTests()
    {
        _hashProvider = new HashProvider();
    }

    [Theory]
    [InlineData(HashType.SHA256)]
    [InlineData(HashType.SHA384)]
    [InlineData(HashType.SHA512)]
    [InlineData(HashType.Blake2b256)]
    [InlineData(HashType.Blake2b512)]
    public void ComputeHash_ShouldProduceCorrectSize(HashType hashType)
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        int expectedSize = hashType.GetHashSize();

        // Act
        byte[] hash = _hashProvider.ComputeHash(data, hashType);

        // Assert
        hash.Should().HaveCount(expectedSize);
    }

    [Fact]
    public void ComputeHash_SHA256_ShouldBeDeterministic()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] hash1 = _hashProvider.ComputeHash(data, HashType.SHA256);
        byte[] hash2 = _hashProvider.ComputeHash(data, HashType.SHA256);

        // Assert
        hash1.Should().Equal(hash2);
    }

    [Fact]
    public void VerifyHash_ValidHash_ShouldReturnTrue()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        byte[] hash = _hashProvider.ComputeHash(data, HashType.SHA256);

        // Act
        bool isValid = _hashProvider.VerifyHash(data, hash, HashType.SHA256);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHash_InvalidHash_ShouldReturnFalse()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        byte[] wrongHash = new byte[32]; // All zeros

        // Act
        bool isValid = _hashProvider.VerifyHash(data, wrongHash, HashType.SHA256);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ComputeHMAC_ShouldProduceValidHMAC()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        byte[] key = System.Text.Encoding.UTF8.GetBytes("secret key");

        // Act
        byte[] hmac = _hashProvider.ComputeHMAC(data, key, HashType.SHA256);

        // Assert
        hmac.Should().HaveCount(32); // SHA-256 HMAC is 32 bytes
    }

    [Fact]
    public async Task ComputeHashAsync_Stream_ShouldMatchByteArrayHash()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        byte[] expectedHash = _hashProvider.ComputeHash(data, HashType.SHA256);

        using var stream = new MemoryStream(data);

        // Act
        byte[] streamHash = await _hashProvider.ComputeHashAsync(stream, HashType.SHA256);

        // Assert
        streamHash.Should().Equal(expectedHash);
    }
}
