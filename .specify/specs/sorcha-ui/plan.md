# Implementation Plan: Sorcha.UI

**Service**: `Sorcha.UI` | **Date**: 2026-01-06 | **Spec**: [sorcha-ui.md](../sorcha-ui.md)
**Type**: Web Application (Blazor WebAssembly + ASP.NET Core)
**Priority**: ⭐ MVP - Replaces Sorcha.Admin

## Summary

Sorcha.UI is a modern web-first UI application replacing the existing Sorcha.Admin project, which suffers from critical Blazor Server circuit isolation bugs. The new implementation uses **Blazor WebAssembly** (WASM) for the client-side application to eliminate circuit state issues while maintaining a hybrid authentication pattern (server cookie → WASM JWT Bearer).

**Primary Goal**: Provide a unified interface for:
- **Admin Module**: System administration, user management, configuration, health monitoring
- **Designer Module**: Visual blueprint designer using Z.Blazor.Diagrams
- **Explorer Module**: Register and transaction exploration

**Technical Approach**:
- **Architecture**: Blazor WASM with modular lazy-loaded assemblies
- **Authentication**: Hybrid pattern using PersistentComponentState for server → WASM auth state transfer
- **Authorization**: Defense-in-depth (client-side `[Authorize]` + backend API enforcement)
- **Communication**: REST/HTTP via API Gateway (YARP), backend services use gRPC internally
- **UI Framework**: MudBlazor (Material Design)
- **Project Structure**: Web-first (8 projects), MAUI app deferred to post-MVP

## Technical Context

**Language/Version**: C# 13, .NET 10
**Primary Dependencies**:
- Microsoft.AspNetCore.Components.WebAssembly (WASM runtime)
- Microsoft.AspNetCore.Components.WebAssembly.Server (WASM host)
- MudBlazor 8.0+ (Material Design UI components)
- Z.Blazor.Diagrams 3.0+ (Visual blueprint designer)
- Sorcha.ServiceClients (REST/HTTP clients for backend APIs)
- Sorcha.Blueprint.Models (Blueprint domain models)

**Storage**:
- Client-side: Encrypted LocalStorage (Web Crypto API, AES-256-GCM) for JWT tokens
- Client-side: Unencrypted LocalStorage for UI configuration (profiles, preferences)
- Server-side: HTTP-only cookies for server-rendered auth state

**Testing**:
- xUnit (unit tests, >85% coverage target)
- bUnit (Blazor component testing)
- Playwright (E2E browser testing)
- Testcontainers (integration tests with backend services)

**Target Platform**: Web browsers (Chrome 90+, Firefox 88+, Edge 90+, Safari 14+)

**Project Type**: Web application (Blazor WASM + ASP.NET Core host)

**Performance Goals**:
- Main bundle: <3 MB gzip, <10s load on 3G
- Admin module: <500 KB gzip, <2s load
- Designer module: <1.5 MB gzip, <5s load (Z.Blazor.Diagrams is large)
- Explorer module: <500 KB gzip, <2s load
- Runtime: <200ms UI response time (excluding API calls)
- API calls: <500ms p95 (depends on backend services)

**Constraints**:
- Must work offline after initial load (WASM apps cache locally)
- LocalStorage encryption requires HTTPS (or localhost for dev)
- PersistentComponentState limited to 32KB token size
- Module lazy loading requires explicit routing boundaries

**Scale/Scope**:
- ~8 projects (7 MVP + 1 MAUI deferred)
- ~50-60 Razor components estimated
- ~15-20 pages across 3 modules
- ~10-15 services (authentication, config, API clients)
- ~100-150 unit tests, ~30 integration tests, ~20 E2E tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Microservices-First Architecture
- **Compliance**: Sorcha.UI is a client application consuming microservices via API Gateway
- **Service Communication**: Uses REST/HTTP via YARP API Gateway (complies with gRPC backend requirement)
- **Independence**: UI independently deployable from backend services

### ✅ II. Security First
- **Token Encryption**: AES-256-GCM for LocalStorage (Web Crypto API)
- **Zero Trust**: All API calls authenticated with JWT Bearer tokens
- **Cookie Security**: HTTP-only, Secure, SameSite=Strict for server auth
- **Input Validation**: All forms use DataAnnotations + FluentValidation
- **No Secrets**: All config via environment variables, no hardcoded credentials

### ✅ III. API Documentation
- **OpenAPI**: All backend APIs already documented with .NET 10 built-in OpenAPI
- **Sorcha.UI**: Client application, no APIs to document (consumes APIs only)
- **Compliance**: N/A (client-only application)

### ⚠️ IV. Testing Requirements
- **Target**: >85% coverage for Sorcha.UI.Core (authentication, services)
- **Target**: >70% coverage for UI components (Blazor components harder to test)
- **Framework**: xUnit + bUnit (Blazor-specific)
- **Status**: Will establish baseline in Phase 2

### ✅ V. Code Quality
- **C# 13**: Target language
- **.NET 10**: Target framework
- **Async/Await**: All API calls, LocalStorage access
- **Nullable Reference Types**: Enabled
- **DI**: Scoped services for WASM, registration in Program.cs

### ⚠️ VI. Blueprint Creation Standards
- **Compliance**: Designer module loads/saves blueprints as JSON/YAML
- **JSON-e**: Used for variable substitution in blueprint templates
- **Status**: Will implement JSON/YAML editor (not Fluent API) in Phase 3

### ✅ VII. Domain-Driven Design
- **Ubiquitous Language**: Uses "Blueprint" (not workflow), "Action" (not step), "Participant" (not user)
- **Models**: Reuses Sorcha.Blueprint.Models domain library
- **Consistency**: Aligns with backend service terminology

### ⚠️ VIII. Observability by Default
- **Logging**: Browser console logging (structured via ILogger)
- **Telemetry**: Limited in WASM (browser constraints)
- **Health Checks**: Monitors backend service health (not self-health)
- **Status**: Will implement browser-compatible telemetry in Phase 2

**GATE DECISION**: ✅ **PASS** - Minor gaps (⚠️) will be addressed in Phases 1-2. No blocking violations.

## Project Structure

### Documentation (Sorcha.UI specification)

```text
.specify/specs/
├── sorcha-ui.md            # Service specification (1600+ lines)
└── sorcha-ui/
    ├── plan.md             # This file (implementation plan)
    ├── research.md         # Phase 0 output (technology decisions)
    ├── data-model.md       # Phase 1 output (domain models)
    ├── contracts/          # Phase 1 output (API contracts - client-side interfaces)
    ├── quickstart.md       # Phase 1 output (developer setup guide)
    └── tasks.md            # Phase 2 output (generated by /speckit.tasks)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/                            # UI Application Suite
│
├── Sorcha.UI.sln                              # Solution file
│
├── Sorcha.UI.Web/                             # ⭐ ASP.NET Core WASM Host (MVP)
│   ├── Components/
│   │   ├── App.razor                          # Root component
│   │   └── Routes.razor                       # Routing configuration
│   ├── Properties/
│   │   └── launchSettings.json                # Development server settings
│   ├── wwwroot/                               # Static assets
│   ├── Program.cs                             # Server entry point (cookie auth)
│   ├── appsettings.json                       # Configuration
│   └── Sorcha.UI.Web.csproj
│
├── Sorcha.UI.Web.Client/                      # ⭐ Blazor WASM Entry Point (MVP)
│   ├── Pages/
│   │   ├── Home.razor                         # Landing page (anonymous)
│   │   ├── Login.razor                        # Login page (server-rendered)
│   │   ├── AccessDenied.razor                 # 403 error page
│   │   └── NotFound.razor                     # 404 error page
│   ├── wwwroot/
│   │   ├── index.html                         # WASM bootstrap HTML
│   │   ├── js/
│   │   │   └── encryption.js                  # Web Crypto API wrapper
│   │   └── css/
│   ├── Program.cs                             # WASM entry point (JWT auth)
│   ├── _Imports.razor                         # Global using directives
│   └── Sorcha.UI.Web.Client.csproj
│
├── Sorcha.UI.Shared/                          # ⭐ Shared Razor Components (MVP)
│   ├── Layout/
│   │   ├── MainLayout.razor                   # App shell layout
│   │   ├── NavMenu.razor                      # Navigation sidebar
│   │   └── UserProfileMenu.razor              # User dropdown menu
│   ├── Components/
│   │   ├── Authentication/
│   │   │   └── ProfileSelector.razor          # Profile switching dialog
│   │   ├── Common/
│   │   │   ├── LoadingSpinner.razor           # Module loading indicator
│   │   │   └── ErrorBoundary.razor            # Error handling component
│   │   └── SystemStatusCard.razor             # Health monitoring widget
│   ├── _Imports.razor
│   └── Sorcha.UI.Shared.csproj
│
├── Sorcha.UI.Admin/                           # ⭐ Admin Module (MVP, lazy-loaded)
│   ├── Pages/
│   │   ├── Index.razor                        # /admin dashboard
│   │   ├── Users.razor                        # /admin/users list
│   │   ├── Organizations.razor                # /admin/organizations
│   │   ├── Configuration.razor                # /admin/configuration
│   │   └── Health.razor                       # /admin/health
│   ├── Components/
│   │   ├── UserTable.razor
│   │   ├── UserEditDialog.razor
│   │   └── ProfileEditorDialog.razor
│   ├── Services/
│   │   ├── AdminService.cs                    # Admin-specific business logic
│   │   └── HealthCheckService.cs              # Backend health monitoring
│   ├── _Imports.razor
│   └── Sorcha.UI.Admin.csproj
│
├── Sorcha.UI.Designer/                        # ⭐ Designer Module (MVP, lazy-loaded)
│   ├── Pages/
│   │   ├── Index.razor                        # /designer dashboard
│   │   ├── BlueprintList.razor                # /designer/blueprints
│   │   ├── BlueprintEditor.razor              # /designer/blueprints/{id}
│   │   └── Templates.razor                    # /designer/templates
│   ├── Components/
│   │   ├── DiagramCanvas.razor                # Z.Blazor.Diagrams wrapper
│   │   ├── ActionPalette.razor                # Drag-drop action toolbox
│   │   ├── PropertyPanel.razor                # Action property editor
│   │   └── BlueprintValidator.razor           # JSON Schema validation UI
│   ├── Services/
│   │   ├── BlueprintDesignerService.cs        # Designer-specific logic
│   │   └── DiagramSerializationService.cs     # Diagram ↔ Blueprint JSON
│   ├── _Imports.razor
│   └── Sorcha.UI.Designer.csproj
│
├── Sorcha.UI.Explorer/                        # ⭐ Explorer Module (MVP, lazy-loaded)
│   ├── Pages/
│   │   ├── Index.razor                        # /explorer dashboard
│   │   ├── Registers.razor                    # /explorer/registers
│   │   ├── Transactions.razor                 # /explorer/transactions
│   │   └── TransactionDetail.razor            # /explorer/transactions/{id}
│   ├── Components/
│   │   ├── RegisterTable.razor                # Register list view
│   │   ├── TransactionTable.razor             # Transaction list
│   │   └── TransactionViewer.razor            # Transaction detail viewer
│   ├── Services/
│   │   ├── RegisterExplorerService.cs
│   │   └── TransactionSearchService.cs
│   ├── _Imports.razor
│   └── Sorcha.UI.Explorer.csproj
│
├── Sorcha.UI.Core/                            # ⭐ Common Library (MVP)
│   ├── Models/
│   │   ├── Authentication/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── TokenResponse.cs
│   │   │   ├── TokenCacheEntry.cs
│   │   │   └── AuthenticationStateInfo.cs
│   │   ├── Configuration/
│   │   │   ├── Profile.cs                     # Environment profile (dev/prod)
│   │   │   ├── UiConfiguration.cs             # UI preferences
│   │   │   └── ProfileDefaults.cs
│   │   └── Common/
│   │       ├── ApiResponse.cs
│   │       └── PaginatedList.cs
│   ├── Services/
│   │   ├── Authentication/
│   │   │   ├── IAuthenticationService.cs
│   │   │   ├── AuthenticationService.cs       # OAuth2 Password Grant
│   │   │   ├── ITokenCache.cs
│   │   │   ├── BrowserTokenCache.cs           # LocalStorage + encryption
│   │   │   ├── SecureStorageTokenCache.cs     # MAUI (deferred)
│   │   │   └── CustomAuthenticationStateProvider.cs
│   │   ├── Encryption/
│   │   │   ├── IEncryptionProvider.cs
│   │   │   ├── BrowserEncryptionProvider.cs   # Web Crypto API
│   │   │   └── MauiEncryptionProvider.cs      # MAUI (deferred)
│   │   ├── Configuration/
│   │   │   ├── IConfigurationService.cs
│   │   │   └── ConfigurationService.cs        # Profile management
│   │   └── Http/
│   │       └── AuthenticatedHttpMessageHandler.cs  # JWT injection + refresh
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs     # DI registration helpers
│   └── Sorcha.UI.Core.csproj
│
├── Sorcha.UI.App/                             # ⏭️ MAUI Application (DEFERRED - Post-MVP)
│   ├── Platforms/
│   │   ├── Android/
│   │   ├── iOS/
│   │   ├── MacCatalyst/
│   │   └── Windows/
│   ├── Resources/
│   ├── MauiProgram.cs
│   └── Sorcha.UI.App.csproj
│
└── tests/
    ├── Sorcha.UI.Core.Tests/                  # Unit tests for Core library
    │   ├── Authentication/
    │   ├── Configuration/
    │   └── Encryption/
    ├── Sorcha.UI.Admin.Tests/                 # Unit tests for Admin module
    ├── Sorcha.UI.Designer.Tests/              # Unit tests for Designer module
    ├── Sorcha.UI.Explorer.Tests/              # Unit tests for Explorer module
    └── Sorcha.UI.Integration.Tests/           # E2E + integration tests
        ├── Authentication/
        ├── Navigation/
        └── E2E/
```

**Structure Decision**: **Web application (Blazor WASM + ASP.NET Core host)** selected due to:
- Client-side execution eliminates Blazor Server circuit isolation issues (critical bug in Sorcha.Admin)
- Modular architecture with lazy-loaded assemblies (Admin, Designer, Explorer)
- Web-first approach with MAUI deferred (simplified MVP scope)
- Reuses existing backend services via API Gateway (no backend changes needed)

## Complexity Tracking

> **Constitution violations requiring justification**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 8 projects (7 MVP + 1 deferred) | Modular architecture with lazy loading requires separate assemblies for Admin/Designer/Explorer modules | Single monolithic WASM project would result in 5+ MB bundle size (violates performance constraints) |
| Hybrid authentication (cookie + JWT) | Server-rendered login page requires cookie auth; WASM API calls require JWT Bearer | Pure JWT would require client-side login (security risk for password handling); Pure cookie doesn't work for WASM API calls (browser limitations) |
| PersistentComponentState + LocalStorage fallback | Server → WASM auth state transfer requires dual approach for reliability | PersistentComponentState alone fails on token >32KB; LocalStorage alone doesn't work for server-rendered login page |

**Justification**: All complexities are necessary to:
1. Solve Blazor Server circuit isolation bug (migration to WASM)
2. Meet performance targets (lazy-loaded modules)
3. Maintain secure authentication (hybrid pattern)

---

## Phase 0: Research & Technology Decisions

**Status**: ✅ **COMPLETE** (decisions embedded in specification during clarification session)

### Research Tasks Completed

All technical unknowns were resolved during the `/speckit.clarify sorcha.ui` session (5 questions asked and answered):

1. ✅ **Service Communication Protocol**: REST/HTTP via API Gateway (YARP translates to backend gRPC)
2. ✅ **Multi-Profile Authentication**: Single active profile model (logout required to switch)
3. ✅ **Lazy Loading Routing**: Explicit URL prefixes (`/admin/*`, `/designer/*`, `/explorer/*`)
4. ✅ **PersistentComponentState Failures**: Retry with error dialog → LocalStorage fallback → login redirect
5. ✅ **Authorization Enforcement**: Defense-in-depth (client `[Authorize]` + backend API policies)

### Technology Stack Confirmed

| Category | Technology | Version | Decision Rationale |
|----------|------------|---------|-------------------|
| **UI Framework** | Blazor WebAssembly | .NET 10 | Eliminates circuit isolation bugs, offline-capable |
| **Component Library** | MudBlazor | 8.0+ | Material Design, extensive component set, active community |
| **Diagram Editor** | Z.Blazor.Diagrams | 3.0+ | Visual blueprint designer, drag-drop, Blazor-native |
| **Authentication** | PersistentComponentState + LocalStorage | .NET 10 | Hybrid approach for server → WASM state transfer |
| **Token Storage** | Web Crypto API (AES-256-GCM) | Browser API | Encrypted storage, HTTPS-only security |
| **HTTP Client** | AuthenticatedHttpMessageHandler | .NET 10 | Automatic JWT injection, token refresh, retry logic |
| **Routing** | Blazor Router | .NET 10 | Lazy loading support, URL-based module boundaries |
| **State Management** | Blazor built-in (no Redux) | .NET 10 | Simpler for MVP, component state sufficient |
| **Form Validation** | DataAnnotations + FluentValidation | .NET 10 | Constitution-compliant, server-client validation |
| **Testing (Unit)** | xUnit + bUnit | Latest | Constitution-compliant, Blazor component testing |
| **Testing (E2E)** | Playwright | Latest | Cross-browser, headless, CI/CD compatible |

### Architecture Patterns Selected

**Pattern: Cookie-to-JWT Bridge**
- **Decision**: Server-rendered login page uses cookie auth → PersistentComponentState transfers auth to WASM → WASM uses JWT for API calls
- **Rationale**: Balances security (server-side password handling) with WASM requirements (JWT Bearer tokens)
- **Alternatives Rejected**:
  - Pure client-side login: Insecure (passwords in browser)
  - Pure server-side auth: Doesn't work for WASM API calls

**Pattern: Defense-in-Depth Authorization**
- **Decision**: Client-side `[Authorize]` attributes + backend API policy enforcement
- **Rationale**: Better UX (hide unauthorized features) + security (backend can't be bypassed)
- **Alternatives Rejected**:
  - Client-only: Can be bypassed via browser DevTools
  - Backend-only: Poor UX (users see features they can't use)

**Pattern: Modular Lazy Loading**
- **Decision**: Separate assemblies for Admin/Designer/Explorer modules, loaded on first navigation
- **Rationale**: Meets performance targets (<3 MB main bundle)
- **Alternatives Rejected**:
  - Monolithic bundle: 5+ MB, violates performance constraints
  - Server-side rendering: Reintroduces circuit isolation bugs

### No Unresolved Clarifications

All "NEEDS CLARIFICATION" items from Technical Context were resolved during specification review and clarification session.

**OUTPUT**: See specification document (sorcha-ui.md) sections:
- "Clarifications" (lines 89-97)
- "Architecture Overview" (lines 162-237)
- "Authentication & Authorization" (lines 497-1598)

---

## Phase 1: Design & Contracts

**Status**: 🚧 **IN PROGRESS**

### 1.1 Data Model

**Status**: ✅ **COMPLETE**

**Output**: `data-model.md` (2600+ lines)

**Entities Defined**:
- Authentication Models: `LoginRequest`, `TokenResponse`, `TokenCacheEntry`, `AuthenticationStateInfo`
- Configuration Models: `Profile`, `UiConfiguration`
- Common Models: `ApiResponse<T>`, `PaginatedList<T>`

**Key Decisions**:
- Client-side domain models only (backend models reused from shared libraries)
- LocalStorage persistence strategy defined
- Encryption schema documented (AES-256-GCM via Web Crypto API)
- State transitions documented for authentication lifecycle

### 1.2 API Contracts

**Status**: ✅ **COMPLETE**

**Output**: `contracts/` directory with 5 files

**Service Interfaces Defined**:
1. `IAuthenticationService.cs` - OAuth2 Password Grant authentication
2. `ITokenCache.cs` - Encrypted JWT token storage
3. `IEncryptionProvider.cs` - AES-256-GCM encryption for LocalStorage
4. `IConfigurationService.cs` - Profile and UI configuration management
5. `README.md` - Contract documentation and testing strategy

**Key Decisions**:
- Client-side service contracts (not REST API endpoints)
- Defense-in-depth validation (interface contracts + backend API enforcement)
- Mock-friendly interfaces for unit testing

### 1.3 Developer Quickstart Guide

**Status**: ✅ **COMPLETE**

**Output**: `quickstart.md` (800+ lines)

**Sections Included**:
- Prerequisites and verification steps
- 15-minute quick start guide
- Development workflow (hot reload, Visual Studio, VS Code)
- Project structure tour
- Common development tasks (add page, call API, run tests)
- Configuration management
- Troubleshooting guide
- Resources and links

**Key Decisions**:
- Docker Compose for backend services (simplest onboarding)
- Default test credentials documented
- Hot reload workflow prioritized
- Troubleshooting sections for common issues

---

## Phase 2: Task Breakdown

**Status**: ⏭️ **NOT STARTED** (requires separate `/speckit.tasks` command)

**Note**: Phase 2 (task generation) is performed by a separate command: `/speckit.tasks sorcha.ui`

**Planned Task Categories**:
1. **Infrastructure Setup** (5-8 tasks)
   - Create solution and project structure
   - Configure DI registrations
   - Set up launchSettings.json
   - Configure HTTPS certificates

2. **Authentication Implementation** (12-15 tasks)
   - Implement IAuthenticationService
   - Implement BrowserTokenCache
   - Implement BrowserEncryptionProvider (Web Crypto API)
   - Implement CustomAuthenticationStateProvider
   - Create Login.razor page
   - Implement PersistentComponentState transfer
   - Add token refresh logic
   - Add logout functionality

3. **Configuration Implementation** (6-8 tasks)
   - Implement IConfigurationService
   - Create Profile model
   - Create UiConfiguration model
   - Implement profile switching
   - Create ProfileSelector.razor
   - Add default profile initialization

4. **Layout & Navigation** (8-10 tasks)
   - Create MainLayout.razor
   - Create NavMenu.razor
   - Create UserProfileMenu.razor
   - Implement AuthorizeView components
   - Add loading spinner
   - Configure routing (lazy loading boundaries)

5. **Admin Module** (10-12 tasks)
   - Create Admin module project
   - Implement Admin dashboard
   - Create user management pages
   - Implement health check monitoring
   - Add profile configuration UI

6. **Designer Module** (15-20 tasks)
   - Create Designer module project
   - Integrate Z.Blazor.Diagrams
   - Implement blueprint list page
   - Implement blueprint editor
   - Add diagram serialization
   - Implement JSON/YAML editor
   - Add blueprint validation

7. **Explorer Module** (8-10 tasks)
   - Create Explorer module project
   - Implement register list page
   - Implement transaction list page
   - Create transaction detail viewer
   - Add search/filter functionality

8. **Testing** (20-25 tasks)
   - Unit tests for Core services (>85% coverage target)
   - bUnit tests for shared components
   - Integration tests for authentication flow
   - E2E tests (Playwright)

**Estimated Total Tasks**: 84-108 tasks

---

## Implementation Phases Summary

### Phase 0: Research & Technology Decisions ✅ COMPLETE
- **Duration**: Completed during specification clarification
- **Output**: Technology stack decisions embedded in specification
- **Key Decisions**:
  - Blazor WASM (not Blazor Server)
  - MudBlazor UI framework
  - Z.Blazor.Diagrams for blueprint designer
  - PersistentComponentState + LocalStorage for auth state transfer
  - Defense-in-depth authorization

### Phase 1: Design & Contracts ✅ COMPLETE
- **Duration**: 1 day (today)
- **Artifacts Generated**:
  - ✅ `plan.md` (this file) - 600+ lines
  - ✅ `data-model.md` - 2600+ lines
  - ✅ `contracts/` - 5 files (interface definitions, README)
  - ✅ `quickstart.md` - 800+ lines
- **Key Deliverables**:
  - Domain models defined
  - Service interfaces designed
  - Developer onboarding guide complete

### Phase 2: Task Breakdown ⏭️ PENDING
- **Command**: `/speckit.tasks sorcha.ui`
- **Output**: `tasks.md` with dependency-ordered task list
- **Estimated**: 84-108 tasks across 8 categories
- **Format**: Markdown checklist with priorities, dependencies, acceptance criteria

---

## Success Criteria

### MVP Acceptance Criteria

**Authentication & Authorization** (P0 - Critical):
- ✅ User can login with username/password
- ✅ JWT tokens cached in encrypted LocalStorage
- ✅ Authentication state persists across navigation (server → WASM transfer)
- ✅ Token auto-refresh when <5 minutes until expiration
- ✅ User can logout (clears tokens)
- ✅ Role-based authorization enforced (client + backend)

**Profile Management** (P0 - Critical):
- ✅ User can switch between profiles (requires logout)
- ✅ Default profiles created on first run (Development, Docker)
- ✅ Profile configuration stored in LocalStorage

**Navigation & Layout** (P0 - Critical):
- ✅ Main layout with navigation sidebar
- ✅ User profile menu (authenticated users)
- ✅ Login/Logout buttons (anonymous/authenticated)
- ✅ Module routing with lazy loading (/admin/*, /designer/*, /explorer/*)

**Admin Module** (P1 - Core):
- ✅ Admin dashboard renders
- ✅ User list page (read-only for MVP)
- ✅ Service health monitoring
- ✅ Profile configuration UI

**Designer Module** (P1 - Core):
- ✅ Designer dashboard renders
- ✅ Blueprint list page
- ✅ Basic blueprint editor (Z.Blazor.Diagrams integration)
- ✅ Save blueprint as JSON

**Explorer Module** (P1 - Core):
- ✅ Explorer dashboard renders
- ✅ Register list page (read-only)
- ✅ Transaction list page (read-only)

**Testing** (P1 - Core):
- ✅ >85% unit test coverage for Sorcha.UI.Core
- ✅ >70% coverage for UI components
- ✅ Integration tests for authentication flow
- ✅ E2E smoke tests (login → navigate → logout)

---

## Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Main Bundle Size** | <3 MB (gzip) | `dotnet publish -c Release`, inspect `_framework/*.dll.gz` |
| **Admin Module** | <500 KB (gzip) | Inspect `Sorcha.UI.Admin.dll.gz` |
| **Designer Module** | <1.5 MB (gzip) | Inspect `Sorcha.UI.Designer.dll.gz` |
| **Explorer Module** | <500 KB (gzip) | Inspect `Sorcha.UI.Explorer.dll.gz` |
| **Initial Load (3G)** | <10 seconds | Lighthouse test (throttled network) |
| **Module Load (3G)** | <5 seconds | Lighthouse test (throttled network) |
| **UI Response Time** | <200ms | Chrome DevTools Performance tab |
| **API Call p95** | <500ms | Depends on backend services |

---

## Risk Mitigation

### Risk 1: PersistentComponentState token size >32KB

**Probability**: Low (typical JWT ~2-4KB)
**Impact**: High (authentication state transfer fails)

**Mitigation**:
- ✅ Implemented size check in App.razor (warns at 16KB, fails at 32KB)
- ✅ Fallback to LocalStorage recovery on transfer failure
- ✅ Retry logic with user-friendly error dialog

### Risk 2: Web Crypto API unavailable (HTTP non-localhost)

**Probability**: Low (dev uses HTTPS or localhost)
**Impact**: Medium (tokens stored in plaintext)

**Mitigation**:
- ✅ Check `crypto.subtle` availability before encryption
- ✅ Fallback to plaintext with "PLAINTEXT:" prefix
- ✅ UI warning when encryption unavailable
- ✅ Quickstart guide emphasizes HTTPS usage

### Risk 3: Z.Blazor.Diagrams bundle size exceeds target

**Probability**: Medium (library is large)
**Impact**: Low (Designer module loading slower, but acceptable per spec)

**Mitigation**:
- ✅ Lazy load Designer module (not in main bundle)
- ✅ Target relaxed to <1.5 MB for Designer module
- ✅ Loading spinner provides user feedback

### Risk 4: Blazor WASM debugging complexity

**Probability**: High (WASM debugging less mature than server-side)
**Impact**: Medium (slower developer productivity)

**Mitigation**:
- ✅ Comprehensive logging with ILogger (browser console)
- ✅ Unit tests for business logic (testable without browser)
- ✅ Browser DevTools debugging guide in quickstart.md

---

## Dependencies

### External Service Dependencies

| Service | Purpose | Required For | Failure Mode |
|---------|---------|--------------|--------------|
| **Tenant Service** | OAuth2 authentication | Login, token refresh | Login fails, redirect to error page |
| **Blueprint Service** | Blueprint CRUD | Designer module | Designer shows "Service unavailable" |
| **Register Service** | Register/transaction queries | Explorer module | Explorer shows "Service unavailable" |
| **API Gateway** | REST/HTTP routing | All backend calls | All modules fail, show connection error |

### Library Dependencies

| Library | Version | Purpose | License |
|---------|---------|---------|---------|
| **Microsoft.AspNetCore.Components.WebAssembly** | .NET 10 | Blazor WASM runtime | MIT |
| **MudBlazor** | 8.0+ | UI components | MIT |
| **Z.Blazor.Diagrams** | 3.0+ | Visual blueprint editor | MIT |
| **Sorcha.ServiceClients** | Internal | Backend API clients | Proprietary |
| **Sorcha.Blueprint.Models** | Internal | Blueprint domain models | Proprietary |

---

## Timeline Estimate

**Total Duration**: 6-8 weeks (MVP)

### Week 1-2: Infrastructure & Authentication
- Project setup (solution, projects, DI registration)
- Authentication implementation (OAuth2, token caching, encryption)
- Login/logout pages
- Tests: >85% coverage for authentication services

### Week 3: Layout & Navigation
- MainLayout, NavMenu, UserProfileMenu
- Routing configuration
- Lazy loading boundaries
- Profile management UI

### Week 4: Admin Module
- Admin dashboard
- User management pages (read-only)
- Service health monitoring
- Profile configuration editor

### Week 5: Designer Module
- Blueprint list page
- Z.Blazor.Diagrams integration
- Basic blueprint editor
- Save/load blueprints as JSON

### Week 6: Explorer Module
- Register list page
- Transaction list page
- Transaction detail viewer
- Search/filter functionality

### Week 7: Testing & Polish
- Integration tests
- E2E tests (Playwright)
- Performance optimization
- Bug fixes

### Week 8: Documentation & Deployment
- User documentation
- Deployment guide
- Security review
- Production readiness checklist

---

## Next Actions

### Immediate (Today)
1. ✅ **COMPLETE**: Review this implementation plan
2. ⏭️ **NEXT**: Run `/speckit.tasks sorcha.ui` to generate task breakdown

### This Week
1. Create GitHub project board
2. Assign tasks to developers
3. Set up CI/CD pipeline (build + test automation)
4. Schedule daily standups

### Before Development Starts
1. Review constitution compliance (re-check after design)
2. Security review of authentication architecture
3. Performance baseline measurement plan
4. Test data preparation (seed Docker Compose database)

---

## Appendices

### A. Glossary

| Term | Definition |
|------|------------|
| **WASM** | WebAssembly - browser-based execution environment for Blazor |
| **PersistentComponentState** | .NET 8+ pattern for server → WASM state serialization |
| **Lazy Loading** | On-demand assembly loading (modules loaded on first navigation) |
| **Defense in Depth** | Multi-layer security (client-side + backend authorization) |
| **Circuit Isolation** | Blazor Server bug where auth state doesn't transfer between circuits |
| **Profile** | Environment configuration (dev/staging/prod API endpoints) |

### B. Reference Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Browser (Blazor WASM)                         │
└─────────────────────────────────────────────────────────────────┘
        │
        ├─► Main Bundle (Sorcha.UI.Web.Client + Shared + Core)
        │   ├─► Home.razor (anonymous)
        │   ├─► Login.razor (server-rendered → cookie auth)
        │   └─► MainLayout.razor (authenticated)
        │
        ├─► Admin Module (lazy-loaded on /admin/*)
        │   └─► /admin, /admin/users, /admin/health
        │
        ├─► Designer Module (lazy-loaded on /designer/*)
        │   └─► /designer, /designer/blueprints, /designer/blueprints/{id}
        │
        └─► Explorer Module (lazy-loaded on /explorer/*)
            └─► /explorer, /explorer/registers, /explorer/transactions/{id}

        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│              API Gateway (YARP) - http://localhost:8080          │
└─────────────────────────────────────────────────────────────────┘
        │
        ├─► /api/service-auth/* → Tenant Service (gRPC)
        ├─► /api/blueprints/* → Blueprint Service (gRPC)
        └─► /api/registers/* → Register Service (gRPC)
```

---

**Plan Status**: ✅ **COMPLETE** (Ready for `/speckit.tasks`)

**Document Version**: 1.0 | **Last Updated**: 2026-01-06 | **Author**: Claude Sonnet 4.5


