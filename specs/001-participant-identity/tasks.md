# Tasks: Participant Identity Registry

**Input**: Design documents from `/specs/001-participant-identity/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/participant-api.yaml

**Tests**: Target >85% coverage per constitution. Tests included for all phases.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US7) this task belongs to
- Paths are relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project structure and shared enums/models

- [x] T001 [P] Create ParticipantIdentityStatus enum in src/Common/Sorcha.Tenant.Models/ParticipantIdentityStatus.cs
- [x] T002 [P] Create WalletLinkStatus enum in src/Common/Sorcha.Tenant.Models/WalletLinkStatus.cs
- [x] T003 [P] Create ChallengeStatus enum in src/Common/Sorcha.Tenant.Models/ChallengeStatus.cs
- [x] T004 [P] Create ParticipantSearchCriteria model in src/Common/Sorcha.Tenant.Models/ParticipantSearchCriteria.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Data Layer

- [x] T005 Create ParticipantIdentity entity in src/Services/Sorcha.Tenant.Service/Models/ParticipantIdentity.cs
- [x] T006 [P] Create LinkedWalletAddress entity in src/Services/Sorcha.Tenant.Service/Models/LinkedWalletAddress.cs
- [x] T007 [P] Create WalletLinkChallenge entity in src/Services/Sorcha.Tenant.Service/Models/WalletLinkChallenge.cs
- [x] T008 [P] Create ParticipantAuditEntry entity in src/Services/Sorcha.Tenant.Service/Models/ParticipantAuditEntry.cs
- [x] T009 Update TenantDbContext with participant DbSets in src/Services/Sorcha.Tenant.Service/Data/TenantDbContext.cs
- [x] T010 Add entity configurations for ParticipantIdentity (org schema) in TenantDbContext.OnModelCreating
- [x] T011 [P] Add entity configurations for LinkedWalletAddress (public schema) in TenantDbContext.OnModelCreating
- [x] T012 [P] Add entity configurations for WalletLinkChallenge (public schema) in TenantDbContext.OnModelCreating
- [x] T013 Create EF migration AddParticipantIdentity in src/Services/Sorcha.Tenant.Service/Migrations/

### Repository Layer

- [x] T014 Create IParticipantRepository interface in src/Services/Sorcha.Tenant.Service/Data/Repositories/IParticipantRepository.cs
- [x] T015 Implement ParticipantRepository in src/Services/Sorcha.Tenant.Service/Data/Repositories/ParticipantRepository.cs

### DTOs

- [x] T016 [P] Create CreateParticipantRequest DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/CreateParticipantRequest.cs
- [x] T017 [P] Create UpdateParticipantRequest DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/UpdateParticipantRequest.cs
- [x] T018 [P] Create ParticipantResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/ParticipantResponse.cs
- [x] T019 [P] Create ParticipantDetailResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/ParticipantDetailResponse.cs
- [x] T020 [P] Create ParticipantListResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/ParticipantListResponse.cs
- [x] T021 [P] Create InitiateWalletLinkRequest DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/InitiateWalletLinkRequest.cs
- [x] T022 [P] Create VerifyWalletLinkRequest DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/VerifyWalletLinkRequest.cs
- [x] T023 [P] Create WalletLinkChallengeResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/WalletLinkChallengeResponse.cs
- [x] T024 [P] Create LinkedWalletAddressResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/LinkedWalletAddressResponse.cs
- [x] T025 [P] Create ParticipantSearchRequest DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/ParticipantSearchRequest.cs
- [x] T026 [P] Create ParticipantSearchResponse DTO in src/Services/Sorcha.Tenant.Service/Models/Dtos/ParticipantSearchResponse.cs

### Service Interfaces

- [x] T027 Create IParticipantService interface in src/Services/Sorcha.Tenant.Service/Services/IParticipantService.cs
- [x] T028 [P] Create IWalletVerificationService interface in src/Services/Sorcha.Tenant.Service/Services/IWalletVerificationService.cs

### Tests - Repository

- [x] T029 Create ParticipantRepositoryTests in tests/Sorcha.Tenant.Service.Tests/Repositories/ParticipantRepositoryTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Admin Registers Participant (Priority: P1) 🎯 MVP

**Goal**: Organization administrators can register users as participants

**Independent Test**: Admin creates participant for existing user, verifies in directory

### Tests for User Story 1

- [x] T030 [P] [US1] Unit tests for ParticipantService.RegisterAsync in tests/Sorcha.Tenant.Service.Tests/Services/ParticipantServiceTests.cs
- [x] T031 [P] [US1] Endpoint tests for POST /organizations/{orgId}/participants in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 1

- [x] T032 [US1] Implement ParticipantService.RegisterAsync in src/Services/Sorcha.Tenant.Service/Services/ParticipantService.cs
- [x] T033 [US1] Implement ParticipantService.GetByIdAsync in src/Services/Sorcha.Tenant.Service/Services/ParticipantService.cs
- [x] T034 [US1] Implement ParticipantService.ListAsync in src/Services/Sorcha.Tenant.Service/Services/ParticipantService.cs
- [x] T035 [US1] Implement audit logging helper in ParticipantService for Created action
- [x] T036 [US1] Create ParticipantEndpoints.cs with MapParticipantEndpoints in src/Services/Sorcha.Tenant.Service/Endpoints/ParticipantEndpoints.cs
- [x] T037 [US1] Implement POST /organizations/{orgId}/participants endpoint (createParticipant)
- [x] T038 [US1] Implement GET /organizations/{orgId}/participants endpoint (listParticipants)
- [x] T039 [US1] Implement GET /organizations/{orgId}/participants/{id} endpoint (getParticipant)
- [x] T040 [US1] Register ParticipantService and endpoints in Program.cs

**Checkpoint**: Admins can register participants and view them in directory

---

## Phase 4: User Story 2 - Participant Links Wallet Address (Priority: P1) 🎯 MVP

**Goal**: Participants can link wallet addresses with signature verification

**Independent Test**: Participant initiates link, signs challenge, verifies address linked

### Tests for User Story 2

- [x] T041 [P] [US2] Unit tests for WalletVerificationService in tests/Sorcha.Tenant.Service.Tests/Services/WalletVerificationServiceTests.cs
- [x] T042 [P] [US2] Endpoint tests for wallet-links in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 2

- [x] T043 [US2] Implement WalletVerificationService.InitiateLinkAsync in src/Services/Sorcha.Tenant.Service/Services/WalletVerificationService.cs
- [x] T044 [US2] Implement WalletVerificationService.VerifyLinkAsync with signature verification
- [x] T045 [US2] Implement WalletVerificationService.ListLinksAsync
- [x] T046 [US2] Add challenge generation with nonce, timestamp, expiration (5 min)
- [x] T047 [US2] Integrate IWalletServiceClient for signature verification
- [x] T048 [US2] Implement POST /participants/{id}/wallet-links endpoint (initiateWalletLink)
- [x] T049 [US2] Implement POST /participants/{id}/wallet-links/{challengeId}/verify endpoint (verifyWalletLink)
- [x] T050 [US2] Implement GET /participants/{id}/wallet-links endpoint (listWalletLinks)
- [x] T051 [US2] Add audit logging for WalletLinked action
- [x] T052 [US2] Enforce platform-wide wallet address uniqueness constraint

**Checkpoint**: Participants can cryptographically link wallet addresses

---

## Phase 5: User Story 3 - Blueprint Designer Assigns Participant (Priority: P2)

**Goal**: Designers can assign participants to workflow roles

**Independent Test**: Open blueprint, assign participant to role, save and verify

**Note**: This story requires Blueprint Service integration - may be deferred

### Tests for User Story 3

- [x] T053 [P] [US3] Unit tests for participant assignment validation in tests/Sorcha.Tenant.Service.Tests/Services/ParticipantServiceTests.cs

### Implementation for User Story 3

- [x] T054 [US3] Create IParticipantServiceClient interface in src/Common/Sorcha.ServiceClients/Participant/IParticipantServiceClient.cs
- [x] T055 [US3] Implement ParticipantServiceClient in src/Common/Sorcha.ServiceClients/Participant/ParticipantServiceClient.cs
- [x] T056 [US3] Register ParticipantServiceClient in ServiceCollectionExtensions
- [x] T057 [US3] Add GetByIdAsync to client for Blueprint Service integration
- [x] T058 [US3] Add ValidateSigningCapabilityAsync to check wallet link status
- [x] T059 [US3] Add warning response when participant has no linked wallet

**Checkpoint**: Service client ready for Blueprint Service integration

---

## Phase 6: User Story 4 - Self-Registration (Priority: P2)

**Goal**: Users can self-register as participants

**Independent Test**: User navigates to self-registration, completes flow, appears in directory

### Tests for User Story 4

- [x] T060 [P] [US4] Unit tests for SelfRegisterAsync in tests/Sorcha.Tenant.Service.Tests/Services/ParticipantServiceTests.cs
- [x] T061 [P] [US4] Endpoint tests for self-registration in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 4

- [x] T062 [US4] Implement ParticipantService.SelfRegisterAsync (creates participant from current user)
- [x] T063 [US4] Add self-registration endpoint variant (POST without userId uses current user)
- [x] T064 [US4] Handle redirect if user already a participant (returns 409 Conflict)
- [x] T065 [US4] Add inline wallet linking option during self-registration (displayName query param supported)

**Checkpoint**: Users can self-register as participants

---

## Phase 7: User Story 5 - Search and Discover Participants (Priority: P2)

**Goal**: Users can search participants by name, email, wallet address

**Independent Test**: Search by various criteria, verify correct results with org-scoped visibility

### Tests for User Story 5

- [x] T066 [P] [US5] Unit tests for SearchAsync with full-text in tests/Sorcha.Tenant.Service.Tests/Services/ParticipantServiceTests.cs
- [x] T067 [P] [US5] Endpoint tests for search in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 5

- [x] T068 [US5] Implement ParticipantService.SearchAsync with PostgreSQL full-text search
- [x] T069 [US5] Add org-scoped visibility filtering (users see only their orgs)
- [x] T070 [US5] Add admin bypass for cross-org search
- [x] T071 [US5] Implement POST /participants/search endpoint (searchParticipants)
- [x] T072 [US5] Implement GET /participants/by-wallet/{address} endpoint (getParticipantByWallet)
- [x] T073 [US5] Add search index optimization for <2s response

**Checkpoint**: Participant search working with org-scoped visibility

---

## Phase 8: User Story 6 - Manage Wallet Addresses (Priority: P3)

**Goal**: Participants can manage multiple linked addresses, revoke old ones

**Independent Test**: View linked addresses, add new address, revoke existing address

### Tests for User Story 6

- [x] T074 [P] [US6] Unit tests for RevokeLinkAsync in tests/Sorcha.Tenant.Service.Tests/Services/WalletVerificationServiceTests.cs
- [x] T075 [P] [US6] Endpoint tests for revoke in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 6

- [x] T076 [US6] Implement WalletVerificationService.RevokeLinkAsync with soft-delete
- [x] T077 [US6] Implement DELETE /participants/{id}/wallet-links/{linkId} endpoint (revokeWalletLink)
- [x] T078 [US6] Add audit logging for WalletRevoked action
- [x] T079 [US6] Enforce max 10 active addresses per participant
- [x] T080 [US6] Add includeRevoked filter to listWalletLinks

**Checkpoint**: Full wallet address lifecycle management

---

## Phase 9: User Story 7 - Multi-Organization Participant (Priority: P3)

**Goal**: Users can manage participant identities across multiple organizations

**Independent Test**: User in 2 orgs, register in both, view unified profile

### Tests for User Story 7

- [x] T081 [P] [US7] Unit tests for GetMyProfilesAsync in tests/Sorcha.Tenant.Service.Tests/Services/ParticipantServiceTests.cs
- [x] T082 [P] [US7] Endpoint tests for /me/participant-profiles in tests/Sorcha.Tenant.Service.Tests/Endpoints/ParticipantEndpointsTests.cs

### Implementation for User Story 7

- [x] T083 [US7] Implement ParticipantService.GetMyProfilesAsync (all user's participant identities)
- [x] T084 [US7] Implement GET /me/participant-profiles endpoint (getMyParticipantProfiles)
- [x] T085 [US7] Implement wallet address transfer flow (unlink from one org to link in another)
- [x] T086 [US7] Add UI grouping by organization in response

**Checkpoint**: Multi-org participant management complete

---

## Phase 10: UI Components (Blazor WASM)

**Purpose**: User interface for participant management

### UI Service Layer

- [x] T087 [P] Create ParticipantViewModels in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Participants/ParticipantViewModels.cs
- [x] T088 Create ParticipantApiService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Participants/ParticipantApiService.cs

### UI Components

- [x] T089 [P] Create ParticipantList.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/ParticipantList.razor
- [x] T090 [P] Create ParticipantForm.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/ParticipantForm.razor
- [x] T091 [P] Create ParticipantSearch.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/ParticipantSearch.razor
- [x] T092 Create WalletLinkForm.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/WalletLinkForm.razor
- [x] T092a Create ParticipantDetail.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/ParticipantDetail.razor

### UI Pages

- [x] T093 [P] Create Participants Index page in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Participants/Index.razor
- [x] T094 [P] Create Participants Create page (integrated in Index.razor dialog)
- [x] T095 [P] Create Participants Details page in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Participants/Detail.razor
- [x] T096 Create MyProfile section (integrated in Index.razor My Profiles tab)
- [x] T097 Add participant service to DI in ServiceCollectionExtensions.cs

### UI Tests

- [x] T098 UI components compile and build successfully

**Checkpoint**: Full UI for participant management

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, documentation, and quality

- [x] T099 [P] Add XML documentation to all public APIs
- [x] T100 [P] Update CLAUDE.md with participant endpoints
- [x] T101 [P] Update API Gateway routing for participant endpoints in src/Services/Sorcha.ApiGateway/
- [x] T102 Run full test suite and verify >85% coverage (110/110 participant tests passing)
- [x] T103 Validate quickstart.md scenarios work end-to-end
- [x] T104 Performance test search with 10,000 participants (<2s)
- [x] T105 Security review: org-scoped access control, signature verification

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - BLOCKS all user stories
- **Phases 3-9 (User Stories)**: All depend on Phase 2 completion
- **Phase 10 (UI)**: Depends on Phase 3 (US1) minimum; can parallelize with Phases 4-9
- **Phase 11 (Polish)**: Depends on Phases 3-10

### User Story Dependencies

| Story | Priority | Depends On | Can Parallelize With |
|-------|----------|------------|---------------------|
| US1 - Admin Register | P1 | Phase 2 | US2 (after T040) |
| US2 - Wallet Linking | P1 | Phase 2 | US1 (after T032) |
| US3 - Blueprint Assign | P2 | US1, US2 | US4, US5 |
| US4 - Self-Register | P2 | US1 | US3, US5 |
| US5 - Search | P2 | US1 | US3, US4 |
| US6 - Manage Wallets | P3 | US2 | US7 |
| US7 - Multi-Org | P3 | US1 | US6 |

### Within Each User Story

1. Tests written first (fail before implementation)
2. Service implementation
3. Endpoint implementation
4. Integration verification
5. Checkpoint validation

---

## Parallel Execution Examples

### Phase 1 (All Parallel)
```
T001: ParticipantIdentityStatus enum
T002: WalletLinkStatus enum
T003: ChallengeStatus enum
T004: ParticipantSearchCriteria model
```

### Phase 2 - DTOs (All Parallel)
```
T016-T026: All DTO files can be created in parallel
```

### User Story 1 + 2 (After Phase 2)
```
Developer A: T030-T040 (US1 - Admin Registration)
Developer B: T041-T052 (US2 - Wallet Linking)
```

### UI Components (Parallel)
```
T089: ParticipantList.razor
T090: ParticipantForm.razor
T091: ParticipantSearch.razor
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup (4 tasks)
2. Complete Phase 2: Foundational (25 tasks)
3. Complete Phase 3: US1 - Admin Registration (11 tasks)
4. Complete Phase 4: US2 - Wallet Linking (12 tasks)
5. **STOP and VALIDATE**: Test admin registration + wallet linking E2E
6. Deploy MVP

### Incremental Delivery

1. MVP (US1 + US2) → Core participant + wallet linking ✓
2. Add US4 (Self-Registration) → User convenience ✓
3. Add US5 (Search) → Discovery capability ✓
4. Add US3 (Blueprint) → Workflow integration ✓
5. Add US6 + US7 → Full wallet + multi-org management ✓
6. Add UI → Complete user experience ✓

---

## Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| 1 | 4 | Setup - Enums and shared models |
| 2 | 25 | Foundational - Entities, DTOs, repository |
| 3 | 11 | US1 - Admin registration (P1 MVP) |
| 4 | 12 | US2 - Wallet linking (P1 MVP) |
| 5 | 7 | US3 - Blueprint integration (P2) |
| 6 | 6 | US4 - Self-registration (P2) |
| 7 | 8 | US5 - Search (P2) |
| 8 | 7 | US6 - Wallet management (P3) |
| 9 | 6 | US7 - Multi-org (P3) |
| 10 | 12 | UI Components |
| 11 | 7 | Polish |
| **Total** | **105** | |

**MVP Scope**: Phases 1-4 (52 tasks) - Admin registration + wallet linking
**Full Feature**: All phases (105 tasks)
