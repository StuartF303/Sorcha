# Feature Specification: Consolidate Transaction Versioning

**Feature Branch**: `023-consolidate-tx-versioning`
**Created**: 2026-02-07
**Status**: Draft
**Input**: User description: "Consolidate transaction versioning by rolling forward V4 capabilities into a new V1, eliminating V1-V3 legacy versions. No production data exists, so no backwards compatibility is needed. Reset the version counter for clean future evolution."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Clean Version Baseline for Future Development (Priority: P1)

As a platform developer, I want the transaction system to have a single version (V1) that includes all current capabilities, so that future version increments start from a clean, well-defined baseline and maintain proper backwards compatibility going forward.

**Why this priority**: This is the core purpose of the feature. Without a clean single-version baseline, future version management becomes increasingly complex with dead code paths and unused adapters.

**Independent Test**: Can be fully tested by creating a transaction, serializing it, and verifying the version field reads as `1` while retaining all fields (sender, recipients, metadata, register ID, block number, payloads). Delivers immediate value by eliminating version confusion.

**Acceptance Scenarios**:

1. **Given** a developer creates a new transaction using the builder with default settings, **When** the transaction is serialized to binary, **Then** the first 4 bytes encode version `1` (little-endian uint32)
2. **Given** a developer creates a new transaction using the builder with default settings, **When** the transaction is serialized to JSON, **Then** the `version` field is `1`
3. **Given** a developer creates a transaction with all fields populated (sender wallet, recipients, metadata, register ID, block number, payloads), **When** the transaction is built and serialized, **Then** all fields are present and correctly preserved in both binary and JSON formats
4. **Given** a developer attempts to create a transaction with a version other than V1, **When** the factory or builder is called, **Then** the system rejects the request with a clear error indicating only V1 is supported

---

### User Story 2 - Removal of Dead Version Code Paths (Priority: P2)

As a platform maintainer, I want all V2, V3, and V4 version-specific code paths, adapters, and factory methods removed, so that the codebase is simpler to understand, maintain, and extend.

**Why this priority**: Dead code creates confusion and maintenance burden. Removing it prevents developers from accidentally using obsolete patterns.

**Independent Test**: Can be verified by confirming the `TransactionVersion` enum contains only `V1 = 1`, the factory has a single creation path, and the version detector rejects any version other than 1.

**Acceptance Scenarios**:

1. **Given** the `TransactionVersion` enum, **When** a developer inspects the available values, **Then** only `V1 = 1` exists
2. **Given** binary data with version bytes encoding `2`, `3`, or `4`, **When** the version detector processes it, **Then** a `NotSupportedException` is thrown
3. **Given** JSON data with `"version": 2` (or 3, or 4), **When** the version detector processes it, **Then** a `NotSupportedException` is thrown
4. **Given** the transaction factory, **When** a developer inspects the code, **Then** no version-specific adapter methods (CreateV2, CreateV3, CreateV4) exist

---

### User Story 3 - Consistent Version References Across All Services (Priority: P2)

As a platform developer, I want all service-layer code that creates transactions to use the new V1 default, so that the entire system produces consistent version-1 transactions.

**Why this priority**: Service-layer consistency prevents version mismatches between components. Equal priority to Story 2 as both are necessary for a clean codebase.

**Independent Test**: Can be verified by searching all service code for transaction creation calls and confirming they use V1 (either explicitly or via the updated default).

**Acceptance Scenarios**:

1. **Given** the Blueprint service builds an action transaction, **When** the transaction is created, **Then** it uses `TransactionVersion.V1`
2. **Given** the Blueprint service builds a rejection transaction, **When** the transaction is created, **Then** it uses `TransactionVersion.V1`
3. **Given** the Blueprint service builds file transactions, **When** the transactions are created, **Then** they use `TransactionVersion.V1`

---

### User Story 4 - Updated Test Suite Validates V1-Only Behavior (Priority: P3)

As a quality engineer, I want the test suite to validate that V1 is the only supported version and that all V1 capabilities work correctly, so that regressions are caught if version handling is accidentally changed.

**Why this priority**: Tests are the safety net. While lower priority than the code changes themselves, they are essential for long-term confidence.

**Independent Test**: Can be verified by running the full test suite and confirming all tests pass with V1-only assertions, and that no tests reference V2, V3, or V4.

**Acceptance Scenarios**:

1. **Given** the updated test suite, **When** all tests are executed, **Then** every test passes
2. **Given** versioning tests, **When** V1 binary detection is tested, **Then** it succeeds
3. **Given** versioning tests, **When** version 2, 3, or 4 detection is tested (binary or JSON), **Then** `NotSupportedException` is thrown
4. **Given** serialization round-trip tests, **When** a V1 transaction is serialized and deserialized, **Then** all fields are preserved (including recipients, metadata, register ID, and block number)

---

### Edge Cases

- What happens when binary data contains version `0` or very large version numbers? The system throws `NotSupportedException` (existing behavior, unchanged).
- What happens when binary data is fewer than 4 bytes? The system throws `ArgumentException` (existing behavior, unchanged).
- What happens when JSON lacks a `version` property? The system throws `ArgumentException` (existing behavior, unchanged).
- What happens when existing code explicitly passes `TransactionVersion.V1` to the builder? It works identically to the default (no behavioral change).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The transaction version enum MUST contain only a single value: `V1 = 1`
- **FR-002**: The transaction factory MUST create transactions only for version V1, rejecting all other version values
- **FR-003**: The version detector MUST recognize only version `1` from both binary and JSON data, throwing `NotSupportedException` for any other value
- **FR-004**: The transaction builder MUST default to `TransactionVersion.V1` when no version is specified
- **FR-005**: V1 transactions MUST support all fields: TxId, Timestamp, PreviousTxHash, SenderWallet, Recipients, Metadata, Signature, Payloads, RegisterId, and BlockNumber
- **FR-006**: The binary wire format MUST remain structurally identical to the current format (field order, encoding, VarInt usage) with only the version bytes changing from `4` to `1`
- **FR-007**: The JSON serialization format MUST remain structurally identical to the current format with only the `version` field value changing from `4` to `1`
- **FR-008**: The JSON-LD serialization MUST continue to function identically, writing version `1` in the output
- **FR-009**: All service-layer transaction creation code MUST reference `TransactionVersion.V1`
- **FR-010**: All version-specific adapter methods and comments referencing V2, V3, and V4 adapters MUST be removed
- **FR-011**: The backward compatibility test folder MUST be updated to test V1-only behavior (or removed if redundant with unit tests)
- **FR-012**: The `TransactionModel` in Register Models MUST continue to default its `Version` property to `1` (no change required, verified only)

### Key Entities

- **Transaction**: The core signed data structure containing version, sender, recipients, metadata, payloads, and cryptographic signature. After consolidation, all transactions are version 1 with the full field set.
- **TransactionVersion**: Enum defining supported protocol versions. After consolidation, contains only `V1 = 1`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The full test suite passes with zero failures after all version consolidation changes
- **SC-002**: The `TransactionVersion` enum contains exactly one value (`V1 = 1`)
- **SC-003**: No source code references to `TransactionVersion.V2`, `TransactionVersion.V3`, or `TransactionVersion.V4` exist anywhere in the codebase
- **SC-004**: Transaction round-trip serialization (create, serialize, deserialize) preserves all 10 transaction fields without data loss
- **SC-005**: The binary wire format size for an identical transaction is unchanged (only the version bytes differ, and the payload structure is identical)

## Assumptions

- No production data exists that uses V1, V2, V3, or V4 transaction formats, so no data migration is needed
- No external systems or third-party integrations consume the transaction binary or JSON format, so no coordination is required
- The JSON-LD context URL (`https://sorcha.dev/contexts/blockchain/v1.jsonld`) already uses "v1" and requires no change
- The `TransactionModel.Version` default of `1` in Register Models is already correct and requires verification only
- Benchmarks referencing specific versions will be updated to V1 to maintain performance baseline comparability
