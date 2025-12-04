# Implementation Plan: Blueprint Service (Workflow Orchestration)

**Feature Branch**: `blueprint-service`
**Created**: 2025-12-03
**Updated**: 2025-12-04 (Workflow orchestration model clarified)
**Status**: 95% Complete - Orchestration enhancements needed
**Spec**: [spec.md](./spec.md)

## Summary

The Blueprint Service orchestrates multi-party workflow execution on the Sorcha platform. It handles:

1. **Administration**: Blueprint CRUD, versioning, publishing to registers
2. **Flow Execution**: State reconstruction, routing evaluation, transaction building
3. **Rejection Handling**: Configurable routing per action
4. **Notifications**: Real-time participant alerts via SignalR

**Key Architectural Insight**: The service acts as an orchestrator:
- Delegates pure logic to **Blueprint Engine** library
- Coordinates **Register Service** for transaction storage
- Coordinates **Wallet Service** for crypto operations (delegated access)
- Coordinates **Tenant Service** for authentication tokens

## Technical Context

**Language/Version**: C# 13, .NET 10.0
**Primary Dependencies**:
- Sorcha.Blueprint.Engine (routing, validation, disclosures)
- Sorcha.Blueprint.Models (domain models)
- Sorcha.TransactionHandler (transaction building with PQC)
- ASP.NET Core Minimal APIs
- SignalR with Redis backplane
- FluentValidation 11.10.0
- JsonSchema.Net 7.4.0

**Storage**:
- Redis (blueprint cache, SignalR backplane)
- Register Service (transaction persistence)
- In-memory for blueprint store (configurable to PostgreSQL)

**Testing**: xUnit, FluentAssertions, Moq, Testcontainers

**Target Platform**: Linux containers (Docker), .NET Aspire orchestration

**Performance Goals**:
| Operation | Target |
|-----------|--------|
| Blueprint CRUD | <200ms P95 |
| Action submission (full flow) | <500ms P95 |
| State reconstruction (10 actions) | <200ms |
| SignalR notification | <100ms |

**Scale/Scope**:
- 1,000+ concurrent workflow instances per register
- 100+ blueprints per tenant
- Up to 50 actions per blueprint
- Up to 20 participants per blueprint
- Up to 5 parallel branches

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ PASS | Independently deployable, downward dependencies |
| II. Security First | ✅ PASS | Delegated access tokens, ML-KEM encryption |
| III. API Documentation | ✅ PASS | .NET 10 OpenAPI, Scalar UI |
| IV. Testing Requirements | ✅ PASS | >85% coverage target, xUnit |
| V. Code Quality | ✅ PASS | .NET 10, C# 13, async/await, DI |
| VI. Blueprint Creation | ✅ PASS | JSON/YAML primary format |
| VII. Domain-Driven Design | ✅ PASS | Blueprint, Action, Participant, Disclosure |
| VIII. Observability | ✅ PASS | OpenTelemetry, structured logging |

**All gates passed.**

## Design Decisions

### Decision 1: Orchestration Pattern

**Approach**: Blueprint Service orchestrates workflow execution, delegating to specialized components.

**Rationale**: Separation of concerns - Engine handles pure logic, Service handles I/O coordination.

```
Blueprint Service (Orchestrator)
├── Register Service → Fetch/submit transactions
├── Wallet Service → Decrypt/encrypt/sign (delegated access)
├── Blueprint Engine → Validate/route/calculate/disclose
└── SignalR → Notify participants
```

### Decision 2: State Reconstruction (Blueprint-Defined Scope)

**Approach**: Only fetch transactions needed for current action, determined by blueprint definition.

**Rationale**: Optimizes performance - avoid fetching/decrypting all prior transactions.

**Implementation**:
```csharp
public interface IStateReconstructionService
{
    Task<AccumulatedState> ReconstructAsync(
        Blueprint blueprint,
        string instanceId,
        int currentActionId,
        string delegationToken,
        CancellationToken ct);
}
```

### Decision 3: Delegated Decrypt Authorization

**Approach**: Participant's credential token (from STS) grants Blueprint Service permission to decrypt on their behalf.

**Rationale**: Maintains security - service never has permanent key access, only per-request delegation.

**Flow**:
1. Participant authenticates → receives credential token
2. Participant submits action with token
3. Blueprint Service calls Wallet Service: `Decrypt(token, payload)`
4. Wallet Service validates token, decrypts, returns data

### Decision 4: Parallel Branch Support

**Approach**: Routing can return multiple next actions; service creates transactions for each branch.

**Rationale**: Real workflows have parallel paths (e.g., multi-party approvals).

**Deadlock Prevention**: Blueprint must define:
- Timeout actions for incomplete branches
- Reduction actions that handle partial completion

### Decision 5: Rejection Routing

**Approach**: Each action defines its rejection target (not always immediate sender).

**Rationale**: Rejections may need to route to supervisor, original submitter, or correction queue.

```yaml
actions:
  review_application:
    routes:
      approved: process_application
      rejected: correct_application  # Back to applicant
      escalate: senior_review        # To supervisor
```

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   BLUEPRINT SERVICE                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  API Layer (Minimal APIs)                                       │
│  ├── BlueprintEndpoints.cs      (CRUD)                         │
│  ├── InstanceEndpoints.cs       (Workflow instances)           │
│  ├── ActionEndpoints.cs         (Execute/reject actions)       │
│  └── SchemaEndpoints.cs         (Validation)                   │
│                                                                 │
│  Orchestration Services                                         │
│  ├── IActionExecutionService    (Main orchestrator)            │
│  │   └── ExecuteActionAsync(submission, token)                 │
│  │       1. Fetch prior transactions (Register)                │
│  │       2. Decrypt payloads (Wallet - delegated)              │
│  │       3. Reconstruct state                                  │
│  │       4. Call Engine (validate, route, calculate, disclose) │
│  │       5. Build transaction (encrypt per recipient)          │
│  │       6. Sign (Wallet)                                      │
│  │       7. Submit (Register)                                  │
│  │       8. Notify (SignalR)                                   │
│  │                                                             │
│  ├── IStateReconstructionService (NEW)                         │
│  │   └── ReconstructAsync(blueprint, instanceId, actionId)     │
│  │                                                             │
│  ├── IPayloadResolverService    (Encrypt/decrypt delegation)   │
│  └── ITransactionBuilderService (Build transactions)           │
│                                                                 │
│  Clients (Service Communication)                                │
│  ├── IRegisterServiceClient     (Fetch/submit transactions)    │
│  └── IWalletServiceClient       (Decrypt/encrypt/sign)         │
│                                                                 │
│  SignalR Hub                                                    │
│  └── ActionsHub.cs              (Real-time notifications)      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Register   │     │   Wallet     │     │   Blueprint  │
│   Service    │     │   Service    │     │   Engine     │
│              │     │              │     │  (Library)   │
│ • Fetch Tx   │     │ • Decrypt    │     │ • Validate   │
│ • Submit Tx  │     │ • Encrypt    │     │ • Route      │
│ • Query      │     │ • Sign       │     │ • Calculate  │
└──────────────┘     └──────────────┘     │ • Disclose   │
                                          └──────────────┘
```

### Action Execution Flow

```
Participant                    Blueprint Service              External Services
     │                               │                              │
     │  POST /actions/{id}           │                              │
     │  + delegationToken            │                              │
     │  + actionData                 │                              │
     │  ───────────────────────►     │                              │
     │                               │                              │
     │                               │  1. Fetch prior Tx           │
     │                               │  ───────────────────────►    │ Register
     │                               │  ◄───────────────────────    │
     │                               │                              │
     │                               │  2. Decrypt payloads         │
     │                               │  ───────────────────────►    │ Wallet
     │                               │  ◄───────────────────────    │ (delegated)
     │                               │                              │
     │                               │  3. Reconstruct state        │
     │                               │  (in-memory merge)           │
     │                               │                              │
     │                               │  4. Engine.Execute()         │
     │                               │  → validate                  │
     │                               │  → route (JSON Logic)        │
     │                               │  → calculate                 │
     │                               │  → disclose                  │
     │                               │                              │
     │                               │  5. Build transaction        │
     │                               │  ───────────────────────►    │ Wallet
     │                               │  (encrypt per recipient)     │ (encrypt)
     │                               │  ◄───────────────────────    │
     │                               │                              │
     │                               │  6. Sign transaction         │
     │                               │  ───────────────────────►    │ Wallet
     │                               │  ◄───────────────────────    │ (sign)
     │                               │                              │
     │                               │  7. Submit to Register       │
     │                               │  ───────────────────────►    │ Register
     │                               │  ◄───────────────────────    │
     │                               │                              │
     │                               │  8. Notify via SignalR       │
     │                               │  ───────────────────────►    │ SignalR
     │                               │                              │
     │  200 OK + result              │                              │
     │  ◄───────────────────────     │                              │
     │                               │                              │
```

## Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Blueprint.Models | 100% | Core domain models with JSON-LD |
| Blueprint.Fluent | 100% | Full fluent builder API |
| Blueprint.Engine | 100% | Portable execution engine |
| Blueprint.Service API | 95% | CRUD, instances, actions |
| StateReconstructionService | NEW | Blueprint-defined scope reconstruction |
| Delegated Decrypt | NEW | Token-based decrypt authorization |
| Parallel Branch Support | NEW | Multiple next actions from routing |
| Rejection Routing | NEW | Configurable per action |
| SignalR ActionsHub | 100% | Real-time notifications |

## New Components Required

### 1. IStateReconstructionService

```csharp
public interface IStateReconstructionService
{
    /// <summary>
    /// Reconstruct accumulated state from prior transactions.
    /// Only fetches transactions needed for current action (blueprint-defined scope).
    /// </summary>
    Task<AccumulatedState> ReconstructAsync(
        Blueprint blueprint,
        string instanceId,
        int currentActionId,
        string delegationToken,
        CancellationToken ct = default);
}

public class AccumulatedState
{
    public Dictionary<string, JsonElement> ActionData { get; init; }
    public string? PreviousTransactionId { get; init; }
    public int ActionCount { get; init; }
}
```

### 2. Enhanced IActionExecutionService

```csharp
public interface IActionExecutionService
{
    Task<ActionExecutionResponse> ExecuteAsync(
        ActionSubmissionRequest request,
        string delegationToken,
        CancellationToken ct = default);

    Task<ActionRejectionResponse> RejectAsync(
        ActionRejectionRequest request,
        string delegationToken,
        CancellationToken ct = default);
}
```

### 3. Parallel Branch Handler

```csharp
public class RoutingResult
{
    public bool IsParallel { get; init; }
    public List<NextAction> NextActions { get; init; } = [];
}

public class NextAction
{
    public string ActionId { get; init; }
    public string ParticipantId { get; init; }
    public string? BranchId { get; init; }
}
```

## API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/blueprints` | Create blueprint | Done |
| GET | `/api/blueprints` | List blueprints | Done |
| GET | `/api/blueprints/{id}` | Get blueprint | Done |
| PUT | `/api/blueprints/{id}` | Update blueprint | Done |
| DELETE | `/api/blueprints/{id}` | Delete blueprint | Done |
| POST | `/api/blueprints/{id}/instances` | Create instance | Done |
| GET | `/api/instances/{id}` | Get instance state | Done |
| POST | `/api/instances/{id}/actions/{actionId}` | Execute action | Enhance |
| POST | `/api/instances/{id}/actions/{actionId}/reject` | Reject action | NEW |
| GET | `/api/instances/{id}/state` | Get accumulated state | NEW |

## Dependencies

### Internal Libraries

| Library | Purpose |
|---------|---------|
| Sorcha.Blueprint.Engine | Pure logic: validate, route, calculate, disclose |
| Sorcha.Blueprint.Models | Domain models |
| Sorcha.TransactionHandler | Transaction building (PQC) |
| Sorcha.ServiceDefaults | .NET Aspire configuration |

### External Services

| Service | Purpose | Criticality |
|---------|---------|-------------|
| Register Service | Transaction storage/retrieval | Required |
| Wallet Service | Decrypt/encrypt/sign (delegated) | Required |
| Tenant Service | Authentication, delegation tokens | Required |
| Redis | Cache, SignalR backplane | Required |

## Open Questions (Resolved)

| Question | Resolution |
|----------|------------|
| Engine vs Service responsibility? | Engine = pure logic, Service = orchestration |
| State reconstruction scope? | Blueprint-defined (optimize fetches) |
| Decrypt authorization? | Delegated access via credential token |
| Rejection routing? | Configurable per action |
| Parallel branches? | Supported with deadlock management |

## Migration Notes

### From Current Implementation

1. Add `IStateReconstructionService` for accumulated state
2. Enhance action execution to use delegation tokens
3. Add rejection endpoint with configurable routing
4. Support parallel branch routing results
5. Add instance state query endpoint

### Breaking Changes

- Action execution now requires `delegationToken` header
- Routing results may return multiple next actions
