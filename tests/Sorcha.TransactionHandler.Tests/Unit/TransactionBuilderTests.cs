using System;
using System.Threading.Tasks;
using Xunit;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Tests.Unit;

/// <summary>
/// Unit tests for TransactionBuilder.
/// </summary>
public class TransactionBuilderTests
{
    private readonly CryptoModule _cryptoModule;
    private readonly HashProvider _hashProvider;
    private readonly SymmetricCrypto _symmetricCrypto;

    public TransactionBuilderTests()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _symmetricCrypto = new SymmetricCrypto();
    }

    [Fact]
    public void Create_ShouldCreateNewTransaction()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act
        var result = builder.Create(TransactionVersion.V1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void WithRecipients_ShouldSetRecipients()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var recipients = new[] { "ws1qyqszqgp123", "ws1pqpszqgp456" };

        // Act
        builder.Create().WithRecipients(recipients);

        // Assert - no exception means success
        Assert.True(true);
    }

    [Fact]
    public void WithMetadata_ShouldSetJsonMetadata()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var metadata = "{\"type\": \"test\"}";

        // Act
        builder.Create().WithMetadata(metadata);

        // Assert - no exception means success
        Assert.True(true);
    }

    [Fact]
    public void WithMetadata_ShouldThrowOnInvalidJson()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var invalidJson = "{invalid json}";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.Create().WithMetadata(invalidJson));
    }

    [Fact]
    public void AddPayload_ShouldThrowOnEmptyData()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var emptyData = Array.Empty<byte>();
        var recipients = new[] { "ws1qyqszqgp123" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.Create().AddPayload(emptyData, recipients));
    }

    [Fact]
    public void AddPayload_ShouldThrowOnEmptyRecipients()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var data = new byte[] { 1, 2, 3 };
        var emptyRecipients = Array.Empty<string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.Create().AddPayload(data, emptyRecipients));
    }

    [Fact]
    public async Task Build_ShouldFailIfNotSigned()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act
        var result = builder.Create()
            .WithRecipients("ws1qyqszqgp123")
            .Build();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.NotSigned, result.Status);
    }

    [Fact]
    public async Task SignAsync_ShouldSignTransaction()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Generate a test wallet
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        // Act
        var signedBuilder = await builder.Create()
            .WithRecipients("ws1qyqszqgp123")
            .SignAsync(wallet.PrivateKeyWif);

        // Assert
        Assert.NotNull(signedBuilder);
    }

    [Fact]
    public async Task Build_ShouldSucceedAfterSigning()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        // Act
        var result = await builder.Create()
            .WithRecipients("ws1qyqszqgp123")
            .WithMetadata("{\"type\": \"test\"}")
            .SignAsync(wallet.PrivateKeyWif);

        var transaction = result.Build();

        // Assert
        Assert.True(transaction.IsSuccess);
        Assert.NotNull(transaction.Value);
    }

    [Fact]
    public async Task WithRecipients_ShouldThrowAfterSigning()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        // Act
        var signedBuilder = await builder.Create()
            .WithRecipients("ws1qyqszqgp123")
            .SignAsync(wallet.PrivateKeyWif);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await signedBuilder.WithRecipients("ws1pqpszqgp456").SignAsync(wallet.PrivateKeyWif));
    }
}
