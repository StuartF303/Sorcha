# Implementation Plan: Fix Transaction Submission Pipeline

**Branch**: `028-fix-transaction-pipeline` | **Date**: 2026-02-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/028-fix-transaction-pipeline/spec.md`

## Summary

Action transactions from blueprint execution bypass the Validator Service entirely, being written directly to the Register database without validation or docket sealing. This plan wires them through the existing validation endpoint (`POST /api/v1/transactions/validate`), mempool staging, and docket build pipeline — mirroring the genesis transaction path. After submitting to the Validator, the Blueprint Service polls for confirmation before returning success, ensuring sequential action execution with confirmed state.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Sorcha.ServiceClients, Sorcha.Blueprint.Service, Sorcha.Validator.Service, Sorcha.Register.Service
**Storage**: MongoDB (Register), Redis (Validator mempool)
**Testing**: xUnit + FluentAssertions + Moq
**Target Platform**: Docker (linux/amd64) and .NET Aspire
**Project Type**: Distributed microservices (cross-service pipeline fix)
**Performance Goals**: Action execution completes within 2x docket build threshold (~20s with 10s default)
**Constraints**: No new services, no new databases. Reuse existing Validator endpoint.
**Scale/Scope**: 4 services touched, ~10 files modified, ~5 new test files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Each service remains independently deployable. Blueprint submits to Validator via HTTP client. No new coupling direction — follows existing service-to-service pattern. |
| II. Security First | PASS | Transactions are validated (structure, signature, payload hash) before mempool admission. No unvalidated data persists. |
| III. API Documentation | PASS | Existing endpoint reused. New client method will have XML docs. |
| IV. Testing Requirements | PASS | Unit tests for new client method, updated integration tests for ActionExecutionService, walkthrough scripts serve as E2E tests. |
| V. Code Quality | PASS | Async/await throughout, DI for new client dependency, nullable types enabled. |
| VI. Blueprint Standards | N/A | No blueprint format changes. |
| VII. Domain-Driven Design | PASS | Uses "Docket" terminology consistently (post-rename). |
| VIII. Observability | PASS | Existing ActivitySource tracing in ActionExecutionService. New log messages for Validator submission path. |

**Post-Phase 1 re-check**: PASS — No violations introduced. Existing Validator endpoint reused (no new API surface). `BuiltTransaction.ToValidateTransactionRequest()` is a simple mapper following existing `ToTransactionModel()` pattern.

## Project Structure

### Documentation (this feature)

```text
specs/028-fix-transaction-pipeline/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research output
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
├── contracts/           # Phase 1 API contracts
│   └── validator-transaction-api.md
└── checklists/
    └── requirements.md
```

### Source Code (files modified)

```text
src/Common/Sorcha.ServiceClients/
├── Validator/
│   ├── IValidatorServiceClient.cs          # Add SubmitTransactionAsync + models
│   └── ValidatorServiceClient.cs           # Implement HTTP POST to /validate

src/Services/Sorcha.Blueprint.Service/
├── Services/
│   ├── Interfaces/ITransactionBuilderService.cs  # Add ToValidateTransactionRequest()
│   └── Implementation/ActionExecutionService.cs  # Route to Validator, add polling

src/Services/Sorcha.Validator.Service/
├── Endpoints/ValidationEndpoints.cs        # Add RegisterForMonitoring after mempool add

src/Services/Sorcha.Register.Service/
├── Program.cs                              # Restrict direct TX endpoint, add events to docket write

tests/
├── Sorcha.ServiceClients.Tests/            # SubmitTransactionAsync tests
├── Sorcha.Blueprint.Service.Tests/         # Updated ActionExecutionService tests
└── Sorcha.Validator.Service.Tests/         # Monitoring registration tests
```

**Structure Decision**: No new projects. All changes are modifications to existing source files across 4 service/library projects and 3 test projects.

## Complexity Tracking

No constitution violations. No new projects, abstractions, or patterns introduced beyond what already exists.
