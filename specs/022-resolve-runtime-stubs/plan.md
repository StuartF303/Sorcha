# Implementation Plan: Resolve Runtime Stubs and Production-Critical TODOs

**Branch**: `022-resolve-runtime-stubs` | **Date**: 2026-02-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/022-resolve-runtime-stubs/spec.md`

## Summary

Eliminate all 5 `NotImplementedException` runtime stubs and resolve ~36 production-critical TODO comments across the Sorcha platform. Work is organized into 7 independently deliverable groups: (A) runtime stub elimination, (B) wallet authorization and repository, (C) peer node metrics, (D) data persistence migration, (E) validator-peer integration, (F) crypto operations, and (G) transaction versioning. Each group can be implemented, tested, and merged independently.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: .NET Aspire 13, gRPC (Grpc.Net 2.71), Redis (StackExchange.Redis), MongoDB, NBitcoin, Sorcha.Cryptography, FluentValidation
**Storage**: Redis (persistence, caching), MongoDB (system register, documents), PostgreSQL (wallet, tenant)
**Testing**: xUnit + FluentAssertions + Moq | Current baseline: 595 Validator, 148 Register Core, 88 Fluent, 323 Engine
**Target Platform**: Linux containers / Docker / .NET Aspire orchestration
**Project Type**: Microservices (7 services, 39 source projects, 30 test projects)
**Performance Goals**: Standard web API latency (<500ms p95 for all operations)
**Constraints**: Zero `NotImplementedException` at runtime, >85% test coverage on new code
**Scale/Scope**: 7 implementation groups, ~36 TODO resolutions, ~25 files modified

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | All changes are within service boundaries; no new cross-service coupling introduced |
| II. Security First | PASS | Adding authorization checks (FR-004/005) improves security posture; AES-256-GCM for keychain export |
| III. API Documentation | PASS | Modified endpoints will update OpenAPI docs via XML comments |
| IV. Testing Requirements | PASS | Each group targets >85% coverage; existing test suites preserved |
| V. Code Quality | PASS | Async/await, DI patterns, nullable reference types maintained |
| VI. Blueprint Creation Standards | N/A | No blueprint changes |
| VII. Domain-Driven Design | PASS | Using ubiquitous language (validators, dockets, registers) |
| VIII. Observability by Default | PASS | Structured logging already in place for all modified services |

**Gate result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/022-resolve-runtime-stubs/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Entity definitions and state transitions
├── quickstart.md        # Implementation guide and ordering
├── contracts/
│   └── api-changes.md   # Behavioral changes to existing APIs
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Cryptography/
│   │   ├── Core/CryptoModule.cs                    # Group F: Key recovery
│   │   └── Models/KeyChain.cs                      # Group F: Export/import
│   ├── Sorcha.TransactionHandler/
│   │   ├── Core/Transaction.cs                     # Group A/G: Binary stub → NotSupportedException
│   │   ├── Serialization/JsonTransactionSerializer.cs  # Group A/G: Same
│   │   └── Versioning/TransactionFactory.cs        # Group G: V1/V2/V3 adapters
│   └── Sorcha.Wallet.Core/
│       ├── Repositories/Interfaces/IWalletRepository.cs  # Group B: Add GetAccessByIdAsync
│       ├── Repositories/Implementation/InMemoryWalletRepository.cs  # Group B: Implement new method
│       ├── Repositories/EfCoreWalletRepository.cs  # Group B: Implement new method
│       └── Services/Implementation/
│           ├── WalletManager.cs                    # Group A: Address generation stub
│           └── DelegationService.cs                # Group A: UpdateAccessAsync stub
├── Services/
│   ├── Sorcha.Peer.Service/
│   │   ├── Services/HeartbeatService.cs            # Group C: Real metrics
│   │   ├── Services/HubNodeConnectionService.cs    # Group C: Real metrics
│   │   ├── Monitoring/HeartbeatMonitorService.cs   # Group C: Session ID
│   │   └── Replication/PeriodicSyncService.cs      # Group C: Session ID
│   ├── Sorcha.Register.Service/
│   │   ├── Services/PendingRegistrationStore.cs    # Group D: Redis migration
│   │   ├── Services/IPendingRegistrationStore.cs   # Group D: Interface unchanged
│   │   └── Program.cs                             # Group D: DI registration update
│   ├── Sorcha.Tenant.Service/
│   │   └── Endpoints/BootstrapEndpoints.cs         # Group B: Token generation
│   ├── Sorcha.Validator.Service/
│   │   ├── Services/SignatureCollector.cs           # Group E: Real gRPC calls
│   │   ├── Services/RotatingLeaderElectionService.cs  # Group E: Heartbeat broadcasting
│   │   ├── Services/ValidatorRegistry.cs           # Group E: On-chain registration
│   │   ├── Services/ValidationEngineService.cs     # Group E: Register discovery
│   │   ├── Services/ConsensusFailureHandler.cs     # Group E: Failure persistence
│   │   ├── Services/DocketBuildTriggerService.cs   # Group E: Consensus trigger
│   │   ├── Services/ValidatorOrchestrator.cs       # Group D: MemPool persistence
│   │   └── GrpcServices/ValidatorGrpcService.cs    # Group C: ActiveRegisters count
│   └── Sorcha.Wallet.Service/
│       └── Endpoints/
│           ├── WalletEndpoints.cs                  # Group B: Auth checks, JWT extraction
│           └── DelegationEndpoints.cs              # Group B: JWT extraction

tests/
├── Sorcha.Wallet.Core.Tests/                       # Groups A, B, F
├── Sorcha.Wallet.Service.Tests/                    # Group B
├── Sorcha.Validator.Service.Tests/                 # Groups D, E
├── Sorcha.Register.Core.Tests/                     # Group D
├── Sorcha.Peer.Service.Tests/                      # Group C
├── Sorcha.Cryptography.Tests/                      # Group F
└── Sorcha.TransactionHandler.Tests/                # Groups A, G
```

**Structure Decision**: No new projects introduced. All changes fit within existing project boundaries following the established microservices architecture.

## Complexity Tracking

No constitution violations to justify. All changes are within existing service boundaries using established patterns (Redis Hash operations, gRPC client calls, repository methods, JWT claim extraction).

## Implementation Groups (Phase 2 — for /speckit.tasks)

### Group A: Runtime Stub Elimination (P1, ~4 hours)
- Replace `NotImplementedException` in WalletManager.GenerateNewAddressAsync with structured 400 error
- Implement `DelegationService.UpdateAccessAsync` using new repository method
- Replace binary serialization stubs with `NotSupportedException`

### Group B: Wallet Auth & Repository (P1, ~8 hours)
- Add `GetAccessByIdAsync` to IWalletRepository + both implementations
- Add ownership/delegation checks to WalletEndpoints.GetWallet
- Fix JWT claim extraction (return 401 instead of "anonymous")
- Implement bootstrap token generation via ITokenService

### Group C: Peer Node Metrics (P2, ~4 hours)
- Inject SystemRegisterCache into HeartbeatService, HubNodeConnectionService
- Add uptime Stopwatch tracking
- Wire real session IDs from connection manager
- Update ValidatorGrpcService.ActiveRegisters count

### Group D: Data Persistence (P2, ~6 hours)
- Rewrite PendingRegistrationStore with Redis backend
- Add memory pool persistence option to ValidatorOrchestrator
- Update DI registration in Register Service Program.cs

### Group E: Validator-Peer Integration (P2, ~12 hours)
- Wire SignatureCollector → gRPC RequestVote calls
- Wire RotatingLeaderElectionService → peer heartbeat broadcasting
- Implement on-chain validator registration in ValidatorRegistry
- Wire ValidationEngineService → RegisterMonitoringRegistry for discovery
- Implement ConsensusFailureHandler persistence to Redis
- Wire DocketBuildTriggerService → consensus process

### Group F: Crypto Operations (P3, ~8 hours)
- Implement CryptoModule.RecoverKeySetAsync using NBitcoin mnemonic recovery
- Implement KeyChain.ExportAsync with AES-256-GCM + PBKDF2
- Implement KeyChain.ImportAsync with decryption and integrity check

### Group G: Transaction Versioning (P3, ~6 hours)
- Implement V1, V2, V3 adapters in TransactionFactory
- Replace binary serialization NotImplementedException → NotSupportedException
- Add version detection and adapter dispatch logic

## Post-Design Constitution Re-Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new cross-service dependencies; all changes within service boundaries |
| II. Security First | PASS | Authorization added to wallet endpoints; AES-256-GCM for keychain; no secrets in code |
| III. API Documentation | PASS | Modified endpoints documented in contracts/api-changes.md |
| IV. Testing Requirements | PASS | Each group specifies test requirements; baseline preserved |
| V. Code Quality | PASS | All patterns match existing codebase conventions |
| VI. Blueprint Creation Standards | N/A | No blueprint changes |
| VII. Domain-Driven Design | PASS | Consistent ubiquitous language throughout |
| VIII. Observability by Default | PASS | Structured logging maintained in all modified services |

**Post-design gate**: PASS
