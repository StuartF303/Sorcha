# Task: Implement PayloadManager

**ID:** TX-005
**Status:** Not Started
**Priority:** Critical
**Estimate:** 14 hours
**Created:** 2025-11-12

## Objective

Implement multi-recipient payload encryption with per-recipient access control, compression, and verification.

## Implementation Details

### Interfaces/IPayloadManager.cs
```csharp
public interface IPayloadManager
{
    // Query
    int Count { get; }
    Task<IEnumerable<IPayload>> GetAllAsync();
    Task<IEnumerable<IPayload>> GetAccessibleAsync(
        string walletAddress,
        string wifPrivateKey);

    // CRUD
    Task<PayloadResult> AddPayloadAsync(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<PayloadResult<byte[]>> GetPayloadDataAsync(
        uint payloadId,
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    Task<TransactionStatus> RemovePayloadAsync(uint payloadId);

    // Access Control
    Task<TransactionStatus> GrantAccessAsync(
        uint payloadId,
        string recipientWallet,
        string ownerWifKey);

    Task<TransactionStatus> RevokeAccessAsync(
        uint payloadId,
        string recipientWallet);

    // Verification
    Task<bool> VerifyAllAsync();
    Task<bool> VerifyPayloadAsync(uint payloadId);
}
```

### Payload/PayloadManager.cs

**Multi-Recipient Encryption Process:**
```csharp
public async Task<PayloadResult> AddPayloadAsync(
    byte[] data,
    string[] recipientWallets,
    PayloadOptions? options)
{
    // 1. Compress (if beneficial)
    var (compressed, wasCompressed) = compressionUtils.Compress(
        data, options?.CompressionType ?? CompressionType.Balanced);

    // 2. Symmetric encryption
    var encResult = await symmetricCrypto.EncryptAsync(
        compressed,
        options?.EncryptionType ?? EncryptionType.XCHACHA20_POLY1305);

    // 3. Per-recipient key encryption
    var encryptedKeys = new Dictionary<string, byte[]>();
    foreach (var wallet in recipientWallets)
    {
        var walletInfo = walletUtils.WalletToPublicKey(wallet);
        var keyResult = await cryptoModule.EncryptAsync(
            encResult.Value.Key,
            walletInfo.Value.Network,
            walletInfo.Value.PublicKey);

        encryptedKeys[wallet] = keyResult.Value;
    }

    // 4. Hash original data
    var hash = hashProvider.ComputeHash(
        data,
        options?.HashType ?? HashType.SHA256);

    // 5. Create payload
    var payload = new Payload
    {
        Id = GetNextPayloadId(),
        Data = encResult.Value.Data,
        IV = encResult.Value.IV,
        Hash = hash,
        EncryptedKeys = encryptedKeys,
        EncryptionType = encResult.Value.Type,
        IsCompressed = wasCompressed,
        OriginalSize = data.Length
    };

    _payloads.Add(payload);
    return PayloadResult.Success(payload.Id);
}
```

**Decryption Process:**
```csharp
public async Task<PayloadResult<byte[]>> GetPayloadDataAsync(
    uint payloadId,
    string wifPrivateKey)
{
    var payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
    if (payload == null)
        return PayloadResult<byte[]>.Failure(
            TransactionStatus.InvalidPayload);

    // 1. Get wallet address from WIF key
    var keyInfo = walletUtils.WIFToPrivateKey(wifPrivateKey);
    var wallet = walletUtils.PublicKeyToWallet(
        /* calculate public key */, keyInfo.Value.Network);

    // 2. Check access
    if (!payload.EncryptedKeys.ContainsKey(wallet))
        return PayloadResult<byte[]>.Failure(
            TransactionStatus.AccessDenied);

    // 3. Decrypt symmetric key
    var encryptedKey = payload.EncryptedKeys[wallet];
    var keyResult = await cryptoModule.DecryptAsync(
        encryptedKey,
        keyInfo.Value.Network,
        keyInfo.Value.PrivateKey);

    // 4. Decrypt data
    var ciphertext = new SymmetricCiphertext
    {
        Data = payload.Data,
        Key = keyResult.Value,
        IV = payload.IV,
        Type = payload.EncryptionType
    };

    var dataResult = await symmetricCrypto.DecryptAsync(ciphertext);

    // 5. Decompress if needed
    var finalData = payload.IsCompressed
        ? compressionUtils.Decompress(dataResult.Value)
        : dataResult.Value;

    // 6. Verify hash
    var hash = hashProvider.ComputeHash(finalData, payload.HashType);
    if (!hash.SequenceEqual(payload.Hash))
        return PayloadResult<byte[]>.Failure(
            TransactionStatus.InvalidPayload);

    return PayloadResult<byte[]>.Success(finalData);
}
```

## Testing Requirements

- [ ] Add payload with single recipient
- [ ] Add payload with multiple recipients
- [ ] Decrypt payload with correct key
- [ ] Decrypt fails with wrong key
- [ ] Access control (grant/revoke)
- [ ] Compression working
- [ ] Hash verification working
- [ ] Multiple payloads in transaction

## Acceptance Criteria

- [ ] IPayloadManager interface complete
- [ ] Multi-recipient encryption working
- [ ] Per-recipient access control working
- [ ] Compression integrated
- [ ] Hash verification working
- [ ] All unit tests passing (>90% coverage)

---

**Dependencies:** TX-001, TX-002, TX-003, Sorcha.Cryptography v2.0
