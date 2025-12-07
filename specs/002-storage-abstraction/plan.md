# Implementation Plan: Multi-Tier Storage Abstraction Layer

**Branch**: `002-storage-abstraction` | **Date**: 2025-12-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-storage-abstraction/spec.md`

## Summary

Implement a unified multi-tier storage abstraction layer for the Sorcha platform with three storage tiers (Hot/Warm/Cold), pluggable providers (Redis, PostgreSQL, MongoDB, In-Memory), and a specialized verified cache for the Register Service that cryptographically validates all ledger data before use.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: StackExchange.Redis, Npgsql/EF Core, MongoDB.Driver, OpenTelemetry
**Storage**: Redis (Hot), PostgreSQL (Warm-Relational), MongoDB (Warm-Documents, Cold-WORM)
**Testing**: xUnit, FluentAssertions, Testcontainers, Moq
**Target Platform**: Linux containers, Windows Server, Cloud (Azure/AWS)
**Project Type**: Microservices with shared libraries
**Performance Goals**: Hot <10ms p99, Warm <100ms p99, 10K dockets verified in <60s on startup
**Constraints**: Graceful degradation on cache failure, zero data loss for warm/cold tiers
**Scale/Scope**: Support existing 5 services (Tenant, Wallet, Blueprint, Register, Peer)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| I. Microservices-First | **PASS** | Storage abstractions in shared libraries, services remain independent |
| II. Security First | **PASS** | Cryptographic verification for Register, no secrets in code |
| III. API Documentation | **PASS** | All interfaces will have XML documentation |
| IV. Testing Requirements | **PASS** | Unit tests for abstractions, integration tests with Testcontainers |
| V. Code Quality | **PASS** | Async/await, DI, nullable enabled, .NET 10 |
| VI. Blueprint Standards | **N/A** | Not creating blueprints |
| VII. Domain-Driven Design | **PASS** | Using domain terminology (Docket, Register, etc.) |
| VIII. Observability by Default | **PASS** | OpenTelemetry integration per FR-060-064 |

**Gate Status**: PASS - No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/002-storage-abstraction/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (interfaces)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Storage.Abstractions/     # NEW: Core interfaces (ICacheStore, IRepository, etc.)
│   │   ├── Interfaces/
│   │   │   ├── ICacheStore.cs           # Hot tier interface
│   │   │   ├── IRepository.cs           # Warm tier relational interface
│   │   │   ├── IDocumentStore.cs        # Warm tier document interface
│   │   │   └── IWormStore.cs            # Cold tier WORM interface
│   │   ├── Models/
│   │   │   ├── StorageConfiguration.cs
│   │   │   ├── PagedResult.cs
│   │   │   └── StorageHealthStatus.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── Sorcha.Storage.InMemory/         # NEW: In-memory implementations (dev/test)
│   │   ├── InMemoryCacheStore.cs
│   │   ├── InMemoryRepository.cs
│   │   ├── InMemoryDocumentStore.cs
│   │   └── InMemoryWormStore.cs
│   │
│   ├── Sorcha.Storage.Redis/            # NEW: Redis hot tier implementation
│   │   ├── RedisCacheStore.cs
│   │   └── RedisServiceExtensions.cs
│   │
│   ├── Sorcha.Storage.EFCore/           # NEW: EF Core warm tier implementation
│   │   ├── EFCoreRepository.cs
│   │   └── EFCoreServiceExtensions.cs
│   │
│   ├── Sorcha.Storage.MongoDB/          # NEW: MongoDB implementation
│   │   ├── MongoDocumentStore.cs
│   │   ├── MongoWormStore.cs
│   │   └── MongoServiceExtensions.cs
│   │
│   └── [existing libraries...]
│
├── Core/
│   ├── Sorcha.Register.Core/
│   │   └── Storage/
│   │       ├── IVerifiedRegisterCache.cs      # NEW: Verified cache interface
│   │       ├── VerifiedRegisterCache.cs       # NEW: Implementation
│   │       ├── RegisterOperationalState.cs    # NEW: State management
│   │       ├── CorruptionRange.cs             # NEW: Corruption tracking
│   │       └── CacheInitializationResult.cs   # NEW: Startup result
│   │
│   └── [existing core libraries...]
│
└── Services/
    ├── Sorcha.Register.Service/         # MODIFY: Integrate verified cache
    ├── Sorcha.Tenant.Service/           # MODIFY: Enable EF Core storage
    ├── Sorcha.Wallet.Service/           # MODIFY: Add cache + warm storage
    ├── Sorcha.Blueprint.Service/        # MODIFY: Add document storage
    └── [other services...]

tests/
├── Sorcha.Storage.Abstractions.Tests/   # NEW: Unit tests for abstractions
├── Sorcha.Storage.InMemory.Tests/       # NEW: In-memory implementation tests
├── Sorcha.Storage.Redis.Tests/          # NEW: Redis integration tests
├── Sorcha.Storage.EFCore.Tests/         # NEW: EF Core integration tests
├── Sorcha.Storage.MongoDB.Tests/        # NEW: MongoDB integration tests
├── Sorcha.Register.Core.Tests/          # MODIFY: Add verified cache tests
└── [existing test projects...]
```

**Structure Decision**: Following existing Sorcha conventions with `src/Common/` for shared libraries, `src/Core/` for domain logic, and `src/Services/` for microservices. New storage libraries follow the `Sorcha.Storage.*` naming pattern.

## Complexity Tracking

> No violations to justify - all gates pass.

## Phase 0: Research Summary

### Research Tasks

1. **Redis Best Practices**: Connection pooling, serialization, key naming conventions for .NET
2. **EF Core Multi-tenancy**: Schema-based isolation patterns (already used in Tenant Service)
3. **MongoDB WORM Patterns**: Enforcing append-only semantics, preventing updates
4. **OpenTelemetry Storage**: Instrumentation patterns for storage operations
5. **Graceful Degradation**: Circuit breaker patterns for cache failures

### Key Decisions (to be detailed in research.md)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Redis Client | StackExchange.Redis | Industry standard, .NET Aspire native support |
| EF Core Provider | Npgsql | PostgreSQL is project standard per constitution |
| MongoDB Driver | MongoDB.Driver | Official driver, mature ecosystem |
| Serialization | System.Text.Json | .NET native, high performance |
| Circuit Breaker | Polly | Standard resilience library, integrates with .NET |

## Phase 1: Design Artifacts

### Interfaces to Define (contracts/)

1. `ICacheStore` - Hot tier operations
2. `IRepository<TEntity, TId>` - Warm tier relational CRUD
3. `IDocumentStore<TDocument, TId>` - Warm tier document storage
4. `IWormStore<TDocument, TId>` - Cold tier append-only storage
5. `IVerifiedRegisterCache` - Register-specific verified cache

### Data Model Entities (data-model.md)

1. `StorageConfiguration` - Provider configuration
2. `CacheEntry<T>` - Hot tier entry with TTL
3. `PagedResult<T>` - Pagination wrapper
4. `VerifiedDocket` - Verified docket in cache
5. `RegisterOperationalState` - Register health states
6. `CorruptionRange` - Corruption tracking
7. `CacheInitializationResult` - Startup results

### Service Integration Points

| Service | Hot Tier | Warm Tier | Cold Tier | Notes |
|---------|----------|-----------|-----------|-------|
| Tenant | Redis (JWKS, sessions) | PostgreSQL (EF Core) | - | Multi-schema |
| Wallet | Redis (nonce tracking) | PostgreSQL (EF Core) | - | Sensitive data |
| Blueprint | Redis (published cache) | MongoDB (documents) | - | Complex JSON |
| Register | - | - | MongoDB (WORM) + Verified Cache | Cryptographic verification |
| Peer | Redis (peer status) | - | - | Ephemeral |

## Implementation Order

### Phase 1: Core Abstractions (P0)
1. Create `Sorcha.Storage.Abstractions` with interfaces
2. Create `Sorcha.Storage.InMemory` implementations
3. Unit tests for abstractions

### Phase 2: Register Verified Cache (P0)
1. Implement `IVerifiedRegisterCache` interface
2. Implement verification logic using `Sorcha.Cryptography`
3. Implement `RegisterOperationalState` management
4. Integration tests with mock storage

### Phase 3: Provider Implementations (P1)
1. Redis implementation for hot tier
2. EF Core implementation for warm tier
3. MongoDB implementation for warm/cold tiers
4. Integration tests with Testcontainers

### Phase 4: Service Integration (P1-P2)
1. Register Service integration
2. Tenant Service integration
3. Wallet Service integration
4. Blueprint Service integration

### Phase 5: Observability (P2)
1. OpenTelemetry instrumentation
2. Health checks
3. Metrics dashboards

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Performance regression | Benchmark before/after, maintain in-memory option |
| Data corruption | Cryptographic verification, comprehensive tests |
| Provider lock-in | Abstract interfaces, multiple implementations |
| Startup latency | Configurable progressive loading |
| Cache failures | Circuit breaker, graceful degradation |

## Next Steps

After this plan is approved:
1. Run `/speckit.tasks` to generate detailed task breakdown
2. Begin implementation with Phase 1 (Core Abstractions)
3. Iterate through phases with incremental testing
