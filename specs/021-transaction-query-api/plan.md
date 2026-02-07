# Implementation Plan: Transaction Query API

**Branch**: `021-transaction-query-api` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/021-transaction-query-api/spec.md`

## Summary

Add a paginated query-by-PreviousTransactionId capability across the full Register Service stack — from MongoDB index through repository, business logic manager, REST endpoint, and service client interface. This enables the ValidationEngine's fork detection (deferred from 020-validator-engine-validation) by allowing queries that find all transactions claiming the same predecessor, which indicates a chain fork.

The implementation follows the established query pattern: `IRegisterRepository` → `QueryManager` → Minimal API endpoint → `IRegisterServiceClient`. Each layer already has analogous methods (e.g., GetTransactionsByWallet, GetTransactionsByBlueprint) that serve as templates.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: MongoDB.Driver, Sorcha.Register.Core, Sorcha.ServiceClients, FluentValidation
**Storage**: MongoDB (per-register databases) with InMemory for testing
**Testing**: xUnit + FluentAssertions + Moq (unit), Testcontainers + MongoDB 7.0 (integration)
**Target Platform**: Linux containers / .NET Aspire orchestration
**Project Type**: Distributed microservices (existing)
**Performance Goals**: <500ms query response for registers with 10,000 transactions
**Constraints**: Paginated results (page/pageSize), scoped to single register, no fork interpretation at query layer
**Scale/Scope**: Touches 8 files across 5 projects, adds ~6 new test classes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Changes span Register Service, Register Core, ServiceClients — all existing projects with correct dependency flow |
| II. Security First | PASS | Endpoint uses existing `CanReadTransactions` authorization policy; input validation on registerId and prevTxId |
| III. API Documentation | PASS | New endpoint gets XML docs, OpenAPI exposure via Scalar |
| IV. Testing Requirements | PASS | Unit tests for QueryManager, repository implementations; integration tests for MongoDB |
| V. Code Quality | PASS | Async/await throughout, DI, nullable enabled, no warnings |
| VI. Blueprint Standards | N/A | No blueprint changes |
| VII. Domain-Driven Design | PASS | Uses established domain terms (Transaction, Register) |
| VIII. Observability | PASS | Query layer is silent per clarification; structured logging at endpoint level for request tracing |

**Gate Result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/021-transaction-query-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── query-api.md     # REST endpoint contract
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Register.Models/
│   │   └── TransactionModel.cs                    # Existing (PrevTxId already present)
│   └── Sorcha.ServiceClients/
│       └── Register/
│           ├── IRegisterServiceClient.cs           # ADD: GetTransactionsByPrevTxIdAsync method
│           └── RegisterServiceClient.cs            # ADD: HTTP implementation
├── Core/
│   ├── Sorcha.Register.Core/
│   │   ├── Storage/
│   │   │   └── IRegisterRepository.cs             # ADD: GetTransactionsByPrevTxIdAsync method
│   │   └── Managers/
│   │       └── QueryManager.cs                    # ADD: GetTransactionsByPrevTxIdPaginatedAsync method
│   ├── Sorcha.Register.Storage.MongoDB/
│   │   └── MongoRegisterRepository.cs             # ADD: index on PrevTxId + query implementation
│   └── Sorcha.Register.Storage.InMemory/
│       └── InMemoryRegisterRepository.cs          # ADD: LINQ-based implementation
└── Services/
    ├── Sorcha.Register.Service/
    │   └── Program.cs                             # ADD: GET /api/query/previous/{txId}/transactions endpoint
    └── Sorcha.Validator.Service/
        └── Services/
            └── ValidationEngine.cs                # UPDATE: Wire fork detection using new query

tests/
├── Sorcha.Register.Core.Tests/
│   └── Managers/
│       └── QueryManagerTests.cs                   # ADD: PrevTxId query tests
├── Sorcha.Register.Storage.MongoDB.Tests/
│   └── MongoRegisterRepositoryIntegrationTests.cs # ADD: PrevTxId index + query tests
├── Sorcha.ServiceClients.Tests/                   # ADD: RegisterServiceClient PrevTxId tests
└── Sorcha.Validator.Service.Tests/
    └── Services/
        └── ValidationEngineTests.cs               # ADD: Fork detection tests
```

**Structure Decision**: Extends existing projects across the established layered architecture. No new projects required. Changes follow the identical pattern used by GetTransactionsByWallet and GetTransactionsByBlueprint.

## Complexity Tracking

> No constitution violations — no complexity justification needed.
