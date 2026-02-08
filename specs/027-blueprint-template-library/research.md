# Research: Blueprint Template Library & Ping-Pong Blueprint

**Date**: 2026-02-08
**Branch**: `027-blueprint-template-library`

## R1: Cycle Detection at Publish Time

**Decision**: Change cycle detection from hard rejection to warning. Cycles are valid workflow patterns (ping-pong, review loops, resubmission flows). The runtime engine already handles cycles correctly — `CurrentActionIds` is updated each execution and the state machine loops until routing returns an empty next-actions list.

**Rationale**: The DFS cycle detection in `PublishService.ValidateBlueprint()` (Program.cs lines 1813-1905) currently adds cycle errors to a rejection list. The engine's `UpdateInstanceAfterExecutionAsync` (ActionExecutionService.cs lines 450-496) handles cycles at runtime by adding routed action IDs back to `CurrentActionIds`. Blocking at publish time prevents legitimate looping workflows.

**Change**: Modify `ValidateBlueprint()` to return cycle detections as warnings (not errors). Add `HasCycles` metadata flag to published blueprints for visibility.

**Alternatives considered**:
- Skip publish validation entirely → Rejected: other validations (participant refs, action refs) are still valuable
- Add `allowCycles` flag per blueprint → Rejected: unnecessary complexity; just warn always
- Pre-defined chain of N actions → Rejected: not a true cycle, limits functionality

## R2: Ping-Pong Blueprint Structure

**Decision**: Two actions in a default-route cycle using the existing Route model.

**Structure**:
- Participant "Ping" (Id: `ping`)
- Participant "Pong" (Id: `pong`)
- Action 0: "Ping" — sender: `ping`, default route → Action 1
- Action 1: "Pong" — sender: `pong`, default route → Action 0
- Data schema: JSON Schema requiring `message` (string) and `counter` (integer, minimum 1)
- Action 0 is the starting action (`IsStartingAction = true`)

**Rationale**: Route-based routing with `IsDefault = true` and `NextActionIds = [target]` is the standard pattern (see RoutingEngineTests.cs lines 634-681). Two actions keep the blueprint minimal while demonstrating the full cycle.

**Alternatives considered**:
- Single action with self-route → Rejected: doesn't demonstrate participant handoff
- Conditional route with counter limit → Rejected: spec says "loop indefinitely"
- JSON-e template with parameters → Rejected: overkill for a fixed 2-participant blueprint; use direct JSON blueprint

## R3: Template Seeding Mechanism

**Decision**: Blueprint Service startup seeding via `IHostedService` that reads embedded JSON template files and calls the existing template service API.

**Rationale**: No startup seeding exists currently. The `BlueprintTemplateService` stores templates in-memory (Dictionary). A hosted service that runs once at startup can load templates from embedded resources or a `templates/` directory, calling `SaveTemplateAsync()` for each. Idempotency is achieved by checking if template ID already exists before saving.

**Alternatives considered**:
- Seed from `examples/templates/` directory → Partially adopted: move template files into the service project as embedded resources
- Seed via admin CLI command → Rejected: doesn't satisfy "no manual intervention" requirement
- Seed from database → Rejected: templates are in-memory currently; adding DB is out of scope

## R4: Template-to-Instance Flow

**Decision**: Use existing endpoint chain: evaluate template → create blueprint → publish blueprint → create instance.

**Flow**:
1. User selects template in UI
2. UI sends `POST /api/templates/evaluate` with participant assignments
3. Service evaluates template → returns Blueprint JSON
4. UI sends `POST /api/blueprints/` with the blueprint
5. UI sends `POST /api/blueprints/{id}/publish`
6. UI sends `POST /api/instances/` with participant wallet mappings
7. Instance is created with `CurrentActionIds = [0]` (starting action)

**Rationale**: All endpoints already exist. The Ping-Pong doesn't need template parameterization (it's a fixed structure), but using the template evaluate flow keeps the architecture consistent.

**Alternative**: For the Ping-Pong specifically, the blueprint JSON could be stored directly (not as a JSON-e template) since it has no parameters. The template wrapper adds discoverability and category metadata.

## R5: UI Template Library Page

**Decision**: Extend the existing `TemplateList.razor` and `TemplateEvaluator.razor` components into a full-page template library experience in Sorcha.UI.Web.Client.

**Rationale**: The UI Core already has `TemplateListItemViewModel`, `TemplateApiService`, and the `TemplateList` component. These need to be composed into a routable page with template detail view, participant assignment form, and instance creation.

**Existing components**:
- `TemplateList.razor` — card grid of templates with category filter
- `TemplateEvaluator.razor` — parameter form dialog
- `TemplateApiService` — HTTP client for template endpoints

## R6: Action Execution & Counter Validation

**Decision**: Use JSON Schema validation on the action's `DataSchemas` to enforce the counter as an integer ≥ 1. Counter increment validation (ensuring each submission increments by 1) would require a custom calculation rule or a schema that references previous state.

**Rationale**: The engine's validate step already runs JSON Schema validation via `DataSchemas`. For the initial implementation, schema validation ensures correct types. True increment-by-1 validation would require the calculate/route step to check accumulated state, which is a more complex feature. For the MVP, the schema enforces the shape and the counter is a user responsibility.

**Simplification**: Counter increment enforcement is deferred. The schema validates type and minimum; the walkthrough script verifies correct behavior end-to-end.
