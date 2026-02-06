# Data Model: Blueprint Engine Integration

**Feature**: 018-blueprint-engine-integration
**Date**: 2026-02-06

## Existing Entities (No Changes Required)

These entities already exist and are well-defined. Listed for reference.

### Route (Sorcha.Blueprint.Models.Route)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Id | string | Yes | "" | Max 100 chars |
| NextActionIds | IEnumerable\<int\> | Yes | [] | Multiple = parallel branches |
| Condition | JsonNode? | No | null | JSON Logic expression |
| IsDefault | bool | No | false | Only one per action |
| Description | string? | No | null | Max 500 chars |
| BranchDeadline | string? | No | null | ISO 8601 duration |

### RejectionConfig (Sorcha.Blueprint.Models.RejectionConfig)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| TargetActionId | int | Yes | - | Where to route on rejection |
| TargetParticipantId | string? | No | null | Override participant |
| RequireReason | bool | No | true | |
| RejectionSchema | JsonElement? | No | null | Structured rejection data schema |
| IsTerminal | bool | No | false | Terminates workflow |

### ExecutionContext (Sorcha.Blueprint.Engine.Models)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Blueprint | Blueprint | Yes | - | Immutable |
| Action | Action | Yes | - | Current action |
| ActionData | Dictionary\<string, object\> | Yes | - | Submitted data |
| PreviousData | Dictionary\<string, object\>? | No | null | Historical state |
| PreviousTransactionHash | string? | No | null | Chain linkage |
| InstanceId | string? | No | null | Workflow instance |
| ParticipantId | string | Yes | - | Executing participant |
| WalletAddress | string | Yes | - | Signing address |
| Mode | ExecutionMode | No | Full | ValidationOnly or Full |

### ActionExecutionResult (Sorcha.Blueprint.Engine.Models)
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Success | bool | false | Overall result |
| Validation | ValidationResult | new() | Schema validation results |
| ProcessedData | Dictionary\<string, object\> | new() | Data with calculations applied |
| CalculatedValues | Dictionary\<string, object\> | new() | Computed values only |
| Routing | RoutingResult | new() | Next action(s) |
| Disclosures | List\<DisclosureResult\> | new() | Per-participant filtered data |
| Errors | List\<string\> | new() | Error messages |
| Warnings | List\<string\> | new() | Warning messages |

## Modified Entities

### RoutingResult (Sorcha.Blueprint.Engine.Models) -- MODIFY

**Current state**: Single next action only (`NextActionId`, `NextParticipantId`).

**Addition**: Support parallel branches with a `NextActions` list.

| Field | Type | Default | Status | Notes |
|-------|------|---------|--------|-------|
| NextActionId | string? | null | EXISTING | Backward-compatible singular |
| NextParticipantId | string? | null | EXISTING | |
| IsWorkflowComplete | bool | false | EXISTING | |
| RejectedToParticipantId | string? | null | EXISTING | |
| MatchedCondition | string? | null | EXISTING | |
| **NextActions** | **List\<RoutedAction\>** | **[]** | **NEW** | Parallel branch support |
| **IsParallel** | **bool** | **false** | **NEW** | True when multiple next actions |

**New nested type: RoutedAction**
| Field | Type | Notes |
|-------|------|-------|
| ActionId | string | Target action ID |
| ParticipantId | string? | Target participant |
| BranchId | string? | Unique branch identifier |
| MatchedRouteId | string? | Which route was matched |

**Static factory updates**:
- `Next()` populates both `NextActionId` and `NextActions[0]` for backward compatibility
- New `Parallel(List<RoutedAction> actions)` factory for multi-branch results

## State Transitions

### Blueprint Execution Pipeline (Engine)

```
ActionData submitted
    │
    ▼
[1] Schema Validation ──── Mode=ValidationOnly? ──▶ Return ValidationResult
    │                              (short-circuit)
    │ (Mode=Full)
    ▼
[2] Apply Calculations ── JSON Logic evaluation of expressions
    │
    ▼
[3] Determine Routing ── Routes? → Route-based (new)
    │                     No Routes? → Condition-based (legacy)
    │                     No conditions? → Workflow complete
    ▼
[4] Apply Disclosures ── JSON Pointer filtering per participant
    │
    ▼
ActionExecutionResult returned
```

### Route Evaluation Order

```
For each Route in Action.Routes (ordered):
    │
    ├── Condition is null AND IsDefault? ── Save as default route
    │
    ├── Condition is null AND NOT IsDefault? ── Skip (invalid)
    │
    └── Condition present? ── Evaluate JSON Logic against data
            │
            ├── Matches? ── Use this route's NextActionIds ── DONE
            │
            └── No match? ── Continue to next route
                    │
                    └── No more routes? ── Use saved default route
                            │
                            └── No default? ── Workflow complete
```

### Graph Cycle Detection (Publish Validation)

```
Build adjacency list:
    For each Action:
        ├── Add edges from Action.Routes[].NextActionIds
        └── Add edge from Action.RejectionConfig.TargetActionId

Run DFS with coloring:
    WHITE = unvisited
    GRAY  = in current path (cycle if revisited)
    BLACK = fully explored

    For each action node:
        If GRAY encountered → CYCLE DETECTED → collect path → ERROR
        If all BLACK → No cycles → PASS
```
