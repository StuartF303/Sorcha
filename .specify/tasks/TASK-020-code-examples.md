# Task: Create Code Examples

**ID:** TASK-020
**Status:** Not Started
**Priority:** Medium
**Estimate:** 6 hours
**Assignee:** Unassigned
**Created:** 2025-11-12

## Objective

Create comprehensive code examples demonstrating common usage scenarios for the Sorcha.Cryptography library.

## Example Categories

### 1. Getting Started (examples/01-GettingStarted/)
```csharp
// Example: Generate key pair and create wallet
var cryptoModule = new CryptoModule();
var keyManager = new KeyManager(cryptoModule);

// Create new key ring with mnemonic
var result = await keyManager.CreateMasterKeyRingAsync(
    WalletNetworks.ED25519,
    password: "secure-password");

if (result.IsSuccess)
{
    var keyRing = result.Value;
    Console.WriteLine($"Wallet: {keyRing.WalletAddress}");
    Console.WriteLine($"Mnemonic: {keyRing.Mnemonic}");
    Console.WriteLine($"WIF: {keyRing.WIFKey}");
}
```

### 2. Signing and Verification (examples/02-Signing/)
```csharp
// Example: Sign data and verify signature
var data = Encoding.UTF8.GetBytes("Important message");
var hash = hashProvider.ComputeHash(data, HashType.SHA256);

// Sign
var signResult = await cryptoModule.SignAsync(
    hash,
    (byte)WalletNetworks.ED25519,
    privateKey);

// Verify
var verifyStatus = await cryptoModule.VerifyAsync(
    signResult.Value,
    hash,
    (byte)WalletNetworks.ED25519,
    publicKey);
```

### 3. Encryption and Decryption (examples/03-Encryption/)
```csharp
// Example: Encrypt payload for multiple recipients
var symmetricCrypto = new SymmetricCrypto();
var plaintext = Encoding.UTF8.GetBytes("Secret data");

// Symmetric encryption
var encResult = await symmetricCrypto.EncryptAsync(
    plaintext,
    EncryptionType.XCHACHA20_POLY1305);

// Asymmetric encryption of symmetric key (for recipient)
var keyResult = await cryptoModule.EncryptAsync(
    encResult.Value.Key,
    (byte)WalletNetworks.ED25519,
    recipientPublicKey);
```

### 4. Key Recovery (examples/04-Recovery/)
```csharp
// Example: Recover wallet from mnemonic
var mnemonic = "word1 word2 word3 ... word12";
var password = "secure-password";

var result = await keyManager.RecoverMasterKeyRingAsync(
    mnemonic,
    password);

if (result.IsSuccess)
{
    Console.WriteLine("Wallet recovered successfully!");
}
```

### 5. KeyChain Management (examples/05-KeyChain/)
```csharp
// Example: Manage multiple wallets
var keyChain = new KeyChain();

// Add multiple key rings
keyChain.AddKeyRing("personal", personalKeyRing);
keyChain.AddKeyRing("business", businessKeyRing);

// Export encrypted
var exportResult = await keyChain.ExportAsync("master-password");

// Import later
await keyChain.ImportAsync(exportResult.Value, "master-password");
```

### 6. Complete Transaction Flow (examples/06-TransactionFlow/)
```csharp
// Example: Complete transaction signing workflow
// 1. Create data
var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));

// 2. Compress
var (compressed, wasCompressed) = compressionUtils.Compress(payload);

// 3. Hash
var hash = hashProvider.ComputeDoubleHash(compressed);

// 4. Sign
var signature = await cryptoModule.SignAsync(hash, network, privateKey);

// 5. Verify
var isValid = await cryptoModule.VerifyAsync(signature.Value, hash, network, publicKey);
```

## Example Projects

Create runnable console applications:

1. **SimpleWallet** - Basic wallet creation and recovery
2. **SecureMessaging** - Encrypt/decrypt messages
3. **DocumentSigning** - Sign and verify documents
4. **MultiSigSetup** - Multi-signature scenario

## Documentation Updates

Update README.md with:
- Quick start code
- Link to examples folder
- Common use cases
- Best practices

## Acceptance Criteria

- [ ] All example categories created
- [ ] Examples compile and run successfully
- [ ] Examples demonstrate best practices
- [ ] Examples include error handling
- [ ] Examples include XML comments
- [ ] README references examples

---

**Task Control**
- **Created By:** Claude Code
- **Dependencies:** TASK-001 through TASK-017
