# Sorcha Blueprint Service - Unified Design Document

**Version:** 2.0
**Date:** 2025-11-15
**Status:** Approved for Implementation
**Supersedes:** ACTION-SERVICE-DESIGN.md (merged into this design)

## Executive Summary

The Sorcha Blueprint Service is a unified microservice that combines blueprint management with action execution capabilities. It serves as the complete solution for defining, managing, validating, and executing multi-participant workflow blueprints with selective data disclosure, conditional routing, and blockchain integration.

**Key Capabilities:**
- Blueprint CRUD operations and versioning
- **Portable Blueprint Execution Engine** (client + server side)
- Action retrieval, submission, and validation
- Schema validation with JSON Schema Draft 2020-12
- Privacy-preserving selective data disclosure
- JSON Logic evaluation for calculations and conditional routing
- Transaction construction and coordination with Register Service
- Real-time notifications via SignalR
- File attachment support
- DID-based participant linking

**Technology:** .NET 10, ASP.NET Core Minimal APIs, SignalR, Redis, OpenAPI/Scalar

**Timeline:** 16 weeks (138 tasks across 12 phases)

**Team:** 2 backend developers, 1 QA engineer, 1 DevOps engineer

---

## 1. Service Overview

### 1.1 Purpose

The unified Blueprint Service manages the complete lifecycle of blueprint-controlled workflows:

1. **Blueprint Management** - CRUD operations, validation, publishing, versioning
2. **Execution Engine** - Portable stateless engine for blueprint flow processing
3. **Action Discovery** - Help participants find actions they can perform
4. **Action Retrieval** - Provide action details with historical context
5. **Action Submission** - Accept, validate, and process action responses
6. **Transaction Coordination** - Build and submit transactions to the register
7. **Real-Time Notification** - Inform participants of action state changes

### 1.2 Position in Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                         Sorcha Platform                           │
├──────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ┌───────────────────┐          ┌──────────────────────────┐    │
│  │  Blueprint        │          │    Register              │    │
│  │  Designer         │──────────│    Service               │    │
│  │  (Blazor WASM)    │   HTTP   │                          │    │
│  │                   │          │  • Storage               │    │
│  │  Uses Execution   │          │  • History               │    │
│  │  Engine client    │          │  • Events                │    │
│  │  -side for        │          └──────────────────────────┘    │
│  │  validation       │                      ▲                    │
│  └───────────────────┘                      │                    │
│          │                                  │                    │
│          │                                  │                    │
│          ▼                                  │                    │
│  ┌──────────────────────────────────────────┼────────────────┐  │
│  │          Sorcha.Blueprint.Service        │                │  │
│  ├──────────────────────────────────────────┴────────────────┤  │
│  │                                                            │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │          Blueprint Management Layer                  │ │  │
│  │  │  • CRUD Operations                                   │ │  │
│  │  │  • Publishing & Versioning                           │ │  │
│  │  │  • Validation                                        │ │  │
│  │  └─────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │      Blueprint Execution Engine (Portable Library)   │ │  │
│  │  │  • Schema Validation                                 │ │  │
│  │  │  • JSON Logic Evaluation                             │ │  │
│  │  │  • Selective Disclosure Processing                   │ │  │
│  │  │  • Routing Determination                             │ │  │
│  │  │  • Calculation Execution                             │ │  │
│  │  │  • ⭐ Runs Client-Side & Server-Side ⭐             │ │  │
│  │  └─────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │           Action Management Layer                    │ │  │
│  │  │  • Action Retrieval (starting, pending, by ID)       │ │  │
│  │  │  • Action Submission                                 │ │  │
│  │  │  • Action Rejection                                  │ │  │
│  │  │  • File Handling                                     │ │  │
│  │  │  • Payload Encryption/Decryption                     │ │  │
│  │  │  • Transaction Building                              │ │  │
│  │  └─────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  │  ┌─────────────────────────────────────────────────────┐ │  │
│  │  │              SignalR Hub                             │ │  │
│  │  │  • Real-time notifications                           │ │  │
│  │  │  • Action availability alerts                        │ │  │
│  │  │  • Transaction confirmations                         │ │  │
│  │  └─────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  └────────────────────────┬───────────────────────────────────┘  │
│                           │                                      │
│              ┌────────────┼────────────┐                        │
│              │            │            │                        │
│              ▼            ▼            ▼                        │
│  ┌──────────────┐  ┌──────────┐  ┌─────────────┐              │
│  │   Wallet     │  │  Redis   │  │  Register   │              │
│  │   Service    │  │  Cache   │  │  Service    │              │
│  │              │  │          │  │             │              │
│  │ • Keys       │  │ • Schema │  │ • Txns      │              │
│  │ • Encryption │  │ • Actions│  │ • History   │              │
│  │ • Signing    │  └──────────┘  └─────────────┘              │
│  └──────────────┘                                               │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 1.3 Core Design Principles

#### 1.3.1 Portable Execution Engine

The **Blueprint Execution Engine** is designed as a **standalone, portable library** (`Sorcha.Blueprint.Engine`) that can execute in multiple contexts:

**Server-Side Execution:**
- In `Sorcha.Blueprint.Service` for processing action submissions
- Validates incoming data, applies calculations, determines routing
- Full transaction coordination

**Client-Side Execution:**
- In Blazor WASM Designer for pre-submission validation
- Allows users to test blueprints before publishing
- Validates data entry in real-time
- Provides instant feedback without server round-trip

**Key Characteristics:**
- **Stateless** - No internal state, all context passed as parameters
- **Pure Functions** - Deterministic results for same inputs
- **Async Throughout** - Non-blocking operations
- **Zero External Dependencies** - Can run in sandbox environments
- **Highly Testable** - Easy to unit test in isolation

#### 1.3.2 Separation of Concerns

```
┌─────────────────────────────────────────────────────────────┐
│  Sorcha.Blueprint.Service (HTTP/API Layer)                  │
│  • Endpoints (REST API)                                     │
│  • SignalR Hubs                                             │
│  • External Service Integration (Wallet, Register)          │
│  • Caching (Redis)                                          │
│  • Authentication & Authorization                           │
└────────────────────────┬────────────────────────────────────┘
                         │ Uses
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Sorcha.Blueprint.Engine (Execution Logic Library)          │
│  • IExecutionEngine - Core stateless engine                 │
│  • IActionProcessor - Action validation & processing        │
│  • ISchemaValidator - JSON Schema validation                │
│  • IJsonLogicEvaluator - Calculations & conditions          │
│  • IDisclosureProcessor - Selective disclosure rules        │
│  • IRoutingEngine - Next participant determination          │
│  • ⭐ Can run client-side (WASM) or server-side ⭐         │
└────────────────────────┬────────────────────────────────────┘
                         │ Uses
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Sorcha.Blueprint.Models (Domain Models)                    │
│  • Blueprint, Action, Participant                           │
│  • Disclosure, Condition, Calculation                       │
│  • ExecutionContext, ActionResult                           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Component Architecture

### 2.1 Solution Structure

```
src/
├── Core/
│   └── Sorcha.Blueprint.Engine/             # ⭐ NEW: Portable execution engine
│       ├── Interfaces/
│       │   ├── IExecutionEngine.cs          # Main engine interface
│       │   ├── IActionProcessor.cs          # Action processing
│       │   ├── ISchemaValidator.cs          # JSON Schema validation
│       │   ├── IJsonLogicEvaluator.cs       # JSON Logic evaluation
│       │   ├── IDisclosureProcessor.cs      # Selective disclosure
│       │   └── IRoutingEngine.cs            # Routing logic
│       │
│       ├── Implementation/
│       │   ├── ExecutionEngine.cs           # Main stateless engine
│       │   ├── ActionProcessor.cs           # Action validation & processing
│       │   ├── SchemaValidator.cs           # JSON Schema validator
│       │   ├── JsonLogicEvaluator.cs        # JSON Logic evaluator
│       │   ├── DisclosureProcessor.cs       # Disclosure rule processor
│       │   └── RoutingEngine.cs             # Routing determination
│       │
│       ├── Models/
│       │   ├── ExecutionContext.cs          # Execution context
│       │   ├── ActionExecutionResult.cs     # Execution result
│       │   ├── ValidationResult.cs          # Validation results
│       │   └── RoutingResult.cs             # Routing results
│       │
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
│
├── Services/
│   └── Sorcha.Blueprint.Service/            # UPDATED: Unified service
│       ├── Program.cs                       # Entry point
│       │
│       ├── Endpoints/                       # Minimal API endpoints
│       │   ├── BlueprintEndpoints.cs        # Blueprint CRUD
│       │   ├── ActionEndpoints.cs           # NEW: Action operations
│       │   ├── FileEndpoints.cs             # NEW: File handling
│       │   └── NotificationEndpoints.cs     # NEW: Internal notifications
│       │
│       ├── Services/                        # Business logic
│       │   ├── Interfaces/
│       │   │   ├── IBlueprintService.cs     # Blueprint management
│       │   │   ├── IActionService.cs        # NEW: Action operations
│       │   │   ├── IActionResolver.cs       # NEW: Action resolution
│       │   │   ├── IPayloadResolver.cs      # NEW: Payload management
│       │   │   └── ITransactionBuilder.cs   # NEW: Transaction building
│       │   │
│       │   └── Implementation/
│       │       ├── BlueprintService.cs      # Blueprint CRUD impl
│       │       ├── ActionService.cs         # NEW: Action service
│       │       ├── ActionResolver.cs        # NEW: Action resolver
│       │       ├── PayloadResolver.cs       # NEW: Payload resolver
│       │       └── TransactionBuilder.cs    # NEW: Transaction builder
│       │
│       ├── Hubs/                            # NEW: SignalR hubs
│       │   └── ActionsHub.cs                # Real-time notifications
│       │
│       ├── Models/                          # DTOs
│       │   ├── Requests/
│       │   │   ├── ActionSubmission.cs      # NEW
│       │   │   ├── ActionRejection.cs       # NEW
│       │   │   └── ActionFilter.cs          # NEW
│       │   │
│       │   ├── Responses/
│       │   │   ├── ActionResponse.cs        # NEW
│       │   │   ├── ActionSummary.cs         # NEW
│       │   │   └── TransactionResult.cs     # NEW
│       │   │
│       │   └── Internal/
│       │       ├── FileAttachment.cs        # NEW
│       │       └── ExecutionContextDto.cs   # NEW
│       │
│       ├── Validators/                      # FluentValidation
│       │   ├── BlueprintValidator.cs        # Existing
│       │   ├── ActionSubmissionValidator.cs # NEW
│       │   └── ActionRejectionValidator.cs  # NEW
│       │
│       └── Configuration/
│           ├── BlueprintServiceOptions.cs   # Existing
│           └── ActionServiceOptions.cs      # NEW
│
└── Common/
    └── Sorcha.Blueprint.Models/             # UPDATED: Add execution models
        ├── Blueprint.cs                     # Existing
        ├── Action.cs                        # Existing
        ├── Participant.cs                   # Existing
        │
        └── Execution/                       # NEW: Execution models
            ├── ExecutionContext.cs          # Context for execution
            ├── ActionExecutionResult.cs     # Action execution result
            ├── ValidationError.cs           # Validation error
            └── RoutingDecision.cs           # Routing decision

tests/
├── Sorcha.Blueprint.Engine.Tests/           # NEW: Engine tests
│   ├── ExecutionEngineTests.cs
│   ├── ActionProcessorTests.cs
│   ├── SchemaValidatorTests.cs
│   ├── JsonLogicEvaluatorTests.cs
│   ├── DisclosureProcessorTests.cs
│   └── RoutingEngineTests.cs
│
└── Sorcha.Blueprint.Service.Tests/          # UPDATED
    ├── Endpoints/
    │   ├── BlueprintEndpointsTests.cs       # Existing
    │   ├── ActionEndpointsTests.cs          # NEW
    │   └── FileEndpointsTests.cs            # NEW
    │
    ├── Services/
    │   ├── BlueprintServiceTests.cs         # Existing
    │   ├── ActionServiceTests.cs            # NEW
    │   ├── ActionResolverTests.cs           # NEW
    │   ├── PayloadResolverTests.cs          # NEW
    │   └── TransactionBuilderTests.cs       # NEW
    │
    └── Integration/
        ├── BlueprintApiTests.cs             # Existing
        ├── ActionApiTests.cs                # NEW
        └── SignalRHubTests.cs               # NEW
```

---

## 3. Blueprint Execution Engine Design

### 3.1 Core Engine Interface

```csharp
namespace Sorcha.Blueprint.Engine;

/// <summary>
/// Stateless blueprint execution engine that can run client-side or server-side.
/// </summary>
public interface IExecutionEngine
{
    /// <summary>
    /// Execute an action within a blueprint workflow.
    /// </summary>
    Task<ActionExecutionResult> ExecuteActionAsync(
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validate action data against schema without executing.
    /// </summary>
    Task<ValidationResult> ValidateActionDataAsync(
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Determine routing for next action based on conditions.
    /// </summary>
    Task<RoutingResult> DetermineRoutingAsync(
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Apply calculations to data.
    /// </summary>
    Task<Dictionary<string, object>> ApplyCalculationsAsync(
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Filter data based on disclosure rules.
    /// </summary>
    Dictionary<string, object> ApplyDisclosure(
        Dictionary<string, object> data,
        Disclosure disclosure);
}
```

### 3.2 Execution Context

```csharp
public class ExecutionContext
{
    // Blueprint definition
    public required Blueprint Blueprint { get; set; }

    // Current action being executed
    public required Models.Action Action { get; set; }

    // Action data submitted by participant
    public required Dictionary<string, object> ActionData { get; set; }

    // Aggregated data from previous actions
    public Dictionary<string, object>? PreviousData { get; set; }

    // Transaction metadata
    public string? PreviousTransactionHash { get; set; }
    public string? InstanceId { get; set; }

    // Requesting participant
    public required string ParticipantId { get; set; }
    public required string WalletAddress { get; set; }

    // Execution mode (validation-only vs full execution)
    public ExecutionMode Mode { get; set; } = ExecutionMode.Full;
}

public enum ExecutionMode
{
    ValidationOnly,  // Client-side validation
    Full            // Server-side full execution
}
```

### 3.3 Execution Result

```csharp
public class ActionExecutionResult
{
    public bool Success { get; set; }
    public ValidationResult Validation { get; set; } = new();
    public Dictionary<string, object> ProcessedData { get; set; } = new();
    public Dictionary<string, object> CalculatedValues { get; set; } = new();
    public RoutingResult Routing { get; set; } = new();
    public List<DisclosureResult> Disclosures { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}

public class RoutingResult
{
    public string? NextActionId { get; set; }
    public string? NextParticipantId { get; set; }
    public bool IsWorkflowComplete { get; set; }
    public string? RejectedToParticipantId { get; set; }
}

public class DisclosureResult
{
    public required string ParticipantId { get; set; }
    public required Dictionary<string, object> DisclosedData { get; set; }
}
```

### 3.4 Component Interfaces

#### IActionProcessor
```csharp
public interface IActionProcessor
{
    /// <summary>
    /// Process an action: validate, calculate, route, disclose.
    /// </summary>
    Task<ActionExecutionResult> ProcessAsync(
        ExecutionContext context,
        CancellationToken ct = default);
}
```

#### ISchemaValidator
```csharp
public interface ISchemaValidator
{
    /// <summary>
    /// Validate data against JSON Schema.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        Dictionary<string, object> data,
        JsonNode schema,
        CancellationToken ct = default);
}
```

#### IJsonLogicEvaluator
```csharp
public interface IJsonLogicEvaluator
{
    /// <summary>
    /// Evaluate JSON Logic expression.
    /// </summary>
    object Evaluate(
        JsonNode expression,
        Dictionary<string, object> data);

    /// <summary>
    /// Apply calculations to data.
    /// </summary>
    Task<Dictionary<string, object>> ApplyCalculationsAsync(
        Dictionary<string, object> data,
        Dictionary<string, JsonNode> calculations,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluate routing conditions.
    /// </summary>
    Task<string?> EvaluateConditionsAsync(
        Dictionary<string, object> data,
        IEnumerable<Condition> conditions,
        CancellationToken ct = default);
}
```

#### IDisclosureProcessor
```csharp
public interface IDisclosureProcessor
{
    /// <summary>
    /// Apply disclosure rules to filter data.
    /// </summary>
    Dictionary<string, object> ApplyDisclosure(
        Dictionary<string, object> data,
        Disclosure disclosure);

    /// <summary>
    /// Create disclosure results for all participants.
    /// </summary>
    List<DisclosureResult> CreateDisclosures(
        Dictionary<string, object> data,
        IEnumerable<Disclosure> disclosures);
}
```

#### IRoutingEngine
```csharp
public interface IRoutingEngine
{
    /// <summary>
    /// Determine next action and participant based on conditions.
    /// </summary>
    Task<RoutingResult> DetermineNextAsync(
        Blueprint blueprint,
        Models.Action currentAction,
        Dictionary<string, object> data,
        CancellationToken ct = default);
}
```

### 3.5 Engine Implementation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  IExecutionEngine.ExecuteActionAsync(context)                   │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 1: Validate Action Data                                   │
│  • ISchemaValidator.ValidateAsync()                             │
│  • Check against action.DataSchemas                             │
│  • Return errors if validation fails                            │
└────────────────────┬────────────────────────────────────────────┘
                     │ Valid ✓
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 2: Apply Calculations                                     │
│  • IJsonLogicEvaluator.ApplyCalculationsAsync()                 │
│  • Execute action.Calculations                                  │
│  • Add calculated fields to data                                │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 3: Determine Routing                                      │
│  • IRoutingEngine.DetermineNextAsync()                          │
│  • Evaluate action.Condition (JSON Logic)                       │
│  • Resolve next participant ID                                  │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 4: Apply Selective Disclosure                             │
│  • IDisclosureProcessor.CreateDisclosures()                     │
│  • For each disclosure rule:                                    │
│  •   - Filter data by JSON Pointers                             │
│  •   - Create participant-specific payloads                     │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 5: Return ActionExecutionResult                           │
│  • Validation result                                            │
│  • Processed data                                               │
│  • Calculated values                                            │
│  • Routing decision                                             │
│  • Disclosure results                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Service Layer Design (Sorcha.Blueprint.Service)

### 4.1 API Endpoints

#### Blueprint Management (Existing - Enhanced)

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| GET | `/api/blueprints` | List all blueprints | JWT |
| GET | `/api/blueprints/{id}` | Get blueprint by ID | JWT |
| POST | `/api/blueprints` | Create blueprint | JWT |
| PUT | `/api/blueprints/{id}` | Update blueprint | JWT |
| DELETE | `/api/blueprints/{id}` | Delete blueprint | JWT |
| POST | `/api/blueprints/{id}/publish` | Publish blueprint | JWT |
| GET | `/api/blueprints/{id}/versions` | Get versions | JWT |
| POST | `/api/blueprints/validate` | Validate blueprint | JWT |

#### Action Operations (NEW)

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| GET | `/api/actions/{wallet}/{register}/blueprints` | Get starting actions | JWT |
| GET | `/api/actions/{wallet}/{register}` | Get pending actions | JWT |
| GET | `/api/actions/{wallet}/{register}/{tx}` | Get action by ID | JWT |
| POST | `/api/actions` | Submit action | JWT |
| POST | `/api/actions/reject` | Reject action | JWT |
| GET | `/api/files/{wallet}/{register}/{tx}/{fileId}` | Download file | JWT |
| POST | `/api/actions/notify` | Internal notification | Internal |

#### Execution & Validation (NEW)

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| POST | `/api/execution/validate` | Validate action data | JWT |
| POST | `/api/execution/calculate` | Apply calculations | JWT |
| POST | `/api/execution/route` | Determine routing | JWT |
| POST | `/api/execution/disclose` | Apply disclosure rules | JWT |

#### SignalR Hub (NEW)

**Endpoint:** `/actionshub`

**Client Methods:**
- `ActionAvailable(notification)` - New action available
- `ActionConfirmed(notification)` - Action confirmed
- `ActionRejected(notification)` - Action rejected

**Server Methods:**
- `SubscribeToWallet(walletAddress)` - Subscribe
- `UnsubscribeFromWallet(walletAddress)` - Unsubscribe

### 4.2 Service Interfaces

#### IActionService
```csharp
public interface IActionService
{
    // Action retrieval
    Task<IEnumerable<ActionResponse>> GetStartingActionsAsync(
        string walletAddress,
        string registerId,
        CancellationToken ct = default);

    Task<PagedResult<ActionSummary>> GetPendingActionsAsync(
        string walletAddress,
        string registerId,
        ActionFilter filter,
        CancellationToken ct = default);

    Task<ActionResponse?> GetActionByIdAsync(
        string walletAddress,
        string registerId,
        string transactionId,
        bool aggregateData = true,
        CancellationToken ct = default);

    // Action submission
    Task<TransactionResult> SubmitActionAsync(
        ActionSubmission submission,
        CancellationToken ct = default);

    Task<TransactionResult> RejectActionAsync(
        ActionRejection rejection,
        CancellationToken ct = default);
}
```

#### IActionResolver
```csharp
public interface IActionResolver
{
    Task<Blueprint> GetBlueprintAsync(
        string blueprintId,
        string registerId,
        CancellationToken ct = default);

    Task<Models.Action> GetActionDefinitionAsync(
        string blueprintId,
        string actionId,
        string registerId,
        CancellationToken ct = default);

    Task<Dictionary<string, string>> ResolveParticipantWalletsAsync(
        Blueprint blueprint,
        string instanceId,
        string registerId,
        CancellationToken ct = default);
}
```

#### IPayloadResolver
```csharp
public interface IPayloadResolver
{
    Task<IEnumerable<Payload>> CreateEncryptedPayloadsAsync(
        ActionSubmission submission,
        List<DisclosureResult> disclosures,
        Dictionary<string, string> participantWallets,
        CancellationToken ct = default);

    Task<Dictionary<string, object>> AggregateHistoricalDataAsync(
        string transactionId,
        string walletAddress,
        Blueprint blueprint,
        Models.Action action,
        string registerId,
        CancellationToken ct = default);
}
```

#### ITransactionBuilder
```csharp
public interface ITransactionBuilder
{
    Task<TransactionRequest> BuildActionTransactionAsync(
        ActionSubmission submission,
        ActionExecutionResult executionResult,
        IEnumerable<Payload> payloads,
        CancellationToken ct = default);

    Task<TransactionRequest> BuildRejectionTransactionAsync(
        ActionRejection rejection,
        string previousTransactionHash,
        Blueprint blueprint,
        CancellationToken ct = default);

    Task<IEnumerable<TransactionRequest>> BuildFileTransactionsAsync(
        IEnumerable<FileAttachment> files,
        string walletAddress,
        string registerId,
        CancellationToken ct = default);
}
```

---

## 5. Execution Flow Examples

### 5.1 Client-Side Validation (Blazor WASM)

```csharp
// In Blazor Designer component
@inject IExecutionEngine ExecutionEngine

private async Task ValidateActionData()
{
    var context = new ExecutionContext
    {
        Blueprint = CurrentBlueprint,
        Action = CurrentAction,
        ActionData = FormData,
        ParticipantId = "buyer",
        WalletAddress = CurrentWallet,
        Mode = ExecutionMode.ValidationOnly  // Client-side only
    };

    var result = await ExecutionEngine.ValidateActionDataAsync(context);

    if (!result.IsValid)
    {
        // Show validation errors to user
        foreach (var error in result.Errors)
        {
            ShowError(error.Message);
        }
    }
    else
    {
        // Enable submit button
        CanSubmit = true;
    }
}
```

### 5.2 Server-Side Full Execution

```csharp
// In ActionService
public async Task<TransactionResult> SubmitActionAsync(
    ActionSubmission submission,
    CancellationToken ct = default)
{
    // 1. Get blueprint
    var blueprint = await _actionResolver.GetBlueprintAsync(
        submission.BlueprintId, submission.RegisterId, ct);

    // 2. Get action definition
    var action = blueprint.Actions.First(a => a.Id == submission.ActionId);

    // 3. Aggregate previous data
    var previousData = submission.TransactionId != null
        ? await _payloadResolver.AggregateHistoricalDataAsync(
            submission.TransactionId, submission.WalletAddress,
            blueprint, action, submission.RegisterId, ct)
        : null;

    // 4. Execute action using engine
    var context = new ExecutionContext
    {
        Blueprint = blueprint,
        Action = action,
        ActionData = submission.Data,
        PreviousData = previousData,
        PreviousTransactionHash = submission.TransactionId,
        ParticipantId = action.Sender,
        WalletAddress = submission.WalletAddress,
        Mode = ExecutionMode.Full  // Full server-side execution
    };

    var executionResult = await _executionEngine.ExecuteActionAsync(context, ct);

    if (!executionResult.Success)
    {
        throw new ValidationException(executionResult.Errors);
    }

    // 5. Resolve participant wallets
    var participantWallets = await _actionResolver.ResolveParticipantWalletsAsync(
        blueprint, submission.InstanceId ?? Guid.NewGuid().ToString(),
        submission.RegisterId, ct);

    // 6. Create encrypted payloads based on disclosure results
    var payloads = await _payloadResolver.CreateEncryptedPayloadsAsync(
        submission, executionResult.Disclosures, participantWallets, ct);

    // 7. Build transaction
    var transaction = await _transactionBuilder.BuildActionTransactionAsync(
        submission, executionResult, payloads, ct);

    // 8. Submit to Register Service
    var result = await _registerServiceClient.SubmitTransactionAsync(
        transaction, ct);

    // 9. Notify participants via SignalR
    await NotifyParticipantsAsync(executionResult.Routing, result.TransactionId, ct);

    return result;
}
```

### 5.3 Data Flow with Selective Disclosure

```
Action Submission:
{
  "blueprintId": "loan-app",
  "actionId": 1,
  "data": {
    "firstName": "John",
    "lastName": "Doe",
    "ssn": "123-45-6789",
    "income": 75000,
    "requestedAmount": 50000
  }
}

Execution Engine Processing:
┌─────────────────────────────────────────────────────────┐
│  Step 1: Validate against schema ✓                      │
│  Step 2: Apply calculations:                            │
│          - loanToIncome = 50000 / 75000 = 0.67         │
│          - requiresManagerApproval = true (> $25k)      │
│  Step 3: Evaluate routing:                              │
│          - Condition: amount > 25000 → "manager"        │
│  Step 4: Apply disclosures:                             │
│          - For "loan-officer": ["/firstName",           │
│            "/lastName", "/requestedAmount"]             │
│          - For "applicant": ["/decision"]               │
└─────────────────────────────────────────────────────────┘

Disclosure Results:
[
  {
    "participantId": "loan-officer",
    "disclosedData": {
      "firstName": "John",
      "lastName": "Doe",
      "requestedAmount": 50000,
      "loanToIncome": 0.67
    }
  },
  {
    "participantId": "applicant",
    "disclosedData": {}  // Will receive decision in next action
  }
]

Encrypted Payloads:
[
  {
    "recipientWallet": "0xLoanOfficerWallet",
    "encryptedData": "base64_encrypted_data_for_officer..."
  }
]

Transaction:
{
  "blueprintId": "loan-app",
  "actionId": 1,
  "instanceId": "loan-inst-001",
  "metadata": {
    "nextParticipant": "manager",
    "nextAction": 2
  },
  "payloads": [...]
}
```

---

## 6. Technology Stack & Dependencies

### 6.1 Sorcha.Blueprint.Engine

**Target Framework:** `net10.0`

**Dependencies:**
```xml
<ItemGroup>
  <!-- JSON Processing -->
  <PackageReference Include="JsonSchema.Net" Version="7.2.4" />
  <PackageReference Include="JsonLogic.Net" Version="2.0.0" />
  <PackageReference Include="JsonPath.Net" Version="1.1.3" />

  <!-- Reference to domain models -->
  <ProjectReference Include="..\..\Common\Sorcha.Blueprint.Models\Sorcha.Blueprint.Models.csproj" />
</ItemGroup>
```

**Key Characteristics:**
- **Minimal dependencies** - Only JSON processing libraries
- **No ASP.NET dependencies** - Can run in Blazor WASM
- **No external service dependencies** - Purely computational
- **Zero I/O** - Stateless, all data passed as parameters

### 6.2 Sorcha.Blueprint.Service

**Dependencies:**
```xml
<ItemGroup>
  <!-- ASP.NET Core -->
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
  <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.0.0" />

  <!-- API Documentation -->
  <PackageReference Include="Scalar.AspNetCore" Version="2.10.0" />

  <!-- Validation -->
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />

  <!-- Caching -->
  <PackageReference Include="Aspire.StackExchange.Redis" Version="13.0.0" />

  <!-- Aspire -->
  <PackageReference Include="Aspire.Hosting.AppHost" Version="13.0.0" />

  <!-- Project References -->
  <ProjectReference Include="..\..\Core\Sorcha.Blueprint.Engine\Sorcha.Blueprint.Engine.csproj" />
  <ProjectReference Include="..\..\Common\Sorcha.Blueprint.Models\Sorcha.Blueprint.Models.csproj" />
  <ProjectReference Include="..\..\Common\Sorcha.Cryptography\Sorcha.Cryptography.csproj" />
  <ProjectReference Include="..\..\Common\Sorcha.TransactionHandler\Sorcha.TransactionHandler.csproj" />
  <ProjectReference Include="..\..\Common\Sorcha.ServiceDefaults\Sorcha.ServiceDefaults.csproj" />
</ItemGroup>
```

---

## 7. Testing Strategy

### 7.1 Engine Testing

```
Sorcha.Blueprint.Engine.Tests/
├── ExecutionEngineTests.cs
│   ├── ExecuteActionAsync_ValidData_ReturnsSuccess
│   ├── ExecuteActionAsync_InvalidData_ReturnsValidationErrors
│   ├── ExecuteActionAsync_WithCalculations_AppliesCorrectly
│   ├── ExecuteActionAsync_WithConditions_RoutesCorrectly
│   └── ExecuteActionAsync_WithDisclosures_FiltersDataCorrectly
│
├── ActionProcessorTests.cs
│   ├── ProcessAsync_MultiStepValidation_Works
│   ├── ProcessAsync_NestedCalculations_Works
│   └── ProcessAsync_ComplexRouting_Works
│
├── SchemaValidatorTests.cs
│   ├── ValidateAsync_ValidData_ReturnsValid
│   ├── ValidateAsync_InvalidData_ReturnsErrors
│   ├── ValidateAsync_NestedObjects_ValidatesDeep
│   └── ValidateAsync_Arrays_ValidatesItems
│
├── JsonLogicEvaluatorTests.cs
│   ├── Evaluate_SimpleExpression_ReturnsCorrect
│   ├── Evaluate_ComplexExpression_ReturnsCorrect
│   ├── ApplyCalculationsAsync_MultipleFields_Works
│   └── EvaluateConditionsAsync_ConditionalRouting_Works
│
├── DisclosureProcessorTests.cs
│   ├── ApplyDisclosure_AllFields_ReturnsAllData
│   ├── ApplyDisclosure_SpecificFields_FiltersCorrectly
│   ├── ApplyDisclosure_NestedFields_FiltersDeep
│   └── CreateDisclosures_MultipleParticipants_Works
│
└── RoutingEngineTests.cs
    ├── DetermineNextAsync_SimpleCondition_RoutesCorrectly
    ├── DetermineNextAsync_ComplexCondition_RoutesCorrectly
    └── DetermineNextAsync_NoCondition_ReturnsDefaultRoute
```

**Coverage Target:** >90%

### 7.2 Service Testing

```
Sorcha.Blueprint.Service.Tests/
├── Endpoints/
│   ├── BlueprintEndpointsTests.cs
│   ├── ActionEndpointsTests.cs
│   ├── FileEndpointsTests.cs
│   └── ExecutionEndpointsTests.cs
│
├── Services/
│   ├── ActionServiceTests.cs
│   ├── ActionResolverTests.cs
│   ├── PayloadResolverTests.cs
│   └── TransactionBuilderTests.cs
│
├── Integration/
│   ├── BlueprintWorkflowTests.cs
│   ├── ActionSubmissionFlowTests.cs
│   ├── SignalRNotificationTests.cs
│   └── EncryptionIntegrationTests.cs
│
└── E2E/
    ├── LoanApplicationScenario.cs
    ├── PurchaseOrderScenario.cs
    └── MultiPartyWorkflowScenario.cs
```

**Coverage Target:** >85%

---

## 8. Implementation Plan Summary

### Phase 1: Blueprint Execution Engine (Weeks 1-4)
1. Create `Sorcha.Blueprint.Engine` project
2. Implement core interfaces
3. Implement `SchemaValidator`
4. Implement `JsonLogicEvaluator`
5. Implement `DisclosureProcessor`
6. Implement `RoutingEngine`
7. Implement `ActionProcessor`
8. Implement `ExecutionEngine`
9. Comprehensive unit tests (>90% coverage)

### Phase 2: Service Layer Foundation (Weeks 5-6)
10. Create action service interfaces
11. Implement `ActionResolver`
12. Implement `PayloadResolver`
13. Implement `TransactionBuilder`
14. Add FluentValidation validators

### Phase 3: API Endpoints (Weeks 7-9)
15. Implement Action endpoints
16. Implement File endpoints
17. Implement Execution endpoints (client validation helpers)
18. Add OpenAPI documentation

### Phase 4: SignalR & Notifications (Week 10)
19. Implement `ActionsHub`
20. Implement notification service
21. Add connection management
22. Test real-time notifications

### Phase 5: Integration & Testing (Weeks 11-13)
23. Wallet Service integration
24. Register Service integration
25. Redis caching setup
26. Integration tests
27. E2E workflow tests

### Phase 6: Client-Side Integration (Week 14)
28. Update Blazor Designer to use execution engine
29. Add client-side validation
30. Add real-time action updates via SignalR

### Phase 7: Performance & Security (Week 15)
31. Performance optimization
32. Security hardening
33. Rate limiting
34. Load testing

### Phase 8: Documentation & Deployment (Week 16)
35. API documentation
36. Developer guides
37. Deployment guides
38. Production deployment

**Total:** 16 weeks, 138 tasks

---

## 9. Success Criteria

### Functional Requirements
1. ✅ Blueprint CRUD operations work correctly
2. ✅ Execution engine validates data against JSON Schema
3. ✅ JSON Logic calculations execute correctly
4. ✅ Conditional routing determines next participant
5. ✅ Selective disclosure filters data correctly
6. ✅ Encrypted payloads created for participants
7. ✅ Transactions submitted to Register Service
8. ✅ SignalR notifications delivered in real-time
9. ✅ File attachments uploaded and retrieved
10. ✅ Client-side validation works in Blazor WASM

### Non-Functional Requirements
11. ✅ Engine unit test coverage >90%
12. ✅ Service test coverage >85%
13. ✅ API response time <200ms (p95) for GET
14. ✅ API response time <500ms (p95) for POST
15. ✅ Support 1000 req/s per instance
16. ✅ Support 10,000 concurrent SignalR connections
17. ✅ OpenAPI documentation complete
18. ✅ Zero critical security vulnerabilities

### Integration Requirements
19. ✅ Wallet Service encryption/decryption works
20. ✅ Register Service transaction submission works
21. ✅ Redis caching reduces load
22. ✅ .NET Aspire orchestration works

---

## 10. Key Architectural Decisions

| ID | Decision | Rationale |
|----|----------|-----------|
| D001 | Separate execution engine into standalone library | Enables client-side validation in Blazor WASM |
| D002 | Stateless engine design | Easier testing, client-side compatibility |
| D003 | Merge Blueprint Service + Action Service | Reduces operational complexity, better cohesion |
| D004 | Use SignalR for notifications | Real-time requirements, built-in .NET support |
| D005 | Redis for caching | Already used in architecture, distributed cache |
| D006 | Minimal APIs for endpoints | Modern, lightweight, aligns with .NET 10 |
| D007 | FluentValidation for request validation | Separation of concerns, reusable validators |
| D008 | JSON Logic for calculations/conditions | Declarative, portable, matches blueprint design |
| D009 | JSON Schema for data validation | Industry standard, extensive tooling |
| D010 | OpenAPI via .NET 10 built-in support | No external dependencies, better integration |

---

## 11. Security Considerations

### Authentication & Authorization
- JWT Bearer tokens required for all endpoints except health checks
- Wallet address must match JWT claim
- Action-level authorization (participant validation)

### Data Protection
- Payload encryption at rest and in transit
- Selective disclosure ensures minimal data exposure
- File upload validation (size, type, content)

### Input Validation
- JSON Schema validation for all action data
- FluentValidation for all request DTOs
- Sanitize file names and content types
- Prevent XXE, XSS, SQL injection

### Rate Limiting
- 100 requests per minute per wallet
- 1000 requests per hour per wallet
- Global: 10,000 requests per minute per instance

### Audit Logging
- Log all action submissions
- Log all transaction creations
- Log authorization failures
- Log validation failures

---

## 12. Next Steps

1. **Review this design** with architecture team
2. **Create detailed implementation plan** with task breakdown
3. **Set up project structure** (`Sorcha.Blueprint.Engine`)
4. **Begin Phase 1** - Execution engine implementation
5. **Weekly progress reviews** to track against plan

---

**Document Control**
- **Created:** 2025-11-15
- **Author:** Sorcha Architecture Team
- **Status:** Approved for Implementation
- **Review Frequency:** Weekly during implementation
- **Next Review:** 2025-11-22
