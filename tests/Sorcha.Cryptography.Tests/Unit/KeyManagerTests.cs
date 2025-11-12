using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

public class KeyManagerTests
{
    private readonly KeyManager _keyManager;

    public KeyManagerTests()
    {
        _keyManager = new KeyManager();
    }

    [Fact]
    public void GenerateMnemonic_ShouldGenerate12Words()
    {
        // Act
        var result = _keyManager.GenerateMnemonic(12);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value!.Split(' ').Should().HaveCount(12);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(18)]
    [InlineData(21)]
    [InlineData(24)]
    public void GenerateMnemonic_ShouldGenerateValidWordCounts(int wordCount)
    {
        // Act
        var result = _keyManager.GenerateMnemonic(wordCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Split(' ').Should().HaveCount(wordCount);
    }

    [Fact]
    public void ValidateMnemonic_ValidMnemonic_ShouldReturnTrue()
    {
        // Arrange
        var mnemonicResult = _keyManager.GenerateMnemonic(12);
        var mnemonic = mnemonicResult.Value!;

        // Act
        var isValid = _keyManager.ValidateMnemonic(mnemonic);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateMnemonic_InvalidMnemonic_ShouldReturnFalse()
    {
        // Arrange
        var invalidMnemonic = "invalid mnemonic phrase with wrong words here";

        // Act
        var isValid = _keyManager.ValidateMnemonic(invalidMnemonic);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndRecoverKeyRing_ShouldProduceSameKeys()
    {
        // Arrange - Create
        var createResult = await _keyManager.CreateMasterKeyRingAsync(WalletNetworks.ED25519, password: null);
        createResult.IsSuccess.Should().BeTrue();
        var originalKeyRing = createResult.Value!;

        // Act - Recover
        var recoverResult = await _keyManager.RecoverMasterKeyRingAsync(originalKeyRing.Mnemonic!, password: null);

        // Assert
        recoverResult.IsSuccess.Should().BeTrue();
        var recoveredKeyRing = recoverResult.Value!;
        recoveredKeyRing.MasterKeySet.PrivateKey.Key.Should().Equal(originalKeyRing.MasterKeySet.PrivateKey.Key);
        recoveredKeyRing.MasterKeySet.PublicKey.Key.Should().Equal(originalKeyRing.MasterKeySet.PublicKey.Key);
    }

    [Fact]
    public void MnemonicToSeed_ShouldProduceDeterministicSeed()
    {
        // Arrange
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        // Act
        var seed1 = _keyManager.MnemonicToSeed(mnemonic);
        var seed2 = _keyManager.MnemonicToSeed(mnemonic);

        // Assert
        seed1.IsSuccess.Should().BeTrue();
        seed2.IsSuccess.Should().BeTrue();
        seed1.Value.Should().Equal(seed2.Value);
    }
}
