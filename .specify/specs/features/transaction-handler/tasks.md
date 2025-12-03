# Tasks: Transaction Handler Library

**Feature Branch**: `transaction-handler`
**Created**: 2025-12-03
**Status**: 68% Complete

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 14 |
| In Progress | 4 |
| Pending | 8 |
| **Total** | **26** |

---

## Tasks

### TXH-001: Project Setup
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
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
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
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

---

### TXH-003: Define Enums and Models
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 3 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-001

**Description**: Define all enum types and models.

**Acceptance Criteria**:
- [x] TransactionVersion enum
- [x] TransactionStatus enum
- [x] PayloadType enum
- [x] PayloadOptions class
- [x] TransactionResult<T> class

---

### TXH-004: Implement Transaction Model
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-002

**Description**: Implement Transaction class.

**Acceptance Criteria**:
- [x] All properties
- [x] Equality comparison
- [x] Hash calculation
- [x] Clone support

---

### TXH-005: Implement TransactionBuilder
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-004

**Description**: Implement fluent transaction builder.

**Acceptance Criteria**:
- [x] Create() method
- [x] WithPreviousTransaction()
- [x] WithRecipients()
- [x] WithMetadata()
- [ ] AddPayload() (uses PayloadManager)
- [ ] SignAsync()
- [ ] Build()

---

### TXH-006: Implement Payload Model
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-002

**Description**: Implement Payload class.

**Acceptance Criteria**:
- [x] All properties
- [x] Per-recipient key containers
- [x] Flags handling

---

### TXH-007: Implement PayloadManager
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 10 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-006

**Description**: Implement payload encryption/decryption.

**Acceptance Criteria**:
- [x] AddPayloadAsync (encryption)
- [ ] DecryptPayloadAsync
- [ ] GetAccessiblePayloadsAsync
- [ ] GrantPayloadAccessAsync
- [ ] Compression integration

---

### TXH-008: Implement BinarySerializer
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-004, TXH-006

**Description**: Implement binary serialization.

**Acceptance Criteria**:
- [x] VarInt encoding
- [x] VarString encoding
- [x] Transaction serialization
- [ ] Payload serialization
- [ ] Deserialization

---

### TXH-009: Implement JsonSerializer
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-004

**Description**: Implement JSON serialization.

**Acceptance Criteria**:
- [x] System.Text.Json integration
- [x] Custom converters
- [x] Pretty print option

---

### TXH-010: Implement TransactionVerifier
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-008

**Description**: Implement signature and payload verification.

**Acceptance Criteria**:
- [x] VerifySignatureAsync
- [ ] VerifyPayloadsAsync
- [ ] VerifyAsync (combined)
- [ ] Double SHA-256 hash verification

---

### TXH-011: Implement VersionDetector
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-008

**Description**: Detect transaction version from data.

**Acceptance Criteria**:
- [x] Binary version detection
- [x] JSON version detection
- [x] Unknown version handling

---

### TXH-012: Implement V1 Compatibility
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-011

**Description**: Read V1 transaction format.

**Acceptance Criteria**:
- [ ] V1 deserializer
- [ ] V1 signature verification
- [ ] V1 payload decryption

---

### TXH-013: Implement V2 Compatibility
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-011

**Description**: Read V2 transaction format.

**Acceptance Criteria**:
- [ ] V2 deserializer
- [ ] V2 signature verification
- [ ] V2 payload decryption

---

### TXH-014: Implement V3 Compatibility
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-011

**Description**: Read V3 transaction format.

**Acceptance Criteria**:
- [x] V3 deserializer
- [x] V3 signature verification
- [x] V3 payload decryption

---

### TXH-015: Implement TransportSerializer
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-008

**Description**: Implement transport format.

**Acceptance Criteria**:
- [ ] Transport packet creation
- [ ] RegisterId/TxId inclusion
- [ ] Network optimization

---

### TXH-016: Implement JsonLdSerializer
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-009

**Description**: Implement JSON-LD serialization.

**Acceptance Criteria**:
- [ ] @context support
- [ ] @type mapping
- [ ] @id generation

---

### TXH-017: Unit Tests - Builder
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-005

**Description**: Unit tests for TransactionBuilder.

**Acceptance Criteria**:
- [x] Fluent chain tests
- [x] Validation tests
- [x] Error handling tests

---

### TXH-018: Unit Tests - PayloadManager
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TXH-007

**Description**: Unit tests for PayloadManager.

**Acceptance Criteria**:
- [ ] Encryption tests
- [ ] Decryption tests
- [ ] Multi-recipient tests
- [ ] Compression tests

---

### TXH-019: Unit Tests - Serialization
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-008, TXH-009

**Description**: Unit tests for serialization.

**Acceptance Criteria**:
- [x] Binary round-trip tests
- [x] JSON round-trip tests
- [x] Edge case tests

---

### TXH-020: Unit Tests - Verification
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-010

**Description**: Unit tests for verification.

**Acceptance Criteria**:
- [ ] Valid transaction tests
- [ ] Tampered signature tests
- [ ] Tampered payload tests

---

### TXH-021: Integration Tests
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: TXH-005, TXH-007

**Description**: End-to-end integration tests.

**Acceptance Criteria**:
- [ ] Create-sign-verify flow
- [ ] Multi-recipient flow
- [ ] Backward compatibility flow

---

### TXH-022: Backward Compatibility Tests
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-012, TXH-013, TXH-014

**Description**: Tests with real V1-V4 transactions.

**Acceptance Criteria**:
- [x] V3 transaction samples
- [x] V4 transaction samples
- [ ] V1 transaction samples
- [ ] V2 transaction samples

---

### TXH-023: Performance Benchmarks
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TXH-021

**Description**: Create performance benchmarks.

**Acceptance Criteria**:
- [ ] Transaction creation benchmarks
- [ ] Signing benchmarks
- [ ] Serialization benchmarks

---

### TXH-024: XML Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-002

**Description**: Complete XML documentation.

**Acceptance Criteria**:
- [x] All interfaces documented
- [x] All public methods documented
- [x] Usage examples in remarks

---

### TXH-025: README Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 3 hours
- **Assignee**: AI Assistant
- **Dependencies**: TXH-001

**Description**: Create library README.

**Acceptance Criteria**:
- [x] Quick start guide
- [x] Usage examples
- [x] API overview

---

### TXH-026: Migration Guide
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 3 hours
- **Assignee**: TBD
- **Dependencies**: TXH-025

**Description**: Migration from old implementation.

**Acceptance Criteria**:
- [ ] Breaking changes list
- [ ] Migration steps
- [ ] Code examples

---

## Notes

- TransactionBuilder fluent API is 70% complete
- PayloadManager encryption working, decryption in progress
- V3/V4 backward compatibility working
- V1/V2 compatibility is lower priority (legacy)
- JSON-LD serialization is post-MVD
