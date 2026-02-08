# Tasks: Blueprint Template Library & Ping-Pong Blueprint

**Input**: Design documents from `/specs/027-blueprint-template-library/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md
**Branch**: `027-blueprint-template-library`

**Tests**: Included per plan.md (unit tests for cycle warning behavior, seeding idempotency, template loading).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Ping-Pong blueprint JSON template and establish the foundation all stories depend on.

- [x] T001 Create Ping-Pong blueprint template JSON file at `examples/templates/ping-pong-template.json` per data-model.md — two participants (ping, pong), two actions in a cycle with default routes, dataSchemas requiring message (string) and counter (integer)
- [x] T002 Run existing Blueprint Service test suite (`tests/Sorcha.Blueprint.Service.Tests`) to establish baseline pass count before any code changes — record in commit message

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modify cycle detection to warn instead of reject — this MUST be complete before any cyclic blueprint can be published

**CRITICAL**: Without this change, the Ping-Pong blueprint cannot be published (cycle detection rejects it)

- [x] T003 Modify `PublishService.ValidateBlueprint()` in `src/Services/Sorcha.Blueprint.Service/Program.cs` (lines ~1813-1905) — separate cycle detection results from other validation errors. Cycles produce warnings (not errors) and set `metadata["hasCycles"] = "true"` on the blueprint. Other validation rules (participant refs, action count) remain as hard errors
- [x] T004 Add `Warnings` property to the publish response model or create a publish result wrapper that carries both the blueprint ID and any warnings. Update the publish endpoint response to include warnings in the JSON body when cycles are detected (200 OK with warnings, not 400 Bad Request)
- [x] T005 Write unit tests for cycle warning behavior in `tests/Sorcha.Blueprint.Service.Tests/` — test that: (a) a blueprint with cyclic routes publishes successfully with warnings, (b) a blueprint with cyclic routes gets `hasCycles` metadata set, (c) a blueprint without cycles publishes with no warnings, (d) a blueprint with other validation errors (e.g., missing participants) still fails even if it also has cycles
- [x] T006 Run full Blueprint Service test suite to confirm no regressions from cycle detection change — compare against baseline from T002

**Checkpoint**: Cyclic blueprints can now be published. All existing tests still pass.

---

## Phase 3: User Story 1 — Run Ping-Pong End-to-End (Priority: P1) MVP

**Goal**: Publish the Ping-Pong blueprint and execute 5+ round-trips through the full action submission pipeline (validate -> calculate -> route -> disclose).

**Independent Test**: Create instance from Ping-Pong blueprint, submit Ping action (message + counter=1), submit Pong response (message + counter=2), repeat 5 times. All submissions succeed and route correctly.

### Implementation for User Story 1

- [x] T007 [US1] Create `walkthroughs/PingPong/test-ping-pong-workflow.ps1` walkthrough script — Phase 1: authenticate, read Ping-Pong blueprint JSON from `examples/templates/ping-pong-template.json`, create blueprint via `POST /api/blueprints/`, publish via `POST /api/blueprints/{id}/publish` (verify cycle warning in response, not error)
- [x] T008 [US1] Extend walkthrough script — Phase 2: create instance via `POST /api/instances/` with two participant wallets mapped to ping and pong roles, verify instance is created with `CurrentActionIds=[0]`
- [x] T009 [US1] Extend walkthrough script — Phase 3: execute 5 full Ping-Pong round-trips (10 action submissions total). For each round: Ping submits `POST /api/instances/{id}/actions/0/execute` with `{message, counter}`, verify nextActions routes to action 1 (pong); Pong submits `POST /api/instances/{id}/actions/1/execute`, verify nextActions routes back to action 0 (ping). Increment counter each submission (1, 2, 3, ... 10)
- [x] T010 [US1] Extend walkthrough script — Phase 4: verify payload integrity by fetching the instance state and confirming all 10 submissions are recorded with correct counter values. Print summary table of all round-trips
- [x] T011 [US1] Run walkthrough script against Docker services to verify full end-to-end pipeline. Debug and fix any issues discovered

**Checkpoint**: Ping-Pong blueprint publishes (with cycle warning), instance creation works, 5 round-trips execute successfully with correct routing and payload preservation.

---

## Phase 4: User Story 3 — Ship Pre-Built Templates with Installation (Priority: P3)

**Goal**: Templates are automatically loaded into the Blueprint Service at startup without manual intervention. Seeding is idempotent.

**Independent Test**: Start Blueprint Service fresh, verify Ping-Pong + 3 existing templates appear in `GET /api/templates/` listing without manual upload. Restart service, verify no duplicates.

**Note**: Implementing US3 before US2/US4 because the UI pages (US2/US4) depend on templates being available via the API.

### Implementation for User Story 3

- [x] T012 [US3] Create `TemplateSeedingService` hosted service at `src/Services/Sorcha.Blueprint.Service/Templates/TemplateSeedingService.cs` — implements `IHostedService`, reads template JSON files from embedded resources or well-known directory path at startup. For each template: check if ID already exists via `IBlueprintTemplateService.GetTemplateAsync()`, if not present call `SaveTemplateAsync()` to seed it. Load all 4 templates: ping-pong + 3 existing examples (approval-workflow, loan-application, supply-chain) from `examples/templates/`
- [x] T013 [US3] Register `TemplateSeedingService` in DI in `src/Services/Sorcha.Blueprint.Service/Program.cs` — add `builder.Services.AddHostedService<TemplateSeedingService>()`. Ensure service has access to `IBlueprintTemplateService` and `ILogger<TemplateSeedingService>`
- [x] T014 [US3] Add optional `POST /api/templates/seed` admin endpoint in `src/Services/Sorcha.Blueprint.Service/Program.cs` — calls the seeding logic manually. Returns `{ seeded: N, skipped: N, errors: [] }`. Apply admin authorization policy
- [x] T015 [US3] Write unit tests for template seeding in `tests/Sorcha.Blueprint.Service.Tests/` — test that: (a) seeding loads all 4 templates when store is empty, (b) seeding is idempotent (second run seeds 0, skips 4), (c) seeding handles missing template files gracefully with logged errors, (d) seeding does not overwrite user-modified templates (checks by ID only)
- [x] T016 [US3] Run Blueprint Service test suite to confirm seeding tests pass and no regressions

**Checkpoint**: Templates are auto-seeded at startup. Repeated restarts don't create duplicates. Admin can manually trigger re-seed.

---

## Phase 5: User Story 2 — Browse and Select Templates (Priority: P2)

**Goal**: Users can navigate to a template library page, see all available templates with names/descriptions/categories, and view detailed structure (participants, actions, data schemas) for any template.

**Independent Test**: Navigate to `/templates` page, verify template cards appear with correct details. Click Ping-Pong template, see participant roles (Ping, Pong) and action flow preview.

### Implementation for User Story 2

- [x] T017 [P] [US2] Create `Templates.razor` page at `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Templates.razor` — routable at `/templates`. Compose existing `TemplateList` component from Sorcha.UI.Core for the main template grid. Add page title and description header
- [x] T018 [US2] Add template detail panel to `Templates.razor` — when a template is selected from the list, display a detail view showing: template title, description, category, author, tags, list of participants with names and descriptions, list of actions with titles and sender roles, data schema fields. Use MudBlazor card/expansion panel components
- [x] T019 [US2] Add nav menu link for Templates page in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Layout/NavMenu.razor` — add a `MudNavLink` with icon, label "Templates", and href "/templates". Place after existing Blueprint nav links
- [x] T020 [US2] Add empty state handling to `Templates.razor` — when no templates are available, show a MudAlert with message "No templates available. Contact your administrator to seed the template library."
- [x] T021 [US2] Build UI projects to verify zero warnings/errors — `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/`

**Checkpoint**: Template library page is accessible, shows all seeded templates with detail view. Empty state handled gracefully.

---

## Phase 6: User Story 4 — Instantiate Blueprint from Template (Priority: P2)

**Goal**: Users can select a template and create a new workflow instance by assigning real participants to template roles. System creates a published blueprint and starts the workflow.

**Independent Test**: Select Ping-Pong template, assign two participant identities to Ping and Pong roles, confirm instance is created with initial Ping action pending. Verify validation rejects incomplete participant assignment.

### Implementation for User Story 4

- [x] T022 [US4] Add "Use Template" button and instance creation flow to `Templates.razor` — clicking "Use Template" on the detail panel opens a dialog or inline form for participant assignment. Show template participant roles (Ping, Pong) with input fields for wallet addresses or participant identifiers
- [x] T023 [US4] Implement template-to-instance creation logic in `Templates.razor` code-behind — call `POST /api/templates/evaluate` with template ID and empty parameters, then `POST /api/blueprints/` with returned blueprint JSON, then `POST /api/blueprints/{id}/publish`, then `POST /api/instances/` with participant wallet mappings. Show progress/status for each step. Handle cycle warnings gracefully (display as info, not error)
- [x] T024 [US4] Add validation to participant assignment — require all template roles to be filled before allowing creation. Show validation message "All participant roles must be assigned" when user tries to submit with empty fields. Validate wallet address format if applicable
- [x] T025 [US4] Add success/error feedback — on successful instance creation, show success message with instance ID and link to instance view. On failure, show error details from API response. Handle specific errors: publish cycle warning (info), validation errors (error), network failures (retry suggestion)
- [x] T026 [US4] Build UI projects to verify zero warnings/errors — `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/`

**Checkpoint**: Users can go from template browsing to running instance in a single flow. Validation prevents incomplete submissions.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation, and cleanup across all stories

- [x] T027 Docker rebuild and full walkthrough execution — rebuild Blueprint Service container with template seeding, verify templates are available via API after container startup, run Ping-Pong walkthrough script end-to-end against fresh Docker deployment
- [x] T028 [P] Verify all 4 templates appear correctly — `GET /api/templates/` returns ping-pong, approval-workflow, loan-application, supply-chain with correct metadata (title, description, category, tags)
- [x] T029 [P] Run full test suite across all affected projects — Blueprint Service tests (expect baseline + new tests passing), Engine tests (323 pass baseline), UI build (zero warnings)
- [x] T030 Update `MASTER-TASKS.md` with Blueprint Template Library completion status
- [x] T031 Verify quickstart.md steps work end-to-end against Docker deployment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (needs baseline test count) — BLOCKS all user stories requiring cyclic blueprint publishing
- **US1 (Phase 3)**: Depends on Phase 2 (needs cycle warning fix to publish Ping-Pong)
- **US3 (Phase 4)**: Depends on Phase 1 (needs Ping-Pong JSON file) and Phase 2 (needs cycle warning fix)
- **US2 (Phase 5)**: Depends on Phase 4 (needs templates available via API for the UI to display)
- **US4 (Phase 6)**: Depends on Phase 5 (needs template library page to add instance creation flow)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1 — MVP)**: Depends only on Foundational phase. Can be tested independently via walkthrough script without UI or seeding.
- **US3 (P3)**: Depends on Foundational phase. Independent of US1 — seeds templates via API, testable via `GET /api/templates/`.
- **US2 (P2)**: Depends on US3 — needs seeded templates to display. Independently testable by navigating to `/templates` page.
- **US4 (P2)**: Depends on US2 — extends the template library page. Independently testable by creating an instance from Ping-Pong template.

### Recommended Execution Order

```
Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1/MVP) → Phase 4 (US3) → Phase 5 (US2) → Phase 6 (US4) → Phase 7 (Polish)
```

### Within Each User Story

- Models/JSON before services
- Services before endpoints
- Core implementation before integration
- Tests alongside or immediately after implementation

### Parallel Opportunities

- **Phase 1**: T001 and T002 can run in parallel (different concerns)
- **Phase 2**: T003 and T004 are sequential (T004 depends on T003 output); T005 depends on T003+T004
- **Phase 3 (US1)**: T007-T010 are sequential (each extends the walkthrough script)
- **Phase 4 (US3)**: T012 and T013 are sequential; T014 can be parallelized with T015
- **Phase 5 (US2)**: T017 and T019 can run in parallel (different files)
- **Phase 7**: T028 and T029 can run in parallel

---

## Parallel Example: Phase 5 (US2)

```bash
# These can run in parallel (different files):
Task T017: "Create Templates.razor page"
Task T019: "Add nav menu link in NavMenu.razor"

# Then sequentially:
Task T018: "Add template detail panel to Templates.razor" (depends on T017)
Task T020: "Add empty state handling to Templates.razor" (depends on T017)
Task T021: "Build UI projects" (depends on all above)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create Ping-Pong JSON)
2. Complete Phase 2: Foundational (cycle detection warning fix)
3. Complete Phase 3: User Story 1 (walkthrough script proves end-to-end pipeline)
4. **STOP and VALIDATE**: Run walkthrough against Docker — 5 round-trips pass
5. Deploy/demo if ready — this alone proves the action execution pipeline works

### Incremental Delivery

1. Setup + Foundational → Cyclic blueprints can publish
2. Add US1 → Walkthrough proves pipeline → Demo (MVP!)
3. Add US3 → Templates auto-seed on startup → Templates available via API
4. Add US2 → Template library UI page → Users can browse
5. Add US4 → Instance creation from template → Full self-service flow
6. Polish → Docker rebuild, full test suite, documentation

### Key Files Summary

| File | Phase | Action |
|------|-------|--------|
| `examples/templates/ping-pong-template.json` | 1 | Create |
| `src/Services/Sorcha.Blueprint.Service/Program.cs` | 2, 4 | Modify (cycle detection, DI, seed endpoint) |
| `src/Services/Sorcha.Blueprint.Service/Templates/TemplateSeedingService.cs` | 4 | Create |
| `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Templates.razor` | 5, 6 | Create |
| `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Layout/NavMenu.razor` | 5 | Modify |
| `walkthroughs/PingPong/test-ping-pong-workflow.ps1` | 3 | Create |
| `tests/Sorcha.Blueprint.Service.Tests/` | 2, 4 | Modify (new test files) |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Blueprint Service baseline: 214 pass (last known from branch 019)
- Engine baseline: 323 pass / 17 pre-existing failures
- Counter increment-by-1 validation is deferred (schema validates type/minimum only per research.md R6)
