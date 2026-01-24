# Cryptography Workflows Reference

## Contents
- Wallet Creation Workflow
- Transaction Signing Workflow
- Key Recovery Workflow
- Symmetric Encryption Workflow
- Testing Crypto Code

---

## Wallet Creation Workflow

### Full Wallet Setup

```csharp
public async Task<WalletInfo> CreateWalletAsync(string? passphrase = null)
{
    // 1. Generate HD wallet with mnemonic
    var keyRingResult = await _keyManager.CreateMasterKeyRingAsync(
        WalletNetworks.ED25519,
        passphrase);
    
    if (!keyRingResult.IsSuccess)
        throw new InvalidOperationException($"Wallet creation failed: {keyRingResult.Status}");
    
    var keyRing = keyRingResult.Value!;
    
    // 2. CRITICAL: User must backup mnemonic
    // keyRing.Mnemonic = "word1 word2 ... word12"
    
    // 3. Derive address key using BIP44 path
    var (privKey, pubKey) = await _keyManagement.DeriveKeyAtPathAsync(
        keyRing.MasterKeySet.PrivateKey.Key!,
        new DerivationPath("m/44'/0'/0'/0/0"),
        "ED25519");
    
    // 4. Generate wallet address
    var address = _walletUtilities.PublicKeyToWallet(
        pubKey,
        (byte)WalletNetworks.ED25519);
    
    // 5. Encrypt private key for storage
    var encrypted = await _encryptionProvider.EncryptAsync(
        privKey,
        "wallet-primary-key");
    
    // 6. Clean up sensitive data
    keyRing.Zeroize();
    Array.Clear(privKey);
    
    return new WalletInfo
    {
        Address = address!,
        PublicKey = pubKey,
        EncryptedPrivateKey = encrypted
    };
}
```

Copy this checklist and track progress:
- [ ] Generate KeyRing with mnemonic
- [ ] Display mnemonic for user backup (NEVER log)
- [ ] Derive address key at BIP44 path
- [ ] Generate Bech32 address
- [ ] Encrypt private key with IEncryptionProvider
- [ ] Zeroize all sensitive memory
- [ ] Store encrypted key and address

---

## Transaction Signing Workflow

### Sign-Verify Round Trip

```csharp
public async Task<SignedTransaction> SignTransactionAsync(
    Transaction tx,
    string encryptedPrivateKey,
    string keyId)
{
    // 1. Serialize transaction to bytes
    var txBytes = _serializer.Serialize(tx);
    
    // 2. Decrypt private key
    var privateKey = await _encryptionProvider.DecryptAsync(
        encryptedPrivateKey,
        keyId);
    
    try
    {
        // 3. Hash transaction data
        var hash = _hashProvider.ComputeHash(txBytes, HashType.SHA256);
        
        // 4. Sign hash
        var signResult = await _cryptoModule.SignAsync(
            hash,
            (byte)WalletNetworks.ED25519,
            privateKey);
        
        if (!signResult.IsSuccess)
            throw new CryptographicException($"Signing failed: {signResult.Status}");
        
        return new SignedTransaction
        {
            Transaction = tx,
            Signature = signResult.Value!,
            Hash = hash
        };
    }
    finally
    {
        // 5. Always zeroize decrypted key
        Array.Clear(privateKey);
    }
}
```

### Verification Workflow

```csharp
public async Task<bool> VerifyTransactionAsync(
    SignedTransaction signedTx,
    byte[] signerPublicKey)
{
    // 1. Re-serialize and hash
    var txBytes = _serializer.Serialize(signedTx.Transaction);
    var hash = _hashProvider.ComputeHash(txBytes, HashType.SHA256);
    
    // 2. Verify hash matches
    if (!CryptographicOperations.FixedTimeEquals(hash, signedTx.Hash))
        return false;
    
    // 3. Verify signature
    var status = await _cryptoModule.VerifyAsync(
        signedTx.Signature,
        hash,
        (byte)WalletNetworks.ED25519,
        signerPublicKey);
    
    return status == CryptoStatus.Success;
}
```

Validation feedback loop:
1. Serialize transaction
2. Compute hash
3. Verify signature against hash
4. If verification fails, check: key mismatch, corrupted data, wrong algorithm
5. Only proceed when `CryptoStatus.Success`

---

## Key Recovery Workflow

### Recover from Mnemonic

```csharp
public async Task<RecoveredWallet> RecoverWalletAsync(
    string mnemonic,
    string? passphrase = null)
{
    // 1. Validate mnemonic format
    if (!_keyManager.ValidateMnemonic(mnemonic))
        throw new ArgumentException("Invalid mnemonic phrase");
    
    // 2. Recover KeyRing
    var keyRingResult = await _keyManager.RecoverMasterKeyRingAsync(
        mnemonic,
        passphrase);
    
    if (!keyRingResult.IsSuccess)
        throw new InvalidOperationException($"Recovery failed: {keyRingResult.Status}");
    
    var keyRing = keyRingResult.Value!;
    
    // 3. Re-derive same address key
    var (privKey, pubKey) = await _keyManagement.DeriveKeyAtPathAsync(
        keyRing.MasterKeySet.PrivateKey.Key!,
        new DerivationPath("m/44'/0'/0'/0/0"),
        "ED25519");
    
    // 4. Regenerate address (should match original)
    var address = _walletUtilities.PublicKeyToWallet(
        pubKey,
        (byte)WalletNetworks.ED25519);
    
    // 5. Re-encrypt for storage
    var encrypted = await _encryptionProvider.EncryptAsync(
        privKey,
        "recovered-wallet-key");
    
    keyRing.Zeroize();
    Array.Clear(privKey);
    
    return new RecoveredWallet
    {
        Address = address!,
        PublicKey = pubKey,
        EncryptedPrivateKey = encrypted
    };
}
```

---

## Symmetric Encryption Workflow

### Encrypt Sensitive Payload

```csharp
public async Task<EncryptedPayload> EncryptPayloadAsync(byte[] payload)
{
    // 1. Generate random key (XChaCha20 preferred for 24-byte nonce)
    var result = await _symmetricCrypto.EncryptAsync(
        payload,
        EncryptionType.XCHACHA20_POLY1305);
    
    if (!result.IsSuccess)
        throw new CryptographicException($"Encryption failed: {result.Status}");
    
    var ciphertext = result.Value!;
    
    // 2. Key must be stored/transmitted securely
    // 3. Ciphertext includes IV, safe to store/transmit
    return new EncryptedPayload
    {
        Data = ciphertext.Data,
        KeyId = await StoreKeySecurelyAsync(ciphertext.Key),
        Algorithm = ciphertext.Type.ToString()
    };
}
```

### Decrypt Payload

```csharp
public async Task<byte[]> DecryptPayloadAsync(EncryptedPayload encrypted)
{
    // 1. Retrieve key from secure storage
    var key = await RetrieveKeyAsync(encrypted.KeyId);
    
    // 2. Reconstruct ciphertext object
    var ciphertext = new SymmetricCiphertext
    {
        Data = encrypted.Data,
        Key = key,
        Type = Enum.Parse<EncryptionType>(encrypted.Algorithm)
    };
    
    // 3. Decrypt
    var result = await _symmetricCrypto.DecryptAsync(ciphertext);
    
    if (!result.IsSuccess)
        throw new CryptographicException($"Decryption failed: {result.Status}");
    
    return result.Value!;
}
```

---

## Testing Crypto Code

### Unit Test Pattern

See the **xunit** and **fluent-assertions** skills for testing setup.

```csharp
[Theory]
[InlineData(WalletNetworks.ED25519)]
[InlineData(WalletNetworks.NISTP256)]
[InlineData(WalletNetworks.RSA4096)]
public async Task SignAndVerify_AllAlgorithms_ShouldRoundTrip(WalletNetworks network)
{
    // Arrange
    var keySetResult = await _cryptoModule.GenerateKeySetAsync(network);
    keySetResult.IsSuccess.Should().BeTrue();
    
    var keySet = keySetResult.Value!;
    var testData = Encoding.UTF8.GetBytes("test transaction data");
    var hash = SHA256.HashData(testData);
    
    // Act - Sign
    var signResult = await _cryptoModule.SignAsync(
        hash,
        (byte)network,
        keySet.PrivateKey.Key!);
    
    // Assert - Sign succeeded
    signResult.IsSuccess.Should().BeTrue();
    signResult.Value.Should().NotBeNullOrEmpty();
    
    // Act - Verify
    var verifyStatus = await _cryptoModule.VerifyAsync(
        signResult.Value!,
        hash,
        (byte)network,
        keySet.PublicKey.Key!);
    
    // Assert - Verification passed
    verifyStatus.Should().Be(CryptoStatus.Success);
}

[Fact]
public async Task Verify_WrongPublicKey_ShouldFail()
{
    // Arrange
    var keySet1 = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value!;
    var keySet2 = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value!;
    
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes("test"));
    var signature = (await _cryptoModule.SignAsync(
        hash,
        (byte)WalletNetworks.ED25519,
        keySet1.PrivateKey.Key!)).Value!;
    
    // Act - Verify with wrong key
    var status = await _cryptoModule.VerifyAsync(
        signature,
        hash,
        (byte)WalletNetworks.ED25519,
        keySet2.PublicKey.Key!);  // Wrong key
    
    // Assert
    status.Should().Be(CryptoStatus.InvalidSignature);
}
```

### Test Checklist

Copy this checklist for crypto test coverage:
- [ ] Key generation for all algorithms
- [ ] Sign/verify round trip for all algorithms
- [ ] Verification fails with wrong public key
- [ ] Verification fails with tampered data
- [ ] Encrypt/decrypt round trip
- [ ] Decryption fails with wrong key
- [ ] Mnemonic generation (12, 24 words)
- [ ] Mnemonic recovery produces same keys
- [ ] Invalid mnemonic rejected
- [ ] Key zeroization clears memory