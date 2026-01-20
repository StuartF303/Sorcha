# Implementation Plan: Registers and Transactions UI

**Branch**: `012-registers-transactions-ui` | **Date**: 2026-01-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-registers-transactions-ui/spec.md`

## Summary

Build a Registers and Transactions viewing UI for Sorcha.UI that allows organization participants to browse available registers, view transactions with real-time updates, inspect transaction details, and (for administrators) create new registers via a guided wizard. The feature leverages existing Register Service APIs and SignalR infrastructure.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Blazor WebAssembly, MudBlazor, Microsoft.AspNetCore.SignalR.Client
**Storage**: Backend APIs (Register Service via API Gateway)
**Testing**: xUnit, bUnit, Playwright (E2E)
**Target Platform**: Web (Blazor WASM)
**Project Type**: Web (existing Sorcha.UI application)
**Performance Goals**: 2s page load, 1s real-time updates, 500ms transaction detail display
**Constraints**: Must work through API Gateway, existing auth system
**Scale/Scope**: Support registers with 100,000+ transactions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ PASS | UI consumes existing Register Service APIs via Gateway |
| II. Security First | ✅ PASS | Uses existing JWT auth, role-based access control |
| III. API Documentation | ✅ PASS | Register Service already has Scalar docs |
| IV. Testing Requirements | ✅ PASS | Plan includes unit, integration, E2E tests |
| V. Code Quality | ✅ PASS | Follows existing Blazor patterns |
| VI. Blueprint Creation Standards | N/A | Not creating blueprints |
| VII. Domain-Driven Design | ✅ PASS | Uses correct terminology (Register, Transaction) |
| VIII. Observability by Default | ✅ PASS | Follows existing telemetry patterns |

**Gate Result**: PASS - All applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/012-registers-transactions-ui/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Research findings
├── data-model.md        # UI data models/DTOs
├── quickstart.md        # Testing guide
├── contracts/           # API contracts (reference existing)
│   └── api-reference.md # Links to Register Service APIs
└── tasks.md             # Implementation tasks
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/
│   ├── Components/
│   │   └── Registers/                    # NEW: Register components
│   │       ├── RegisterCard.razor        # Register list item display
│   │       ├── RegisterStatusBadge.razor # Status indicator (Online/Offline/etc)
│   │       ├── TransactionList.razor     # Transaction list with virtual scroll
│   │       ├── TransactionRow.razor      # Single transaction display
│   │       ├── TransactionDetail.razor   # Full transaction details panel
│   │       ├── CreateRegisterWizard.razor# Multi-step register creation
│   │       └── RealTimeIndicator.razor   # SignalR connection status
│   └── Services/
│       ├── RegisterService.cs            # NEW: API client for registers
│       ├── TransactionService.cs         # NEW: API client for transactions
│       └── RegisterHubConnection.cs      # NEW: SignalR connection manager
│
└── Sorcha.UI.Web.Client/
    └── Pages/
        └── Registers/                    # NEW: Register pages
            ├── Index.razor               # /registers - List all registers
            └── Detail.razor              # /registers/{id} - Register detail with transactions

tests/
├── Sorcha.UI.Core.Tests/
│   └── Components/
│       └── Registers/                    # NEW: Component unit tests
│           ├── RegisterCardTests.cs
│           ├── TransactionListTests.cs
│           └── TransactionDetailTests.cs
│
└── Sorcha.UI.E2E.Tests/
    └── Registers/                        # NEW: E2E tests
        ├── RegisterListTests.cs
        ├── TransactionViewTests.cs
        └── RegisterCreationTests.cs
```

**Structure Decision**: Follows existing Sorcha.UI patterns - shared components in `Sorcha.UI.Core`, pages in `Sorcha.UI.Web.Client`. Navigation already configured at `/registers` route.

## Complexity Tracking

> No violations to justify - implementation follows existing patterns.

## Existing Infrastructure (No New Services Required)

### Register Service APIs (via API Gateway at `/api/register/`)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/registers` | GET | List all registers (filterable by tenantId) |
| `/api/registers/{id}` | GET | Get single register details |
| `/api/registers/initiate` | POST | Phase 1: Initiate register creation |
| `/api/registers/finalize` | POST | Phase 2: Finalize register creation |
| `/api/registers/{registerId}/transactions` | GET | Paginated transactions (newest first) |
| `/api/registers/{registerId}/transactions/{txId}` | GET | Single transaction details |

### SignalR Hub (`/hubs/register`)

| Method | Direction | Purpose |
|--------|-----------|---------|
| `SubscribeToRegister(registerId)` | Client→Server | Join register updates group |
| `UnsubscribeFromRegister(registerId)` | Client→Server | Leave register updates group |
| `TransactionConfirmed(registerId, txId)` | Server→Client | New transaction notification |
| `RegisterHeightUpdated(registerId, newHeight)` | Server→Client | Register height changed |
| `DocketSealed(registerId, docketId, hash)` | Server→Client | New block sealed |

## Implementation Approach

### Phase 1: Core Services (Foundation)
1. Create `RegisterService` - HTTP client wrapper for Register APIs
2. Create `TransactionService` - HTTP client for transaction operations
3. Create `RegisterHubConnection` - SignalR connection manager with reconnection logic

### Phase 2: Read-Only Components (P1, P2, P4)
1. `RegisterCard` - Display single register in list
2. `RegisterStatusBadge` - Visual status indicator
3. `TransactionRow` - Single transaction in list
4. `TransactionList` - Virtualized scrolling list
5. `TransactionDetail` - Full transaction info panel

### Phase 3: Pages & Navigation (P1, P2, P4)
1. `Registers/Index.razor` - Main registers list page
2. `Registers/Detail.razor` - Register detail with transaction list

### Phase 4: Real-Time Updates (P3)
1. `RealTimeIndicator` - Connection status display
2. Integrate SignalR into `TransactionList` for live updates
3. New transaction highlight animation
4. "New transactions available" notification when scrolled

### Phase 5: Register Creation (P5)
1. `CreateRegisterWizard` - Multi-step wizard component
2. Admin-only visibility check
3. Integration with initiate/finalize API flow

### Phase 6: Testing & Polish
1. bUnit component tests
2. E2E Playwright tests
3. Error states and loading indicators
4. Performance optimization for large lists
