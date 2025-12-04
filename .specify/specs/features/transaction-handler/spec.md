# Feature Specification: Transaction Handler Library (PQC)

**Feature Branch**: `transaction-handler`
**Created**: 2025-12-03
**Status**: Draft - PQC Migration
**Input**: Derived from `.specify/specs/sorcha-transaction-handler.md` and PQC migration plan

## Clarifications

### Session 2025-12-04

- Q: Should legacy transaction versions (V1-V4) be supported? → A: No - reset versioning to V1 (PQC-only). No existing data requires backward compatibility.
- Q: What signing algorithm should be used? → A: ML-DSA-44 (default), configurable to ML-DSA-65/87 via blueprint policy.
- Q: What encryption should payloads use? → A: ML-KEM-512 + AES-256-GCM hybrid encryption (default), configurable to ML-KEM-768/1024.
- Q: What are the maximum transaction limits? → A: Permissive (1,000 recipients, 1GB total, 100MB per payload) with Blueprint Designer warnings when sizes approach unmanageable thresholds.
- Q: What happens to symmetric keys after payload encryption? → A: Configurable - default to immediate zeroization, but allow retention via PayloadOptions.RetainSymmetricKey flag for re-encryption scenarios.
- Q: How should observability be exposed? → A: Both OpenTelemetry integration (metrics/traces) AND extensible .NET events for custom consumer telemetry.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create PQC Transactions (Priority: P0)

As a blockchain developer, I need to create post-quantum secure transactions with a fluent API so that I can build and sign transactions with quantum-resistant cryptography.

**Why this priority**: Core functionality - all blockchain operations require transactions.

**Independent Test**: Can be tested by creating a transaction, signing with ML-DSA, and verifying the signature.

**Acceptance Scenarios**:

1. **Given** a new TransactionBuilder, **When** I create a V1 transaction with recipients and metadata, **Then** a valid transaction object is returned.
2. **Given** a transaction with payloads, **When** I sign with an ML-DSA private key, **Then** a valid 2,420-4,627 byte signature is computed using double SHA-256 + ML-DSA.
3. **Given** a signed transaction, **When** I call Build(), **Then** a complete transaction with TxId is returned.
4. **Given** invalid inputs (empty recipients), **When** I build, **Then** validation errors are returned.
5. **Given** PQC is not supported on the platform, **When** I attempt to sign, **Then** a clear error with platform requirements is returned.

---

### User Story 2 - Multi-Recipient Payload Encryption (Priority: P0)

As a transaction sender, I need to encrypt payloads using ML-KEM hybrid encryption for specific recipients so that only authorized Participants can access the data with quantum resistance.

**Why this priority**: Essential for privacy - payloads contain sensitive data.

**Independent Test**: Can be tested by encrypting a payload for multiple recipients using ML-KEM and verifying each can decrypt.

**Acceptance Scenarios**:

1. **Given** data and recipient wallets, **When** I call AddPayload, **Then** the data is encrypted with AES-256-GCM and the symmetric key is encapsulated via ML-KEM for each recipient.
2. **Given** an encrypted payload, **When** recipient A decrypts with their ML-KEM private key, **Then** the original data is returned.
3. **Given** an encrypted payload, **When** non-recipient tries to decrypt, **Then** decryption fails.
4. **Given** compression is enabled, **When** data is compressible, **Then** it is compressed before encryption.
5. **Given** ML-KEM-512 (default), **When** encrypting for one recipient, **Then** 768-byte ciphertext overhead is added per recipient.

---

### User Story 3 - Transaction Verification (Priority: P0)

As a transaction receiver, I need to verify ML-DSA signatures and payload integrity so that I can trust the transaction authenticity with quantum resistance.

**Why this priority**: Essential for security - verifies transaction integrity.

**Independent Test**: Can be tested by verifying valid and tampered transactions.

**Acceptance Scenarios**:

1. **Given** a valid transaction signed with ML-DSA, **When** I call VerifyAsync, **Then** verification succeeds.
2. **Given** a transaction with tampered signature, **When** I verify, **Then** verification fails.
3. **Given** a transaction with tampered payload hash, **When** I verify payloads, **Then** verification fails.
4. **Given** wrong sender public key, **When** I verify signature, **Then** verification fails.
5. **Given** ML-DSA verification, **When** checking signature, **Then** verification completes in under 50ms.

---

### User Story 4 - Transaction Serialization (Priority: P1)

As a developer, I need to serialize transactions with large PQC signatures to multiple formats so that they can be stored and transmitted efficiently.

**Why this priority**: Required for storage and network transmission.

**Independent Test**: Can be tested by serializing and deserializing transactions with 2,420+ byte signatures.

**Acceptance Scenarios**:

1. **Given** a signed transaction with ML-DSA-44 signature, **When** I serialize to binary, **Then** compact binary format with VarInt length-prefixed signature is produced.
2. **Given** binary data, **When** I deserialize, **Then** the original transaction with large signature is reconstructed.
3. **Given** a transaction, **When** I serialize to JSON, **Then** human-readable JSON with Base64-encoded signature is produced.
4. **Given** a transaction, **When** I create transport packet, **Then** network-optimized format is produced.

---

### User Story 5 - Payload Access Control (Priority: P2)

As a payload owner, I need to grant access to additional recipients using ML-KEM re-encapsulation so that I can share data after transaction creation.

**Why this priority**: Convenience feature for post-creation sharing.

**Independent Test**: Can be tested by granting access and verifying new recipient can decrypt via ML-KEM.

**Acceptance Scenarios**:

1. **Given** an existing payload, **When** owner grants access to new recipient, **Then** symmetric key is re-encapsulated with new recipient's ML-KEM public key.
2. **Given** a non-owner, **When** attempting to grant access, **Then** operation fails with error.
3. **Given** a protected payload, **When** attempting to modify access, **Then** operation fails.

---

### Edge Cases

- What happens when payload encryption fails for one recipient but succeeds for others? → Fail entire operation, return list of failed recipients.
- How does the system handle transactions with zero payloads? → Valid - metadata-only transactions allowed.
- What happens when deserializing corrupted binary data? → Return `TransactionStatus.SerializationFailed` with error details.
- How are transactions with very large payloads (>10MB) handled? → Stream-based encryption with chunking.
- What happens when signature exceeds expected size? → Validate against known ML-DSA sizes, reject malformed signatures.

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Library MUST provide fluent builder API for transaction creation
- **FR-002**: Library MUST sign transactions using double SHA-256 + ML-DSA (NIST FIPS 204)
- **FR-003**: Library MUST default to ML-DSA-44 for signing, configurable to ML-DSA-65/87
- **FR-004**: Library MUST encrypt payloads using ML-KEM + AES-256-GCM hybrid encryption
- **FR-005**: Library MUST default to ML-KEM-512 for key encapsulation, configurable to ML-KEM-768/1024
- **FR-006**: Library MUST support compression with file type detection
- **FR-007**: Library MUST serialize to binary (VarInt length-prefixed), JSON, and transport formats
- **FR-008**: Library MUST handle signatures up to 4,627 bytes (ML-DSA-87)
- **FR-009**: Library MUST verify ML-DSA signatures and payload hashes
- **FR-010**: Library MUST support multiple payload types (Data, Metadata, Document)
- **FR-011**: Library MUST generate deterministic TxId from signature hash (SHA-256)
- **FR-012**: Library MUST support transaction chaining via PreviousTransaction hash
- **FR-013**: Library MUST use transaction version V1 (PQC-only, no legacy versions)
- **FR-014**: Library MUST NOT support legacy transaction versions or classical cryptography algorithms
- **FR-015**: Library MUST check PQC platform availability and fail with clear error if unavailable
- **FR-016**: Library MUST enforce maximum limits: 1,000 recipients, 1GB total payload, 100MB per payload
- **FR-017**: Library MUST expose transaction size estimates for Blueprint Designer integration
- **FR-018**: Library SHOULD log warnings when transaction size exceeds 10MB or recipients exceed 100
- **FR-019**: Library MUST integrate with OpenTelemetry for metrics (signing time, encryption time, payload sizes) and distributed tracing
- **FR-020**: Library MUST raise .NET events (TransactionSigned, PayloadEncrypted, TransactionVerified) for consumer telemetry extensibility

### Key Entities

- **ITransaction**: Core transaction interface with sender, recipients, payloads, ML-DSA signature
- **ITransactionBuilder**: Fluent builder for transaction construction
- **IPayload**: Encrypted payload with per-recipient ML-KEM key containers
- **IPayloadManager**: Manages ML-KEM + AES-256-GCM payload encryption/decryption
- **TransactionResult<T>**: Result type with status and optional value

### Transaction Version

| Version | Status | Description |
|---------|--------|-------------|
| **V1** | Current | PQC-only (ML-DSA signing, ML-KEM encryption) |

**Note**: Legacy versions (formerly V1-V4 with ED25519/RSA) are not supported. Version numbering reset for PQC era.

### Platform Requirements

- **Windows**: Windows 11 with November 2025 security update or later
- **Linux**: OpenSSL 3.5 or later
- **.NET**: .NET 10 or later
- **Runtime Check**: `MLDsa.IsSupported` and `MLKem.IsSupported` must return true

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transaction creation under 100ms (excluding signing)
- **SC-002**: ML-DSA-44 signing under 100ms
- **SC-003**: ML-DSA-44 verification under 50ms
- **SC-004**: ML-KEM-512 payload encryption under 50ms per recipient
- **SC-005**: Binary serialization under 20ms (including large signatures)
- **SC-006**: Test coverage exceeds 90%
- **SC-007**: All APIs documented with XML documentation
- **SC-008**: Platform availability check under 10ms
