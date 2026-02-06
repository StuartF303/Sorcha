# Research: Payload Encryption for DAD Security Model

**Feature**: 019-payload-encryption
**Date**: 2026-02-06

## Research Questions & Findings

### RQ-1: Which symmetric encryption algorithms are available?

**Decision**: Use XChaCha20-Poly1305 as default, with AES-GCM as alternative.

**Rationale**: `ISymmetricCrypto` / `SymmetricCrypto` in `Sorcha.Cryptography` already implements both algorithms with full encrypt/decrypt support. XChaCha20-Poly1305 is the existing default in `PayloadOptions.EncryptionType`. Both are authenticated encryption (AEAD) algorithms providing confidentiality + integrity.

**Key Details**:
- `ISymmetricCrypto.EncryptAsync(plaintext, encryptionType, key?, ct)` → `CryptoResult<SymmetricCiphertext>`
- `ISymmetricCrypto.DecryptAsync(ciphertext, ct)` → `CryptoResult<byte[]>`
- `ISymmetricCrypto.GenerateKey(encryptionType)` → random key bytes
- `ISymmetricCrypto.GenerateIV(encryptionType)` → random IV/nonce bytes
- XChaCha20-Poly1305: 32-byte key, 24-byte nonce
- AES-GCM: 32-byte key, 12-byte nonce, 128-bit auth tag

**Alternatives Considered**:
- AES-256-CBC: Available but NOT authenticated encryption — rejected for security reasons
- ChaCha20-Poly1305: Available but 8-byte nonce is too short for random generation — XChaCha20 is safer

### RQ-2: How to encrypt the symmetric key per-recipient (key wrapping)?

**Decision**: Use `ICryptoModule.EncryptAsync()` for asymmetric key wrapping.

**Rationale**: The symmetric key is 32 bytes. `ICryptoModule` supports:
- **ED25519** (network byte `0x00`): Converts to Curve25519, uses libsodium SealedPublicKeyBox. No size limit concern.
- **RSA-4096** (network byte `0x02`): OAEP-SHA256 padding. Max plaintext = 446 bytes. 32-byte key easily fits.
- **NIST P-256** (network byte `0x01`): **NOT IMPLEMENTED** — `CryptoModule.Encrypt()` returns `CryptoStatus.EncryptionFailed` for this type.

**Key Details**:
- `ICryptoModule.EncryptAsync(data, network, publicKey, ct)` → `CryptoResult<byte[]>`
- `ICryptoModule.DecryptAsync(ciphertext, network, privateKey, ct)` → `CryptoResult<byte[]>`
- The `network` byte identifies the algorithm: ED25519=0x00, RSA4096=0x02

**Design Decision**: The current `IPayloadManager` interface passes `string recipientWallets` and `string wifPrivateKey` — wallet addresses and WIF-encoded private keys. The implementation must resolve wallet addresses to public keys and determine the network type. Since `PayloadManager` is a low-level component used inside `TransactionBuilder`, it needs crypto interfaces injected.

**Alternatives Considered**:
- Passing raw public key bytes directly: Would require API changes to callers
- Using a separate key wrapping service: Over-engineering for a single use case

### RQ-3: How to compute content hashes?

**Decision**: Use `IHashProvider.ComputeHash(data, HashType.SHA256)`.

**Rationale**: `IHashProvider` is already implemented in `Sorcha.Cryptography` with SHA-256 support. The `PayloadOptions.HashType` defaults to `SHA256`. The `IHashProvider.VerifyHash(data, hash, hashType)` method can be used for integrity verification.

**Key Details**:
- `IHashProvider.ComputeHash(byte[] data, HashType hashType)` → `byte[]` (32 bytes for SHA-256)
- `IHashProvider.VerifyHash(byte[] data, byte[] hash, HashType hashType)` → `bool`
- SHA-256 produces 32-byte digests — matches the existing `Hash = new byte[32]` placeholder

### RQ-4: How does PayloadManager get constructed today?

**Decision**: Add constructor injection for `ISymmetricCrypto`, `ICryptoModule`, and `IHashProvider`.

**Rationale**: `PayloadManager` is currently instantiated via `new PayloadManager()` in 10 call sites:
- `TransactionBuilder.Create()` — already has `ICryptoModule` and `IHashProvider`
- `TransactionFactory` (4 places) — version adapters
- `JsonTransactionSerializer` / `BinaryTransactionSerializer` — deserialization
- `TransactionBuilderService` (3 places) — Blueprint Service

Since `TransactionBuilder` already holds `ICryptoModule` and `IHashProvider`, it can pass them to `PayloadManager`. Other call sites need to be updated to pass the crypto dependencies.

**Breaking Change Assessment**: The parameterless `PayloadManager()` constructor will be replaced. All 10 call sites must be updated. This is an internal API — no external consumers.

### RQ-5: How to handle recipient wallet address → public key resolution?

**Decision**: Accept public key bytes + network byte directly in payload operations; let callers resolve wallet addresses to keys.

**Rationale**: The current `IPayloadManager` interface uses `string[] recipientWallets` and `string wifPrivateKey`. For real encryption, we need:
- Public keys (bytes) + network type for each recipient (to encrypt the symmetric key)
- Private key (bytes) + network type for the decryptor (to decrypt the symmetric key)

**Design Decision**: Modify `AddPayloadAsync` to accept recipient public keys alongside wallet addresses. The interface will use a dictionary mapping wallet address → (publicKey, network) so the caller provides resolution. Similarly, `GetPayloadDataAsync` needs the private key bytes + network, not just WIF string.

**Alternative Considered**: Having PayloadManager resolve wallet addresses via a service client — rejected because PayloadManager is a Common library that shouldn't depend on service infrastructure.

### RQ-6: Legacy payload backward compatibility detection

**Decision**: Detect legacy payloads by checking if IV is all zeros.

**Rationale**: Current stub sets `IV = new byte[12]` (all zeros). Real encryption always generates cryptographically random IVs. The probability of a random 12+ byte IV being all zeros is astronomically small (2^-96 for AES-GCM, 2^-192 for XChaCha20). A simple `IV.All(b => b == 0)` check reliably distinguishes legacy from encrypted payloads.

### RQ-7: Thread safety for concurrent payload additions

**Decision**: Use `lock` or `ConcurrentDictionary` for `_payloads` list and `_nextPayloadId` counter.

**Rationale**: The current `List<Payload>` and `uint _nextPayloadId++` are not thread-safe. Since `PayloadManager` instances are typically scoped per-transaction (created in `TransactionBuilder.Create()`), contention is low. A simple `lock` object is sufficient — no need for `ConcurrentBag` or lock-free structures.

## Technology Decisions Summary

| Decision | Choice | Key Reason |
|----------|--------|------------|
| Symmetric encryption | XChaCha20-Poly1305 (default) / AES-GCM | Already implemented, AEAD, configurable via PayloadOptions |
| Key wrapping | ICryptoModule.EncryptAsync (ED25519/RSA-4096) | Existing asymmetric encrypt, 32-byte key fits all algorithms |
| Content hashing | IHashProvider.ComputeHash (SHA-256) | Already implemented, matches PayloadOptions default |
| DI approach | Constructor injection of ISymmetricCrypto, ICryptoModule, IHashProvider | TransactionBuilder already has 2 of 3; natural extension |
| Legacy detection | Check for all-zero IV | Simple, reliable, zero false positive probability |
| Thread safety | lock object | Low contention (per-transaction scope), simple solution |
| Key resolution | Caller provides public key bytes + network | Keeps PayloadManager in Common layer, no service dependencies |
