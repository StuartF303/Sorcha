# Tasks: Peer Network Management & Observability

**Input**: Design documents from `/specs/024-peer-network-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/peer-management-api.md

**Tests**: Included — spec requires unit tests for all new service methods and endpoints.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Entity changes and shared DTOs that multiple user stories depend on

- [x] T001 Add IsBanned (bool), BannedAt (DateTimeOffset?), BanReason (string?) properties to PeerNode in `src/Services/Sorcha.Peer.Service/Core/PeerNode.cs`
- [x] T002 Update PeerDbContext to map IsBanned, BannedAt, BanReason columns on PeerNodeEntity in `src/Services/Sorcha.Peer.Service/data/PeerDbContext.cs`
- [x] T003 Create EF Core migration for ban columns on peer.Peers table via `dotnet ef migrations add AddPeerBanFields`
- [x] T004 [P] Add response DTOs (AvailableRegisterInfo, BanResponse, ResetResponse, SubscribeResponse, UnsubscribeResponse, PurgeResponse) in a new file `src/Services/Sorcha.Peer.Service/Models/PeerManagementDtos.cs`
- [x] T005 [P] Add IsBanned, BannedAt, BanReason, QualityScore, QualityRating, AdvertisedRegisters fields to PeerInfo DTO in `src/Apps/Sorcha.Cli/Models/Peer.cs`

**Checkpoint**: PeerNode entity updated, migration ready, shared DTOs available for all stories.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core service methods that MUST be complete before endpoint/CLI/UI work

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Implement BanPeerAsync(string peerId, string? reason) in PeerListManager — set IsBanned=true, BannedAt=now, BanReason=reason, persist to DB, log warning if seed node in `src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs`
- [x] T007 Implement UnbanPeerAsync(string peerId) in PeerListManager — set IsBanned=false, BannedAt=null, BanReason=null, persist to DB in `src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs`
- [x] T008 Implement ResetFailureCountAsync(string peerId) returning previous count in PeerListManager — set FailureCount=0, persist to DB in `src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs`
- [x] T009 Update GetHealthyPeers() in PeerListManager to exclude banned peers (IsBanned == true) in `src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs`
- [x] T010 Implement GetNetworkAdvertisedRegisters() in RegisterAdvertisementService — aggregate AdvertisedRegisters across all known peers from PeerListManager.GetAllPeers(), count peers per register, track max versions, filter to IsPublic only, return List<AvailableRegisterInfo> in `src/Services/Sorcha.Peer.Service/Replication/RegisterAdvertisementService.cs`
- [x] T011 [P] Add unit tests for BanPeerAsync, UnbanPeerAsync, ResetFailureCountAsync, and GetHealthyPeers ban exclusion in `tests/Sorcha.Peer.Service.Tests/Discovery/PeerListManagerTests.cs`
- [x] T012 [P] Add unit tests for GetNetworkAdvertisedRegisters in `tests/Sorcha.Peer.Service.Tests/Replication/RegisterAdvertisementServiceTests.cs`

**Checkpoint**: Foundation ready — all service methods tested, user story implementation can begin.

---

## Phase 3: User Story 1 — View Peer Network State (Priority: P1) 🎯 MVP

**Goal**: Operators can view complete peer network state (peers with quality, registers, ban status) via REST API, CLI, and UI.

**Independent Test**: Start peer service, call `GET /api/peers` and verify response includes isBanned, qualityScore, qualityRating, advertisedRegisters for each peer. Run `sorcha peer list` and verify enhanced output. Open UI Peer Service tab and verify Network Overview panel shows all peer data.

### Implementation

- [x] T013 [US1] Enhance GET /api/peers endpoint to include isBanned, qualityScore, qualityRating, advertisedRegisterCount, advertisedRegisters per peer — join PeerListManager.GetAllPeers() with ConnectionQualityTracker.GetAllQualities() in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T014 [US1] Enhance GET /api/peers/{peerId} endpoint to include ban status, full quality details, and advertised registers in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T015 [US1] Add GET /api/peers/quality endpoint — return ConnectionQualityTracker.GetAllQualities() as JSON in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T016 [P] [US1] Add GetQualityScoresAsync() Refit method to IPeerServiceClient in `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs`
- [x] T017 [P] [US1] Add ConnectionQualityInfo DTO and SubscriptionInfo DTO to CLI models in `src/Apps/Sorcha.Cli/Models/Peer.cs`
- [x] T018 [US1] Add `peer quality` subcommand — call GetQualityScoresAsync, render table with PeerId, QualityScore, QualityRating, AvgLatency, SuccessRate using Spectre.Console in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T019 [US1] Enhance PeerServiceAdmin.razor — add MudTabs with "Network Overview" as first tab, replace existing peer table with enhanced MudDataGrid showing PeerId, Address, Latency, QualityScore, QualityRating, AdvertisedRegisterCount, IsBanned columns, add summary cards (total peers, healthy peers, avg quality, banned count) at top in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`
- [x] T020 [P] [US1] Add PeerQualityInfo and EnhancedPeerInfo response models to UI models in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Admin/HealthResponse.cs`
- [x] T021 [US1] Add endpoint tests for GET /api/peers (enhanced), GET /api/peers/{peerId} (enhanced), GET /api/peers/quality in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`

**Checkpoint**: User Story 1 complete — peer network state visible across all 3 surfaces.

---

## Phase 4: User Story 2 — Monitor Register Subscriptions & Replication Progress (Priority: P1)

**Goal**: Operators can view register subscriptions with mode, sync state, progress, and errors via REST API, CLI, and UI.

**Independent Test**: With active subscriptions, call `GET /api/registers/subscriptions` and verify response includes mode, syncState, progressPercent. Run `sorcha peer subscriptions` and verify table output. Open UI and verify "Register Subscriptions" tab shows progress bars.

### Implementation

- [x] T022 [US2] Enhance GET /api/registers/subscriptions endpoint response to include all RegisterSubscription fields (mode, syncState, syncProgressPercent, lastSyncedDocketVersion, lastSyncedTransactionVersion, totalDocketsInChain, canParticipateInValidation, isReceiving, lastSyncAt, consecutiveFailures, errorMessage) — delegate to RegisterSyncBackgroundService.GetSubscriptions() in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T023 [P] [US2] Add GetSubscriptionsAsync() Refit method to IPeerServiceClient in `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs`
- [x] T024 [US2] Add `peer subscriptions` subcommand — call GetSubscriptionsAsync, render table with RegisterId, Mode, SyncState, Progress%, LastSync using Spectre.Console, show progress bar for full-replica syncs in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T025 [US2] Add "Register Subscriptions" tab to PeerServiceAdmin.razor — MudDataGrid with RegisterId, Mode, SyncState, Progress (MudProgressLinear), LastSync, Errors columns, color-code Error state rows red in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`
- [x] T026 [US2] Add endpoint test for GET /api/registers/subscriptions (enhanced response) in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`

**Checkpoint**: User Story 2 complete — subscription monitoring visible across all 3 surfaces.

---

## Phase 5: User Story 3 — Subscribe to Registers from Other Peers (Priority: P2)

**Goal**: Operators can discover available registers and subscribe to them via REST API, CLI, and UI.

**Independent Test**: With peers advertising registers, call `GET /api/registers/available` to see available registers. Call `POST /api/registers/{registerId}/subscribe` with mode and verify subscription created. Run `sorcha peer subscribe` and verify. Use UI "Available Registers" tab to subscribe.

### Implementation

- [x] T027 [US3] Add GET /api/registers/available endpoint — call RegisterAdvertisementService.GetNetworkAdvertisedRegisters(), return AvailableRegisterInfo list in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T028 [US3] Add POST /api/registers/{registerId}/subscribe endpoint — parse SubscribeRequest body, validate mode ("forward-only"→ForwardOnly, "full-replica"→FullReplica), check not already subscribed (409), check register exists in network advertisements (404), delegate to RegisterSyncBackgroundService.SubscribeToRegisterAsync(), require [Authorize], return 201 with subscription state in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T029 [P] [US3] Add GetAvailableRegistersAsync() and SubscribeToRegisterAsync(string registerId, SubscribeRequest request) Refit methods to IPeerServiceClient in `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs`
- [x] T030 [P] [US3] Add AvailableRegisterInfo and SubscribeRequest DTOs to CLI models in `src/Apps/Sorcha.Cli/Models/Peer.cs`
- [x] T031 [US3] Add `peer subscribe --register-id <id> --mode <forward-only|full-replica>` subcommand — validate mode, call SubscribeToRegisterAsync, display result in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T032 [US3] Add "Available Registers" tab to PeerServiceAdmin.razor — MudDataGrid with RegisterId, PeerCount, LatestVersion, LatestDocketVersion, FullReplicaPeerCount columns, "Subscribe" button per row that opens dialog to select mode, call subscribe endpoint on confirm in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`
- [x] T033 [US3] Add endpoint tests for GET /api/registers/available, POST /api/registers/{registerId}/subscribe (success 201, duplicate 409, not found 404, invalid mode 400) in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`

**Checkpoint**: User Story 3 complete — register discovery and subscription via all 3 surfaces.

---

## Phase 6: User Story 4 — Unsubscribe from a Register (Priority: P2)

**Goal**: Operators can unsubscribe from registers and optionally purge cached data via REST API, CLI, and UI.

**Independent Test**: With an active subscription, call `DELETE /api/registers/{registerId}/subscribe` and verify unsubscribed with cache retained. Call with `?purge=true` and verify cache deleted. Call `DELETE /api/registers/{registerId}/cache` for standalone purge.

### Implementation

- [x] T034 [US4] Add DELETE /api/registers/{registerId}/subscribe endpoint — parse optional ?purge=true query param, check subscription exists (404), delegate to RegisterSyncBackgroundService.UnsubscribeFromRegisterAsync(), if purge also clear cache, require [Authorize], return UnsubscribeResponse in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T035 [US4] Add DELETE /api/registers/{registerId}/cache endpoint — standalone purge of cached data for a register, check cache exists (404), count removed transactions/dockets, require [Authorize], return PurgeResponse in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T036 [P] [US4] Add UnsubscribeFromRegisterAsync(string registerId, bool purge) and PurgeCacheAsync(string registerId) Refit methods to IPeerServiceClient in `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs`
- [x] T037 [US4] Add `peer unsubscribe --register-id <id> [--purge]` subcommand — call UnsubscribeFromRegisterAsync, display result noting cache retention unless --purge in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T038 [US4] Add "Unsubscribe" button to Register Subscriptions tab rows in PeerServiceAdmin.razor — confirmation dialog noting cache retention, optional purge checkbox in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`
- [x] T039 [US4] Add endpoint tests for DELETE /api/registers/{registerId}/subscribe (success, with purge, not found 404), DELETE /api/registers/{registerId}/cache (success, not found 404) in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`

**Checkpoint**: User Story 4 complete — unsubscribe and purge via all 3 surfaces.

---

## Phase 7: User Story 5 — View Peer Reputation & Network Quality (Priority: P3)

**Goal**: Operators can view per-peer reputation scores with quality breakdown via REST API, CLI, and UI.

**Independent Test**: Call `GET /api/peers/quality` and verify each peer has qualityScore, qualityRating, latency breakdown, success rate. Run `sorcha peer quality` and verify ranked table. Open UI "Peer Quality" tab and verify quality breakdown.

### Implementation

- [x] T040 [US5] Add "Peer Quality" tab to PeerServiceAdmin.razor — summary cards showing quality distribution (count per rating), MudDataGrid with PeerId, QualityScore, QualityRating, AvgLatency, MinLatency, MaxLatency, SuccessRate, TotalRequests columns, sort by QualityScore descending, color badges per rating in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`

**Checkpoint**: User Story 5 complete — peer quality visible in UI (REST and CLI already done in US1 T015/T018).

---

## Phase 8: User Story 6 — Manage Peer Reputation (Priority: P3)

**Goal**: Operators can ban/unban peers and reset failure counts via REST API, CLI, and UI.

**Independent Test**: Call `POST /api/peers/{peerId}/ban` and verify peer excluded from GetHealthyPeers. Call `DELETE /api/peers/{peerId}/ban` and verify restored. Call `POST /api/peers/{peerId}/reset` and verify failure count zeroed.

### Implementation

- [x] T041 [US6] Add POST /api/peers/{peerId}/ban endpoint — parse optional BanRequest body, check peer exists (404), check not already banned (409), delegate to PeerListManager.BanPeerAsync(), require [Authorize], return BanResponse in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T042 [US6] Add DELETE /api/peers/{peerId}/ban endpoint — check peer exists (404), check is banned (409), delegate to PeerListManager.UnbanPeerAsync(), require [Authorize], return unban response in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T043 [US6] Add POST /api/peers/{peerId}/reset endpoint — check peer exists (404), delegate to PeerListManager.ResetFailureCountAsync(), require [Authorize], return ResetResponse with previous count in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T044 [P] [US6] Add BanPeerAsync, UnbanPeerAsync, ResetFailureCountAsync Refit methods to IPeerServiceClient in `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs`
- [x] T045 [P] [US6] Add BanRequest, BanResponse, ResetResponse DTOs to CLI models in `src/Apps/Sorcha.Cli/Models/Peer.cs`
- [x] T046 [US6] Add `peer ban --peer-id <id> [--reason <text>]` subcommand — call BanPeerAsync, display ban confirmation in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T047 [US6] Add `peer reset --peer-id <id>` subcommand — call ResetFailureCountAsync, display previous and new failure count in `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs`
- [x] T048 [US6] Add ban/unban and reset action buttons to Peer Quality tab rows in PeerServiceAdmin.razor — ban button opens dialog for optional reason, unban button with confirmation, reset button with confirmation in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor`
- [x] T049 [US6] Add endpoint tests for POST /api/peers/{peerId}/ban (success, not found 404, already banned 409), DELETE /api/peers/{peerId}/ban (success, not found 404, not banned 409), POST /api/peers/{peerId}/reset (success, not found 404) in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`

**Checkpoint**: User Story 6 complete — peer management actions via all 3 surfaces.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Integration validation, documentation, and cleanup

- [x] T050 [P] Add OpenAPI tags and summaries to all new endpoints (Monitoring, Management, Registers groups) via .WithTags()/.WithSummary()/.WithDescription() in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T051 [P] Add XML documentation comments to all new public methods (BanPeerAsync, UnbanPeerAsync, ResetFailureCountAsync, GetNetworkAdvertisedRegisters) in service files
- [x] T052 Verify all management endpoints (ban, unban, reset, subscribe, unsubscribe, purge) require [Authorize] and return 401 for unauthenticated requests — add auth test cases in `tests/Sorcha.Peer.Service.Tests/Endpoints/PeerManagementEndpointTests.cs`
- [x] T053 Run full Peer Service test suite (`dotnet test tests/Sorcha.Peer.Service.Tests`) and verify no regressions from baseline (433 pass / 29 pre-existing fail)
- [x] T054 [P] Run CLI build (`dotnet build src/Apps/Sorcha.Cli`) and verify no compilation errors
- [x] T055 [P] Run UI build (`dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core`) and verify no compilation errors
- [x] T056 Update MASTER-TASKS.md with 024-peer-network-management completion status in `.specify/MASTER-TASKS.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (entity changes must exist before service methods)
- **US1 (Phase 3)**: Depends on Phase 2 — enhanced peer endpoints need quality data and ban fields
- **US2 (Phase 4)**: Depends on Phase 2 — subscription endpoint enhancement needs foundation
- **US3 (Phase 5)**: Depends on Phase 2 (GetNetworkAdvertisedRegisters) and US2 (subscription display)
- **US4 (Phase 6)**: Depends on US3 (must subscribe before unsubscribing)
- **US5 (Phase 7)**: Depends on US1 (quality endpoint and CLI already built in US1)
- **US6 (Phase 8)**: Depends on Phase 2 (ban/unban/reset service methods)
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Independence

- **US1 + US2**: Both P1, can run in parallel after Phase 2
- **US3**: Depends on US2 (subscription monitoring should exist before adding subscribe)
- **US4**: Depends on US3 (must be able to subscribe before unsubscribing)
- **US5**: Can run in parallel with US3/US4 (quality display is read-only)
- **US6**: Can run in parallel with US3/US4 (ban management is independent of subscribe flow)

### Within Each User Story

- Endpoints before CLI (CLI calls endpoints via Refit)
- CLI Refit methods before CLI commands
- Endpoint + CLI before UI (UI calls same endpoints)
- Tests alongside or after endpoint implementation

### Parallel Opportunities

Phase 1: T004 and T005 can run in parallel (different projects)
Phase 2: T011 and T012 can run in parallel (different test files)
Phase 3: T016, T017, T20 can run in parallel (different files/projects)
Phase 5: T029, T030 can run in parallel (different files)
Phase 8: T044, T045 can run in parallel (different files)
Phase 9: T050, T051, T054, T055 can all run in parallel

---

## Parallel Example: User Story 1

```
# After Phase 2 is complete, launch parallel tasks:
Task T016: "Add GetQualityScoresAsync() Refit method"  (CLI services file)
Task T017: "Add ConnectionQualityInfo DTO"              (CLI models file)
Task T020: "Add PeerQualityInfo UI models"              (UI models file)

# Then sequential tasks that depend on above:
Task T013: "Enhance GET /api/peers endpoint"            (Program.cs)
Task T014: "Enhance GET /api/peers/{peerId} endpoint"   (Program.cs)
Task T015: "Add GET /api/peers/quality endpoint"        (Program.cs)
Task T018: "Add peer quality CLI subcommand"            (PeerCommands.cs - needs T016, T017)
Task T019: "Enhance PeerServiceAdmin.razor"             (UI - needs T020)
Task T021: "Add endpoint tests"                         (tests - needs T013-T015)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (entity changes, DTOs)
2. Complete Phase 2: Foundational (service methods + tests)
3. Complete Phase 3: US1 — View Peer Network State
4. Complete Phase 4: US2 — Monitor Register Subscriptions
5. **STOP and VALIDATE**: Peer network fully observable across all 3 surfaces
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Entity and service methods ready
2. Add US1 + US2 → Network state + subscriptions observable (MVP!)
3. Add US3 + US4 → Register subscribe/unsubscribe management
4. Add US5 + US6 → Peer quality and reputation management
5. Polish → Documentation, auth hardening, final validation

### Suggested MVP Scope

**Phases 1-4 (T001-T026)**: 26 tasks delivering full network observability — the most impactful subset. Operators can see everything but management actions (subscribe/ban/reset) come in the next increment.

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | **56** |
| Phase 1: Setup | 5 |
| Phase 2: Foundational | 7 |
| Phase 3: US1 — View Peer Network State | 9 |
| Phase 4: US2 — Monitor Subscriptions | 5 |
| Phase 5: US3 — Subscribe to Registers | 7 |
| Phase 6: US4 — Unsubscribe from Registers | 6 |
| Phase 7: US5 — View Peer Quality | 1 |
| Phase 8: US6 — Manage Peer Reputation | 9 |
| Phase 9: Polish | 7 |
| **Parallelizable Tasks** | **18** |
| **Files Modified** | ~12 |
| **Files Created** | 2 (PeerManagementDtos.cs, PeerManagementEndpointTests.cs) |

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- YARP gateway already covers needed routes — no gateway changes required
- ConnectionQualityTracker and RegisterSyncBackgroundService already have needed data/methods — tasks focus on endpoint wiring
