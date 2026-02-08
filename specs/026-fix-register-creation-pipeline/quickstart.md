# Quickstart: 026-fix-register-creation-pipeline

**Branch**: `026-fix-register-creation-pipeline`

## Overview

This feature fixes 8 issues in the register creation pipeline affecting payload persistence, docket creation reliability, peer advertisement, and validator monitoring. Work is organized into 4 phases by priority.

## Implementation Order

Work should proceed in this order (dependencies flow downward):

1. **Phase 1: Critical Pipeline Fixes** (FR-001, FR-002, FR-003, FR-009, FR-010)
   - Fix transaction mapping in `WriteDocketAndTransactionsAsync()` to include full payload data
   - Fix genesis write retry logic — only set `_genesisWritten` on success, add retry counter (max 3)
   - Remove catch-all in `GenesisManager.NeedsGenesisDocketAsync()` to propagate errors
   - Define `GenesisConstants.BlueprintId` and `GenesisConstants.ActionId` constants
   - Touches: DocketBuildTriggerService.cs, GenesisManager.cs, ValidationEndpoints.cs, GenesisConstants.cs (new)

2. **Phase 2: Advertise Flag & Peer Integration** (FR-004, FR-005, FR-006)
   - Add `Advertise` property to `InitiateRegisterCreationRequest` and `PendingRegistration`
   - Thread advertise flag through `RegisterCreationOrchestrator` (replace hardcoded `false`)
   - Add `AdvertiseRegisterAsync` to `IPeerServiceClient`
   - Add `POST /api/registers/{id}/advertise` endpoint to Peer Service
   - Add fire-and-forget notification in Register Service on creation and update
   - Touches: RegisterCreationModels.cs, RegisterCreationOrchestrator.cs, IPeerServiceClient.cs, PeerServiceClient.cs, Peer Service endpoints, Register Service Program.cs

3. **Phase 3: Validator Monitoring** (FR-007)
   - Add `GET /api/admin/validators/monitoring` endpoint to `AdminEndpoints.cs`
   - Returns monitored register IDs from `IRegisterMonitoringRegistry.GetAll()`
   - Touches: AdminEndpoints.cs

4. **Phase 4: Test Fixes** (FR-008)
   - Fix 26 compilation errors across 4 test files
   - `SignalRHubTests.cs`: Task → ValueTask for xUnit v3 IAsyncLifetime
   - `RegisterCreationOrchestratorTests.cs`: Add TransactionManager/IPendingRegistrationStore mocks, Creator → Owners
   - `MongoSystemRegisterRepositoryTests.cs`: Fix namespace, constructor, add publishedBy param
   - `QueryApiTests.cs`: Replace null-propagating in expression tree lambdas
   - Touches: 4 test files in tests/Sorcha.Register.Service.Tests/

## Testing Strategy

- Validator tests must continue passing (baseline: 210 pass / 0 fail)
- Register Service tests must compile and pass (baseline: 0 pass / 26 compilation errors → target: all pass)
- New tests for: transaction mapping, genesis retry logic, advertise flag threading, monitoring endpoint
- Test naming: `MethodName_Scenario_ExpectedBehavior`

## Key Files by Phase

| Phase | Primary Files |
|-------|--------------|
| 1 | DocketBuildTriggerService.cs, GenesisManager.cs, ValidationEndpoints.cs, GenesisConstants.cs |
| 2 | RegisterCreationModels.cs, RegisterCreationOrchestrator.cs, IPeerServiceClient.cs, PeerServiceClient.cs, Peer Service endpoints, Register Service Program.cs |
| 3 | AdminEndpoints.cs |
| 4 | SignalRHubTests.cs, RegisterCreationOrchestratorTests.cs, MongoSystemRegisterRepositoryTests.cs, QueryApiTests.cs |
