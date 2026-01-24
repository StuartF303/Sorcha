# Cryptography Patterns Reference

## Contents
- Algorithm Selection
- Signing and Verification
- Encryption Patterns
- Key Size Reference
- Anti-Patterns

---

## Algorithm Selection

| Algorithm | Best For | Key Sizes | Signature Size |
|-----------|----------|-----------|----------------|
| ED25519 | General signing, fast operations | Pub: 32B, Priv: 64B | 64B |
| P-256 | FIPS compliance, interop | Pub: 64B, Priv: 32B | ~70B (DER) |
| RSA-4096 | Legacy systems, certificates | Variable (DER) | 512B |

**Decision Tree:**
```
Need signing?
├── Performance critical? → ED25519
├── FIPS compliance required? → P-256
└── Legacy/PKI interop? → RSA-4096

Need encryption?
├── Small data (<446 bytes)? → RSA-4096
├── Any size data? → ED25519 (SealedBox)
└── P-256 encryption? → NOT SUPPORTED (use symmetric)
```

---

## Signing and Verification

### ED25519 Pattern (Preferred)

```csharp
// GOOD - Standard signing workflow
public async Task<byte[]> SignDataAsync(byte[] data, byte[] privateKey)
{
    // 1. Hash first (required for deterministic signatures)
    var hash = _hashProvider.ComputeHash(data, HashType.SHA256);
    
    // 2. Sign the hash, not raw data
    var result = await _cryptoModule.SignAsync(
        hash,
        (byte)WalletNetworks.ED25519,
        privateKey);
    
    if (!result.IsSuccess)
        throw new CryptographicException($"Sign failed: {result.Status}");
    
    return result.Value!;
}
```

### RSA-4096 Pattern

```csharp
// GOOD - RSA signing with proper padding
var result = await _cryptoModule.SignAsync(
    hash,
    (byte)WalletNetworks.RSA4096,
    privateKey);
// Uses PKCS#1 v1.5 padding with SHA-256 internally
```

### Verification Pattern

```csharp
// GOOD - Always verify before trusting
public async Task<bool> VerifySignatureAsync(
    byte[] data,
    byte[] signature,
    byte[] publicKey,
    WalletNetworks network)
{
    var hash = _hashProvider.ComputeHash(data, HashType.SHA256);
    
    var status = await _cryptoModule.VerifyAsync(
        signature,
        hash,
        (byte)network,
        publicKey);
    
    return status == CryptoStatus.Success;
}
```

---

## Encryption Patterns

### Asymmetric Encryption (ED25519)

```csharp
// GOOD - Sealed box encryption (no sender key needed)
var encrypted = await _cryptoModule.EncryptAsync(
    plaintext,
    (byte)WalletNetworks.ED25519,
    recipientPublicKey);

// Decryption requires matching private key
var decrypted = await _cryptoModule.DecryptAsync(
    encrypted.Value!,
    (byte)WalletNetworks.ED25519,
    recipientPrivateKey);
```

### Symmetric Encryption (Large Data)

```csharp
// GOOD - XChaCha20-Poly1305 for large payloads
var encrypted = await _symmetricCrypto.EncryptAsync(
    largePayload,
    EncryptionType.XCHACHA20_POLY1305);

// encrypted.Key must be stored securely
// encrypted.Data can be transmitted/stored
// encrypted.IV is included in ciphertext
```

### RSA Size Limitation

```csharp
// WARNING: RSA-4096 has 446-byte plaintext limit with OAEP-SHA256
if (data.Length > 446)
{
    // Use hybrid encryption: RSA encrypts symmetric key
    var symmetricKey = RandomNumberGenerator.GetBytes(32);
    var encryptedKey = await _cryptoModule.EncryptAsync(
        symmetricKey,
        (byte)WalletNetworks.RSA4096,
        recipientPublicKey);
    
    var encryptedData = await _symmetricCrypto.EncryptAsync(
        data,
        EncryptionType.AES_GCM,
        symmetricKey);
}
```

---

## Key Size Reference

```csharp
// Use extension methods for correct sizes
int keySize = WalletNetworks.ED25519.GetPrivateKeySize();  // 64
int pubSize = WalletNetworks.ED25519.GetPublicKeySize();   // 32

// Symmetric encryption
int aesKey = EncryptionType.AES_256.GetSymmetricKeySize(); // 32
int aesIV = EncryptionType.AES_256.GetIVSize();            // 16
int chachaIV = EncryptionType.XCHACHA20_POLY1305.GetIVSize(); // 24
```

---

## Anti-Patterns

### WARNING: Signing Raw Data

**The Problem:**

```csharp
// BAD - Signing raw data directly
var signature = await _cryptoModule.SignAsync(
    rawTransactionData,  // Wrong: should be hash
    (byte)WalletNetworks.ED25519,
    privateKey);
```

**Why This Breaks:**
1. Non-deterministic signatures for same data
2. Performance degrades with large data
3. Some algorithms require fixed-size input

**The Fix:**

```csharp
// GOOD - Always hash first
var hash = _hashProvider.ComputeHash(rawTransactionData, HashType.SHA256);
var signature = await _cryptoModule.SignAsync(hash, network, privateKey);
```

### WARNING: Ignoring CryptoResult Status

**The Problem:**

```csharp
// BAD - Assuming success
var result = await _cryptoModule.SignAsync(hash, network, privateKey);
return result.Value!;  // NullReferenceException if failed
```

**Why This Breaks:**
1. Crypto operations can fail (wrong key size, invalid format)
2. Silent failures lead to security vulnerabilities
3. Null signatures passed downstream

**The Fix:**

```csharp
// GOOD - Check status explicitly
var result = await _cryptoModule.SignAsync(hash, network, privateKey);
if (!result.IsSuccess)
    throw new CryptographicException($"Signing failed: {result.Status}");
return result.Value!;
```

### WARNING: Not Zeroizing Keys

**The Problem:**

```csharp
// BAD - Keys remain in memory
var keySet = (await _cryptoModule.GenerateKeySetAsync(network)).Value;
// ... use keys ...
// Keys stay in memory until GC
```

**Why This Breaks:**
1. Memory dumps expose private keys
2. Keys persist in swap/hibernation files
3. Forensic recovery possible

**The Fix:**

```csharp
// GOOD - Explicitly clear sensitive data
var keySet = (await _cryptoModule.GenerateKeySetAsync(network)).Value;
try
{
    // ... use keys ...
}
finally
{
    keySet.Zeroize();  // Secure memory clearing
}
```

### WARNING: Hardcoded Algorithm Selection

**The Problem:**

```csharp
// BAD - Hardcoded network byte
var result = await _cryptoModule.SignAsync(hash, 0x01, privateKey);
```

**Why This Breaks:**
1. Magic numbers are unmaintainable
2. Algorithm mismatches cause verification failures
3. No compiler help if enum values change

**The Fix:**

```csharp
// GOOD - Use enum
var result = await _cryptoModule.SignAsync(
    hash,
    (byte)WalletNetworks.ED25519,
    privateKey);