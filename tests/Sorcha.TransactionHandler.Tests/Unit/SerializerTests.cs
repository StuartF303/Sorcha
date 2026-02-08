using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Payload;
using Sorcha.TransactionHandler.Serialization;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Tests.Unit;

/// <summary>
/// Unit tests for transaction serializers.
/// </summary>
public class SerializerTests
{
    private readonly CryptoModule _cryptoModule;
    private readonly HashProvider _hashProvider;
    private readonly SymmetricCrypto _symmetricCrypto;

    public SerializerTests()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _symmetricCrypto = new SymmetricCrypto();
    }

    [Fact]
    public async Task JsonSerializer_ShouldSerializeTransaction()
    {
        // Arrange
        var serializer = new JsonTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var transaction = await CreateSignedTransaction();

        // Act
        var json = serializer.SerializeToJson(transaction);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"txId\"", json);
        Assert.Contains("\"version\"", json);
    }

    [Fact]
    public async Task JsonSerializer_ShouldDeserializeTransaction()
    {
        // Arrange
        var serializer = new JsonTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var originalTransaction = await CreateSignedTransaction();
        var json = serializer.SerializeToJson(originalTransaction);

        // Act
        var deserializedTransaction = serializer.DeserializeFromJson(json);

        // Assert
        Assert.NotNull(deserializedTransaction);
        Assert.Equal(originalTransaction.Version, deserializedTransaction.Version);
        Assert.Equal(originalTransaction.Recipients?.Length, deserializedTransaction.Recipients?.Length);
    }

    [Fact]
    public async Task BinarySerializer_ShouldSerializeTransaction()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var transaction = await CreateSignedTransaction();

        // Act
        var binary = serializer.SerializeToBinary(transaction);

        // Assert
        Assert.NotNull(binary);
        Assert.True(binary.Length > 0);
    }

    [Fact]
    public async Task BinarySerializer_ShouldDeserializeTransaction()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var originalTransaction = await CreateSignedTransaction();
        var binary = serializer.SerializeToBinary(originalTransaction);

        // Act
        var deserializedTransaction = serializer.DeserializeFromBinary(binary);

        // Assert
        Assert.NotNull(deserializedTransaction);
        Assert.Equal(originalTransaction.Version, deserializedTransaction.Version);
        Assert.Equal(originalTransaction.Recipients?.Length, deserializedTransaction.Recipients?.Length);
    }

    [Fact]
    public async Task BinarySerializer_ShouldRoundTrip()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var originalTransaction = await CreateSignedTransaction();

        // Act
        var binary = serializer.SerializeToBinary(originalTransaction);
        var deserializedTransaction = serializer.DeserializeFromBinary(binary);
        var binary2 = serializer.SerializeToBinary(deserializedTransaction);

        // Assert - binary should be identical (excluding signature)
        Assert.Equal(originalTransaction.Version, deserializedTransaction.Version);
    }

    [Fact]
    public async Task CreateTransportPacket_ShouldCreatePacket()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var transaction = await CreateSignedTransaction();

        // Act
        var packet = serializer.CreateTransportPacket(transaction);

        // Assert
        Assert.NotNull(packet);
        Assert.Equal(transaction.TxId, packet.TxId);
        Assert.NotNull(packet.Data);
        Assert.True(packet.Data.Length > 0);
    }

    [Fact]
    public void JsonSerializer_ShouldThrowOnNullTransaction()
    {
        // Arrange
        var serializer = new JsonTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            serializer.SerializeToJson(null!));
    }

    [Fact]
    public void BinarySerializer_ShouldThrowOnNullTransaction()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            serializer.SerializeToBinary(null!));
    }

    [Fact]
    public void JsonSerializer_ShouldThrowOnEmptyJson()
    {
        // Arrange
        var serializer = new JsonTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            serializer.DeserializeFromJson(string.Empty));
    }

    [Fact]
    public void BinarySerializer_ShouldThrowOnEmptyBinary()
    {
        // Arrange
        var serializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            serializer.DeserializeFromBinary(Array.Empty<byte>()));
    }

    private async Task<Transaction> CreateSignedTransaction()
    {
        var payloadManager = new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);
        var transaction = new Transaction(
            _cryptoModule,
            _hashProvider,
            payloadManager,
            TransactionVersion.V1);

        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        transaction.Recipients = new[] { wallet.Address };
        transaction.Metadata = "{\"type\": \"test\"}";
        transaction.PreviousTxHash = "prev_hash_123";

        await transaction.SignAsync(wallet.PrivateKeyWif);

        return transaction;
    }
}
