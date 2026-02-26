# Feature Specification: Post-Quantum Cryptography Library

**Feature Branch**: `cryptography`
**Created**: 2025-12-03
**Status**: Draft - Clarified
**Input**: Derived from `.specify/specs/sorcha-cryptography-rewrite.md` and PQC migration plan

## Clarifications

### Session 2025-12-04

- Q: Should the spec use PQC-only or support classical algorithms? → A: PQC-Only - Remove ED25519/P-256/RSA; use only ML-DSA (signing) and ML-KEM (encryption)
- Q: How should key backup/recovery work without BIP39 HD derivation? → A: Encrypted Seed Export (primary) with Seed Phrase Encoding as optional human-readable alternative
- Q: What are acceptable performance targets for PQC operations? → A: Balanced - Signing < 100ms, verification < 50ms
- Q: Should the spec require specific platform versions for PQC support? → A: Strict Requirement - Require Windows 11 Nov 2025+ or Linux OpenSSL 3.5+; fail with clear error if unavailable
- Q: Should the spec mandate minimum security levels? → A: Level 3 Default (CNSA 2.0), Configurable - Default to Level 3 (ML-DSA-65/ML-KEM-768); blueprints can require Level 5 for high-security workflows. Level 1 (ML-DSA-44/ML-KEM-512) deferred as not CNSA 2.0 compliant.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Post-Quantum Key Generation (Priority: P0)

As a developer, I need to generate post-quantum cryptographic key pairs so that I can create quantum-resistant wallets and sign transactions.

**Why this priority**: Core functionality - all crypto operations depend on key generation.

**Independent Test**: Can be tested by generating keys and verifying they can sign/verify.

**Acceptance Scenarios**:

1. **Given** ML-DSA-44 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** a valid 2,560-byte private key and 1,312-byte public key are returned.
2. **Given** ML-DSA-65 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** a valid 4,032-byte private key and 1,952-byte public key are returned.
3. **Given** ML-DSA-87 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** a valid 4,896-byte private key and 2,592-byte public key are returned.
4. **Given** ML-KEM-512 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** a valid 1,632-byte private key and 800-byte public key are returned.
5. **Given** PQC is not supported on the platform, **When** I call any crypto operation, **Then** a clear error with platform requirements is returned.

---

### User Story 2 - Seed-Based Key Recovery (Priority: P0)

As a wallet Participant, I need to backup and recover my wallet using an encrypted seed export or optional seed phrase encoding so that I can restore my wallet securely.

**Why this priority**: Essential for wallet backup and recovery.

**Independent Test**: Can be tested by exporting seed, recovering keys, and verifying they match.

**Acceptance Scenarios**:

1. **Given** a new wallet request, **When** I call `CreateMasterSeedAsync`, **Then** a 32-byte random master seed is generated.
2. **Given** a master seed and password, **When** I call `ExportEncryptedSeedAsync`, **Then** an Argon2id + AES-256-GCM encrypted Base64 string is returned.
3. **Given** an encrypted seed export and correct password, **When** I call `ImportEncryptedSeedAsync`, **Then** the original master seed is recovered.
4. **Given** a master seed, **When** I call `SeedToPhrase`, **Then** a 24-word BIP39-encoded phrase is returned (encoding only, not HD derivation).
5. **Given** a 24-word phrase, **When** I call `PhraseToSeed`, **Then** the original master seed is recovered.
6. **Given** a master seed and key index, **When** I call `DeriveKeyAsync`, **Then** deterministic keys are derived using HKDF.

---

### User Story 3 - Post-Quantum Digital Signatures (Priority: P0)

As a transaction sender, I need to sign data with my ML-DSA private key so that I can prove authenticity with quantum resistance.

**Why this priority**: Essential for transaction signing and verification.

**Independent Test**: Can be tested by signing data and verifying the signature.

**Acceptance Scenarios**:

1. **Given** data and an ML-DSA-44 private key, **When** I call `SignAsync`, **Then** a valid 2,420-byte signature is returned.
2. **Given** data and an ML-DSA-65 private key, **When** I call `SignAsync`, **Then** a valid 3,309-byte signature is returned.
3. **Given** data and an ML-DSA-87 private key, **When** I call `SignAsync`, **Then** a valid 4,627-byte signature is returned.
4. **Given** a valid signature, **When** I call `VerifyAsync` with correct public key, **Then** verification succeeds.
5. **Given** tampered data, **When** I verify the original signature, **Then** verification fails.
6. **Given** wrong public key, **When** I verify, **Then** verification fails.

---

### User Story 4 - Hybrid Encryption with ML-KEM (Priority: P0)

As a developer, I need to encrypt data using ML-KEM key encapsulation combined with AES-256-GCM so that payload encryption is quantum-resistant.

**Why this priority**: Required for quantum-resistant payload encryption in transactions.

**Independent Test**: Can be tested by encrypting data and decrypting to verify round-trip.

**Acceptance Scenarios**:

1. **Given** plaintext data and recipient's ML-KEM public key, **When** I call `HybridEncryptAsync`, **Then** ML-KEM encapsulation + AES-256-GCM ciphertext is returned.
2. **Given** hybrid ciphertext and ML-KEM private key, **When** I call `HybridDecryptAsync`, **Then** original plaintext is returned.
3. **Given** tampered ciphertext, **When** I decrypt, **Then** authentication fails.
4. **Given** ML-KEM-768 selected (default, CNSA 2.0), **When** I encapsulate, **Then** 1,088-byte ciphertext is produced.
5. **Given** ML-KEM-512 selected, **When** I encapsulate, **Then** 768-byte ciphertext is produced. *(Deferred: not CNSA 2.0 compliant)*
6. **Given** ML-KEM-1024 selected, **When** I encapsulate, **Then** 1,568-byte ciphertext is produced.

---

### User Story 5 - Wallet Address Encoding (Priority: P1)

As a wallet service, I need to convert PQC public keys to/from wallet addresses in multiple formats so that Participants have flexible address options.

**Why this priority**: Required for Participant-facing wallet functionality.

**Independent Test**: Can be tested by converting public key to address and back.

**Acceptance Scenarios**:

1. **Given** an ML-DSA public key, **When** I call `PublicKeyToWallet` with full format, **Then** a Bech32m address with "ws1f" prefix containing complete public key is returned.
2. **Given** an ML-DSA public key, **When** I call `PublicKeyToWallet` with hash format, **Then** a Bech32m address with "ws1h" prefix containing SHA-256 hash is returned.
3. **Given** an ML-DSA public key, **When** I call `PublicKeyToWallet` with truncated format, **Then** a Bech32m address with "ws1t" prefix containing truncated key + checksum is returned.
4. **Given** any wallet address, **When** I call `DetectAddressFormat`, **Then** the format (Full/Hash/Truncated) is automatically detected.
5. **Given** a full-format wallet address, **When** I call `WalletToPublicKey`, **Then** original public key and network are returned.
6. **Given** an invalid address, **When** I call `WalletToPublicKey`, **Then** null is returned.

---

### User Story 6 - Hash Functions (Priority: P1)

As a developer, I need to compute cryptographic hashes so that I can create transaction IDs and verify data integrity.

**Why this priority**: Used throughout the platform for integrity and identification.

**Independent Test**: Can be tested by hashing known data and comparing to expected values.

**Acceptance Scenarios**:

1. **Given** data and SHA-256 selected, **When** I call `ComputeHash`, **Then** correct 32-byte hash is returned.
2. **Given** Blake2b-512 selected, **When** I compute hash, **Then** correct 64-byte hash is returned.
3. **Given** a stream of data, **When** I call async hash, **Then** streaming hash is computed.
4. **Given** data and key, **When** I call `ComputeHMAC`, **Then** correct HMAC is returned.

---

### Edge Cases

- What happens when generating keys on unsupported platform? → Fail with `PlatformNotSupportedException` and clear message about requirements.
- What happens when importing encrypted seed with wrong password? → Return `CryptoStatus.DecryptionFailed` with no timing leak.
- How does the library handle large signature serialization? → Use length-prefixed binary format.
- What happens when blueprint requires Level 5 but only Level 1 keys exist? → Return `CryptoStatus.InsufficientSecurityLevel` error.

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Library MUST generate key pairs for ML-DSA-44, ML-DSA-65, and ML-DSA-87 (NIST FIPS 204)
- **FR-002**: Library MUST generate key pairs for ML-KEM-512, ML-KEM-768, and ML-KEM-1024 (NIST FIPS 203)
- **FR-003**: Library MUST default to ML-DSA-65 (signing) and ML-KEM-768 (encryption) for NIST Level 3 (CNSA 2.0 compliant). ML-DSA-44 and ML-KEM-512 (Level 1) are deferred as not CNSA 2.0 compliant.
- **FR-004**: Library MUST allow blueprint-configurable security levels (Level 3/5)
- **FR-005**: Library MUST generate 32-byte random master seeds for key derivation
- **FR-006**: Library MUST derive keys using HKDF from master seed with index
- **FR-007**: Library MUST export seeds encrypted with Argon2id + AES-256-GCM
- **FR-008**: Library MUST optionally encode seeds as 24-word BIP39 phrases (encoding only)
- **FR-009**: Library MUST sign data and verify signatures for all ML-DSA variants
- **FR-010**: Library MUST implement hybrid encryption using ML-KEM + AES-256-GCM
- **FR-011**: Library MUST support AES-256-GCM as default symmetric cipher
- **FR-012**: Library MUST compute SHA-256, SHA-384, SHA-512, Blake2b-256, Blake2b-512 hashes
- **FR-013**: Library MUST encode/decode Bech32m for PQC-length public keys
- **FR-014**: Library MUST support three address formats: Full, Hash, Truncated with auto-detection
- **FR-015**: Library MUST check `MLDsa.IsSupported` and `MLKem.IsSupported` at initialization
- **FR-016**: Library MUST fail with clear error on unsupported platforms (Windows 11 Nov 2025+ or Linux OpenSSL 3.5+ required)
- **FR-017**: Library MUST use cryptographically secure random number generation
- **FR-018**: Library MUST NOT support ED25519, NIST P-256, RSA-4096, or BIP32/BIP44 HD derivation

### Key Entities

- **KeySet**: Public/private key pair with PQC algorithm identifier (ML-DSA or ML-KEM variant)
- **MasterSeed**: 32-byte random seed for HKDF-based key derivation
- **KeyRing**: Signing key + encryption key pair derived from master seed at specific index
- **KeyChain**: Collection of named KeyRings with encryption
- **CryptoResult<T>**: Result type with status and optional value
- **HybridCiphertext**: ML-KEM ciphertext + AES-256-GCM encrypted payload

### Platform Requirements

- **Windows**: Windows 11 with November 2025 security update or later
- **Linux**: OpenSSL 3.5 or later
- **.NET**: .NET 10 or later
- **Runtime Check**: `MLDsa.IsSupported` and `MLKem.IsSupported` must return true

### Client Deployment Constraints

#### Blazor WebAssembly - Not Currently Supported

The cryptography library **cannot run in Blazor WebAssembly** due to PQC algorithm dependencies on native OS cryptographic libraries:

| Constraint | Reason |
|------------|--------|
| **ML-DSA/ML-KEM require native OS support** | .NET's `System.Security.Cryptography.MLDsa` and `MLKem` are thin wrappers around Windows CNG or Linux OpenSSL - not available in browser sandbox |
| **`MLDsa.IsSupported` returns false in WASM** | No native crypto provider available |
| **WebCrypto API lacks PQC** | W3C WebCrypto does not yet include ML-DSA or ML-KEM |

#### Supported Client Deployment Models

| Model | Status | Notes |
|-------|--------|-------|
| **Blazor Server** | ✅ Supported | Crypto executes server-side |
| **Blazor Hybrid (MAUI)** | ✅ Supported | Native app has OS crypto access |
| **Blazor WASM + Server Signing** | ✅ Supported | Client constructs unsigned transactions, Wallet Service API signs |
| **Blazor WASM Native** | ❌ Blocked | Awaiting browser PQC support |

#### Future Browser PQC Support

When browser PQC support becomes available, client-side implementation should be straightforward because:

1. **Interface-based design**: All crypto operations go through `ICryptoModule` interface - swap implementation without changing consumers
2. **Platform availability pattern**: Existing `IsPqcSupported` property already handles capability detection
3. **Algorithm abstraction**: `WalletNetworks` enum abstracts algorithm selection from implementation details
4. **Potential future paths**:
   - W3C WebCrypto adding ML-DSA/ML-KEM (NIST standardization may drive this)
   - WebAssembly System Interface (WASI) crypto proposals
   - Pure .NET PQC implementation (e.g., BouncyCastle adding FIPS 203/204)

**Design Principle**: Keep `ICryptoModule` implementations swappable so a future `WasmCryptoModule` can be added when browser support materializes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: ML-DSA key generation under 200ms for all variants
- **SC-002**: ML-KEM key generation under 100ms for all variants
- **SC-003**: ML-DSA signing under 100ms for all variants
- **SC-004**: ML-DSA verification under 50ms for all variants
- **SC-005**: ML-KEM encapsulation under 50ms for all variants
- **SC-006**: ML-KEM decapsulation under 50ms for all variants
- **SC-007**: Hybrid encryption throughput over 50 MB/s
- **SC-008**: Hashing throughput over 100 MB/s for SHA-256
- **SC-009**: Test coverage exceeds 90% for all code
- **SC-010**: 100% coverage for core cryptographic operations
- **SC-011**: All algorithms validated against NIST FIPS 203/204 test vectors
- **SC-012**: Zero critical vulnerabilities in security audit
- **SC-013**: Platform availability check completes under 10ms

## Dependencies

### Downstream Consumers

The following components depend on this library and will require updates when PQC migration is complete:

| Component | Dependency | Impact |
|-----------|------------|--------|
| **Transaction Handler** | ML-DSA signing, ML-KEM encryption | Version reset to V1 (PQC-only) |
| **Wallet Service** | Key generation, signing, address encoding | Update KeyManagementService |
| **Wallet Core** | HD derivation replacement | Remove NBitcoin, use HKDF |
| **Register Service** | Signature verification | Update verification logic |
| **Peer Service** | Transaction signing for gossip | Use ML-DSA signatures |

### Implementation Order

1. **Cryptography Library** (this spec) - Foundation
2. **Transaction Handler** - Depends on signing/encryption
3. **Wallet Service** - Depends on key management
4. **Register Service** - Depends on verification
5. **Peer Service** - Depends on transaction format
