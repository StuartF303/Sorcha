---
name: blueprint-builder
description: |
  Creates and maintains Sorcha blueprint JSON templates and workflow definitions.
  Use when: Building new blueprints, creating template JSON files, defining participants/actions/routes/schemas, configuring cycle detection, or troubleshooting blueprint publishing.
allowed-tools: Read, Edit, Write, Glob, Grep, Bash
---

# Blueprint Builder Skill

Sorcha blueprints define multi-participant workflows as JSON documents. Each blueprint has participants, actions (with data schemas), and routes that determine the action flow. Templates wrap blueprints with parameterization for reuse.

## Quick Start

### Minimal Blueprint (Two-Party, No Cycles)

```json
{
  "id": "my-blueprint",
  "title": "My Workflow",
  "description": "A simple two-participant workflow (min 5 chars)",
  "version": 1,
  "metadata": { "category": "demo" },
  "participants": [
    { "id": "sender", "name": "Sender", "description": "Initiates the workflow" },
    { "id": "receiver", "name": "Receiver", "description": "Receives and completes" }
  ],
  "actions": [
    {
      "id": 0,
      "title": "Submit",
      "sender": "sender",
      "isStartingAction": true,
      "dataSchemas": [
        {
          "type": "object",
          "properties": {
            "message": { "type": "string", "minLength": 1 }
          },
          "required": ["message"]
        }
      ],
      "routes": [
        { "id": "to-receiver", "nextActionIds": [1], "isDefault": true }
      ]
    },
    {
      "id": 1,
      "title": "Complete",
      "sender": "receiver",
      "dataSchemas": [
        {
          "type": "object",
          "properties": {
            "status": { "type": "string", "enum": ["accepted", "rejected"] }
          },
          "required": ["status"]
        }
      ],
      "routes": []
    }
  ]
}
```

### Cyclic Blueprint (Looping Workflow)

```json
{
  "metadata": { "hasCycles": "true" },
  "actions": [
    {
      "id": 0, "title": "Ping", "sender": "ping", "isStartingAction": true,
      "routes": [{ "id": "ping-to-pong", "nextActionIds": [1], "isDefault": true }]
    },
    {
      "id": 1, "title": "Pong", "sender": "pong",
      "routes": [{ "id": "pong-to-ping", "nextActionIds": [0], "isDefault": true }]
    }
  ]
}
```

**Cycle detection** produces warnings (not errors). Cyclic blueprints publish with `metadata["hasCycles"] = "true"`.

## Key Concepts

| Concept | Details |
|---------|---------|
| Participants | Min 2 required. Each has `id`, `name`. `id` is referenced by `action.sender` |
| Actions | Sequential IDs starting at 0. One must have `isStartingAction: true` |
| Routes | Define flow between actions. `nextActionIds: []` = workflow completion |
| DataSchemas | JSON Schema for action payload. `IEnumerable<JsonDocument>` in C# |
| Conditions | JSON Logic expressions for conditional routing |
| Calculations | JSON Logic for computed values (e.g., `requiresApproval`) |
| Cycles | Allowed with warning. Set `metadata.hasCycles = "true"` |

## Blueprint Validation Rules

1. **Participant references**: Every `action.sender` must reference a valid `participant.id`
2. **Action count**: At least 1 action required
3. **Participant count**: At least 2 participants required (enforced by `BlueprintBuilder.Build()`)
4. **Description length**: Min 5 characters
5. **Title length**: Min 3 characters
6. **Cycles**: Detected but allowed — produce warnings, not errors

## Route Types

### Default Route (Always Taken)
```json
{ "id": "always", "nextActionIds": [1], "isDefault": true }
```

### Conditional Route (JSON Logic)
```json
{
  "id": "approve-route",
  "nextActionIds": [2],
  "condition": { "==": [{ "var": "decision" }, "approved"] }
}
```

### Terminal Route (Workflow Ends)
```json
{ "id": "complete", "nextActionIds": [], "isDefault": true }
```

### Parallel Branch (Multiple Next Actions)
```json
{
  "id": "parallel-review",
  "nextActionIds": [2, 3],
  "isDefault": true,
  "branchDeadline": "P7D"
}
```

## Route Precedence

Route-based routing (via `Action.Routes`) takes precedence over legacy condition-based routing (via `Action.Participants`). Always use `routes` for new blueprints.

## DataSchema Patterns

### String Field with Validation
```json
{ "type": "string", "minLength": 1, "maxLength": 500, "title": "Message" }
```

### Integer with Minimum
```json
{ "type": "integer", "minimum": 1, "title": "Counter" }
```

### Enum (Fixed Choices)
```json
{ "type": "string", "enum": ["approved", "rejected", "escalate"], "title": "Decision" }
```

### Number with Threshold
```json
{ "type": "number", "minimum": 0, "title": "Amount" }
```

## Template Wrapper

Templates wrap blueprints for reuse with optional parameterization:

```json
{
  "id": "template-id",
  "title": "Template Title",
  "description": "What this template does (min 5 chars)",
  "version": 1,
  "category": "demo|approval|finance|supply-chain",
  "tags": ["tag1", "tag2"],
  "author": "Sorcha Team",
  "published": true,
  "template": { /* raw blueprint JSON or JSON-e template */ },
  "parameterSchema": null,
  "defaultParameters": null,
  "examples": []
}
```

### Fixed Template (No Parameters)
Set `parameterSchema: null` — the `template` field contains the raw blueprint JSON directly. Used for simple blueprints like Ping-Pong.

### Parameterized Template (JSON-e)
Uses JSON-e expressions (`$eval`, `$if`, `$flattenDeep`) in the `template` field. Requires `parameterSchema` (JSON Schema), `defaultParameters`, and `examples`.

**JSON-e expressions:**
- `{ "$eval": "paramName" }` — substitute parameter value
- `{ "$if": "condition", "then": ..., "else": ... }` — conditional inclusion
- `{ "$flattenDeep": [...] }` — flatten nested arrays (for conditional participants/actions)

## Blueprint Publishing Flow

1. `POST /api/blueprints/` — Create draft blueprint
2. `POST /api/blueprints/{id}/publish` — Publish (validates, returns warnings for cycles)
3. `POST /api/instances/` — Create instance with participant wallet mappings

### Publish Response (with cycle warning)
```json
{
  "blueprintId": "...",
  "version": 1,
  "publishedAt": "...",
  "warnings": ["Cyclic route detected: action 0 → action 1 → action 0. This blueprint will loop indefinitely unless routing conditions provide a termination path."]
}
```

## Action Execution

```
POST /api/instances/{id}/actions/{actionId}/execute
Headers: Authorization: Bearer <token>, X-Delegation-Token: <token>
Body: {
  "blueprintId": "string",
  "actionId": "string",
  "instanceId": "string",
  "senderWallet": "string",
  "registerAddress": "string",
  "payloadData": { "message": "hello", "counter": 1 }
}
```

Engine pipeline: **validate** (schema check) → **calculate** (JSON Logic) → **route** (determine next) → **disclose** (visibility rules)

## Common Patterns

### Approval Chain (Linear)
```
Submit(requester) → Review(manager) → Approve(director) → Complete
```

### Ping-Pong (Cyclic)
```
Ping(A) → Pong(B) → Ping(A) → Pong(B) → ... (indefinite)
```

### Conditional Branching
```
Submit → [amount > 10000] → Director Approval
Submit → [amount <= 10000] → Manager Approval
Both → Complete
```

## File Locations

| File | Purpose |
|------|---------|
| `examples/templates/*.json` | Built-in template JSON files |
| `src/Common/Sorcha.Blueprint.Models/` | Blueprint, Action, Route, Participant models |
| `src/Common/Sorcha.Blueprint.Models/BlueprintTemplate.cs` | Template model |
| `src/Core/Sorcha.Blueprint.Engine/` | Execution engine (validate/calculate/route/disclose) |
| `src/Core/Sorcha.Blueprint.Fluent/` | Fluent API for programmatic blueprint creation |
| `src/Services/Sorcha.Blueprint.Service/Program.cs` | PublishService, ValidateBlueprint, DetectCycles |
| `src/Services/Sorcha.Blueprint.Service/Templates/TemplateSeedingService.cs` | Startup seeding |

## See Also

- [patterns](references/patterns.md) - Blueprint design patterns and examples
- [workflows](references/workflows.md) - Publishing and execution workflows

## Related Skills

- **dotnet** - .NET 10 / C# 13 patterns
- **minimal-apis** - Blueprint Service endpoint definitions
- **xunit** - Testing blueprint validation
- **blazor** - Template library UI pages
