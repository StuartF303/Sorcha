# Implementation Plan: Fix Register Creation Pipeline

**Branch**: `026-fix-register-creation-pipeline` | **Date**: 2026-02-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/026-fix-register-creation-pipeline/spec.md`

## Summary

Fix 8 issues in the register creation pipeline where genesis transaction payloads are lost during docket writes, genesis docket creation fails silently without retry, the advertise flag is hardcoded to false, and the Peer Service is never notified of new registers. The fix implements correct transaction mapping with full payload data, retry-aware genesis docket creation (max 3 attempts), advertise flag threading from initiation through finalization, Peer Service advertisement integration, a validator monitoring query endpoint, and Register Service test suite restoration.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: .NET Aspire 13, gRPC (Grpc.Net 2.71), Redis (StackExchange.Redis), MongoDB, Sorcha.Cryptography, FluentValidation
**Storage**: Redis (pending registrations, monitoring registry), MongoDB (registers, transactions, dockets)
**Testing**: xUnit + FluentAssertions + Moq | Baselines: Validator 210 pass/0 fail, Register Service 0 pass/26 compilation errors
**Target Platform**: Linux containers / Docker / .NET Aspire orchestration
**Project Type**: Microservices (7 services, 39 source projects, 30 test projects)
**Performance Goals**: Standard API latency (<500ms p95); genesis docket created within one 10-second trigger cycle
**Constraints**: Zero payload data loss, max 3 genesis retry attempts, fire-and-forget peer notification
**Scale/Scope**: 4 implementation phases, ~15 files modified, 2 new endpoints, 1 new constants file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Changes respect service boundaries; Peer Service notification via HTTP client, not direct dependency |
| II. Security First | PASS | No security degradation; genesis control record preservation improves auditability |
| III. Service Communication | NOTE | Peer advertisement uses REST (not gRPC) for simplicity. Acceptable per constitution: "External client-facing APIs MAY use REST/HTTP". The Peer Service endpoint is internal but follows existing `IPeerServiceClient` HTTP pattern |
| IV. API Documentation | PASS | New endpoints will include OpenAPI documentation via XML comments and Scalar |
| V. Testing Requirements | PASS | Existing Validator tests preserved; Register Service tests restored from 0→all passing |
| VI. Code Quality | PASS | Async/await, DI patterns, structured logging maintained |
| VII. Blueprint Creation Standards | N/A | No blueprint changes |
| VIII. Observability | PASS | Structured logging for retry attempts, advertisement failures, and monitoring queries |

**Gate result**: PASS — one note on REST vs gRPC for Peer Service, justified by consistency with existing service client pattern.

## Project Structure

### Documentation (this feature)

```text
specs/026-fix-register-creation-pipeline/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings (R1-R7)
├── data-model.md        # Entity changes and state transitions
├── quickstart.md        # Implementation guide and ordering
├── contracts/
│   └── api-changes.md   # New and modified API contracts
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Register.Models/
│   │   ├── RegisterCreationModels.cs          # Phase 2: Add Advertise to request/pending
│   │   └── Constants/
│   │       └── GenesisConstants.cs            # Phase 1: NEW — BlueprintId + ActionId constants
│   └── Sorcha.ServiceClients/
│       └── Peer/
│           ├── IPeerServiceClient.cs          # Phase 2: Add AdvertiseRegisterAsync
│           └── PeerServiceClient.cs           # Phase 2: Implement AdvertiseRegisterAsync
├── Services/
│   ├── Sorcha.Validator.Service/
│   │   ├── Services/
│   │   │   ├── DocketBuildTriggerService.cs   # Phase 1: Fix payload mapping + retry logic
│   │   │   └── GenesisManager.cs              # Phase 1: Remove catch-all
│   │   └── Endpoints/
│   │       ├── ValidationEndpoints.cs         # Phase 1: Use GenesisConstants
│   │       └── AdminEndpoints.cs              # Phase 3: Add monitoring endpoint
│   ├── Sorcha.Register.Service/
│   │   ├── Services/
│   │   │   └── RegisterCreationOrchestrator.cs # Phase 2: Thread advertise flag, inject IPeerServiceClient
│   │   └── Program.cs                         # Phase 2: Fire-and-forget on PUT advertise change
│   └── Sorcha.Peer.Service/
│       └── Endpoints/                         # Phase 2: Add POST /api/registers/{id}/advertise

tests/
└── Sorcha.Register.Service.Tests/
    ├── SignalRHubTests.cs                     # Phase 4: Task → ValueTask
    ├── Unit/
    │   ├── RegisterCreationOrchestratorTests.cs # Phase 4: Constructor + model fixes
    │   └── MongoSystemRegisterRepositoryTests.cs # Phase 4: Namespace + constructor fixes
    └── QueryApiTests.cs                       # Phase 4: Expression tree fix
```

**Structure Decision**: All changes are within existing service boundaries. One new file (`GenesisConstants.cs`) in the shared models project. One new endpoint each in the Peer Service and Validator Service. No new projects.

## Complexity Tracking

No constitution violations requiring justification. All changes are within existing patterns and service boundaries.
