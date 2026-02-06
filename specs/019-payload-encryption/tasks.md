# Tasks: Payload Encryption for DAD Security Model

**Input**: Design documents from `/specs/019-payload-encryption/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included per SC-007 (>85% unit test coverage requirement in spec).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/Common/Sorcha.TransactionHandler/` (primary), `src/Services/Sorcha.Blueprint.Service/` (call sites)
- **Tests**: `tests/Sorcha.TransactionHandler.Tests/`
- **Crypto**: `src/Common/Sorcha.Cryptography/` (read-only dependency — no changes)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add new types and update constructor signatures before any encryption logic

- [x] T001 Add `RecipientKeyInfo` and `DecryptionKeyInfo` record types to `src/Common/Sorcha.TransactionHandler/Interfaces/IPayloadManager.cs` — `RecipientKeyInfo(string WalletAddress, byte[] PublicKey, WalletNetworks Network)` and `DecryptionKeyInfo(string WalletAddress, byte[] PrivateKey, WalletNetworks Network)` as public records
- [x] T002 Add `EncryptionType` property to internal `Payload` class in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — stores which algorithm was used for this payload's encryption
- [x] T003 Update `PayloadManager` constructor to accept `ISymmetricCrypto`, `ICryptoModule`, `IHashProvider` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — add private readonly fields, null-check all three parameters. Add thread-safety lock object `private readonly object _lock = new()`
- [x] T004 Add new method overloads to `IPayloadManager` interface in `src/Common/Sorcha.TransactionHandler/Interfaces/IPayloadManager.cs` — add `AddPayloadAsync(byte[] data, RecipientKeyInfo[] recipients, ...)`, `GetPayloadDataAsync(uint payloadId, DecryptionKeyInfo keyInfo, ...)`, `GrantAccessAsync(uint payloadId, RecipientKeyInfo newRecipient, DecryptionKeyInfo ownerKeyInfo)`, `VerifyPayloadAsync(uint payloadId, DecryptionKeyInfo keyInfo)`

**Checkpoint**: New types and signatures defined. Existing code still compiles (old overloads preserved).

---

## Phase 2: Foundational (Call Site Updates)

**Purpose**: Update all 10 `new PayloadManager()` call sites to pass crypto dependencies. MUST complete before user story implementation since PayloadManager constructor will require parameters.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Update `TransactionBuilder.Create()` in `src/Common/Sorcha.TransactionHandler/Core/TransactionBuilder.cs` — pass existing `_cryptoModule` and `_hashProvider` plus add `ISymmetricCrypto` field (inject via constructor or resolve) to `new PayloadManager(symmetricCrypto, _cryptoModule, _hashProvider)`
- [x] T006 [P] Update `JsonTransactionSerializer` in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs` — accept crypto dependencies via constructor, pass to `new PayloadManager(...)`
- [x] T007 [P] Update `BinaryTransactionSerializer` in `src/Common/Sorcha.TransactionHandler/Serialization/BinaryTransactionSerializer.cs` — accept crypto dependencies via constructor, pass to `new PayloadManager(...)`
- [x] T008 [P] Update `TransactionFactory` (4 call sites) in `src/Common/Sorcha.TransactionHandler/Versioning/TransactionFactory.cs` — accept crypto dependencies via constructor, pass to all `new PayloadManager(...)` calls
- [x] T009 Update `TransactionBuilderService` (3 call sites) in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/TransactionBuilderService.cs` — pass existing crypto dependencies to `new PayloadManager(...)` calls
- [x] T010 Build verification — run `dotnet build src/Common/Sorcha.TransactionHandler/Sorcha.TransactionHandler.csproj` and `dotnet build src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj` to confirm all call sites compile

**Checkpoint**: Foundation ready — all call sites pass crypto dependencies. Old string-based overloads still functional. User story implementation can now begin.

---

## Phase 3: User Story 1 — Encrypted Payload Storage (Priority: P1) 🎯 MVP

**Goal**: Replace `AddPayloadAsync` stub with real encryption — symmetric encryption of data, random IV generation, SHA-256 hashing, per-recipient key wrapping, key zeroization.

**Independent Test**: Submit a payload through PayloadManager, verify stored data differs from plaintext, IV is non-zero and correct size, hash is valid SHA-256 digest, each recipient has unique encrypted key.

### Tests for User Story 1

- [x] T011 [P] [US1] Create test file `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` with test helper methods — generate ED25519 key pairs via `CryptoModule.GenerateKeySetAsync()`, create `RecipientKeyInfo` from generated keys, build a `PayloadManager` instance with real `SymmetricCrypto`, `CryptoModule`, `HashProvider`
- [x] T012 [P] [US1] Write encryption tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `AddPayloadAsync_WithRecipients_EncryptsData` (stored data differs from input), `AddPayloadAsync_WithRecipients_GeneratesNonZeroIV` (IV is correct size and non-zero), `AddPayloadAsync_WithRecipients_ComputesSHA256Hash` (hash is 32 bytes, matches `HashProvider.ComputeHash(data)`), `AddPayloadAsync_WithTwoRecipients_ProducesUniqueEncryptedKeys` (Alice and Bob have different encrypted keys)
- [x] T013 [P] [US1] Write non-determinism test in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `AddPayloadAsync_SameDataTwice_ProducesDifferentCiphertext` (encrypt same data twice, verify ciphertexts differ due to random IV)
- [x] T014 [P] [US1] Write validation tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `AddPayloadAsync_EmptyData_ReturnsInvalidPayload`, `AddPayloadAsync_NullData_ReturnsInvalidPayload`, `AddPayloadAsync_NoRecipients_ReturnsInvalidRecipients`

### Implementation for User Story 1

- [x] T015 [US1] Implement `AddPayloadAsync(byte[] data, RecipientKeyInfo[] recipients, PayloadOptions?, CancellationToken)` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — (1) validate data non-null/non-empty, (2) validate recipients non-empty, (3) compute hash via `_hashProvider.ComputeHash(data, options.HashType)`, (4) generate symmetric key via `_symmetricCrypto.GenerateKey(options.EncryptionType)`, (5) encrypt data via `_symmetricCrypto.EncryptAsync(data, options.EncryptionType, key)`, (6) for each recipient encrypt key via `_cryptoModule.EncryptAsync(key, recipient.Network, recipient.PublicKey)`, (7) zeroize key with `Array.Clear()`, (8) store Payload with ciphertext.Data, ciphertext.IV, hash, encryptedKeys, encryptionType, (9) use lock for thread safety on `_payloads` and `_nextPayloadId`
- [x] T016 [US1] Update `PayloadInfo.GetInfo()` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — use actual `EncryptionType` field from Payload instead of hardcoded `XCHACHA20_POLY1305`, set `IsEncrypted` based on whether IV is non-zero
- [x] T017 [US1] Wire the old `AddPayloadAsync(byte[] data, string[] recipientWallets, ...)` overload to call the new one or mark as obsolete in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — for backward compatibility, the string-based overload should still work (can create dummy RecipientKeyInfo with placeholder keys for callers not yet upgraded, or throw NotSupportedException with clear message)
- [x] T018 [US1] Run US1 tests — `dotnet test tests/Sorcha.TransactionHandler.Tests/ --filter "FullyQualifiedName~PayloadManagerTests"` — all encryption tests from T012-T014 must pass

**Checkpoint**: All new payloads are encrypted with real cryptography. IV is random, hash is real, keys are wrapped per-recipient. MVP encryption is functional.

---

## Phase 4: User Story 2 — Authorized Payload Decryption (Priority: P1)

**Goal**: Replace `GetPayloadDataAsync` stub with real decryption — unwrap symmetric key with recipient's private key, decrypt data, verify round-trip fidelity.

**Independent Test**: Encrypt a payload for a recipient (using US1), then decrypt using recipient's private key and verify byte-for-byte match with original plaintext.

### Tests for User Story 2

- [x] T019 [P] [US2] Write decryption round-trip tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `GetPayloadDataAsync_AuthorizedRecipient_ReturnsOriginalPlaintext` (encrypt then decrypt, byte-for-byte match), `GetPayloadDataAsync_MultipleRecipients_EachCanDecrypt` (encrypt for Alice+Bob, both decrypt successfully to same plaintext)
- [x] T020 [P] [US2] Write access control tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `GetPayloadDataAsync_UnauthorizedWallet_ReturnsAccessDenied` (wallet not in EncryptedKeys), `GetPayloadDataAsync_WrongPrivateKey_ReturnsDecryptionFailed` (valid wallet but wrong key), `GetPayloadDataAsync_InvalidPayloadId_ReturnsInvalidPayload`

### Implementation for User Story 2

- [x] T021 [US2] Implement `GetPayloadDataAsync(uint payloadId, DecryptionKeyInfo keyInfo, CancellationToken)` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — (1) find payload by ID, (2) check `keyInfo.WalletAddress` in `EncryptedKeys` → `AccessDenied` if missing, (3) legacy check: if `IV.All(b => b == 0)` return raw Data, (4) decrypt symmetric key via `_cryptoModule.DecryptAsync(encryptedKey, keyInfo.Network, keyInfo.PrivateKey)`, (5) construct `SymmetricCiphertext` with Data, decrypted key, IV, EncryptionType, (6) decrypt via `_symmetricCrypto.DecryptAsync(ciphertext)`, (7) zeroize symmetric key, (8) return plaintext
- [x] T022 [US2] Wire old `GetPayloadDataAsync(uint payloadId, string wifPrivateKey, CancellationToken)` overload in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — mark as obsolete or delegate to new signature
- [x] T023 [US2] Run US2 tests — `dotnet test tests/Sorcha.TransactionHandler.Tests/ --filter "FullyQualifiedName~PayloadManagerTests"` — all decryption and access control tests from T019-T020 must pass

**Checkpoint**: Full encrypt/decrypt round-trip works. Authorized recipients can recover original data. Unauthorized access is rejected.

---

## Phase 5: User Story 3 — Payload Integrity Verification (Priority: P2)

**Goal**: Replace `VerifyPayloadAsync` and `VerifyAllAsync` stubs with real hash-based integrity verification using constant-time comparison.

**Independent Test**: Encrypt and store a payload, verify it passes. Tamper with stored encrypted data, verify it fails.

### Tests for User Story 3

- [x] T024 [P] [US3] Write verification tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `VerifyPayloadAsync_UntamperedPayload_ReturnsTrue` (with DecryptionKeyInfo overload), `VerifyPayloadAsync_TamperedData_ReturnsFalse` (modify payload.Data byte, verify fails), `VerifyPayloadAsync_NonExistentPayload_ReturnsFalse`, `VerifyAllAsync_AllValid_ReturnsTrue`, `VerifyAllAsync_OneTampered_ReturnsFalse`

### Implementation for User Story 3

- [x] T025 [US3] Implement `VerifyPayloadAsync(uint payloadId, DecryptionKeyInfo keyInfo)` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — (1) decrypt payload via `GetPayloadDataAsync`, (2) compute hash of decrypted data via `_hashProvider.ComputeHash(plaintext, HashType.SHA256)`, (3) compare with stored `Hash` using `System.Security.Cryptography.CryptographicOperations.FixedTimeEquals()` for constant-time comparison, (4) return match result
- [x] T026 [US3] Update `VerifyPayloadAsync(uint payloadId)` (no-key overload) in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — check if `Hash.All(b => b == 0)` → return true (legacy), otherwise return true only if hash field is non-zero (structural check without decryption)
- [x] T027 [US3] Update `VerifyAllAsync()` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — iterate all payloads, call `VerifyPayloadAsync(id)` for each, return false if any fails. Note: without decryption keys, this performs structural verification only (non-zero hash for encrypted payloads, skip for legacy)
- [x] T028 [US3] Run US3 tests — `dotnet test tests/Sorcha.TransactionHandler.Tests/ --filter "FullyQualifiedName~PayloadManagerTests"` — all verification tests from T024 must pass

**Checkpoint**: Integrity verification detects tampered payloads. Constant-time comparison prevents timing side-channels.

---

## Phase 6: User Story 4 — Recipient Access Grant After Encryption (Priority: P2)

**Goal**: Replace `GrantAccessAsync` stub with real key re-encryption — decrypt owner's copy of symmetric key, re-encrypt for new recipient.

**Independent Test**: Encrypt for Alice, grant Bob access using Alice's key, verify Bob can decrypt and gets same plaintext.

### Tests for User Story 4

- [x] T029 [P] [US4] Write access grant tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `GrantAccessAsync_NewRecipient_CanDecryptPayload` (Alice grants Bob, Bob decrypts successfully), `GrantAccessAsync_ExistingRecipient_ReturnsSuccessNoChange` (grant to already-authorized wallet is idempotent), `GrantAccessAsync_InvalidPayloadId_ReturnsInvalidPayload`, `GrantAccessAsync_OwnerNotAuthorized_ReturnsAccessDenied`

### Implementation for User Story 4

- [x] T030 [US4] Implement `GrantAccessAsync(uint payloadId, RecipientKeyInfo newRecipient, DecryptionKeyInfo ownerKeyInfo)` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — (1) find payload, (2) check newRecipient already in EncryptedKeys → return Success if so, (3) verify owner has access, (4) decrypt owner's symmetric key via `_cryptoModule.DecryptAsync(encryptedKey, ownerKeyInfo.Network, ownerKeyInfo.PrivateKey)`, (5) encrypt symmetric key for new recipient via `_cryptoModule.EncryptAsync(key, newRecipient.Network, newRecipient.PublicKey)`, (6) add to EncryptedKeys, (7) zeroize symmetric key, (8) use lock for thread safety
- [x] T031 [US4] Wire old `GrantAccessAsync(uint payloadId, string recipientWallet, string ownerWifKey)` overload in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — mark as obsolete or delegate
- [x] T032 [US4] Run US4 tests — `dotnet test tests/Sorcha.TransactionHandler.Tests/ --filter "FullyQualifiedName~PayloadManagerTests"` — all access grant tests from T029 must pass

**Checkpoint**: Dynamic access control works. New recipients can be added without re-encrypting the entire payload.

---

## Phase 7: User Story 5 — Backward-Compatible Upgrade (Priority: P3)

**Goal**: Ensure legacy payloads (zeroed IV, zeroed hash) coexist with encrypted payloads. Legacy payloads return raw data, encrypted payloads go through full decryption.

**Independent Test**: Create a legacy payload (zeroed IV), verify it reads back raw data. Create encrypted payload, verify full decryption. Both coexist in same PayloadManager.

### Tests for User Story 5

- [x] T033 [P] [US5] Write backward compatibility tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `GetPayloadDataAsync_LegacyZeroIV_ReturnsRawData` (manually create payload with zeroed IV, decrypt returns raw bytes), `VerifyPayloadAsync_LegacyZeroHash_ReturnsTrue` (legacy payload with zeroed hash passes verification), `VerifyAllAsync_MixedLegacyAndEncrypted_HandlesCorrectly` (legacy passes, encrypted verified properly)

### Implementation for User Story 5

- [x] T034 [US5] Add `IsLegacy()` helper method to `Payload` class in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — returns `true` if `IV.All(b => b == 0)`, encapsulates legacy detection logic used by `GetPayloadDataAsync`, `VerifyPayloadAsync`, and `VerifyAllAsync`
- [x] T035 [US5] Verify legacy detection is properly integrated in `GetPayloadDataAsync`, `VerifyPayloadAsync`, and `VerifyAllAsync` in `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — ensure all three methods call `IsLegacy()` and handle appropriately (US2/US3 implementations should already have this, but verify and refactor if needed)
- [x] T036 [US5] Run US5 tests — `dotnet test tests/Sorcha.TransactionHandler.Tests/ --filter "FullyQualifiedName~PayloadManagerTests"` — all backward compatibility tests from T033 must pass

**Checkpoint**: Legacy and encrypted payloads coexist. No existing workflows break.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, documentation, test coverage, and full regression

- [x] T037 [P] Write edge case tests in `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` — `AddPayloadAsync_InvalidPublicKey_ReturnsFailure` (malformed key), `AddPayloadAsync_WithAesGcmAlgorithm_EncryptsSuccessfully` (verify AES-GCM alternative works), `GetPayloadDataAsync_ConcurrentAccess_IsThreadSafe` (parallel decrypt calls)
- [x] T038 [P] Add XML documentation to all public methods and types in `src/Common/Sorcha.TransactionHandler/Interfaces/IPayloadManager.cs` and `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` — document parameters, return values, exceptions, security notes (key zeroization, constant-time comparison)
- [x] T039 Run full test suite — `dotnet test` across all projects to verify no regressions in TransactionHandler, Blueprint Service, or other consumers
- [x] T040 Run quickstart.md validation — execute smoke test from `specs/019-payload-encryption/quickstart.md` to verify end-to-end encryption workflow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — encryption must work before decryption
- **US2 (Phase 4)**: Depends on US1 (needs encrypted payloads to decrypt)
- **US3 (Phase 5)**: Depends on US2 (verification needs decryption to compute hash)
- **US4 (Phase 6)**: Depends on US1 and US2 (needs encryption + decryption for key re-wrap)
- **US5 (Phase 7)**: Depends on US2 and US3 (needs both decrypt and verify to test legacy paths)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1) — Encrypt**: Foundation only. MVP scope.
- **US2 (P1) — Decrypt**: Requires US1 (needs encrypted payloads)
- **US3 (P2) — Verify**: Requires US2 (needs decryption to compute hash for comparison)
- **US4 (P2) — Grant Access**: Requires US1 + US2 (needs encrypt + decrypt for key re-wrap)
- **US5 (P3) — Backward Compat**: Requires US2 + US3 (needs decrypt + verify with legacy check)

### Within Each User Story

- Tests written first (T011-T014, T019-T020, T024, T029, T033)
- Implementation follows (T015-T017, T021-T022, T025-T027, T030-T031, T034-T035)
- Verification run last (T018, T023, T028, T032, T036)

### Parallel Opportunities

- **Phase 1**: T001-T004 are sequential (same files)
- **Phase 2**: T006, T007, T008 can run in parallel (different files); T005 and T009 are separate files
- **Within US1**: T011, T012, T013, T014 can all run in parallel (same new test file, different test methods)
- **Within US2**: T019, T020 in parallel
- **Phase 8**: T037, T038 in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all test writing tasks in parallel (all in same new file, different methods):
Task: T011 "Create PayloadManagerTests.cs with test helpers"
Task: T012 "Write encryption assertion tests"
Task: T013 "Write non-determinism test"
Task: T014 "Write validation tests"

# Then implementation (sequential — same file):
Task: T015 "Implement AddPayloadAsync with real encryption"
Task: T016 "Update PayloadInfo.GetInfo()"
Task: T017 "Wire old overload for backward compat"

# Then verify:
Task: T018 "Run US1 tests"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (types + constructor)
2. Complete Phase 2: Foundational (all 10 call sites)
3. Complete Phase 3: User Story 1 (encryption)
4. **STOP and VALIDATE**: Verify all new payloads are encrypted, IVs are random, hashes are real
5. This alone eliminates the #1 critical stub in the codebase

### Incremental Delivery

1. Setup + Foundational → All call sites compile with crypto deps
2. US1 (Encrypt) → Payloads encrypted on storage (MVP!)
3. US2 (Decrypt) → Full encrypt/decrypt round-trip
4. US3 (Verify) → Integrity verification functional
5. US4 (Grant Access) → Dynamic access control
6. US5 (Backward Compat) → Legacy payload support
7. Each story adds DAD security capability without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Tests use real crypto implementations (not mocks) for integration-level confidence
- Key zeroization (FR-013) is implemented in T015, T021, T030 — verify with each
- Constant-time comparison (FR-007 clarification) is implemented in T025
- Thread safety (lock object) added in T003, used in T015, T030
- Total: 40 tasks across 8 phases
