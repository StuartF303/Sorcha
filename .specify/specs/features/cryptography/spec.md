# Feature Specification: Cryptography Library

**Feature Branch**: `cryptography`
**Created**: 2025-12-03
**Status**: Draft
**Input**: Derived from `.specify/specs/sorcha-cryptography-rewrite.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Key Generation (Priority: P0)

As a developer, I need to generate cryptographic key pairs for supported algorithms so that I can create wallets and sign transactions.

**Why this priority**: Core functionality - all crypto operations depend on key generation.

**Independent Test**: Can be tested by generating keys and verifying they can sign/verify.

**Acceptance Scenarios**:

1. **Given** ED25519 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** a valid 32-byte private key and 32-byte public key are returned.
2. **Given** NIST P-256 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** valid ECDSA key pair is returned.
3. **Given** RSA-4096 algorithm selected, **When** I call `GenerateKeySetAsync`, **Then** 4096-bit RSA key pair is returned.
4. **Given** a seed value, **When** generating keys, **Then** deterministic keys are produced.

---

### User Story 2 - Mnemonic Recovery Phrases (Priority: P0)

As a wallet user, I need to generate and recover wallets from BIP39 mnemonic phrases so that I can backup and restore my wallet.

**Why this priority**: Essential for wallet backup and recovery.

**Independent Test**: Can be tested by generating mnemonic, recovering keys, and verifying they match.

**Acceptance Scenarios**:

1. **Given** a new key ring request, **When** I call `CreateMasterKeyRingAsync`, **Then** a 12-word mnemonic is generated.
2. **Given** a valid mnemonic, **When** I call `RecoverMasterKeyRingAsync`, **Then** the same keys are recovered.
3. **Given** an invalid mnemonic (wrong checksum), **When** I attempt recovery, **Then** an error status is returned.
4. **Given** a password is provided, **When** generating keys, **Then** the mnemonic is password-protected.

---

### User Story 3 - Digital Signatures (Priority: P0)

As a transaction sender, I need to sign data with my private key so that I can prove authenticity and integrity.

**Why this priority**: Essential for transaction signing and verification.

**Independent Test**: Can be tested by signing data and verifying the signature.

**Acceptance Scenarios**:

1. **Given** data and a private key, **When** I call `SignAsync`, **Then** a valid signature is returned.
2. **Given** a valid signature, **When** I call `VerifyAsync` with correct public key, **Then** verification succeeds.
3. **Given** a tampered data, **When** I verify the original signature, **Then** verification fails.
4. **Given** wrong public key, **When** I verify, **Then** verification fails.

---

### User Story 4 - Symmetric Encryption (Priority: P1)

As a developer, I need to encrypt and decrypt data with symmetric keys so that I can protect sensitive information.

**Why this priority**: Required for payload encryption in transactions.

**Independent Test**: Can be tested by encrypting data and decrypting to verify round-trip.

**Acceptance Scenarios**:

1. **Given** plaintext data, **When** I call `EncryptAsync` with AES-256-GCM, **Then** ciphertext with authentication tag is returned.
2. **Given** ciphertext, **When** I call `DecryptAsync` with correct key, **Then** original plaintext is returned.
3. **Given** tampered ciphertext, **When** I decrypt authenticated encryption, **Then** authentication fails.
4. **Given** XChaCha20-Poly1305 selected, **When** I encrypt/decrypt, **Then** operation succeeds.

---

### User Story 5 - Wallet Address Encoding (Priority: P1)

As a wallet service, I need to convert public keys to/from wallet addresses so that users have readable addresses.

**Why this priority**: Required for user-facing wallet functionality.

**Independent Test**: Can be tested by converting public key to address and back.

**Acceptance Scenarios**:

1. **Given** a public key and network, **When** I call `PublicKeyToWallet`, **Then** a Bech32 address with "ws1" prefix is returned.
2. **Given** a wallet address, **When** I call `WalletToPublicKey`, **Then** original public key and network are returned.
3. **Given** an invalid address, **When** I call `WalletToPublicKey`, **Then** null is returned.
4. **Given** multiple addresses, **When** I call `ValidateWallets`, **Then** batch validation is performed.

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

- What happens when generating keys with zero-length seed?
- How does the library handle malformed mnemonics with correct checksum but invalid words?
- What happens when encrypting with maximum-size data for RSA?
- How does the library handle timing attacks during signature verification?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Library MUST generate key pairs for ED25519, NIST P-256, and RSA-4096
- **FR-002**: Library MUST generate BIP39-compatible 12-word mnemonics
- **FR-003**: Library MUST recover keys from valid mnemonics
- **FR-004**: Library MUST sign data and verify signatures for all supported algorithms
- **FR-005**: Library MUST support AES-128-CBC, AES-256-CBC, AES-256-GCM, ChaCha20-Poly1305, XChaCha20-Poly1305
- **FR-006**: Library MUST compute SHA-256, SHA-384, SHA-512, Blake2b-256, Blake2b-512 hashes
- **FR-007**: Library MUST encode/decode Base58, Bech32, and hexadecimal
- **FR-008**: Library MUST convert public keys to/from wallet addresses
- **FR-009**: Library MUST support WIF (Wallet Import Format) for private keys
- **FR-010**: Library MUST support KeyRing and KeyChain management
- **FR-011**: Library MUST provide compression utilities
- **FR-012**: Library MUST use cryptographically secure random number generation

### Key Entities

- **KeySet**: Public/private key pair with algorithm identifier
- **KeyRing**: Master key with derivation capabilities
- **KeyChain**: Collection of named KeyRings with encryption
- **CryptoResult<T>**: Result type with status and optional value
- **SymmetricCiphertext**: Encrypted data with key, IV, and type

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Key generation under 100ms for ED25519/NIST P-256, under 500ms for RSA-4096
- **SC-002**: Signing under 10ms for ED25519/NIST P-256, under 50ms for RSA-4096
- **SC-003**: Hashing throughput over 100 MB/s for SHA-256
- **SC-004**: Symmetric encryption over 200 MB/s for ChaCha20-Poly1305
- **SC-005**: Test coverage exceeds 90% for all code
- **SC-006**: 100% coverage for core cryptographic operations
- **SC-007**: All algorithms validated against NIST/RFC test vectors
- **SC-008**: Zero critical vulnerabilities in security audit
