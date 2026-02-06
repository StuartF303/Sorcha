# Research: Blueprint Engine Integration

**Feature**: 018-blueprint-engine-integration
**Date**: 2026-02-06

## Research Findings

### R1: How to Wire Engine Components into ActionExecutionService

**Decision**: Inject `IExecutionEngine` (the facade) into `ActionExecutionService` and delegate the four stub methods to Engine components.

**Rationale**: The `IExecutionEngine` facade already orchestrates all sub-components (ISchemaValidator, IJsonLogicEvaluator, IDisclosureProcessor, IRoutingEngine). Rather than injecting 4 individual interfaces, inject the single facade and use its individual methods (`ValidateAsync`, `ApplyCalculationsAsync`, `DetermineRoutingAsync`, `ApplyDisclosures`). This matches how the `/api/execution` endpoints already use the engine.

**Alternatives Considered**:
- Inject all 4 sub-components individually: More granular but increases constructor complexity (already 8 parameters). Rejected.
- Call `IExecutionEngine.ExecuteActionAsync()` for the full pipeline: Would require building an `Engine.Models.ExecutionContext` and mapping the result back. More elegant but requires adapting the 15-step orchestration flow. May be a future refactor. Rejected for now.

**Integration approach for each stub method**:

| Stub Method | Engine Method | Mapping Required |
|-------------|---------------|------------------|
| `ValidateActionDataAsync` | `IExecutionEngine.ValidateAsync(data, action)` | Map `Engine.Models.ValidationResult` → service `ValidationResult` |
| `EvaluateJsonLogicCondition` + `EvaluateRouting` | `IExecutionEngine.DetermineRoutingAsync(blueprint, action, data)` | Map `Engine.Models.RoutingResult` → service `RoutingResult` |
| `EvaluateCalculations` | `IExecutionEngine.ApplyCalculationsAsync(data, action)` | Return `Dictionary<string, object>` directly |
| `ApplyDisclosures` | `IExecutionEngine.ApplyDisclosures(data, action)` | Map `List<DisclosureResult>` → `Dictionary<string, Dictionary<string, object>>` |

### R2: Route-Based Routing Implementation in RoutingEngine

**Decision**: Extend existing `RoutingEngine.DetermineNextAsync()` to check `Action.Routes` first, falling back to legacy `Action.Participants` (Condition-based) routing when Routes is null/empty.

**Rationale**: The `Route` model already exists in `Sorcha.Blueprint.Models.Route` with `NextActionIds`, `Condition` (JsonNode), `IsDefault`, and `BranchDeadline`. The `RoutingEngine` already has `IJsonLogicEvaluator` injected. Adding Route evaluation requires:
1. Check if `action.Routes` has entries → use Route-based routing
2. Iterate routes in order, evaluate each `Condition` via `IJsonLogicEvaluator.Evaluate()`
3. First matching route wins; default route used when no conditions match
4. Map `NextActionIds` to routing results (single or parallel)

**Parallel branch support**: The existing `Engine.Models.RoutingResult` has `NextActionId` (singular) and `NextParticipantId`. For parallel branches, need to extend to support multiple next actions. Options:
- Add `List<(string ActionId, string ParticipantId)> NextActions` to `RoutingResult`
- Or create a new `ParallelRoutingResult` that extends `RoutingResult`

**Decision**: Add a `NextActions` list to `RoutingResult` alongside the existing singular properties for backward compatibility. Single-route results populate both `NextActionId` and `NextActions[0]`. Parallel results populate only `NextActions`.

### R3: Fluent API Builder Design for Routes and RejectionConfig

**Decision**: Create `RouteBuilder` and `RejectionConfigBuilder` as new files, add corresponding methods to `ActionBuilder`.

**Rationale**: Follow the existing `ConditionBuilder` / `DisclosureBuilder` pattern:
- Builder classes are `public` with `internal Build()` methods
- Accept delegates via `Action<TBuilder>` pattern on `ActionBuilder`
- Reuse existing `JsonLogicBuilder` for route conditions

**New methods on ActionBuilder**:
- `AddRoute(string routeId, Action<RouteBuilder> configure)` → adds to `_routes` list → sets `_action.Routes`
- `WithDefaultRoute(params int[] nextActionIds)` → shorthand for default route
- `OnRejection(Action<RejectionConfigBuilder> configure)` → sets `_action.RejectionConfig`
- `AsStartingAction()` → sets `_action.IsStartingAction = true`
- `RequiresPriorActions(params int[] actionIds)` → sets `_action.RequiredPriorActions`

**RouteBuilder API**:
- `ToActions(params int[] nextActionIds)` → required, sets `NextActionIds`
- `When(Func<JsonLogicBuilder, JsonNode> condition)` → optional, sets `Condition`
- `AsDefault()` → sets `IsDefault = true`
- `WithDescription(string)` → optional
- `WithBranchDeadline(string isoDuration)` → optional

**RejectionConfigBuilder API**:
- `RouteToAction(int targetActionId)` → required
- `WithTargetParticipant(string participantId)` → optional
- `RequireReason(bool require = true)` → optional, default true
- `WithRejectionSchema(JsonElement schema)` → optional
- `AsTerminal()` → sets `IsTerminal = true`

### R4: JsonLogicCache Integration Strategy

**Decision**: Inject `JsonLogicCache` into `JsonLogicEvaluator` via constructor, use it to cache parsed `Json.Logic.Rule` objects by expression hash.

**Rationale**: The `JsonLogicCache` already implements SHA256-based cache keying with `IMemoryCache` (max 1000 entries, 1hr absolute / 15min sliding). The `JsonLogicEvaluator.Evaluate()` method currently calls `JsonSerializer.Deserialize<Rule>(expression.ToJsonString())` on every invocation (line 35). Wrapping this in `_cache.GetOrAdd(expression, expr => JsonSerializer.Deserialize<Rule>(expr.ToJsonString()))` caches the deserialized `Rule` object.

**DI registration**: Add `builder.Services.AddSingleton<JsonLogicCache>()` in Program.cs after the existing engine registrations. Use Singleton because the cache should be shared across scoped evaluator instances.

### R5: TransactionBuilderServiceExtensions Fix

**Decision**: Refactor the stub extension methods to call through to the real `ITransactionBuilderService` methods with proper payload encryption.

**Rationale**: The stubs currently return `TransactionData = []` (empty byte array). The fix requires:
1. Serialize payload data to JSON bytes
2. For action transactions: encrypt per-recipient payloads using the wallet client, then call `ITransactionBuilderService.BuildActionTransactionAsync()` with the encrypted payloads
3. Convert the returned `Transaction` to `BuiltTransaction`

**Complexity**: The extension methods don't have access to `IWalletServiceClient` for encryption. Two options:
- Make them instance methods on a new service class that holds `IWalletServiceClient`
- Pass `IWalletServiceClient` as a parameter

**Decision**: Convert the extension methods to instance methods on `ActionExecutionService` or a new `ExecutionTransactionBuilder` helper class that wraps `ITransactionBuilderService` + `IWalletServiceClient`. This avoids the awkward extension-method-with-service-dependency pattern.

### R6: Graph Cycle Detection Algorithm

**Decision**: Implement DFS-based cycle detection in the blueprint publish validation.

**Rationale**: Standard graph cycle detection using depth-first search with coloring (WHITE/GRAY/BLACK). Build an adjacency list from:
1. `Action.Routes[].NextActionIds` for each action
2. `Action.RejectionConfig.TargetActionId` for rejection routing
3. Legacy: scan `Action.Participants` conditions for next action references

The algorithm detects:
- Direct cycles (A → B → C → A)
- Self-references (A → A)
- Reports the cycle path in the error message

**Placement**: Add a private `DetectCycles()` method inside the `PublishService` class in `Program.cs` (near the existing `ValidateBlueprint()` method at line 1720).

### R7: ExecutionMode.ValidationOnly Support

**Decision**: Add a mode check at the beginning of `ActionProcessor.ProcessAsync()` to short-circuit after schema validation when `Mode == ExecutionMode.ValidationOnly`.

**Rationale**: The `ExecutionMode` enum and `ExecutionContext.Mode` property already exist. The change is minimal:
1. After Step 1 (schema validation), check `context.Mode == ExecutionMode.ValidationOnly`
2. If true, return early with `ActionExecutionResult { Success = validation.IsValid, Validation = validation }`
3. Steps 2-4 (calculations, routing, disclosure) are skipped

### R8: Disclosure in POST /api/actions Endpoint

**Decision**: Inject `IDisclosureProcessor` into the endpoint and use it to filter payloads per recipient before encryption.

**Rationale**: The endpoint at Program.cs line 605-619 currently gives all participants the full payload. The fix:
1. Get the action definition to access its `Disclosures` list
2. Call `IDisclosureProcessor.CreateDisclosures(payloadData, action.Disclosures)`
3. Build the `participantWalletMap` from disclosure results + participant wallets
4. Encrypt each participant's filtered payload separately
