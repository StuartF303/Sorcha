# Tasks: Transaction Handler Library (PQC)

**Feature Branch**: `transaction-handler`
**Created**: 2025-12-03
**Updated**: 2025-12-04 (PQC Migration - Version Reset)
**Status**: Requires Rewrite for PQC

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 8 |
| In Progress | 0 |
| Pending (Rewrite) | 13 |
| ~~Removed~~ | 6 |
| **Total** | **21** |

**Note**: Tasks TXH-012, TXH-013, TXH-014, TXH-022 (backward compatibility) removed - no legacy version support needed.

---

## Phase 1: Foundation (Retain & Update)

### TXH-001: Project Setup
- **Status**: Complete
- **Priority**: P0
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Create Sorcha.TransactionHandler project.

**Acceptance Criteria**:
- [x] Project created with .NET 10
- [x] Sorcha.Cryptography reference
- [x] System.Text.Json reference
- [x] Test project created

---

### TXH-002: Define Interfaces
- **Status**: Complete (Requires PQC Update)
- **Priority**: P0
- **Assignee**: AI Assistant
- **Dependencies**: TXH-001

**Description**: Define all core interfaces.

**Acceptance Criteria**:
- [x] ITransaction interface
- [x] ITransactionBuilder interface
- [x] IPayload interface
- [x] IPayloadManager interface
- [x] ITransactionSerializer interface
- [x] ITransactionVerifier interface

**PQC Update Required**: Update method signatures to use PQC key types.

---

### TXH-003: Define Enums and Models
- **Status**: Complete (Requires PQC Update)
- **Priority**: P0
- **Assignee**: AI Assistant
- **Dependencies**: TXH-001

**Description**: Define all enum types and models.

**Acceptance Criteria**:
- [x] TransactionVersion enum
- [x] TransactionStatus enum
- [x] PayloadType enum
- [x] PayloadOptions class
- [x] TransactionResult<T> class

**PQC Update Required**:
- Reset TransactionVersion to `V1 = 1` only (remove V2-V4)
- Update PayloadOptions for ML-KEM encryption types

---

## Phase 2: PQC Core Implementation

### TXH-004: Reset TransactionVersion Enum
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: TBD
- **Dependencies**: TXH-003

**Description**: Reset version enum for PQC era.

**Acceptance Criteria**:
- [ ] Remove V1, V2, V3, V4 enum values
- [ ] Add new `V1 = 1` for PQC transactions
- [ ] Update XML documentation

**Code Change**:
```csharp
public enum TransactionVersion : uint
{
    /// <summary>
    /// Transaction version 1 - Post-Quantum Cryptography (ML-DSA, ML-KEM)
    /// </summary>
    V1 = 1
}
```

---

### TXH-005: Update Transaction Model for PQC
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-004

**Description**: Update Transaction class for ML-DSA signing.

**Acceptance Criteria**:
- [ ] Replace ED25519 signing with ML-DSA
- [ ] Support ML-DSA-44/65/87 signature sizes (2,420-4,627 bytes)
- [ ] Update signature validation for PQC sizes
- [ ] Add platform availability check (`MLDsa.IsSupported`)
- [ ] Remove hardcoded `WalletNetworks.ED25519`

---

### TXH-006: Update TransactionBuilder for PQC
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-005

**Description**: Update fluent builder for PQC signing.

**Acceptance Criteria**:
- [ ] Update SignAsync to use ML-DSA
- [ ] Support configurable security level (Level 1/3/5)
- [ ] Update Build() for PQC validation
- [ ] Add platform check before signing

---

### TXH-007: Implement ML-KEM Payload Encryption
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: TXH-005

**Description**: Implement hybrid ML-KEM + AES-256-GCM payload encryption.

**Acceptance Criteria**:
- [ ] Implement ML-KEM key encapsulation per recipient
- [ ] Use AES-256-GCM for symmetric payload encryption
- [ ] Support ML-KEM-512/768/1024 variants
- [ ] Store encapsulated keys per recipient (768-1,568 bytes each)
- [ ] Implement decryption with ML-KEM decapsulation

---

### TXH-008: Update PayloadManager for ML-KEM
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-007

**Description**: Update PayloadManager for ML-KEM hybrid encryption.

**Acceptance Criteria**:
- [ ] AddPayloadAsync with ML-KEM encapsulation
- [ ] DecryptPayloadAsync with ML-KEM decapsulation
- [ ] GetAccessiblePayloadsAsync
- [ ] GrantPayloadAccessAsync (re-encapsulation)
- [ ] Compression integration

---

### TXH-009: Update BinarySerializer for Large Signatures
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-005

**Description**: Update binary serialization for 2,420-4,627 byte signatures.

**Acceptance Criteria**:
- [ ] VarInt length prefix for signature field
- [ ] Handle signatures up to 4,627 bytes
- [ ] Update buffer allocation for large signatures
- [ ] Complete deserialization implementation

---

### TXH-010: Simplify TransactionFactory
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: TBD
- **Dependencies**: TXH-004

**Description**: Remove legacy version support from factory.

**Acceptance Criteria**:
- [ ] Remove CreateV1Transaction, CreateV2Transaction, CreateV3Transaction, CreateV4Transaction
- [ ] Single Create() method for V1 PQC transactions
- [ ] Remove version-specific serializer mapping
- [ ] Simplify VersionDetector (only V1)

---

### TXH-011: Update TransactionVerifier for ML-DSA
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-005

**Description**: Implement ML-DSA signature verification.

**Acceptance Criteria**:
- [ ] VerifySignatureAsync with ML-DSA
- [ ] VerifyPayloadsAsync
- [ ] VerifyAsync (combined)
- [ ] Double SHA-256 hash verification
- [ ] Verification under 50ms target

---

## Phase 3: Testing

### TXH-012: Unit Tests - PQC Signing
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-006

**Description**: Unit tests for ML-DSA signing.

**Acceptance Criteria**:
- [ ] ML-DSA-44 signing tests
- [ ] ML-DSA-65 signing tests
- [ ] ML-DSA-87 signing tests
- [ ] Signature size validation tests
- [ ] Platform unavailability tests

---

### TXH-013: Unit Tests - ML-KEM Encryption
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-008

**Description**: Unit tests for ML-KEM payload encryption.

**Acceptance Criteria**:
- [ ] ML-KEM-512 encryption/decryption tests
- [ ] ML-KEM-768 encryption/decryption tests
- [ ] ML-KEM-1024 encryption/decryption tests
- [ ] Multi-recipient tests
- [ ] Access grant/revoke tests

---

### TXH-014: Unit Tests - Serialization
- **Status**: Complete (Requires PQC Update)
- **Priority**: P0
- **Assignee**: AI Assistant
- **Dependencies**: TXH-009

**Description**: Unit tests for serialization with large signatures.

**Acceptance Criteria**:
- [x] Binary round-trip tests
- [x] JSON round-trip tests
- [ ] Large signature (4,627 byte) tests
- [ ] Edge case tests

---

### TXH-015: Integration Tests - End-to-End PQC
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: TXH-011

**Description**: End-to-end integration tests with PQC.

**Acceptance Criteria**:
- [ ] Create-sign-verify flow with ML-DSA
- [ ] Multi-recipient encryption with ML-KEM
- [ ] Serialization round-trip with large signatures
- [ ] Platform availability handling

---

### TXH-016: Performance Benchmarks
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-015

**Description**: Performance benchmarks for PQC operations.

**Acceptance Criteria**:
- [ ] ML-DSA-44 signing < 100ms
- [ ] ML-DSA-44 verification < 50ms
- [ ] ML-KEM-512 encryption < 50ms per recipient
- [ ] Serialization < 20ms

---

## Phase 4: Documentation

### TXH-017: XML Documentation
- **Status**: Complete
- **Priority**: P1
- **Assignee**: AI Assistant
- **Dependencies**: TXH-002

**Description**: Complete XML documentation.

**Acceptance Criteria**:
- [x] All interfaces documented
- [x] All public methods documented
- [x] Usage examples in remarks

---

### TXH-018: README Documentation
- **Status**: Complete (Requires PQC Update)
- **Priority**: P1
- **Assignee**: AI Assistant
- **Dependencies**: TXH-001

**Description**: Update library README for PQC.

**Acceptance Criteria**:
- [x] Quick start guide
- [x] Usage examples
- [x] API overview
- [ ] Update for PQC algorithms
- [ ] Update for V1 only (remove V1-V4 references)

---

### TXH-019: Transport Serializer
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-009

**Description**: Implement transport format for network.

**Acceptance Criteria**:
- [ ] Transport packet creation
- [ ] RegisterId/TxId inclusion
- [ ] Network optimization for large signatures

---

### TXH-020: JSON-LD Serializer
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-014

**Description**: Implement JSON-LD serialization (post-MVD).

**Acceptance Criteria**:
- [ ] @context support
- [ ] @type mapping
- [ ] @id generation

---

### TXH-021: OpenTelemetry & Events Integration
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-006, TXH-008

**Description**: Add observability through OpenTelemetry metrics/traces and .NET events.

**Acceptance Criteria**:
- [ ] OpenTelemetry metrics: signing_duration_ms, encryption_duration_ms, payload_size_bytes
- [ ] OpenTelemetry traces for distributed tracing across services
- [ ] TransactionSigned event with signature algorithm and duration
- [ ] PayloadEncrypted event with recipient count and payload size
- [ ] TransactionVerified event with verification result and duration
- [ ] ActivitySource for transaction operations

---

## Removed Tasks (Legacy Version Support)

The following tasks have been **removed** as legacy transaction version support is no longer required:

| Task ID | Original Description | Reason Removed |
|---------|---------------------|----------------|
| ~~TXH-012~~ | V1 Compatibility | No legacy support - PQC only |
| ~~TXH-013~~ | V2 Compatibility | No legacy support - PQC only |
| ~~TXH-014~~ | V3 Compatibility | No legacy support - PQC only |
| ~~TXH-022~~ | Backward Compatibility Tests | No legacy support - PQC only |
| ~~TXH-026~~ | Migration Guide (old→new) | Clean slate - no migration needed |

---

## Implementation Notes

### Version Reset Rationale

The transaction version numbering has been **reset to V1** for the PQC era because:

1. **No existing data** - Clean slate deployment with no backward compatibility requirements
2. **Breaking cryptographic change** - ML-DSA/ML-KEM fundamentally different from ED25519/RSA
3. **Signature size change** - 64 bytes → 2,420-4,627 bytes requires format changes
4. **Simplified codebase** - Remove ~150 lines of legacy version adapter code
5. **Clear demarcation** - V1 = PQC era, old versions = deprecated classical era

### Code Deletion Summary

Files/code to delete:
- `TransactionVersion.V2`, `V3`, `V4` enum values
- `TransactionFactory.CreateV1Transaction()` through `CreateV4Transaction()` (replace with single `Create()`)
- `VersionDetector` complexity (only detect V1)
- All backward compatibility test fixtures

### Dependencies

This task list depends on completion of:
- **Cryptography Library PQC Migration** (ML-DSA, ML-KEM implementation)
- **WalletNetworks enum update** (MLDSA44, MLDSA65, MLDSA87, MLKEM512, etc.)
