# Feature Specification: Transaction Handler Library

**Feature Branch**: `transaction-handler`
**Created**: 2025-12-03
**Status**: Draft
**Input**: Derived from `.specify/specs/sorcha-transaction-handler.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Transactions (Priority: P0)

As a blockchain developer, I need to create transactions with a fluent API so that I can build and sign transactions easily.

**Why this priority**: Core functionality - all blockchain operations require transactions.

**Independent Test**: Can be tested by creating a transaction, signing it, and verifying the signature.

**Acceptance Scenarios**:

1. **Given** a new TransactionBuilder, **When** I create a V4 transaction with recipients and metadata, **Then** a valid transaction object is returned.
2. **Given** a transaction with payloads, **When** I sign with a WIF private key, **Then** a valid signature is computed using double SHA-256.
3. **Given** a signed transaction, **When** I call Build(), **Then** a complete transaction with TxId is returned.
4. **Given** invalid inputs (empty recipients), **When** I build, **Then** validation errors are returned.

---

### User Story 2 - Multi-Recipient Payload Encryption (Priority: P0)

As a transaction sender, I need to encrypt payloads for specific recipients so that only authorized parties can access the data.

**Why this priority**: Essential for privacy - payloads contain sensitive data.

**Independent Test**: Can be tested by encrypting a payload for multiple recipients and verifying each can decrypt.

**Acceptance Scenarios**:

1. **Given** data and recipient wallets, **When** I call AddPayload, **Then** the data is encrypted with a symmetric key and the key is encrypted for each recipient.
2. **Given** an encrypted payload, **When** recipient A decrypts with their private key, **Then** the original data is returned.
3. **Given** an encrypted payload, **When** non-recipient tries to decrypt, **Then** decryption fails.
4. **Given** compression is enabled, **When** data is compressible, **Then** it is compressed before encryption.

---

### User Story 3 - Transaction Verification (Priority: P0)

As a transaction receiver, I need to verify transaction signatures and payload integrity so that I can trust the transaction authenticity.

**Why this priority**: Essential for security - verifies transaction integrity.

**Independent Test**: Can be tested by verifying valid and tampered transactions.

**Acceptance Scenarios**:

1. **Given** a valid transaction, **When** I call VerifyAsync, **Then** verification succeeds.
2. **Given** a transaction with tampered signature, **When** I verify, **Then** verification fails.
3. **Given** a transaction with tampered payload hash, **When** I verify payloads, **Then** verification fails.
4. **Given** wrong sender public key, **When** I verify signature, **Then** verification fails.

---

### User Story 4 - Transaction Serialization (Priority: P1)

As a developer, I need to serialize transactions to multiple formats so that they can be stored and transmitted.

**Why this priority**: Required for storage and network transmission.

**Independent Test**: Can be tested by serializing and deserializing transactions.

**Acceptance Scenarios**:

1. **Given** a transaction, **When** I serialize to binary, **Then** compact binary format is produced.
2. **Given** binary data, **When** I deserialize, **Then** the original transaction is reconstructed.
3. **Given** a transaction, **When** I serialize to JSON, **Then** human-readable JSON is produced.
4. **Given** a transaction, **When** I create transport packet, **Then** network-optimized format is produced.

---

### User Story 5 - Backward Compatibility (Priority: P1)

As a system that receives old transactions, I need to read V1-V4 transaction formats so that historical data remains accessible.

**Why this priority**: Required for working with existing blockchain data.

**Independent Test**: Can be tested by reading and verifying old transaction formats.

**Acceptance Scenarios**:

1. **Given** V1 binary data, **When** I deserialize, **Then** a valid transaction is returned.
2. **Given** V2 binary data, **When** I deserialize, **Then** a valid transaction is returned.
3. **Given** V3 binary data, **When** I deserialize, **Then** a valid transaction is returned.
4. **Given** unknown version byte, **When** I deserialize, **Then** version detection returns Unknown.

---

### User Story 6 - Payload Access Control (Priority: P2)

As a payload owner, I need to grant access to additional recipients so that I can share data after transaction creation.

**Why this priority**: Convenience feature for post-creation sharing.

**Independent Test**: Can be tested by granting access and verifying new recipient can decrypt.

**Acceptance Scenarios**:

1. **Given** an existing payload, **When** owner grants access to new recipient, **Then** new recipient can decrypt.
2. **Given** a non-owner, **When** attempting to grant access, **Then** operation fails with error.
3. **Given** a protected payload, **When** attempting to modify access, **Then** operation fails.

---

### Edge Cases

- What happens when payload encryption fails for one recipient but succeeds for others?
- How does the system handle transactions with zero payloads?
- What happens when deserializing corrupted binary data?
- How are transactions with very large payloads (>10MB) handled?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Library MUST provide fluent builder API for transaction creation
- **FR-002**: Library MUST sign transactions using double SHA-256 with Sorcha.Cryptography
- **FR-003**: Library MUST encrypt payloads per-recipient with symmetric key + asymmetric key encryption
- **FR-004**: Library MUST support compression with file type detection
- **FR-005**: Library MUST serialize to binary, JSON, and transport formats
- **FR-006**: Library MUST deserialize V1-V4 transaction formats
- **FR-007**: Library MUST verify transaction signatures and payload hashes
- **FR-008**: Library MUST support multiple payload types (Data, Metadata, Document)
- **FR-009**: Library MUST generate deterministic TxId from transaction content
- **FR-010**: Library MUST support transaction chaining via PreviousTransaction hash

### Key Entities

- **ITransaction**: Core transaction interface with sender, recipients, payloads, signature
- **ITransactionBuilder**: Fluent builder for transaction construction
- **IPayload**: Encrypted payload with per-recipient key containers
- **IPayloadManager**: Manages payload encryption/decryption
- **TransactionResult<T>**: Result type with status and optional value

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transaction creation under 100ms (excluding signing)
- **SC-002**: Transaction signing under 50ms for ED25519/NIST P-256
- **SC-003**: Payload encryption under 20ms per recipient
- **SC-004**: Binary serialization under 10ms
- **SC-005**: Test coverage exceeds 90%
- **SC-006**: 100% backward compatibility with V1-V4 transactions
- **SC-007**: All APIs documented with XML documentation
