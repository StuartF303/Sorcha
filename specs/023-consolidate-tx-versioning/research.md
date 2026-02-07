# Research: Consolidate Transaction Versioning

**Feature**: 023-consolidate-tx-versioning
**Date**: 2026-02-07

## Research Summary

This feature has no unresolved NEEDS CLARIFICATION items. The spec was fully defined based on codebase analysis. Research focused on verifying assumptions and identifying the complete change surface.

## Findings

### 1. Version Usage Across Codebase

**Decision**: All V2/V3/V4 references can be safely removed.

**Rationale**: Exhaustive codebase search confirmed:
- Only the Blueprint service's `TransactionBuilderService` creates transactions (3 call sites, all hardcoded to V4)
- No service reads or branches on specific version values at runtime
- The `VersionDetector` is used only during deserialization, and all versions share the same serializer
- The `TransactionModel.Version` in Register Models already defaults to `1`

**Alternatives considered**:
- Keep V2/V3/V4 as deprecated but present: Rejected — adds no value with no production data
- Remove versioning entirely: Rejected — version field in wire format is needed for future evolution

### 2. Binary Wire Format Compatibility

**Decision**: Wire format is structurally unchanged. Only the first 4 bytes (version uint32) change value.

**Rationale**: Analysis of `BinaryTransactionSerializer` confirmed:
- All versions use the same serialization path — the serializer writes all fields regardless of version
- The `TransactionFactory.GetSerializer()` returns the same `BinaryTransactionSerializer` for all versions
- `Transaction` class is shared across all versions; version is just a marker property
- No version-conditional serialization logic exists anywhere

**Alternatives considered**: None — this was a verification, not a choice.

### 3. Test Strategy

**Decision**: Update existing tests in-place rather than deleting and recreating.

**Rationale**:
- BackwardCompatibility tests: Rewrite to test V1-only detection and rejection of unsupported versions (2, 3, 4). The folder name "BackwardCompatibility" becomes forward-looking — it now tests that only V1 is accepted.
- Unit/Integration tests: Simple find-replace of V4→V1 references plus removal of V2/V3/V4 theory data.
- No new test files needed; existing test coverage already validates all capabilities.

**Alternatives considered**:
- Delete BackwardCompatibility folder entirely: Rejected — the tests for unsupported version rejection are still valuable
- Create new "VersionConsolidation" test class: Rejected — unnecessary indirection; existing test structure is sufficient

### 4. TransactionModel Default Verification

**Decision**: No change needed to `TransactionModel.cs`.

**Rationale**: The `Version` property already defaults to `1` (`public uint Version { get; set; } = 1;`). This was set during the original Register Models design and happens to align with the consolidated V1. Verified at `src/Common/Sorcha.Register.Models/TransactionModel.cs:60`.

### 5. JSON-LD Context URL

**Decision**: No change needed.

**Rationale**: The context URL `https://sorcha.dev/contexts/blockchain/v1.jsonld` already references "v1". The "v1" in the URL refers to the context schema version, not the transaction version, but the alignment is a bonus.
