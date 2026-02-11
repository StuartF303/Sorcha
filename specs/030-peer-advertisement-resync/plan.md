# Implementation Plan: Register-to-Peer Advertisement Resync

**Branch**: `030-peer-advertisement-resync` | **Date**: 2026-02-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/030-peer-advertisement-resync/spec.md`

## Summary

Fix the confirmed bug where public registers are invisible in the Peer Service after a Docker restart. The solution has three pillars:

1. **Redis persistence** — Replace the Peer Service's volatile in-memory `ConcurrentDictionary` with a unified Redis-backed store (5-minute TTL) for both local and remote advertisements, with an in-memory cache layer.
2. **Register Service startup push** — Add a background service to the Register Service that pushes all `advertise: true` registers to the Peer Service on startup (with retry/backoff).
3. **Periodic reconciliation** — The Register Service periodically pushes its full advertisement state to the Peer Service via a bulk/full-sync endpoint, ensuring drift self-heals within 5 minutes.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: Aspire.StackExchange.Redis, StackExchange.Redis, Sorcha.ServiceClients
**Storage**: Redis (unified advertisement pool with 5-min TTL), MongoDB (Register Service ground truth)
**Testing**: xUnit v3 + FluentAssertions v8.8.0 + Moq v4.20.72
**Target Platform**: Linux containers (Docker), .NET Aspire orchestration
**Project Type**: Microservices (existing multi-project solution)
**Performance Goals**: 5s Peer Service restart recovery (SC-002), 10s for 100 registers bulk push (SC-004), <100ms reconciliation overhead (SC-005)
**Constraints**: Redis already configured in Aspire AppHost; Peer Service has no Redis package yet; Register Service has no background services yet
**Scale/Scope**: ~100 registers typical, 2 services modified (Peer + Register), 1 client extended (PeerServiceClient)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Register Service pushes to Peer Service (no upward dependency). Peer Service receives via HTTP endpoint. Services remain independently deployable. |
| II. Security First | PASS | Bulk endpoint uses existing service-to-service auth pattern. No secrets stored. Redis data is reconstructable (non-sensitive advertisement metadata). |
| III. API Documentation | PASS | New bulk endpoint will have OpenAPI docs via Scalar (.WithName, .WithSummary, .WithDescription). |
| IV. Testing Requirements | PASS | >85% coverage target for new code. Unit tests for Redis store, background service, bulk endpoint. Integration tests for resync flow. |
| V. Code Quality | PASS | Async/await for all Redis I/O. DI for all dependencies. Nullable reference types enabled. |
| VI. Blueprint Creation Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Uses existing domain terms: Register, Advertisement, Peer. No new domain concepts introduced. |
| VIII. Observability by Default | PASS | Structured logging for all resync operations. Health check integration for Redis connectivity. |

**Gate result: PASS** — No violations.

## Project Structure

### Documentation (this feature)

```text
specs/030-peer-advertisement-resync/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── bulk-advertise.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   └── Sorcha.ServiceClients/
│       └── Peer/
│           ├── IPeerServiceClient.cs          # Add BulkAdvertiseAsync method
│           └── PeerServiceClient.cs           # Implement BulkAdvertiseAsync
├── Services/
│   ├── Sorcha.Peer.Service/
│   │   ├── Program.cs                         # Add Redis DI, bulk endpoint
│   │   ├── Sorcha.Peer.Service.csproj         # Add Aspire.StackExchange.Redis
│   │   └── Replication/
│   │       ├── RegisterAdvertisementService.cs # Refactor to use Redis-backed store
│   │       └── RedisAdvertisementStore.cs      # NEW: Redis persistence layer
│   └── Sorcha.Register.Service/
│       ├── Program.cs                         # Register background service
│       └── Services/
│           └── AdvertisementResyncService.cs   # NEW: Background reconciliation

tests/
├── Sorcha.Peer.Service.Tests/
│   └── Replication/
│       ├── RegisterAdvertisementServiceTests.cs  # Update for Redis dependency
│       └── RedisAdvertisementStoreTests.cs       # NEW: Redis store tests
└── Sorcha.Register.Service.Tests/                # Existing test project
    └── Services/
        └── AdvertisementResyncServiceTests.cs    # NEW: Resync service tests
```

**Structure Decision**: Modifies 2 existing services and 1 shared client library. Creates 2 new classes (RedisAdvertisementStore, AdvertisementResyncService) and their corresponding test classes. No new projects — all changes fit within existing project boundaries.

## Complexity Tracking

No constitution violations to justify.
