# Tasks: Blueprint Engine Integration

**Input**: Design documents from `/specs/018-blueprint-engine-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per Constitution IV (>85% coverage on new code). Write tests alongside implementation.

**Organization**: Tasks grouped by user story. US1-US4 (P1) share foundational engine work. US5-US9 are independent.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify baseline and prepare for changes

- [x] T001 Verify all existing tests pass by running `dotnet test` across entire solution
- [x] T002 Verify 319 Blueprint Engine tests pass by running `dotnet test tests/Sorcha.Blueprint.Engine.Tests` (Baseline: 299 pass, 17 pre-existing failures, 1 skipped)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend Engine models needed by ALL user stories before integration work begins

**CRITICAL**: US1-US4 all depend on RoutingResult extensions and service wiring. Complete this phase first.

- [ ] T003 Add `RoutedAction` record and `NextActions` list, `IsParallel` property to `RoutingResult` in `src/Core/Sorcha.Blueprint.Engine/Models/RoutingResult.cs`. Add `RoutedAction` with ActionId, ParticipantId, BranchId, MatchedRouteId. Update `Next()` factory to populate both `NextActionId` and `NextActions[0]`. Add `Parallel(List<RoutedAction>)` factory method. Preserve all existing properties and factories for backward compatibility.
- [ ] T004 Add `IExecutionEngine` as a constructor dependency in `ActionExecutionService` at `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs`. Add `private readonly IExecutionEngine _executionEngine` field. Update constructor to accept and store it. Update `IActionExecutionService` interface if needed.
- [ ] T005 Register `JsonLogicCache` as Singleton in DI at `src/Services/Sorcha.Blueprint.Service/Program.cs` line ~58, after existing engine registrations: `builder.Services.AddSingleton<Sorcha.Blueprint.Engine.Caching.JsonLogicCache>()`
- [ ] T006 Add tests for `RoutingResult` extensions in `tests/Sorcha.Blueprint.Engine.Tests/RoutingResultTests.cs`: test `Next()` populates both singular and list properties, test `Parallel()` factory creates correct `NextActions` with branch IDs, test `Complete()` still works, test backward compatibility of existing `NextActionId`/`NextParticipantId`

**Checkpoint**: Foundation ready. RoutingResult supports parallel branches, ActionExecutionService has engine injected, cache registered in DI.

---

## Phase 3: User Story 1 - Workflow Conditional Routing Works Correctly (Priority: P1)

**Goal**: Replace stub routing in ActionExecutionService with real JSON Logic evaluation via the Blueprint Engine

**Independent Test**: Submit action data to a blueprint with conditional routes and verify correct next action is returned

### Implementation for User Story 1

- [ ] T007 [US1] Replace `EvaluateRouting()` and `EvaluateJsonLogicCondition()` methods in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` (lines 338-427). Delete both stub methods. Replace with a call to `_executionEngine.DetermineRoutingAsync(blueprint, actionDef, mergedData)` where mergedData combines accumulated state + current data. Map the returned `Engine.Models.RoutingResult` to the service-layer `RoutingResult` (populate `NextActions` list from engine result, map `NextActionId`/`NextParticipantId` for singular results, set `IsParallel`).
- [ ] T008 [US1] Update `ActionExecutionService.ExecuteAsync()` step 7 (line 115) to use the new engine-delegated routing. Ensure the merged data dictionary (accumulated state + current submission) is constructed before the routing call. Remove any references to the deleted stub methods.
- [ ] T009 [US1] Update `ActionExecutionServiceTests` in `tests/Sorcha.Blueprint.Service.Tests/Services/ActionExecutionServiceTests.cs`: Add mock setup for `IExecutionEngine.DetermineRoutingAsync()`. Add test `ExecuteAsync_WithConditionalRoute_DelegatesToEngine` verifying engine is called with correct parameters. Add test `ExecuteAsync_WithDefaultRoute_ReturnsDefaultRouteResult`. Add test `ExecuteAsync_WithLegacyConditions_FallsBackCorrectly`. Update existing tests to mock the new `IExecutionEngine` dependency.

**Checkpoint**: Routing decisions are now made by the real JSON Logic engine, not stubs.

---

## Phase 4: User Story 2 - Action Data Is Validated Against JSON Schemas (Priority: P1)

**Goal**: Replace stub schema validation with real JSON Schema Draft 2020-12 validation via the Blueprint Engine

**Independent Test**: Submit invalid data to an action with a JSON Schema and verify validation errors are returned

### Implementation for User Story 2

- [ ] T010 [US2] Replace `ValidateActionDataAsync()` method in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` (lines 298-336). Delete the stub method body. Call `_executionEngine.ValidateAsync(data, actionDef)` to get `Engine.Models.ValidationResult`. Map the engine result to the service-layer `ValidationResult` (map `IsValid`, `Errors` list from engine `ValidationError` objects extracting messages, preserve `Warnings`). Keep the backward-compatible `RequiredActionData` field-presence check as a fallback when no DataSchemas are defined.
- [ ] T011 [US2] Add tests in `tests/Sorcha.Blueprint.Service.Tests/Services/ActionExecutionServiceTests.cs`: Add test `ExecuteAsync_WithInvalidSchemaData_ThrowsValidationException` verifying engine validation is called and errors are propagated. Add test `ExecuteAsync_WithNoSchema_FallsBackToFieldPresenceCheck`. Add test `ExecuteAsync_WithMultipleSchemas_CollectsAllErrors`.

**Checkpoint**: Action data is validated against full JSON Schema before any transaction is created.

---

## Phase 5: User Story 3 - Calculations Are Evaluated During Execution (Priority: P1)

**Goal**: Replace stub calculation evaluation with real JSON Logic computation via the Blueprint Engine

**Independent Test**: Submit data to an action with calculations and verify computed numerical results in response

### Implementation for User Story 3

- [ ] T012 [US3] Replace `EvaluateCalculations()` method in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` (lines 429-461). Delete the stub method body. Call `_executionEngine.ApplyCalculationsAsync(mergedData, actionDef)` where mergedData combines accumulated state + current data. Return the `Dictionary<string, object>` result directly (engine already returns computed values, not expression strings). Handle exceptions gracefully: log errors and return partial results.
- [ ] T013 [US3] Add tests in `tests/Sorcha.Blueprint.Service.Tests/Services/ActionExecutionServiceTests.cs`: Add test `ExecuteAsync_WithCalculations_ReturnsComputedValues` verifying engine is called and computed results are in the response. Add test `ExecuteAsync_WithChainedCalculations_ReferencesEarlierResults`. Add test `ExecuteAsync_WithFailedCalculation_LogsAndContinues`.

**Checkpoint**: Calculations return evaluated numerical values, not raw expression strings.

---

## Phase 6: User Story 4 - Selective Disclosure Filters Data Per Recipient (Priority: P1)

**Goal**: Replace stub disclosure with real JSON Pointer filtering and fix the POST /api/actions endpoint

**Independent Test**: Execute an action with disclosure rules and verify each recipient's payload contains only authorized fields

### Implementation for User Story 4

- [ ] T014 [US4] Replace `ApplyDisclosures()` method in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` (lines 463-515). Delete the stub method body. Call `_executionEngine.ApplyDisclosures(data, actionDef)` to get `List<DisclosureResult>`. Map each `DisclosureResult` to the service-layer dictionary format: `Dictionary<string, Dictionary<string, object>>` keyed by wallet address (resolve `ParticipantId` to wallet address using `participantWallets` dictionary).
- [ ] T015 [US4] Fix disclosure in `POST /api/actions` endpoint at `src/Services/Sorcha.Blueprint.Service/Program.cs` lines 605-619. Inject `IDisclosureProcessor` into the endpoint handler. Replace the hardcoded `disclosureResults` that sends full payload to sender only. Instead: get the action definition from the blueprint, call `disclosureProcessor.CreateDisclosures(payloadData, actionDef.Disclosures)`, build `participantWalletMap` from disclosure results mapping participant IDs to wallet addresses, encrypt each participant's filtered payload separately.
- [ ] T016 [US4] Fix `TransactionBuilderServiceExtensions.BuildActionTransactionAsync()` stub in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` (lines 89-113). Replace the stub that returns `TransactionData = []`. Serialize the disclosed payload data to JSON bytes, encrypt per-recipient using the wallet client (passed as parameter or moved to instance method), call through to the real `ITransactionBuilderService.BuildActionTransactionAsync()` with the encrypted payloads, convert the returned `Transaction` to `BuiltTransaction` with actual `TransactionData` and `TxId`.
- [ ] T017 [US4] Fix `TransactionBuilderServiceExtensions.BuildRejectionTransactionAsync()` stub in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` (lines 118-141). Apply the same pattern: serialize rejection data, call through to the real `ITransactionBuilderService.BuildRejectionTransactionAsync()`, return a `BuiltTransaction` with real transaction data.
- [ ] T018 [US4] Add tests in `tests/Sorcha.Blueprint.Service.Tests/Services/ActionExecutionServiceTests.cs`: Add test `ExecuteAsync_WithDisclosureRules_FiltersDataPerRecipient` verifying engine disclosure is called and recipient payloads are filtered. Add test `ExecuteAsync_WithWildcardDisclosure_SendsAllFields`. Add test for POST /api/actions endpoint disclosure behavior.

**Checkpoint**: Each participant receives only their authorized data. DAD model disclosure is enforced.

---

## Phase 7: User Story 5 - Route-Based Routing with Parallel Branches (Priority: P2)

**Goal**: Implement Route-based routing in the Blueprint Engine, supporting the Route model with parallel branches

**Independent Test**: Create a blueprint with Route definitions and verify the engine evaluates conditions and creates parallel branches

### Implementation for User Story 5

- [ ] T019 [P] [US5] Implement Route-based routing in `src/Core/Sorcha.Blueprint.Engine/Implementation/RoutingEngine.cs`. Modify `DetermineNextAsync()`: at the beginning, check if `currentAction.Routes` is not null and has entries. If so, execute the Route evaluation algorithm (from data-model.md): iterate routes in order, save default route as fallback, evaluate each route's `Condition` using `_evaluator.Evaluate(route.Condition, data)`, first matching route wins. For matched route: if single `NextActionId`, use `RoutingResult.Next()`. If multiple `NextActionIds`, use `RoutingResult.Parallel()` creating `RoutedAction` entries with unique branch IDs and the matched route ID. If no route matches, use saved default or return `RoutingResult.Complete()`. Preserve existing legacy Condition-based routing as `else` fallback when `Routes` is null/empty.
- [ ] T020 [P] [US5] Add Route-based routing tests in `tests/Sorcha.Blueprint.Engine.Tests/RoutingEngineTests.cs`: Add `#region Route-Based Routing` with tests: `DetermineNextAsync_WithRoutes_UsesRouteBasedRouting`, `DetermineNextAsync_WithConditionalRoute_EvaluatesCondition`, `DetermineNextAsync_WithDefaultRoute_FallsBackToDefault`, `DetermineNextAsync_WithParallelRoutes_CreatesMultipleBranches`, `DetermineNextAsync_WithRoutesAndConditions_RoutesTakePrecedence`, `DetermineNextAsync_WithNoMatchingRouteAndNoDefault_ReturnsComplete`, `DetermineNextAsync_WithMissingDataField_TreatsAsFalsy`.

**Checkpoint**: Engine evaluates Route-based routing with parallel branch support and legacy fallback.

---

## Phase 8: User Story 6 - Fluent API Supports Route and Rejection Configuration (Priority: P2)

**Goal**: Add RouteBuilder, RejectionConfigBuilder, and new ActionBuilder methods for full Route/Rejection support

**Independent Test**: Build a blueprint with Fluent API using route builders and verify correct Route/RejectionConfig in the model

### Implementation for User Story 6

- [ ] T021 [P] [US6] Create `RouteBuilder` in `src/Core/Sorcha.Blueprint.Fluent/RouteBuilder.cs`. Add license header. Follow `ConditionBuilder` pattern: public class with internal `Build()` method returning `Route`. Methods: `ToActions(params int[] nextActionIds)` (required, validates at least one ID), `When(Func<JsonLogicBuilder, JsonNode> condition)` (optional, reuses existing `JsonLogicBuilder`), `AsDefault()` (sets `IsDefault = true`), `WithDescription(string description)`, `WithBranchDeadline(string isoDuration)`. Store `_route` with auto-generated ID from route ID parameter. Validate: `ToActions` must be called before `Build()`.
- [ ] T022 [P] [US6] Create `RejectionConfigBuilder` in `src/Core/Sorcha.Blueprint.Fluent/RejectionConfigBuilder.cs`. Add license header. Public class with internal `Build()` method returning `RejectionConfig`. Methods: `RouteToAction(int targetActionId)` (required), `WithTargetParticipant(string participantId)`, `RequireReason(bool require = true)`, `WithRejectionSchema(JsonElement schema)`, `AsTerminal()`. Validate: `RouteToAction` must be called before `Build()`.
- [ ] T023 [US6] Extend `ActionBuilder` in `src/Core/Sorcha.Blueprint.Fluent/ActionBuilder.cs`. Add private `List<Route> _routes = new()` field. Add methods: `AddRoute(string routeId, Action<RouteBuilder> configure)` (creates `RouteBuilder`, passes to delegate, calls `Build()`, adds to `_routes`), `WithDefaultRoute(params int[] nextActionIds)` (shorthand creating a default route), `OnRejection(Action<RejectionConfigBuilder> configure)` (creates builder, passes to delegate, sets `_action.RejectionConfig`), `AsStartingAction()` (sets `_action.IsStartingAction = true`), `RequiresPriorActions(params int[] actionIds)` (sets `_action.RequiredPriorActions`). In `Build()` method, set `_action.Routes = _routes` if any routes were added.
- [ ] T024 [P] [US6] Create `RouteBuilderTests` in `tests/Sorcha.Blueprint.Fluent.Tests/RouteBuilderTests.cs`. Tests: `Build_WithBasicRoute_CreatesRoute`, `Build_WithCondition_SetsJsonLogicCondition`, `Build_WithDefault_SetsIsDefault`, `Build_WithParallelActions_SetsMultipleNextActionIds`, `Build_WithBranchDeadline_SetsDuration`, `Build_WithoutToActions_ThrowsInvalidOperationException`.
- [ ] T025 [P] [US6] Create `RejectionConfigBuilderTests` in `tests/Sorcha.Blueprint.Fluent.Tests/RejectionConfigBuilderTests.cs`. Tests: `Build_WithBasicConfig_CreatesRejectionConfig`, `Build_WithTargetParticipant_SetsOverride`, `Build_AsTerminal_SetsIsTerminal`, `Build_WithRejectionSchema_SetsSchema`, `Build_WithoutRouteToAction_ThrowsInvalidOperationException`.
- [ ] T026 [US6] Add `ActionBuilder` extension tests in `tests/Sorcha.Blueprint.Fluent.Tests/ActionBuilderTests.cs`. Tests: `ActionBuilder_AddRoute_AddsRouteToAction`, `ActionBuilder_WithDefaultRoute_AddsDefaultRoute`, `ActionBuilder_OnRejection_SetsRejectionConfig`, `ActionBuilder_AsStartingAction_SetsFlag`, `ActionBuilder_RequiresPriorActions_SetsActionIds`, `ActionBuilder_FullBlueprint_WithRoutesAndRejection_Builds`.

**Checkpoint**: Fluent API can construct blueprints with Route-based routing and rejection configuration.

---

## Phase 9: User Story 7 - Blueprint Publish Validates Action Graph Integrity (Priority: P2)

**Goal**: Implement DFS-based cycle detection in the publish validation

**Independent Test**: Attempt to publish a blueprint with circular action references and verify rejection with cycle path in error

### Implementation for User Story 7

- [ ] T027 [US7] Implement graph cycle detection in `src/Services/Sorcha.Blueprint.Service/Program.cs`. Add a private `DetectCycles(Blueprint blueprint)` method near the existing `ValidateBlueprint()` method (~line 1720). Build adjacency list: for each action, add edges from `Action.Routes[].NextActionIds` and `Action.RejectionConfig?.TargetActionId`. Implement DFS with WHITE/GRAY/BLACK coloring. When a GRAY node is revisited, collect the cycle path. Return `List<string>` errors with messages like "Circular dependency detected: Action {A} -> Action {B} -> Action {A}" and "Self-referencing route detected: Action {A} routes to itself". Call `DetectCycles()` from `ValidateBlueprint()` at the existing TODO comment on line 1754.
- [ ] T028 [US7] Create `CycleDetectionTests` in `tests/Sorcha.Blueprint.Service.Tests/CycleDetectionTests.cs`. Tests: `DetectCycles_LinearGraph_ReturnsNoErrors`, `DetectCycles_SimpleCycle_ReturnsCycleError`, `DetectCycles_SelfReference_ReturnsSelfReferenceError`, `DetectCycles_ComplexCycleWithBranches_DetectsAllCycles`, `DetectCycles_ParallelBranchesNoCycle_Passes`, `DetectCycles_RejectionConfigCycle_DetectedViaTRejectionTarget`. Note: may need to extract `DetectCycles` to a testable static method or internal class to unit test independently from the publish endpoint.

**Checkpoint**: Blueprints with circular dependencies are rejected at publish time.

---

## Phase 10: User Story 8 - ValidationOnly Mode for Client-Side Feedback (Priority: P3)

**Goal**: Implement ExecutionMode.ValidationOnly short-circuit in the ActionProcessor

**Independent Test**: Execute engine in ValidationOnly mode and verify only schema validation runs

### Implementation for User Story 8

- [ ] T029 [US8] Modify `ActionProcessor.ProcessAsync()` in `src/Core/Sorcha.Blueprint.Engine/Implementation/ActionProcessor.cs`. After Step 1 (schema validation, ~line 52), add check: `if (context.Mode == ExecutionMode.ValidationOnly)` → return early with `ActionExecutionResult { Success = validation.IsValid, Validation = validation, ProcessedData = context.ActionData }`. Steps 2-4 (calculations, routing, disclosure) are skipped.
- [ ] T030 [US8] Add ValidationOnly mode tests in `tests/Sorcha.Blueprint.Engine.Tests/ActionProcessorTests.cs`. Tests: `ProcessAsync_ValidationOnlyMode_SkipsCalculations`, `ProcessAsync_ValidationOnlyMode_SkipsRouting`, `ProcessAsync_ValidationOnlyMode_SkipsDisclosure`, `ProcessAsync_ValidationOnlyMode_ReturnsValidationResult`, `ProcessAsync_FullMode_RunsAllSteps` (existing behavior verification).

**Checkpoint**: ValidationOnly mode provides instant validation feedback without full pipeline execution.

---

## Phase 11: User Story 9 - JSON Logic Expression Caching (Priority: P3)

**Goal**: Wire the existing JsonLogicCache into the JsonLogicEvaluator for expression caching

**Independent Test**: Evaluate the same expression multiple times and verify cache utilization

### Implementation for User Story 9

- [ ] T031 [US9] Modify `JsonLogicEvaluator` constructor in `src/Core/Sorcha.Blueprint.Engine/Implementation/JsonLogicEvaluator.cs` to accept an optional `JsonLogicCache? cache = null` parameter. Store as `private readonly JsonLogicCache? _cache`. In the `Evaluate()` method (~line 35), wrap the `JsonSerializer.Deserialize<Rule>(expression.ToJsonString())` call: if `_cache` is not null, use `_cache.GetOrAdd(expression, expr => JsonSerializer.Deserialize<Rule>(expr.ToJsonString()))` to cache deserialized `Rule` objects. If cache is null, use the existing direct deserialization (backward-compatible).
- [ ] T032 [US9] Create cache integration tests in `tests/Sorcha.Blueprint.Engine.Tests/JsonLogicCacheIntegrationTests.cs`. Tests: `Evaluate_WithCache_CachesDeserializedRule`, `Evaluate_SameExpression_UsesCachedResult`, `Evaluate_DifferentExpressions_CachesSeparately`, `Evaluate_WithoutCache_WorksNormally` (backward compatibility), `ApplyCalculationsAsync_WithCache_CachesExpressions`.

**Checkpoint**: Repeated JSON Logic evaluations use cached parsed rules for improved throughput.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Verification, documentation, cleanup

- [ ] T033 Run full test suite `dotnet test` and verify ALL existing tests (319 Engine + all others) still pass alongside new tests
- [ ] T034 [P] Update `.specify/MASTER-TASKS.md` with task completion status for blueprint execution integration items
- [ ] T035 [P] Run quickstart.md verification: execute the three phase verification commands from `specs/018-blueprint-engine-integration/quickstart.md` (Engine tests, Fluent tests, Service tests)
- [ ] T036 Review all changed files for license headers, XML doc comments, nullable reference types, and no compiler warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies -- start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 -- BLOCKS all user stories
- **US1-US4 (Phases 3-6)**: All depend on Phase 2 (T003, T004 specifically). US1-US3 can run in parallel after Phase 2. US4 depends on US1 routing being wired (T007-T008).
- **US5 (Phase 7)**: Depends on Phase 2 (T003 RoutingResult extensions). Can run in parallel with US1-US4 (different files: Engine vs Service).
- **US6 (Phase 8)**: No dependency on service changes. Can run in parallel with US1-US5 (different project: Fluent).
- **US7 (Phase 9)**: No dependency on Engine changes. Can run in parallel with US5-US6 (Program.cs changes are isolated).
- **US8 (Phase 10)**: Depends on Phase 2. Can run in parallel with all service-layer work (Engine-only change).
- **US9 (Phase 11)**: Depends on Phase 2 (T005 cache DI registration). Can run in parallel with US1-US8 (JsonLogicEvaluator is a different file).
- **Polish (Phase 12)**: Depends on all user stories being complete.

### User Story Dependencies

```
Phase 2 (Foundation)
    ├── US1 (Routing)     ──┐
    ├── US2 (Validation)  ──┤── Can run in parallel (different methods in ActionExecutionService)
    ├── US3 (Calculations)──┤
    ├── US4 (Disclosure)  ──┘── Also depends on US1 for complete flow testing
    ├── US5 (Route Engine) ── Can run in parallel (Engine RoutingEngine.cs)
    ├── US6 (Fluent API)   ── Can run in parallel (Fluent project, no service deps)
    ├── US7 (Cycle Detection)── Can run in parallel (Program.cs publish validation)
    ├── US8 (ValidationOnly)── Can run in parallel (Engine ActionProcessor.cs)
    └── US9 (Caching)      ── Can run in parallel (Engine JsonLogicEvaluator.cs)
```

### Within Each User Story

- Implementation task(s) before test task(s) where tests require the implementation to exist
- Service wiring before endpoint changes
- Core implementation before integration

### Parallel Opportunities

**Maximum parallelism after Phase 2**:
- US5, US6, US7, US8, US9 can ALL run in parallel (different files, different projects)
- US1, US2, US3 can run in parallel (different methods in same file -- coordinate edits)
- US4 is best done after US1-US3 to validate end-to-end flow

---

## Parallel Example: After Phase 2 Completion

```
# These can all be launched simultaneously:
Agent 1: T019-T020 [US5] Route-based routing in Engine (RoutingEngine.cs)
Agent 2: T021-T026 [US6] Fluent API builders (RouteBuilder.cs, RejectionConfigBuilder.cs, ActionBuilder.cs)
Agent 3: T029-T030 [US8] ValidationOnly mode (ActionProcessor.cs)
Agent 4: T031-T032 [US9] Cache wiring (JsonLogicEvaluator.cs)

# Then these can run in parallel:
Agent 1: T007-T009 [US1] Routing integration (ActionExecutionService.cs)
Agent 2: T010-T011 [US2] Validation integration (ActionExecutionService.cs)
Agent 3: T027-T028 [US7] Cycle detection (Program.cs)

# Then:
Agent 1: T012-T013 [US3] Calculations integration
Agent 2: T014-T018 [US4] Disclosure integration + transaction stubs
```

---

## Implementation Strategy

### MVP First (US1 + US2 -- Routing and Validation)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Foundation (RoutingResult, DI wiring)
3. Complete Phase 3: US1 (Routing) -- conditional workflows work
4. Complete Phase 4: US2 (Validation) -- data integrity enforced
5. **STOP and VALIDATE**: Test routing + validation independently
6. These two stories alone fix the most critical stubs

### Incremental Delivery

1. Setup + Foundation → Ready
2. US1 + US2 → Core orchestration works (routing + validation)
3. US3 → Calculations produce real values
4. US4 → DAD model disclosure enforced end-to-end
5. US5 → Route model with parallel branches in Engine
6. US6 → Fluent API complete
7. US7 → Publish safety (cycle detection)
8. US8 + US9 → Polish (ValidationOnly mode, caching)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1-US4 share `ActionExecutionService.cs` -- coordinate edits if parallelizing
- US5-US9 are in separate files and can safely parallelize
- Constitution IV requires >85% coverage on changed files -- tests are included per story
- Commit after each completed user story phase
- Stop at any checkpoint to validate independently
