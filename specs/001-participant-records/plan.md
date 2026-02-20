# Implementation Plan: Published Participant Records on Register

**Branch**: `001-participant-records` | **Date**: 2026-02-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-participant-records/spec.md`

## Summary

Add a new `TransactionType.Participant` (value 3) for publishing participant identity records as immutable transactions on a register. Participant records contain a UUID identity anchor, organization name, participant name, multi-algorithm address array (with public keys), status lifecycle, and optional metadata. Publishing flows through the Tenant Service (authorization) → Validator Service (validation via mempool) → Register Service (storage + indexing). The Register Service provides query endpoints for participant lookup by wallet address, enabling downstream blueprint integration and field-level encryption.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: .NET Aspire 13, JsonSchema.Net 7.4, FluentValidation 11.10, Sorcha.Cryptography
**Storage**: MongoDB (per-register databases for transactions), Redis (participant address index cache), PostgreSQL (Tenant Service auth)
**Testing**: xUnit + FluentAssertions + Moq (target >85% coverage)
**Target Platform**: Linux containers (Docker), orchestrated by .NET Aspire
**Project Type**: Distributed microservices (existing solution — 38 source + 33 test projects)
**Performance Goals**: Participant publish within same time bounds as blueprint publish; address lookup in single query
**Constraints**: Follow existing canonical JSON serialization for hashing; no new project creation (extend existing services)
**Scale/Scope**: Hundreds of participants per register; 10 addresses per participant max

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new services — extends Tenant, Validator, Register services |
| II. Security First | PASS | Signature verification, schema validation, Tenant Service authorization |
| III. API Documentation | PASS | Scalar/OpenAPI docs for all new endpoints |
| IV. Testing Requirements | PASS | Target >85% for new code, xUnit + FluentAssertions + Moq |
| V. Code Quality | PASS | Async/await, DI, nullable reference types, C# 13 |
| VI. Blueprint Standards | N/A | No blueprint changes in Phase 1 |
| VII. Domain-Driven Design | PASS | Uses "Participant" (not "user"), "Publish" (not "deploy") per ubiquitous language |
| VIII. Observability | PASS | Structured logging, health checks inherited from existing services |

**Post-Phase 1 re-check**: All gates still pass. No new projects created. TransactionSubmission nullable field change is backwards-compatible.

## Project Structure

### Documentation (this feature)

```text
specs/001-participant-records/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: design decisions and rationale
├── data-model.md        # Phase 1: entity definitions and relationships
├── quickstart.md        # Phase 1: developer getting started guide
├── contracts/           # Phase 1: API contracts
│   ├── tenant-service-api.md
│   ├── register-service-api.md
│   └── service-client-api.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Register.Models/
│   │   ├── Enums/
│   │   │   ├── TransactionType.cs           # MODIFY: Add Participant = 3
│   │   │   └── ParticipantRecordStatus.cs   # NEW: active/deprecated/revoked enum
│   │   └── ParticipantRecord.cs             # NEW: Payload model + ParticipantAddress
│   └── Sorcha.ServiceClients/
│       ├── Validator/
│       │   └── IValidatorServiceClient.cs   # MODIFY: BlueprintId/ActionId nullable
│       └── Register/
│           ├── IRegisterServiceClient.cs    # MODIFY: Add participant query methods
│           └── RegisterServiceClient.cs     # MODIFY: Implement participant queries
├── Services/
│   ├── Sorcha.Validator.Service/
│   │   ├── Services/
│   │   │   └── ValidationEngine.cs          # MODIFY: Participant validation path
│   │   └── Schemas/
│   │       └── participant-record-v1.json   # NEW: Built-in JSON Schema
│   ├── Sorcha.Register.Service/
│   │   ├── Program.cs                       # MODIFY: Add participant query endpoints
│   │   └── Services/
│   │       └── ParticipantIndexService.cs   # NEW: Address index management
│   └── Sorcha.Tenant.Service/
│       ├── Services/
│       │   ├── IParticipantPublishingService.cs  # NEW: Publishing interface
│       │   └── ParticipantPublishingService.cs   # NEW: Build TX + sign + submit
│       └── Endpoints/
│           └── ParticipantEndpoints.cs      # MODIFY: Add publish endpoints

tests/
├── Sorcha.Register.Models.Tests/            # NEW: ParticipantRecord model tests
├── Sorcha.Validator.Service.Tests/
│   └── Services/
│       └── ValidationEngineTests.cs         # MODIFY: Participant validation tests
├── Sorcha.Register.Service.Tests/           # MODIFY: Participant query tests
├── Sorcha.Tenant.Service.Tests/             # MODIFY: Publishing service tests
└── Sorcha.ServiceClients.Tests/             # MODIFY: Client method tests
```

**Structure Decision**: Extends existing service folder structure. No new projects needed — all changes go into existing source and test projects. The `ParticipantIndexService` is the only new service class (in Register Service). The `ParticipantPublishingService` is new in Tenant Service. All other changes are modifications to existing files.

## Implementation Layers

### Layer 1: Shared Models (Foundation)
- `TransactionType.Participant` enum value
- `ParticipantRecord` and `ParticipantAddress` payload models
- `ParticipantRecordStatus` enum
- `TransactionSubmission` nullable fields

### Layer 2: Validator Service (Validation Rules)
- Built-in participant record JSON Schema
- Schema validation path for Participant TXs (skip blueprint, use built-in schema)
- Governance skip for Participant TXs
- Fork detection: allow first-publish from Control TX, block version-chain forks

### Layer 3: Register Service (Storage + Query)
- Participant query endpoints (list, by-address, by-id, public-key resolution)
- `ParticipantIndexService` for address indexing (in-memory + Redis cache)
- MongoDB index on `MetaData.TransactionType`
- OData pagination on participant list

### Layer 4: Tenant Service (Publishing)
- `IParticipantPublishingService` / `ParticipantPublishingService`
- Build transaction payload (canonical JSON, deterministic TxId, hash)
- Sign with user's wallet via `IWalletServiceClient`
- Submit via `IValidatorServiceClient`
- Publish endpoints (POST create, PUT update, DELETE revoke)

### Layer 5: Service Clients (Cross-Service Communication)
- `IRegisterServiceClient` participant query methods
- Response models (`PublishedParticipantRecord`, `ParticipantPage`, `PublicKeyResolution`)

## Key Design Decisions

| Decision | Choice | Rationale | Reference |
|----------|--------|-----------|-----------|
| Identity anchor | UUID (participantId) | Allows renaming across versions | research.md R6 |
| Chain linking | First from Control TX, updates from previous version | Leverages existing fork detection | research.md R2 |
| Authorization | Tenant Service enforces, validator trusts | Lighter than Control TX governance | spec.md Clarifications |
| Address indexing | Application-level + Redis cache | MongoDB can't index nested JSON payload arrays | research.md R4 |
| Schema validation | Built-in JSON Schema | No blueprint context for Participant TXs | research.md R7 |
| TransactionSubmission | Make BlueprintId/ActionId nullable | Avoids sentinel values, clean for non-blueprint TXs | research.md R1 |
| Max addresses | 10 per record | Matches existing LinkedWalletAddress limit | research.md R5 |

## Complexity Tracking

No constitution violations requiring justification. All changes extend existing patterns within existing projects.
