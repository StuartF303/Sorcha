# Tasks: Fix Wallet Dashboard and Navigation Bugs

**Input**: Design documents from `/specs/033-fix-wallet-dashboard-bugs/`
**Prerequisites**: plan.md (complete), spec.md (complete), research.md (complete), data-model.md (complete), contracts/ (complete)

**Tests**: E2E tests and unit tests are included per the testing requirements in plan.md (Phase 2C and 2D)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to repository root `C:\projects\Sorcha\`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify environment and branch setup

- [x] T001 Verify branch 033-fix-wallet-dashboard-bugs is checked out and up to date
- [x] T002 Verify Docker services are running via `docker-compose ps`
- [x] T003 [P] Run baseline tests to confirm no pre-existing failures: `dotnet test --filter "FullyQualifiedName~Sorcha.UI"`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Review existing code to understand current implementation before making changes

**⚠️ CRITICAL**: Complete this phase before ANY user story work begins

- [x] T004 Review existing dashboard wizard logic in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor` lines 180-189 **NOTE: IsLoaded check ALREADY present!**
- [x] T005 [P] Review DashboardStatsViewModel structure in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Dashboard/DashboardStatsViewModel.cs`
- [x] T006 [P] Review MyWallet navigation logic in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyWallet.razor` line 132-135 **BUG CONFIRMED: absolute path used**
- [x] T007 [P] Review existing Playwright E2E test patterns in `tests/Sorcha.UI.E2E.Tests/Infrastructure/AuthenticatedDockerTestBase.cs` **Found in Infrastructure/ not Fixtures/**
- [x] T008 Verify base href configuration in `src/Apps/Sorcha.UI/Sorcha.UI.Web/wwwroot/app/index.html` line 7

**Checkpoint**: Foundation reviewed - user story implementation can now begin

---

## Phase 3: User Story 1 - Dashboard Loads Without Recurring Wizard (Priority: P1) 🎯 MVP

**Goal**: Fix dashboard wizard logic so it only shows when user truly has no wallets, preventing the loop bug

**Independent Test**: Create a wallet, return to dashboard, verify wizard doesn't reappear. Refresh page, verify wizard still doesn't reappear. This story is complete when dashboard loads correctly after wallet creation without re-triggering the wizard.

### E2E Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (TDD approach)**

- [x] T009 [P] [US1] Create E2E test file `tests/Sorcha.UI.E2E.Tests/Tests/WalletDashboardTests.cs` with NUnit and Playwright setup ✅ **COMPLETE**
- [x] T010 [P] [US1] Implement `FirstLogin_NoWallets_ShowsWizard()` test - verify wizard appears for new user ✅ **COMPLETE**
- [x] T011 [P] [US1] Implement `AfterWalletCreation_DashboardLoads_WizardDoesNotReappear()` test - verify wizard doesn't loop ✅ **COMPLETE**
- [x] T012 [P] [US1] Implement `ExistingWallet_DashboardLoad_SkipsWizard()` test - verify returning user sees dashboard ✅ **COMPLETE**
- [x] T013 [P] [US1] Implement `StatsFailToLoad_DashboardLoads_DoesNotRedirectToWizard()` test - verify graceful degradation ✅ **COMPLETE**

### Unit Tests for User Story 1

- [x] T014 [P] [US1] Create unit test file `tests/Sorcha.UI.Core.Tests/Services/DashboardServiceTests.cs` with xUnit setup ✅ **COMPLETE**
- [x] T015 [P] [US1] Implement `GetDashboardStatsAsync_Success_ReturnsStatsWithIsLoadedTrue()` test ✅ **COMPLETE**
- [x] T016 [P] [US1] Implement `GetDashboardStatsAsync_ApiFailure_ReturnsIsLoadedFalse()` test ✅ **COMPLETE**
- [x] T017 [P] [US1] Implement `GetDashboardStatsAsync_Exception_ReturnsIsLoadedFalse()` test ✅ **COMPLETE**

### Implementation for User Story 1

- [x] T018 [US1] Modify `OnInitializedAsync()` in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor` line 185 to add `_stats.IsLoaded` check before redirecting to wizard ✅ **ALREADY PRESENT IN CODEBASE**
- [x] T019 [US1] Add error state display in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor` after line 168 to show alert when stats fail to load (optional improvement per plan.md Phase 2A) ✅ **COMPLETE**
- [ ] T020 [US1] Run E2E tests for US1 to verify all 4 scenarios pass: `dotnet test --filter "FullyQualifiedName~WalletDashboardTests"` ⚠️ **REQUIRES DOCKER (T002)**
- [x] T021 [US1] Run unit tests for US1 to verify DashboardService logic: `dotnet test --filter "FullyQualifiedName~DashboardServiceTests"` ✅ **COMPLETE - 4/4 PASSED**
- [ ] T022 [US1] Manual testing: Follow quickstart.md "Step 2: Test Dashboard Wizard" to verify first-time user flow works

**Checkpoint**: User Story 1 is complete and independently testable. Dashboard wizard loop bug is fixed. Verify by creating wallet and confirming wizard doesn't reappear.

---

## Phase 4: User Story 2 - Wallet Navigation Works Correctly (Priority: P2)

**Goal**: Fix navigation URLs in MyWallet page to include proper `/app/` prefix so wallet detail pages load correctly

**Independent Test**: From "My Activity - My Wallet", click a wallet card, verify URL is `/app/wallets/{address}` and page loads. Bookmark URL, navigate away, return via bookmark, verify page still loads. This story is complete when wallet navigation uses correct URLs with base href.

### E2E Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T023 [P] [US2] Create E2E test file `tests/Sorcha.UI.E2E.Tests/Tests/WalletNavigationTests.cs` with NUnit and Playwright setup ✅ **COMPLETE**
- [x] T024 [P] [US2] Implement `MyWallet_ClickWallet_NavigatesToCorrectUrl()` test - verify URL includes `/app/` prefix ✅ **COMPLETE**
- [x] T025 [P] [US2] Implement `MyWallet_ClickWallet_PageLoadsSuccessfully()` test - verify wallet detail page displays ✅ **COMPLETE**
- [x] T026 [P] [US2] Implement `WalletDetailUrl_DirectAccess_PageLoads()` test - verify bookmarked URLs work ✅ **COMPLETE**

### Implementation for User Story 2

- [x] T027 [US2] Modify `NavigateToWallet()` method in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyWallet.razor` line 134 to use relative path (remove leading `/`) ✅ **COMPLETE - BUG FIXED!**
- [ ] T028 [US2] Run E2E tests for US2 to verify all 3 navigation scenarios pass: `dotnet test --filter "FullyQualifiedName~WalletNavigationTests"` ⚠️ **REQUIRES DOCKER (T002)**
- [ ] T029 [US2] Manual testing: Follow quickstart.md "Step 3: Test Wallet Navigation" to verify URL format and bookmark functionality **REQUIRES DOCKER (T002)**

**Checkpoint**: User Story 2 is complete and independently testable. Wallet navigation URLs are correct. Both User Stories 1 AND 2 should work independently.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and cleanup

- [x] T030 [P] Run full UI test suite to check for regressions: `dotnet test --filter "FullyQualifiedName~Sorcha.UI"` ✅ **COMPLETE - 494/499 PASSED (5 pre-existing YAML failures)**
- [ ] T031 [P] Run full solution build to ensure no warnings: `dotnet build /warnaserror`
- [ ] T032 Perform manual end-to-end walkthrough per `specs/033-fix-wallet-dashboard-bugs/quickstart.md` covering all 5 scenarios
- [ ] T033 [P] Update CLAUDE.md if any new patterns discovered (e.g., Blazor navigation conventions)
- [ ] T034 [P] Verify Docker containers build and run: `docker-compose build sorcha-ui && docker-compose up -d sorcha-ui`
- [ ] T035 Review git diff to ensure only intended files modified (Home.razor, MyWallet.razor, test files)
- [ ] T036 Prepare commit message following format from CLAUDE.md with Co-Authored-By tag

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-4)**: Both depend on Foundational phase completion
  - User Story 1 (P1) can proceed after Phase 2
  - User Story 2 (P2) can proceed after Phase 2 in parallel with US1
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories ✅ Independent
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories ✅ Independent

**Key Insight**: Both user stories are FULLY INDEPENDENT - they modify different files and can be worked on in parallel by different developers.

### Within Each User Story

**User Story 1 Task Order:**
1. T009-T017: Write ALL tests first (can run in parallel)
2. T018-T019: Implementation (sequential, depends on tests being written)
3. T020-T022: Validation (sequential, depends on implementation)

**User Story 2 Task Order:**
1. T023-T026: Write ALL tests first (can run in parallel)
2. T027: Implementation (depends on tests being written)
3. T028-T029: Validation (sequential, depends on implementation)

### Parallel Opportunities

**Phase 1 (Setup):**
- T002 and T003 can run in parallel

**Phase 2 (Foundational):**
- T005, T006, T007 can all run in parallel (different files)

**After Phase 2 Completes:**
- **User Story 1 and User Story 2 can proceed IN PARALLEL** (different developers, different files)

**Within User Story 1:**
- T009-T013 (E2E tests) can all run in parallel
- T014-T017 (unit tests) can all run in parallel

**Within User Story 2:**
- T023-T026 (E2E tests) can all run in parallel

**Phase 5 (Polish):**
- T030, T031, T033, T034 can all run in parallel

---

## Parallel Example: User Story 1

```bash
# After Phase 2 completes, launch all E2E tests for User Story 1 together:
Task: "Create E2E test file tests/Sorcha.UI.E2E.Tests/Tests/WalletDashboardTests.cs"
Task: "Implement FirstLogin_NoWallets_ShowsWizard() test"
Task: "Implement AfterWalletCreation_DashboardLoads_WizardDoesNotReappear() test"
Task: "Implement ExistingWallet_DashboardLoad_SkipsWizard() test"
Task: "Implement StatsFailToLoad_DashboardLoads_DoesNotRedirectToWizard() test"

# Launch all unit tests for User Story 1 together (in parallel with E2E tests):
Task: "Create unit test file tests/Sorcha.UI.Core.Tests/Services/DashboardServiceTests.cs"
Task: "Implement GetDashboardStatsAsync_Success_ReturnsStatsWithIsLoadedTrue() test"
Task: "Implement GetDashboardStatsAsync_ApiFailure_ReturnsIsLoadedFalse() test"
Task: "Implement GetDashboardStatsAsync_Exception_ReturnsIsLoadedFalse() test"
```

## Parallel Example: Both User Stories

```bash
# After Phase 2 completes, these can run in parallel (different developers):

# Developer A: User Story 1 (Dashboard wizard fix)
- Work on T009-T022 sequentially within US1

# Developer B: User Story 2 (Navigation URL fix)
- Work on T023-T029 sequentially within US2

# Both stories are independent and don't conflict!
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T008) - CRITICAL review phase
3. Complete Phase 3: User Story 1 (T009-T022)
4. **STOP and VALIDATE**:
   - Run E2E tests: `dotnet test --filter "FullyQualifiedName~WalletDashboardTests"`
   - Manual test: Create wallet, verify no wizard loop
5. **MVP READY**: Dashboard wizard bug is fixed, can be deployed

### Incremental Delivery

1. Complete Setup + Foundational → Ready to start implementation
2. Add User Story 1 → Test independently → **Deploy/Demo MVP** (critical bug fixed!)
3. Add User Story 2 → Test independently → **Deploy/Demo** (navigation improved)
4. Each story adds value without breaking previous work

### Parallel Team Strategy

With 2 developers available:

1. **Both devs together**: Complete Setup (Phase 1) + Foundational (Phase 2)
2. **After Phase 2 completes**:
   - **Developer A**: User Story 1 (T009-T022) - Dashboard wizard fix
   - **Developer B**: User Story 2 (T023-T029) - Navigation URL fix
3. Stories complete independently and can be tested/merged separately
4. **Advantage**: Both bugs fixed simultaneously instead of sequentially

### Single Developer Strategy

With 1 developer:

1. Complete Setup (Phase 1)
2. Complete Foundational (Phase 2)
3. **Priority order**: User Story 1 first (P1 - critical bug)
4. Then User Story 2 (P2 - important but not blocking)
5. Each story is a natural checkpoint for commit/PR

---

## Risk Mitigation

### High-Risk Tasks

- **T018** (Home.razor modification): CRITICAL - this is the main bug fix
  - Mitigation: Write E2E tests first (T009-T013) to catch regressions
  - Mitigation: Manual testing mandatory (T022)

- **T027** (MyWallet.razor modification): Medium risk - URL format change
  - Mitigation: Write E2E tests first (T023-T026) to verify URLs
  - Mitigation: Test bookmarked URLs (T026)

### Validation Checkpoints

After each user story:
- Run automated tests
- Perform manual testing per quickstart.md
- Verify no regressions in other areas
- Can commit and create PR at this point

---

## Testing Coverage

### E2E Tests (Playwright)

- **User Story 1**: 4 test scenarios (T010-T013)
  - First-time user flow
  - Wizard loop prevention
  - Existing user flow
  - Error handling

- **User Story 2**: 3 test scenarios (T024-T026)
  - URL format validation
  - Page load verification
  - Bookmark functionality

**Total E2E Tests**: 7 comprehensive scenarios

### Unit Tests (xUnit)

- **User Story 1**: 3 test cases (T015-T017)
  - Success case
  - API failure case
  - Exception case

**Total Unit Tests**: 3 service layer tests

### Manual Testing

- Quickstart guide provides 5 detailed scenarios
- Covers edge cases not easily automated
- Required before final sign-off

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [US1] and [US2] labels map tasks to specific user stories for traceability
- Both user stories are independently completable and testable
- **TDD Approach**: Write tests FIRST for each user story (T009-T017 for US1, T023-T026 for US2)
- Verify tests FAIL before implementing fixes
- Stop at any checkpoint to validate story independently
- Commit after each user story completion
- Both stories modify different files - zero merge conflicts expected
- Total LOC changes: ~50-100 lines across 2 Razor files + ~200-300 lines of test code
- Estimated time:
  - US1: 3-4 hours (tests + implementation + validation)
  - US2: 2-3 hours (tests + implementation + validation)
  - Polish: 1-2 hours (full validation + documentation)
  - **Total**: 6-9 hours for complete feature

---

## Success Criteria

Upon completion of all tasks:

- ✅ Dashboard wizard only shows when user truly has no wallets
- ✅ Dashboard wizard does not reappear after wallet creation
- ✅ API failures don't cause false redirects to wizard
- ✅ Wallet navigation URLs include `/app/` prefix
- ✅ Wallet detail pages load correctly
- ✅ Bookmarked URLs work as expected
- ✅ 7 E2E tests pass
- ✅ 3 unit tests pass
- ✅ Manual testing scenarios all pass
- ✅ No regressions in other UI areas
- ✅ Docker build succeeds
- ✅ Code follows Sorcha conventions (license header, naming, etc.)

---

**Task Count**: 36 tasks total
- Phase 1 (Setup): 3 tasks
- Phase 2 (Foundational): 5 tasks
- Phase 3 (User Story 1): 14 tasks (5 E2E tests + 4 unit tests + 5 implementation)
- Phase 4 (User Story 2): 7 tasks (4 E2E tests + 3 implementation)
- Phase 5 (Polish): 7 tasks

**Parallel Opportunities**: 18 tasks marked [P] can run in parallel within their phases
**Independent Stories**: 2 user stories that can be developed in parallel by different team members
**MVP Scope**: User Story 1 only (14 tasks) fixes the critical dashboard wizard loop bug
