# Implementation Plan: UI Register Management

**Branch**: `017-ui-register-management` | **Date**: 2026-01-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/017-ui-register-management/spec.md`

## Summary

Enhance the existing Sorcha.UI Blazor WASM application to provide comprehensive register management functionality, mirroring the CLI capabilities. This includes enhancing the register list view with filtering and search, adding transaction detail drill-down with copy functionality, completing the two-phase register creation wizard with wallet integration, and implementing real-time updates via the existing SignalR infrastructure.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Blazor WASM, MudBlazor 8.x, SignalR Client
**Storage**: N/A (API clients only - backend handles storage)
**Testing**: xUnit + FluentAssertions + Playwright (E2E)
**Target Platform**: WebAssembly (modern browsers), responsive for tablet/desktop (read-only mobile)
**Project Type**: Web application (Blazor WASM client)
**Performance Goals**: Page loads < 2s, real-time updates < 5s latency
**Constraints**: Must work with existing SignalR hub, API Gateway endpoints, MudBlazor patterns
**Scale/Scope**: Support 50+ registers, paginated transactions, 3 new/enhanced pages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Microservices-First | ✅ Pass | UI client calls existing services via API Gateway |
| Service Communication | ✅ Pass | REST via API Gateway (external client), SignalR for real-time |
| Cloud-Native Design | ✅ Pass | Blazor WASM is containerized with existing infrastructure |
| Zero Trust Security | ✅ Pass | JWT authentication via existing auth flow |
| Code Quality | ✅ Pass | Will include E2E tests via Playwright |
| .NET Framework Standards | ✅ Pass | .NET 10, C# 13, dependency injection |
| API Documentation | N/A | UI client, not exposing APIs |
| Blueprint Creation | N/A | Not creating blueprints |
| Testing Principles | ✅ Pass | E2E tests with Playwright |
| Documentation Standards | ✅ Pass | Component documentation in code |

## Project Structure

### Documentation (this feature)

```text
specs/017-ui-register-management/
├── plan.md              # This file
├── research.md          # Phase 0: Existing patterns analysis
├── data-model.md        # Phase 1: ViewModel definitions
├── quickstart.md        # Phase 1: E2E test scenarios
├── contracts/           # Phase 1: API contracts (existing endpoints)
└── tasks.md             # Phase 2: Implementation tasks
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/
│   ├── Components/
│   │   └── Registers/
│   │       ├── CreateRegisterWizard.razor      # Enhanced: wallet selection step
│   │       ├── RegisterCard.razor              # Existing: no changes
│   │       ├── RegisterStatusBadge.razor       # Existing: no changes
│   │       ├── RegisterSearchBar.razor         # NEW: search/filter component
│   │       ├── TransactionList.razor           # Existing: no changes
│   │       ├── TransactionRow.razor            # Existing: no changes
│   │       ├── TransactionDetail.razor         # Enhanced: copy-to-clipboard
│   │       ├── TransactionQueryForm.razor      # NEW: wallet address search
│   │       └── RealTimeIndicator.razor         # Existing: no changes
│   ├── Models/
│   │   └── Registers/
│   │       ├── RegisterViewModel.cs            # Existing: no changes
│   │       ├── TransactionViewModel.cs         # Existing: no changes
│   │       ├── RegisterCreationState.cs        # Enhanced: wallet selection
│   │       ├── TransactionQueryState.cs        # NEW: query form state
│   │       └── RegisterFilterState.cs          # NEW: filter/search state
│   └── Services/
│       ├── IRegisterService.cs                 # Existing: no changes
│       ├── RegisterService.cs                  # Existing: no changes
│       ├── ITransactionService.cs              # Enhanced: query by wallet
│       ├── TransactionService.cs               # Enhanced: query by wallet
│       └── RegisterHubConnection.cs            # Existing: no changes
├── Sorcha.UI.Web/                              # Host - no changes
└── Sorcha.UI.Web.Client/
    └── Pages/
        └── Registers/
            ├── Index.razor                     # Enhanced: search/filter
            ├── Detail.razor                    # Enhanced: transaction detail panel
            └── Query.razor                     # NEW: cross-register query page

tests/Sorcha.UI.E2E.Tests/
└── Tests/
    └── Registers/
        ├── RegisterListTests.cs                # NEW: list/filter E2E tests
        ├── RegisterDetailTests.cs              # NEW: detail/transaction E2E tests
        ├── RegisterCreationTests.cs            # NEW: wizard E2E tests
        └── TransactionQueryTests.cs            # NEW: query E2E tests
```

**Structure Decision**: Enhances existing Sorcha.UI.Core component library and Sorcha.UI.Web.Client pages. Follows the established pattern of shared components in Core, pages in Web.Client, and E2E tests in the dedicated test project.

## Complexity Tracking

> No constitution violations - all changes enhance existing patterns.

| Enhancement | Justification |
|-------------|---------------|
| New RegisterSearchBar component | Keeps filter/search logic separate from list page |
| New TransactionQueryForm component | Cross-register query is distinct from single-register view |
| New Query.razor page | Transaction search across registers is a new user workflow |

## Implementation Phases

### Phase 0: Research (research.md)
- Document existing API endpoints used by UI
- Map existing component capabilities to spec requirements
- Identify gaps between current implementation and spec
- Document wallet service integration approach

### Phase 1: Design Artifacts
- **data-model.md**: Define new/enhanced ViewModels (FilterState, QueryState)
- **contracts/**: Document API endpoints used (existing + any needed)
- **quickstart.md**: Define E2E test scenarios for each user story

### Phase 2: Implementation (tasks.md)
Tasks organized by user story priority:

**P1 - User Story 1: Register List**
- Enhance Index.razor with search bar component
- Add status filtering dropdown
- Implement empty state messaging

**P1 - User Story 2: Register Details & Transactions**
- Verify transaction list pagination works correctly
- Add new transaction notification banner
- Implement transaction selection highlighting

**P2 - User Story 3: Transaction Details**
- Enhance TransactionDetail with copy-to-clipboard
- Add visual confirmation on copy
- Display all transaction fields per spec

**P2 - User Story 4: Create Register**
- Add wallet selection step to wizard
- Implement two-phase flow with progress indication
- Add error handling with retry option

**P3 - User Story 5: Filter & Search**
- Create RegisterSearchBar component
- Implement real-time search filtering
- Add status filter chips

**P3 - User Story 6: Transaction Query**
- Create Query.razor page
- Create TransactionQueryForm component
- Implement cross-register wallet search

### Phase 3: Testing & Polish
- E2E tests for all user stories
- Responsive design verification (tablet/desktop)
- Mobile read-only verification
- Accessibility (keyboard navigation)
