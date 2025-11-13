using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

public class SymmetricCryptoTests
{
    private readonly SymmetricCrypto _symmetricCrypto;

    public SymmetricCryptoTests()
    {
        _symmetricCrypto = new SymmetricCrypto();
    }

    [Theory]
    [InlineData(EncryptionType.AES_128)]
    [InlineData(EncryptionType.AES_256)]
    [InlineData(EncryptionType.AES_GCM)]
    [InlineData(EncryptionType.CHACHA20_POLY1305)]
    [InlineData(EncryptionType.XCHACHA20_POLY1305)]
    public async Task EncryptAndDecrypt_ShouldRoundTrip(EncryptionType encryptionType)
    {
        // Arrange
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, Sorcha Cryptography!");

        // Act - Encrypt
        var encryptResult = await _symmetricCrypto.EncryptAsync(plaintext, encryptionType);

        // Assert - Encrypt
        encryptResult.IsSuccess.Should().BeTrue();
        encryptResult.Value.Should().NotBeNull();
        encryptResult.Value!.Data.Should().NotBeEmpty();

        // Act - Decrypt
        var decryptResult = await _symmetricCrypto.DecryptAsync(encryptResult.Value);

        // Assert - Decrypt
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Equal(plaintext);
    }

    [Fact]
    public async Task Encrypt_WithProvidedKey_ShouldUseProvidedKey()
    {
        // Arrange
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("test");
        byte[] key = _symmetricCrypto.GenerateKey(EncryptionType.AES_256);

        // Act
        var result = await _symmetricCrypto.EncryptAsync(plaintext, EncryptionType.AES_256, key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Key.Should().Equal(key);
    }

    [Fact]
    public async Task Decrypt_WithWrongKey_ShouldFail()
    {
        // Arrange
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("test");
        var encryptResult = await _symmetricCrypto.EncryptAsync(plaintext, EncryptionType.XCHACHA20_POLY1305);
        var ciphertext = encryptResult.Value!;

        // Modify the key
        ciphertext.Key[0] ^= 0xFF;

        // Act
        var decryptResult = await _symmetricCrypto.DecryptAsync(ciphertext);

        // Assert
        decryptResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GenerateKey_ShouldProduceCorrectSize()
    {
        // Act
        byte[] key = _symmetricCrypto.GenerateKey(EncryptionType.AES_256);

        // Assert
        key.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateIV_ShouldProduceCorrectSize()
    {
        // Act
        byte[] iv = _symmetricCrypto.GenerateIV(EncryptionType.XCHACHA20_POLY1305);

        // Assert
        iv.Should().HaveCount(24);
    }
}
