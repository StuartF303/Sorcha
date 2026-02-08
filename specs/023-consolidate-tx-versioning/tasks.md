# Tasks: Consolidate Transaction Versioning

**Feature**: 023-consolidate-tx-versioning
**Date**: 2026-02-08

## Phase 1: Enum Foundation

- [X] **T1**: Remove V2, V3, V4 from `TransactionVersion` enum in `src/Common/Sorcha.TransactionHandler/Enums/TransactionVersion.cs`
  - Keep only `V1 = 1` with updated doc comment

## Phase 2: Source Code (fix compile errors)

- [X] **T2**: Update `TransactionFactory.cs` — remove `CreateV2Transaction()`, `CreateV3Transaction()`, `CreateV4Transaction()`, simplify switch to V1-only
- [X] **T3**: Update `VersionDetector.cs` — remove version 2/3/4 from both binary and JSON switch statements
- [X] **T4**: Update `Transaction.cs` — change default parameter from `V4` to `V1`
- [X] **T5**: Update `TransactionBuilder.cs` — change default parameter from `V4` to `V1`
- [X] **T6**: Update `ITransactionBuilder.cs` — change default parameter from `V4` to `V1`
- [X] **T7**: Update `TransactionBuilderService.cs` — change 3 call sites from `V4` to `V1`

## Phase 3: Test Updates

- [X] **T8**: Rewrite `VersionDetectionTests.cs` — V1-only detection, V2/V3/V4 rejection tests
- [X] **T9**: Rewrite `TransactionFactoryTests.cs` — V1-only creation, unsupported version rejection
- [X] **T10**: Update `VersioningTests.cs` — V1-only detection, V2/V3/V4 as unsupported
- [X] **T11**: Update `TransactionBuilderTests.cs` — V4→V1 references
- [X] **T12**: Update `SerializerTests.cs` — V4→V1 references
- [X] **T13**: Update `TransactionTests.cs` — V4→V1, remove multi-version theory
- [X] **T14**: Update `EndToEndTransactionTests.cs` — V4→V1
- [X] **T15**: Update `MultiRecipientTests.cs` — V4→V1
- [X] **T16**: Update `SigningVerificationTests.cs` — V4→V1
- [X] **T17**: Update `TransactionBenchmarks.cs` — V4→V1
- [X] **T18**: Update `TransactionBuilderServiceTests.cs` — V4→V1 assertion

## Phase 4: Validation

- [X] **T19**: Build solution — 0 errors (4 pre-existing UI Core errors unrelated)
- [X] **T20**: Run TransactionHandler tests — 127/127 passed
- [X] **T21**: Run Blueprint Service tests — 194/194 passed
- [X] **T22**: Codebase sweep — 0 remaining `TransactionVersion.V[234]` in source/test files
- [X] **T23**: Update TransactionHandler README.md — V4→V1
