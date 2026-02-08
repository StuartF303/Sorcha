# Blueprint Design Patterns

## Pattern 1: Simple Two-Party Exchange

**Use case**: One party sends, other receives and responds once.

```json
{
  "participants": [
    { "id": "sender", "name": "Sender" },
    { "id": "receiver", "name": "Receiver" }
  ],
  "actions": [
    {
      "id": 0, "title": "Send", "sender": "sender", "isStartingAction": true,
      "routes": [{ "id": "to-recv", "nextActionIds": [1], "isDefault": true }]
    },
    {
      "id": 1, "title": "Respond", "sender": "receiver",
      "routes": []
    }
  ]
}
```

**Key**: Last action has empty `routes: []` to terminate workflow.

---

## Pattern 2: Cyclic Exchange (Ping-Pong)

**Use case**: Two parties alternate indefinitely.

```json
{
  "metadata": { "hasCycles": "true" },
  "participants": [
    { "id": "ping", "name": "Ping" },
    { "id": "pong", "name": "Pong" }
  ],
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

**Key**: Set `metadata.hasCycles = "true"`. Publish returns warnings (not errors).

---

## Pattern 3: Linear Approval Chain

**Use case**: Sequential approvals (requester → manager → director → executive).

```json
{
  "participants": [
    { "id": "requester", "name": "Requester" },
    { "id": "manager", "name": "Manager" },
    { "id": "director", "name": "Director" }
  ],
  "actions": [
    {
      "id": 0, "title": "Submit", "sender": "requester", "isStartingAction": true,
      "routes": [{ "id": "to-mgr", "nextActionIds": [1], "isDefault": true }]
    },
    {
      "id": 1, "title": "Manager Review", "sender": "manager",
      "dataSchemas": [{
        "type": "object",
        "properties": {
          "decision": { "type": "string", "enum": ["approved", "rejected"] }
        },
        "required": ["decision"]
      }],
      "routes": [
        { "id": "mgr-approve", "nextActionIds": [2], "condition": { "==": [{ "var": "decision" }, "approved"] } },
        { "id": "mgr-reject", "nextActionIds": [], "condition": { "==": [{ "var": "decision" }, "rejected"] } }
      ]
    },
    {
      "id": 2, "title": "Director Approval", "sender": "director",
      "routes": []
    }
  ]
}
```

**Key**: Conditional routes use JSON Logic. `nextActionIds: []` terminates on rejection.

---

## Pattern 4: Conditional Branching

**Use case**: Different paths based on data (e.g., amount thresholds).

```json
{
  "actions": [
    {
      "id": 0, "title": "Submit", "sender": "requester", "isStartingAction": true,
      "dataSchemas": [{
        "type": "object",
        "properties": { "amount": { "type": "number", "minimum": 0 } },
        "required": ["amount"]
      }],
      "routes": [
        { "id": "high-value", "nextActionIds": [2], "condition": { ">": [{ "var": "amount" }, 10000] } },
        { "id": "low-value", "nextActionIds": [1], "isDefault": true }
      ]
    }
  ]
}
```

**Key**: Specific conditions evaluated first; `isDefault: true` is the fallback.

---

## Pattern 5: Parallel Branches

**Use case**: Multiple reviewers simultaneously.

```json
{
  "routes": [
    { "id": "parallel", "nextActionIds": [1, 2], "isDefault": true, "branchDeadline": "P3D" }
  ]
}
```

**Key**: Multiple IDs in `nextActionIds` creates parallel execution. `branchDeadline` is ISO 8601 duration.

---

## Pattern 6: Parameterized Template (JSON-e)

**Use case**: Reusable workflow with configurable structure.

```json
{
  "template": {
    "$eval": "blueprintTemplate",
    "context": {
      "blueprintTemplate": {
        "participants": {
          "$flattenDeep": [
            [{ "id": "requester", "name": "Requester" }],
            { "$if": "needsManager", "then": [{ "id": "manager", "name": { "$eval": "managerTitle" } }], "else": [] }
          ]
        }
      }
    }
  },
  "parameterSchema": {
    "type": "object",
    "properties": {
      "needsManager": { "type": "boolean" },
      "managerTitle": { "type": "string", "default": "Manager" }
    },
    "required": ["needsManager"]
  },
  "defaultParameters": { "needsManager": true, "managerTitle": "Manager" }
}
```

**Key**: `$flattenDeep` handles conditional array elements. `$eval` substitutes parameter values.

---

## DataSchema Best Practices

1. **Always use `"required"` array** — list all mandatory fields
2. **Use `"type": "object"` as root** — wrap all fields in an object schema
3. **Add `"title"` for UI display** — human-readable field labels
4. **Validate at boundaries** — `minLength`, `minimum`, `enum` for user input
5. **Keep schemas simple** — avoid `$ref` or complex compositions

## Route Best Practices

1. **Always provide at least one route** (or empty array for terminal actions)
2. **Use `isDefault: true`** on exactly one route per action as fallback
3. **Conditional routes evaluated first**, default route as fallback
4. **Terminal actions**: `"routes": []` or route with `"nextActionIds": []`
5. **Give routes descriptive IDs** — e.g., `"approve-route"`, `"reject-to-requester"`

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Missing `isStartingAction: true` | Exactly one action must be the starting action |
| Action `sender` doesn't match participant `id` | Sender must reference a valid participant ID |
| Using `participants` (legacy) instead of `routes` | Use `routes` for all new blueprints |
| Forgetting `metadata.hasCycles` on cyclic blueprints | Set `"hasCycles": "true"` in metadata |
| Description < 5 chars | `BlueprintBuilder.Build()` enforces min 5 chars |
| < 2 participants | `BlueprintBuilder.Build()` enforces min 2 participants |
| Action ID not sequential from 0 | IDs should be 0, 1, 2, 3... |
