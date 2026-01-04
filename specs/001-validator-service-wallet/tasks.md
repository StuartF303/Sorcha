# Tasks: Validator Service Wallet Access

**Feature**: 001-validator-service-wallet
**Branch**: `001-validator-service-wallet`
**Input**: Design documents from `/specs/001-validator-service-wallet/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Test tasks are included per constitutional requirements (>85% coverage target for new code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Each sprint delivers a complete, testable user story.

---

## Format: `- [ ] [ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions
- All tasks must follow strict checklist format

---

## Path Conventions

This feature enhances the existing **Sorcha.Validator.Service** microservice:

- **Service**: `src/Services/Sorcha.Validator.Service/`
- **Tests**: `tests/Sorcha.Validator.Service.Tests/` and `tests/Sorcha.Validator.Service.IntegrationTests/`
- **Common**: `src/Common/Sorcha.ServiceClients/`
- **Wallet Service**: `src/Services/Sorcha.Wallet.Service/` (add gRPC endpoints)
- **Tenant Service**: `src/Services/Sorcha.Tenant.Service/` (add system org config)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, proto files, and configuration structure

- [ ] T001 Copy proto files from `specs/001-validator-service-wallet/contracts/` to service directories
- [ ] T002 [P] Add Grpc.Net.Client 2.71.0 and Polly 8.5.0 to `src/Services/Sorcha.Validator.Service/Sorcha.Validator.Service.csproj`
- [ ] T003 [P] Add gRPC proto reference for wallet_service.proto in Validator Service .csproj
- [ ] T004 [P] Create WalletConfiguration.cs in `src/Services/Sorcha.Validator.Service/Configuration/`
- [ ] T005 [P] Create RetryPolicyConfiguration.cs in `src/Services/Sorcha.Validator.Service/Configuration/`
- [ ] T006 [P] Add WalletService configuration section to `src/Services/Sorcha.Validator.Service/appsettings.json`

**Checkpoint**: Project structure ready for implementation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models, interfaces, and gRPC infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 [P] Create WalletDetails.cs model in `src/Services/Sorcha.Validator.Service/Models/`
- [ ] T008 [P] Create WalletAlgorithm enum in `src/Services/Sorcha.Validator.Service/Models/`
- [ ] T009 [P] Update Signature.cs model to add SignedBy field in `src/Services/Sorcha.Validator.Service/Models/`
- [ ] T010 [P] Create IWalletIntegrationService interface in `src/Services/Sorcha.Validator.Service/Services/`
- [ ] T011 Create WalletIntegrationService skeleton in `src/Services/Sorcha.Validator.Service/Services/` (constructor, fields, no logic yet)
- [ ] T012 [P] Implement Polly retry policy factory method in WalletIntegrationService
- [ ] T013 [P] Register Sorcha.Cryptography ICryptoModule in Validator Service Program.cs
- [ ] T014 Register WalletIntegrationService as singleton in Validator Service Program.cs
- [ ] T015 [P] Add Wallet Service gRPC proto to `src/Services/Sorcha.Wallet.Service/Protos/wallet_service.proto`
- [ ] T016 Add gRPC service implementation skeleton WalletGrpcService.cs in `src/Services/Sorcha.Wallet.Service/GrpcServices/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Validator Service Initialization with System Wallet (Priority: P1) 🎯 MVP

**Goal**: Enable Validator Service to authenticate to Wallet Service and retrieve wallet details on startup

**Independent Test**: Start Validator Service, verify it logs successful wallet initialization with wallet ID and address

**Acceptance Criteria**:
- ✅ AS1: Validator retrieves wallet details (wallet ID, address, algorithm) from Wallet Service on startup
- ✅ AS2: Validator successfully signs a test message using wallet service
- ✅ AS3: Service fails to start with clear error if wallet not configured

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T017 [P] [US1] Unit test for GetWalletDetailsAsync caching in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T018 [P] [US1] Unit test for wallet configuration loading from environment variable in `tests/Sorcha.Validator.Service.Tests/Configuration/WalletConfigurationTests.cs`
- [ ] T019 [P] [US1] Integration test for wallet initialization in `tests/Sorcha.Validator.Service.IntegrationTests/WalletIntegrationTests.cs`

### Implementation for User Story 1

- [ ] T020 [P] [US1] Implement GetWalletDetailsAsync with in-memory caching in WalletIntegrationService.cs
- [ ] T021 [P] [US1] Implement wallet configuration loader (environment variable fallback) in WalletIntegrationService.cs or separate provider
- [ ] T022 [US1] Implement GetWalletDetails RPC handler in `src/Services/Sorcha.Wallet.Service/GrpcServices/WalletGrpcService.cs`
- [ ] T023 [US1] Add wallet initialization on Validator Service startup in Program.cs (call GetWalletDetailsAsync)
- [ ] T024 [US1] Add structured logging for wallet initialization (wallet ID, address, algorithm)
- [ ] T025 [US1] Add graceful failure handling if wallet not found (log error, exit with code 1)
- [ ] T026 [US1] Add health check for wallet connectivity in Validator Service

**Checkpoint**: Validator Service can successfully initialize and retrieve wallet details. Test by starting service and checking logs.

---

## Phase 4: User Story 2 - Docket Signing with Validator Wallet (Priority: P1)

**Goal**: Enable Validator Service to sign dockets using its wallet before broadcasting to peers

**Independent Test**: Trigger docket building, verify docket contains ProposerSignature with valid signature that can be verified

**Acceptance Criteria**:
- ✅ AS1: Docket contains signature from validator's wallet address
- ✅ AS2: Peers can verify signature using validator's public wallet address
- ✅ AS3: Signing retries with exponential backoff on Wallet Service failure

### Tests for User Story 2

- [ ] T027 [P] [US2] Unit test for SignDocketAsync using local crypto in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T028 [P] [US2] Unit test for GetDerivedPrivateKeyAsync caching in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T029 [P] [US2] Unit test for retry policy on Wallet Service failures in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T030 [P] [US2] Integration test for docket signing end-to-end in `tests/Sorcha.Validator.Service.IntegrationTests/DocketSigningTests.cs`

### Implementation for User Story 2

- [ ] T031 [P] [US2] Implement GetDerivedKey RPC handler in `src/Services/Sorcha.Wallet.Service/GrpcServices/WalletGrpcService.cs`
- [ ] T032 [P] [US2] Implement DerivedKeyCache internal class in WalletIntegrationService.cs (thread-safe cache with SemaphoreSlim)
- [ ] T033 [P] [US2] Implement GetDerivedPrivateKeyAsync in WalletIntegrationService.cs (fetch + cache)
- [ ] T034 [US2] Implement SignDocketAsync using Sorcha.Cryptography with derived keys in WalletIntegrationService.cs
- [ ] T035 [US2] Add ProposerSignature field to Docket model in `src/Services/Sorcha.Validator.Service/Models/Docket.cs`
- [ ] T036 [US2] Add ProposerValidatorId field to Docket model in `src/Services/Sorcha.Validator.Service/Models/Docket.cs`
- [ ] T037 [US2] Update DocketBuilder to call SignDocketAsync before finalizing in `src/Services/Sorcha.Validator.Service/Services/DocketBuilder.cs`
- [ ] T038 [US2] Add docket signing logging (docket number, algorithm, signature length) in DocketBuilder.cs
- [ ] T039 [US2] Implement IDisposable in WalletIntegrationService to zero out cached private keys on shutdown

**Checkpoint**: Dockets are signed with validator wallet. Test by building a docket and verifying signature is present and valid.

---

## Phase 5: User Story 3 - Consensus Vote Signing (Priority: P2)

**Goal**: Enable Validator Service to sign consensus votes to prove vote authenticity

**Independent Test**: Trigger consensus vote, verify VoteResponse contains ValidatorSignature that can be verified

**Acceptance Criteria**:
- ✅ AS1: Vote responses include signature signed with validator's wallet
- ✅ AS2: Validator verifies peer vote signatures before counting them
- ✅ AS3: Invalid vote signatures are rejected and logged

### Tests for User Story 3

- [ ] T040 [P] [US3] Unit test for SignVoteAsync using local crypto in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T041 [P] [US3] Unit test for VerifySignatureAsync in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T042 [P] [US3] Integration test for consensus vote signing in `tests/Sorcha.Validator.Service.IntegrationTests/ConsensusVoteTests.cs`

### Implementation for User Story 3

- [ ] T043 [P] [US3] Implement SignVoteAsync using Sorcha.Cryptography with derived keys in WalletIntegrationService.cs (use different derivation path m/44'/0'/0'/1/0)
- [ ] T044 [P] [US3] Implement VerifySignatureAsync using Sorcha.Cryptography in WalletIntegrationService.cs
- [ ] T045 [US3] Update ConsensusVote model to include ValidatorSignature field in `src/Services/Sorcha.Validator.Service/Models/ConsensusVote.cs`
- [ ] T046 [US3] Update ConsensusEngine.ValidateAndVoteAsync to call SignVoteAsync in `src/Services/Sorcha.Validator.Service/Services/ConsensusEngine.cs`
- [ ] T047 [US3] Update ConsensusEngine to verify peer vote signatures using VerifySignatureAsync before counting votes
- [ ] T048 [US3] Add vote signature validation logging (validator ID, result, signature mismatch details) in ConsensusEngine.cs
- [ ] T049 [US3] Implement VerifySignature RPC handler in `src/Services/Sorcha.Wallet.Service/GrpcServices/WalletGrpcService.cs` (optional - for fallback if local crypto fails)

**Checkpoint**: Consensus votes are signed and verified. Test by triggering consensus and checking vote signatures in logs.

---

## Phase 6: User Story 4 - System Organization Wallet Configuration (Priority: P3)

**Goal**: Enable administrators to configure validator wallet via Tenant Service

**Independent Test**: Update system organization config via Tenant Service API, restart Validator Service, verify it uses new wallet

**Acceptance Criteria**:
- ✅ AS1: System administrator can set validator wallet ID via Tenant Service API
- ✅ AS2: Validator Service uses newly configured wallet on restart
- ✅ AS3: Validator fails to start if configured wallet ID doesn't exist

### Tests for User Story 4

- [ ] T050 [P] [US4] Unit test for Tenant Service configuration retrieval in `tests/Sorcha.Validator.Service.Tests/Configuration/TenantConfigurationProviderTests.cs`
- [ ] T051 [P] [US4] Integration test for wallet configuration from Tenant Service in `tests/Sorcha.Validator.Service.IntegrationTests/TenantConfigurationTests.cs`

### Implementation for User Story 4

- [ ] T052 [P] [US4] Add ValidatorWalletId field to OrganizationConfiguration model in `src/Services/Sorcha.Tenant.Service/Models/OrganizationConfiguration.cs`
- [ ] T053 [P] [US4] Add tenant_service.proto to `src/Services/Sorcha.Tenant.Service/Protos/`
- [ ] T054 [P] [US4] Implement GetSystemOrganizationConfig RPC handler in `src/Services/Sorcha.Tenant.Service/GrpcServices/TenantGrpcService.cs`
- [ ] T055 [P] [US4] Implement UpdateSystemOrganizationConfig RPC handler in Tenant Service (admin authorization required)
- [ ] T056 [US4] Create TenantServiceClient wrapper in `src/Common/Sorcha.ServiceClients/Tenant/` (or extend existing client)
- [ ] T057 [US4] Implement configuration provider that calls Tenant Service in Validator Service (TenantConfigurationProvider.cs)
- [ ] T058 [US4] Update Validator Service startup to try Tenant Service before falling back to environment variable in Program.cs
- [ ] T059 [US4] Add validation: Verify wallet ID exists by calling Wallet Service GetWalletDetails during config load
- [ ] T060 [US4] Add logging for configuration source (environment variable vs Tenant Service) in Program.cs

**Checkpoint**: Validator wallet can be configured via Tenant Service. Test by updating config and restarting service.

---

## Phase 7: Edge Cases & Error Handling

**Purpose**: Handle edge cases from spec.md (wallet deletion, rotation, network failures)

- [ ] T061 [P] Unit test for wallet deletion detection in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T062 [P] Unit test for wallet rotation detection in `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`
- [ ] T063 [P] Implement wallet deletion detection in SignDocketAsync (detect 404 from Wallet Service, log critical error, trigger shutdown)
- [ ] T064 [P] Implement wallet rotation detection in signing operations (compare version before/after, invalidate cache if changed)
- [ ] T065 [P] Add graceful shutdown handler for wallet deletion in Validator Service Program.cs
- [ ] T066 Add network connectivity loss handling with retry logic (verify exponential backoff: 1s, 2s, 4s) in WalletIntegrationService
- [ ] T067 [P] Add logging for all edge cases (deletion, rotation, network failure) with appropriate severity levels

**Checkpoint**: Edge cases handled gracefully. Test by simulating wallet deletion, rotation, and network failures.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T068 [P] Add OpenTelemetry spans for all wallet operations (GetWalletDetails, SignDocket, SignVote, VerifySignature)
- [ ] T069 [P] Add performance metrics tracking (docket signing rate, signature verification latency)
- [ ] T070 [P] Security audit: Verify no private keys logged in any log statements across all files
- [ ] T071 [P] Security audit: Verify derived keys never persisted to disk/config/database
- [ ] T072 [P] Update Validator Service README with wallet integration documentation
- [ ] T073 [P] Update quickstart.md with actual test results and troubleshooting tips
- [ ] T074 Code cleanup: Remove any unused imports, add XML documentation comments
- [ ] T075 Run full test suite and verify >85% code coverage for new wallet integration code
- [ ] T076 Performance validation: Verify 10+ dockets/sec signing throughput using local crypto
- [ ] T077 [P] Update MASTER-TASKS.md with completion status for this feature

**Checkpoint**: All cross-cutting concerns addressed, ready for production deployment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - Can start immediately after foundation
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) AND User Story 1 (needs wallet initialization)
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) AND User Story 1 (needs wallet integration service)
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) - Can run parallel to US2/US3
- **Edge Cases (Phase 7)**: Depends on User Stories 1-3 being complete
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

```
Setup (Phase 1)
    ↓
Foundational (Phase 2)
    ↓
    ├─→ User Story 1 (P1) ──→ User Story 2 (P1) ──┐
    │                             ↓                │
    │                      User Story 3 (P2) ──────┤
    │                                              │
    └─→ User Story 4 (P3) ────────────────────────┤
                                                   ↓
                                          Edge Cases (Phase 7)
                                                   ↓
                                          Polish (Phase 8)
```

**Key Dependencies**:
- US2 (Docket Signing) requires US1 (Wallet Initialization) to be complete
- US3 (Vote Signing) requires US1 (Wallet Initialization) to be complete
- US4 (Configuration) is independent and can run parallel to US2/US3 after US1

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. Models/entities before services
3. Services before integration points
4. Core implementation before error handling
5. Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup)**:
- T002, T003, T004, T005, T006 can all run in parallel

**Phase 2 (Foundational)**:
- T007, T008, T009, T010 can run in parallel (different models/interfaces)
- T012, T013, T015 can run in parallel (different services)

**Phase 3 (US1)**:
- T017, T018, T019 (tests) can run in parallel
- T020, T021 can run in parallel (different methods in service)

**Phase 4 (US2)**:
- T027, T028, T029, T030 (tests) can run in parallel
- T031, T032, T033 can run in parallel (Wallet Service vs Validator Service)

**Phase 5 (US3)**:
- T040, T041, T042 (tests) can run in parallel
- T043, T044 can run in parallel (different methods)

**Phase 6 (US4)**:
- T050, T051 (tests) can run in parallel
- T052, T053, T054, T055 (Tenant Service) can run in parallel

**Phase 7 (Edge Cases)**:
- T061, T062, T063, T064, T067 can run in parallel (different test files/methods)

**Phase 8 (Polish)**:
- T068, T069, T070, T071, T072, T073, T077 can all run in parallel

---

## Sprint Planning

### Sprint 1: Foundation + US1 (MVP)

**Goal**: Validator can initialize with wallet

**Tasks**: T001-T026 (26 tasks)
**Duration**: 1 sprint (2 weeks)
**Deliverable**: Validator Service retrieves wallet details on startup

**Demo**: Start Validator Service, show logs with wallet ID and address

---

### Sprint 2: US2 (Docket Signing)

**Goal**: Validator signs all dockets

**Tasks**: T027-T039 (13 tasks)
**Duration**: 1 sprint (2 weeks)
**Deliverable**: Dockets contain valid signatures from validator wallet

**Demo**: Build docket, show ProposerSignature in docket JSON

---

### Sprint 3: US3 (Consensus Vote Signing)

**Goal**: Validator signs consensus votes and verifies peer votes

**Tasks**: T040-T049 (10 tasks)
**Duration**: 1 sprint (2 weeks)
**Deliverable**: Consensus votes are signed and verified

**Demo**: Trigger consensus, show signed votes in logs

---

### Sprint 4: US4 (Configuration) + Edge Cases

**Goal**: Admin can configure wallet via Tenant Service, edge cases handled

**Tasks**: T050-T067 (18 tasks)
**Duration**: 1 sprint (2 weeks)
**Deliverable**: Wallet configurable via API, graceful error handling

**Demo**: Update wallet config, restart service, show wallet deletion handling

---

### Sprint 5: Polish & Production Readiness

**Goal**: Production-ready with observability and security

**Tasks**: T068-T077 (10 tasks)
**Duration**: 1 sprint (2 weeks)
**Deliverable**: Full observability, security audit complete, performance validated

**Demo**: Show metrics dashboard, performance test results (10+ dockets/sec)

---

## Parallel Example: User Story 1 (Sprint 1)

```bash
# Launch all tests for User Story 1 together:
Task: "[US1] Unit test for GetWalletDetailsAsync caching" (T017)
Task: "[US1] Unit test for wallet configuration loading" (T018)
Task: "[US1] Integration test for wallet initialization" (T019)

# Launch parallel implementation tasks:
Task: "[US1] Implement GetWalletDetailsAsync with caching" (T020)
Task: "[US1] Implement wallet configuration loader" (T021)
```

---

## Implementation Strategy

### MVP First (Sprint 1: User Story 1 Only)

1. ✅ Complete Phase 1: Setup (T001-T006)
2. ✅ Complete Phase 2: Foundational (T007-T016) - CRITICAL BLOCKER
3. ✅ Complete Phase 3: User Story 1 (T017-T026)
4. **STOP and VALIDATE**: Test wallet initialization independently
5. Deploy/demo wallet initialization

**Value Delivered**: Validator Service can authenticate and retrieve wallet details

---

### Incremental Delivery (Sprint-by-Sprint)

**Sprint 1** (Foundation + US1):
- Setup + Foundational → Foundation ready
- Add US1 → Validator initializes with wallet → **Demo: Show wallet in logs**

**Sprint 2** (US2):
- Add US2 → Dockets are signed → **Demo: Show signed docket**

**Sprint 3** (US3):
- Add US3 → Votes are signed and verified → **Demo: Show consensus with signatures**

**Sprint 4** (US4 + Edge Cases):
- Add US4 → Configurable via API → **Demo: Change wallet via API**
- Add edge cases → Graceful error handling → **Demo: Wallet deletion recovery**

**Sprint 5** (Polish):
- Add observability, security, performance → **Demo: Metrics dashboard, 10+ dockets/sec**

**Each sprint adds value without breaking previous sprints**

---

### Parallel Team Strategy

With multiple developers working in parallel after Foundation (Phase 2):

**After Sprint 1 Foundation Complete**:
- **Developer A**: User Story 1 (T017-T026) - Wallet initialization
- **Developer B**: User Story 4 (T050-T060) - Tenant Service config (independent)

**After User Story 1 Complete**:
- **Developer A**: User Story 2 (T027-T039) - Docket signing
- **Developer B**: User Story 3 (T040-T049) - Vote signing

**Final Sprint**:
- **All developers**: Edge cases + Polish (T061-T077)

---

## Summary

**Total Tasks**: 77 tasks across 8 phases
**Total Sprints**: 5 sprints (10 weeks)

### Task Count by Phase

- Phase 1 (Setup): 6 tasks
- Phase 2 (Foundational): 10 tasks - **CRITICAL BLOCKER**
- Phase 3 (US1 - P1): 10 tasks (7 implementation + 3 tests)
- Phase 4 (US2 - P1): 13 tasks (10 implementation + 3 tests + cleanup)
- Phase 5 (US3 - P2): 10 tasks (7 implementation + 3 tests)
- Phase 6 (US4 - P3): 11 tasks (9 implementation + 2 tests)
- Phase 7 (Edge Cases): 7 tasks
- Phase 8 (Polish): 10 tasks

### Parallel Opportunities

- **Setup phase**: 5 parallel tasks (T002-T006)
- **Foundational phase**: 6 parallel tasks
- **Per user story**: 2-4 parallel tasks (tests, models, separate services)
- **Polish phase**: 7 parallel tasks (T068-T073, T077)

**Total parallelizable tasks**: ~35 tasks (45% of all tasks)

### Independent Test Criteria

- ✅ **US1**: Start service → logs show wallet ID/address → SUCCESS
- ✅ **US2**: Build docket → docket has ProposerSignature → signature verifies → SUCCESS
- ✅ **US3**: Trigger vote → vote has ValidatorSignature → peer verifies → SUCCESS
- ✅ **US4**: Update config via API → restart → uses new wallet → SUCCESS

### Suggested MVP Scope

**Sprint 1 only (US1)**: Validator Service wallet initialization

**Why**: Establishes foundation and proves wallet integration works before adding signing complexity.

---

## Notes

- [P] tasks = different files, can run in parallel
- [Story] label maps task to specific user story (US1, US2, US3, US4)
- Each user story is independently testable
- Tests written FIRST (TDD approach per constitutional requirements)
- Commit after each task or logical group of parallel tasks
- Stop at any sprint checkpoint to validate story independently
- Follow quickstart.md for implementation patterns and troubleshooting

**Constitutional Compliance**: ✅ All tasks align with coding standards, testing requirements (>85% coverage), security guidelines, and architectural principles.

---

**Status**: Ready for Sprint 1 implementation 🚀
