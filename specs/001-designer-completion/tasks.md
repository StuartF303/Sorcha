# Tasks: Blueprint Designer Completion

**Input**: Design documents from `/specs/001-designer-completion/`
**Prerequisites**: plan.md, spec.md, data-model.md, quickstart.md, research.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US5)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and directory structure

- [x] T001 Create Designer subdirectory in src/Apps/Sorcha.Admin/Components/Designer/
- [x] T002 [P] Create Models subdirectory in src/Apps/Sorcha.Admin/Models/
- [x] T003 [P] Create test directory in tests/Sorcha.Admin.Tests/Components/Designer/
- [x] T004 [P] Add YamlDotNet package to Sorcha.Admin project

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models and service interfaces that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create ParticipantModel.cs in src/Apps/Sorcha.Admin/Models/ParticipantModel.cs
- [x] T006 [P] Create ConditionModel.cs and ConditionClause.cs in src/Apps/Sorcha.Admin/Models/ConditionModel.cs
- [x] T007 [P] Create CalculationModel.cs and CalculationElement.cs in src/Apps/Sorcha.Admin/Models/CalculationModel.cs
- [x] T008 [P] Create SyncQueueItem.cs in src/Apps/Sorcha.Admin/Models/SyncQueueItem.cs
- [x] T009 [P] Create BlueprintExportModel.cs in src/Apps/Sorcha.Admin/Models/BlueprintExportModel.cs
- [x] T010 [P] Create ImportValidationResult.cs in src/Apps/Sorcha.Admin/Models/ImportValidationResult.cs
- [x] T011 Create IBlueprintStorageService interface in src/Apps/Sorcha.Admin/Services/IBlueprintStorageService.cs
- [x] T012 Create IOfflineSyncService interface in src/Apps/Sorcha.Admin/Services/IOfflineSyncService.cs
- [x] T013 [P] Create BlueprintSerializationService.cs (JSON/YAML) in src/Apps/Sorcha.Admin/Services/BlueprintSerializationService.cs
- [x] T014 Register new services in DI container in Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Manage Blueprint Participants (Priority: P1) - BD-022

**Goal**: Enable designers to add, edit, and remove participants with wallet addresses and roles

**Independent Test**: Add participants with wallet addresses and roles, verify they appear in blueprint JSON

### Tests for User Story 1

- [x] T015 [P] [US1] Create ParticipantEditorTests.cs in tests/Sorcha.Admin.Tests/Components/Designer/ParticipantEditorTests.cs

### Implementation for User Story 1

- [x] T016 [P] [US1] Create WalletSelectorDialog.razor in src/Apps/Sorcha.Admin/Components/Designer/WalletSelectorDialog.razor
- [x] T017 [US1] Create ParticipantEditor.razor dialog in src/Apps/Sorcha.Admin/Components/Designer/ParticipantEditor.razor
- [x] T018 [US1] Create ParticipantList.razor component in src/Apps/Sorcha.Admin/Components/Designer/ParticipantList.razor
- [x] T019 [US1] Integrate ParticipantEditor into Designer.razor (replace "Coming soon!" snackbar)
- [x] T020 [US1] Add participant list display to PropertiesPanel.razor
- [x] T021 [US1] Implement wallet address validation in ParticipantEditor.razor
- [x] T022 [US1] Add role selection dropdown with Initiator, Approver, Observer, Member roles

**Checkpoint**: User Story 1 complete - designers can manage blueprint participants

---

## Phase 4: User Story 2 - Build Routing Conditions Visually (Priority: P2) - BD-023

**Goal**: Enable designers to build routing conditions with visual clause builder

**Independent Test**: Build a multi-clause condition using visual builder, verify generated JSON Logic

### Tests for User Story 2

- [x] T023 [P] [US2] Create ConditionEditorTests.cs in tests/Sorcha.Admin.Tests/Components/Designer/ConditionEditorTests.cs

### Implementation for User Story 2

- [x] T024 [US2] Create ConditionClauseComponent.razor in src/Apps/Sorcha.Admin/Components/Designer/ConditionClause.razor
- [x] T025 [US2] Create ConditionEditor.razor dialog in src/Apps/Sorcha.Admin/Components/Designer/ConditionEditor.razor
- [x] T026 [US2] Implement field selector dropdown based on action schema in ConditionEditor.razor
- [x] T027 [US2] Implement operator selector (==, !=, >, <, >=, <=, contains, startsWith, endsWith)
- [x] T028 [US2] Implement AND/OR logic toggle between clauses
- [x] T029 [US2] Implement JSON Logic generation from visual model (ToJsonLogic method)
- [x] T030 [US2] Implement JSON Logic parsing to visual model (FromJsonLogic method)
- [x] T031 [US2] Add target participant selector to ConditionEditor.razor
- [x] T032 [US2] Integrate ConditionEditor into Designer.razor (replace "Coming soon!" snackbar)
- [x] T033 [US2] Add condition preview panel showing generated JSON Logic

**Checkpoint**: User Story 2 complete - designers can visually build routing conditions

---

## Phase 5: User Story 3 - Export and Import Blueprints (Priority: P3) - BD-024

**Goal**: Enable blueprint export to JSON/YAML and import from files

**Independent Test**: Export blueprint, close designer, import file, verify blueprint restored correctly

### Tests for User Story 3

- [x] T034 [P] [US3] Create ExportImportTests.cs in tests/Sorcha.Admin.Tests/Components/Designer/ExportImportTests.cs

### Implementation for User Story 3

- [x] T035 [US3] Implement JSON serialization in BlueprintSerializationService.cs
- [x] T036 [US3] Implement YAML serialization using YamlDotNet in BlueprintSerializationService.cs
- [x] T037 [US3] Implement JSON deserialization with validation in BlueprintSerializationService.cs
- [x] T038 [US3] Implement YAML deserialization with validation in BlueprintSerializationService.cs
- [x] T039 [US3] Create ExportDialog.razor in src/Apps/Sorcha.Admin/Components/Designer/ExportDialog.razor
- [x] T040 [US3] Create ImportDialog.razor in src/Apps/Sorcha.Admin/Components/Designer/ImportDialog.razor
- [x] T041 [US3] Implement file download logic for JSON/YAML export
- [x] T042 [US3] Implement file upload and parsing for import
- [x] T043 [US3] Add validation error display in ImportDialog with specific messages
- [x] T044 [US3] Integrate Export/Import buttons into Designer.razor toolbar
- [x] T045 [US3] Create sample blueprints in wwwroot/sample-blueprints/ for testing

**Checkpoint**: User Story 3 complete - blueprints can be exported and imported as files

---

## Phase 6: User Story 4 - Save Blueprints to Server (Priority: P4) - BD-025

**Goal**: Enable server-side persistence with offline queue support

**Independent Test**: Save blueprint, clear browser data, log back in, verify blueprint retrieved from server

### Tests for User Story 4

- [x] T046 [P] [US4] Create BlueprintStorageServiceTests.cs in tests/Sorcha.Admin.Tests/Components/Designer/BlueprintStorageServiceTests.cs

### Implementation for User Story 4

- [x] T047 [US4] Implement BlueprintStorageService with server API calls in src/Apps/Sorcha.Admin/Services/BlueprintStorageService.cs
- [x] T048 [US4] Implement OfflineSyncService with LocalStorage queue in src/Apps/Sorcha.Admin/Services/OfflineSyncService.cs
- [x] T049 [US4] Add connectivity detection and automatic sync trigger
- [x] T050 [US4] Update Designer.razor Save button to use BlueprintStorageService
- [x] T051 [US4] Update Blueprints.razor page to load from server instead of LocalStorage only
- [x] T052 [US4] Create OfflineSyncIndicator.razor component in src/Apps/Sorcha.Admin/Components/Designer/OfflineSyncIndicator.razor
- [x] T053 [US4] Implement LocalStorage to server migration on first load
- [x] T054 [US4] Add sync status display in designer toolbar
- [x] T055 [US4] Implement conflict resolution with user notification
- [x] T056 [US4] Add manual sync retry button for failed syncs

**Checkpoint**: User Story 4 complete - blueprints persist to server with offline support

---

## Phase 7: User Story 5 - Build Calculated Field Expressions (Priority: P5)

**Goal**: Enable designers to define calculated fields with visual expression builder

**Independent Test**: Define calculated field using visual builder, verify calculation works in form preview

### Implementation for User Story 5

- [x] T057 [US5] Create CalculationEditor.razor dialog in src/Apps/Sorcha.Admin/Components/Designer/CalculationEditor.razor
- [x] T058 [US5] Implement field reference selector for numeric fields
- [x] T059 [US5] Implement arithmetic operator selector (+, -, *, /)
- [x] T060 [US5] Implement constant value input
- [x] T061 [US5] Implement parentheses grouping for order of operations
- [x] T062 [US5] Implement expression preview with test values panel
- [x] T063 [US5] Implement JSON Logic generation from calculation model
- [x] T064 [US5] Integrate CalculationEditor into action form configuration

**Checkpoint**: User Story 5 complete - designers can define calculated field expressions

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements affecting multiple user stories

- [x] T065 [P] Add comprehensive error handling and user feedback across all dialogs
- [x] T066 [P] Add loading states for all async operations
- [x] T067 Add keyboard navigation and accessibility support to editors
- [x] T068 Performance optimization for large blueprints (50+ actions)
- [x] T069 [P] Add clear local cache option in settings for post-migration cleanup
- [x] T070 Run quickstart.md validation scenarios end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - Participants for routing
- **User Story 2 (P2)**: Can start after Foundational - Can reference US1 participants
- **User Story 3 (P3)**: Can start after Foundational - Independent of US1/US2
- **User Story 4 (P4)**: Can start after Foundational - Independent persistence
- **User Story 5 (P5)**: Can start after Foundational - Extension of US2 concepts

### Parallel Opportunities

**Phase 1** (directories parallel):
- T002, T003, T004 parallel with T001

**Phase 2** (models in parallel):
- T006, T007, T008, T009, T010 (all models parallel)
- T013 parallel with interfaces

**Phase 3-7** (tests can run parallel within each story):
- T015 parallel with T016
- T023 parallel with T024
- T034 parallel with T035
- T046 parallel with T047

**Cross-Story Parallelism** (with sufficient team capacity):
- US1, US3, US4 can run in parallel after Foundational
- US2 can reference US1 participants (optional dependency)
- US5 extends US2 concepts but is independent

---

## Parallel Example: Foundational Models

```bash
# Launch all models together:
Task: "Create ConditionModel.cs in src/.../Models/ConditionModel.cs"
Task: "Create CalculationModel.cs in src/.../Models/CalculationModel.cs"
Task: "Create SyncQueueItem.cs in src/.../Models/SyncQueueItem.cs"
Task: "Create BlueprintExportModel.cs in src/.../Models/BlueprintExportModel.cs"
Task: "Create ImportValidationResult.cs in src/.../Models/ImportValidationResult.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 4 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Participant Editor)
4. Complete Phase 6: User Story 4 (Server Persistence)
5. **STOP and VALIDATE**: Test participant management and server save
6. Deploy/demo if ready - essential designer features complete

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → **MVP: Participant management**
3. Add User Story 4 → **MVP+: Server persistence**
4. Add User Story 2 → Visual condition builder
5. Add User Story 3 → Export/Import
6. Add User Story 5 → Calculated fields
7. Polish phase → Production-ready

### Suggested MVP Scope

**User Stories 1 + 4** deliver:
- Participant editor with wallet selection
- Role assignment (Initiator, Approver, Observer, Member)
- Server-side blueprint persistence
- Offline queue with automatic sync
- LocalStorage migration

This completes the designer's critical gaps while additional visual editors can be added incrementally.

---

## Summary

| Phase | Tasks | Parallel Opportunities |
|-------|-------|----------------------|
| Setup | 4 | 3 |
| Foundational | 10 | 7 |
| US1 (P1) BD-022 | 8 | 2 |
| US2 (P2) BD-023 | 11 | 1 |
| US3 (P3) BD-024 | 12 | 1 |
| US4 (P4) BD-025 | 11 | 1 |
| US5 (P5) | 8 | 0 |
| Polish | 6 | 3 |
| **Total** | **70** | **18** |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- BD-022: Participant Editor (US1)
- BD-023: Condition Editor (US2) + Calculation Editor (US5)
- BD-024: Export/Import (US3)
- BD-025: Backend Integration (US4)
- YamlDotNet required for YAML serialization
- JSON Logic for conditions/calculations (existing dependency)
- Offline sync uses Blazored.LocalStorage (existing dependency)
