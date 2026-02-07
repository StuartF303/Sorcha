# Quickstart: 022-resolve-runtime-stubs

**Branch**: `022-resolve-runtime-stubs`

## Overview

This feature resolves all runtime stubs (`NotImplementedException`) and production-critical TODOs across the Sorcha platform. It spans 7 independently deliverable groups touching Wallet, Validator, Peer, Register, Cryptography, and TransactionHandler components.

## Implementation Order

Work should proceed in this order (dependencies flow downward):

1. **Group A: Runtime Stub Elimination** (FR-001 to FR-003)
   - Replace 5 `NotImplementedException` with either real implementations or structured errors
   - Lowest risk, highest impact on stability
   - Touches: WalletManager, DelegationService, JsonTransactionSerializer, Transaction

2. **Group B: Wallet Auth & Repository** (FR-004 to FR-006)
   - Add `GetAccessByIdAsync` to `IWalletRepository`
   - Add authorization checks to wallet endpoints
   - Fix JWT claim extraction (remove "anonymous" fallback)
   - Implement bootstrap token generation
   - Touches: WalletEndpoints, DelegationEndpoints, BootstrapEndpoints, IWalletRepository

3. **Group C: Peer Node Metrics** (FR-013 to FR-015)
   - Replace hardcoded zeros with real values from SystemRegisterCache/repository
   - Add uptime tracking via Stopwatch
   - Wire session IDs from connection manager
   - Touches: HubNodeConnectionService, HeartbeatService, HeartbeatMonitorService, PeriodicSyncService

4. **Group D: Data Persistence** (FR-016 to FR-017)
   - Migrate PendingRegistrationStore from ConcurrentDictionary to Redis
   - Add memory pool persistence option to ValidatorOrchestrator
   - Touches: PendingRegistrationStore, ValidatorOrchestrator, Register Service Program.cs

5. **Group E: Validator-Peer Integration** (FR-007 to FR-012)
   - Wire SignatureCollector to real gRPC calls
   - Wire leader election heartbeat broadcasting
   - Implement on-chain validator registration
   - Add register discovery
   - Add consensus failure persistence
   - Trigger consensus from docket build
   - Touches: SignatureCollector, RotatingLeaderElectionService, ValidatorRegistry, ValidationEngineService, ConsensusFailureHandler, DocketBuildTriggerService

6. **Group F: Crypto Operations** (FR-018 to FR-020)
   - Implement RecoverKeySetAsync in CryptoModule
   - Implement KeyChain export/import with AES-256-GCM encryption
   - Touches: CryptoModule, KeyChain, WalletManager

7. **Group G: Transaction Versioning** (FR-021 to FR-022)
   - Implement V1/V2/V3 backward compatibility adapters
   - Replace binary serialization NotImplementedException with NotSupportedException
   - Touches: TransactionFactory, JsonTransactionSerializer, Transaction

## Testing Strategy

- Each group should maintain >85% test coverage
- Existing test suites must continue to pass (595 Validator, 148 Register Core)
- New tests follow pattern: `MethodName_Scenario_ExpectedBehavior`
- Integration tests for Groups D and E (Redis, gRPC)

## Key Files by Group

| Group | Primary Files |
|-------|--------------|
| A | WalletManager.cs, DelegationService.cs, JsonTransactionSerializer.cs, Transaction.cs |
| B | WalletEndpoints.cs, DelegationEndpoints.cs, BootstrapEndpoints.cs, IWalletRepository.cs |
| C | HubNodeConnectionService.cs, HeartbeatService.cs, HeartbeatMonitorService.cs, PeriodicSyncService.cs |
| D | PendingRegistrationStore.cs, ValidatorOrchestrator.cs, Register Service Program.cs |
| E | SignatureCollector.cs, RotatingLeaderElectionService.cs, ValidatorRegistry.cs, ValidationEngineService.cs, ConsensusFailureHandler.cs, DocketBuildTriggerService.cs |
| F | CryptoModule.cs, KeyChain.cs, WalletManager.cs |
| G | TransactionFactory.cs, JsonTransactionSerializer.cs, Transaction.cs |
