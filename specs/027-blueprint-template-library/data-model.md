# Data Model: Blueprint Template Library & Ping-Pong Blueprint

**Date**: 2026-02-08
**Branch**: `027-blueprint-template-library`

## Entities

### Ping-Pong Blueprint (JSON)

The Ping-Pong blueprint is a direct JSON document (not a JSON-e parameterized template) stored as a built-in template.

```json
{
  "id": "ping-pong-001",
  "title": "Ping-Pong",
  "description": "A simple two-participant workflow where a text message and counter are passed back and forth. Demonstrates the core action submission pipeline.",
  "version": 1,
  "metadata": {
    "category": "demo",
    "author": "Sorcha Team",
    "hasCycles": "true"
  },
  "participants": [
    {
      "id": "ping",
      "name": "Ping",
      "description": "Initiates the exchange and responds to Pong"
    },
    {
      "id": "pong",
      "name": "Pong",
      "description": "Responds to Ping and sends back"
    }
  ],
  "actions": [
    {
      "id": 0,
      "title": "Ping",
      "sender": "ping",
      "isStartingAction": true,
      "dataSchemas": [
        {
          "type": "object",
          "properties": {
            "message": { "type": "string", "minLength": 1 },
            "counter": { "type": "integer", "minimum": 1 }
          },
          "required": ["message", "counter"]
        }
      ],
      "routes": [
        {
          "id": "ping-to-pong",
          "nextActionIds": [1],
          "isDefault": true,
          "description": "Route to Pong participant"
        }
      ]
    },
    {
      "id": 1,
      "title": "Pong",
      "sender": "pong",
      "dataSchemas": [
        {
          "type": "object",
          "properties": {
            "message": { "type": "string", "minLength": 1 },
            "counter": { "type": "integer", "minimum": 1 }
          },
          "required": ["message", "counter"]
        }
      ],
      "routes": [
        {
          "id": "pong-to-ping",
          "nextActionIds": [0],
          "isDefault": true,
          "description": "Route back to Ping participant"
        }
      ]
    }
  ]
}
```

### Payload Schema (per action)

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| message | string | yes | minLength: 1 |
| counter | integer | yes | minimum: 1 |

### BlueprintTemplate Wrapper

The Ping-Pong blueprint is wrapped in a `BlueprintTemplate` for the template library:

| Field | Value |
|-------|-------|
| Id | `ping-pong-001` |
| Title | `Ping-Pong` |
| Description | A simple two-participant workflow... |
| Version | 1 |
| Category | `demo` |
| Tags | `["demo", "simple", "two-party", "looping"]` |
| Template | The blueprint JSON above (as JsonNode) |
| ParameterSchema | null (no parameters — fixed structure) |
| DefaultParameters | null |
| Published | true |
| Author | `Sorcha Team` |

### Instance State Transitions

```
Created (CurrentActionIds=[0])
    ↓ Ping submits action 0
Active (CurrentActionIds=[1])
    ↓ Pong submits action 1
Active (CurrentActionIds=[0])
    ↓ Ping submits action 0
Active (CurrentActionIds=[1])
    ↓ ... cycles indefinitely
```

Instance never reaches `Completed` state unless manually cancelled — the cycle has no termination route.

## Existing Entities (unchanged)

- **Blueprint** — `src/Common/Sorcha.Blueprint.Models/Blueprint.cs`
- **BlueprintTemplate** — `src/Common/Sorcha.Blueprint.Models/BlueprintTemplate.cs`
- **Action** — `src/Common/Sorcha.Blueprint.Models/Action.cs` (with Routes)
- **Route** — `src/Common/Sorcha.Blueprint.Models/Route.cs`
- **Instance** — `src/Services/Sorcha.Blueprint.Service/Models/Instance.cs`

## Modification: PublishService Cycle Detection

**Current**: `DetectCycles()` returns errors → publish rejected
**Proposed**: `DetectCycles()` returns warnings → publish succeeds with `hasCycles` metadata

```
ValidateBlueprint(blueprint)
  ├── Check participants (≥2) → error if violated
  ├── Check actions (≥1) → error if violated
  ├── Check participant references → error if violated
  └── DetectCycles() → WARNING (not error), set metadata["hasCycles"] = "true"
```
