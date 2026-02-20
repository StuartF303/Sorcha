# Tasks: Published Participant Records on Register

**Input**: Design documents from `/specs/001-participant-records/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — SC-007 requires >85% coverage for new code.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Models)

**Purpose**: Create the foundational types and models that all services depend on

- [x] T001 Add `Participant = 3` to TransactionType enum in `src/Common/Sorcha.Register.Models/Enums/TransactionType.cs`
- [x] T002 [P] Create ParticipantRecordStatus enum (Active, Deprecated, Revoked) in `src/Common/Sorcha.Register.Models/Enums/ParticipantRecordStatus.cs`
- [x] T003 [P] Create ParticipantRecord and ParticipantAddress models in `src/Common/Sorcha.Register.Models/ParticipantRecord.cs` — include participantId (UUID), organizationName, participantName, status, version, addresses array, optional metadata (JsonElement?)
- [x] T004 Make BlueprintId and ActionId nullable (`string?` instead of `required string`) on TransactionSubmission record in `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs` — update all callers to handle null
- [x] T005 [P] Create built-in participant record JSON Schema in `src/Services/Sorcha.Validator.Service/Schemas/participant-record-v1.json` per research.md R5, and embed as resource in csproj

**Checkpoint**: All shared models compile. Run `dotnet build src/Common/Sorcha.Register.Models/ && dotnet build src/Common/Sorcha.ServiceClients/`

---

## Phase 2: Foundational (Validator Service)

**Purpose**: Validator must accept and validate Participant transactions before any publishing or querying can work

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Add participant schema validation path in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — when TransactionType is Participant, validate payload against built-in participant-record-v1.json schema instead of blueprint schema lookup (research.md R7)
- [x] T007 Add governance rights skip for Participant transactions in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — skip ValidateGovernanceRightsAsync when TransactionType is Participant (research.md R8)
- [x] T008 Skip blueprint conformance validation for Participant transactions in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — extend the genesis/control skip to include Participant type (research.md R1)
- [x] T009 [P] Add ParticipantRecord and ParticipantRecordStatus model unit tests in `tests/Sorcha.Register.Models.Tests/` — test construction, validation constraints, JSON serialization round-trip, address primary default behavior
- [x] T010 [P] Add Participant validation unit tests in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs` — test schema validation (valid/invalid payloads), governance skip, blueprint conformance skip, signature verification still runs

**Checkpoint**: Validator accepts well-formed Participant transactions and rejects malformed ones. Run `dotnet test tests/Sorcha.Validator.Service.Tests/ && dotnet test tests/Sorcha.Register.Models.Tests/`

---

## Phase 3: User Story 1 — Publish a Participant Record (Priority: P1) MVP

**Goal**: An org admin can publish a participant record to a register via the Tenant Service. The transaction is signed, submitted through the validator pipeline, and written to the register.

**Independent Test**: Publish a participant record and confirm it appears in register transaction history as a Participant-type transaction with correct content.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T011 [P] [US1] Write ParticipantPublishingService unit tests in `tests/Sorcha.Tenant.Service.Tests/Services/ParticipantPublishingServiceTests.cs` — test PublishParticipantAsync: builds correct TransactionSubmission (Type=Participant, BlueprintId=null, ActionId="participant-publish"), deterministic TxId formula (SHA256("participant-publish-{registerId}-{participantId}-v{version}")), canonical JSON payload hash, PrevTxId chains from latest Control TX, calls IWalletServiceClient.SignAsync, calls IValidatorServiceClient.SubmitTransactionAsync
- [x] T012 [P] [US1] Write publish endpoint tests in `tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointTests.cs` — test POST /api/organizations/{orgId}/participants/publish: 202 on valid request, 400 on missing fields (no name, no addresses), 400 on >10 addresses, 403 on unauthorized user, 409 on duplicate wallet address

### Implementation for User Story 1

- [x] T013 [US1] Create IParticipantPublishingService interface in `src/Services/Sorcha.Tenant.Service/Services/IParticipantPublishingService.cs` — PublishParticipantAsync(request) returns ParticipantPublishResult with transactionId, participantId, registerId, version, status
- [x] T014 [US1] Implement ParticipantPublishingService.PublishParticipantAsync in `src/Services/Sorcha.Tenant.Service/Services/ParticipantPublishingService.cs` — inject IRegisterServiceClient, IValidatorServiceClient, IWalletServiceClient, IHashProvider. Build ParticipantRecord payload, generate UUID participantId, compute deterministic TxId, serialize canonical JSON, compute payload hash, fetch latest Control TX for PrevTxId, sign with user's wallet, submit via validator. Follow existing blueprint publish pattern in BlueprintPublishService.
- [x] T015 [US1] Add POST /api/organizations/{organizationId}/participants/publish endpoint in `src/Services/Sorcha.Tenant.Service/Endpoints/ParticipantEndpoints.cs` — RequireAdministrator authorization, validate request body (participantName 1-200 chars, organizationName 1-200 chars, addresses 1-10 entries each with walletAddress+publicKey+algorithm, signerWalletAddress required, optional metadata max 10KB), return 202 Accepted with ParticipantPublishResult
- [x] T016 [US1] Register IParticipantPublishingService in DI and map ParticipantEndpoints in `src/Services/Sorcha.Tenant.Service/Program.cs`
- [x] T017 [US1] Add YARP route for participant publish endpoints in `src/Services/Sorcha.ApiGateway/` configuration — route `/api/organizations/*/participants/publish**` to tenant-cluster (already covered by existing organizations-direct-route)

**Checkpoint**: User Story 1 is fully functional. An authorized user can publish a participant record that flows through the validator and is written to the register.

---

## Phase 4: User Story 2 — Look Up Published Participants (Priority: P1)

**Goal**: Users can query published participants on a register by wallet address, participant ID, or list all. The Register Service indexes addresses for fast O(1) lookups.

**Independent Test**: Publish participant records and query them by address — confirm correct record returned with all address entries.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T018 [P] [US2] Write ParticipantIndexService unit tests in `tests/Sorcha.Register.Service.Tests/Services/ParticipantIndexServiceTests.cs` — test IndexParticipantAsync (indexes all addresses), GetByAddressAsync (returns correct participant), GetByIdAsync (returns latest version), ListAsync (paginates, filters by status), RebuildIndexAsync (scans Participant TXs)
- [x] T019 [P] [US2] Write participant query endpoint tests in `tests/Sorcha.Register.Service.Tests/Endpoints/ParticipantEndpointTests.cs` — test GET .../participants (list with OData skip/top/count), GET .../by-address/{addr} (200 found, 404 not found), GET .../participants/{id} (200 found, 404 not found, includeHistory=true returns version history)
- [x] T020 [P] [US2] Write service client participant query tests in `tests/Sorcha.ServiceClients.Tests/Register/RegisterServiceClientParticipantTests.cs` — test GetPublishedParticipantsAsync, GetPublishedParticipantByAddressAsync, GetPublishedParticipantByIdAsync deserialization

### Implementation for User Story 2

- [x] T021 [P] [US2] Create response models in `src/Common/Sorcha.ServiceClients/Register/Models/` — PublishedParticipantRecord, ParticipantAddressInfo, ParticipantPage, ParticipantVersionSummary per service-client-api.md contract
- [x] T022 [US2] Create ParticipantIndexService in `src/Services/Sorcha.Register.Service/Services/ParticipantIndexService.cs` — in-memory index (ConcurrentDictionary) mapping walletAddress→participantId and participantId→latestRecord. Methods: IndexParticipantAsync (extract addresses from TX payload, upsert), GetByAddressAsync, GetByIdAsync (with optional history from TX chain), ListAsync (paginated, status filter), RebuildIndexAsync (scan Participant-type TXs on register, group by participantId, take highest version)
- [x] T023 [US2] Add participant list endpoint `GET /api/registers/{registerId}/participants` in `src/Services/Sorcha.Register.Service/Program.cs` — OData pagination ($skip, $top, $count), status filter (default "active"), CanReadTransactions authorization. Return ParticipantPage.
- [x] T024 [US2] Add participant by-address endpoint `GET /api/registers/{registerId}/participants/by-address/{walletAddress}` in `src/Services/Sorcha.Register.Service/Program.cs` — return PublishedParticipantRecord or 404
- [x] T025 [US2] Add participant by-id endpoint `GET /api/registers/{registerId}/participants/{participantId}` in `src/Services/Sorcha.Register.Service/Program.cs` — optional includeHistory query param, return PublishedParticipantRecord (with history array when requested) or 404
- [x] T026 [US2] Wire ParticipantIndexService into Register Service DI and trigger indexing when Participant transactions are stored — hook into transaction storage flow in `src/Services/Sorcha.Register.Service/Program.cs`
- [x] T027 [US2] Add participant query methods to IRegisterServiceClient interface in `src/Common/Sorcha.ServiceClients/Register/IRegisterServiceClient.cs` — GetPublishedParticipantsAsync(registerId, page, pageSize, status?), GetPublishedParticipantByAddressAsync(registerId, walletAddress), GetPublishedParticipantByIdAsync(registerId, participantId, includeHistory?)
- [x] T028 [US2] Implement participant query methods in RegisterServiceClient in `src/Common/Sorcha.ServiceClients/Register/RegisterServiceClient.cs` — HTTP GET calls to Register Service endpoints with JSON deserialization
- [x] T029 [US2] Add YARP routes for participant query endpoints in API Gateway configuration — route `/api/registers/*/participants**` to register-cluster (already covered by existing registers-direct-route)

**Checkpoint**: User Stories 1 AND 2 are both functional. Publish a participant and query it by address.

---

## Phase 5: User Story 3 — Update a Published Participant Record (Priority: P2)

**Goal**: An authorized user can publish updated versions of participant records. The latest version supersedes previous ones. Version chain integrity is enforced (PrevTxId references previous version).

**Independent Test**: Publish a participant, then publish an update (version 2 with changed name and new address). Query returns only version 2.

### Tests for User Story 3

- [x] T030 [P] [US3] Write update publishing tests in `tests/Sorcha.Tenant.Service.Tests/Services/ParticipantPublishingServiceTests.cs` — test UpdateParticipantAsync: fetches existing participant by ID, increments version, PrevTxId references previous version's TxId (not Control TX), deterministic TxId includes new version, allows name/org changes, allows address changes
- [x] T031 [P] [US3] Write update endpoint tests in `tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointTests.cs` — test PUT .../publish/{participantId}: 202 on valid update, 404 when participant not found, 400 when registerId doesn't match original

### Implementation for User Story 3

- [x] T032 [US3] Implement UpdateParticipantAsync in `src/Services/Sorcha.Tenant.Service/Services/ParticipantPublishingService.cs` — fetch current participant via IRegisterServiceClient.GetPublishedParticipantByIdAsync, verify registerId matches, build new ParticipantRecord with incremented version and same participantId, PrevTxId = latestTxId of current version, sign and submit
- [x] T033 [US3] Add PUT /api/organizations/{organizationId}/participants/publish/{participantId} endpoint in `src/Services/Sorcha.Tenant.Service/Endpoints/ParticipantEndpoints.cs` — same body as POST plus optional status field, return 202 with version and previousVersion info per tenant-service-api.md contract

**Checkpoint**: Participant records can be published and updated. Queries return latest version only.

---

## Phase 6: User Story 4 — Revoke or Deprecate a Participant Record (Priority: P2)

**Goal**: An authorized user can revoke or deprecate a participant. Revoked participants are excluded from default queries. Full history preserved for audit.

**Independent Test**: Publish a participant, revoke it, confirm default listing excludes it but history query shows all versions.

### Tests for User Story 4

- [x] T034 [P] [US4] Write revocation tests in `tests/Sorcha.Tenant.Service.Tests/Services/ParticipantPublishingServiceTests.cs` — test RevokeParticipantAsync: builds version with status="revoked", preserves participantId, PrevTxId from current version
- [x] T035 [P] [US4] Write revoke endpoint and status filtering tests — DELETE endpoint returns 202, revoked participant excluded from default list (test GET .../participants with status=all includes it, default excludes it)

### Implementation for User Story 4

- [x] T036 [US4] Implement RevokeParticipantAsync in `src/Services/Sorcha.Tenant.Service/Services/ParticipantPublishingService.cs` — fetch current participant, build new version with status="revoked" and minimal payload (same participantId, org, name, addresses from current version), sign and submit
- [x] T037 [US4] Add DELETE /api/organizations/{organizationId}/participants/publish/{participantId} endpoint in `src/Services/Sorcha.Tenant.Service/Endpoints/ParticipantEndpoints.cs` — query params: registerId (required), signerWalletAddress (required). Return 202 per tenant-service-api.md contract.
- [x] T038 [US4] Update ParticipantIndexService status filtering in `src/Services/Sorcha.Register.Service/Services/ParticipantIndexService.cs` — ListAsync excludes revoked by default, GetByAddressAsync returns revoked with status visible, support "all" status filter (already implemented in T022)

**Checkpoint**: Full participant lifecycle works: publish → update → deprecate → revoke. All versions preserved on chain.

---

## Phase 7: User Story 5 — Resolve Participant Address for Encryption (Priority: P3)

**Goal**: System can resolve a participant's public key by wallet address and optional algorithm for field-level encryption. Revoked participants return 410 Gone.

**Independent Test**: Publish a participant with ED25519 and P-256 addresses. Resolve public key for ED25519 — correct key returned. Revoke participant — resolution returns 410.

### Tests for User Story 5

- [x] T039 [P] [US5] Write public key resolution tests in `tests/Sorcha.Register.Service.Tests/Endpoints/ParticipantEndpointTests.cs` — test GET .../by-address/{addr}/public-key: 200 with correct key for specific algorithm, 200 with primary key when no algorithm specified, 404 when address not found, 410 when participant revoked
- [x] T040 [P] [US5] Write ResolvePublicKeyAsync service client test in `tests/Sorcha.ServiceClients.Tests/Register/RegisterServiceClientParticipantTests.cs` — test deserialization of PublicKeyResolution response

### Implementation for User Story 5

- [x] T041 [US5] Create PublicKeyResolution response model in `src/Common/Sorcha.ServiceClients/Register/Models/` — participantId, participantName, walletAddress, publicKey, algorithm, status per service-client-api.md
- [x] T042 [US5] Add public-key resolution endpoint `GET /api/registers/{registerId}/participants/by-address/{walletAddress}/public-key` in `src/Services/Sorcha.Register.Service/Program.cs` — optional algorithm query param, return 200 with PublicKeyResolution, 404 if not found, 410 if revoked per register-service-api.md contract
- [x] T043 [US5] Add ResolvePublicKeyAsync to IRegisterServiceClient in `src/Common/Sorcha.ServiceClients/Register/IRegisterServiceClient.cs` and implement in RegisterServiceClient

**Checkpoint**: All 5 user stories are independently functional.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T044 [P] Add MongoDB index on `MetaData.TransactionType` in Register Service MongoDB setup (research.md R3) — enables efficient participant-only queries
- [x] T045 [P] Add Redis caching layer to ParticipantIndexService in `src/Services/Sorcha.Register.Service/Services/ParticipantIndexService.cs` — cache keys per data-model.md (participant:addr:{registerId}:{walletAddress}, participant:id:{registerId}:{participantId}, participant:list:{registerId}), 1-hour TTL, invalidate on new Participant TX
- [x] T046 [P] Add wallet address uniqueness check — before publishing, query ParticipantIndexService to verify no other active participant claims the same address on that register (FR-010). Reject with 409 Conflict from Tenant Service endpoint.
- [x] T047 [P] Update CLAUDE.md with participant publishing endpoints and service client methods
- [x] T048 [P] Update `.specify/MASTER-TASKS.md` with feature completion status
- [x] T049 Verify >85% test coverage for all new code — run `dotnet test --collect:"XPlat Code Coverage"` on affected test projects
- [x] T050 Run quickstart.md validation — execute the example curl commands from quickstart.md against running services

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1 (Phase 3) must complete before US2 (Phase 4) — lookup needs published data
  - US3 (Phase 5) depends on US2 — update needs to fetch current version via query
  - US4 (Phase 6) depends on US3 — revoke is a special case of update
  - US5 (Phase 7) depends on US2 — resolution uses the same index/query infrastructure
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (Publish, P1)**: Depends on Phase 2 only — core publishing capability
- **US2 (Lookup, P1)**: Depends on US1 — needs published participants to query
- **US3 (Update, P2)**: Depends on US2 — needs to fetch current version before updating
- **US4 (Revoke, P2)**: Depends on US3 — revocation is implemented as a status update
- **US5 (Resolve, P3)**: Depends on US2 — uses same index infrastructure, adds public-key endpoint

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- Phase 1: T002, T003, T005 can all run in parallel (different files)
- Phase 2: T009, T010 can run in parallel with each other (different test projects)
- Each user story: test tasks marked [P] can run in parallel
- Phase 8: T044-T048 can all run in parallel (different concerns)

---

## Parallel Example: Phase 1 Setup

```bash
# Launch all independent model tasks together:
Task: "T002 Create ParticipantRecordStatus enum in src/Common/Sorcha.Register.Models/Enums/ParticipantRecordStatus.cs"
Task: "T003 Create ParticipantRecord and ParticipantAddress models in src/Common/Sorcha.Register.Models/ParticipantRecord.cs"
Task: "T005 Create participant-record-v1.json schema in src/Services/Sorcha.Validator.Service/Schemas/participant-record-v1.json"

# Then sequentially:
Task: "T001 Add Participant = 3 to TransactionType enum" (quick, foundational)
Task: "T004 Make BlueprintId/ActionId nullable on TransactionSubmission" (touches callers)
```

## Parallel Example: User Story 2

```bash
# Launch all tests together:
Task: "T018 ParticipantIndexService tests"
Task: "T019 Participant query endpoint tests"
Task: "T020 Service client participant query tests"

# Launch response models (no deps):
Task: "T021 Create response models"

# Then implement sequentially:
Task: "T022 ParticipantIndexService" → "T023-T025 Endpoints" → "T026 Wire DI" → "T027-T028 Service client" → "T029 YARP routes"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (shared models)
2. Complete Phase 2: Foundational (validator accepts Participant TXs)
3. Complete Phase 3: US1 — Publish participant records
4. Complete Phase 4: US2 — Query participant records
5. **STOP and VALIDATE**: Publish a participant and look it up by address
6. Deploy/demo if ready — this is the minimum viable increment

### Incremental Delivery

1. Setup + Foundational → Validator ready
2. Add US1 (Publish) → Test independently → First increment
3. Add US2 (Lookup) → Test independently → **MVP complete!**
4. Add US3 (Update) → Test independently → Version management
5. Add US4 (Revoke) → Test independently → Full lifecycle
6. Add US5 (Resolve) → Test independently → Encryption integration ready
7. Polish → Redis cache, indexes, docs, coverage

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Follow existing patterns: BlueprintPublishService for TX building, ValidationEngine for validation, Register Service Program.cs for endpoints
