# Implementation Plan: Unified Transaction Submission & System Wallet Signing Service

**Branch**: `036-unified-transaction-submission` | **Date**: 2026-02-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/036-unified-transaction-submission/spec.md`

## Summary

Unify all transaction submission through a single generic validator endpoint (`POST /api/v1/transactions/validate`). Create a secure `ISystemWalletSigningService` in `Sorcha.ServiceClients` with audit logging, operation whitelist, and rate limiting. Migrate all callers (register creation, blueprint publish) from the legacy genesis endpoint to the generic endpoint. Remove the legacy genesis endpoint and associated models.

**Critical discovery during research:** Blueprint Service Program.cs has two endpoints that write transactions directly to the register, bypassing the validator entirely. These are flagged for audit but are out of scope for this feature (they require Blueprint Service changes).

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Sorcha.ServiceClients, Sorcha.Cryptography, Sorcha.Validator.Core
**Storage**: N/A (no new persistence — audit via structured logging, rate limits in-memory)
**Testing**: xUnit + FluentAssertions + Moq (existing test infrastructure)
**Target Platform**: Linux containers (Docker)
**Project Type**: Microservices (existing architecture)
**Performance Goals**: System signing completes within 500ms including wallet service round-trip
**Constraints**: Rate limit: configurable max signs per register per minute (default 10)
**Scale/Scope**: 4 files modified in ServiceClients, 3 files modified in Validator Service, 2 files modified in Register Service, ~20 new test files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | ISystemWalletSigningService in shared ServiceClients library, no cross-service coupling added |
| II. Security First | PASS | Operation whitelist, rate limiting, audit logging, least-privilege DI registration |
| III. API Documentation | PASS | Existing endpoint unchanged; no new public API surface (internal service contract) |
| IV. Testing Requirements | PASS | Unit tests for signing service, integration tests for submission flow |
| V. Code Quality | PASS | async/await, DI, nullable enabled, no warnings |
| VI. Blueprint Standards | N/A | No blueprint changes |
| VII. Domain-Driven Design | PASS | Uses Sorcha terminology (Transaction, Register, Docket) |
| VIII. Observability | PASS | Structured audit logging for every sign operation |

**Post-Phase 1 Re-check**: All gates still pass. The signing service adds no new cross-service dependencies beyond the existing IWalletServiceClient.

## Project Structure

### Documentation (this feature)

```text
specs/036-unified-transaction-submission/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Entity definitions and state transitions
├── quickstart.md        # Build/test/deploy guide
├── contracts/           # API and service contracts
│   ├── system-wallet-signing-service.md
│   └── unified-submission-endpoint.md
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/Common/Sorcha.ServiceClients/
├── SystemWallet/                              # NEW — signing service
│   ├── ISystemWalletSigningService.cs         # Interface
│   ├── SystemWalletSigningService.cs          # Implementation
│   ├── SystemSignResult.cs                    # Result model
│   ├── SystemWalletSigningOptions.cs          # Configuration
│   └── SystemWalletSigningExtensions.cs       # DI registration (AddSystemWalletSigning)
└── Validator/
    ├── IValidatorServiceClient.cs             # MODIFIED — deprecate genesis method
    └── ValidatorServiceClient.cs              # MODIFIED — remove genesis method

src/Services/Sorcha.Validator.Service/
├── Endpoints/
│   └── ValidationEndpoints.cs                 # MODIFIED — remove genesis endpoint
└── Services/
    └── ValidationEngine.cs                    # MODIFIED — remove signature skip

src/Services/Sorcha.Register.Service/
├── Program.cs                                 # MODIFIED — blueprint publish uses generic endpoint
└── Services/
    └── RegisterCreationOrchestrator.cs        # MODIFIED — uses signing service + generic endpoint

tests/Sorcha.ServiceClients.Tests/
└── SystemWallet/                              # NEW — signing service tests
    └── SystemWalletSigningServiceTests.cs

tests/Sorcha.Validator.Service.Tests/
└── Endpoints/                                 # MODIFIED — update submission tests
    └── ValidationEndpointsTests.cs

tests/Sorcha.Register.Core.Tests/              # MODIFIED — update orchestrator tests
```

**Structure Decision**: No new projects. All changes are within existing projects following established patterns. The `SystemWallet/` folder in ServiceClients follows the same pattern as `Validator/`, `Wallet/`, `Peer/`, etc.

## Complexity Tracking

No constitution violations to justify.
