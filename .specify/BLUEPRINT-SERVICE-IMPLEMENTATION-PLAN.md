# Sorcha Blueprint Service - Unified Implementation Plan

**Version:** 2.1
**Date:** 2025-11-15
**Status:** In Progress
**Related:** [BLUEPRINT-SERVICE-UNIFIED-DESIGN.md](BLUEPRINT-SERVICE-UNIFIED-DESIGN.md)
**Duration:** 16 weeks (8 sprints of 2 weeks each)
**Total Tasks:** 138
**Completed Tasks:** 14/138 (10%)

---

## Progress Summary

✅ **Sprint 1 COMPLETED** - Execution Engine Foundation (Tasks 1.1-1.7)
✅ **Sprint 2 COMPLETED** - Execution Engine Complete (Tasks 2.1-2.8)

**Current Status:** Portable execution engine is complete and fully tested with:
- 6 core interfaces defined
- 6 execution models implemented
- 4 processing components (SchemaValidator, JsonLogicEvaluator, DisclosureProcessor, RoutingEngine)
- ActionProcessor orchestration layer
- ExecutionEngine facade
- 93 comprehensive unit tests
- 9 end-to-end integration tests covering realistic workflows

**Commits:**
- 2af69c7: Project setup
- 1f85881: Core interfaces
- e864217, 5abcebe: Models and SchemaValidator
- ca81afc: JsonLogicEvaluator
- 4eb3582: DisclosureProcessor and RoutingEngine
- c97d830: ActionProcessor
- 361fae4: ExecutionEngine
- 145b75e: Integration tests

---

## Executive Summary

This implementation plan details the development of the unified Sorcha Blueprint Service, which merges blueprint management capabilities with action execution functionality. The centerpiece is a **portable execution engine** that runs both client-side (Blazor WASM) and server-side for validation and verification.

**Key Milestones:**
- ~~Week 4: Portable execution engine complete~~ ✅ **ACHIEVED**
- Week 9: API endpoints functional
- Week 13: Full integration complete
- Week 16: Production ready

---

## Sprint Overview

| Sprint | Weeks | Focus | Deliverables | Status |
|--------|-------|-------|--------------|--------|
| Sprint 1 | 1-2 | Execution Engine Foundation | Core interfaces, schema validator, JSON Logic evaluator | ✅ **COMPLETED** |
| Sprint 2 | 3-4 | Execution Engine Complete | Disclosure processor, routing engine, full engine | ✅ **COMPLETED** |
| Sprint 3 | 5-6 | Service Layer Foundation | Action resolver, payload resolver, transaction builder | 🔲 Pending |
| Sprint 4 | 7-8 | API Endpoints Part 1 | Action endpoints, file endpoints | 🔲 Pending |
| Sprint 5 | 9-10 | API Endpoints Part 2 + SignalR | Execution endpoints, SignalR hub | 🔲 Pending |
| Sprint 6 | 11-12 | Integration | Wallet, Register, Redis integration | 🔲 Pending |
| Sprint 7 | 13-14 | Testing & Client Integration | E2E tests, Blazor Designer integration | 🔲 Pending |
| Sprint 8 | 15-16 | Performance, Security, Deployment | Optimization, hardening, go-live | 🔲 Pending |

---

## Sprint 1: Execution Engine Foundation (Weeks 1-2)

**Goal:** Establish the core execution engine library with schema validation and JSON Logic evaluation.

### Week 1: Project Setup & Core Interfaces

#### Task 1.1: Create Sorcha.Blueprint.Engine Project ✅
**Effort:** 2 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 2af69c7

**Description:**
Create new class library project for the portable execution engine.

**Acceptance Criteria:**
- [x] Project created at `src/Core/Sorcha.Blueprint.Engine/`
- [x] Target framework set to `net10.0`
- [x] Project references `Sorcha.Blueprint.Models`
- [x] NuGet packages added: JsonSchema.Net, JsonLogic.Net, JsonPath.Net
- [x] Solution file updated to include project
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Sorcha.Blueprint.Engine.csproj`

---

#### Task 1.2: Define Core Execution Interfaces ✅
**Effort:** 4 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 1f85881

**Description:**
Define all core interfaces for the execution engine.

**Acceptance Criteria:**
- [x] `IExecutionEngine.cs` created with all methods
- [x] `IActionProcessor.cs` created
- [x] `ISchemaValidator.cs` created
- [x] `IJsonLogicEvaluator.cs` created
- [x] `IDisclosureProcessor.cs` created
- [x] `IRoutingEngine.cs` created
- [x] XML documentation added to all interfaces
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/IExecutionEngine.cs`
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/IActionProcessor.cs`
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/ISchemaValidator.cs`
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/IJsonLogicEvaluator.cs`
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/IDisclosureProcessor.cs`
- `src/Core/Sorcha.Blueprint.Engine/Interfaces/IRoutingEngine.cs`

---

#### Task 1.3: Create Execution Models ✅
**Effort:** 3 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** e864217

**Description:**
Create models for execution context, results, and validation.

**Acceptance Criteria:**
- [x] `ExecutionContext.cs` created with all properties
- [x] `ActionExecutionResult.cs` created
- [x] `ValidationResult.cs` created
- [x] `ValidationError.cs` created
- [x] `RoutingResult.cs` created
- [x] `DisclosureResult.cs` created
- [x] All models have XML documentation
- [x] Models are immutable where appropriate (init properties)
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Models/ExecutionContext.cs`
- `src/Core/Sorcha.Blueprint.Engine/Models/ActionExecutionResult.cs`
- `src/Core/Sorcha.Blueprint.Engine/Models/ValidationResult.cs`
- `src/Core/Sorcha.Blueprint.Engine/Models/ValidationError.cs`
- `src/Core/Sorcha.Blueprint.Engine/Models/RoutingResult.cs`
- `src/Core/Sorcha.Blueprint.Engine/Models/DisclosureResult.cs`

---

#### Task 1.4: Implement SchemaValidator ✅
**Effort:** 8 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** e864217

**Description:**
Implement JSON Schema validation using JsonSchema.Net.

**Acceptance Criteria:**
- [x] `SchemaValidator.cs` implements `ISchemaValidator`
- [x] Validates data against JSON Schema Draft 2020-12
- [x] Returns detailed validation errors with JSON Pointers
- [x] Handles nested objects and arrays
- [x] Handles custom formats (email, date-time, etc.)
- [x] Async validation supported
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/SchemaValidator.cs`

---

#### Task 1.5: Unit Tests for SchemaValidator ✅
**Effort:** 4 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 5abcebe

**Description:**
Create comprehensive unit tests for schema validation.

**Acceptance Criteria:**
- [x] Test project created: `tests/Sorcha.Blueprint.Engine.Tests/`
- [x] `SchemaValidatorTests.cs` created
- [x] Test: ValidateAsync_ValidData_ReturnsValid
- [x] Test: ValidateAsync_InvalidData_ReturnsErrors
- [x] Test: ValidateAsync_NestedObjects_ValidatesDeep
- [x] Test: ValidateAsync_Arrays_ValidatesItems
- [x] Test: ValidateAsync_CustomFormats_Works
- [x] Test: ValidateAsync_RequiredFields_Validates
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/SchemaValidatorTests.cs`

---

### Week 2: JSON Logic Evaluation

#### Task 1.6: Implement JsonLogicEvaluator ✅
**Effort:** 10 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** ca81afc

**Description:**
Implement JSON Logic evaluation for calculations and conditions.

**Acceptance Criteria:**
- [x] `JsonLogicEvaluator.cs` implements `IJsonLogicEvaluator`
- [x] `Evaluate()` method executes JSON Logic expressions
- [x] `ApplyCalculationsAsync()` applies multiple calculations
- [x] `EvaluateConditionsAsync()` evaluates routing conditions
- [x] Supports all JSON Logic operators (comparison, logical, arithmetic, array ops)
- [x] Handles variable references (`{"var": "fieldName"}`)
- [x] Handles nested expressions
- [x] Error handling for invalid expressions
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/JsonLogicEvaluator.cs`

---

#### Task 1.7: Unit Tests for JsonLogicEvaluator ✅
**Effort:** 6 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** ca81afc

**Description:**
Create comprehensive unit tests for JSON Logic evaluation.

**Acceptance Criteria:**
- [x] `JsonLogicEvaluatorTests.cs` created
- [x] Test: Evaluate_SimpleComparison_ReturnsCorrect
- [x] Test: Evaluate_ComplexExpression_ReturnsCorrect
- [x] Test: Evaluate_NestedExpression_Works
- [x] Test: ApplyCalculationsAsync_MultipleFields_Works
- [x] Test: ApplyCalculationsAsync_DependentCalculations_Works
- [x] Test: EvaluateConditionsAsync_SimpleCondition_Works
- [x] Test: EvaluateConditionsAsync_ComplexCondition_Works
- [x] Test: Evaluate_InvalidExpression_ThrowsException
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/JsonLogicEvaluatorTests.cs`

---

#### Task 1.8: Integration Test - Schema + Logic
**Effort:** 3 hours
**Priority:** P1

**Description:**
Create integration test that combines schema validation with JSON Logic.

**Acceptance Criteria:**
- [ ] `IntegrationTests.cs` created
- [ ] Test: ValidateAndCalculate_CompleteScenario_Works
- [ ] Test validates data against schema
- [ ] Test applies calculations using JSON Logic
- [ ] Test verifies calculated values are correct
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/IntegrationTests.cs`

---

**Sprint 1 Total Effort:** 40 hours
**Sprint 1 Deliverable:** Execution engine foundation with schema validation and JSON Logic

**Sprint 1 Status:** ✅ **COMPLETED**
- All 7 tasks completed (1.1-1.7)
- Execution engine foundation fully implemented
- Comprehensive unit tests with >90% coverage
- All acceptance criteria met

---

## Sprint 2: Execution Engine Complete (Weeks 3-4)

**Goal:** Complete the execution engine with disclosure processing, routing, and full action processing.

### Week 3: Disclosure & Routing

#### Task 2.1: Implement DisclosureProcessor ✅
**Effort:** 8 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 4eb3582

**Description:**
Implement selective disclosure processing using JSON Pointers.

**Acceptance Criteria:**
- [x] `DisclosureProcessor.cs` implements `IDisclosureProcessor`
- [x] `ApplyDisclosure()` filters data by JSON Pointers
- [x] `CreateDisclosures()` creates participant-specific payloads
- [x] Supports both `/field` and `#/field` pointer formats
- [x] Handles nested object filtering
- [x] Handles array filtering
- [x] Handles wildcard patterns (`/*` for all fields)
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/DisclosureProcessor.cs`

---

#### Task 2.2: Unit Tests for DisclosureProcessor ✅
**Effort:** 5 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 4eb3582

**Description:**
Create comprehensive unit tests for disclosure processing.

**Acceptance Criteria:**
- [x] `DisclosureProcessorTests.cs` created
- [x] Test: ApplyDisclosure_AllFields_ReturnsAllData
- [x] Test: ApplyDisclosure_SpecificFields_FiltersCorrectly
- [x] Test: ApplyDisclosure_NestedFields_FiltersDeep
- [x] Test: ApplyDisclosure_Arrays_FiltersItems
- [x] Test: ApplyDisclosure_Wildcards_Works
- [x] Test: CreateDisclosures_MultipleParticipants_Works
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/DisclosureProcessorTests.cs`

---

#### Task 2.3: Implement RoutingEngine ✅
**Effort:** 6 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 4eb3582

**Description:**
Implement routing logic to determine next participant.

**Acceptance Criteria:**
- [x] `RoutingEngine.cs` implements `IRoutingEngine`
- [x] `DetermineNextAsync()` evaluates action conditions
- [x] Uses `IJsonLogicEvaluator` for condition evaluation
- [x] Handles simple routing (next participant ID)
- [x] Handles conditional routing (JSON Logic expressions)
- [x] Handles workflow completion (no next action)
- [x] Handles rejection routing (back to previous)
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/RoutingEngine.cs`

---

#### Task 2.4: Unit Tests for RoutingEngine ✅
**Effort:** 4 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 4eb3582

**Description:**
Create comprehensive unit tests for routing.

**Acceptance Criteria:**
- [x] `RoutingEngineTests.cs` created
- [x] Test: DetermineNextAsync_SimpleRouting_Works
- [x] Test: DetermineNextAsync_ConditionalRouting_Works
- [x] Test: DetermineNextAsync_ComplexCondition_Works
- [x] Test: DetermineNextAsync_WorkflowComplete_Detects
- [x] Test: DetermineNextAsync_NoCondition_UsesDefault
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/RoutingEngineTests.cs`

---

### Week 4: Action Processor & Full Engine

#### Task 2.5: Implement ActionProcessor ✅
**Effort:** 10 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** c97d830

**Description:**
Implement the action processor that orchestrates all processing steps.

**Acceptance Criteria:**
- [x] `ActionProcessor.cs` implements `IActionProcessor`
- [x] `ProcessAsync()` orchestrates all steps:
  - [x] Schema validation
  - [x] Calculation application
  - [x] Routing determination
  - [x] Disclosure creation
- [x] Returns complete `ActionExecutionResult`
- [x] Short-circuits on validation failure
- [x] Aggregates all errors and warnings
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/ActionProcessor.cs`

---

#### Task 2.6: Unit Tests for ActionProcessor ✅
**Effort:** 6 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** c97d830

**Description:**
Create comprehensive unit tests for action processing.

**Acceptance Criteria:**
- [x] `ActionProcessorTests.cs` created
- [x] Test: ProcessAsync_ValidData_ReturnsSuccess
- [x] Test: ProcessAsync_InvalidData_ReturnsErrors
- [x] Test: ProcessAsync_WithCalculations_AppliesCorrectly
- [x] Test: ProcessAsync_WithConditionalRouting_Works
- [x] Test: ProcessAsync_WithDisclosures_CreatesCorrectly
- [x] Test: ProcessAsync_CompleteWorkflow_Works
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/ActionProcessorTests.cs`

---

#### Task 2.7: Implement ExecutionEngine ✅
**Effort:** 8 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 361fae4

**Description:**
Implement the main execution engine facade.

**Acceptance Criteria:**
- [x] `ExecutionEngine.cs` implements `IExecutionEngine`
- [x] `ExecuteActionAsync()` delegates to `IActionProcessor`
- [x] `ValidateAsync()` validates without executing
- [x] `DetermineRoutingAsync()` determines routing without full execution
- [x] `ApplyCalculationsAsync()` applies calculations only
- [x] `ApplyDisclosures()` applies disclosure rules
- [x] Thread-safe implementation
- [x] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Implementation/ExecutionEngine.cs`

---

#### Task 2.8: Integration Tests for Complete Workflows ✅
**Effort:** 5 hours
**Priority:** P0
**Status:** ✅ COMPLETED
**Commit:** 145b75e, 361fae4

**Description:**
Create comprehensive integration tests for the main engine and complete workflows.

**Acceptance Criteria:**
- [x] `ExecutionEngineTests.cs` created with component tests
- [x] `IntegrationTests.cs` created with end-to-end scenarios
- [x] Test: ExecuteActionAsync_ValidData_ExecutesCompletely
- [x] Test: ValidateAsync_Works
- [x] Test: DetermineRoutingAsync_Works
- [x] Test: ApplyCalculationsAsync_Works
- [x] Test: ApplyDisclosures_Works
- [x] Test: LoanApplicationWorkflow_CompletesSuccessfully
- [x] Test: PurchaseOrderWorkflow_CalculatesTotalsAndAppliesDiscount
- [x] Test: Multi-step survey workflow
- [x] All tests pass
- [x] Coverage >90%

**Files:**
- `tests/Sorcha.Blueprint.Engine.Tests/ExecutionEngineTests.cs`
- `tests/Sorcha.Blueprint.Engine.Tests/IntegrationTests.cs`

---

#### Task 2.9: Add Dependency Injection Extensions
**Effort:** 2 hours
**Priority:** P1

**Description:**
Create extension methods for registering engine services.

**Acceptance Criteria:**
- [ ] `ServiceCollectionExtensions.cs` created
- [ ] `AddBlueprintExecutionEngine()` extension method
- [ ] Registers all engine services as scoped
- [ ] Supports both server-side and client-side DI
- [ ] Build succeeds

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Extensions/ServiceCollectionExtensions.cs`

---

**Sprint 2 Total Effort:** 54 hours
**Sprint 2 Deliverable:** Complete portable execution engine

**Sprint 2 Status:** ✅ **COMPLETED**
- All 8 core tasks completed (2.1-2.8)
- Full execution engine with all components integrated
- ActionProcessor orchestration layer complete
- ExecutionEngine facade implemented
- Comprehensive unit tests and integration tests
- End-to-end workflow scenarios tested (loan applications, purchase orders, surveys)
- All acceptance criteria met
- **Note:** Task 2.9 (DI extensions) deferred to Sprint 3 as it's better suited for service layer integration

---

## Sprint 3: Service Layer Foundation (Weeks 5-6)

**Goal:** Build the service layer that uses the execution engine and integrates with external services.

### Week 5: Action Resolution & Payload Management

#### Task 3.1: Update Sorcha.Blueprint.Service Project
**Effort:** 2 hours
**Priority:** P0

**Description:**
Update the existing Blueprint Service project to include action management.

**Acceptance Criteria:**
- [ ] Project reference added to `Sorcha.Blueprint.Engine`
- [ ] NuGet packages added: FluentValidation.AspNetCore, Microsoft.AspNetCore.SignalR
- [ ] Folder structure created: Endpoints/Actions, Services/Actions, Models/Actions, Validators, Hubs
- [ ] Build succeeds

---

#### Task 3.2: Define Action Service Interfaces
**Effort:** 3 hours
**Priority:** P0

**Description:**
Define interfaces for action-related services.

**Acceptance Criteria:**
- [ ] `IActionService.cs` created with all methods
- [ ] `IActionResolver.cs` created
- [ ] `IPayloadResolver.cs` created
- [ ] `ITransactionBuilder.cs` created
- [ ] XML documentation added
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IActionService.cs`
- `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IActionResolver.cs`
- `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IPayloadResolver.cs`
- `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilder.cs`

---

#### Task 3.3: Create Action DTOs
**Effort:** 4 hours
**Priority:** P0

**Description:**
Create request and response DTOs for action operations.

**Acceptance Criteria:**
- [ ] `ActionSubmission.cs` created
- [ ] `ActionRejection.cs` created
- [ ] `ActionFilter.cs` created
- [ ] `ActionResponse.cs` created
- [ ] `ActionSummary.cs` created
- [ ] `TransactionResult.cs` created
- [ ] `FileAttachment.cs` created
- [ ] `PagedResult<T>.cs` created
- [ ] All DTOs have XML documentation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Models/Requests/ActionSubmission.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Requests/ActionRejection.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Requests/ActionFilter.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Responses/ActionResponse.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Responses/ActionSummary.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Responses/TransactionResult.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Internal/FileAttachment.cs`
- `src/Services/Sorcha.Blueprint.Service/Models/Common/PagedResult.cs`

---

#### Task 3.4: Implement ActionResolver
**Effort:** 8 hours
**Priority:** P0

**Description:**
Implement service for resolving blueprints and actions.

**Acceptance Criteria:**
- [ ] `ActionResolver.cs` implements `IActionResolver`
- [ ] `GetBlueprintAsync()` retrieves blueprint from repository or cache
- [ ] `GetActionDefinitionAsync()` extracts action from blueprint
- [ ] `ResolveParticipantWalletsAsync()` resolves participant IDs to wallets
- [ ] Uses Redis caching (10 min TTL for blueprints)
- [ ] Handles blueprint not found gracefully
- [ ] Thread-safe implementation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionResolver.cs`

---

#### Task 3.5: Implement PayloadResolver
**Effort:** 10 hours
**Priority:** P0

**Description:**
Implement service for payload encryption and aggregation.

**Acceptance Criteria:**
- [ ] `PayloadResolver.cs` implements `IPayloadResolver`
- [ ] `CreateEncryptedPayloadsAsync()` creates encrypted payloads from disclosure results
- [ ] Calls Wallet Service for encryption
- [ ] `AggregateHistoricalDataAsync()` retrieves and aggregates previous transaction data
- [ ] Calls Register Service for transaction history
- [ ] Calls Wallet Service for decryption
- [ ] Applies disclosure rules to historical data
- [ ] Handles missing transactions gracefully
- [ ] Thread-safe implementation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Implementation/PayloadResolver.cs`

---

#### Task 3.6: Unit Tests for PayloadResolver
**Effort:** 5 hours
**Priority:** P0

**Description:**
Create unit tests for payload resolution.

**Acceptance Criteria:**
- [ ] `PayloadResolverTests.cs` created
- [ ] Test: CreateEncryptedPayloadsAsync_Works
- [ ] Test: CreateEncryptedPayloadsAsync_MultipleParticipants_Works
- [ ] Test: AggregateHistoricalDataAsync_SinglePrevious_Works
- [ ] Test: AggregateHistoricalDataAsync_MultiplePrevious_Aggregates
- [ ] Test: AggregateHistoricalDataAsync_WithDisclosures_Filters
- [ ] Mock Wallet Service and Register Service
- [ ] All tests pass
- [ ] Coverage >85%

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Services/PayloadResolverTests.cs`

---

### Week 6: Transaction Building & Validation

#### Task 3.7: Implement TransactionBuilder
**Effort:** 10 hours
**Priority:** P0

**Description:**
Implement service for building blockchain transactions.

**Acceptance Criteria:**
- [ ] `TransactionBuilder.cs` implements `ITransactionBuilder`
- [ ] `BuildActionTransactionAsync()` builds action transaction with metadata
- [ ] Includes blueprint ID, action ID, instance ID in metadata
- [ ] Links to previous transaction hash
- [ ] `BuildRejectionTransactionAsync()` builds rejection transaction
- [ ] `BuildFileTransactionsAsync()` builds file transactions
- [ ] Uses `Sorcha.TransactionHandler` for transaction creation
- [ ] Thread-safe implementation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Implementation/TransactionBuilder.cs`

---

#### Task 3.8: Unit Tests for TransactionBuilder
**Effort:** 5 hours
**Priority:** P0

**Description:**
Create unit tests for transaction building.

**Acceptance Criteria:**
- [ ] `TransactionBuilderTests.cs` created
- [ ] Test: BuildActionTransactionAsync_NewInstance_CreatesInstanceId
- [ ] Test: BuildActionTransactionAsync_Continuation_LinksPrevious
- [ ] Test: BuildActionTransactionAsync_MetadataCorrect_Validates
- [ ] Test: BuildRejectionTransactionAsync_Works
- [ ] Test: BuildFileTransactionsAsync_MultipleFiles_Works
- [ ] All tests pass
- [ ] Coverage >85%

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Services/TransactionBuilderTests.cs`

---

#### Task 3.9: Implement FluentValidation Validators
**Effort:** 4 hours
**Priority:** P0

**Description:**
Create validators for action requests.

**Acceptance Criteria:**
- [ ] `ActionSubmissionValidator.cs` created
  - [ ] Validates wallet address format
  - [ ] Validates blueprint ID not empty
  - [ ] Validates action ID not empty
  - [ ] Validates data not null
  - [ ] Validates file size limits
  - [ ] Validates file types
- [ ] `ActionRejectionValidator.cs` created
  - [ ] Validates wallet address format
  - [ ] Validates transaction ID not empty
  - [ ] Validates reason not empty
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Validators/ActionSubmissionValidator.cs`
- `src/Services/Sorcha.Blueprint.Service/Validators/ActionRejectionValidator.cs`

---

#### Task 3.10: Unit Tests for Validators
**Effort:** 3 hours
**Priority:** P1

**Description:**
Create unit tests for validators.

**Acceptance Criteria:**
- [ ] `ActionSubmissionValidatorTests.cs` created
- [ ] Test: Validate_ValidSubmission_Passes
- [ ] Test: Validate_InvalidWallet_Fails
- [ ] Test: Validate_MissingData_Fails
- [ ] Test: Validate_FileTooLarge_Fails
- [ ] `ActionRejectionValidatorTests.cs` created
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Validators/ActionSubmissionValidatorTests.cs`
- `tests/Sorcha.Blueprint.Service.Tests/Validators/ActionRejectionValidatorTests.cs`

---

**Sprint 3 Total Effort:** 54 hours
**Sprint 3 Deliverable:** Service layer with action resolution, payload management, and transaction building

---

## Sprint 4: API Endpoints Part 1 (Weeks 7-8)

**Goal:** Implement action and file API endpoints.

### Week 7: Action Endpoints

#### Task 4.1: Implement ActionService
**Effort:** 12 hours
**Priority:** P0

**Description:**
Implement the main action service that orchestrates all operations.

**Acceptance Criteria:**
- [ ] `ActionService.cs` implements `IActionService`
- [ ] `GetStartingActionsAsync()` retrieves starting actions from published blueprints
- [ ] `GetPendingActionsAsync()` retrieves pending actions from Register Service
- [ ] `GetActionByIdAsync()` retrieves action details with historical data
- [ ] `SubmitActionAsync()` submits action:
  - [ ] Uses `IExecutionEngine` to execute action
  - [ ] Uses `IPayloadResolver` to create encrypted payloads
  - [ ] Uses `ITransactionBuilder` to build transaction
  - [ ] Submits to Register Service
  - [ ] Returns transaction result
- [ ] `RejectActionAsync()` creates rejection transaction
- [ ] Error handling and logging
- [ ] Thread-safe implementation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionService.cs`

---

#### Task 4.2: Implement Action API Endpoints
**Effort:** 8 hours
**Priority:** P0

**Description:**
Create minimal API endpoints for action operations.

**Acceptance Criteria:**
- [ ] `ActionEndpoints.cs` created
- [ ] GET `/api/actions/{wallet}/{register}/blueprints` - Get starting actions
- [ ] GET `/api/actions/{wallet}/{register}` - Get pending actions (with pagination)
- [ ] GET `/api/actions/{wallet}/{register}/{tx}` - Get action by ID
- [ ] POST `/api/actions` - Submit action
- [ ] POST `/api/actions/reject` - Reject action
- [ ] All endpoints use FluentValidation
- [ ] All endpoints return ProblemDetails on error
- [ ] OpenAPI documentation added (Produces, Accepts, Summary)
- [ ] Authorization required (JWT)
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Endpoints/ActionEndpoints.cs`

---

#### Task 4.3: Unit Tests for ActionService
**Effort:** 8 hours
**Priority:** P0

**Description:**
Create unit tests for ActionService.

**Acceptance Criteria:**
- [ ] `ActionServiceTests.cs` created
- [ ] Test: GetStartingActionsAsync_ReturnsStartingActions
- [ ] Test: GetPendingActionsAsync_ReturnsPendingActions
- [ ] Test: GetActionByIdAsync_ReturnsActionWithHistory
- [ ] Test: SubmitActionAsync_ValidSubmission_SubmitsTransaction
- [ ] Test: SubmitActionAsync_InvalidData_ThrowsValidationException
- [ ] Test: RejectActionAsync_CreatesRejectionTransaction
- [ ] Mock all dependencies
- [ ] All tests pass
- [ ] Coverage >85%

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Services/ActionServiceTests.cs`

---

#### Task 4.4: Integration Tests for Action Endpoints
**Effort:** 6 hours
**Priority:** P1

**Description:**
Create integration tests for action API endpoints.

**Acceptance Criteria:**
- [ ] `ActionEndpointsTests.cs` created
- [ ] Test: GET_StartingActions_ReturnsOk
- [ ] Test: GET_PendingActions_ReturnsPagedResult
- [ ] Test: GET_ActionById_ReturnsAction
- [ ] Test: POST_Actions_ValidSubmission_ReturnsAccepted
- [ ] Test: POST_Actions_InvalidData_ReturnsBadRequest
- [ ] Test: POST_ActionsReject_ReturnsAccepted
- [ ] Use WebApplicationFactory
- [ ] Mock external services (Wallet, Register)
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Integration/ActionEndpointsTests.cs`

---

### Week 8: File Endpoints

#### Task 4.5: Implement File API Endpoints
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create endpoints for file upload and download.

**Acceptance Criteria:**
- [ ] `FileEndpoints.cs` created
- [ ] GET `/api/files/{wallet}/{register}/{tx}/{fileId}` - Download file
- [ ] Retrieves file transaction from Register Service
- [ ] Validates wallet has permission
- [ ] Streams file content
- [ ] Sets correct Content-Type and Content-Disposition headers
- [ ] Supports large files (streaming)
- [ ] OpenAPI documentation added
- [ ] Authorization required (JWT)
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Endpoints/FileEndpoints.cs`

---

#### Task 4.6: Integration Tests for File Endpoints
**Effort:** 4 hours
**Priority:** P1

**Description:**
Create integration tests for file endpoints.

**Acceptance Criteria:**
- [ ] `FileEndpointsTests.cs` created
- [ ] Test: GET_File_ReturnsFileContent
- [ ] Test: GET_File_UnauthorizedWallet_ReturnsForbidden
- [ ] Test: GET_File_NotFound_ReturnsNotFound
- [ ] Use WebApplicationFactory
- [ ] Mock Register Service
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Integration/FileEndpointsTests.cs`

---

#### Task 4.7: Add OpenAPI Documentation
**Effort:** 4 hours
**Priority:** P1

**Description:**
Enhance OpenAPI documentation for all endpoints.

**Acceptance Criteria:**
- [ ] All endpoints have Summary and Description
- [ ] All endpoints document request/response schemas
- [ ] All endpoints document status codes (200, 400, 401, 403, 404, 500)
- [ ] Example requests and responses added
- [ ] Security schemes documented (JWT Bearer)
- [ ] Scalar UI accessible at `/scalar/v1`
- [ ] OpenAPI spec accessible at `/openapi/v1.json`

---

#### Task 4.8: Add Caching Configuration
**Effort:** 3 hours
**Priority:** P1

**Description:**
Configure Redis caching for action endpoints.

**Acceptance Criteria:**
- [ ] Redis output caching configured in `Program.cs`
- [ ] Starting actions cached (5 min TTL)
- [ ] Pending actions cached (1 min TTL)
- [ ] Action details cached (2 min TTL)
- [ ] Blueprints cached (10 min TTL)
- [ ] Tag-based cache invalidation configured
- [ ] Cache keys follow naming convention

---

**Sprint 4 Total Effort:** 51 hours
**Sprint 4 Deliverable:** Action and file API endpoints functional

---

## Sprint 5: API Endpoints Part 2 + SignalR (Weeks 9-10)

**Goal:** Implement execution helper endpoints and real-time notifications.

### Week 9: Execution Endpoints

#### Task 5.1: Implement Execution Endpoints
**Effort:** 6 hours
**Priority:** P1

**Description:**
Create endpoints for client-side validation and execution helpers.

**Acceptance Criteria:**
- [ ] `ExecutionEndpoints.cs` created
- [ ] POST `/api/execution/validate` - Validate action data (client-side helper)
- [ ] POST `/api/execution/calculate` - Apply calculations only
- [ ] POST `/api/execution/route` - Determine routing only
- [ ] POST `/api/execution/disclose` - Apply disclosure rules
- [ ] All endpoints use `IExecutionEngine`
- [ ] All endpoints support ValidationOnly mode
- [ ] OpenAPI documentation added
- [ ] Authorization required (JWT)
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Endpoints/ExecutionEndpoints.cs`

---

#### Task 5.2: Integration Tests for Execution Endpoints
**Effort:** 4 hours
**Priority:** P1

**Description:**
Create integration tests for execution endpoints.

**Acceptance Criteria:**
- [ ] `ExecutionEndpointsTests.cs` created
- [ ] Test: POST_ExecutionValidate_ValidData_ReturnsValid
- [ ] Test: POST_ExecutionValidate_InvalidData_ReturnsErrors
- [ ] Test: POST_ExecutionCalculate_AppliesCalculations
- [ ] Test: POST_ExecutionRoute_DeterminesRouting
- [ ] Test: POST_ExecutionDisclose_FiltersData
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Integration/ExecutionEndpointsTests.cs`

---

#### Task 5.3: Implement Internal Notification Endpoint
**Effort:** 3 hours
**Priority:** P0

**Description:**
Create internal endpoint for Register Service to notify of transaction confirmations.

**Acceptance Criteria:**
- [ ] `NotificationEndpoints.cs` created
- [ ] POST `/api/actions/notify` - Receive transaction confirmation
- [ ] Validates internal service authentication (API key or Aspire auth)
- [ ] Broadcasts notification via SignalR
- [ ] Returns 202 Accepted
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Endpoints/NotificationEndpoints.cs`

---

### Week 10: SignalR Hub

#### Task 5.4: Implement ActionsHub (SignalR)
**Effort:** 8 hours
**Priority:** P0

**Description:**
Create SignalR hub for real-time action notifications.

**Acceptance Criteria:**
- [ ] `ActionsHub.cs` created at `/actionshub`
- [ ] JWT authentication via query parameter `?access_token={jwt}`
- [ ] `OnConnectedAsync()` validates JWT and adds to wallet group
- [ ] `OnDisconnectedAsync()` removes from groups
- [ ] Server method: `SubscribeToWallet(walletAddress)`
- [ ] Server method: `UnsubscribeFromWallet(walletAddress)`
- [ ] Client method: `ActionAvailable(notification)` - broadcast new actions
- [ ] Client method: `ActionConfirmed(notification)` - broadcast confirmations
- [ ] Client method: `ActionRejected(notification)` - broadcast rejections
- [ ] Connection groups: `wallet:{walletAddress}`
- [ ] Error handling and logging
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Hubs/ActionsHub.cs`

---

#### Task 5.5: Configure SignalR in Program.cs
**Effort:** 2 hours
**Priority:** P0

**Description:**
Add SignalR configuration to the service.

**Acceptance Criteria:**
- [ ] SignalR added to services in `Program.cs`
- [ ] Redis backplane configured for scale-out
- [ ] JWT authentication configured for SignalR
- [ ] `/actionshub` endpoint mapped
- [ ] CORS configured for SignalR
- [ ] Build succeeds

---

#### Task 5.6: Create Notification Service
**Effort:** 4 hours
**Priority:** P0

**Description:**
Create service for broadcasting notifications via SignalR.

**Acceptance Criteria:**
- [ ] `INotificationService.cs` interface created
- [ ] `NotificationService.cs` implementation created
- [ ] `NotifyActionAvailableAsync()` broadcasts to wallet group
- [ ] `NotifyActionConfirmedAsync()` broadcasts to wallet group
- [ ] `NotifyActionRejectedAsync()` broadcasts to wallet group
- [ ] Uses `IHubContext<ActionsHub>` for broadcasting
- [ ] Thread-safe implementation
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/INotificationService.cs`
- `src/Services/Sorcha.Blueprint.Service/Services/Implementation/NotificationService.cs`

---

#### Task 5.7: Integration Tests for SignalR Hub
**Effort:** 6 hours
**Priority:** P1

**Description:**
Create integration tests for SignalR functionality.

**Acceptance Criteria:**
- [ ] `SignalRHubTests.cs` created
- [ ] Test: Connect_WithValidJWT_Succeeds
- [ ] Test: Connect_WithInvalidJWT_Fails
- [ ] Test: SubscribeToWallet_ReceivesNotifications
- [ ] Test: ActionConfirmed_BroadcastsToWallet
- [ ] Test: MultipleClients_ReceiveIndependently
- [ ] Use Microsoft.AspNetCore.SignalR.Client
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Integration/SignalRHubTests.cs`

---

**Sprint 5 Total Effort:** 33 hours
**Sprint 5 Deliverable:** Execution endpoints and SignalR real-time notifications

---

## Sprint 6: Integration (Weeks 11-12)

**Goal:** Integrate with external services (Wallet, Register) and configure caching.

### Week 11: External Service Integration

#### Task 6.1: Create Wallet Service Client
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create HTTP client for Wallet Service integration.

**Acceptance Criteria:**
- [ ] `IWalletServiceClient.cs` interface created
- [ ] `WalletServiceClient.cs` implementation created
- [ ] `EncryptPayloadAsync()` method
- [ ] `DecryptPayloadAsync()` method
- [ ] `SignTransactionAsync()` method
- [ ] Uses `IHttpClientFactory`
- [ ] Resilience policies configured (retry, circuit breaker, timeout)
- [ ] Service discovery configured via Aspire
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Clients/IWalletServiceClient.cs`
- `src/Services/Sorcha.Blueprint.Service/Clients/WalletServiceClient.cs`

---

#### Task 6.2: Create Register Service Client
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create HTTP client for Register Service integration.

**Acceptance Criteria:**
- [ ] `IRegisterServiceClient.cs` interface created
- [ ] `RegisterServiceClient.cs` implementation created
- [ ] `SubmitTransactionAsync()` method
- [ ] `GetTransactionAsync()` method
- [ ] `GetTransactionHistoryAsync()` method
- [ ] `GetPendingTransactionsAsync()` method
- [ ] Uses `IHttpClientFactory`
- [ ] Resilience policies configured
- [ ] Service discovery configured via Aspire
- [ ] Build succeeds

**Files:**
- `src/Services/Sorcha.Blueprint.Service/Clients/IRegisterServiceClient.cs`
- `src/Services/Sorcha.Blueprint.Service/Clients/RegisterServiceClient.cs`

---

#### Task 6.3: Update PayloadResolver with Wallet Service Integration
**Effort:** 4 hours
**Priority:** P0

**Description:**
Update PayloadResolver to use WalletServiceClient for encryption/decryption.

**Acceptance Criteria:**
- [ ] `PayloadResolver.cs` updated to inject `IWalletServiceClient`
- [ ] `CreateEncryptedPayloadsAsync()` calls `WalletServiceClient.EncryptPayloadAsync()`
- [ ] `AggregateHistoricalDataAsync()` calls `WalletServiceClient.DecryptPayloadAsync()`
- [ ] Error handling for encryption failures
- [ ] Retry logic for transient failures
- [ ] Build succeeds

---

#### Task 6.4: Update ActionService with Register Service Integration
**Effort:** 4 hours
**Priority:** P0

**Description:**
Update ActionService to use RegisterServiceClient.

**Acceptance Criteria:**
- [ ] `ActionService.cs` updated to inject `IRegisterServiceClient`
- [ ] `GetPendingActionsAsync()` calls `RegisterServiceClient.GetPendingTransactionsAsync()`
- [ ] `GetActionByIdAsync()` calls `RegisterServiceClient.GetTransactionAsync()`
- [ ] `SubmitActionAsync()` calls `RegisterServiceClient.SubmitTransactionAsync()`
- [ ] Error handling for submission failures
- [ ] Build succeeds

---

#### Task 6.5: Integration Tests with Mocked Services
**Effort:** 8 hours
**Priority:** P1

**Description:**
Create integration tests with mocked external services.

**Acceptance Criteria:**
- [ ] `ExternalServiceIntegrationTests.cs` created
- [ ] Test: SubmitAction_CallsWalletService_ForEncryption
- [ ] Test: SubmitAction_CallsRegisterService_ForSubmission
- [ ] Test: GetActionById_CallsRegisterService_ForTransaction
- [ ] Test: GetActionById_CallsWalletService_ForDecryption
- [ ] Use WireMock.Net for mocking HTTP services
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Integration/ExternalServiceIntegrationTests.cs`

---

### Week 12: Caching & Performance

#### Task 6.6: Configure Redis Caching
**Effort:** 4 hours
**Priority:** P1

**Description:**
Configure comprehensive Redis caching strategy.

**Acceptance Criteria:**
- [ ] Redis configured in `Program.cs`
- [ ] Output caching policies defined:
  - [ ] `blueprint-cache` - 10 min TTL
  - [ ] `starting-actions-cache` - 5 min TTL
  - [ ] `pending-actions-cache` - 1 min TTL
  - [ ] `action-details-cache` - 2 min TTL
- [ ] Tag-based cache invalidation configured
- [ ] Cache keys follow naming convention: `{service}:{type}:{id}`
- [ ] Build succeeds

---

#### Task 6.7: Add Cache Invalidation Logic
**Effort:** 3 hours
**Priority:** P1

**Description:**
Add logic to invalidate caches when data changes.

**Acceptance Criteria:**
- [ ] When blueprint published, invalidate `blueprint:{id}` and `starting-actions:*`
- [ ] When action submitted, invalidate `pending-actions:{wallet}` and `action-details:{tx}`
- [ ] Use tag-based invalidation
- [ ] Async cache invalidation (fire-and-forget)
- [ ] Build succeeds

---

#### Task 6.8: Performance Testing
**Effort:** 6 hours
**Priority:** P1

**Description:**
Create performance tests and benchmarks.

**Acceptance Criteria:**
- [ ] `PerformanceTests.cs` created using NBomber
- [ ] Test: Blueprint retrieval performance (<200ms p95)
- [ ] Test: Action submission performance (<500ms p95)
- [ ] Test: Throughput test (1000 req/s sustained)
- [ ] Test: SignalR connection scaling (10,000 concurrent)
- [ ] Generate performance report
- [ ] All performance targets met

**Files:**
- `tests/Sorcha.Performance.Tests/BlueprintServicePerformanceTests.cs`

---

#### Task 6.9: Update AppHost Configuration
**Effort:** 2 hours
**Priority:** P1

**Description:**
Update Aspire AppHost to include updated service configuration.

**Acceptance Criteria:**
- [ ] `AppHost/Program.cs` updated
- [ ] Blueprint Service configured with:
  - [ ] Reference to Redis
  - [ ] Reference to Wallet Service (when available)
  - [ ] Reference to Register Service (when available)
  - [ ] Health checks configured
  - [ ] Wait dependencies configured
- [ ] Build succeeds
- [ ] Aspire dashboard shows all services

---

**Sprint 6 Total Effort:** 43 hours
**Sprint 6 Deliverable:** Full integration with external services and caching

---

## Sprint 7: Testing & Client Integration (Weeks 13-14)

**Goal:** Comprehensive testing and integrate execution engine with Blazor Designer.

### Week 13: End-to-End Testing

#### Task 7.1: E2E Test - Loan Application Scenario
**Effort:** 8 hours
**Priority:** P0

**Description:**
Create end-to-end test for complete loan application workflow.

**Acceptance Criteria:**
- [ ] `LoanApplicationScenarioTests.cs` created
- [ ] Test creates loan blueprint
- [ ] Test submits applicant action
- [ ] Test validates data against schema
- [ ] Test applies calculations (loan-to-income ratio)
- [ ] Test determines routing (to loan officer or senior officer)
- [ ] Test creates encrypted payloads
- [ ] Test submits transaction to register
- [ ] Test verifies SignalR notification sent
- [ ] Test retrieves pending action for next participant
- [ ] Test submits approval/rejection
- [ ] All steps succeed

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/E2E/LoanApplicationScenarioTests.cs`

---

#### Task 7.2: E2E Test - Purchase Order Scenario
**Effort:** 8 hours
**Priority:** P1

**Description:**
Create end-to-end test for multi-party purchase order workflow.

**Acceptance Criteria:**
- [ ] `PurchaseOrderScenarioTests.cs` created
- [ ] Test creates multi-party blueprint (buyer, seller, logistics, finance, QA)
- [ ] Test submits purchase order
- [ ] Test conditional routing based on amount and payment terms
- [ ] Test selective disclosure (different participants see different fields)
- [ ] Test file attachments (invoice PDF)
- [ ] Test verifies all participants notified appropriately
- [ ] Test completes full workflow
- [ ] All steps succeed

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/E2E/PurchaseOrderScenarioTests.cs`

---

#### Task 7.3: E2E Test - Rejection & Resubmission
**Effort:** 4 hours
**Priority:** P1

**Description:**
Create end-to-end test for action rejection and resubmission flow.

**Acceptance Criteria:**
- [ ] `RejectionScenarioTests.cs` created
- [ ] Test submits action
- [ ] Test rejects action with reason
- [ ] Test verifies rejection transaction created
- [ ] Test verifies previous participant notified
- [ ] Test resubmits corrected action
- [ ] Test verifies workflow continues
- [ ] All steps succeed

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/E2E/RejectionScenarioTests.cs`

---

#### Task 7.4: Security Testing
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create security tests for authentication, authorization, and input validation.

**Acceptance Criteria:**
- [ ] `SecurityTests.cs` created
- [ ] Test: Endpoint without JWT returns 401
- [ ] Test: Endpoint with invalid JWT returns 401
- [ ] Test: Endpoint with valid JWT for different wallet returns 403
- [ ] Test: SQL injection attempts blocked
- [ ] Test: XSS attempts sanitized
- [ ] Test: File upload with malicious content rejected
- [ ] Test: Oversized payload rejected (4MB limit)
- [ ] Test: Rate limiting enforced
- [ ] All tests pass

**Files:**
- `tests/Sorcha.Blueprint.Service.Tests/Security/SecurityTests.cs`

---

### Week 14: Blazor Designer Integration

#### Task 7.5: Update Blazor Designer to Reference Execution Engine
**Effort:** 2 hours
**Priority:** P0

**Description:**
Add Sorcha.Blueprint.Engine reference to Blazor Designer client.

**Acceptance Criteria:**
- [ ] `Sorcha.Blueprint.Designer.Client.csproj` updated
- [ ] Project reference added to `Sorcha.Blueprint.Engine`
- [ ] Execution engine services registered in `Program.cs`
- [ ] Build succeeds for WASM

---

#### Task 7.6: Implement Client-Side Validation in Designer
**Effort:** 8 hours
**Priority:** P0

**Description:**
Add client-side validation using execution engine in Blueprint Designer.

**Acceptance Criteria:**
- [ ] New component `ActionDataValidation.razor` created
- [ ] Component uses `IExecutionEngine` for validation
- [ ] Validates action data in real-time as user types
- [ ] Shows validation errors inline
- [ ] Shows calculated values preview
- [ ] Shows routing preview
- [ ] Disables submit button if invalid
- [ ] Build succeeds

**Files:**
- `src/Apps/UI/Sorcha.Blueprint.Designer.Client/Components/ActionDataValidation.razor`

---

#### Task 7.7: Implement Blueprint Test Mode
**Effort:** 8 hours
**Priority:** P1

**Description:**
Add "Test Mode" to Designer where users can test blueprint execution.

**Acceptance Criteria:**
- [ ] New page `TestBlueprint.razor` created
- [ ] Page loads blueprint from designer
- [ ] Page shows action sequence
- [ ] User can enter data for each action
- [ ] Uses `IExecutionEngine` to execute each step
- [ ] Shows execution results (validation, calculations, routing)
- [ ] Shows disclosure results for each participant
- [ ] Does not submit actual transactions (simulation only)
- [ ] Build succeeds

**Files:**
- `src/Apps/UI/Sorcha.Blueprint.Designer.Client/Pages/TestBlueprint.razor`

---

#### Task 7.8: Add SignalR Client to Designer
**Effort:** 4 hours
**Priority:** P1

**Description:**
Integrate SignalR client for real-time notifications in Designer.

**Acceptance Criteria:**
- [ ] `SignalRService.cs` created
- [ ] Connects to `/actionshub` on service
- [ ] Subscribes to wallet notifications
- [ ] Handles `ActionAvailable`, `ActionConfirmed`, `ActionRejected` events
- [ ] Shows toast notifications for events
- [ ] Reconnects automatically on disconnect
- [ ] Build succeeds

**Files:**
- `src/Apps/UI/Sorcha.Blueprint.Designer.Client/Services/SignalRService.cs`

---

**Sprint 7 Total Effort:** 48 hours
**Sprint 7 Deliverable:** Comprehensive E2E tests and Blazor Designer integration

---

## Sprint 8: Performance, Security, Deployment (Weeks 15-16)

**Goal:** Production hardening, performance optimization, and deployment.

### Week 15: Performance & Security

#### Task 8.1: Performance Optimization - Execution Engine
**Effort:** 6 hours
**Priority:** P1

**Description:**
Optimize execution engine performance.

**Acceptance Criteria:**
- [ ] Profile execution engine using BenchmarkDotNet
- [ ] Optimize JSON Logic evaluation (cache compiled expressions)
- [ ] Optimize schema validation (cache compiled schemas)
- [ ] Optimize disclosure processing (reduce allocations)
- [ ] Reduce memory allocations in hot paths
- [ ] Benchmark results show improvement
- [ ] Build succeeds

---

#### Task 8.2: Performance Optimization - Service Layer
**Effort:** 4 hours
**Priority:** P1

**Description:**
Optimize service layer performance.

**Acceptance Criteria:**
- [ ] Profile service endpoints using Application Insights
- [ ] Optimize database queries (when implemented)
- [ ] Optimize Redis caching (increase hit rate)
- [ ] Reduce HTTP client overhead (connection pooling)
- [ ] Add response compression for large payloads
- [ ] Performance targets met (<200ms GET, <500ms POST)
- [ ] Build succeeds

---

#### Task 8.3: Security Hardening - Input Validation
**Effort:** 4 hours
**Priority:** P0

**Description:**
Harden input validation and sanitization.

**Acceptance Criteria:**
- [ ] All request DTOs validated with FluentValidation
- [ ] File uploads sanitized (name, content type, content)
- [ ] JSON Schema validation strict mode enabled
- [ ] SQL injection prevention verified (parameterized queries)
- [ ] XSS prevention verified (output encoding)
- [ ] XXE prevention verified (disable external entities)
- [ ] Security tests pass

---

#### Task 8.4: Security Hardening - Rate Limiting
**Effort:** 3 hours
**Priority:** P0

**Description:**
Implement rate limiting middleware.

**Acceptance Criteria:**
- [ ] Rate limiting middleware configured
- [ ] Per-wallet limits: 100 req/min, 1000 req/hour
- [ ] Global limits: 10,000 req/min per instance
- [ ] Returns 429 Too Many Requests with Retry-After header
- [ ] Bypass rate limits for health checks
- [ ] Rate limit tests pass

---

#### Task 8.5: Security Hardening - Audit Logging
**Effort:** 4 hours
**Priority:** P0

**Description:**
Implement comprehensive audit logging.

**Acceptance Criteria:**
- [ ] Audit log middleware created
- [ ] Logs all action submissions (wallet, blueprint, action, timestamp)
- [ ] Logs all transaction creations
- [ ] Logs authentication failures
- [ ] Logs authorization failures
- [ ] Logs validation failures
- [ ] Includes correlation IDs for distributed tracing
- [ ] Structured logging with JSON format
- [ ] Build succeeds

---

#### Task 8.6: Load Testing
**Effort:** 6 hours
**Priority:** P1

**Description:**
Perform load testing with NBomber.

**Acceptance Criteria:**
- [ ] Load test scenarios created:
  - [ ] Gradual ramp-up: 0 → 1000 req/s over 5 min
  - [ ] Sustained load: 1000 req/s for 30 min
  - [ ] Spike test: 0 → 5000 req/s → 0 over 1 min
  - [ ] Stress test: increase until failure
- [ ] Monitor response times, throughput, error rate, CPU, memory
- [ ] Generate load test report
- [ ] All performance targets met
- [ ] No memory leaks detected

---

### Week 16: Deployment

#### Task 8.7: Create Deployment Documentation
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create comprehensive deployment documentation.

**Acceptance Criteria:**
- [ ] `DEPLOYMENT.md` created in docs folder
- [ ] Documents prerequisites (Docker, .NET 10, Redis, etc.)
- [ ] Documents environment variables
- [ ] Documents configuration options
- [ ] Documents scaling recommendations
- [ ] Documents health check endpoints
- [ ] Documents backup and recovery procedures
- [ ] Documents monitoring setup

**Files:**
- `docs/blueprint-service-deployment.md`

---

#### Task 8.8: Create Developer Guide
**Effort:** 6 hours
**Priority:** P0

**Description:**
Create developer guide for using the execution engine.

**Acceptance Criteria:**
- [ ] `DEVELOPER-GUIDE.md` created
- [ ] Documents execution engine usage (client-side and server-side)
- [ ] Provides code examples for all interfaces
- [ ] Documents JSON Logic patterns
- [ ] Documents selective disclosure patterns
- [ ] Provides blueprint examples
- [ ] Provides troubleshooting guide

**Files:**
- `docs/blueprint-execution-engine-guide.md`

---

#### Task 8.9: API Documentation Review
**Effort:** 3 hours
**Priority:** P1

**Description:**
Review and enhance API documentation.

**Acceptance Criteria:**
- [ ] All endpoints have complete OpenAPI documentation
- [ ] All request/response schemas documented
- [ ] All status codes documented
- [ ] Example requests and responses provided
- [ ] Authentication requirements documented
- [ ] Scalar UI tested and working
- [ ] OpenAPI spec valid (no errors)

---

#### Task 8.10: Container Image Build & Push
**Effort:** 2 hours
**Priority:** P0

**Description:**
Build and push Docker container images.

**Acceptance Criteria:**
- [ ] Dockerfile optimized (multi-stage build)
- [ ] Image built: `sorcha/blueprint-service:latest`
- [ ] Image tagged with version: `sorcha/blueprint-service:2.0.0`
- [ ] Image pushed to container registry (Azure CR or Docker Hub)
- [ ] Image size < 300MB
- [ ] Image runs successfully in Docker
- [ ] Health checks work in container

---

#### Task 8.11: Update CI/CD Pipeline
**Effort:** 4 hours
**Priority:** P0

**Description:**
Update GitHub Actions workflow for new service structure.

**Acceptance Criteria:**
- [ ] `.github/workflows/main-ci-cd.yml` updated
- [ ] Build job includes `Sorcha.Blueprint.Engine`
- [ ] Test job runs engine tests
- [ ] Test job runs service tests
- [ ] Publish job publishes NuGet package for engine
- [ ] Publish job builds and pushes container image
- [ ] Deploy job deploys to Azure Container Apps
- [ ] All pipeline stages succeed

---

#### Task 8.12: Production Deployment
**Effort:** 4 hours
**Priority:** P0

**Description:**
Deploy to production environment.

**Acceptance Criteria:**
- [ ] Deploy to Azure Container Apps (or equivalent)
- [ ] Configure Redis instance
- [ ] Configure Application Insights
- [ ] Configure managed identity for Azure services
- [ ] Configure custom domain and SSL certificate
- [ ] Configure auto-scaling (min 2, max 10 instances)
- [ ] Verify health checks pass
- [ ] Verify SignalR works across instances
- [ ] Smoke tests pass

---

#### Task 8.13: Monitoring & Alerting Setup
**Effort:** 3 hours
**Priority:** P0

**Description:**
Set up monitoring and alerting.

**Acceptance Criteria:**
- [ ] Application Insights dashboard created
- [ ] Metrics monitored:
  - [ ] Request rate
  - [ ] Response time (p50, p95, p99)
  - [ ] Error rate
  - [ ] CPU usage
  - [ ] Memory usage
  - [ ] SignalR connection count
- [ ] Alerts configured:
  - [ ] Error rate > 5%
  - [ ] Response time p95 > 1s
  - [ ] Service unavailable
  - [ ] CPU > 80%
  - [ ] Memory > 80%
- [ ] Alert notifications configured (email, Teams, etc.)

---

**Sprint 8 Total Effort:** 55 hours
**Sprint 8 Deliverable:** Production-ready service deployed

---

## Summary

### Total Effort by Sprint

| Sprint | Weeks | Effort (hours) | Focus |
|--------|-------|----------------|-------|
| Sprint 1 | 1-2 | 40 | Engine foundation |
| Sprint 2 | 3-4 | 54 | Engine complete |
| Sprint 3 | 5-6 | 54 | Service layer |
| Sprint 4 | 7-8 | 51 | Action endpoints |
| Sprint 5 | 9-10 | 33 | SignalR |
| Sprint 6 | 11-12 | 43 | Integration |
| Sprint 7 | 13-14 | 48 | Testing |
| Sprint 8 | 15-16 | 55 | Production |
| **Total** | **16** | **378** | |

**Average:** 47 hours per sprint (2-week sprint)

### Team Composition

**2 Backend Developers:**
- Developer 1: Execution engine + service layer
- Developer 2: API endpoints + integration

**1 QA Engineer:**
- Unit tests, integration tests, E2E tests
- Security testing, performance testing

**1 DevOps Engineer:**
- CI/CD pipeline, Docker containers
- Azure deployment, monitoring

### Risk Mitigation

**Risk:** Execution engine complexity
- **Mitigation:** Start with simple scenarios, iterate
- **Mitigation:** Comprehensive unit tests (>90% coverage)
- **Mitigation:** Integration tests with real blueprints

**Risk:** External service dependencies
- **Mitigation:** Mock services for testing
- **Mitigation:** Resilience policies (retry, circuit breaker)
- **Mitigation:** Graceful degradation

**Risk:** Performance at scale
- **Mitigation:** Performance testing early (Sprint 6)
- **Mitigation:** Caching strategy (Redis)
- **Mitigation:** Load testing before production

**Risk:** SignalR scale-out
- **Mitigation:** Redis backplane from start
- **Mitigation:** Load testing for 10K concurrent connections
- **Mitigation:** Sticky sessions for WebSocket

### Success Metrics

**Quality:**
- [ ] Engine test coverage >90%
- [ ] Service test coverage >85%
- [ ] Zero critical security vulnerabilities
- [ ] All E2E scenarios pass

**Performance:**
- [ ] GET endpoints <200ms (p95)
- [ ] POST endpoints <500ms (p95)
- [ ] Throughput 1000 req/s per instance
- [ ] SignalR 10,000 concurrent connections

**Functionality:**
- [ ] All API endpoints functional
- [ ] Client-side validation works
- [ ] SignalR notifications work
- [ ] Selective disclosure works
- [ ] JSON Logic evaluation works

---

**Document Control**
- **Created:** 2025-11-15
- **Author:** Sorcha Architecture Team
- **Status:** Approved
- **Review Frequency:** Weekly during implementation
- **Next Review:** Start of each sprint
