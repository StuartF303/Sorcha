# Implementation Plan: Validator Service Wallet Access

**Branch**: `001-validator-service-wallet` | **Date**: 2026-01-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-validator-service-wallet/spec.md`

## Summary

Enable the Validator Service to authenticate with the Wallet Service using system organization credentials, retrieve and cache wallet details (wallet ID, address, algorithm), and use that wallet for cryptographic operations including docket signing, consensus vote signing, and signature verification. The integration uses gRPC for all Wallet Service communication, implements retry logic with exponential backoff (3 attempts, 2x multiplier, 1s initial delay), and allows local cryptographic operations using derived path private keys via the Sorcha.Cryptography library for performance optimization while never accessing the root private key.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**:
- Sorcha.Wallet.Service (gRPC client)
- Sorcha.Tenant.Service (system organization config)
- Sorcha.Cryptography (local crypto operations with derived keys)
- Grpc.Net.Client 2.71.0
- Polly (retry policies with exponential backoff)

**Storage**:
- No direct storage - caches wallet details in memory for service lifetime
- Configuration may be read from Tenant Service or environment variables

**Testing**:
- xUnit (unit tests for wallet integration logic)
- Integration tests with Testcontainers for Wallet Service interaction
- Mock gRPC clients for isolated testing

**Target Platform**:
- Linux/Windows server (containerized via .NET Aspire)
- Deployed alongside other Sorcha microservices

**Project Type**:
- Microservice enhancement (existing Sorcha.Validator.Service)
- gRPC client integration

**Performance Goals**:
- Wallet initialization < 5 seconds on startup
- Docket signing rate ≥ 10 dockets/second
- Signature verification < 100ms per vote
- Local crypto operations with derived keys to minimize Wallet Service calls

**Constraints**:
- MUST NOT access or store root private key (security requirement)
- MAY cache derived path private keys in memory only (never persisted)
- Network latency to Wallet Service with retry (3 attempts over 7 seconds max)
- Graceful shutdown on wallet deletion detection
- Seamless operation during wallet key rotation

**Scale/Scope**:
- Single validator instance processes 1 register at a time
- Multiple validator instances may use same wallet for different registers
- System organization (1) with validator wallet (1)
- Supports ED25519, NISTP256, RSA4096 algorithms

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Constitutional Compliance

| Principle | Compliance | Notes |
|-----------|-----------|-------|
| **I. Microservices-First Architecture** | ✅ PASS | Integration between existing Validator Service and Wallet Service. No new services. Dependencies flow correctly: Validator → Wallet, Validator → Tenant. |
| **II. Security First** | ✅ PASS | Zero trust model: wallet root private key never accessed. Derived keys may be retrieved for performance. All communication over gRPC with TLS. Environment variables for sensitive config. |
| **III. API Documentation** | ✅ PASS | New gRPC proto files will include comprehensive documentation. Using .NET 10 built-in OpenAPI for REST endpoints (if any). No Swagger/Swashbuckle. |
| **IV. Testing Requirements** | ✅ PASS | Target >85% coverage for new code. Unit tests for wallet integration, retry logic. Integration tests with Testcontainers for Wallet Service. |
| **V. Code Quality** | ✅ PASS | C# 13, .NET 10. Async/await for gRPC calls. Dependency injection for wallet clients. Nullable reference types enabled. |
| **VI. Blueprint Creation Standards** | ⚪ N/A | Not applicable - this feature is about wallet integration, not blueprint creation. |
| **VII. Domain-Driven Design** | ✅ PASS | Uses existing domain terminology: Docket, Consensus Vote, Validator. Wallet concepts align with Sorcha.Cryptography domain model. |
| **VIII. Observability by Default** | ✅ PASS | OpenTelemetry for wallet operations (initialization, signing, verification). Structured logging for all wallet interactions. Health checks for wallet connectivity. |

### Technology Stack Compliance

| Category | Required | Planned | Compliance |
|----------|----------|---------|-----------|
| Framework | .NET 10 | ✅ .NET 10 | ✅ PASS |
| gRPC | Grpc.Net 2.71.0 | ✅ Grpc.Net.Client 2.71.0 | ✅ PASS |
| Resilience | Polly | ✅ Polly (retry policies) | ✅ PASS |
| Telemetry | OpenTelemetry 1.12.0 | ✅ OpenTelemetry | ✅ PASS |
| Testing | xUnit | ✅ xUnit + Testcontainers | ✅ PASS |

### Development Workflow Compliance

✅ **Before Writing Code**:
- ✅ Read constitution and standards *(completed)*
- ✅ Review existing Validator Service patterns *(completed)*
- ✅ Verify architectural fit *(microservices integration - compliant)*
- ✅ Check dependencies *(Wallet Service gRPC, Tenant Service)*

✅ **During Development**:
- ⏳ Follow coding standards strictly *(to be enforced)*
- ⏳ Write tests alongside code (TDD) *(to be implemented)*
- ⏳ Document gRPC APIs with proto comments *(to be implemented)*
- ⏳ Use meaningful commit messages *(to be enforced)*

✅ **Before Committing**:
- ⏳ Run full test suite (`dotnet test`) *(to be run)*
- ⏳ Check code coverage (>85% target) *(to be verified)*
- ⏳ Run static analysis *(to be run)*
- ⏳ Verify no build warnings *(to be verified)*
- ⏳ Update documentation *(to be completed)*

### GATE RESULT: ✅ PASS

**No violations detected.** All constitutional principles are satisfied. Proceed to Phase 0 research.

## Project Structure

### Documentation (this feature)

```text
specs/001-validator-service-wallet/
├── spec.md                     # Feature specification (completed)
├── checklists/
│   └── requirements.md         # Quality checklist (completed - all passed)
├── plan.md                     # This file (in progress)
├── research.md                 # Phase 0 output (to be generated)
├── data-model.md               # Phase 1 output (to be generated)
├── quickstart.md               # Phase 1 output (to be generated)
├── contracts/                  # Phase 1 output (to be generated)
│   ├── wallet-service.proto    # gRPC proto for Wallet Service
│   └── tenant-service.proto    # gRPC proto for Tenant Service (if needed)
└── tasks.md                    # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

This feature enhances the existing **Sorcha.Validator.Service** microservice. The source structure follows the existing Sorcha multi-service architecture:

```text
src/
├── Services/
│   ├── Sorcha.Validator.Service/              # Target service for this feature
│   │   ├── Configuration/
│   │   │   ├── ConsensusConfiguration.cs
│   │   │   ├── DocketBuildConfiguration.cs
│   │   │   ├── MemPoolConfiguration.cs
│   │   │   ├── ValidatorConfiguration.cs
│   │   │   └── WalletConfiguration.cs         # NEW: Wallet integration config
│   │   ├── Endpoints/
│   │   │   ├── AdminEndpoints.cs
│   │   │   └── ValidationEndpoints.cs
│   │   ├── GrpcServices/
│   │   │   └── ValidatorGrpcService.cs        # Existing gRPC service (may need wallet signing)
│   │   ├── Managers/
│   │   │   └── DocketManager.cs
│   │   ├── Models/
│   │   │   ├── ConsensusResult.cs
│   │   │   ├── ConsensusVote.cs
│   │   │   ├── Docket.cs
│   │   │   ├── DocketStatus.cs
│   │   │   ├── Signature.cs
│   │   │   ├── Transaction.cs
│   │   │   ├── TransactionPriority.cs
│   │   │   ├── VoteDecision.cs
│   │   │   └── WalletDetails.cs               # NEW: Cached wallet info
│   │   ├── Services/
│   │   │   ├── ConsensusEngine.cs
│   │   │   ├── DocketBuilder.cs
│   │   │   ├── DocketBuildTriggerService.cs
│   │   │   ├── GenesisManager.cs
│   │   │   ├── IConsensusEngine.cs
│   │   │   ├── IDocketBuilder.cs
│   │   │   ├── IGenesisManager.cs
│   │   │   ├── IMemPoolManager.cs
│   │   │   ├── IValidatorOrchestrator.cs
│   │   │   ├── IWalletIntegrationService.cs   # NEW: Wallet operations interface
│   │   │   ├── MemPoolCleanupService.cs
│   │   │   ├── MemPoolManager.cs
│   │   │   ├── ValidatorOrchestrator.cs
│   │   │   └── WalletIntegrationService.cs    # NEW: Wallet operations implementation
│   │   ├── Protos/
│   │   │   └── wallet_service.proto           # NEW: Wallet Service gRPC contract
│   │   ├── Program.cs                         # MODIFY: Register wallet services
│   │   └── Sorcha.Validator.Service.csproj    # MODIFY: Add gRPC dependencies
│   │
│   ├── Sorcha.Wallet.Service/                 # May need gRPC endpoint additions
│   │   ├── Endpoints/
│   │   ├── Protos/
│   │   │   └── wallet_service.proto           # NEW: Define gRPC contract
│   │   └── GrpcServices/
│   │       └── WalletGrpcService.cs           # NEW: gRPC service implementation
│   │
│   └── Sorcha.Tenant.Service/                 # May need system org config APIs
│       ├── Endpoints/
│       └── Models/
│           └── OrganizationConfiguration.cs   # MODIFY: Add validator wallet field
│
└── Common/
    ├── Sorcha.Cryptography/                   # Used for local crypto operations
    │   └── [existing crypto library]
    └── Sorcha.ServiceClients/                 # Service client consolidation
        ├── Wallet/
        │   ├── IWalletServiceClient.cs        # MODIFY: Add gRPC methods
        │   └── WalletServiceClient.cs         # MODIFY: Implement gRPC client
        └── Tenant/
            ├── ITenantServiceClient.cs        # MODIFY: Add system org config method
            └── TenantServiceClient.cs         # MODIFY: Implement config retrieval

tests/
├── Sorcha.Validator.Service.Tests/            # NEW: Unit tests for wallet integration
│   ├── Services/
│   │   └── WalletIntegrationServiceTests.cs
│   └── Managers/
│       └── DocketManagerWalletTests.cs
└── Sorcha.Validator.Service.IntegrationTests/ # NEW: Integration tests
    └── WalletIntegrationTests.cs
```

**Structure Decision**: This feature enhances the existing **Sorcha.Validator.Service** microservice by adding wallet integration capabilities. It follows the established Sorcha microservices pattern:

1. **Configuration**: New `WalletConfiguration.cs` for wallet service endpoints and retry policies
2. **Models**: New `WalletDetails.cs` to cache wallet information (ID, address, algorithm)
3. **Services**: New `WalletIntegrationService.cs` implementing `IWalletIntegrationService` for all wallet operations
4. **Protos**: New `wallet_service.proto` defining the gRPC contract for Wallet Service communication
5. **Service Clients**: Extend existing `Sorcha.ServiceClients` with gRPC methods (per constitution - no duplicate clients)
6. **Testing**: Unit tests for wallet logic, integration tests with Testcontainers for Wallet Service interaction

The existing `DocketBuilder`, `ConsensusEngine`, and `GenesisManager` services will be modified to use `IWalletIntegrationService` for signing operations. The `Program.cs` will be updated to register the wallet integration service and configure gRPC clients.

## Complexity Tracking

> **No constitutional violations - this section intentionally left blank.**

All constitutional requirements are satisfied. No complexity justification needed.

---

## Next Steps

### Phase 0: Outline & Research *(next)*
Generate `research.md` to resolve:
- gRPC proto contract design for Wallet Service (signing, verification, derived key retrieval)
- Retry policy implementation with Polly (exponential backoff configuration)
- Wallet caching strategy (in-memory, thread-safe, rotation detection)
- System organization configuration retrieval from Tenant Service
- BIP32 derived key retrieval patterns
- Local cryptography patterns with Sorcha.Cryptography library

### Phase 1: Design & Contracts *(after research)*
Generate:
- `data-model.md` - WalletDetails, WalletConfiguration entities
- `contracts/wallet_service.proto` - gRPC service definition
- `quickstart.md` - Developer guide for wallet integration

### Phase 2: Tasks *(separate command)*
Use `/speckit.tasks` to generate actionable task breakdown after design is complete.

---

**Status**: Ready for Phase 0 research
