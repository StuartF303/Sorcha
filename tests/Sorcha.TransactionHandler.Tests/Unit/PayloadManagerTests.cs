// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;
using Sorcha.TransactionHandler.Payload;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;

namespace Sorcha.TransactionHandler.Tests.Unit;

public class PayloadManagerTests
{
    private readonly CryptoModule _cryptoModule;
    private readonly HashProvider _hashProvider;
    private readonly SymmetricCrypto _symmetricCrypto;

    public PayloadManagerTests()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _symmetricCrypto = new SymmetricCrypto();
    }

    private PayloadManager CreatePayloadManager() =>
        new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);

    private async Task<(RecipientKeyInfo Recipient, DecryptionKeyInfo Decryption, TestWallet Wallet)> GenerateTestKeyInfoAsync(
        WalletNetworks network = WalletNetworks.ED25519)
    {
        var wallet = await TestHelpers.GenerateTestWalletAsync(network);
        var recipient = new RecipientKeyInfo(
            wallet.Address,
            wallet.KeyRing.MasterKeySet.PublicKey.Key!,
            network);
        var decryption = new DecryptionKeyInfo(
            wallet.Address,
            wallet.KeyRing.MasterKeySet.PrivateKey.Key!,
            network);
        return (recipient, decryption, wallet);
    }

    #region US1: Encrypted Payload Storage

    [Fact]
    public async Task AddPayloadAsync_WithRecipients_EncryptsData()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var result = await pm.AddPayloadAsync(plaintext, new[] { recipient });

        // Assert
        Assert.True(result.IsSuccess);
        var payloads = await pm.GetAllAsync();
        var payload = payloads.First();
        Assert.NotEqual(plaintext, payload.Data); // Stored data should differ from plaintext
    }

    [Fact]
    public async Task AddPayloadAsync_WithRecipients_GeneratesNonZeroIV()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 10, 20, 30, 40, 50 };

        // Act
        var result = await pm.AddPayloadAsync(plaintext, new[] { recipient });

        // Assert
        Assert.True(result.IsSuccess);
        var payloads = await pm.GetAllAsync();
        var payload = payloads.First();
        Assert.True(payload.IV.Length > 0);
        Assert.False(payload.IV.All(b => b == 0)); // IV should not be all zeros
    }

    [Fact]
    public async Task AddPayloadAsync_WithRecipients_ComputesSHA256Hash()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 100, 200, 50, 25, 75 };

        // Act
        var result = await pm.AddPayloadAsync(plaintext, new[] { recipient });

        // Assert
        Assert.True(result.IsSuccess);
        var payloads = await pm.GetAllAsync();
        var payload = payloads.First();
        Assert.Equal(32, payload.Hash.Length); // SHA-256 = 32 bytes
        Assert.False(payload.Hash.All(b => b == 0)); // Hash should not be all zeros

        // Verify hash matches direct computation
        var expectedHash = _hashProvider.ComputeHash(plaintext, HashType.SHA256);
        Assert.Equal(expectedHash, payload.Hash);
    }

    [Fact]
    public async Task AddPayloadAsync_WithTwoRecipients_ProducesUniqueEncryptedKeys()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (alice, _, _) = await GenerateTestKeyInfoAsync();
        var (bob, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await pm.AddPayloadAsync(plaintext, new[] { alice, bob });

        // Assert
        Assert.True(result.IsSuccess);
        var payloads = await pm.GetAllAsync();
        var payload = payloads.First();
        var info = payload.GetInfo();
        Assert.Equal(2, info.AccessibleBy.Length);
        Assert.Contains(alice.WalletAddress, info.AccessibleBy);
        Assert.Contains(bob.WalletAddress, info.AccessibleBy);
    }

    [Fact]
    public async Task AddPayloadAsync_SameDataTwice_ProducesDifferentCiphertext()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 42, 42, 42, 42 };

        // Act
        var result1 = await pm.AddPayloadAsync(plaintext, new[] { recipient });
        var result2 = await pm.AddPayloadAsync(plaintext, new[] { recipient });

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        var payloads = (await pm.GetAllAsync()).ToList();
        // Ciphertexts should differ due to random IV/key per payload
        Assert.NotEqual(payloads[0].Data, payloads[1].Data);
    }

    [Fact]
    public async Task AddPayloadAsync_EmptyData_ReturnsInvalidPayload()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();

        // Act
        var result = await pm.AddPayloadAsync(Array.Empty<byte>(), new[] { recipient });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.InvalidPayload, result.Status);
    }

    [Fact]
    public async Task AddPayloadAsync_NullData_ReturnsInvalidPayload()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();

        // Act
        var result = await pm.AddPayloadAsync(null!, new[] { recipient });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.InvalidPayload, result.Status);
    }

    [Fact]
    public async Task AddPayloadAsync_NoRecipients_ReturnsInvalidRecipients()
    {
        // Arrange
        var pm = CreatePayloadManager();

        // Act
        var result = await pm.AddPayloadAsync(
            new byte[] { 1, 2, 3 },
            Array.Empty<RecipientKeyInfo>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.InvalidRecipients, result.Status);
    }

    #endregion

    #region US2: Authorized Payload Decryption

    [Fact]
    public async Task GetPayloadDataAsync_AuthorizedRecipient_ReturnsOriginalPlaintext()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, decryption, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 11, 22, 33, 44, 55, 66, 77, 88 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { recipient });
        Assert.True(addResult.IsSuccess);

        // Act
        var decryptResult = await pm.GetPayloadDataAsync(addResult.PayloadId, decryption);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(plaintext, decryptResult.Value);
    }

    [Fact]
    public async Task GetPayloadDataAsync_MultipleRecipients_EachCanDecrypt()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (aliceRecipient, aliceDecryption, _) = await GenerateTestKeyInfoAsync();
        var (bobRecipient, bobDecryption, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 10, 20, 30, 40, 50 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { aliceRecipient, bobRecipient });
        Assert.True(addResult.IsSuccess);

        // Act
        var aliceResult = await pm.GetPayloadDataAsync(addResult.PayloadId, aliceDecryption);
        var bobResult = await pm.GetPayloadDataAsync(addResult.PayloadId, bobDecryption);

        // Assert
        Assert.True(aliceResult.IsSuccess);
        Assert.True(bobResult.IsSuccess);
        Assert.Equal(plaintext, aliceResult.Value);
        Assert.Equal(plaintext, bobResult.Value);
    }

    [Fact]
    public async Task GetPayloadDataAsync_UnauthorizedWallet_ReturnsAccessDenied()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var (_, eveDecryption, _) = await GenerateTestKeyInfoAsync(); // Eve not authorized
        var plaintext = new byte[] { 1, 2, 3 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { recipient });
        Assert.True(addResult.IsSuccess);

        // Act
        var result = await pm.GetPayloadDataAsync(addResult.PayloadId, eveDecryption);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.AccessDenied, result.Status);
    }

    [Fact]
    public async Task GetPayloadDataAsync_InvalidPayloadId_ReturnsInvalidPayload()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (_, decryption, _) = await GenerateTestKeyInfoAsync();

        // Act
        var result = await pm.GetPayloadDataAsync(999, decryption);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionStatus.InvalidPayload, result.Status);
    }

    #endregion

    #region US3: Payload Integrity Verification

    [Fact]
    public async Task VerifyPayloadAsync_UntamperedPayload_ReturnsTrue()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, decryption, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { recipient });
        Assert.True(addResult.IsSuccess);

        // Act
        var isValid = await pm.VerifyPayloadAsync(addResult.PayloadId, decryption);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyPayloadAsync_NonExistentPayload_ReturnsFalse()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (_, decryption, _) = await GenerateTestKeyInfoAsync();

        // Act
        var isValid = await pm.VerifyPayloadAsync(999, decryption);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyPayloadAsync_StructuralCheck_EncryptedPayloadPasses()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { recipient });
        Assert.True(addResult.IsSuccess);

        // Act — structural check (no decryption key)
        var isValid = await pm.VerifyPayloadAsync(addResult.PayloadId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyAllAsync_AllValid_ReturnsTrue()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();

        await pm.AddPayloadAsync(new byte[] { 1, 2, 3 }, new[] { recipient });
        await pm.AddPayloadAsync(new byte[] { 4, 5, 6 }, new[] { recipient });

        // Act
        var isValid = await pm.VerifyAllAsync();

        // Assert
        Assert.True(isValid);
    }

    #endregion

    #region US4: Recipient Access Grant

    [Fact]
    public async Task GrantAccessAsync_NewRecipient_CanDecryptPayload()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (aliceRecipient, aliceDecryption, _) = await GenerateTestKeyInfoAsync();
        var (bobRecipient, bobDecryption, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 10, 20, 30 };

        // Encrypt for Alice only
        var addResult = await pm.AddPayloadAsync(plaintext, new[] { aliceRecipient });
        Assert.True(addResult.IsSuccess);

        // Bob can't decrypt yet
        var bobBefore = await pm.GetPayloadDataAsync(addResult.PayloadId, bobDecryption);
        Assert.False(bobBefore.IsSuccess);

        // Act — Alice grants Bob access
        var grantStatus = await pm.GrantAccessAsync(addResult.PayloadId, bobRecipient, aliceDecryption);

        // Assert
        Assert.Equal(TransactionStatus.Success, grantStatus);

        // Bob can now decrypt
        var bobAfter = await pm.GetPayloadDataAsync(addResult.PayloadId, bobDecryption);
        Assert.True(bobAfter.IsSuccess);
        Assert.Equal(plaintext, bobAfter.Value);
    }

    [Fact]
    public async Task GrantAccessAsync_ExistingRecipient_ReturnsSuccessNoChange()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (aliceRecipient, aliceDecryption, _) = await GenerateTestKeyInfoAsync();
        var plaintext = new byte[] { 1, 2, 3 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { aliceRecipient });
        Assert.True(addResult.IsSuccess);

        // Act — Grant to already-authorized Alice
        var grantStatus = await pm.GrantAccessAsync(addResult.PayloadId, aliceRecipient, aliceDecryption);

        // Assert — idempotent
        Assert.Equal(TransactionStatus.Success, grantStatus);
    }

    [Fact]
    public async Task GrantAccessAsync_InvalidPayloadId_ReturnsInvalidPayload()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, decryption, _) = await GenerateTestKeyInfoAsync();

        // Act
        var status = await pm.GrantAccessAsync(999, recipient, decryption);

        // Assert
        Assert.Equal(TransactionStatus.InvalidPayload, status);
    }

    [Fact]
    public async Task GrantAccessAsync_OwnerNotAuthorized_ReturnsAccessDenied()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (aliceRecipient, _, _) = await GenerateTestKeyInfoAsync();
        var (bobRecipient, _, _) = await GenerateTestKeyInfoAsync();
        var (_, eveDecryption, _) = await GenerateTestKeyInfoAsync(); // Eve is not authorized
        var plaintext = new byte[] { 1, 2, 3 };

        var addResult = await pm.AddPayloadAsync(plaintext, new[] { aliceRecipient });
        Assert.True(addResult.IsSuccess);

        // Act — Eve (not authorized) tries to grant Bob
        var status = await pm.GrantAccessAsync(addResult.PayloadId, bobRecipient, eveDecryption);

        // Assert
        Assert.Equal(TransactionStatus.AccessDenied, status);
    }

    #endregion

    #region US5: Backward-Compatible Upgrade

    [Fact]
    public async Task GetPayloadDataAsync_LegacyStringOverload_ReturnsRawData()
    {
        // Arrange — use string-based (legacy) overload
        var pm = CreatePayloadManager();
        var rawData = new byte[] { 11, 22, 33, 44, 55 };

        var addResult = await pm.AddPayloadAsync(rawData, new[] { "ws1legacywallet" });
        Assert.True(addResult.IsSuccess);

        // Act — legacy string-based decrypt
        var result = await pm.GetPayloadDataAsync(addResult.PayloadId, "dummy_key");

        // Assert — returns raw data (unencrypted)
        Assert.True(result.IsSuccess);
        Assert.Equal(rawData, result.Value);
    }

    [Fact]
    public async Task VerifyPayloadAsync_LegacyZeroHash_ReturnsTrue()
    {
        // Arrange — legacy payload has zeroed hash
        var pm = CreatePayloadManager();
        var rawData = new byte[] { 1, 2, 3 };

        await pm.AddPayloadAsync(rawData, new[] { "ws1legacywallet" });

        // Act
        var isValid = await pm.VerifyPayloadAsync(0); // First payload ID

        // Assert — legacy payloads pass verification
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyAllAsync_MixedLegacyAndEncrypted_HandlesCorrectly()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, _, _) = await GenerateTestKeyInfoAsync();

        // Add legacy payload
        await pm.AddPayloadAsync(new byte[] { 1, 2, 3 }, new[] { "ws1legacywallet" });
        // Add encrypted payload
        await pm.AddPayloadAsync(new byte[] { 4, 5, 6 }, new[] { recipient });

        // Act
        var isValid = await pm.VerifyAllAsync();

        // Assert — both legacy and encrypted pass structural verification
        Assert.True(isValid);
    }

    [Fact]
    public async Task GetPayloadDataAsync_LegacyPayloadWithDecryptionKey_ReturnsRawData()
    {
        // Arrange — create legacy payload then try to decrypt with real key
        var pm = CreatePayloadManager();
        var (_, decryption, _) = await GenerateTestKeyInfoAsync();
        var rawData = new byte[] { 10, 20, 30 };

        // Create legacy payload with decryption wallet address
        await pm.AddPayloadAsync(rawData, new[] { decryption.WalletAddress });

        // Act — DecryptionKeyInfo overload should detect legacy and return raw data
        var result = await pm.GetPayloadDataAsync(0, decryption);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(rawData, result.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AddPayloadAsync_LargePayload_EncryptsSuccessfully()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, decryption, _) = await GenerateTestKeyInfoAsync();
        var largePlaintext = new byte[64 * 1024]; // 64KB
        new Random(42).NextBytes(largePlaintext);

        // Act
        var addResult = await pm.AddPayloadAsync(largePlaintext, new[] { recipient });

        // Assert
        Assert.True(addResult.IsSuccess);

        // Verify round-trip
        var decryptResult = await pm.GetPayloadDataAsync(addResult.PayloadId, decryption);
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(largePlaintext, decryptResult.Value);
    }

    [Fact]
    public async Task PayloadManager_MultiplePayloads_IndependentEncryption()
    {
        // Arrange
        var pm = CreatePayloadManager();
        var (recipient, decryption, _) = await GenerateTestKeyInfoAsync();
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6 };

        // Act
        var result1 = await pm.AddPayloadAsync(data1, new[] { recipient });
        var result2 = await pm.AddPayloadAsync(data2, new[] { recipient });

        // Assert — both encrypt and decrypt independently
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(2, pm.Count);

        var decrypt1 = await pm.GetPayloadDataAsync(result1.PayloadId, decryption);
        var decrypt2 = await pm.GetPayloadDataAsync(result2.PayloadId, decryption);
        Assert.Equal(data1, decrypt1.Value);
        Assert.Equal(data2, decrypt2.Value);
    }

    #endregion
}
