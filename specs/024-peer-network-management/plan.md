# Implementation Plan: Peer Network Management & Observability

**Branch**: `024-peer-network-management` | **Date**: 2026-02-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/024-peer-network-management/spec.md`

## Summary

Add comprehensive peer network inspection, register subscription management, and peer reputation tracking across three surfaces: Peer Service REST API (new management endpoints), Sorcha CLI (new subcommands), and Sorcha UI (enhanced PeerServiceAdmin Blazor component). Changes include adding ban/unban persistence to PeerNode, exposing ConnectionQualityTracker data, wiring RegisterSyncBackgroundService subscribe/unsubscribe to REST endpoints, and building matching CLI commands and UI panels.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: ASP.NET Minimal APIs, MudBlazor, System.CommandLine 2.0.2, Refit, Scalar.AspNetCore
**Storage**: PostgreSQL via EF Core (peer schema — PeerDbContext), in-memory ConcurrentDictionary caches
**Testing**: xUnit + FluentAssertions + Moq (unit), Playwright (E2E)
**Target Platform**: Linux/Windows server (services), Blazor WASM (UI), cross-platform CLI
**Project Type**: Distributed microservices + web frontend + CLI
**Performance Goals**: Dashboard loads in <3s, 200+ peers without degradation
**Constraints**: All management endpoints require JWT auth, bans persisted to PostgreSQL
**Scale/Scope**: ~15 new REST endpoints, ~6 new CLI subcommands, 1 enhanced Blazor component with 4 panels

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Changes scoped to Peer Service, CLI, UI — no new cross-service dependencies |
| II. Security First | PASS | All management endpoints require JWT auth (FR-015), input validation on all boundaries |
| III. API Documentation | PASS | All new endpoints get OpenAPI docs via Scalar, XML documentation on public APIs |
| IV. Testing Requirements | PASS | Unit tests for all new service methods, CLI commands, UI component |
| V. Code Quality | PASS | Async/await, DI, nullable enabled, no warnings |
| VI. Blueprint Standards | N/A | No blueprint changes |
| VII. Domain-Driven Design | PASS | Uses existing Sorcha ubiquitous language (Register, Peer, Subscription) |
| VIII. Observability | PASS | Existing PeerServiceMetrics/ActivitySource extended for ban/subscribe operations |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/024-peer-network-management/
├── plan.md              # This file
├── research.md          # Phase 0: gap analysis and design decisions
├── data-model.md        # Phase 1: entity changes and new fields
├── quickstart.md        # Phase 1: developer quickstart
├── contracts/           # Phase 1: REST API contracts
│   └── peer-management-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/Services/Sorcha.Peer.Service/
├── Core/
│   └── PeerNode.cs                    # MODIFY: add IsBanned, BannedAt, BanReason
├── Discovery/
│   └── PeerListManager.cs             # MODIFY: add BanPeerAsync, UnbanPeerAsync, ResetFailureCountAsync
├── Monitoring/
│   └── ConnectionQualityTracker.cs    # READ-ONLY (existing data exposed via new endpoints)
├── Replication/
│   ├── RegisterAdvertisementService.cs  # MODIFY: add GetNetworkAdvertisedRegisters()
│   └── RegisterSyncBackgroundService.cs # READ-ONLY (subscribe/unsubscribe already exist)
├── data/
│   └── PeerDbContext.cs               # MODIFY: add IsBanned/BannedAt/BanReason to entity
└── Program.cs                         # MODIFY: add ~10 new REST endpoints

src/Apps/Sorcha.Cli/
├── Commands/
│   └── PeerCommands.cs                # MODIFY: add subscriptions, subscribe, unsubscribe, quality, ban, reset subcommands
├── Services/
│   └── IPeerServiceClient.cs          # MODIFY: add Refit methods for new endpoints
└── Models/
    └── Peer.cs                        # MODIFY: add new DTOs (subscription, quality, ban)

src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Components/Admin/
│   └── PeerServiceAdmin.razor         # MODIFY: enhance with 4 panels (peers, subscriptions, available registers, reputation)
└── Models/Admin/
    └── HealthResponse.cs              # MODIFY: add new response models

tests/Sorcha.Peer.Service.Tests/
├── Discovery/
│   └── PeerListManagerTests.cs        # MODIFY: add ban/unban/reset tests
└── Endpoints/
    └── PeerManagementEndpointTests.cs # NEW: test all management endpoints
```

**Structure Decision**: Extends existing projects (Peer Service, CLI, UI Core). No new projects needed. All changes are additive within established directory structures.
