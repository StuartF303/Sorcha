# Data Model: Blueprint Service

**Created**: 2025-12-04
**Source**: [spec.md](./spec.md), [plan.md](./plan.md)

## Entity Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN ENTITIES                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Blueprint (Workflow Definition)                                │
│  ├── Participant[] (Who can participate)                        │
│  ├── Action[] (Steps in workflow)                               │
│  │   ├── Disclosure[] (Data visibility rules)                   │
│  │   ├── Route[] (Next action conditions)                       │
│  │   ├── Calculation[] (Computed fields)                        │
│  │   └── Control[] (Form elements)                              │
│  └── Schema[] (Data validation rules)                           │
│                                                                 │
│  Instance (Running Workflow)                                    │
│  ├── BlueprintId (Reference)                                    │
│  ├── State (Active/Complete/Rejected)                           │
│  ├── CurrentActionId                                            │
│  └── Transactions[] (via Register - not stored locally)         │
│                                                                 │
│  AccumulatedState (Runtime - not persisted)                     │
│  ├── ActionData (Decrypted payloads by action)                  │
│  ├── PreviousTransactionId                                      │
│  └── ActionCount                                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Core Entities

### Blueprint

The workflow definition published to a Register.

```csharp
public class Blueprint
{
    /// <summary>Unique identifier (UUID)</summary>
    [Required]
    public string Id { get; set; }

    /// <summary>Human-readable title</summary>
    [Required, MaxLength(200)]
    public string Title { get; set; }

    /// <summary>Detailed description</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Version number (incremented on updates)</summary>
    public int Version { get; set; } = 1;

    /// <summary>Participants who can execute actions</summary>
    [Required, MinLength(2)]
    public List<Participant> Participants { get; set; } = [];

    /// <summary>Actions (steps) in the workflow</summary>
    [Required, MinLength(1)]
    public List<Action> Actions { get; set; } = [];

    /// <summary>Shared schemas for data validation</summary>
    public List<Schema> Schemas { get; set; } = [];

    /// <summary>Register ID where blueprint is published</summary>
    public string? RegisterId { get; set; }

    /// <summary>Tenant isolation</summary>
    [Required]
    public string TenantId { get; set; }

    /// <summary>JSON-LD context for semantic interoperability</summary>
    public string? Context { get; set; }

    /// <summary>Created timestamp</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modified timestamp</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Validation Rules**:
- `Id`: Valid UUID format
- `Participants`: At least 2 participants
- `Actions`: At least 1 action
- Each action's `Sender` must reference a valid participant

**State Transitions**:
- Draft → Published (when assigned to RegisterId)
- Published → Updated (Version incremented)

---

### Participant

A party who can execute actions in the workflow.

```csharp
public class Participant
{
    /// <summary>Unique identifier within blueprint</summary>
    [Required, MaxLength(100)]
    public string Id { get; set; }

    /// <summary>Display name</summary>
    [Required, MaxLength(200)]
    public string Name { get; set; }

    /// <summary>Organization name</summary>
    [MaxLength(200)]
    public string? Organization { get; set; }

    /// <summary>DID (Decentralized Identifier) - optional</summary>
    public string? Did { get; set; }

    /// <summary>Wallet address (runtime binding)</summary>
    /// <remarks>Set when participant joins workflow instance</remarks>
    public string? WalletAddress { get; set; }
}
```

**Identity Model**:
- `Did`: External identity (from STS)
- `WalletAddress`: Cryptographic identity (from Wallet Service)
- Binding between these is **local node only** (privacy)

---

### Action

A step in the workflow that a participant executes.

```csharp
public class Action
{
    /// <summary>Unique identifier within blueprint</summary>
    [Required]
    public int Id { get; set; }

    /// <summary>Display title</summary>
    [Required, MaxLength(200)]
    public string Title { get; set; }

    /// <summary>Participant who executes this action</summary>
    [Required]
    public string Sender { get; set; }

    /// <summary>Is this a starting action (can initiate workflow)?</summary>
    public bool IsStartingAction { get; set; }

    /// <summary>JSON Schema for data validation</summary>
    public JsonElement? DataSchema { get; set; }

    /// <summary>Schema reference (alternative to inline)</summary>
    public string? DataSchemaRef { get; set; }

    /// <summary>Disclosure rules per participant</summary>
    public List<Disclosure> Disclosures { get; set; } = [];

    /// <summary>Routing conditions to next action(s)</summary>
    public List<Route> Routes { get; set; } = [];

    /// <summary>Calculated fields (JSON Logic)</summary>
    public List<Calculation> Calculations { get; set; } = [];

    /// <summary>Form definition for UI rendering</summary>
    public Form? Form { get; set; }

    /// <summary>Rejection routing configuration</summary>
    public RejectionConfig? RejectionConfig { get; set; }

    /// <summary>Required data from prior actions (for state reconstruction)</summary>
    public List<string>? RequiredPriorActions { get; set; }
}
```

**Validation Rules**:
- `Sender` must reference valid participant ID
- At least one action must have `IsStartingAction = true`
- `RequiredPriorActions` IDs must exist in blueprint

---

### Disclosure

Controls what data each participant can see.

```csharp
public class Disclosure
{
    /// <summary>Participant who receives this disclosure</summary>
    [Required]
    public string ParticipantId { get; set; }

    /// <summary>JSON Pointer paths to include</summary>
    /// <remarks>
    /// Examples: "/name", "/*", "/address/city"
    /// "/*" = all fields, "/" = root object
    /// </remarks>
    [Required]
    public List<string> Paths { get; set; } = [];
}
```

**JSON Pointer Examples**:
| Path | Meaning |
|------|---------|
| `/*` | All top-level fields |
| `/name` | Only the name field |
| `/address/city` | Nested city field |
| `/items/*` | All items in array |

---

### Route

Conditional routing to next action(s).

```csharp
public class Route
{
    /// <summary>Route identifier (e.g., "approved", "rejected")</summary>
    [Required]
    public string Id { get; set; }

    /// <summary>Next action ID(s) - supports parallel branches</summary>
    [Required]
    public List<int> NextActionIds { get; set; } = [];

    /// <summary>JSON Logic condition (evaluated against accumulated state)</summary>
    public JsonElement? Condition { get; set; }

    /// <summary>Is this the default route (when no conditions match)?</summary>
    public bool IsDefault { get; set; }
}
```

**Parallel Branches**:
- If `NextActionIds` contains multiple IDs, parallel branches are created
- Each branch gets its own transaction

---

### RejectionConfig

Configures where rejections route.

```csharp
public class RejectionConfig
{
    /// <summary>Target action for rejection</summary>
    [Required]
    public int TargetActionId { get; set; }

    /// <summary>Target participant (override action's sender)</summary>
    public string? TargetParticipantId { get; set; }

    /// <summary>Require rejection reason</summary>
    public bool RequireReason { get; set; } = true;

    /// <summary>Optional JSON Schema for rejection data</summary>
    public JsonElement? RejectionSchema { get; set; }
}
```

---

### Calculation

Computed field using JSON Logic.

```csharp
public class Calculation
{
    /// <summary>Output field name</summary>
    [Required]
    public string OutputField { get; set; }

    /// <summary>JSON Logic expression</summary>
    [Required]
    public JsonElement Expression { get; set; }

    /// <summary>Description for documentation</summary>
    public string? Description { get; set; }
}
```

**Example**:
```json
{
  "outputField": "total",
  "expression": { "*": [{ "var": "quantity" }, { "var": "price" }] }
}
```

---

### Instance

A running workflow execution.

```csharp
public class Instance
{
    /// <summary>Unique identifier (UUID)</summary>
    [Required]
    public string Id { get; set; }

    /// <summary>Blueprint being executed</summary>
    [Required]
    public string BlueprintId { get; set; }

    /// <summary>Blueprint version at creation</summary>
    public int BlueprintVersion { get; set; }

    /// <summary>Register where transactions are stored</summary>
    [Required]
    public string RegisterId { get; set; }

    /// <summary>Current workflow state</summary>
    public InstanceState State { get; set; } = InstanceState.Active;

    /// <summary>Current action ID(s) - multiple for parallel branches</summary>
    public List<int> CurrentActionIds { get; set; } = [];

    /// <summary>Participant wallet bindings</summary>
    public Dictionary<string, string> ParticipantWallets { get; set; } = new();

    /// <summary>Active parallel branches</summary>
    public List<Branch> ActiveBranches { get; set; } = [];

    /// <summary>Created timestamp</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Completed timestamp</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum InstanceState
{
    Active,      // Workflow in progress
    Completed,   // All actions completed successfully
    Rejected,    // Workflow rejected (terminal rejection)
    TimedOut,    // Workflow timed out (parallel branch deadline)
    Cancelled    // Manually cancelled
}
```

---

### Branch

Tracks parallel execution branches.

```csharp
public class Branch
{
    /// <summary>Branch identifier</summary>
    [Required]
    public string Id { get; set; }

    /// <summary>Current action in this branch</summary>
    public int CurrentActionId { get; set; }

    /// <summary>Branch state</summary>
    public BranchState State { get; set; } = BranchState.Active;

    /// <summary>Last transaction ID in this branch</summary>
    public string? LastTransactionId { get; set; }

    /// <summary>Deadline for branch completion (optional)</summary>
    public DateTimeOffset? Deadline { get; set; }
}

public enum BranchState
{
    Active,
    Completed,
    TimedOut
}
```

---

### AccumulatedState

Runtime object for state reconstruction (not persisted).

```csharp
public class AccumulatedState
{
    /// <summary>Decrypted data from each action, keyed by action ID</summary>
    public Dictionary<string, JsonElement> ActionData { get; init; } = new();

    /// <summary>Previous transaction ID for chaining</summary>
    public string? PreviousTransactionId { get; init; }

    /// <summary>Number of actions completed</summary>
    public int ActionCount { get; init; }

    /// <summary>Active branch states (for parallel workflows)</summary>
    public Dictionary<string, BranchState> BranchStates { get; init; } = new();
}
```

---

## Request/Response Models

### ActionSubmissionRequest

```csharp
public class ActionSubmissionRequest
{
    /// <summary>Instance ID</summary>
    [Required]
    public string InstanceId { get; set; }

    /// <summary>Action ID being executed</summary>
    [Required]
    public int ActionId { get; set; }

    /// <summary>Submitted data</summary>
    [Required]
    public JsonElement Data { get; set; }

    /// <summary>Branch ID (for parallel workflows)</summary>
    public string? BranchId { get; set; }

    /// <summary>File attachments (if any)</summary>
    public List<FileAttachment>? Attachments { get; set; }
}
```

### ActionSubmissionResponse

```csharp
public class ActionSubmissionResponse
{
    /// <summary>Created transaction ID</summary>
    public string TransactionId { get; set; }

    /// <summary>Next action(s) in workflow</summary>
    public List<NextAction> NextActions { get; set; } = [];

    /// <summary>Calculated values</summary>
    public Dictionary<string, object>? Calculations { get; set; }

    /// <summary>Workflow complete?</summary>
    public bool IsComplete { get; set; }

    /// <summary>Validation warnings (non-blocking)</summary>
    public List<string>? Warnings { get; set; }
}

public class NextAction
{
    public int ActionId { get; set; }
    public string ActionTitle { get; set; }
    public string ParticipantId { get; set; }
    public string? BranchId { get; set; }
}
```

### ActionRejectionRequest

```csharp
public class ActionRejectionRequest
{
    /// <summary>Instance ID</summary>
    [Required]
    public string InstanceId { get; set; }

    /// <summary>Action ID being rejected</summary>
    [Required]
    public int ActionId { get; set; }

    /// <summary>Rejection reason</summary>
    [Required, MaxLength(1000)]
    public string Reason { get; set; }

    /// <summary>Specific field errors</summary>
    public Dictionary<string, string>? FieldErrors { get; set; }

    /// <summary>Branch ID (for parallel workflows)</summary>
    public string? BranchId { get; set; }
}
```

---

## Relationships

```
Blueprint 1:N Participant
Blueprint 1:N Action
Blueprint 1:N Schema
Action 1:N Disclosure
Action 1:N Route
Action 1:N Calculation
Action 1:1 Form (optional)
Action 1:1 RejectionConfig (optional)
Instance N:1 Blueprint
Instance 1:N Branch (for parallel workflows)
```

## Indexes

### Blueprint Store

| Index | Fields | Purpose |
|-------|--------|---------|
| PK | Id | Primary lookup |
| UK | TenantId, Title, Version | Uniqueness per tenant |
| IX | RegisterId | Find blueprints by register |
| IX | TenantId, CreatedAt | Tenant listing with ordering |

### Instance Store

| Index | Fields | Purpose |
|-------|--------|---------|
| PK | Id | Primary lookup |
| IX | BlueprintId, State | Find active instances |
| IX | RegisterId | Find instances by register |
| IX | ParticipantWallets | Find instances by participant |

## Data Volume Estimates

| Entity | Expected Volume | Storage Notes |
|--------|-----------------|---------------|
| Blueprint | 100-1,000 per tenant | ~50KB avg (includes schemas) |
| Instance | 10,000+ per register | ~5KB (state tracking only) |
| Transaction | 100,000+ per register | Stored in Register Service |
| AccumulatedState | Transient | In-memory only, ~10KB per reconstruction |
