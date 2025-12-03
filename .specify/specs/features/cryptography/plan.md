# Implementation Plan: Cryptography Library

**Feature Branch**: `cryptography`
**Created**: 2025-12-03
**Status**: 90% Complete

## Summary

The Sorcha.Cryptography library is a standalone, reusable cryptography library providing key management, digital signatures, symmetric/asymmetric encryption, hashing, and encoding utilities. It is designed with minimal dependencies for broad compatibility.

## Design Decisions

### Decision 1: Libsodium via Sodium.Core

**Approach**: Use Sodium.Core NuGet package for core cryptographic operations.

**Rationale**:
- Well-audited, battle-tested library
- Constant-time operations for timing attack resistance
- Excellent ED25519 and ChaCha20-Poly1305 support
- Cross-platform compatibility

**Alternatives Considered**:
- BouncyCastle - Larger footprint, more complex
- System.Security.Cryptography only - Missing some algorithms

### Decision 2: Result Type Pattern

**Approach**: Use CryptoResult<T> with CryptoStatus enum instead of exceptions.

**Rationale**:
- Expected failures (invalid keys, wrong passwords) are not exceptional
- Clear status codes for debugging
- No exception overhead for common cases

### Decision 3: Async API Design

**Approach**: Async methods with CancellationToken for long-running operations.

**Rationale**:
- RSA operations can be slow
- Streaming hash computation benefits from async
- Consistent with modern .NET patterns

### Decision 4: Zero Project Dependencies

**Approach**: Library has no references to other Sorcha projects.

**Rationale**:
- Maximum reusability
- No circular dependencies
- Can be published as standalone NuGet package

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Sorcha.Cryptography                     │
│                    (.NET 10 Library)                     │
├─────────────────────────────────────────────────────────┤
│  Interfaces/                                             │
│  ├── ICryptoModule.cs       (Main crypto operations)    │
│  ├── IKeyManager.cs         (Key/mnemonic management)   │
│  ├── IHashProvider.cs       (Hashing operations)        │
│  ├── ISymmetricCrypto.cs    (Symmetric encryption)      │
│  └── IEncodingProvider.cs   (Encoding utilities)        │
├─────────────────────────────────────────────────────────┤
│  Core/                                                   │
│  ├── CryptoModule.cs        (Main implementation)       │
│  ├── KeyManager.cs          (Mnemonic/key derivation)   │
│  ├── HashProvider.cs        (SHA, Blake2b, HMAC)        │
│  ├── SymmetricCrypto.cs     (AES, ChaCha20)            │
│  └── CryptoModuleBase.cs    (Algorithm dispatch)        │
├─────────────────────────────────────────────────────────┤
│  Models/                                                 │
│  ├── CryptoKey.cs           (Key representation)        │
│  ├── KeySet.cs              (Key pair)                  │
│  ├── KeyRing.cs             (Master key + derivation)   │
│  ├── KeyChain.cs            (Collection of KeyRings)    │
│  └── CryptoResult.cs        (Result pattern)            │
├─────────────────────────────────────────────────────────┤
│  Utilities/                                              │
│  ├── EncodingUtilities.cs   (Base58, Bech32, Hex)       │
│  ├── WalletUtilities.cs     (Address conversion, WIF)   │
│  ├── CompressionUtilities.cs (Deflate compression)      │
│  └── RandomnessProvider.cs  (CSPRNG)                    │
├─────────────────────────────────────────────────────────┤
│  Enums/                                                  │
│  ├── WalletNetworks.cs      (Algorithm identifiers)     │
│  ├── HashType.cs            (Hash algorithm selection)  │
│  ├── EncryptionType.cs      (Symmetric algo selection)  │
│  ├── CompressionType.cs     (Compression levels)        │
│  └── CryptoStatus.cs        (Result status codes)       │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| ICryptoModule | 100% | Interface complete |
| CryptoModule | 100% | ED25519, NIST P-256, RSA-4096 |
| KeyManager | 100% | BIP39 mnemonics |
| HashProvider | 100% | SHA, Blake2b, HMAC |
| SymmetricCrypto | 100% | AES, ChaCha20 variants |
| EncodingUtilities | 100% | Base58, Bech32, Hex |
| WalletUtilities | 100% | Address conversion |
| CompressionUtilities | 100% | Deflate compression |
| KeyRing | 100% | Master key management |
| KeyChain | 90% | Encryption export pending |
| Unit Tests | 85% | 85+ tests passing |

### Algorithm Support Matrix

| Algorithm | Key Gen | Sign | Verify | Encrypt | Decrypt |
|-----------|---------|------|--------|---------|---------|
| ED25519 | Done | Done | Done | Done* | Done* |
| NIST P-256 | Done | Done | Done | Done | Done |
| RSA-4096 | Done | Done | Done | Done | Done |

*ED25519 encryption via Curve25519 conversion

| Symmetric | Encrypt | Decrypt |
|-----------|---------|---------|
| AES-128-CBC | Done | Done |
| AES-256-CBC | Done | Done |
| AES-256-GCM | Done | Done |
| ChaCha20-Poly1305 | Done | Done |
| XChaCha20-Poly1305 | Done | Done |

## Dependencies

### Production Dependencies

```xml
<PackageReference Include="Sodium.Core" Version="1.3.4" />
```

### Test Dependencies

```xml
<PackageReference Include="xUnit" Version="2.9.2" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
```

### Service Dependencies

- None (standalone library)

## Migration/Integration Notes

### Usage Example

```csharp
// Create crypto module
var crypto = new CryptoModule();

// Generate key pair
var keyResult = await crypto.GenerateKeySetAsync(WalletNetworks.ED25519);
if (keyResult.IsSuccess)
{
    var keySet = keyResult.Value;

    // Sign data
    var hash = crypto.HashProvider.ComputeHash(data, HashType.SHA256);
    var signResult = await crypto.SignAsync(hash, keySet.Network, keySet.PrivateKey);

    // Verify signature
    var status = await crypto.VerifyAsync(signResult.Value, hash, keySet.Network, keySet.PublicKey);
}
```

### Migration from SorchaPlatformCryptography

1. Replace namespace imports
2. Update method signatures (async pattern)
3. Handle CryptoResult instead of exceptions
4. Remove transaction/payload specific code (now in TransactionHandler)

## Open Questions

1. Should we add hardware token (PKCS#11) support?
2. Should we support post-quantum algorithms (Kyber, Dilithium)?
3. Should we add key stretching (Argon2id) as public API?
