namespace Sorcha.WalletService.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly TransactionService _transactionService;

    public TransactionServiceTests()
    {
        _mockCryptoModule = new Mock<ICryptoModule>();
        _mockHashProvider = new Mock<IHashProvider>();

        _transactionService = new TransactionService(
            _mockCryptoModule.Object,
            _mockHashProvider.Object,
            Mock.Of<ILogger<TransactionService>>());

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Setup signing - SignAsync(hash, network, privateKey)
        _mockCryptoModule
            .Setup(x => x.SignAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] hash, byte network, byte[] privateKey, CancellationToken ct) =>
            {
                // Return deterministic signature based on hash
                var signature = new byte[64]; // Typical signature size
                Array.Copy(hash, signature, Math.Min(hash.Length, signature.Length));
                return CryptoResult<byte[]>.Success(signature);
            });

        // Setup verification - VerifyAsync(signature, hash, network, publicKey) returns CryptoStatus
        _mockCryptoModule
            .Setup(x => x.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] signature, byte[] hash, byte network, byte[] publicKey, CancellationToken ct) =>
            {
                // Simple verification: check if signature starts with hash
                var isValid = signature.Length >= hash.Length &&
                             signature.Take(hash.Length).SequenceEqual(hash);
                return isValid ? CryptoStatus.Success : CryptoStatus.InvalidSignature;
            });

        // Setup encryption - EncryptAsync(data, network, publicKey)
        _mockCryptoModule
            .Setup(x => x.EncryptAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] data, byte network, byte[] publicKey, CancellationToken ct) =>
            {
                // Simple "encryption": XOR with public key (just for testing)
                var encrypted = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    encrypted[i] = (byte)(data[i] ^ publicKey[i % publicKey.Length]);
                }
                return CryptoResult<byte[]>.Success(encrypted);
            });

        // Setup decryption - DecryptAsync(ciphertext, network, privateKey)
        _mockCryptoModule
            .Setup(x => x.DecryptAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] encrypted, byte network, byte[] privateKey, CancellationToken ct) =>
            {
                // Simple "decryption": XOR with private key (just for testing)
                var decrypted = new byte[encrypted.Length];
                for (int i = 0; i < encrypted.Length; i++)
                {
                    decrypted[i] = (byte)(encrypted[i] ^ privateKey[i % privateKey.Length]);
                }
                return CryptoResult<byte[]>.Success(decrypted);
            });

        // Setup hashing - ComputeHash(data, hashType)
        _mockHashProvider
            .Setup(x => x.ComputeHash(It.IsAny<byte[]>(), It.IsAny<HashType>()))
            .Returns((byte[] data, HashType hashType) =>
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                return sha256.ComputeHash(data);
            });
    }

    [Fact]
    public async Task SignTransactionAsync_ShouldSignTransaction_WithValidParameters()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test transaction data");
        var privateKey = new byte[32];
        Random.Shared.NextBytes(privateKey);
        var algorithm = "ED25519";

        // Act
        var signature = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, algorithm);

        // Assert
        signature.Should().NotBeNull();
        signature.Should().NotBeEmpty();
        _mockCryptoModule.Verify(x => x.SignAsync(
            It.IsAny<byte[]>(), // hash of transaction data
            (byte)WalletNetworks.ED25519,
            privateKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("ED25519")]
    [InlineData("NISTP256")]
    [InlineData("RSA4096")]
    public async Task SignTransactionAsync_ShouldSupportDifferentAlgorithms(string algorithm)
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test data");
        var privateKey = new byte[32];
        Random.Shared.NextBytes(privateKey);

        // Act
        var signature = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, algorithm);

        // Assert
        signature.Should().NotBeNull();
        signature.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SignTransactionAsync_ShouldGenerateDifferentSignatures_ForDifferentData()
    {
        // Arrange
        var data1 = System.Text.Encoding.UTF8.GetBytes("transaction 1");
        var data2 = System.Text.Encoding.UTF8.GetBytes("transaction 2");
        var privateKey = new byte[32];
        Random.Shared.NextBytes(privateKey);

        // Act
        var signature1 = await _transactionService.SignTransactionAsync(data1, privateKey, "ED25519");
        var signature2 = await _transactionService.SignTransactionAsync(data2, privateKey, "ED25519");

        // Assert
        signature1.Should().NotBeEquivalentTo(signature2);
    }

    [Fact]
    public async Task SignTransactionAsync_ShouldGenerateSameSignature_ForSameData()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test transaction");
        var privateKey = new byte[32];
        Random.Shared.NextBytes(privateKey);

        // Act
        var signature1 = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, "ED25519");
        var signature2 = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, "ED25519");

        // Assert
        signature1.Should().BeEquivalentTo(signature2);
    }

    [Fact]
    public async Task SignTransactionAsync_ShouldThrow_WhenTransactionDataIsEmpty()
    {
        // Arrange
        var privateKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.SignTransactionAsync(
                Array.Empty<byte>(), privateKey, "ED25519"));
    }

    [Fact]
    public async Task SignTransactionAsync_ShouldThrow_WhenPrivateKeyIsEmpty()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.SignTransactionAsync(
                transactionData, Array.Empty<byte>(), "ED25519"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SignTransactionAsync_ShouldThrow_WhenAlgorithmIsInvalid(string algorithm)
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test");
        var privateKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.SignTransactionAsync(
                transactionData, privateKey, algorithm!));
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldVerifyValidSignature()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test transaction");
        var privateKey = new byte[32];
        var publicKey = new byte[32];
        Random.Shared.NextBytes(privateKey);
        Random.Shared.NextBytes(publicKey);

        var signature = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, "ED25519");

        // Act
        var isValid = await _transactionService.VerifySignatureAsync(
            transactionData, signature, publicKey, "ED25519");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectInvalidSignature()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test transaction");
        var publicKey = new byte[32];
        Random.Shared.NextBytes(publicKey);
        var invalidSignature = new byte[64];
        Random.Shared.NextBytes(invalidSignature);

        // Act
        var isValid = await _transactionService.VerifySignatureAsync(
            transactionData, invalidSignature, publicKey, "ED25519");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectModifiedData()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("original data");
        var modifiedData = System.Text.Encoding.UTF8.GetBytes("modified data");
        var privateKey = new byte[32];
        var publicKey = new byte[32];
        Random.Shared.NextBytes(privateKey);
        Random.Shared.NextBytes(publicKey);

        var signature = await _transactionService.SignTransactionAsync(
            originalData, privateKey, "ED25519");

        // Act
        var isValid = await _transactionService.VerifySignatureAsync(
            modifiedData, signature, publicKey, "ED25519");

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ED25519")]
    [InlineData("NISTP256")]
    [InlineData("RSA4096")]
    public async Task VerifySignatureAsync_ShouldSupportDifferentAlgorithms(string algorithm)
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test");
        var privateKey = new byte[32];
        var publicKey = new byte[32];
        Random.Shared.NextBytes(privateKey);
        Random.Shared.NextBytes(publicKey);

        var signature = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, algorithm);

        // Act
        var isValid = await _transactionService.VerifySignatureAsync(
            transactionData, signature, publicKey, algorithm);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldThrow_WhenTransactionDataIsEmpty()
    {
        // Arrange
        var signature = new byte[64];
        var publicKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.VerifySignatureAsync(
                Array.Empty<byte>(), signature, publicKey, "ED25519"));
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldThrow_WhenSignatureIsEmpty()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test");
        var publicKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.VerifySignatureAsync(
                transactionData, Array.Empty<byte>(), publicKey, "ED25519"));
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldThrow_WhenPublicKeyIsEmpty()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("test");
        var signature = new byte[64];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.VerifySignatureAsync(
                transactionData, signature, Array.Empty<byte>(), "ED25519"));
    }

    [Fact]
    public async Task EncryptPayloadAsync_ShouldEncryptPayload()
    {
        // Arrange
        var payload = System.Text.Encoding.UTF8.GetBytes("secret payload data");
        var recipientPublicKey = new byte[32];
        Random.Shared.NextBytes(recipientPublicKey);

        // Act
        var encrypted = await _transactionService.EncryptPayloadAsync(
            payload, recipientPublicKey, "ED25519");

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.Should().NotBeEmpty();
        encrypted.Should().NotBeEquivalentTo(payload); // Encrypted should differ from plain
    }

    [Fact]
    public async Task EncryptPayloadAsync_ShouldProduceDifferentCiphertext_ForDifferentPayloads()
    {
        // Arrange
        var payload1 = System.Text.Encoding.UTF8.GetBytes("payload 1");
        var payload2 = System.Text.Encoding.UTF8.GetBytes("payload 2");
        var publicKey = new byte[32];
        Random.Shared.NextBytes(publicKey);

        // Act
        var encrypted1 = await _transactionService.EncryptPayloadAsync(payload1, publicKey, "ED25519");
        var encrypted2 = await _transactionService.EncryptPayloadAsync(payload2, publicKey, "ED25519");

        // Assert
        encrypted1.Should().NotBeEquivalentTo(encrypted2);
    }

    [Theory]
    [InlineData("ED25519")]
    [InlineData("NISTP256")]
    [InlineData("RSA4096")]
    public async Task EncryptPayloadAsync_ShouldSupportDifferentAlgorithms(string algorithm)
    {
        // Arrange
        var payload = System.Text.Encoding.UTF8.GetBytes("test payload");
        var publicKey = new byte[32];
        Random.Shared.NextBytes(publicKey);

        // Act
        var encrypted = await _transactionService.EncryptPayloadAsync(
            payload, publicKey, algorithm);

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EncryptPayloadAsync_ShouldThrow_WhenPayloadIsEmpty()
    {
        // Arrange
        var publicKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.EncryptPayloadAsync(
                Array.Empty<byte>(), publicKey, "ED25519"));
    }

    [Fact]
    public async Task EncryptPayloadAsync_ShouldThrow_WhenPublicKeyIsEmpty()
    {
        // Arrange
        var payload = System.Text.Encoding.UTF8.GetBytes("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.EncryptPayloadAsync(
                payload, Array.Empty<byte>(), "ED25519"));
    }

    [Fact]
    public async Task DecryptPayloadAsync_ShouldDecryptPayload()
    {
        // Arrange
        var originalPayload = System.Text.Encoding.UTF8.GetBytes("secret payload");
        var publicKey = new byte[32];
        var privateKey = new byte[32];
        Random.Shared.NextBytes(publicKey);
        Random.Shared.NextBytes(privateKey);

        var encrypted = await _transactionService.EncryptPayloadAsync(
            originalPayload, publicKey, "ED25519");

        // Act
        var decrypted = await _transactionService.DecryptPayloadAsync(
            encrypted, privateKey, "ED25519");

        // Assert
        decrypted.Should().NotBeNull();
        decrypted.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DecryptPayloadAsync_ShouldThrow_WhenEncryptedPayloadIsEmpty()
    {
        // Arrange
        var privateKey = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.DecryptPayloadAsync(
                Array.Empty<byte>(), privateKey, "ED25519"));
    }

    [Fact]
    public async Task DecryptPayloadAsync_ShouldThrow_WhenPrivateKeyIsEmpty()
    {
        // Arrange
        var encryptedPayload = new byte[32];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _transactionService.DecryptPayloadAsync(
                encryptedPayload, Array.Empty<byte>(), "ED25519"));
    }

    [Fact]
    public async Task EncryptionDecryptionRoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalPayload = System.Text.Encoding.UTF8.GetBytes("This is a secret message!");
        var keyPair = new byte[32];
        Random.Shared.NextBytes(keyPair);

        // Act - Encrypt
        var encrypted = await _transactionService.EncryptPayloadAsync(
            originalPayload, keyPair, "ED25519");

        // Act - Decrypt
        var decrypted = await _transactionService.DecryptPayloadAsync(
            encrypted, keyPair, "ED25519");

        // Assert
        System.Text.Encoding.UTF8.GetString(decrypted).Should().Be(
            System.Text.Encoding.UTF8.GetString(originalPayload));
    }

    [Fact]
    public async Task SignAndVerifyRoundTrip_ShouldSucceed()
    {
        // Arrange
        var transactionData = System.Text.Encoding.UTF8.GetBytes("Complete transaction data");
        var privateKey = new byte[32];
        var publicKey = new byte[32];
        Random.Shared.NextBytes(privateKey);
        Random.Shared.NextBytes(publicKey);

        // Act - Sign
        var signature = await _transactionService.SignTransactionAsync(
            transactionData, privateKey, "ED25519");

        // Act - Verify
        var isValid = await _transactionService.VerifySignatureAsync(
            transactionData, signature, publicKey, "ED25519");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteTransactionFlow_ShouldWork()
    {
        // Arrange
        var transactionPayload = System.Text.Encoding.UTF8.GetBytes("Transfer 100 coins to Alice");
        var senderPrivateKey = new byte[32];
        var senderPublicKey = new byte[32];
        var recipientKeyPair = new byte[32]; // Use same key for both public and private in mock
        Random.Shared.NextBytes(senderPrivateKey);
        Random.Shared.NextBytes(senderPublicKey);
        Random.Shared.NextBytes(recipientKeyPair);

        // Act - Encrypt payload for recipient
        var encryptedPayload = await _transactionService.EncryptPayloadAsync(
            transactionPayload, recipientKeyPair, "ED25519");

        // Act - Sign the encrypted payload
        var signature = await _transactionService.SignTransactionAsync(
            encryptedPayload, senderPrivateKey, "ED25519");

        // Act - Verify signature
        var signatureValid = await _transactionService.VerifySignatureAsync(
            encryptedPayload, signature, senderPublicKey, "ED25519");

        // Act - Decrypt payload (use same key since mock XOR is symmetric)
        var decryptedPayload = await _transactionService.DecryptPayloadAsync(
            encryptedPayload, recipientKeyPair, "ED25519");

        // Assert
        signatureValid.Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(decryptedPayload).Should().Contain("Transfer 100 coins");
    }

    public void Dispose()
    {
        // No disposal needed
    }
}
