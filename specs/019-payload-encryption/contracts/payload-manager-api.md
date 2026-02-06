# Internal API Contract: IPayloadManager

**Feature**: 019-payload-encryption
**Scope**: Internal library API (no REST/gRPC — this is a C# interface used by TransactionBuilder)

## Constructor

```
PayloadManager(ISymmetricCrypto symmetricCrypto, ICryptoModule cryptoModule, IHashProvider hashProvider)
```

All three parameters are required. Throws `ArgumentNullException` if any is null.

## Modified Methods

### AddPayloadAsync

```
Task<PayloadResult> AddPayloadAsync(
    byte[] data,
    RecipientKeyInfo[] recipients,
    PayloadOptions? options = null,
    CancellationToken cancellationToken = default)
```

**Behavior**:
1. Validate `data` is non-null, non-empty → `InvalidPayload` on failure
2. Validate `recipients` has at least one entry → `InvalidRecipients` on failure
3. Compute SHA-256 hash of `data` via `IHashProvider.ComputeHash(data, options.HashType)`
4. Generate random symmetric key via `ISymmetricCrypto.GenerateKey(options.EncryptionType)`
5. Generate random IV via `ISymmetricCrypto.GenerateIV(options.EncryptionType)`
6. Encrypt `data` with symmetric key + IV via `ISymmetricCrypto.EncryptAsync(data, options.EncryptionType, key)`
7. For each recipient: encrypt symmetric key via `ICryptoModule.EncryptAsync(key, recipient.Network, recipient.PublicKey)`
8. Zeroize symmetric key from memory
9. Store payload with encrypted data, IV, hash, encrypted keys
10. Return `PayloadResult.Success(payloadId)`

**Error Cases**:
- `InvalidPayload`: null/empty data, encryption failure
- `InvalidRecipients`: no recipients, key encryption failure for any recipient

### GetPayloadDataAsync

```
Task<PayloadResult<byte[]>> GetPayloadDataAsync(
    uint payloadId,
    DecryptionKeyInfo keyInfo,
    CancellationToken cancellationToken = default)
```

**Behavior**:
1. Find payload by ID → `InvalidPayload` if not found
2. Check `keyInfo.WalletAddress` in `EncryptedKeys` → `AccessDenied` if not present
3. **Legacy check**: If IV is all zeros, return `Data` as-is (backward compatibility)
4. Decrypt symmetric key via `ICryptoModule.DecryptAsync(encryptedKey, keyInfo.Network, keyInfo.PrivateKey)`
5. Reconstruct `SymmetricCiphertext` with encrypted data + decrypted key + IV + encryption type
6. Decrypt data via `ISymmetricCrypto.DecryptAsync(ciphertext)`
7. Zeroize symmetric key from memory
8. Return `PayloadResult<byte[]>.Success(plaintext)`

### GrantAccessAsync

```
Task<TransactionStatus> GrantAccessAsync(
    uint payloadId,
    RecipientKeyInfo newRecipient,
    DecryptionKeyInfo ownerKeyInfo)
```

**Behavior**:
1. Find payload by ID → `InvalidPayload` if not found
2. If `newRecipient.WalletAddress` already in `EncryptedKeys` → return `Success`
3. Decrypt owner's symmetric key via `ICryptoModule.DecryptAsync()`
4. Encrypt symmetric key for new recipient via `ICryptoModule.EncryptAsync()`
5. Add new entry to `EncryptedKeys`
6. Zeroize symmetric key
7. Return `Success`

### VerifyPayloadAsync

```
Task<bool> VerifyPayloadAsync(uint payloadId)
```

**Behavior**:
1. Find payload by ID → return `false` if not found
2. **Legacy check**: If `Hash` is all zeros, return `true` (skip verification for legacy)
3. Note: Full verification requires decryption key. Without it, can only verify the hash field exists and is non-zero. For full verification with decryption, use the overload with `DecryptionKeyInfo`.

### VerifyPayloadAsync (with decryption)

```
Task<bool> VerifyPayloadAsync(uint payloadId, DecryptionKeyInfo keyInfo)
```

**Behavior**:
1. Decrypt payload data using `GetPayloadDataAsync`
2. Compute hash of decrypted data
3. Compare with stored hash
4. Return match result

## New Types

### RecipientKeyInfo

```csharp
public record RecipientKeyInfo(
    string WalletAddress,
    byte[] PublicKey,
    WalletNetworks Network);
```

### DecryptionKeyInfo

```csharp
public record DecryptionKeyInfo(
    string WalletAddress,
    byte[] PrivateKey,
    WalletNetworks Network);
```

## Backward Compatibility

The existing `IPayloadManager` interface with `string[] recipientWallets` and `string wifPrivateKey` signatures will be preserved as overloads or with default parameter handling to avoid breaking external callers. Internal callers (TransactionBuilder, TransactionBuilderService) will be updated to use the new signatures.
