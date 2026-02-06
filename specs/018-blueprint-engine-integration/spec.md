# Feature Specification: Blueprint Engine Integration

**Feature Branch**: `018-blueprint-engine-integration`
**Created**: 2026-02-06
**Status**: Draft
**Input**: Wire Blueprint Engine into ActionExecutionService and fix all stub implementations, implement Route-based routing, add Fluent API builders, wire caching, implement ValidationOnly mode, fix transaction builder stubs, implement full disclosure, and add graph cycle detection.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Workflow Conditional Routing Works Correctly (Priority: P1)

A workflow designer creates a blueprint with conditional routes (e.g., loan amount above $10,000 requires senior approval, below goes to standard approval). When a participant submits action data, the system evaluates the JSON Logic conditions against the submitted data and accumulated state, and routes to the correct next action and participant.

**Why this priority**: Without working routing, workflows cannot branch conditionally -- every condition evaluates to true, making multi-path workflows impossible. This is the core of workflow orchestration.

**Independent Test**: Can be tested by submitting action data to a blueprint with conditional routes and verifying the correct next action is returned in the response.

**Acceptance Scenarios**:

1. **Given** a blueprint with two routes (one conditional, one default) on an action, **When** a participant submits data that matches the conditional route, **Then** the system routes to the conditional route's next action(s).
2. **Given** a blueprint with two routes on an action, **When** a participant submits data that matches no conditional route, **Then** the system falls back to the default route.
3. **Given** a blueprint with routes containing multiple next action IDs, **When** the route condition matches, **Then** parallel branches are created for each next action ID.
4. **Given** a blueprint using legacy Condition-based routing (no Routes defined), **When** conditions are evaluated, **Then** the system falls back to Condition-based evaluation and finds the correct next participant/action.

---

### User Story 2 - Action Data Is Validated Against JSON Schemas (Priority: P1)

When a participant submits data for a workflow action, the system validates the submitted data against the action's defined JSON Schema(s). If validation fails, the system returns clear error messages identifying which fields failed and why, without creating any transactions.

**Why this priority**: Without schema validation, invalid data enters the workflow pipeline and gets recorded on the ledger. This undermines data integrity, which is a core principle of the DAD security model.

**Independent Test**: Can be tested by submitting invalid data to an action with a JSON Schema and verifying the system returns validation errors without submitting a transaction.

**Acceptance Scenarios**:

1. **Given** an action with a JSON Schema requiring fields "name" (string) and "amount" (number), **When** a participant submits data missing "name", **Then** the system returns a validation error identifying the missing field.
2. **Given** an action with a JSON Schema, **When** a participant submits data with a field of the wrong type, **Then** the system returns a type mismatch error.
3. **Given** an action with no JSON Schema defined, **When** a participant submits data, **Then** only required field presence is checked (backward-compatible behavior).
4. **Given** an action with multiple JSON Schemas, **When** data is submitted, **Then** all schemas are evaluated and all errors are collected before returning.

---

### User Story 3 - Calculations Are Evaluated During Execution (Priority: P1)

When a workflow action defines calculations (e.g., compute total = quantity * price, or apply discount), the system evaluates these JSON Logic expressions against the submitted data and accumulated state, producing computed values that are included in the execution response and available to subsequent actions.

**Why this priority**: Calculations enable derived fields (totals, taxes, discounts) that downstream actions depend on. Without working calculations, computed values are stored as raw expression strings instead of results.

**Independent Test**: Can be tested by submitting data to an action with calculations and verifying the response contains computed numerical results, not expression strings.

**Acceptance Scenarios**:

1. **Given** an action with a calculation "total = quantity * unitPrice", **When** data is submitted with quantity=5 and unitPrice=10.00, **Then** the response includes total=50.00.
2. **Given** an action with chained calculations where "subtotal" feeds into "tax", **When** data is submitted, **Then** later calculations can reference earlier calculation results.
3. **Given** an action with a calculation referencing accumulated state from a prior action, **When** data is submitted, **Then** the calculation uses the merged current + historical data.
4. **Given** a calculation that fails (e.g., division by zero), **When** data is submitted, **Then** the error is logged and the calculation field is omitted without failing the entire execution.

---

### User Story 4 - Selective Disclosure Filters Data Per Recipient (Priority: P1)

When an action defines disclosure rules specifying which data each participant can see, the system filters the submitted data using JSON Pointer paths so that each recipient's encrypted payload contains only the fields they are authorized to view. This is the core enforcement of the DAD (Disclosure) security model.

**Why this priority**: Without proper disclosure, all participants receive the full payload, violating the DAD model's fundamental data privacy guarantees. This is a security-critical feature.

**Independent Test**: Can be tested by executing an action with disclosure rules and verifying each recipient's payload contains only their authorized fields.

**Acceptance Scenarios**:

1. **Given** an action that discloses "/name" and "/email" to participant A but only "/name" to participant B, **When** data is submitted, **Then** participant A's payload contains name and email, while participant B's payload contains only name.
2. **Given** a disclosure rule with wildcard path "/*", **When** data is submitted, **Then** the recipient receives all fields.
3. **Given** a disclosure rule with nested JSON Pointer paths (e.g., "/address/city"), **When** data is submitted, **Then** only the nested field is included in the recipient's payload.
4. **Given** the direct action submission endpoint (POST /api/actions), **When** an action is submitted, **Then** disclosure rules are applied per recipient rather than sending full payload to all participants.

---

### User Story 5 - Route-Based Routing with Parallel Branches (Priority: P2)

A workflow designer creates a blueprint using the Route model (with explicit next action IDs, conditions, default routes, and branch deadlines). The execution engine evaluates routes in order, supports parallel branching when a route specifies multiple next action IDs, and respects default route fallback.

**Why this priority**: Route-based routing is the newer, more expressive routing model that supports parallel branches and explicit action references. It is already defined in the domain models but not implemented in the engine.

**Independent Test**: Can be tested by creating a blueprint with Route definitions and verifying the engine evaluates them correctly, including parallel branch creation.

**Acceptance Scenarios**:

1. **Given** a blueprint action with Routes defined, **When** the engine evaluates routing, **Then** Routes are used (not legacy Condition routing).
2. **Given** a route with multiple NextActionIds, **When** the route condition matches, **Then** parallel branches are created with unique branch IDs.
3. **Given** an action with both Routes and legacy Conditions, **When** the engine evaluates routing, **Then** Routes take precedence over legacy Conditions.
4. **Given** multiple routes where the first has a condition and the second is default, **When** the condition does not match, **Then** the default route is used.

---

### User Story 6 - Fluent API Supports Route and Rejection Configuration (Priority: P2)

A developer building blueprints programmatically can define Routes, RejectionConfig, IsStartingAction, and RequiredPriorActions using the Fluent API builders, enabling full use of the Route-based routing model and rejection workflows without writing raw JSON.

**Why this priority**: The Fluent API is the primary programmatic interface for blueprint construction. Without Route/Rejection builders, developers cannot use the newer routing model without manually constructing JSON.

**Independent Test**: Can be tested by building a blueprint with the Fluent API using route builders and verifying the resulting blueprint model contains correctly configured Routes and RejectionConfig.

**Acceptance Scenarios**:

1. **Given** the Fluent API, **When** a developer calls route-building methods on an ActionBuilder, **Then** the resulting action has properly configured Route objects with conditions, next action IDs, and default flags.
2. **Given** the Fluent API, **When** a developer configures rejection on an ActionBuilder, **Then** the resulting action has a properly configured RejectionConfig with target action, reason requirement, and terminal flag.
3. **Given** the Fluent API, **When** a developer marks an action as a starting action, **Then** the resulting action has IsStartingAction = true.
4. **Given** the Fluent API, **When** a developer specifies required prior actions, **Then** the resulting action has the correct RequiredPriorActions list.

---

### User Story 7 - Blueprint Publish Validates Action Graph Integrity (Priority: P2)

When a workflow designer publishes a blueprint, the system validates that the action graph has no circular dependencies. This prevents infinite loops during workflow execution where actions route back to themselves in cycles.

**Why this priority**: Without cycle detection, publishing a blueprint with circular routes could cause infinite execution loops, resource exhaustion, and corrupted workflow state.

**Independent Test**: Can be tested by attempting to publish a blueprint with circular action references and verifying the system rejects it with a clear error message.

**Acceptance Scenarios**:

1. **Given** a blueprint where action A routes to B, B routes to C, and C routes back to A, **When** publish is attempted, **Then** the system rejects it with an error identifying the cycle.
2. **Given** a blueprint with valid linear or branching routes (no cycles), **When** publish is attempted, **Then** validation passes.
3. **Given** a blueprint with self-referencing routes (action A routes to itself), **When** publish is attempted, **Then** the system rejects it with an error identifying the self-reference.

---

### User Story 8 - ValidationOnly Mode for Client-Side Feedback (Priority: P3)

A Blazor WASM client uses the Blueprint Engine in ValidationOnly mode to provide instant validation feedback as users fill in form data, without triggering calculations, routing, or transaction creation. This enables responsive client-side validation before server submission.

**Why this priority**: Improves user experience by providing instant feedback. The engine is designed to run in WASM, but ValidationOnly mode is not yet respected.

**Independent Test**: Can be tested by executing the engine in ValidationOnly mode and verifying only schema validation runs, with no calculations or routing.

**Acceptance Scenarios**:

1. **Given** an ExecutionContext with Mode = ValidationOnly, **When** the engine processes the action, **Then** only schema validation is performed.
2. **Given** an ExecutionContext with Mode = Full, **When** the engine processes the action, **Then** all four steps (validate, calculate, route, disclose) are performed.

---

### User Story 9 - JSON Logic Expression Caching Improves Performance (Priority: P3)

When the same JSON Logic expressions are evaluated repeatedly across multiple workflow executions (e.g., the same routing condition for a popular blueprint), the system caches parsed expressions to avoid redundant parsing, improving throughput for high-volume workflows.

**Why this priority**: Performance optimization for production workloads. The cache infrastructure exists but is not connected.

**Independent Test**: Can be tested by evaluating the same expression multiple times and verifying cache hits via metrics or reduced evaluation time.

**Acceptance Scenarios**:

1. **Given** a JSON Logic expression that has been evaluated before, **When** the same expression is evaluated again, **Then** the cached result is used instead of re-parsing.
2. **Given** a cache that reaches its size limit, **When** new expressions are added, **Then** least-recently-used entries are evicted.

---

### Edge Cases

- What happens when a route condition references a field that doesn't exist in the data? The system should treat missing fields as null/falsy and fall through to the next route.
- What happens when an action has Routes defined but all routes have conditions and none match (no default route)? The system should treat this as workflow completion (no next action).
- What happens when a blueprint has disconnected action subgraphs (actions unreachable from any starting action)? Cycle detection should also identify unreachable actions as warnings.
- What happens when the TransactionBuilder receives empty disclosed payloads for a recipient? The transaction should still be created with an empty encrypted payload for that recipient.
- What happens when parallel branches have overlapping next action IDs? Each branch should track independently even if they converge to the same action.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The orchestration service MUST delegate schema validation to the Blueprint Engine's schema validator for all action submissions, replacing the current field-presence-only check.
- **FR-002**: The orchestration service MUST delegate JSON Logic condition evaluation to the Blueprint Engine's evaluator for all routing decisions, replacing the current stub that returns true for equality conditions.
- **FR-003**: The orchestration service MUST delegate calculation evaluation to the Blueprint Engine's JSON Logic evaluator, replacing the current stub that stores expressions as strings.
- **FR-004**: The orchestration service MUST delegate disclosure processing to the Blueprint Engine's disclosure processor, replacing the current simplified inline implementation.
- **FR-005**: The Blueprint Engine's routing engine MUST support Route-based routing (evaluating Action.Routes with conditions, next action IDs, default routes, and parallel branches) in addition to legacy Condition-based routing.
- **FR-006**: Route-based routing MUST take precedence over legacy Condition-based routing when both are present on an action.
- **FR-007**: When a Route specifies multiple NextActionIds, the engine MUST create parallel branch results with unique identifiers.
- **FR-008**: The Fluent API MUST provide builders for Route configuration (conditions, next action IDs, default flag, branch deadline).
- **FR-009**: The Fluent API MUST provide builders for RejectionConfig (target action, target participant, require reason, terminal flag).
- **FR-010**: The Fluent API MUST support marking actions as starting actions and specifying required prior actions.
- **FR-011**: Blueprint publish validation MUST detect and reject circular action dependencies using graph traversal.
- **FR-012**: The Blueprint Engine's action processor MUST respect ExecutionMode, running only schema validation in ValidationOnly mode.
- **FR-013**: The JSON Logic evaluator MUST use the existing JsonLogicCache for expression caching to avoid redundant parsing.
- **FR-014**: The transaction builder extension methods MUST produce valid transaction data with properly serialized and encrypted payloads, replacing the current empty byte array stub.
- **FR-015**: The direct action submission endpoint (POST /api/actions) MUST apply disclosure rules per recipient rather than sending full payload to all participants.

### Key Entities

- **Route**: Conditional routing rule with ID, NextActionIds (supporting parallel branches), JSON Logic Condition, IsDefault flag, and optional BranchDeadline.
- **RejectionConfig**: Rejection routing configuration with TargetActionId, optional TargetParticipantId override, RequireReason flag, optional RejectionSchema, and IsTerminal flag.
- **ExecutionContext**: Immutable context for engine execution containing Blueprint, Action, ActionData, PreviousData, ParticipantId, WalletAddress, and ExecutionMode.
- **ActionExecutionResult**: Result of engine execution containing validation results, calculated values, routing result, and disclosed payloads.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All conditional routing decisions in workflows produce correct next actions based on submitted data -- verified by executing blueprints with known conditional routes and confirming the correct branches are taken 100% of the time.
- **SC-002**: Action data submitted with schema violations is rejected with specific error messages before any transaction is created -- verified by submitting invalid data and confirming rejection with field-level errors.
- **SC-003**: Calculated fields in workflow responses contain evaluated numerical results, not raw expression strings -- verified by executing actions with arithmetic calculations and confirming correct computed values.
- **SC-004**: Each workflow participant receives only the data they are authorized to view per disclosure rules -- verified by executing actions with per-recipient disclosure rules and confirming filtered payloads.
- **SC-005**: Blueprints with circular action dependencies are rejected at publish time with clear error messages identifying the cycle -- verified by attempting to publish blueprints with known cycles.
- **SC-006**: All existing Blueprint Engine tests (319+) continue to pass after changes, and new tests cover Route-based routing, Fluent API builders, cache integration, and ValidationOnly mode, achieving >85% code coverage on changed files.
- **SC-007**: The Fluent API can construct blueprints using Route-based routing and rejection configuration without writing raw JSON -- verified by building a complete multi-route blueprint using only the Fluent API.
- **SC-008**: Repeated evaluation of identical JSON Logic expressions shows measurable cache hit rates when the cache is active -- verified by evaluating the same expression multiple times and confirming cache utilization.

## Assumptions

- The existing Blueprint Engine components (ISchemaValidator, IJsonLogicEvaluator, IDisclosureProcessor, IRoutingEngine) are well-tested (319 tests) and functionally correct. The integration work primarily involves wiring these into the service layer.
- The Route model takes precedence over legacy Condition routing. Legacy Condition routing is preserved as a fallback for backward compatibility with existing blueprints.
- Graph cycle detection applies to both Route-based routing (NextActionIds) and legacy Condition-based routing paths.
- The TransactionBuilder extension method fix involves properly serializing and encrypting payload data through the existing TransactionBuilder pipeline rather than returning empty byte arrays.
- The JsonLogicCache uses the existing IMemoryCache infrastructure already registered in DI; no new caching infrastructure is needed.
- Parallel branch tracking uses the existing Instance.ActiveBranches model -- no new storage models are needed for branch state.
