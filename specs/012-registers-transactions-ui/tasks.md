# Tasks: Registers and Transactions UI

**Input**: Design documents from `/specs/012-registers-transactions-ui/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: E2E Playwright tests included as specified in plan.md. Unit tests (bUnit) included for components.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

- **UI Core Components**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/`
- **UI Core Services**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/`
- **UI Pages**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/`
- **Unit Tests**: `tests/Sorcha.UI.Core.Tests/Components/Registers/`
- **E2E Tests**: `tests/Sorcha.UI.E2E.Tests/Registers/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create directory structure and project files for the Registers feature

- [x] T001 Create Registers component directory at src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/
- [x] T002 Create Registers page directory at src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/
- [x] T003 [P] Create unit test directory at tests/Sorcha.UI.Core.Tests/Components/Registers/
- [x] T004 [P] Create E2E test directory at tests/Sorcha.UI.E2E.Tests/Registers/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core services and infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create RegisterViewModel record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/RegisterViewModel.cs
- [x] T006 [P] Create TransactionViewModel record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/TransactionViewModel.cs
- [x] T007 [P] Create TransactionListResponse record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/TransactionListResponse.cs
- [x] T008 [P] Create ConnectionState record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/ConnectionState.cs
- [x] T009 Implement IRegisterService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IRegisterService.cs
- [x] T010 Implement RegisterService HTTP client in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/RegisterService.cs
- [x] T011 [P] Implement ITransactionService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ITransactionService.cs
- [x] T012 Implement TransactionService HTTP client in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/TransactionService.cs
- [x] T013 Implement RegisterHubConnection SignalR manager in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/RegisterHubConnection.cs
- [x] T014 Register services in DI container in src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Registers List (Priority: P1) 🎯 MVP

**Goal**: Organization participants can view a list of available registers with status, height, and last update time

**Independent Test**: Navigate to /registers page, verify registers list loads with correct information displayed

### Tests for User Story 1

- [ ] T015 [P] [US1] Create RegisterCardTests.cs in tests/Sorcha.UI.Core.Tests/Components/Registers/RegisterCardTests.cs
- [ ] T016 [P] [US1] Create RegisterListTests.cs E2E test in tests/Sorcha.UI.E2E.Tests/Registers/RegisterListTests.cs

### Implementation for User Story 1

- [x] T017 [P] [US1] Create RegisterStatusBadge.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterStatusBadge.razor
- [x] T018 [P] [US1] Create RegisterCard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterCard.razor
- [x] T019 [US1] Create Registers/Index.razor page in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor
- [x] T020 [US1] Add loading state and error handling to Index.razor
- [x] T021 [US1] Add empty state message when no registers exist

**Checkpoint**: At this point, User Story 1 should be fully functional - users can view registers list

---

## Phase 4: User Story 2 - View Transactions List (Priority: P2)

**Goal**: Users can view transactions within a register sorted newest first with virtual scrolling

**Independent Test**: Click on a register, verify transaction list loads with correct columns and infinite scroll works

### Tests for User Story 2

- [ ] T022 [P] [US2] Create TransactionRowTests.cs in tests/Sorcha.UI.Core.Tests/Components/Registers/TransactionRowTests.cs
- [ ] T023 [P] [US2] Create TransactionListTests.cs in tests/Sorcha.UI.Core.Tests/Components/Registers/TransactionListTests.cs
- [ ] T024 [P] [US2] Create TransactionViewTests.cs E2E test in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs

### Implementation for User Story 2

- [x] T025 [P] [US2] Create TransactionRow.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionRow.razor
- [x] T026 [US2] Create TransactionList.razor component with MudVirtualize in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionList.razor
- [x] T027 [US2] Create Registers/Detail.razor page in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Detail.razor
- [x] T028 [US2] Add pagination/infinite scroll loading to TransactionList.razor
- [x] T029 [US2] Add back navigation from Detail.razor to Index.razor
- [x] T030 [US2] Add empty state for registers with no transactions

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Real-Time Transaction Updates (Priority: P3)

**Goal**: New transactions appear automatically without page refresh via SignalR

**Independent Test**: Submit transaction via CLI, verify it appears in UI within 1 second without refresh

### Tests for User Story 3

- [ ] T031 [P] [US3] Create RegisterHubConnectionTests.cs in tests/Sorcha.UI.Core.Tests/Services/RegisterHubConnectionTests.cs

### Implementation for User Story 3

- [x] T032 [P] [US3] Create RealTimeIndicator.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RealTimeIndicator.razor
- [x] T033 [US3] Integrate RegisterHubConnection into Detail.razor for real-time updates
- [x] T034 [US3] Add new transaction highlight animation in TransactionRow.razor
- [x] T035 [US3] Add "New transactions available" notification when scrolled down
- [x] T036 [US3] Implement auto-reconnection logic in RegisterHubConnection.cs
- [x] T037 [US3] Display connection status indicator in Detail.razor header

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently

---

## Phase 6: User Story 4 - View Transaction Details (Priority: P4)

**Goal**: Users can select a transaction and view complete details in a lower panel

**Independent Test**: Click on a transaction row, verify detail panel shows all fields correctly

### Tests for User Story 4

- [ ] T038 [P] [US4] Create TransactionDetailTests.cs in tests/Sorcha.UI.Core.Tests/Components/Registers/TransactionDetailTests.cs

### Implementation for User Story 4

- [x] T039 [US4] Create TransactionDetail.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor
- [x] T040 [US4] Integrate TransactionDetail into Detail.razor with selection state
- [x] T041 [US4] Add close button to dismiss detail panel
- [x] T042 [US4] Display all transaction fields: ID, sender, recipients, timestamp, block, signature, payload count
- [x] T043 [US4] Add copy-to-clipboard for transaction ID and addresses

**Checkpoint**: At this point, User Stories 1-4 should all work independently

---

## Phase 7: User Story 5 - Create Register (Priority: P5, Admin Only)

**Goal**: Administrators can create new registers through a guided wizard

**Independent Test**: Login as admin, click Create Register, complete wizard, verify register appears in list

### Tests for User Story 5

- [ ] T044 [P] [US5] Create CreateRegisterWizardTests.cs in tests/Sorcha.UI.Core.Tests/Components/Registers/CreateRegisterWizardTests.cs
- [ ] T045 [P] [US5] Create RegisterCreationTests.cs E2E test in tests/Sorcha.UI.E2E.Tests/Registers/RegisterCreationTests.cs

### Implementation for User Story 5

- [x] T046 [P] [US5] Create RegisterCreationRequest record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/RegisterCreationRequest.cs (in IRegisterService.cs)
- [x] T047 [P] [US5] Create RegisterCreationState record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/RegisterCreationState.cs
- [x] T048 [US5] Add initiate/finalize methods to RegisterService.cs
- [x] T049 [US5] Create CreateRegisterWizard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T050 [US5] Implement wizard step 1: Enter register name (validation 1-38 chars)
- [x] T051 [US5] Implement wizard step 2: Configure options (advertise, full replica)
- [x] T052 [US5] Implement wizard step 3: Confirm and create
- [x] T053 [US5] Add admin role check to conditionally show Create Register button
- [x] T054 [US5] Integrate CreateRegisterWizard into Index.razor with MudDialog

**Checkpoint**: All user stories should now be independently functional

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T055 [P] Add responsive design breakpoints to all components (already implemented in TransactionRow, TransactionList)
- [ ] T056 [P] Add keyboard navigation support (arrow keys, Enter to select)
- [x] T057 Code cleanup and refactoring across all components
- [ ] T058 Performance optimization for 100K+ transaction lists
- [x] T059 [P] Add logging for register and transaction operations (RegisterService, TransactionService, RegisterHubConnection)
- [ ] T060 Run quickstart.md validation scenarios
- [x] T061 Update MainLayout.razor navigation styling for active state (MudNavLink handles automatically)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4 → P5)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Uses same RegisterService as US1 but independent
- **User Story 3 (P3)**: Depends on US2 (needs TransactionList component to integrate SignalR)
- **User Story 4 (P4)**: Depends on US2 (needs TransactionList component for selection)
- **User Story 5 (P5)**: Can start after Foundational (Phase 2) - Independent admin feature

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- ViewModels/Models before services
- Services before components
- Components before pages
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, US1, US2, and US5 can start in parallel
- US3 and US4 must wait for US2's TransactionList component
- All tests for a user story marked [P] can run in parallel
- Components within a story marked [P] can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all parallel model creation:
Task: "Create RegisterViewModel record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/RegisterViewModel.cs"
Task: "Create TransactionViewModel record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/TransactionViewModel.cs"
Task: "Create TransactionListResponse record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/TransactionListResponse.cs"
Task: "Create ConnectionState record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/ConnectionState.cs"

# Then launch interface definitions:
Task: "Implement IRegisterService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IRegisterService.cs"
Task: "Implement ITransactionService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ITransactionService.cs"
```

## Parallel Example: User Story 1 and User Story 5

```bash
# US1 and US5 can run in parallel after Phase 2:

# US1 components:
Task: "Create RegisterStatusBadge.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterStatusBadge.razor"
Task: "Create RegisterCard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterCard.razor"

# US5 models (parallel with US1):
Task: "Create RegisterCreationRequest record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/RegisterCreationRequest.cs"
Task: "Create RegisterCreationState record in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/RegisterCreationState.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 - View Registers List
4. **STOP and VALIDATE**: Test registers list independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (can view transactions)
4. Add User Story 3 → Test independently → Deploy/Demo (real-time updates!)
5. Add User Story 4 → Test independently → Deploy/Demo (transaction details)
6. Add User Story 5 → Test independently → Deploy/Demo (admin can create registers)
7. Each story adds value without breaking previous stories

### Recommended Sequence

For single developer:
1. Phase 1 → Phase 2 → US1 → US2 → US3 → US4 → US5 → Phase 8

For two developers:
1. Both: Phase 1 + Phase 2
2. Dev A: US1 → US2 → US3 → US4
3. Dev B: US5 → Phase 8 (polish)
4. Merge and test together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Navigation already exists in MainLayout.razor (lines 77-79) - no new nav link needed
- All backend APIs already exist in Register Service - this is a pure UI feature
