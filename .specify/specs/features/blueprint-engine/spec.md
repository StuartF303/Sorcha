# Feature Specification: Blueprint Engine

**Feature Branch**: `blueprint-engine`
**Created**: 2025-12-04
**Status**: Library - 100% API Design Complete
**Input**: Derived from workflow model clarifications and existing `Sorcha.Blueprint.Engine` implementation

## Overview

The Blueprint Engine is a **pure, stateless library** that evaluates blueprint workflows. It receives pre-decrypted state and returns routing decisions, calculations, and disclosure results. The engine has **no service dependencies** - all I/O (fetching transactions, decrypting payloads) is the responsibility of the caller.

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                     BLUEPRINT.ENGINE (Library)                  │
├─────────────────────────────────────────────────────────────────┤
│  Portable, stateless, no service dependencies                   │
│                                                                 │
│  Inputs (provided by caller):                                   │
│  • Blueprint definition (JSON/YAML)                             │
│  • Accumulated state (decrypted data from prior actions)        │
│  • Current action submission data                               │
│  • Participant context (wallet, identity)                       │
│                                                                 │
│  Operations:                                                    │
│  • Validate data against JSON Schema                            │
│  • Evaluate JSON Logic (routing conditions, calculations)       │
│  • Apply selective disclosure rules                             │
│  • Determine next action(s) and recipient(s)                    │
│  • Handle parallel branch evaluation                            │
│                                                                 │
│  Outputs:                                                       │
│  • Routing decision (next action ID, or multiple for branches)  │
│  • Calculated values                                            │
│  • Disclosed data per participant                               │
│  • Validation results with detailed errors                      │
│                                                                 │
│  Does NOT:                                                      │
│  • Fetch data from Register                                     │
│  • Decrypt payloads (caller provides clear data)                │
│  • Encrypt payloads (caller handles via Wallet Service)         │
│  • Build or sign transactions                                   │
│  • Communicate with any external services                       │
└─────────────────────────────────────────────────────────────────┘
```

## Clarifications

### Session 2025-12-04

- Q: Does the engine fetch its own data? → A: No - the engine is a pure library. Caller (Blueprint Service) provides pre-decrypted accumulated state.
- Q: Does the engine handle encryption/decryption? → A: No - encryption/decryption is handled by caller via Wallet Service.
- Q: Can the engine run client-side? → A: Yes - it's portable and can run in Blazor WASM for validation preview, but full workflow execution requires server-side orchestration.
- Q: Does the engine handle parallel branches? → A: Yes - routing can return multiple next actions for parallel execution.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate Action Data (Priority: P0)

As the Blueprint Service, I need to validate submitted action data against the action's schema so that invalid data is rejected before processing.

**Acceptance Scenarios**:

1. **Given** an action with a JSON Schema and valid data, **When** `ValidateAsync` is called, **Then** validation passes with no errors.
2. **Given** an action with required fields and missing data, **When** `ValidateAsync` is called, **Then** validation fails with specific field errors.
3. **Given** an action with format constraints (email, date, etc.), **When** invalid format data is submitted, **Then** validation fails with format-specific errors.

---

### User Story 2 - Evaluate Routing Conditions (Priority: P0)

As the Blueprint Service, I need to determine the next action based on JSON Logic conditions evaluated against accumulated workflow state.

**Acceptance Scenarios**:

1. **Given** a routing condition `{"<": [{"var": "age"}, 18]}` and state `{"age": 17}`, **When** `DetermineRoutingAsync` is called, **Then** the underage route is selected.
2. **Given** a routing condition based on prior action data, **When** the state includes data from action 1, **Then** routing correctly evaluates against accumulated state.
3. **Given** a routing condition that results in multiple branches, **When** evaluated, **Then** multiple next actions are returned.

---

### User Story 3 - Apply Calculations (Priority: P1)

As the Blueprint Service, I need to compute calculated fields using JSON Logic expressions so that derived values are included in action results.

**Acceptance Scenarios**:

1. **Given** a calculation `{"*": [{"var": "quantity"}, {"var": "price"}]}`, **When** `ApplyCalculationsAsync` is called, **Then** the result includes the computed `total` field.
2. **Given** calculations referencing prior action data, **When** state includes prior data, **Then** calculations can reference cross-action values.

---

### User Story 4 - Apply Selective Disclosures (Priority: P1)

As the Blueprint Service, I need to filter action data based on disclosure rules so that each participant receives only their authorized view.

**Acceptance Scenarios**:

1. **Given** disclosure rules granting participant A access to `/name` and participant B access to `/*`, **When** `ApplyDisclosures` is called, **Then** A receives only name, B receives all fields.
2. **Given** nested data and JSON Pointer paths `/address/city`, **When** disclosures are applied, **Then** only the specified nested path is included.

---

### User Story 5 - Handle Parallel Branches (Priority: P2)

As the Blueprint Service, I need routing to support parallel branches so that multiple participants can receive actions simultaneously.

**Acceptance Scenarios**:

1. **Given** a blueprint with parallel branch definition, **When** routing is evaluated, **Then** multiple `NextAction` entries are returned.
2. **Given** parallel branches with different conditions, **When** only some conditions are met, **Then** only the matching branches are activated.

---

### Edge Cases

- What happens when JSON Logic references a field not in state? → Returns `null`, condition evaluates accordingly
- What happens when schema validation encounters circular references? → Detect and fail with clear error
- What happens when routing conditions match no routes? → Return error indicating no valid route
- What happens when routing conditions match multiple routes but blueprint expects single? → Blueprint defines if parallel is allowed per action

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Engine MUST validate data against JSON Schema Draft 2020-12
- **FR-002**: Engine MUST evaluate JSON Logic expressions for routing conditions
- **FR-003**: Engine MUST evaluate JSON Logic expressions for calculations
- **FR-004**: Engine MUST apply JSON Pointer-based disclosure rules
- **FR-005**: Engine MUST support accumulated state from multiple prior actions
- **FR-006**: Engine MUST support parallel routing (multiple next actions)
- **FR-007**: Engine MUST be stateless - all state provided via ExecutionContext
- **FR-008**: Engine MUST NOT have any I/O or service dependencies
- **FR-009**: Engine MUST provide detailed validation error messages with field paths
- **FR-010**: Engine MUST support JSON-e template evaluation for dynamic schemas

### Non-Functional Requirements

- **NFR-001**: Engine MUST be portable to Blazor WASM (no platform-specific code)
- **NFR-002**: Engine MUST complete validation in under 50ms for typical payloads
- **NFR-003**: Engine MUST complete routing evaluation in under 20ms
- **NFR-004**: Engine MUST complete disclosure processing in under 10ms

### Key Entities

- **ExecutionContext**: Immutable context with blueprint, action, data, accumulated state
- **ActionExecutionResult**: Complete result with validation, routing, calculations, disclosures
- **RoutingResult**: Next action(s) with participant(s) - supports parallel branches
- **DisclosureResult**: Filtered data per participant based on disclosure rules
- **ValidationResult**: Pass/fail with detailed error list

### Key Interfaces

```csharp
public interface IExecutionEngine
{
    Task<ActionExecutionResult> ExecuteActionAsync(ExecutionContext context, CancellationToken ct);
    Task<ValidationResult> ValidateAsync(Dictionary<string, object> data, Action action, CancellationToken ct);
    Task<Dictionary<string, object>> ApplyCalculationsAsync(Dictionary<string, object> data, Action action, CancellationToken ct);
    Task<RoutingResult> DetermineRoutingAsync(Blueprint blueprint, Action currentAction, Dictionary<string, object> data, CancellationToken ct);
    List<DisclosureResult> ApplyDisclosures(Dictionary<string, object> data, Action action);
}
```

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Schema validation completes in under 50ms for payloads up to 100KB
- **SC-002**: JSON Logic routing evaluation completes in under 20ms
- **SC-003**: Disclosure processing completes in under 10ms for 10 participants
- **SC-004**: Test coverage exceeds 90% for core logic
- **SC-005**: Zero external service dependencies (verified by project references)
- **SC-006**: Runs successfully in Blazor WASM (verified by integration test)

## Dependencies

### Upstream Dependencies

| Component | Dependency | Purpose |
|-----------|------------|---------|
| **Sorcha.Blueprint.Models** | Blueprint, Action, Disclosure definitions | Domain models |
| **JsonLogic.Net** | JSON Logic evaluation | Routing and calculations |
| **JsonSchema.Net** | JSON Schema validation | Data validation |
| **JsonE.NET** | JSON-e template evaluation | Dynamic schemas |

### Downstream Consumers

| Component | Dependency | Usage |
|-----------|------------|-------|
| **Blueprint Service** | Full execution | Server-side workflow orchestration |
| **Blueprint Designer** | Validation preview | Client-side validation before submission |

## Implementation Notes

### Accumulated State Model

The engine receives accumulated state from all prior actions in the workflow instance. The **Blueprint Service** is responsible for:

1. Querying Register for transactions by `InstanceId`
2. Determining which transactions are needed (blueprint-defined)
3. Decrypting payloads via Wallet Service (delegated access)
4. Merging into accumulated state object
5. Passing to engine via `ExecutionContext.PreviousData`

```
Accumulated State Structure:
{
  "action_1": { /* decrypted payload from action 1 */ },
  "action_2": { /* decrypted payload from action 2 */ },
  ...
}
```

### Parallel Branch Handling

When routing evaluates to multiple next actions:

```csharp
public class RoutingResult
{
    public bool IsParallel { get; init; }
    public List<NextAction> NextActions { get; init; }
    // For parallel: multiple entries
    // For sequential: single entry
}

public class NextAction
{
    public string ActionId { get; init; }
    public string ParticipantId { get; init; }
    public string? BranchId { get; init; }  // For tracking parallel branches
}
```

### Deadlock Prevention (Blueprint Design Responsibility)

The engine evaluates what the blueprint defines. Deadlock prevention is a **blueprint design concern**:

- Blueprints should define timeout actions for parallel branches
- Blueprints should define reduction actions that handle incomplete branches
- Blueprint Designer should warn about potential deadlock patterns
