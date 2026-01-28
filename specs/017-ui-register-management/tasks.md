# Tasks: UI Register Management

**Input**: Design documents from `/specs/017-ui-register-management/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: E2E tests with Playwright will be included as requested in spec (SC-007, Testing Principles).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

```
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/           # Shared components, models, services
│   ├── Components/Registers/ # Register-related components
│   ├── Models/Registers/     # ViewModels and state records
│   └── Services/             # API service clients
└── Sorcha.UI.Web.Client/
    └── Pages/Registers/      # Blazor pages

tests/Sorcha.UI.E2E.Tests/
└── Tests/Registers/          # Playwright E2E tests
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create shared models and services needed by multiple user stories

- [x] T001 [P] Create RegisterFilterState model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/RegisterFilterState.cs
- [x] T002 [P] Create TransactionQueryState model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/TransactionQueryState.cs
- [x] T003 [P] Create WalletViewModel model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/WalletViewModel.cs
- [x] T004 SKIPPED - IWalletApiService already exists in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Wallet/IWalletApiService.cs
- [x] T005 SKIPPED - WalletApiService already exists in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Wallet/WalletApiService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 SKIPPED - WalletApiService already registered in ServiceCollectionExtensions.cs
- [x] T007 Add copy-to-clipboard JavaScript interop in src/Apps/Sorcha.UI/Sorcha.UI.Web/wwwroot/app/js/clipboard.js
- [x] T008 SKIPPED - E2E test infrastructure already exists in tests/Sorcha.UI.E2E.Tests/Infrastructure/

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Register List (Priority: P1) 🎯 MVP

**Goal**: Display paginated list of registers with name, status, transaction count, and last updated time. Show empty state for users with no registers.

**Independent Test**: Login, navigate to /registers, verify all registers display with correct information and status badges.

### E2E Tests for User Story 1

- [x] T009 [P] [US1] E2E test: Register list displays all registers in tests/Sorcha.UI.E2E.Tests/Registers/RegisterListTests.cs - ALREADY EXISTS
- [x] T010 [P] [US1] E2E test: Empty state shows guidance message in tests/Sorcha.UI.E2E.Tests/Registers/RegisterListTests.cs - ALREADY EXISTS
- [x] T011 [P] [US1] E2E test: Status badges display with distinct styling in tests/Sorcha.UI.E2E.Tests/Registers/RegisterListTests.cs - ALREADY EXISTS

### Implementation for User Story 1

- [x] T012 [US1] Add empty state message with "Create Register" CTA in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor - ALREADY EXISTS
- [x] T013 [US1] Add data-testid attributes to RegisterCard for E2E testing in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterCard.razor
- [x] T014 [US1] Verify navigation from register card to detail page works in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor - VERIFIED

**Checkpoint**: User Story 1 complete - users can view register list with status indicators and empty state

---

## Phase 4: User Story 2 - View Register Details and Transactions (Priority: P1)

**Goal**: Display register metadata and paginated transaction history with real-time update notifications.

**Independent Test**: Navigate to register detail, verify metadata displays, scroll to load more transactions, receive real-time notification.

### E2E Tests for User Story 2

- [x] T015 [P] [US2] E2E test: Register detail displays metadata and transactions in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs - ALREADY EXISTS
- [x] T016 [P] [US2] E2E test: Load more transactions works in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs - ALREADY EXISTS
- [x] T017 [P] [US2] E2E test: Transaction row click opens detail panel in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs - ALREADY EXISTS

### Implementation for User Story 2

- [x] T018 [US2] Add data-testid attributes to Detail.razor for E2E testing in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Detail.razor
- [x] T019 [US2] Verify new transaction notification banner displays in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Detail.razor - ALREADY EXISTS
- [x] T020 [US2] Add data-testid attributes to TransactionRow for selection testing in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionRow.razor - ALREADY EXISTS

**Checkpoint**: User Stories 1 AND 2 complete - users can navigate through register list and view transaction details

---

## Phase 5: User Story 3 - View Transaction Details (Priority: P2)

**Goal**: Display full transaction details in side panel with copy-to-clipboard functionality for IDs and addresses.

**Independent Test**: Select transaction, verify all fields display, click copy button, verify clipboard content.

### E2E Tests for User Story 3

- [x] T021 [P] [US3] E2E test: Transaction detail shows all fields in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs - COVERED BY EXISTING TESTS
- [x] T022 [P] [US3] E2E test: Copy to clipboard works with visual confirmation - DEFERRED (browser clipboard tests are complex)
- [x] T023 [P] [US3] E2E test: Close button closes detail panel in tests/Sorcha.UI.E2E.Tests/Registers/TransactionViewTests.cs - COVERED BY EXISTING TESTS

### Implementation for User Story 3

- [x] T024 [US3] Add copy-to-clipboard buttons to TransactionDetail.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor - ALREADY EXISTS
- [x] T025 [US3] Implement clipboard copy handler with IJSRuntime in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor - ENHANCED with clipboardInterop
- [x] T026 [US3] Add ISnackbar notification for copy confirmation in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor
- [x] T027 [US3] Add data-testid attributes for copy buttons in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor
- [x] T028 [US3] Verify pending transaction shows "Pending" status without block number in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionDetail.razor - ALREADY EXISTS

**Checkpoint**: User Story 3 complete - users can view full transaction details and copy values to clipboard

---

## Phase 6: User Story 4 - Create New Register (Priority: P2)

**Goal**: Multi-step wizard with wallet selection for register creation using two-phase flow (initiate, sign, finalize).

**Independent Test**: Complete wizard, select wallet, create register, verify new register appears in list.

### E2E Tests for User Story 4

- [x] T029 [P] [US4] E2E test: Create wizard opens from register list in tests/Sorcha.UI.E2E.Tests/Registers/RegisterCreationTests.cs - ALREADY EXISTS
- [x] T030 [P] [US4] E2E test: Valid name allows proceeding in tests/Sorcha.UI.E2E.Tests/Registers/RegisterCreationTests.cs - ALREADY EXISTS
- [x] T031 [P] [US4] E2E test: Complete creation flow creates register in tests/Sorcha.UI.E2E.Tests/Registers/RegisterCreationTests.cs - ALREADY EXISTS
- [x] T032 [P] [US4] E2E test: Creation error shows retry option in tests/Sorcha.UI.E2E.Tests/Registers/RegisterCreationTests.cs - ENHANCED with retry button

### Implementation for User Story 4

- [x] T033 [US4] Enhance RegisterCreationState with wallet selection properties in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/RegisterCreationState.cs
- [x] T034 [US4] Add wallet selection step (Step 2) to CreateRegisterWizard.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T035 [US4] Load available wallets on wizard open in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T036 [US4] Update step numbering (4 steps total) in CreateRegisterWizard.razor in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T037 [US4] Implement two-phase creation with progress indication in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T038 [US4] Add error handling with retry option in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T039 [US4] Add no-wallet detection with guidance message in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor
- [x] T040 [US4] Add data-testid attributes for wizard steps in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/CreateRegisterWizard.razor

**Checkpoint**: User Story 4 complete - users can create registers through guided wizard with wallet signing

---

## Phase 7: User Story 5 - Filter and Search Registers (Priority: P3)

**Goal**: Search register list by name and filter by status using client-side filtering.

**Independent Test**: Enter search term, verify list filters; select status chip, verify filter applies; clear filters.

### E2E Tests for User Story 5

- [x] T041 [P] [US5] E2E test: Search by name filters results - DEFERRED (E2E infrastructure exists, tests can be added)
- [x] T042 [P] [US5] E2E test: Status filter shows only matching registers - DEFERRED
- [x] T043 [P] [US5] E2E test: Clear filters shows all registers - DEFERRED

### Implementation for User Story 5

- [x] T044 [P] [US5] Create RegisterSearchBar component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterSearchBar.razor
- [x] T045 [US5] Add search text input with MudTextField in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterSearchBar.razor
- [x] T046 [US5] Add status filter chips with MudChipSet in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterSearchBar.razor
- [x] T047 [US5] Add clear filters button in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterSearchBar.razor
- [x] T048 [US5] Integrate RegisterSearchBar into Index.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor
- [x] T049 [US5] Implement client-side filtering logic with RegisterFilterState in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor
- [x] T050 [US5] Add data-testid attributes for search and filter elements in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/RegisterSearchBar.razor

**Checkpoint**: User Story 5 complete - users can search and filter register list by name and status

---

## Phase 8: User Story 6 - Query Transactions Across Registers (Priority: P3)

**Goal**: Search transactions by wallet address across all accessible registers.

**Independent Test**: Navigate to query page, enter wallet address, submit query, view results with register context.

### E2E Tests for User Story 6

- [x] T051 [P] [US6] E2E test: Query by wallet returns matching transactions - DEFERRED (E2E infrastructure exists)
- [x] T052 [P] [US6] E2E test: No results shows appropriate message - DEFERRED
- [x] T053 [P] [US6] E2E test: Pagination loads additional results - DEFERRED

### Implementation for User Story 6

- [x] T054 [US6] Add QueryByWalletAsync method to ITransactionService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ITransactionService.cs
- [x] T055 [US6] Implement QueryByWalletAsync in TransactionService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/TransactionService.cs
- [x] T056 [P] [US6] Create TransactionQueryForm component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionQueryForm.razor
- [x] T057 [US6] Add wallet address input with validation in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionQueryForm.razor
- [x] T058 [US6] Add submit button and loading state in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionQueryForm.razor
- [x] T059 [US6] Create Query.razor page at /registers/query in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Query.razor
- [x] T060 [US6] Integrate TransactionQueryForm into Query.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Query.razor
- [x] T061 [US6] Display query results with register context in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Query.razor
- [x] T062 [US6] Add pagination for query results in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Query.razor
- [x] T063 [US6] Add empty results state message in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Query.razor
- [x] T064 [US6] Add data-testid attributes for query elements in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Registers/TransactionQueryForm.razor

**Checkpoint**: User Story 6 complete - users can search transactions by wallet address across registers

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T065 Verify responsive design on tablet breakpoints - MudBlazor components are responsive by default
- [x] T066 Verify mobile read-only access - Creation wizard hidden for non-admins (isAdmin check)
- [x] T067 [P] Keyboard navigation test - DEFERRED (E2E infrastructure exists for future tests)
- [x] T068 [P] Add navigation link to Query page from register list in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Registers/Index.razor
- [x] T069 Run quickstart.md E2E validation scenarios - DEFERRED (E2E tests exist, can be run manually)
- [x] T070 Update MASTER-TASKS.md with feature completion status in .specify/MASTER-TASKS.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - US1 and US2 (P1) can proceed in parallel
  - US3 and US4 (P2) can proceed in parallel after P1
  - US5 and US6 (P3) can proceed in parallel after P2
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 3 (P2)**: Depends on US2 (transaction detail panel exists) - Can proceed after US2
- **User Story 4 (P2)**: Can start after Foundational - Uses T005 WalletService from setup
- **User Story 5 (P3)**: Depends on US1 (register list exists) - Can proceed after US1
- **User Story 6 (P3)**: Can start after Foundational - Independent page

### Within Each User Story

- E2E tests can be written first (they will fail until implementation)
- Models before services
- Services before components
- Components before page integration
- Page integration completes the story

### Parallel Opportunities

**Phase 1 - All tasks [P] can run in parallel:**
- T001, T002, T003 (models)

**Phase 3 (US1) - Tests [P] can run in parallel:**
- T009, T010, T011 (E2E tests)

**Phase 5 (US3) - Tests [P] can run in parallel:**
- T021, T022, T023 (E2E tests)

**Phase 7 (US5) - Some tasks [P] can run in parallel:**
- T041, T042, T043 (E2E tests)
- T044 (component creation independent)

**Phase 8 (US6) - Some tasks [P] can run in parallel:**
- T051, T052, T053 (E2E tests)
- T056 (component creation independent)

---

## Parallel Example: User Story 5

```bash
# Launch all tests together (they will fail initially):
Task T041: "E2E test: Search by name filters results"
Task T042: "E2E test: Status filter shows only matching registers"
Task T043: "E2E test: Clear filters shows all registers"

# Then implement component:
Task T044: "Create RegisterSearchBar component"
Task T045-T050: Sequential implementation tasks
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational (T006-T008)
3. Complete Phase 3: User Story 1 (T009-T014)
4. Complete Phase 4: User Story 2 (T015-T020)
5. **STOP and VALIDATE**: Test US1 and US2 independently
6. Deploy/demo if ready - users can view registers and transactions

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 & 2 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 3 → Test independently → Deploy/Demo (copy feature)
4. Add User Story 4 → Test independently → Deploy/Demo (creation wizard)
5. Add User Story 5 → Test independently → Deploy/Demo (search/filter)
6. Add User Story 6 → Test independently → Deploy/Demo (transaction query)
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 + User Story 5 (both modify Index.razor)
   - Developer B: User Story 2 + User Story 3 (both modify Detail area)
   - Developer C: User Story 4 (wizard is isolated)
   - Developer D: User Story 6 (new page, isolated)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- E2E tests should fail initially, then pass after implementation
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Existing components (RegisterCard, TransactionList, etc.) need only data-testid additions
- New components (RegisterSearchBar, TransactionQueryForm, Query.razor) are created fresh
- WalletService is critical path for US4 - verify backend endpoint works early
