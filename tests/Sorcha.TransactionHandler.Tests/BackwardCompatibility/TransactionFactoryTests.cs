using System.Threading.Tasks;
using Xunit;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Versioning;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Tests.BackwardCompatibility;

/// <summary>
/// Tests for TransactionFactory backward compatibility.
/// </summary>
public class TransactionFactoryTests
{
    private readonly CryptoModule _cryptoModule;
    private readonly HashProvider _hashProvider;
    private readonly SymmetricCrypto _symmetricCrypto;
    private readonly VersionDetector _versionDetector;
    private readonly TransactionFactory _factory;

    public TransactionFactoryTests()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _symmetricCrypto = new SymmetricCrypto();
        _versionDetector = new VersionDetector();
        _factory = new TransactionFactory(_cryptoModule, _hashProvider, _symmetricCrypto, _versionDetector);
    }

    [Fact]
    public void Create_V1_ShouldCreateTransaction()
    {
        // Act
        var transaction = _factory.Create(TransactionVersion.V1);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(TransactionVersion.V1, transaction.Version);
    }

    [Fact]
    public async Task Deserialize_V1BinaryTransaction_ShouldSucceed()
    {
        // Arrange - Create a V1 transaction
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients("ws1recipient")
            .SignAsync(wallet.PrivateKeyWif);

        var originalTransaction = builderResult.Build().Value!;

        // Serialize it
        var serializer = new Serialization.BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var binaryData = serializer.SerializeToBinary(originalTransaction);

        // Act - Deserialize using factory
        var deserializedTransaction = _factory.Deserialize(binaryData);

        // Assert
        Assert.NotNull(deserializedTransaction);
        Assert.Equal(TransactionVersion.V1, deserializedTransaction.Version);
    }

    [Fact]
    public async Task Deserialize_V1JsonTransaction_ShouldSucceed()
    {
        // Arrange - Create a V1 transaction
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients("ws1recipient")
            .SignAsync(wallet.PrivateKeyWif);

        var originalTransaction = builderResult.Build().Value!;

        // Serialize to JSON
        var serializer = new Serialization.JsonTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
        var json = serializer.SerializeToJson(originalTransaction);

        // Act - Deserialize using factory
        var deserializedTransaction = _factory.Deserialize(json);

        // Assert
        Assert.NotNull(deserializedTransaction);
        Assert.Equal(TransactionVersion.V1, deserializedTransaction.Version);
    }

    [Theory]
    [InlineData((TransactionVersion)2)]
    [InlineData((TransactionVersion)3)]
    [InlineData((TransactionVersion)4)]
    [InlineData((TransactionVersion)99)]
    public void Create_UnsupportedVersion_ShouldThrow(TransactionVersion version)
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _factory.Create(version));
    }

    [Fact]
    public void Create_InvalidVersion_ShouldThrow()
    {
        // Arrange
        var invalidVersion = (TransactionVersion)99;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _factory.Create(invalidVersion));
    }

    [Fact]
    public void Deserialize_NullBinaryData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Deserialize((byte[])null!));
    }

    [Fact]
    public void Deserialize_EmptyBinaryData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Deserialize(System.Array.Empty<byte>()));
    }

    [Fact]
    public void Deserialize_NullJsonData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Deserialize((string)null!));
    }

    [Fact]
    public void Deserialize_EmptyJsonData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Deserialize(string.Empty));
    }
}
