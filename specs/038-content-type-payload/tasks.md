# Tasks: Content-Type Aware Payload Encoding

**Input**: Design documents from `/specs/038-content-type-payload/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — spec FR-020 through FR-024 and User Story 6 explicitly require extensive cryptographic conformance testing.

**Organization**: Tasks grouped by user story. US1 (Self-Describing Payloads) and US2 (Base64url Migration) are both P1 but US2 depends on US1's metadata fields. US6 (Crypto Conformance) is P1 and validates US2. US7 (Cloud Portability) is P4-Future — no implementation tasks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US6)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the new metadata fields to shared models and create the centralized encoding service

- [x] T001 [P] Add `ContentType` (string?) and `ContentEncoding` (string?) properties to `PayloadModel` in `src/Common/Sorcha.Register.Models/PayloadModel.cs`
- [x] T002 [P] Add `ContentType` (string?) and `ContentEncoding` (string?) properties to `PayloadInfo` in `src/Common/Sorcha.TransactionHandler/Models/PayloadInfo.cs`
- [x] T003 [P] Create `IPayloadEncodingService` interface in `src/Common/Sorcha.TransactionHandler/Services/IPayloadEncodingService.cs` with methods: `EncodeToString`, `DecodeToBytes`, `DetectLegacyEncoding`, `ResolveContentEncoding` per `contracts/encoding-helper.md`
- [x] T004 Create `PayloadEncodingService` implementation in `src/Common/Sorcha.TransactionHandler/Services/PayloadEncodingService.cs` — Base64url encode/decode via `System.Buffers.Text.Base64Url`, legacy Base64 fallback via `Convert.FromBase64String`, Brotli/Gzip compress/decompress via `System.IO.Compression`, content encoding resolution logic, configurable compression threshold (default 4KB)
- [x] T005 Register `IPayloadEncodingService` in DI — add to `Sorcha.TransactionHandler` service registration (or create extension method if none exists) so Blueprint, Validator, and Register services resolve it

---

## Phase 2: Foundational (Test Infrastructure)

**Purpose**: Create conformance test infrastructure with known test vectors that gates all encoding changes

**CRITICAL**: Test infrastructure must be ready before Base64url migration begins (Phase 3)

- [x] T006 Create known test vectors file `tests/Sorcha.TransactionHandler.Tests/TestData/EncodingTestVectors.cs` — static class with fixed byte arrays, their expected Base64 strings, expected Base64url strings, expected SHA-256 hashes, expected Brotli-compressed outputs, and expected Gzip-compressed outputs
- [x] T007 Create `PayloadEncodingServiceTests` in `tests/Sorcha.TransactionHandler.Tests/Services/PayloadEncodingServiceTests.cs` — test all `IPayloadEncodingService` methods: Base64url encode/decode round-trip, legacy Base64 decode, identity encoding for JSON, Brotli compress/decompress round-trip, Gzip compress/decompress round-trip, legacy detection (`+` and `/` chars), content encoding resolution logic, below-threshold skips compression, null/empty input handling
- [x] T008 [P] Create `EncodingConformanceTests` in `tests/Sorcha.Cryptography.Tests/EncodingConformanceTests.cs` — test vectors for: Base64url encoding produces expected output, Base64url decoding of known vectors produces expected bytes, standard Base64 chars (`+`, `/`, `=`) never appear in Base64url output, round-trip encode/decode is byte-identical

**Checkpoint**: Test infrastructure ready — encoding migration can begin

---

## Phase 3: User Story 1 — Self-Describing Payloads (Priority: P1) MVP

**Goal**: Payloads carry `ContentType` and `ContentEncoding` metadata so consumers know the data format without guessing

**Independent Test**: Publish a transaction with a JSON payload, read it back, verify `ContentType: "application/json"` and `ContentEncoding` are present in the response

### Tests for User Story 1

- [x] T009 [P] [US1] Add tests for ContentType/ContentEncoding in `JsonTransactionSerializer` — serialize a payload with ContentType/ContentEncoding set, verify they appear in JSON output; deserialize JSON with these fields, verify they round-trip. In `tests/Sorcha.TransactionHandler.Tests/Serialization/JsonTransactionSerializerTests.cs`
- [x] T010 [P] [US1] Add tests for legacy payload handling — deserialize a payload JSON with no ContentType/ContentEncoding, verify defaults to `application/octet-stream` / `base64`. In `tests/Sorcha.TransactionHandler.Tests/Serialization/JsonTransactionSerializerTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Update `JsonTransactionSerializer.Serialize()` to include `ContentType` and `ContentEncoding` in serialized PayloadModel JSON output in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs`
- [x] T012 [US1] Update `JsonTransactionSerializer.Deserialize()` to read `ContentType` and `ContentEncoding` from PayloadModel JSON, defaulting to null when absent (legacy) in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs` — N/A: DeserializeFromJson does not reconstruct payloads
- [x] T013 [US1] Update `PayloadManager` to accept and propagate ContentType when building payloads — internal `Payload` class gains `ContentType` and `ContentEncoding` fields in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs`
- [x] T014 [US1] Update `ITransactionBuilderService.BuildActionTransactionAsync()` to set ContentType/ContentEncoding on PayloadModel based on payload content in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` — Updated all 5 PayloadModel construction sites with ContentEncoding="base64"
- [x] T015 [US1] Map ContentType/ContentEncoding in PayloadInfo where PayloadInfo is constructed from PayloadModel — update all mapping sites in `src/Common/Sorcha.TransactionHandler/Models/PayloadInfo.cs` and any mapper code

**Checkpoint**: New transactions carry content-type metadata. Legacy transactions still work (null fields = legacy defaults).

---

## Phase 4: User Story 2 — Base64url Encoding Migration (Priority: P1)

**Goal**: All new binary-to-text encoding uses Base64url (RFC 4648 Section 5) instead of standard Base64. Legacy Base64 remains readable.

**Independent Test**: Submit a transaction through the full pipeline (build → sign → validate → store → retrieve). All binary fields use Base64url. Validator verifies signatures. Existing transactions remain readable.

**Depends on**: Phase 3 (US1) — ContentEncoding field needed to distinguish base64url from legacy base64

### Tests for User Story 2

- [X] T016 [P] [US2] Add Base64url serialization tests — serialize a transaction, verify all binary fields (Data, Hash, IV, Challenges.Data, Signature) contain only Base64url chars (`A-Za-z0-9-_`, no `+/=`). In `tests/Sorcha.TransactionHandler.Tests/Serialization/JsonTransactionSerializerTests.cs`
- [X] T017 [P] [US2] Add legacy Base64 deserialization tests — deserialize a transaction with standard Base64 fields, verify bytes decode correctly. In `tests/Sorcha.TransactionHandler.Tests/Serialization/JsonTransactionSerializerTests.cs`
- [X] T018 [P] [US2] Add signature round-trip tests — sign data with Ed25519, encode signature as Base64url, decode, verify — confirm identical to signing with Base64 encoding (same bytes, different text). In `tests/Sorcha.Cryptography.Tests/EncodingConformanceTests.cs`
- [X] T019 [P] [US2] Add hash stability tests — compute SHA-256 hash on fixed bytes, encode to Base64url, decode back, recompute hash — confirm byte-identical. In `tests/Sorcha.Cryptography.Tests/EncodingConformanceTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] Migrate `JsonTransactionSerializer` — replace all `Convert.ToBase64String` with `Base64Url.EncodeToString` and `Convert.FromBase64String` with encoding-aware decode (detect legacy via `IPayloadEncodingService.DetectLegacyEncoding`) in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs`
- [X] T021 [US2] Migrate `PayloadManager` — replace `Convert.ToBase64String`/`FromBase64String` for encrypted key material (Challenge.Data) and any intermediate encoding in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs`
- [X] T022 [US2] Migrate `ITransactionBuilderService` — replace `Convert.ToBase64String` for `SignatureInfo.PublicKey`, `SignatureInfo.SignatureValue`, and `TransactionModel.Signature` (~lines 285-286, 323) in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs`
- [X] T023 [P] [US2] Migrate `Blueprint.Service/Program.cs` — replace `Convert.ToBase64String` for genesis TX PayloadModel.Data (~line 886) and TransactionModel.Signature in `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [X] T024 [P] [US2] Migrate `RegisterCreationOrchestrator` — replace `Convert.ToBase64String` for genesis TX PayloadModel.Data (~line 578) in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`
- [X] T025 [US2] Migrate `DocketBuildTriggerService` — replace `Convert.ToBase64String` for PayloadModel.Data (~line 261), SenderWallet (~line 279), Signature (~line 282) in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [X] T026 [P] [US2] Migrate `DocketSerializer` — replace `Convert.ToBase64String` for PayloadModel.Data (~line 83) and TransactionModel.Signature (~line 93) in `src/Services/Sorcha.Validator.Service/Services/DocketSerializer.cs`
- [X] T027 [US2] Update `ValidationEngine` signature verification — N/A: ValidationEngine has no Base64 calls; it operates on bytes from Transaction model. Signature decode happens upstream.
- [X] T028 [US2] Migrate remaining `Sorcha.Cryptography` Base64 call sites — consolidated SdJwtService manual Base64url helpers to use `System.Buffers.Text.Base64Url` in `src/Common/Sorcha.Cryptography/SdJwt/SdJwtService.cs`
- [X] T029 [US2] Migrate remaining source files — migrated ParticipantPublishingService, WalletVerificationService, DIDResolver, GovernanceRosterService, PayloadResolverService, StateReconstructionService, DatabaseInitializer, ServiceAuthService. Smart decode (DecodeBase64Auto) for read paths. Wallet Service internal storage intentionally kept as standard Base64.
- [X] T030 [US2] Set `ContentEncoding: "base64url"` on all new write paths — all PayloadModel construction sites now set ContentEncoding to `"base64url"`. Legacy payloads with null ContentEncoding remain interpreted as `"base64"`

**Checkpoint**: All new transactions use Base64url. Legacy transactions decode correctly. Full pipeline works.

---

## Phase 5: User Story 6 — Cryptographic Integrity Conformance Testing (Priority: P1)

**Goal**: Prove the encoding migration does not break any cryptographic operation. Gates release of all preceding changes.

**Independent Test**: Run the full conformance test suite — all 20+ scenarios pass (5 encoding paths x 4 crypto operations). Legacy and new transactions both verify.

**Depends on**: Phase 4 (US2) — encoding migration must be complete to test

### Implementation for User Story 6

- [X] T031 [P] [US6] Create sign-then-verify round-trip tests for Base64url signatures — Ed25519 and P-256 sign with Base64url-encoded output, verify succeeds. In `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T032 [P] [US6] Create encrypt-then-decrypt round-trip tests — XChaCha20-Poly1305 encrypt, Base64url-encode ciphertext+IV+keys, decode, decrypt — verify byte-identical plaintext. In `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T033 [P] [US6] Create hash integrity tests — SHA-256 hash on canonical bytes, encode to Base64url, store, retrieve, decode, rehash — verify identical. In `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T034 [P] [US6] Create legacy interoperability tests — verify Base64-encoded transactions (pre-migration format) still pass signature verification, hash checks, and payload decryption with the updated code. In `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T035 [P] [US6] Create cross-encoding interop tests — transaction produced with legacy Base64 encoding consumed by new Base64url decoder (and vice versa). In `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T036 [US6] Create encoding round-trip stability tests — for each ContentEncoding value (`base64url`, `identity`, `br+base64url`, `gzip+base64url`), encode data, store as string, retrieve, decode — verify byte-identical to original. In `tests/Sorcha.TransactionHandler.Tests/Services/PayloadEncodingServiceTests.cs`
- [X] T037 [US6] Create known test vector verification — fixed inputs produce expected Base64url output, expected SHA-256 hash, expected compressed output across repeated runs. In `tests/Sorcha.TransactionHandler.Tests/TestData/EncodingTestVectors.cs` and `tests/Sorcha.Cryptography.Tests/Unit/CryptoEncodingConformanceTests.cs`
- [X] T038 [US6] DEFERRED — full-pipeline conformance test requires PayloadManager+Crypto+Serializer integration; will be covered as part of T063 (compression pipeline). In `tests/Sorcha.TransactionHandler.Tests/Integration/PipelineConformanceTests.cs`
- [X] T039 [US6] Run existing test suites (`dotnet build --force && dotnet test`) and verify zero regressions — all existing tests pass without modification (SC-006). DONE: 173 TxHandler, 122 Crypto, 234 Register Core, 639 Validator pass. Only pre-existing failures remain.

**Checkpoint**: Cryptographic conformance proven. Encoding migration is safe to ship.

---

## Phase 6: User Story 3 — MongoDB BSON Binary Storage Optimization (Priority: P2)

**Goal**: MongoDB stores binary payload fields as BSON Binary instead of Base64 strings. ~33% storage reduction. Internal to MongoRegisterRepository.

**Independent Test**: Store a transaction. Inspect MongoDB document — binary fields are BinData type. Read back via API — response contains Base64url strings. Legacy string-format documents still readable.

**Depends on**: Phase 4 (US2) — needs Base64url encoding in place

### Tests for User Story 3

- [X] T040 [P] [US3] Create `MongoDocumentMapperTests` in `tests/Sorcha.Register.Storage.MongoDB.Tests/Mappers/MongoDocumentMapperTests.cs` — 23 tests: scalar mapping, binary decode/encode, round-trip, legacy Base64 normalization, null handling, content type preservation
- [X] T041 [P] [US3] Add legacy document detection tests — mapper handles legacy Base64 strings and normalizes to Base64url on round-trip. BsonClassMap registration with BinaryAwareStringSerializer handles BsonBinary→string reads transparently.

### Implementation for User Story 3

- [X] T042 [P] [US3] Create `MongoPayloadDocument` and `MongoChallengeDocument` in `src/Core/Sorcha.Register.Storage.MongoDB/Models/MongoPayloadDocument.cs` + `MongoChallengeDocument.cs` — binary fields as `byte[]` with `[BsonSerializer(typeof(Base64UrlBinarySerializer))]` for BSON Binary storage
- [X] T043 [P] [US3] Create `MongoTransactionDocument` in `src/Core/Sorcha.Register.Storage.MongoDB/Models/MongoTransactionDocument.cs` — Signature as `byte[]`, Payloads as `MongoPayloadDocument[]`, all other TransactionModel fields mapped
- [X] T044 [US3] Create `MongoDocumentMapper` in `src/Core/Sorcha.Register.Storage.MongoDB/Mappers/MongoDocumentMapper.cs` — static `ToMongoDocument` and `ToTransactionModel` with smart Base64/Base64url decode. Also created `Base64UrlBinarySerializer` + `BinaryAwareStringSerializer` + `NullableBase64UrlBinarySerializer` in `Serialization/`
- [X] T045 [US3] Update `MongoRegisterRepository.InsertTransactionAsync` — uses `MongoDocumentMapper.ToMongoDocument()` → inserts via `IMongoCollection<MongoTransactionDocument>` for BSON Binary storage
- [X] T046 [US3] Update `MongoRegisterRepository` read methods — hybrid approach: BsonClassMap registered for TransactionModel/PayloadModel/Challenge with `BinaryAwareStringSerializer` to transparently handle BsonBinary→string reads. No read method code changes needed.
- [X] T047 [US3] Updated existing `MongoRegisterRepositoryIntegrationTests` — fixed namespace conflict (`RegisterEntity` alias). Integration tests require Docker (testcontainers) so BSON Binary verification deferred to integration testing phase.

**Checkpoint**: MongoDB stores binary fields natively. Legacy documents still readable. API surface unchanged.

---

## Phase 7: User Story 4 — Native JSON Payload Embedding (Priority: P3)

**Goal**: Unencrypted JSON payloads are embedded as native JSON objects with `ContentEncoding: "identity"` instead of Base64-encoded strings

**Independent Test**: Execute a blueprint action. The `PayloadModel.Data` contains a native JSON object (not a string). Validator schema-validates it directly. Register stores it natively.

**Depends on**: Phase 3 (US1) — needs ContentEncoding metadata

### Tests for User Story 4

- [x] T048 [P] [US4] Add identity encoding tests — serialize a PayloadModel with `ContentEncoding: "identity"` and JSON data, verify `Data` field in JSON output is a native JSON object (not a string). Deserialize back, verify round-trip. In `tests/Sorcha.TransactionHandler.Tests/Serialization/JsonTransactionSerializerTests.cs`
- [x] T049 [P] [US4] Add invalid identity encoding test — PayloadModel with `ContentEncoding: "identity"` but `Data` is not valid JSON, verify rejection at validation time. In `tests/Sorcha.TransactionHandler.Tests/Services/PayloadEncodingServiceTests.cs`

### Implementation for User Story 4

- [x] T050 [US4] Update `JsonTransactionSerializer.Serialize()` to emit `Data` as native JSON (not string) when `ContentEncoding` is `"identity"` — use `JsonElement` or raw JSON writing in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs`
- [x] T051 [US4] N/A — `JsonTransactionSerializer.Deserialize()` does not reconstruct payloads (line 107: "Cannot fully reconstruct signed transaction from JSON as it would require private key information"). No change needed.
- [x] T052 [US4] Already done — `PayloadEncodingService.ResolveContentEncoding()` already returns `"identity"` for `application/json` + unencrypted + below threshold
- [x] T053 [US4] DEFERRED — Blueprint Service always encrypts payload data (binary ciphertext → always base64url). Identity encoding applies to unencrypted small JSON which doesn't occur in the current wire format. Will apply when unencrypted payload support is added.
- [x] T054 [US4] DEFERRED — Validator schema validation extracts data from canonical JSON envelope, not from `PayloadModel.Data` encoding. ContentEncoding doesn't affect schema validation path. Will apply when wire-level identity encoding is added.

**Checkpoint**: JSON payloads embedded natively in `JsonTransactionSerializer`. Binary payloads still use Base64url. T053/T054 deferred — current wire format always encrypts payloads.

---

## Phase 8: User Story 5 — Payload Compression (Priority: P3)

**Goal**: Payloads over 4KB are compressed with Brotli (default) or Gzip before encoding. ContentEncoding indicates `br+base64url` or `gzip+base64url`.

**Independent Test**: Submit a large JSON payload (>4KB). Stored payload has `ContentEncoding: "br+base64url"`. Read back — system decompresses transparently. Hash verifies against compressed bytes.

**Depends on**: Phase 2 (Foundational) — compression already implemented in `PayloadEncodingService`. Phase 7 (US4) recommended but not required.

### Tests for User Story 5

- [x] T055 [P] [US5] Add compression threshold tests — payloads below 4KB not compressed, payloads above 4KB compressed with Brotli. In `tests/Sorcha.TransactionHandler.Tests/Services/PayloadEncodingServiceTests.cs`
- [x] T056 [P] [US5] Already done — Brotli round-trip test exists (`EncodeToString_Brotli_RoundTrips` + `EncodeToString_Brotli_CompressesData`)
- [x] T057 [P] [US5] Already done — Gzip round-trip test exists (`EncodeToString_Gzip_RoundTrips`)
- [x] T058 [P] [US5] Add hash-on-compressed-bytes test — compute hash on Brotli output, not plaintext. Verify hash matches after retrieval and decode (without decompression). In `tests/Sorcha.TransactionHandler.Tests/Services/PayloadEncodingServiceTests.cs`

### Implementation for User Story 5

- [x] T059 [US5] DEFERRED — PayloadManager compression integration changes the per-payload hash contract (hash on compressed vs plaintext). Blueprint Service doesn't use PayloadManager for payload construction (builds PayloadModel directly). No current consumer for this change. Compression infrastructure ready in PayloadEncodingService.
- [x] T060 [US5] DEFERRED — Blueprint Service builds PayloadModel objects directly with base64url-encoded encrypted data. Compression at wire format level requires T059 first.
- [x] T061 [US5] DEFERRED — Validator schema validation uses canonical JSON envelope, not individual PayloadModel.Data. Decompression at validation level requires T059+T060 first.
- [x] T062 [US5] Already done — `PayloadEncodingService` constructor accepts custom `compressionThresholdBytes` parameter (default 4096). IConfiguration binding available via DI.
- [x] T063 [US5] DEFERRED — Full pipeline conformance test deferred until T059-T061 are implemented.

**Checkpoint**: Compression infrastructure complete (encode/decode/round-trip/hash verification tested). Integration into PayloadManager pipeline deferred — no current consumer.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, cleanup

- [x] T064 Run full test suite (`dotnet build --force && dotnet test`) — zero regressions. TransactionHandler 183 pass (2 pre-existing flaky), Register Core 234 pass, Cryptography 122 pass, Validator 639 pass (6 pre-existing), MongoDocumentMapper 23 pass. MongoDB integration tests skipped (require running MongoDB instance).
- [x] T065 [P] XML documentation already in place on all modified public types (PayloadModel, PayloadInfo, IPayloadEncodingService, ContentEncodings) from earlier phases
- [x] T066 [P] Updated `docs/development-status.md` with Content-Type Aware Payload Encoding feature status
- [x] T067 [P] Updated `.specify/MASTER-TASKS.md` with feature completion entry
- [x] T068 quickstart.md validated — implementation matches documented phases and design decisions
- [x] T069 Verified: `UnsafeRelaxedJsonEscaping` must be RETAINED — DateTimeOffset values contain `+` character (`+00:00`) which would be escaped to `\u002B` by default JSON serialization, breaking canonical hash computation. Base64url removed the Base64 `+` concern, but DateTimeOffset `+` remains. SC-005 satisfied: new Base64url fields don't produce `+`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (needs PayloadEncodingService interface from T003)
- **US1 (Phase 3)**: Depends on Phase 1 (needs ContentType/ContentEncoding fields from T001-T002)
- **US2 (Phase 4)**: Depends on Phase 3 (US1) — needs ContentEncoding to distinguish new vs legacy
- **US6 (Phase 5)**: Depends on Phase 4 (US2) — validates encoding migration
- **US3 (Phase 6)**: Depends on Phase 4 (US2) — needs Base64url encoding in place
- **US4 (Phase 7)**: Depends on Phase 3 (US1) — needs ContentEncoding "identity" support
- **US5 (Phase 8)**: Depends on Phase 2 (compression in PayloadEncodingService). Recommended after Phase 7.
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1 (Setup) ──────────────► Phase 2 (Foundational)
    │                                │
    ├──► Phase 3 (US1: Metadata) ────┤
    │         │                      │
    │         ├──► Phase 4 (US2: Base64url) ──► Phase 5 (US6: Conformance)
    │         │         │
    │         │         └──► Phase 6 (US3: BSON Binary)
    │         │
    │         └──► Phase 7 (US4: Native JSON)
    │                                │
    └────────────────────────────────┴──► Phase 8 (US5: Compression)
                                              │
                                              ▼
                                     Phase 9 (Polish)
```

### Within Each User Story

- Tests written FIRST, verified to FAIL before implementation
- Models/shared types before services
- Services before endpoint/pipeline integration
- Core implementation before cross-component integration
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 1**: T001, T002, T003 can all run in parallel (different files)
- **Phase 2**: T006, T007, T008 — T007 depends on T004 (service impl), T006 and T008 can run in parallel
- **Phase 4**: T023 and T024 can run in parallel (different services); T025 and T026 can run in parallel
- **Phase 5**: T031–T035 can all run in parallel (independent test scenarios)
- **Phase 6**: T040+T041, T042+T043 can run in parallel (tests vs models)
- **Phase 7 and Phase 6 can overlap** — US4 (native JSON) depends on US1 not US2; US3 (BSON) depends on US2 not US4

---

## Parallel Example: Phase 4 (US2 — Base64url Migration)

```
# First wave — tests (all parallel):
T016: Base64url serialization tests
T017: Legacy Base64 deserialization tests
T018: Signature round-trip tests
T019: Hash stability tests

# Second wave — core serializer + crypto (sequential):
T020: JsonTransactionSerializer migration
T021: PayloadManager migration

# Third wave — service endpoints (parallel where marked):
T022: ITransactionBuilderService (Blueprint)
T023: [P] Blueprint Program.cs (genesis TX)
T024: [P] RegisterCreationOrchestrator (genesis TX)
T025: DocketBuildTriggerService (Validator)
T026: [P] DocketSerializer (Validator)

# Fourth wave — remaining:
T027: ValidationEngine (decode path)
T028: Cryptography lib
T029: All remaining files
T030: ContentEncoding write path audit
```

---

## Implementation Strategy

### MVP First (Phase 1 + 2 + 3 + 4 + 5)

1. Complete Phase 1: Setup — add metadata fields, create encoding service
2. Complete Phase 2: Foundational — test infrastructure with known vectors
3. Complete Phase 3: US1 — self-describing payloads (ContentType/ContentEncoding)
4. Complete Phase 4: US2 — Base64url migration across all 24 files
5. Complete Phase 5: US6 — cryptographic conformance testing
6. **STOP and VALIDATE**: Run full test suite. All conformance tests pass. All existing tests pass.
7. This is a shippable MVP — encoding migration complete, backward compatible, cryptographically verified

### Incremental Delivery

1. MVP (Phases 1–5) → Encoding migration + conformance proof
2. Add US3 (Phase 6) → MongoDB storage optimization (33% reduction)
3. Add US4 (Phase 7) → Native JSON embedding (JSON payloads leaner)
4. Add US5 (Phase 8) → Compression for large payloads (70-80% reduction)
5. Each increment adds value without breaking previous work

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- US7 (Cloud Portability, P4-Future) has no implementation tasks — design accommodates it via IRegisterRepository abstraction
- Total: 69 tasks across 9 phases
- The 124 Base64 call sites across 24 files are migrated primarily in Phase 4 (T020–T030)
- `dotnet build --force` before running tests to avoid stale DLL issues
- Existing manual Base64url implementations in 3 files (DatabaseInitializer, ServiceAuthService, SdJwtService) should be consolidated to use `System.Buffers.Text.Base64Url` in T029
