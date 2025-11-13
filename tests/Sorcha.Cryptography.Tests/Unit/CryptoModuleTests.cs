using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

public class CryptoModuleTests
{
    private readonly CryptoModule _cryptoModule;

    public CryptoModuleTests()
    {
        _cryptoModule = new CryptoModule();
    }

    [Theory]
    [InlineData(WalletNetworks.ED25519)]
    [InlineData(WalletNetworks.NISTP256)]
    [InlineData(WalletNetworks.RSA4096)]
    public async Task GenerateKeySetAsync_ShouldGenerateValidKeys(WalletNetworks network)
    {
        // Act
        var result = await _cryptoModule.GenerateKeySetAsync(network);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PrivateKey.Key.Should().NotBeNullOrEmpty();
        result.Value.PublicKey.Key.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SignAndVerify_ED25519_ShouldSucceed()
    {
        // Arrange
        var keySetResult = await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
        var keySet = keySetResult.Value!;
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test data"));

        // Act - Sign
        var signResult = await _cryptoModule.SignAsync(hash, (byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

        // Assert - Sign
        signResult.IsSuccess.Should().BeTrue();
        signResult.Value.Should().NotBeNullOrEmpty();

        // Act - Verify
        var verifyResult = await _cryptoModule.VerifyAsync(signResult.Value!, hash, (byte)WalletNetworks.ED25519, keySet.PublicKey.Key!);

        // Assert - Verify
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task EncryptAndDecrypt_ED25519_ShouldRoundTrip()
    {
        // Arrange
        var keySetResult = await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
        var keySet = keySetResult.Value!;
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, Sorcha!");

        // Act - Encrypt
        var encryptResult = await _cryptoModule.EncryptAsync(plaintext, (byte)WalletNetworks.ED25519, keySet.PublicKey.Key!);

        // Assert - Encrypt
        encryptResult.IsSuccess.Should().BeTrue();
        encryptResult.Value.Should().NotBeNullOrEmpty();

        // Act - Decrypt
        var decryptResult = await _cryptoModule.DecryptAsync(encryptResult.Value!, (byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

        // Assert - Decrypt
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Equal(plaintext);
    }

    [Fact]
    public async Task CalculatePublicKey_ED25519_ShouldMatchGenerated()
    {
        // Arrange
        var keySetResult = await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
        var keySet = keySetResult.Value!;

        // Act
        var calculatedResult = await _cryptoModule.CalculatePublicKeyAsync((byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

        // Assert
        calculatedResult.IsSuccess.Should().BeTrue();
        calculatedResult.Value.Should().Equal(keySet.PublicKey.Key);
    }
}
