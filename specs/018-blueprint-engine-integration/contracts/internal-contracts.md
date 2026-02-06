# Internal Contracts: Blueprint Engine Integration

**Feature**: 018-blueprint-engine-integration
**Date**: 2026-02-06

> No new public API endpoints are created. This feature modifies the behavior of existing internal interfaces and endpoints. These contracts document the internal integration points.

## Contract 1: Engine ↔ ActionExecutionService Delegation

### IExecutionEngine Methods Used by ActionExecutionService

```
ValidateAsync(data: Dictionary<string,object>, action: Action) → ValidationResult
  - Called by: ActionExecutionService.ExecuteAsync() step 6
  - Replaces: stub ValidateActionDataAsync() (field-presence-only)
  - Maps to: service ValidationResult { IsValid, Errors, Warnings }

ApplyCalculationsAsync(data: Dictionary<string,object>, action: Action) → Dictionary<string,object>
  - Called by: ActionExecutionService.ExecuteAsync() step 8
  - Replaces: stub EvaluateCalculations() (stores expressions as strings)
  - Returns: computed values directly

DetermineRoutingAsync(blueprint: Blueprint, action: Action, data: Dictionary<string,object>) → RoutingResult
  - Called by: ActionExecutionService.ExecuteAsync() step 7
  - Replaces: stub EvaluateRouting() + EvaluateJsonLogicCondition()
  - Maps to: service RoutingResult { NextActions, IsParallel }

ApplyDisclosures(data: Dictionary<string,object>, action: Action) → List<DisclosureResult>
  - Called by: ActionExecutionService.ExecuteAsync() step 9
  - Replaces: inline ApplyDisclosures() with simplified logic
  - Maps to: Dictionary<string, Dictionary<string,object>> (walletAddress → filtered data)
```

## Contract 2: RoutingEngine Route Evaluation

### Input
```
DetermineNextAsync(blueprint: Blueprint, action: Action, data: Dictionary<string,object>)
```

### Evaluation Logic
```
IF action.Routes is not null and has entries:
    FOR EACH route IN action.Routes (in order):
        IF route.Condition is null AND route.IsDefault:
            Save as fallback default route
        ELSE IF route.Condition is not null:
            Evaluate JSON Logic condition against data
            IF matches:
                RETURN RoutingResult with route.NextActionIds
        END
    END
    IF default route saved:
        RETURN RoutingResult with default route's NextActionIds
    RETURN RoutingResult.Complete()
ELSE IF action.Participants has entries:
    (Legacy Condition-based routing - existing behavior)
ELSE:
    RETURN RoutingResult.Complete()
```

### Output
```
Single route match:  RoutingResult { NextActionId, NextParticipantId, NextActions[1] }
Parallel branches:   RoutingResult { NextActions[N], IsParallel = true }
Workflow complete:    RoutingResult { IsWorkflowComplete = true }
```

## Contract 3: Fluent API Builder Signatures

### RouteBuilder
```
RouteBuilder
  .ToActions(params int[] nextActionIds)    → RouteBuilder  [required]
  .When(Func<JsonLogicBuilder, JsonNode>)   → RouteBuilder  [optional]
  .AsDefault()                               → RouteBuilder  [optional]
  .WithDescription(string)                   → RouteBuilder  [optional]
  .WithBranchDeadline(string isoDuration)   → RouteBuilder  [optional]
  .Build()                                   → Route         [internal]
```

### RejectionConfigBuilder
```
RejectionConfigBuilder
  .RouteToAction(int targetActionId)         → RejectionConfigBuilder  [required]
  .WithTargetParticipant(string)             → RejectionConfigBuilder  [optional]
  .RequireReason(bool require = true)        → RejectionConfigBuilder  [optional]
  .WithRejectionSchema(JsonElement)          → RejectionConfigBuilder  [optional]
  .AsTerminal()                               → RejectionConfigBuilder  [optional]
  .Build()                                    → RejectionConfig         [internal]
```

### ActionBuilder Additions
```
ActionBuilder
  .AddRoute(string routeId, Action<RouteBuilder>)  → ActionBuilder
  .WithDefaultRoute(params int[] nextActionIds)     → ActionBuilder
  .OnRejection(Action<RejectionConfigBuilder>)      → ActionBuilder
  .AsStartingAction()                                → ActionBuilder
  .RequiresPriorActions(params int[] actionIds)     → ActionBuilder
```

## Contract 4: Graph Cycle Detection

### Input
```
Blueprint with Actions, each having:
  - Routes[].NextActionIds (list of target action IDs)
  - RejectionConfig.TargetActionId (single target)
```

### Output
```
Success: validation passes, no errors
Failure: List<string> errors containing:
  - "Circular dependency detected: Action {A} → Action {B} → Action {C} → Action {A}"
  - "Self-referencing route detected: Action {A} routes to itself"
```

## Contract 5: JsonLogicCache Integration

### Cache Key
```
SHA256(expression.ToJsonString()) → Base64 string
```

### Cached Value
```
Json.Logic.Rule (deserialized from JsonNode)
```

### Cache Configuration
```
MaxEntries: 1000
AbsoluteExpiration: 1 hour
SlidingExpiration: 15 minutes
Lifetime: Singleton (shared across scoped evaluators)
```
