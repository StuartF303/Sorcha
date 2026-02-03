# Implementation Plan: Sorcha.UI Authentication Token Management and Login UX

**Branch**: `001-ui-token-refresh` | **Date**: 2026-02-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-ui-token-refresh/spec.md`

## Summary

Enhance Sorcha.UI authentication with three capabilities: (1) proactive token refresh before expiration to prevent session interruption, (2) automatic redirect to login page with return URL when tokens cannot be refreshed, and (3) Enter key submission on the login form for improved UX. The implementation builds on existing `AuthenticatedHttpMessageHandler` and `AuthenticationService` infrastructure.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Blazor WASM, MudBlazor, Microsoft.AspNetCore.Components.Authorization
**Storage**: Browser localStorage (encrypted via BrowserTokenCache)
**Testing**: xUnit + FluentAssertions + Moq (unit), Playwright (E2E)
**Target Platform**: Browser (Blazor WebAssembly)
**Project Type**: Web application (Blazor WASM client)
**Performance Goals**: Token refresh completes within existing HTTP request latency (no additional user-visible delay)
**Constraints**: Must work with existing OAuth2 password grant flow, 5-minute refresh threshold
**Scale/Scope**: Single-page application, ~10 authenticated pages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ PASS | Changes are client-side only (Sorcha.UI.Core, Sorcha.UI.Web.Client) |
| II. Security First | ✅ PASS | Return URL validation prevents open redirect; tokens encrypted in localStorage |
| III. API Documentation | N/A | No new API endpoints |
| IV. Testing Requirements | ✅ PASS | Unit tests for URL validation, E2E tests for login flow |
| V. Code Quality | ✅ PASS | Async/await, DI, nullable enabled |
| VI. Blueprint Creation | N/A | Not applicable |
| VII. Domain-Driven Design | N/A | UI enhancement, no domain changes |
| VIII. Observability | ✅ PASS | Existing logging patterns maintained |

**Gate Result**: PASS - All applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/001-ui-token-refresh/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 research findings
├── data-model.md        # Phase 1 data model (minimal - uses existing)
├── quickstart.md        # Phase 1 implementation quickstart
├── contracts/           # Phase 1 contracts (N/A - no new APIs)
└── tasks.md             # Phase 2 task breakdown
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/
│   ├── Services/
│   │   ├── Authentication/
│   │   │   ├── AuthenticationService.cs      # Enhance redirect logic
│   │   │   └── IAuthenticationService.cs     # Add redirect method signature
│   │   ├── Http/
│   │   │   └── AuthenticatedHttpMessageHandler.cs  # Add redirect on refresh failure
│   │   └── Navigation/
│   │       ├── INavigationService.cs         # NEW: Navigation abstraction
│   │       └── NavigationService.cs          # NEW: Return URL handling
│   └── Utilities/
│       └── UrlValidator.cs                   # NEW: Return URL security validation
├── Sorcha.UI.Web.Client/
│   └── Pages/
│       └── Login.razor                       # Enhance: Enter key + return URL redirect

tests/
├── Sorcha.UI.Core.Tests/
│   ├── Services/
│   │   └── Navigation/
│   │       └── NavigationServiceTests.cs     # NEW: Return URL tests
│   └── Utilities/
│       └── UrlValidatorTests.cs              # NEW: Security validation tests
└── Sorcha.UI.E2E.Tests/
    └── Authentication/
        ├── TokenRefreshTests.cs              # NEW: E2E token refresh tests
        └── LoginFlowTests.cs                 # Enhance: Enter key + return URL tests
```

**Structure Decision**: Extends existing Sorcha.UI.Core and Sorcha.UI.Web.Client projects. New NavigationService abstracts return URL handling. UrlValidator provides security validation as a separate testable utility.

## Complexity Tracking

No constitution violations requiring justification. Implementation uses existing patterns and minimal new abstractions.

## Implementation Phases

### Phase 0: Research (Complete)

See [research.md](./research.md) for detailed findings.

### Phase 1: Design

1. **NavigationService** - Abstraction for login redirect with return URL
2. **UrlValidator** - Security utility for return URL validation
3. **Login.razor enhancements** - Enter key handling + return URL consumption

### Phase 2: Tasks

Generated via `/speckit.tasks` command after plan approval.
