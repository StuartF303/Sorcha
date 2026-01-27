# Tasks: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Input**: Design documents from `/specs/015-fix-register-crypto/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit tests are included per constitution requirement (IV. Testing Requirements: >85% for new code).

**Organization**: Tasks grouped by user story. US1/US2/US3 are co-equal P1 priorities and form the foundational layer together. US4/US5 are P2 cleanup. US6 is P3 verification.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New shared components that multiple user stories depend on

- [ ] T001 [P] Create `IServiceAuthClient` interface for service-to-service JWT token acquisition in `src/Common/Sorcha.ServiceClients/Auth/IServiceAuthClient.cs`. Interface should expose `Task<string?> GetTokenAsync(CancellationToken ct)`. See contracts/wallet-sign-api.md for token caching requirements.

- [ ] T002 [P] Implement `ServiceAuthClient` with OAuth2 `client_credentials` grant in `src/Common/Sorcha.ServiceClients/Auth/ServiceAuthClient.cs`. Inject `HttpClient` and `IConfiguration`. Call Tenant Service `POST /api/service-auth/token` with form-urlencoded body (`grant_type=client_credentials`, `client_id`, `client_secret`). Cache token in-memory with `SemaphoreSlim` for thread-safe refresh. Refresh when within 5 minutes of expiry. Configure via `ServiceAuth:ClientId`, `ServiceAuth:ClientSecret`. Use Aspire service discovery for Tenant Service URL (`http://tenant-service`).

- [ ] T003 [P] Add `WalletSignResult` record to `src/Common/Sorcha.ServiceClients/Wallet/IWalletServiceClient.cs`. Fields: `Signature` (byte[]), `PublicKey` (byte[]), `SignedBy` (string), `Algorithm` (string). Update `SignTransactionAsync` return type from `Task<string>` to `Task<WalletSignResult>`. Add `bool isPreHashed = false` parameter. Update `SignDataAsync` similarly.

- [ ] T004 [P] Add `AttestationHashes` dictionary to `PendingRegistration` class in `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`. Type: `Dictionary<string, byte[]>` keyed by `"{role}:{subject}"`. Initialize to empty dictionary in constructor. Update `AttestationToSign.DataToSign` XML doc comment from "SHA-256 hash" to "hex-encoded SHA-256 hash of the canonical JSON attestation data".

- [ ] T005 Register `ServiceAuthClient` and `HttpClient` for `WalletServiceClient` in `src/Common/Sorcha.ServiceClients/Extensions/ServiceCollectionExtensions.cs`. Add `services.AddHttpClient<WalletServiceClient>()`, `services.AddSingleton<IServiceAuthClient, ServiceAuthClient>()`. Ensure `ServiceAuthClient` gets its own `HttpClient` via `services.AddHttpClient<ServiceAuthClient>()`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core changes to Wallet Service signing that ALL user stories depend on

**CRITICAL**: US1, US2, and US3 all depend on Phase 2 completion

- [ ] T006 [P] Add `IsPreHashed` property to `SignTransactionRequest` DTO in `src/Services/Sorcha.Wallet.Service/Models/SignTransactionRequest.cs`. Type: `public bool IsPreHashed { get; set; } = false`. Add XML doc explaining the pre-hashed behavior.

- [ ] T007 Add `bool isPreHashed = false` parameter to `ITransactionService.SignTransactionAsync` in `src/Common/Sorcha.Wallet.Core/Services/Interfaces/ITransactionService.cs`. Add same parameter to `VerifySignatureAsync`.

- [ ] T008 Implement conditional hash skip in `TransactionService.SignTransactionAsync` in `src/Common/Sorcha.Wallet.Core/Services/Implementation/TransactionService.cs`. Change line ~50 from unconditional `_hashProvider.ComputeHash(transactionData, HashType.SHA256)` to: `var dataToSign = isPreHashed ? transactionData : _hashProvider.ComputeHash(transactionData, HashType.SHA256)`. Pass `dataToSign` to `_cryptoModule.SignAsync`. Apply same pattern to `VerifySignatureAsync`.

- [ ] T009 Add `bool isPreHashed = false` parameter to `WalletManager.SignTransactionAsync` in `src/Common/Sorcha.Wallet.Core/Services/Implementation/WalletManager.cs`. Pass the parameter through to `_transactionService.SignTransactionAsync` call (around line ~609).

- [ ] T010 Pass `request.IsPreHashed` through in wallet sign endpoint handler in `src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs`. In the `SignTransaction` handler, pass `request.IsPreHashed` to `walletManager.SignTransactionAsync` call (around line ~352-356).

- [ ] T011 De-stub `WalletServiceClient` with real HTTP calls in `src/Common/Sorcha.ServiceClients/Wallet/WalletServiceClient.cs`. Replace constructor to inject `HttpClient` and `IServiceAuthClient`. Implement `SignTransactionAsync`: (1) get JWT token via `_serviceAuth.GetTokenAsync()`, (2) set `Authorization: Bearer {token}` header, (3) POST to `/api/v1/wallets/{address}/sign` with JSON body `{ transactionData, derivationPath, isPreHashed }`, (4) parse `SignTransactionResponse` JSON, (5) return `WalletSignResult` with `Convert.FromBase64String` for signature and public key bytes. Implement `SignDataAsync` by delegating to `SignTransactionAsync` with `isPreHashed: true`. Remove all placeholder/stub signature generation code.

**Checkpoint**: Foundation ready - pre-hashed signing works, WalletServiceClient makes real HTTP calls with JWT auth

---

## Phase 3: User Story 3 - Pre-Hashed Signing Support (Priority: P1)

**Goal**: Wallet Service sign endpoint accepts `isPreHashed` flag and signs pre-computed hashes directly

**Independent Test**: Send a known SHA-256 hash to the wallet sign endpoint with `isPreHashed: true`, verify signature against original hash bytes using returned public key

### Tests for User Story 3

- [ ] T012 [P] [US3] Write unit tests for pre-hashed signing in `tests/Sorcha.Wallet.Core.Tests/`. Test `TransactionService.SignTransactionAsync` with `isPreHashed: true` for all three algorithms (ED25519, NISTP256, RSA4096): (1) when `isPreHashed=false`, data is hashed before signing (existing behavior); (2) when `isPreHashed=true`, data is passed directly to CryptoModule without hashing; (3) signature produced with `isPreHashed=true` can be verified against the original hash bytes. Use Moq to mock `IHashProvider` and verify `ComputeHash` is NOT called when `isPreHashed=true`.

### Implementation for User Story 3

Implementation completed in Phase 2 (T006-T010). This phase validates the implementation.

- [ ] T013 [US3] Verify pre-hashed signing end-to-end: build solution (`dotnet build`), run `Sorcha.Wallet.Core.Tests` (`dotnet test tests/Sorcha.Wallet.Core.Tests/`), ensure all existing tests still pass and new pre-hashed tests pass.

**Checkpoint**: Pre-hashed signing works for all three algorithms. Existing sign behavior unchanged when `isPreHashed=false`.

---

## Phase 4: User Story 2 - System Wallet Signing via Wallet Service (Priority: P1)

**Goal**: Validator Service calls real Wallet Service for system wallet signing instead of returning placeholders

**Independent Test**: Call Validator's genesis endpoint with a valid control record payload, verify the returned transaction contains real cryptographic signatures from the system wallet

### Tests for User Story 2

- [ ] T014 [P] [US2] Write unit tests for `ServiceAuthClient` in `tests/Sorcha.ServiceClients.Tests/` (create test file if project exists, else in nearest test project). Test: (1) `GetTokenAsync` calls Tenant Service with correct form data; (2) token is cached on subsequent calls; (3) token is refreshed when within 5 minutes of expiry; (4) thread-safe concurrent access (parallel calls don't duplicate token requests). Use mocked `HttpMessageHandler`.

- [ ] T015 [P] [US2] Write unit tests for de-stubbed `WalletServiceClient.SignTransactionAsync` in `tests/Sorcha.ServiceClients.Tests/`. Test: (1) makes HTTP POST to `/api/v1/wallets/{address}/sign` with correct JSON body including `isPreHashed`; (2) sets Bearer token from `IServiceAuthClient`; (3) parses response into `WalletSignResult` with correct byte arrays; (4) handles 404 (wallet not found) gracefully; (5) handles auth failure (401) by logging error.

### Implementation for User Story 2

- [ ] T016 [US2] Update `ValidationEndpoints.SubmitGenesisTransaction` in `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs` to use real wallet signing. Replace the stub system wallet signing (lines ~201-230): (1) call `walletClient.SignTransactionAsync(systemWalletAddress, controlRecordBytes, "sorcha:register-control", isPreHashed: true, ct)` which now returns `WalletSignResult`; (2) use `result.PublicKey` instead of `UTF8.GetBytes(systemWalletAddress)`; (3) use `result.Signature` instead of placeholder; (4) compute real `PayloadHash = Convert.ToHexString(hashProvider.ComputeHash(controlRecordBytes, HashType.SHA256))`. Add error handling: if system wallet not found, return 503 with message "Platform bootstrap incomplete: system wallet not available" and log at Error level.

- [ ] T017 [US2] Update `GenesisManager.CreateGenesisDocketAsync` in `src/Services/Sorcha.Validator.Service/Services/GenesisManager.cs` to use real wallet signing. Replace the stub signing (lines ~85-93): (1) call `_walletClient.SignTransactionAsync(systemWalletAddress, docketHashBytes, "sorcha:docket-signing", isPreHashed: true, ct)` which returns `WalletSignResult`; (2) use `result.PublicKey` and `result.Signature` as real byte arrays instead of `UTF8.GetBytes` placeholders. Update `IWalletServiceClient.SignDataAsync` callers to use `SignTransactionAsync` with `isPreHashed: true`.

- [ ] T018 [US2] Add ServiceAuth configuration to Validator Service in `src/Services/Sorcha.Validator.Service/Program.cs`. Ensure `AddServiceClients` is called (already is). Add ServiceAuth config section to `src/Services/Sorcha.Validator.Service/appsettings.json` (or appsettings.Development.json) with `ServiceAuth:ClientId` and `ServiceAuth:ClientSecret` placeholders. Add docker-compose environment variables for `ServiceAuth__ClientId` and `ServiceAuth__ClientSecret`.

**Checkpoint**: Validator Service produces real cryptographic signatures via the Wallet Service. Genesis transactions contain real public keys and signatures.

---

## Phase 5: User Story 1 - Register Creation with Cryptographic Attestation (Priority: P1)

**Goal**: End-to-end two-phase register creation with hash-based signing, stored-hash verification, and atomic register+genesis

**Independent Test**: Call initiate, sign returned hashes, call finalize, verify register and genesis transaction have real cryptographic signatures

**Dependencies**: Requires Phase 3 (pre-hashed signing) and Phase 4 (real system wallet signing) to be complete

### Tests for User Story 1

- [ ] T019 [P] [US1] Write unit tests for updated `RegisterCreationOrchestrator.InitiateAsync` in `tests/Sorcha.Register.Service.Tests/`. Test: (1) `DataToSign` is a hex-encoded SHA-256 hash (64 hex chars), not canonical JSON; (2) `PendingRegistration.AttestationHashes` contains an entry for each owner keyed by `"{role}:{subject}"`; (3) hash bytes stored match SHA-256 of the canonical JSON serialization.

- [ ] T020 [P] [US1] Write unit tests for updated `RegisterCreationOrchestrator.FinalizeAsync` in `tests/Sorcha.Register.Service.Tests/`. Test: (1) verification uses stored hash from `AttestationHashes`, not re-serialized data; (2) genesis is submitted BEFORE register is persisted (atomic ordering); (3) if genesis submission fails, register is NOT created in DB; (4) if genesis succeeds, register IS created in DB; (5) correct `WalletSignResult` values flow through to the genesis transaction.

### Implementation for User Story 1

- [ ] T021 [US1] Update `RegisterCreationOrchestrator.InitiateAsync` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`. Change `DataToSign` assignment (line ~121) from `canonicalJson` to `Convert.ToHexString(hashBytes).ToLowerInvariant()`. Store each hash in `PendingRegistration.AttestationHashes` keyed by `"{role}:{subject}"` (e.g., `"Owner:did:sorcha:user123"`). Ensure the hash bytes used for `DataToSign` and stored in `AttestationHashes` are identical.

- [ ] T022 [US1] Update `RegisterCreationOrchestrator.VerifyAttestationsAsync` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`. Replace the re-serialization+re-hashing logic (lines ~414-421) with: (1) build the key `"{role}:{subject}"` from `signedAttestation.AttestationData`; (2) look up stored hash bytes from the passed-in `attestationHashes` dictionary; (3) pass stored hash bytes directly to `_cryptoModule.VerifyAsync`. Add parameter `Dictionary<string, byte[]> attestationHashes` to the method signature. Remove the `_canonicalJsonOptions` usage and `_hashProvider.ComputeHash` call in the verification path.

- [ ] T023 [US1] Update `RegisterCreationOrchestrator.FinalizeAsync` for atomic register+genesis in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`. Reorder: (1) verify attestations (pass `pending.AttestationHashes`); (2) build control record; (3) create genesis transaction model; (4) compute real `PayloadHash = Convert.ToHexString(hashProvider.ComputeHash(controlRecordBytes, SHA256))`; (5) submit genesis to Validator via `_validatorClient.SubmitGenesisTransactionAsync`; (6) ONLY if genesis succeeds, call `_registerManager.CreateRegisterAsync`; (7) if genesis fails, throw appropriate error (do NOT persist register). Update `CreateGenesisTransaction` to compute real `PayloadHash` instead of `string.Empty`.

- [ ] T024 [US1] Run full test suite to verify no regressions: `dotnet test`. Fix any compilation errors from interface changes (T003 `WalletSignResult` return type, T007 `isPreHashed` parameter) in all callers across the solution.

**Checkpoint**: End-to-end register creation works with real crypto. Initiate returns hex hashes, finalize verifies from stored hashes, genesis has real signatures, register creation is atomic.

---

## Phase 6: User Story 4 - Removal of Non-Cryptographic Register Creation (Priority: P2)

**Goal**: Remove the simple CRUD POST endpoint that creates registers without crypto

**Independent Test**: POST to `/api/registers/` returns 404; GET/PUT/DELETE still work

- [ ] T025 [US4] Remove the `CreateRegister` POST handler from `src/Services/Sorcha.Register.Service/Program.cs` (lines ~431-461). Keep GET, PUT, DELETE, and stats endpoints in the `registersGroup`. Remove the `CreateRegisterRequest` record (lines ~936-940) if unused elsewhere. Search for any test files that call `POST /api/registers/` directly (not via initiate/finalize) and update or remove them.

- [ ] T026 [US4] Verify register management endpoints still work after removal: build and run Register Service tests (`dotnet test tests/Sorcha.Register.Service.Tests/`). Ensure GET, PUT, DELETE endpoints are unaffected.

**Checkpoint**: No way to create a register without cryptographic attestations.

---

## Phase 7: User Story 5 - Correct API Gateway Routing (Priority: P2)

**Goal**: API Gateway correctly routes `/api/validator/genesis` to the Validator Service

**Independent Test**: Send request to gateway's `/api/validator/genesis` path, verify it reaches the Validator (not 404)

- [ ] T027 [US5] Add `validator-genesis-route` to `src/Services/Sorcha.ApiGateway/appsettings.json`. Insert a new route BEFORE the existing `validator-route` catch-all (before line ~686). Route config: `"Match": { "Path": "/api/validator/genesis" }`, no path transformation (passthrough), cluster: `validator-cluster`. Set `"Order": 1` to ensure it takes priority over the catch-all. Verify existing validator routes (`validator-health-route`, `validator-route`, `validator-status-route`) remain functional.

**Checkpoint**: External clients can reach the genesis endpoint through the API Gateway.

---

## Phase 8: User Story 6 - Updated Walkthrough (Priority: P3)

**Goal**: Walkthrough scripts work end-to-end with the new hash-based signing flow

**Independent Test**: Run walkthrough script against Docker environment, all 6 steps complete

- [ ] T028 [US6] Update `walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1`. Changes: (1) receive `dataToSign` as hex hash (not canonical JSON); (2) convert hex to bytes: `[byte[]]$hashBytes = [System.Convert]::FromHexString($dataToSign)`; (3) Base64-encode hash bytes for wallet endpoint: `$dataBase64 = [Convert]::ToBase64String($hashBytes)`; (4) add `isPreHashed = $true` to the wallet sign request body; (5) update any assertions/output messages to reflect hex hash format. Update the README.md in the walkthrough directory to document the new flow.

- [ ] T029 [US6] Update or remove legacy walkthrough script `walkthroughs/RegisterCreationFlow/test-register-creation.ps1` that uses deprecated `creator`/`controlRecord` fields. Either update to use the new `owners`/`signedAttestations` pattern or remove if superseded by the real-signing script.

**Checkpoint**: Walkthrough executes end-to-end with real cryptographic signatures.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [ ] T030 Run full solution build and test suite: `dotnet restore && dotnet build && dotnet test`. Ensure zero warnings in Release build per constitution requirement (V. Code Quality).

- [ ] T031 Update walkthrough `WALKTHROUGH-RESULTS.md` in `walkthroughs/RegisterCreationFlow/` to document successful end-to-end execution with the new flow. Remove references to the "double-hashing bug" (which was a misdiagnosis).

- [ ] T032 Verify Docker end-to-end: `docker-compose build && docker-compose up -d`, then run `pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1` and confirm all 6 steps pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - T001-T004 can all run in parallel
- **Phase 2 (Foundational)**: T006-T010 depend on Phase 1 completion. T011 depends on T001-T003 and T005.
- **Phase 3 (US3)**: Depends on Phase 2 (T006-T010). Tests validate pre-hashed signing.
- **Phase 4 (US2)**: Depends on Phase 2 (T011 de-stubbed client). Can run in parallel with Phase 3.
- **Phase 5 (US1)**: Depends on Phase 3 AND Phase 4 (needs pre-hashed signing + real system wallet)
- **Phase 6 (US4)**: Depends on Phase 5 (remove CRUD only after crypto path works)
- **Phase 7 (US5)**: Independent of other phases; can run after Phase 2
- **Phase 8 (US6)**: Depends on Phase 5 (walkthrough needs working end-to-end flow)
- **Phase 9 (Polish)**: Depends on all previous phases

### User Story Dependencies

- **US3 (Pre-Hashed Signing)**: Independent - no dependencies on other stories
- **US2 (System Wallet Signing)**: Independent - no dependencies on other stories (needs Phase 2 foundation)
- **US1 (Register Creation)**: Depends on US2 + US3 (needs pre-hashed signing AND real system wallet)
- **US4 (Remove CRUD)**: Depends on US1 (don't remove until crypto path works)
- **US5 (Gateway Routing)**: Independent - can run anytime after Phase 2
- **US6 (Walkthrough)**: Depends on US1 (needs working end-to-end flow)

### Within Each User Story

- Tests before implementation (where applicable)
- Models/DTOs before services
- Services before endpoints
- Core implementation before integration
- Build/test verification at each checkpoint

### Parallel Opportunities

**Phase 1** (all parallel):
```
T001 IServiceAuthClient    |  T002 ServiceAuthClient  |  T003 WalletSignResult + interface  |  T004 PendingRegistration model
```

**Phase 2** (T006-T010 parallel, then T011):
```
T006 SignTransactionRequest  |  T007 ITransactionService  |  (then T008, T009, T010 sequential)  |  T011 WalletServiceClient (after T001-T005)
```

**Phase 3 + Phase 4** (can run in parallel):
```
Phase 3: T012-T013 (pre-hashed tests + verify)
Phase 4: T014-T018 (system wallet tests + implementation)
```

**Phase 6 + Phase 7** (can run in parallel):
```
Phase 6: T025-T026 (remove CRUD)
Phase 7: T027 (gateway routing)
```

---

## Implementation Strategy

### MVP First (US3 + US2 + US1)

1. Complete Phase 1: Setup (shared infrastructure)
2. Complete Phase 2: Foundational (pre-hashed signing + de-stubbed client)
3. Complete Phase 3: US3 (validate pre-hashed signing)
4. Complete Phase 4: US2 (validate real system wallet signing)
5. Complete Phase 5: US1 (end-to-end register creation)
6. **STOP and VALIDATE**: Test full register creation flow against Docker

### Incremental Delivery

1. Setup + Foundational -> Foundation ready
2. US3 (pre-hashed signing) -> Wallet Service enhanced, all existing tests pass
3. US2 (system wallet signing) -> Validator produces real genesis signatures
4. US1 (register creation) -> Full end-to-end crypto flow works **(MVP!)**
5. US4 + US5 (cleanup) -> CRUD removed, gateway fixed
6. US6 (walkthrough) -> Documentation verified
7. Polish -> Final validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1/US2/US3 are all P1 but US1 depends on the other two completing first
- Constitution requires >85% test coverage for new code (IV. Testing Requirements)
- All services share the same JWT signing key via `JwtAuthenticationExtensions`
- Service principals must be pre-registered in the Tenant Service for JWT auth to work
- The `IWalletIntegrationService` (Validator's gRPC client) is NOT modified -- it continues handling high-frequency docket/vote signing separately
