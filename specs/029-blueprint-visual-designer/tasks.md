# Tasks: Blueprint Visual Designer

**Input**: Design documents from `/specs/029-blueprint-visual-designer/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-components.md, quickstart.md

**Tests**: Unit tests included for layout algorithm (core logic). No E2E tests — manual UI verification via quickstart.md scenarios.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **UI Core**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/`
- **Web Client**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/`
- **Models**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/`
- **Components**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/`
- **Services**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/`
- **Pages**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/`

---

## Phase 1: Setup

**Purpose**: Verify existing infrastructure and ensure the project builds cleanly before making changes.

- [x] T001 Verify Sorcha.UI.Core and Sorcha.UI.Web.Client projects build with zero errors by running `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj` and `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Sorcha.UI.Web.Client.csproj`
- [x] T002 Verify Z.Blazor.Diagrams v3.0.4 NuGet package is referenced in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj` and confirm existing Designer.razor page compiles

---

## Phase 2: Foundational (Layout Models & Service)

**Purpose**: Create the shared layout models and auto-layout algorithm that ALL user stories depend on. No user story work can begin until this phase is complete.

**⚠️ CRITICAL**: The BlueprintLayoutService and layout models are used by all subsequent phases.

- [x] T003 [P] Create EdgeType enum in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/EdgeType.cs` — define Default, Conditional, Rejection, BackEdge, Terminal values per data-model.md
- [x] T004 [P] Create ParticipantInfo record in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/ParticipantInfo.cs` — id, name, colour fields; include a static method to assign colours from a predefined palette based on participant index
- [x] T005 [P] Create DiagramNode record in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/DiagramNode.cs` — actionId, title, senderParticipantId, layer, position (Blazor.Diagrams.Core.Geometry.Point), isStarting, isTerminal, isCycleTarget, detailSummary fields per data-model.md
- [x] T006 [P] Create DiagramEdge record in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/DiagramEdge.cs` — sourceActionId, targetActionId, routeId, edgeType (EdgeType), label, isBackEdge fields per data-model.md
- [x] T007 Create DiagramLayout record in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/DiagramLayout.cs` — nodes (List<DiagramNode>), edges (List<DiagramEdge>), participantLegend (List<ParticipantInfo>), width, height fields per data-model.md
- [x] T008 Implement BlueprintLayoutService in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/BlueprintLayoutService.cs` — public method `ComputeLayout(Blueprint blueprint)` returning `DiagramLayout`. Algorithm: (1) build adjacency graph from Action.routes[].nextActionIds, (2) find starting actions (isStartingAction==true), (3) BFS to assign layer depths handling cycles via visited set, (4) classify edges as Default/Conditional/BackEdge/Terminal based on route properties, (5) order nodes within layers by parent position, (6) compute positions using constants: nodeWidth=280, verticalSpacing=180, horizontalSpacing=320, startXOffset=50, startYOffset=50. Handle legacy routing (Action.condition/participants) as fallback when routes are empty. Mark terminal actions (no outgoing routes with non-empty nextActionIds).
- [x] T009 Write unit tests for BlueprintLayoutService in `tests/Sorcha.UI.Core.Tests/Services/BlueprintLayoutServiceTests.cs` — create test project if it does not exist. Test cases: (a) linear 2-action chain produces 2 layers, (b) branching routes produce fan-out in same layer, (c) cycle detection marks back-edges correctly, (d) parallel routes (multiple nextActionIds) create side-by-side nodes, (e) empty blueprint (no actions) returns empty layout, (f) blueprint with only starting action and no routes produces single node marked as both starting and terminal. Use xUnit + FluentAssertions.
- [x] T010 Register BlueprintLayoutService as scoped service in DI — add `builder.Services.AddScoped<BlueprintLayoutService>()` in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Program.cs` (or the appropriate Blazor WASM startup file)

**Checkpoint**: Layout algorithm complete and tested. All models available for UI components.

---

## Phase 3: User Story 1 — View Blueprint as Visual Diagram (Priority: P1) 🎯 MVP

**Goal**: Create the core readonly visual diagram component that renders any blueprint JSON as an interactive flow diagram with locked nodes, directed route arrows, and zoom support.

**Independent Test**: Load the built-in ping-pong template JSON in-memory, pass it to BlueprintViewerDiagram, verify 2 action nodes and 2 route arrows render with correct structure. No backend required.

### Implementation for User Story 1

- [x] T011 [US1] Create ReadOnlyActionNodeWidget.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/ReadOnlyActionNodeWidget.razor` — render action title, ID chip (MudChip), sender participant label, starting action badge (green MudChip if isStartingAction), terminal indicator (red "END" chip if no outgoing routes). Colour-coded left border matching sender participant colour (use ParticipantInfo from DiagramLayout). NO edit toolbar buttons (no Add Participant, Add Condition, Properties buttons). Parameter: `[Parameter] public ActionNodeModel Node { get; set; }`. Reuse CSS styling patterns from existing ActionNodeWidget.razor (header gradient, min-width 280px, shadow) but simpler body.
- [x] T012 [US1] Create BlueprintViewerDiagram.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/BlueprintViewerDiagram.razor` — main readonly diagram component. Parameters: `Blueprint` (required), `Height` (string, default "400px"), `OnActionClicked` (EventCallback<Sorcha.Blueprint.Models.Action>). On parameter set: (1) inject BlueprintLayoutService, (2) call ComputeLayout(Blueprint) to get DiagramLayout, (3) create BlazorDiagram with options: AllowMultiSelection=false, Zoom.Enabled=true, Links.DefaultColor="#666666", (4) register ReadOnlyActionNodeWidget via Diagram.RegisterComponent<ActionNodeModel, ReadOnlyActionNodeWidget>(), (5) for each DiagramNode: create ActionNodeModel with position, set Locked=true, add Top+Bottom ports, (6) for each DiagramEdge: create LinkModel between source bottom port and target top port, (7) render via CascadingValue+DiagramCanvas. Handle selection events to trigger OnActionClicked. Include participant legend at top using MudChips showing participant name + colour dot.
- [x] T013 [US1] Add CSS styling for BlueprintViewerDiagram in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/BlueprintViewerDiagram.razor.css` (scoped CSS) — diagram container with configurable height, overflow hidden, border radius, background colour (#f5f5f5), participant legend bar with horizontal flex layout. ReadOnlyActionNodeWidget styles: starting-action class (green left border), terminal-action class (red left border), cycle-target class (orange dashed border), readonly cursor (default, not grab).
- [x] T014 [US1] Add edge visual differentiation in BlueprintViewerDiagram — after creating links, apply visual properties per EdgeType: Default=solid "#1976d2", Conditional=dashed "#ff9800" with label, BackEdge="#9c27b0" with label "loop", Terminal="#f44336". Use LinkModel.Labels property if available in Z.Blazor.Diagrams v3.0.4, otherwise add small MudChip overlays positioned at edge midpoints.
- [x] T015 [US1] Verify readonly enforcement — ensure no nodes can be dragged (Locked=true), no new links can be created (test by attempting to drag from port), no nodes can be deleted. Verify zoom in/out works via mouse wheel. Verify scroll/pan works for diagrams larger than container.
- [x] T016 [US1] Build verification — run `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj` and `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Sorcha.UI.Web.Client.csproj` with zero errors and zero warnings

**Checkpoint**: BlueprintViewerDiagram renders any Blueprint object as a readonly visual diagram. Core viewer complete.

---

## Phase 4: User Story 2 — Browse Templates with Visual Preview (Priority: P2)

**Goal**: Integrate the viewer into the Templates page so users see a visual diagram when selecting a template.

**Independent Test**: Navigate to /templates, select ping-pong template, verify diagram appears in the detail panel alongside template metadata.

### Implementation for User Story 2

- [x] T017 [US2] Modify Templates.razor detail panel in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Templates.razor` — add a visual preview section below the description/parameters area and above the "Use Template" button. Add a `_previewBlueprint` field (Blueprint?) and `_previewLoading` bool. When `_selectedTemplate` changes (in SelectTemplate method), call `LoadTemplatePreview()` to populate `_previewBlueprint`. Render `<BlueprintViewerDiagram Blueprint="@_previewBlueprint" Height="350px" />` when `_previewBlueprint` is not null. Show MudProgressLinear during loading.
- [x] T018 [US2] Implement LoadTemplatePreview method in Templates.razor — for templates with no parameters (like ping-pong): call `TemplateApi.EvaluateTemplateAsync(template.Id, new Dictionary<string, object>())` to get a Blueprint. For templates with parameters: use defaultParameters from the template to evaluate. Wrap in try-catch — on failure, show MudAlert with "Preview unavailable" message (don't crash the page). Set `_previewBlueprint = evaluatedBlueprint` on success.
- [x] T019 [US2] Verify "Use Template" button still works — ensure the existing TemplateEvaluator dialog flow is not broken by the preview addition. Click "Use Template", configure parameters, evaluate, navigate to designer. No regression.
- [x] T020 [US2] Test with all 4 built-in templates — verify ping-pong (2 nodes, cycle), approval workflow (branching), loan application (conditional path), supply-chain (complex multi-action). Each should render correctly in the preview panel.

**Checkpoint**: Templates page shows visual diagram preview. Users can understand workflow structure before using a template.

---

## Phase 5: User Story 4 — Verify Blueprint Deployment Pipeline (Priority: P2)

**Goal**: Confirm the end-to-end pipeline works: template → evaluate → save → publish → create instance.

**Independent Test**: Execute the full pipeline via API or UI and verify instance creation succeeds.

### Implementation for User Story 4

- [x] T021 [US4] Verify template seeding on fresh startup — requires Docker (manual verification) — start Docker services (`docker-compose up -d`), navigate to /templates, confirm all 4 built-in templates appear (ping-pong, approval-workflow, loan-application, supply-chain-order). If templates are missing, check TemplateSeedingService logs.
- [x] T022 [US4] Test template evaluation → designer flow — requires Docker (manual verification) — select ping-pong template, click "Use Template", verify TemplateEvaluator dialog opens, click "Use Template" in dialog, verify navigation to /designer with the evaluated blueprint loaded.
- [x] T023 [US4] Test blueprint save → publish flow — requires Docker (manual verification) — from the designer with a loaded blueprint, click Save, navigate to /blueprints, find the saved blueprint, click "Publish". Verify PublishReview dialog shows validation results. For ping-pong (which has cycles), verify cycle warnings are displayed but publish succeeds. Confirm blueprint status changes to "published".
- [x] T024 [US4] Test instance creation — requires Docker (manual verification) — verify a published blueprint can have an instance created via API: `POST /api/actions` with blueprintId, actionId=0 (starting action), senderWallet, registerAddress, payloadData. Verify response includes instanceId with state "Active" and nextActions.
- [x] T025 [US4] Document any pipeline issues found — requires Docker (manual verification) — if any step fails, document the issue and fix it. Update quickstart.md with actual results and any workarounds needed.

**Checkpoint**: End-to-end deployment pipeline verified. Blueprints can go from template to running instance.

---

## Phase 6: User Story 3 — View Published Blueprints Visually (Priority: P3)

**Goal**: Add "View" button to blueprint cards on the Blueprints page that opens a dialog with the readonly diagram.

**Independent Test**: Navigate to /blueprints, click "View" on a saved blueprint, verify dialog shows readonly diagram.

### Implementation for User Story 3

- [x] T026 [US3] Add GetBlueprintDetailAsync to IBlueprintApiService in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IBlueprintApiService.cs` — new method: `Task<Blueprint?> GetBlueprintDetailAsync(string id, CancellationToken cancellationToken = default)`. Returns the full Blueprint domain model (not BlueprintListItemViewModel).
- [x] T027 [US3] Implement GetBlueprintDetailAsync in BlueprintApiService in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/BlueprintApiService.cs` — call `GET /api/blueprints/{id}`, deserialise response as `Blueprint` (from Sorcha.Blueprint.Models) using the existing HttpClient and JsonSerializerOptions. Handle 404 → return null, other errors → throw.
- [x] T028 [US3] Create ActionDetailPopover.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/ActionDetailPopover.razor` — read-only popover/dialog shown on action node click. Parameters: Action, List<Participant>, bool IsOpen, EventCallback OnClose. Display sections: Description, Sender (resolve participant name from list), Data Schemas (count + JsonTreeView if schemas exist), Disclosures (list of participant address → data pointers), Routes (table: route ID, condition summary, next action IDs), Calculations (if any), RejectionConfig (if any). Use MudDialog (not popover) for better mobile support — MaxWidth.Medium, no fullscreen.
- [x] T029 [US3] Create BlueprintViewerDialog.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/BlueprintViewerDialog.razor` — MudDialog wrapper containing BlueprintViewerDiagram with Height="600px". Parameters: Blueprint (required), string Title (default: blueprint.Title). Include participant legend and action count summary at top. Close button in dialog actions. Wire OnActionClicked from BlueprintViewerDiagram to open ActionDetailPopover for the clicked action.
- [x] T030 [US3] Modify Blueprints.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Blueprints.razor` — add a "View" icon button (MudIconButton with Visibility icon, Color.Primary) to each blueprint card's actions row, between "Open" and "Publish"/"Versions". On click: call `ViewBlueprint(blueprint)` which fetches full blueprint via `BlueprintApi.GetBlueprintDetailAsync(blueprint.Id)`, then opens BlueprintViewerDialog via IDialogService with the Blueprint parameter. Show loading indicator during fetch.
- [x] T031 [US3] Wire ActionDetailPopover into BlueprintViewerDiagram — update BlueprintViewerDiagram.razor to maintain `_selectedAction` state. On diagram SelectionChanged: if selected node is ActionNodeModel, set `_selectedAction` to the corresponding Blueprint Action. Render ActionDetailPopover below the diagram canvas, passing _selectedAction, Blueprint.Participants, and IsOpen flag.
- [x] T032 [US3] Build verification — run `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj` and `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Sorcha.UI.Web.Client.csproj` with zero errors

**Checkpoint**: Blueprints page has "View" button. Full readonly diagram with action detail popover available.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Visual refinements, edge cases, and final validation across all stories.

- [x] T033 Refine participant colour legend — ensure consistent colour assignment across all views (Templates preview and Blueprints dialog use same palette). Test with blueprints having 2, 4, and 8+ participants.
- [x] T034 Handle edge case: blueprint with legacy routing only (no Action.routes, uses Action.condition/participants instead) — BlueprintLayoutService should fall back to creating edges from condition-based participant mappings. Test with a blueprint that has no routes defined.
- [x] T035 Handle edge case: blueprint with 20+ actions — verify scrolling and zoom work correctly, no performance degradation. Test by creating a blueprint JSON with 20 sequential actions and verifying render time < 2 seconds.
- [x] T036 Handle edge case: blueprint with parallel branches (multiple nextActionIds on single route) — verify fan-out layout places parallel actions side by side without overlap.
- [x] T037 Run full quickstart.md validation — requires Docker (manual verification) — execute all 5 scenarios from quickstart.md against Docker services and verify all manual testing checklist items pass.
- [x] T038 Final build verification — run `dotnet build` for entire UI solution and `dotnet test` for layout service tests. Ensure zero errors, zero warnings.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — verify builds first
- **Phase 2 (Foundational)**: Depends on Phase 1 — creates layout models and service used by all stories
- **Phase 3 (US1 - Core Viewer)**: Depends on Phase 2 — creates the BlueprintViewerDiagram component
- **Phase 4 (US2 - Templates)**: Depends on Phase 3 — integrates viewer into Templates page
- **Phase 5 (US4 - Deployment)**: Can run in parallel with Phase 4 — verifies backend pipeline
- **Phase 6 (US3 - Blueprints)**: Depends on Phase 3 — integrates viewer into Blueprints page; also adds ActionDetailPopover
- **Phase 7 (Polish)**: Depends on Phases 3-6 — final refinements across all stories

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 only — standalone viewer component
- **User Story 2 (P2)**: Depends on US1 — uses BlueprintViewerDiagram in Templates page
- **User Story 4 (P2)**: No dependency on US1 — verifies backend pipeline independently
- **User Story 3 (P3)**: Depends on US1 — uses BlueprintViewerDiagram in Blueprints page

### Parallel Opportunities

- **Phase 2**: T003, T004, T005, T006 can all run in parallel (different model files)
- **Phase 4 + Phase 5**: US2 (template integration) and US4 (deployment verification) can run in parallel
- **Phase 4 + Phase 6**: US2 (templates) and US3 (blueprints) can run in parallel after US1 is complete

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all model creation tasks in parallel (different files):
Task T003: "Create EdgeType enum in Models/Designer/EdgeType.cs"
Task T004: "Create ParticipantInfo record in Models/Designer/ParticipantInfo.cs"
Task T005: "Create DiagramNode record in Models/Designer/DiagramNode.cs"
Task T006: "Create DiagramEdge record in Models/Designer/DiagramEdge.cs"

# Then sequentially:
Task T007: "Create DiagramLayout record" (depends on T003-T006)
Task T008: "Implement BlueprintLayoutService" (depends on T007)
Task T009: "Write unit tests" (depends on T008)
Task T010: "Register in DI" (depends on T008)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Layout models + BlueprintLayoutService + tests
3. Complete Phase 3: BlueprintViewerDiagram + ReadOnlyActionNodeWidget
4. **STOP and VALIDATE**: Load a blueprint JSON in-memory and verify diagram renders correctly
5. This gives a working readonly viewer component that can be used anywhere

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready (layout engine tested)
2. Phase 3 (US1) → Core viewer component (MVP!)
3. Phase 4 (US2) → Templates page integration → Users can preview templates
4. Phase 5 (US4) → Deployment pipeline verified → Confidence in end-to-end flow
5. Phase 6 (US3) → Blueprints page integration → Full coverage
6. Phase 7 → Polish → Production-ready

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All new components go in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/` alongside existing designer components
- No new backend API endpoints — all data from existing Blueprint Service
- Z.Blazor.Diagrams v3.0.4 API: `BlazorDiagram`, `NodeModel`, `LinkModel`, `PortAlignment`, `DiagramCanvas`
- Existing `ActionNodeModel` and `ParticipantNodeModel` in `Models/Designer/` are reused, not modified
- CSS scoped isolation: each .razor file gets its own .razor.css for scoped styles
