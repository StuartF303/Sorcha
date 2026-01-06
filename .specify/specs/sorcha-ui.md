# Sorcha.UI - Unified Multi-Platform User Interface

**Version:** 1.1
**Status:** Approved - Ready for Implementation
**Created:** 2026-01-06
**Updated:** 2026-01-06
**Author:** AI Assistant with User Review
**Type:** Service Specification

**Architecture Decisions Approved:**
- ✅ Authentication: PersistentComponentState for server → WASM state transfer
- ✅ Authorization: Simple roles (Administrator, Designer, Viewer)
- ✅ Module Size: Designer module size not a concern (Z.Blazor.Diagrams valuable)
- ✅ Offline Support: Not needed for MVP, focus on online functionality
- ✅ Project Structure: Web-first emphasis, MAUI app in subdirectory (deferred)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Background & Context](#background--context)
3. [Goals & Non-Goals](#goals--non-goals)
4. [Architecture Overview](#architecture-overview)
5. [Project Structure](#project-structure)
6. [Authentication & Authorization](#authentication--authorization)
7. [Functional Components](#functional-components)
8. [Technology Stack](#technology-stack)
9. [Implementation Plan](#implementation-plan)
10. [Security Considerations](#security-considerations)
11. [Migration from Sorcha.Admin](#migration-from-sorchaadmin)
12. [Open Questions](#open-questions)

---

## Executive Summary

**Sorcha.UI** is a next-generation multi-platform user interface for the Sorcha distributed ledger platform, replacing the existing **Sorcha.Admin** application. Built on the **MAUI Blazor Hybrid and Web** template, Sorcha.UI provides:

- **Primary Focus: Web Deployment (Blazor WASM)** - Browser-based application for immediate use
- **Modular architecture**: Functionality split into WASM assemblies (Admin, Designer, Explorer) for lazy loading and maintainability
- **Hybrid authentication**: Cookie-based authentication for server-rendered pages + JWT Bearer tokens for WASM components calling backend APIs
- **Unified codebase**: Shared Razor components across web and future MAUI platforms
- **Future: Multi-platform deployment** - Desktop (Windows/macOS/Linux) and Mobile (iOS/Android) via MAUI (deferred)

**Key Differentiators from Sorcha.Admin:**
- ✅ Solves Blazor Server circuit isolation issues by using WASM for authenticated components
- ✅ Modular WASM assembly architecture for better performance and maintainability
- ✅ Hybrid authentication model that supports both anonymous landing pages and authenticated API access
- ✅ Web-first development with optional MAUI desktop/mobile support later
- ✅ Simple role-based authorization (Administrator, Designer, Viewer)

---

## Background & Context

### Why Replace Sorcha.Admin?

**Sorcha.Admin** (Blazor Server) has the following critical issues documented in [KNOWN-ISSUES.md](../../src/Apps/Sorcha.Admin/KNOWN-ISSUES.md):

1. **❌ BLOCKING: Authentication State Not Displaying After Login**
   - Blazor Server circuit isolation prevents authentication state from persisting across navigation
   - `CustomAuthenticationStateProvider` never called in new circuits after login
   - JWT tokens stored in LocalStorage but not accessible across circuits
   - 20+ attempted fixes, all failed

2. **⚠️ Architectural Limitations:**
   - Blazor Server requires persistent WebSocket connection (poor for mobile/unreliable networks)
   - Server-side circuit memory management complexity
   - Cannot work offline
   - LocalStorage access issues during prerendering

3. **🔄 Migration Recommended:**
   - Official recommendation: Migrate to Blazor WebAssembly (see KNOWN-ISSUES.md)
   - WASM solves circuit isolation, state persistence, and offline support

### Why MAUI Blazor Hybrid + Web?

The **MAUI Blazor Hybrid and Web** template provides:

1. **Unified Codebase:** Share Razor components across Web (WASM), Desktop, and Mobile
2. **Multi-Platform:** Deploy to browsers, Windows, macOS, Linux, iOS, Android from one codebase
3. **Blazor WASM for Web:** Solves Sorcha.Admin authentication state issues
4. **Native Platform Integration:** Access device features (camera, GPS, biometrics) for future enhancements
5. **Offline Support:** WASM apps work offline, native apps always available

---

## Clarifications

### Session 2026-01-06

- **Q: Service Communication Protocol** - Should Sorcha.UI use gRPC-Web or REST/HTTP to communicate with backend services? → **A: Option B** - Sorcha.UI uses existing REST/HTTP clients via API Gateway (YARP translates to backend gRPC as needed, simpler WASM compatibility)
- **Q: Multi-Profile Concurrent Authentication** - Can users be authenticated to multiple profiles simultaneously, or does switching profiles require logout? → **A: Option A** - Single active profile, logout required to switch (simple, secure, clear UX)
- **Q: Lazy Loading Module Routing Boundaries** - What URL patterns trigger lazy loading of each WASM assembly (Admin, Designer, Explorer)? → **A: Option A** - Explicit module prefixes (`/admin/*`, `/designer/*`, `/explorer/*`) with loading spinner during first access
- **Q: PersistentComponentState Failure Modes** - What should happen when PersistentComponentState fails to transfer authentication data from server to WASM? → **A: Hybrid B+C** - Show error dialog with retry on first failure (Option B), fall back to LocalStorage token recovery on repeated failure (Option C), redirect to login as final fallback
- **Q: Authorization Policy Enforcement** - At which layers should authorization be enforced (client-side UI, backend API, or both)? → **A: Option C** - Defense in Depth - Enforce authorization at both client-side (Blazor `[Authorize]` attributes for UX) and backend (API Gateway + service-level policies for security)

---

## Goals & Non-Goals

### Goals

**Primary Goals (MVP):**
1. ✅ Replace Sorcha.Admin with a multi-platform UI that solves authentication issues
2. ✅ Support **Web (Blazor WASM)** deployment as primary target
3. ✅ Implement **hybrid authentication** (cookie for landing page, JWT Bearer for WASM API calls)
4. ✅ Split functionality into **modular WASM assemblies** (Admin, Designer, Explorer)
5. ✅ Migrate all existing Sorcha.Admin features (Blueprint Designer, Configuration, Health Monitoring)
6. ✅ Maintain existing authentication architecture (OAuth2 Password Grant, encrypted LocalStorage)
7. ✅ Ensure authentication state persists across navigation (fixing Sorcha.Admin bug)

**Secondary Goals (Post-MVP):**
8. ⏭️ Enable **Desktop (MAUI)** deployment for Windows, macOS, Linux
9. ⏭️ Enable **Mobile (MAUI)** deployment for iOS, Android
10. ⏭️ Implement platform-specific features (biometrics, camera, offline sync)
11. ⏭️ Progressive Web App (PWA) support for web deployment
12. ⏭️ Mobile-responsive design optimizations

### Non-Goals

❌ **Out of Scope for MVP:**
- Multi-factor authentication (MFA) - deferred to post-MVP
- OAuth2/OIDC integration (Google, Microsoft login) - deferred
- Offline-first architecture for mobile - deferred
- Native platform integrations (camera, GPS, biometrics) - deferred
- Real-time collaboration (multi-user blueprint editing) - deferred

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Sorcha.UI Platform                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────────────┐              ┌──────────────────────┐         │
│  │  Sorcha.UI           │              │  Sorcha.UI.Web       │         │
│  │  (MAUI App)          │              │  (ASP.NET Core Host) │         │
│  │                      │              │                      │         │
│  │  Target Platforms:   │              │  Web Host            │         │
│  │  - Windows           │              │  - Serves WASM       │         │
│  │  - macOS             │              │  - Server Auth       │         │
│  │  - Linux (future)    │              │  - Static Assets     │         │
│  │  - iOS               │              │                      │         │
│  │  - Android           │              │                      │         │
│  └──────────────────────┘              └──────────────────────┘         │
│           │                                       │                      │
│           │                                       │                      │
│           └───────────┬───────────────────────────┘                      │
│                       │                                                  │
│                       ▼                                                  │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              Sorcha.UI.Shared (Razor Components)                   │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │  • Layout (MainLayout, NavMenu)                                   │ │
│  │  • Pages (Home, Login, Settings)                                  │ │
│  │  • Authentication Components (LoginDialog, ProfileMenu)           │ │
│  │  • Shared Services (IFormFactor, platform abstractions)           │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                       │                                                  │
│                       ▼                                                  │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │       Sorcha.UI.Web.Client (Blazor WASM Entry Point)              │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │  • Program.cs (WASM bootstrapping)                                │ │
│  │  • DI configuration                                               │ │
│  │  • HTTP client setup                                              │ │
│  │  • WASM-specific services                                         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                       │                                                  │
│                       ▼                                                  │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │         Modular WASM Assemblies (Lazy-Loaded)                     │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │                                                                    │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐ │ │
│  │  │ Sorcha.UI.Admin  │  │ Sorcha.UI.       │  │ Sorcha.UI.     │ │ │
│  │  │                  │  │ Designer         │  │ Explorer       │ │ │
│  │  │ - User Mgmt      │  │                  │  │                │ │ │
│  │  │ - Config Mgmt    │  │ - Blueprint      │  │ - Register     │ │ │
│  │  │ - Health Checks  │  │   Visual Editor  │  │   Browser      │ │ │
│  │  │ - Audit Logs     │  │ - Schema Editor  │  │ - Transaction  │ │ │
│  │  │ - Profiles       │  │ - Validation     │  │   Viewer       │ │ │
│  │  │                  │  │ - Templates      │  │ - Block        │ │ │
│  │  │                  │  │                  │  │   Explorer     │ │ │
│  │  └──────────────────┘  └──────────────────┘  └────────────────┘ │ │
│  │                                                                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                  Common Libraries                                  │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │  • Sorcha.UI.Core (Models, Interfaces, Shared Logic)              │ │
│  │  • Sorcha.ServiceClients (API clients for backend services)       │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└───────────────────────────────────────┬───────────────────────────────────┘
                                        │
                                        ▼
        ┌────────────────────────────────────────────────────────┐
        │           Backend Sorcha Services (gRPC/REST)          │
        ├────────────────────────────────────────────────────────┤
        │  • Tenant Service (Auth, Users, Orgs)                 │
        │  • Blueprint Service (Blueprint CRUD, Validation)      │
        │  • Wallet Service (Wallet Management)                 │
        │  • Register Service (Blockchain Storage)              │
        │  • Peer Service (P2P Networking)                      │
        │  • API Gateway (YARP routing)                         │
        └────────────────────────────────────────────────────────┘
```

**Service Communication Protocol:**

Sorcha.UI communicates with backend services using **REST/HTTP via the API Gateway**. The API Gateway (YARP) translates REST requests to internal gRPC calls as needed, maintaining the Sorcha platform's gRPC-based service-to-service communication while providing WASM-compatible REST endpoints for browser clients. This approach:
- ✅ Leverages existing `Sorcha.ServiceClients` REST/HTTP implementation
- ✅ Ensures WASM browser compatibility (no gRPC-Web proxy required)
- ✅ Maintains platform's internal gRPC architecture (backend services communicate via gRPC)
- ✅ Simplifies authentication (JWT Bearer tokens in HTTP Authorization headers)

### Render Mode Strategy

Sorcha.UI uses **hybrid rendering** to balance performance, security, and user experience:

| Component Type | Render Mode | Rationale |
|----------------|-------------|-----------|
| **Landing Page (Home)** | Static SSR or InteractiveServer | Fast initial load, SEO-friendly, anonymous access |
| **Login Page** | InteractiveServer | Server-side validation, cookie-based auth, no WASM download |
| **Authenticated Pages** | InteractiveWebAssembly | Solves auth state persistence, offline support, JWT Bearer tokens |
| **Admin Module** | InteractiveWebAssembly | Lazy-loaded WASM assembly, JWT auth |
| **Designer Module** | InteractiveWebAssembly | Lazy-loaded WASM assembly, JWT auth, complex interactions |
| **Explorer Module** | InteractiveWebAssembly | Lazy-loaded WASM assembly, JWT auth, data visualization |

**Key Principle:** Anonymous pages use Server rendering, authenticated pages use WASM with lazy-loaded assemblies.

### Module Routing & Lazy Loading Boundaries

**URL Structure:** Each WASM module has an explicit URL prefix that triggers lazy loading:

| Module | URL Prefix | Assembly | Example Routes | Lazy Load Trigger |
|--------|------------|----------|----------------|-------------------|
| **Core/Shared** | `/`, `/login`, `/settings` | Main WASM bundle | `/` (home), `/login`, `/settings`, `/not-found` | Initial page load |
| **Admin** | `/admin/*` | Sorcha.UI.Admin.dll | `/admin`, `/admin/users`, `/admin/config`, `/admin/health` | First navigation to `/admin/*` |
| **Designer** | `/designer/*` | Sorcha.UI.Designer.dll | `/designer`, `/designer/blueprints`, `/designer/blueprints/{id}`, `/designer/templates` | First navigation to `/designer/*` |
| **Explorer** | `/explorer/*` | Sorcha.UI.Explorer.dll | `/explorer`, `/explorer/registers`, `/explorer/transactions/{id}` | First navigation to `/explorer/*` |

**Lazy Loading Behavior:**

1. **Initial Load (Main Bundle):**
   - User navigates to `/` or `/login`
   - Main WASM bundle loads: `Sorcha.UI.Web.Client.dll`, `Sorcha.UI.Shared.dll`, `Sorcha.UI.Core.dll`
   - Size: ~2-3 MB (gzip compressed)

2. **First Module Access:**
   - User navigates to `/admin` (first time)
   - Blazor detects route requires `Sorcha.UI.Admin` assembly
   - **Loading UX:** Full-page loading spinner with progress message: "Loading Admin module..."
   - Download `Sorcha.UI.Admin.dll` (~200-500 KB gzip)
   - Cache assembly in browser (subsequent `/admin/*` navigations are instant)

3. **Subsequent Module Navigations:**
   - Assembly already cached → instant navigation (no loading spinner)

4. **Module Load Failure:**
   - Network error or CDN unavailable during assembly download
   - **Fallback UX:** Error page with retry button
   - Error message: "Failed to load module. Please check your connection and try again."
   - User can retry or navigate back to home

**Loading Spinner Component:**

```razor
<!-- LoadingSpinner.razor -->
<MudOverlay Visible="@IsLoading" DarkBackground="true" ZIndex="9999">
    <MudPaper Class="pa-6" Elevation="4">
        <MudStack Spacing="3" AlignItems="AlignItems.Center">
            <MudProgressCircular Size="Size.Large" Indeterminate="true" Color="Color.Primary" />
            <MudText Typo="Typo.h6">@Message</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">This may take a few seconds...</MudText>
        </MudStack>
    </MudPaper>
</MudOverlay>

@code {
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string Message { get; set; } = "Loading module...";
}
```

**Assembly Dependencies:**

- **Sorcha.UI.Admin** → Depends on: `Sorcha.UI.Core`, `Sorcha.ServiceClients`
- **Sorcha.UI.Designer** → Depends on: `Sorcha.UI.Core`, `Sorcha.Blueprint.Models`, `Z.Blazor.Diagrams`
- **Sorcha.UI.Explorer** → Depends on: `Sorcha.UI.Core`, `Sorcha.Register.Models` (if exists)

Shared dependencies are included in main bundle to avoid duplication across modules.

**Performance Targets:**

- **Main Bundle:** <3 MB (gzip), <10s load on 3G
- **Admin Module:** <500 KB (gzip), <2s load on 3G
- **Designer Module:** <1.5 MB (gzip), <5s load on 3G (Z.Blazor.Diagrams is large but acceptable)
- **Explorer Module:** <500 KB (gzip), <2s load on 3G

---

## Project Structure

### Solution Layout

**Development Priority:** Web-first (WASM), MAUI desktop/mobile deferred to post-MVP

```
src/Apps/Sorcha.UI/                            # UI Application Suite
│
├── Sorcha.UI.sln                              # Solution file
│
├── Sorcha.UI.Web/                             # ASP.NET Core Web Host (PRIMARY FOCUS)
│   ├── Components/
│   │   └── App.razor                          # Root component for web
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── wwwroot/
│   │   ├── js/
│   │   │   └── encryption.js                  # Web Crypto API encryption
│   │   └── index.html
│   ├── appsettings.json
│   ├── Program.cs                             # Web host entry point
│   └── Sorcha.UI.Web.csproj
│
├── Sorcha.UI.Web.Client/                      # Blazor WASM Entry Point (PRIMARY FOCUS)
│   │   ├── Program.cs                         # WASM bootstrapping
│   │   ├── Services/                          # WASM-specific services
│   │   │   ├── FormFactor.cs                  # Browser form factor
│   │   │   └── WasmHttpClientFactory.cs       # WASM HTTP client setup
│   │   └── Sorcha.UI.Web.Client.csproj
│   │
│   ├── Sorcha.UI.Shared/                      # Shared Razor Components (All Platforms)
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   └── NavMenu.razor
│   │   ├── Pages/
│   │   │   ├── Home.razor                     # Landing page (anonymous)
│   │   │   ├── Login.razor                    # Login page (server auth)
│   │   │   ├── Settings.razor                 # Settings page (WASM)
│   │   │   └── NotFound.razor
│   │   ├── Components/
│   │   │   ├── Authentication/
│   │   │   │   ├── LoginDialog.razor
│   │   │   │   ├── ProfileSelector.razor
│   │   │   │   ├── UserProfileMenu.razor
│   │   │   │   └── RedirectToLogin.razor
│   │   │   ├── SystemStatusCard.razor
│   │   │   ├── RecentActivityLog.razor
│   │   │   └── DashboardStatistics.razor
│   │   ├── Services/                          # Platform-agnostic services
│   │   │   ├── IFormFactor.cs                 # Platform abstraction interface
│   │   │   └── SharedStateService.cs          # Shared state management
│   │   ├── Routes.razor                       # Routing configuration
│   │   ├── _Imports.razor
│   │   └── Sorcha.UI.Shared.csproj
│   │
│   ├── Sorcha.UI.Admin/                       # Admin Module (WASM Assembly)
│   │   ├── Pages/
│   │   │   ├── Users/
│   │   │   │   ├── UserList.razor
│   │   │   │   ├── UserEdit.razor
│   │   │   │   └── UserCreate.razor
│   │   │   ├── Configuration/
│   │   │   │   ├── ProfileEditor.razor
│   │   │   │   └── ProfileList.razor
│   │   │   ├── Health/
│   │   │   │   └── ServiceHealthDashboard.razor
│   │   │   └── AdminDashboard.razor
│   │   ├── Components/
│   │   │   ├── ProfileEditorDialog.razor
│   │   │   └── UserTable.razor
│   │   ├── Services/
│   │   │   ├── Configuration/
│   │   │   │   ├── IConfigurationService.cs
│   │   │   │   └── ConfigurationService.cs
│   │   │   └── Health/
│   │   │       └── HealthCheckService.cs
│   │   ├── _Imports.razor
│   │   └── Sorcha.UI.Admin.csproj
│   │
│   ├── Sorcha.UI.Designer/                    # Blueprint Designer Module (WASM Assembly)
│   │   ├── Pages/
│   │   │   ├── BlueprintEditor.razor
│   │   │   ├── BlueprintList.razor
│   │   │   ├── TemplateGallery.razor
│   │   │   └── SchemaEditor.razor
│   │   ├── Components/
│   │   │   ├── DiagramCanvas.razor
│   │   │   ├── ActionEditor.razor
│   │   │   ├── ParticipantEditor.razor
│   │   │   ├── SchemaBuilder.razor
│   │   │   └── ValidationPanel.razor
│   │   ├── Services/
│   │   │   ├── BlueprintEditorService.cs
│   │   │   └── BlueprintValidationService.cs
│   │   ├── _Imports.razor
│   │   └── Sorcha.UI.Designer.csproj
│   │
│   ├── Sorcha.UI.Explorer/                    # Register Explorer Module (WASM Assembly)
│   │   ├── Pages/
│   │   │   ├── RegisterList.razor
│   │   │   ├── RegisterDetail.razor
│   │   │   ├── TransactionList.razor
│   │   │   ├── TransactionDetail.razor
│   │   │   └── BlockExplorer.razor
│   │   ├── Components/
│   │   │   ├── TransactionTable.razor
│   │   │   ├── BlockViewer.razor
│   │   │   ├── ChainVisualizer.razor
│   │   │   └── SearchBar.razor
│   │   ├── Services/
│   │   │   ├── RegisterExplorerService.cs
│   │   │   └── TransactionSearchService.cs
│   │   ├── _Imports.razor
│   │   └── Sorcha.UI.Explorer.csproj
│   │
│   └── Sorcha.UI.Core/                        # Common Library (Models, Interfaces, Shared Logic)
│       ├── Models/
│       │   ├── Authentication/
│       │   │   ├── LoginRequest.cs
│       │   │   ├── TokenResponse.cs
│       │   │   ├── TokenCacheEntry.cs
│       │   │   └── AuthenticationStateInfo.cs
│       │   ├── Configuration/
│       │   │   ├── Profile.cs
│       │   │   ├── UiConfiguration.cs
│       │   │   └── ProfileDefaults.cs
│       │   └── Common/
│       │       ├── ApiResponse.cs
│       │       └── PaginatedList.cs
│       ├── Services/
│       │   ├── Authentication/
│       │   │   ├── IAuthenticationService.cs
│       │   │   ├── AuthenticationService.cs
│       │   │   ├── ITokenCache.cs
│       │   │   ├── BrowserTokenCache.cs      # Web implementation
│       │   │   ├── SecureStorageTokenCache.cs # MAUI implementation
│       │   │   └── CustomAuthenticationStateProvider.cs
│       │   ├── Encryption/
│       │   │   ├── IEncryptionProvider.cs
│       │   │   ├── BrowserEncryptionProvider.cs  # Web Crypto API (WASM)
│       │   │   └── MauiEncryptionProvider.cs     # MAUI secure storage
│       │   └── Http/
│       │       └── AuthenticatedHttpMessageHandler.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       └── Sorcha.UI.Core.csproj
│
├── Sorcha.UI.App/                             # MAUI Application (DEFERRED - Post-MVP)
│   ├── Platforms/
│   │   ├── Android/
│   │   ├── iOS/
│   │   ├── MacCatalyst/
│   │   └── Windows/
│   ├── Resources/
│   ├── MauiProgram.cs                         # MAUI app entry point
│   └── Sorcha.UI.App.csproj                   # MAUI project file
│
├── tests/
│   ├── Sorcha.UI.Core.Tests/
│   ├── Sorcha.UI.Admin.Tests/
│   ├── Sorcha.UI.Designer.Tests/
│   ├── Sorcha.UI.Explorer.Tests/
│   └── Sorcha.UI.Integration.Tests/
│
├── docs/
│   └── sorcha-ui-guide.md
│
└── README.md
```

### Project Descriptions

**MVP Priority:** Focus on Sorcha.UI.Web and Sorcha.UI.Web.Client projects for initial release

| Project | Type | Priority | Purpose | Dependencies |
|---------|------|----------|---------|--------------|
| **Sorcha.UI.Web** | ASP.NET Core | ⭐ MVP | Web host for Blazor WASM (serves static files, handles server auth) | Sorcha.UI.Web.Client |
| **Sorcha.UI.Web.Client** | Blazor WASM | ⭐ MVP | WASM entry point, bootstrapping, DI configuration | Sorcha.UI.Shared, Sorcha.UI.Core |
| **Sorcha.UI.Shared** | Razor Class Library | ⭐ MVP | Shared Razor components (pages, layout, components) | Sorcha.UI.Core |
| **Sorcha.UI.Admin** | Blazor WASM Assembly | ⭐ MVP | Admin module (lazy-loaded) | Sorcha.UI.Core |
| **Sorcha.UI.Designer** | Blazor WASM Assembly | ⭐ MVP | Blueprint Designer module (lazy-loaded) | Sorcha.UI.Core, Sorcha.Blueprint.Models |
| **Sorcha.UI.Explorer** | Blazor WASM Assembly | ⭐ MVP | Register Explorer module (lazy-loaded) | Sorcha.UI.Core |
| **Sorcha.UI.Core** | Class Library | ⭐ MVP | Common models, services, abstractions | Sorcha.ServiceClients |
| **Sorcha.UI.App** | MAUI App | ⏭️ Post-MVP | Desktop/Mobile application (Windows, macOS, iOS, Android) - DEFERRED | Sorcha.UI.Shared, Sorcha.UI.Core |

---

## Authentication & Authorization

### Challenge: Hybrid Authentication Model

**Requirements:**
1. **Anonymous Landing Page:** Home page must be accessible without authentication (server-rendered, fast load)
2. **Server Cookie Authentication:** Login page uses server-side cookie auth for security
3. **WASM JWT Bearer Authentication:** Authenticated WASM components call backend APIs with JWT Bearer tokens
4. **Shared Authentication State:** User's auth state must be accessible in both server and WASM contexts

**Solution: Cookie-to-JWT Bridge Pattern**

### Authentication Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Hybrid Authentication Flow                           │
└─────────────────────────────────────────────────────────────────────────┘

Step 1: Anonymous Access (Server Rendering)
┌──────────────┐
│   Browser    │──► GET /  (Home Page)
└──────────────┘
       │
       ▼
┌──────────────────┐
│ Sorcha.UI.Web    │──► Renders Home.razor (Static SSR or InteractiveServer)
│ (ASP.NET Core)   │    No authentication required
└──────────────────┘
       │
       ▼
User sees landing page with "Login" button


Step 2: Login Flow (Server Cookie Auth)
┌──────────────┐
│   Browser    │──► Click "Login" → Navigate to /login
└──────────────┘
       │
       ▼
┌──────────────────┐
│ Login.razor      │──► InteractiveServer mode
│ (Server-rendered)│    User enters username/password
└──────────────────┘
       │
       ▼
┌──────────────────────────┐
│ AuthenticationService    │──► POST /api/service-auth/token (Tenant Service)
│ (Server-side instance)   │    OAuth2 Password Grant Flow
└──────────────────────────┘
       │
       ▼
┌──────────────────────────┐
│ Tenant Service           │──► Validates credentials, returns JWT tokens
│ (Backend)                │    { access_token, refresh_token, expires_in }
└──────────────────────────┘
       │
       ▼
┌──────────────────────────┐
│ AuthenticationService    │──► 1. Store JWT in encrypted LocalStorage (browser)
│                          │    2. Set HTTP-only cookie with encrypted JWT (server)
│                          │    3. Update server auth state (cookie authentication)
└──────────────────────────┘
       │
       ▼
User is authenticated with both cookie (server) and JWT (browser LocalStorage)


Step 3: Transition to WASM (Cookie → JWT)
┌──────────────┐
│   Browser    │──► Navigate to /blueprints (WASM page)
└──────────────┘
       │
       ▼
┌──────────────────────────┐
│ Sorcha.UI.Web            │──► 1. Validate cookie (server auth state)
│ (Server)                 │    2. Serialize auth state for WASM
│                          │    3. Inject auth state into WASM bootstrap
└──────────────────────────┘
       │
       ▼
┌──────────────────────────┐
│ Blazor WASM Bootstraps   │──► 1. Load WASM runtime
│ (Browser)                │    2. Retrieve JWT from LocalStorage
│                          │    3. Initialize CustomAuthenticationStateProvider
│                          │    4. Parse JWT claims, create ClaimsPrincipal
└──────────────────────────┘
       │
       ▼
┌──────────────────────────┐
│ WASM AuthStateProvider   │──► Authentication state available in WASM context
│                          │    User authenticated, UI updates (show profile menu)
└──────────────────────────┘


Step 4: WASM API Calls (JWT Bearer Auth)
┌──────────────────────────┐
│ BlueprintEditor.razor    │──► User interacts with Blueprint Designer (WASM)
│ (WASM component)         │    Needs to call Blueprint Service API
└──────────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ AuthenticatedHttpHandler     │──► 1. Retrieve JWT from LocalStorage
│ (DelegatingHandler)          │    2. Check token expiration (<5 min → refresh)
│                              │    3. Add Authorization: Bearer {token}
└──────────────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ HttpClient.PostAsync()       │──► POST /api/blueprints (Blueprint Service)
│                              │    Header: Authorization: Bearer eyJhbGc...
└──────────────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ API Gateway / Blueprint Svc  │──► 1. Validate JWT signature
│ (Backend)                    │    2. Extract claims (sub, org_id, roles)
│                              │    3. Authorize request
│                              │    4. Process request, return response
└──────────────────────────────┘
       │
       ▼
WASM component receives API response, updates UI
```

### Key Authentication Components

#### 1. IAuthenticationService

```csharp
public interface IAuthenticationService
{
    // OAuth2 Password Grant Login
    Task<TokenResponse> LoginAsync(LoginRequest request, string profileName);

    // Token Management
    Task<string?> GetAccessTokenAsync(string profileName);
    Task<string?> GetRefreshTokenAsync(string profileName);
    Task<bool> RefreshTokenAsync(string profileName);

    // Logout
    Task LogoutAsync(string profileName);

    // State Queries
    bool IsAuthenticated(string profileName);
    Task<AuthenticationStateInfo> GetAuthenticationInfoAsync();
}
```

**Implementations:**
- **Server-side (Sorcha.UI.Web)**: Uses cookie authentication, stores JWT in encrypted cookie + LocalStorage
- **WASM-side (Sorcha.UI.Web.Client)**: Uses JWT from LocalStorage, no cookies

#### 2. ITokenCache

**Interface:**
```csharp
public interface ITokenCache
{
    Task StoreTokenAsync(string profileName, TokenCacheEntry entry);
    Task<TokenCacheEntry?> GetTokenAsync(string profileName);
    Task RemoveTokenAsync(string profileName);
    Task<bool> HasTokenAsync(string profileName);
}
```

**Implementations:**
- **BrowserTokenCache (Web/WASM)**: Uses `Blazored.LocalStorage` with Web Crypto API encryption
- **SecureStorageTokenCache (MAUI)**: Uses MAUI `SecureStorage` API (OS keychain/credential manager)

#### 3. CustomAuthenticationStateProvider

**Responsibilities:**
- Retrieve JWT from cache (LocalStorage or SecureStorage)
- Parse JWT claims (sub, email, name, roles, org_id, org_name)
- Create `ClaimsPrincipal` for authorization
- Provide `NotifyAuthenticationStateChanged()` for state updates
- **CRITICAL FIX:** No circuit isolation in WASM - state persists naturally across navigation

**Key Difference from Sorcha.Admin:**
- ✅ **WASM Context:** No Blazor Server circuits, state persists in browser memory
- ✅ **Natural State Persistence:** `GetAuthenticationStateAsync()` called on every navigation
- ✅ **LocalStorage Access:** Direct JavaScript interop, no prerendering issues

#### 4. AuthenticatedHttpMessageHandler

**Responsibilities:**
- DelegatingHandler for HTTP client pipeline
- Automatic JWT injection into `Authorization: Bearer {token}` header
- Token expiration check (<5 min → auto-refresh)
- Retry on 401 (Unauthorized) after token refresh
- Handle refresh failures (redirect to login)

### Profile Management & Switching

**Profile Session Model:** Single Active Profile

Sorcha.UI implements a **single active profile** authentication model:

- **One Active Session:** Users can only be authenticated to **one profile at a time**
- **Profile Switch Requires Logout:** Switching from one profile (e.g., Development) to another (e.g., Production) requires:
  1. Logout from current profile (clears JWT tokens, server cookie)
  2. Login to new profile (new OAuth2 authentication flow)
  3. New JWT tokens issued for the new profile

**Rationale:**
- ✅ **Simpler Implementation:** Single token cache, single authentication state
- ✅ **Security:** No concurrent sessions reduces attack surface
- ✅ **Clear UX:** Explicit login/logout makes current profile obvious
- ✅ **OAuth2 Compliance:** Standard single-session model

**Profile Switching UX:**

```csharp
// ProfileSelector.razor - Profile switching workflow
private async Task SwitchProfileAsync(string newProfileName)
{
    // 1. Confirm logout from current profile
    var confirmed = await DialogService.ShowMessageBox(
        "Switch Profile",
        $"You will be logged out of '{_currentProfile}' and redirected to login for '{newProfileName}'. Continue?",
        yesText: "Switch", noText: "Cancel");

    if (confirmed != true) return;

    // 2. Logout from current profile
    await AuthService.LogoutAsync(_currentProfile);

    // 3. Set new active profile (no authentication yet)
    await ConfigService.SetActiveProfileAsync(newProfileName);

    // 4. Redirect to login page for new profile
    Navigation.NavigateTo("/login", forceLoad: true);
}
```

**Token Storage:**
- Tokens stored per-profile: `sorcha:tokens:{profileName}`
- Only one profile's tokens are "active" at a time (determined by `activeProfile` in configuration)
- Previous profile's tokens remain in LocalStorage but are not used after logout

**Future Enhancement (Post-MVP):**
- Optional: Cache tokens for multiple profiles (no re-authentication required on switch)
- Requires: Profile-scoped `AuthenticationStateProvider`, token isolation, security review

### Authentication State Serialization (Server → WASM)

**Problem:** When navigating from server-rendered page (Login) to WASM page (Blueprint Designer), authentication state must transfer.

**Solution:** Use .NET 8+ `PersistentComponentState` and `PersistingComponentStateProvider`:

**Server-side (Sorcha.UI.Web/Program.cs):**
```csharp
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<PersistingComponentStateProvider>();

// Register authentication services
builder.Services.AddScoped<IAuthenticationService, ServerAuthenticationService>();
```

**Server-side (App.razor or persistent component):**
```razor
@inject PersistentComponentState PersistentState
@inject IAuthenticationService AuthService

@code {
    protected override async Task OnInitializedAsync()
    {
        // Serialize auth state for WASM
        PersistentState.RegisterOnPersisting(() =>
        {
            var authInfo = await AuthService.GetAuthenticationInfoAsync();
            PersistentState.PersistAsJson("auth-state", authInfo);
            return Task.CompletedTask;
        });
    }
}
```

**WASM-side (Sorcha.UI.Web.Client/Program.cs):**
```csharp
builder.Services.AddAuthorizationCore();

// Retrieve persisted auth state
var persistedState = builder.Services.BuildServiceProvider()
    .GetRequiredService<PersistentComponentState>();

if (persistedState.TryTakeFromJson<AuthenticationStateInfo>("auth-state", out var authInfo))
{
    // Restore auth state in WASM
    builder.Services.AddScoped(sp => authInfo);
}

builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<CustomAuthenticationStateProvider>());
```

### PersistentComponentState Failure Handling

**Strategy:** Hybrid retry-with-fallback approach for robust authentication state recovery.

**Failure Scenarios:**
- Serialization errors (token size >32KB)
- Browser refresh during state transfer
- Network interruption during WASM bootstrap
- Corrupted persisted state JSON

**Recovery Flow:**

1. **First Attempt:** Retrieve state from `PersistentComponentState`
2. **On Failure:** Display error dialog with retry option (user-friendly)
3. **On Repeated Failure:** Fall back to LocalStorage token recovery (graceful degradation)
4. **Final Fallback:** Redirect to login page

**Implementation:**

```csharp
// Sorcha.UI.Web.Client/Program.cs - WASM Bootstrap with Failure Handling

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddAuthorizationCore();

// Attempt to recover authentication state
var authStateRecovered = false;
var retryCount = 0;
const int maxRetries = 2;

while (!authStateRecovered && retryCount < maxRetries)
{
    try
    {
        // ATTEMPT 1: Retrieve from PersistentComponentState
        var persistedState = builder.Services.BuildServiceProvider()
            .GetRequiredService<PersistentComponentState>();

        if (persistedState.TryTakeFromJson<AuthenticationStateInfo>("auth-state", out var authInfo))
        {
            // Validate auth info before using
            if (authInfo != null && !string.IsNullOrEmpty(authInfo.AccessToken))
            {
                builder.Services.AddScoped(sp => authInfo);
                authStateRecovered = true;
                builder.Logging.AddDebug().Services.BuildServiceProvider()
                    .GetRequiredService<ILogger<Program>>()
                    .LogInformation("✓ Authentication state recovered from PersistentComponentState");
            }
            else
            {
                throw new InvalidOperationException("PersistentComponentState contained invalid auth data");
            }
        }
        else
        {
            throw new InvalidOperationException("PersistentComponentState auth-state key not found");
        }
    }
    catch (Exception ex)
    {
        retryCount++;
        var logger = builder.Logging.AddDebug().Services.BuildServiceProvider()
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(ex, "Failed to recover auth state from PersistentComponentState (attempt {Retry}/{Max})",
            retryCount, maxRetries);

        if (retryCount < maxRetries)
        {
            // RETRY: Show error dialog to user
            await ShowAuthStateRecoveryErrorAsync(retryCount, maxRetries);
        }
        else
        {
            // FALLBACK: Try LocalStorage recovery
            logger.LogWarning("Falling back to LocalStorage recovery after {Count} failed attempts", maxRetries);
            authStateRecovered = await TryRecoverFromLocalStorageAsync(builder.Services);
        }
    }
}

// If all recovery attempts failed, redirect to login
if (!authStateRecovered)
{
    builder.Logging.AddDebug().Services.BuildServiceProvider()
        .GetRequiredService<ILogger<Program>>()
        .LogWarning("All auth state recovery attempts failed. User will be redirected to /login");

    // Register empty auth state (user will see login page)
    builder.Services.AddScoped<AuthenticationStateInfo>(sp => new AuthenticationStateInfo());
}

// Continue with normal WASM initialization
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<CustomAuthenticationStateProvider>());

await builder.Build().RunAsync();


// Helper Methods

async Task ShowAuthStateRecoveryErrorAsync(int attemptNumber, int maxAttempts)
{
    // Note: This would typically use IJSRuntime for dialog, simplified here
    Console.Error.WriteLine($"⚠️ Authentication transfer failed (attempt {attemptNumber}/{maxAttempts}).");
    Console.Error.WriteLine("Please try again or check your connection.");

    // In real implementation, show MudBlazor dialog:
    // await DialogService.ShowMessageBox(
    //     "Authentication Error",
    //     "Failed to transfer authentication state. This may be due to a network issue. Please try again.",
    //     yesText: "Retry", noText: "Cancel");

    await Task.Delay(1000); // Brief delay before retry
}

async Task<bool> TryRecoverFromLocalStorageAsync(IServiceCollection services)
{
    try
    {
        // Build temporary service provider to access LocalStorage
        var sp = services.BuildServiceProvider();
        var tokenCache = sp.GetRequiredService<ITokenCache>();
        var configService = sp.GetRequiredService<IConfigurationService>();

        // Get active profile name
        var activeProfile = await configService.GetActiveProfileNameAsync();

        // Attempt to retrieve tokens from LocalStorage
        var tokenEntry = await tokenCache.GetTokenAsync(activeProfile);

        if (tokenEntry != null && !string.IsNullOrEmpty(tokenEntry.AccessToken))
        {
            // Check token expiration
            if (tokenEntry.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                // Token is valid, create AuthenticationStateInfo
                var authInfo = new AuthenticationStateInfo
                {
                    AccessToken = tokenEntry.AccessToken,
                    RefreshToken = tokenEntry.RefreshToken,
                    ExpiresAt = tokenEntry.ExpiresAt,
                    ProfileName = activeProfile
                };

                services.AddScoped(s => authInfo);

                sp.GetRequiredService<ILogger<Program>>()
                    .LogInformation("✓ Authentication state recovered from LocalStorage (profile: {Profile})", activeProfile);

                return true;
            }
            else
            {
                sp.GetRequiredService<ILogger<Program>>()
                    .LogWarning("LocalStorage token expired (profile: {Profile})", activeProfile);
            }
        }

        return false;
    }
    catch (Exception ex)
    {
        services.BuildServiceProvider().GetRequiredService<ILogger<Program>>()
            .LogError(ex, "LocalStorage recovery failed");
        return false;
    }
}
```

**Token Size Limits:**

To prevent serialization failures, enforce token size limits:

```csharp
// Sorcha.UI.Web/App.razor or persistent component

@inject PersistentComponentState PersistentState
@inject IAuthenticationService AuthService
@inject ILogger<App> Logger

@code {
    private const int TOKEN_SIZE_WARNING_THRESHOLD = 16 * 1024; // 16 KB
    private const int TOKEN_SIZE_MAX_THRESHOLD = 32 * 1024;     // 32 KB

    protected override async Task OnInitializedAsync()
    {
        PersistentState.RegisterOnPersisting(async () =>
        {
            var authInfo = await AuthService.GetAuthenticationInfoAsync();

            // Serialize to JSON to check size
            var json = JsonSerializer.Serialize(authInfo);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);

            if (sizeBytes > TOKEN_SIZE_MAX_THRESHOLD)
            {
                Logger.LogError("Auth state too large to persist ({Size} bytes > {Max} bytes). User will need to re-login.",
                    sizeBytes, TOKEN_SIZE_MAX_THRESHOLD);
                // Don't persist - fallback to LocalStorage recovery will occur
                return;
            }

            if (sizeBytes > TOKEN_SIZE_WARNING_THRESHOLD)
            {
                Logger.LogWarning("Auth state size ({Size} bytes) exceeds warning threshold ({Threshold} bytes)",
                    sizeBytes, TOKEN_SIZE_WARNING_THRESHOLD);
            }

            PersistentState.PersistAsJson("auth-state", authInfo);
        });
    }
}
```

**User Experience:**

| Scenario | UX Behavior |
|----------|-------------|
| **Success (first attempt)** | Seamless transition, no user notification |
| **Failure (first attempt)** | Brief error message: "Authentication transfer failed. Retrying..." (automatic retry after 1s) |
| **Failure (second attempt)** | Fallback to LocalStorage recovery (silent, logged to console) |
| **LocalStorage success** | User authenticated seamlessly, warning logged |
| **All methods fail** | Redirect to `/login` with message: "Your session has expired. Please log in again." |

**Security Considerations:**

- ✅ **No token exposure in error messages** - Errors log generic failure, not token content
- ✅ **Retry limit** - Maximum 2 retry attempts to prevent infinite loops
- ✅ **Fallback validates expiration** - LocalStorage tokens checked for validity before use
- ✅ **Graceful degradation** - Always redirect to login as final fallback (secure default)

### Cookie Authentication (Server-side Only)

**Server-side authentication** uses ASP.NET Core Cookie Authentication for the Login page and server-rendered components.

**Sorcha.UI.Web/Program.cs:**
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();
```

**Login.razor (Server-rendered):**
```razor
@page "/login"
@attribute [AllowAnonymous]
@rendermode InteractiveServer
@inject IAuthenticationService AuthService
@inject NavigationManager Navigation

<MudContainer>
    <MudPaper>
        <EditForm Model="@_loginRequest" OnValidSubmit="HandleLogin">
            <MudTextField @bind-Value="_loginRequest.Username" Label="Username" />
            <MudTextField @bind-Value="_loginRequest.Password" Label="Password" InputType="InputType.Password" />
            <MudButton ButtonType="ButtonType.Submit" Color="Color.Primary">Login</MudButton>
        </EditForm>
    </MudPaper>
</MudContainer>

@code {
    private LoginRequest _loginRequest = new();

    private async Task HandleLogin()
    {
        try
        {
            // 1. Authenticate with Tenant Service (OAuth2 Password Grant)
            var tokenResponse = await AuthService.LoginAsync(_loginRequest, "active-profile");

            // 2. Store JWT in LocalStorage (for WASM access)
            await TokenCache.StoreTokenAsync("active-profile", new TokenCacheEntry
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            });

            // 3. Create server-side cookie authentication
            var claims = ParseJwtClaims(tokenResponse.AccessToken);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                });

            // 4. Navigate to authenticated page (WASM)
            Navigation.NavigateTo("/blueprints", forceLoad: false);
        }
        catch (Exception ex)
        {
            // Handle login error
            Snackbar.Add($"Login failed: {ex.Message}", Severity.Error);
        }
    }
}
```

### JWT Bearer Authentication (WASM API Calls)

**WASM components** use JWT Bearer tokens for all backend API calls.

**Sorcha.UI.Web.Client/Program.cs:**
```csharp
builder.Services.AddScoped<AuthenticatedHttpMessageHandler>();

builder.Services.AddHttpClient("SorchaAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiGateway:BaseUrl"] ?? "https://localhost:7082");
})
.AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("SorchaAPI"));
```

**AuthenticatedHttpMessageHandler:**
```csharp
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly ITokenCache _tokenCache;
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get active profile
        var activeProfile = await _configService.GetActiveProfileAsync();
        if (activeProfile == null)
            return await base.SendAsync(request, cancellationToken);

        // Get access token from cache
        var tokenEntry = await _tokenCache.GetTokenAsync(activeProfile.Name);
        if (tokenEntry == null)
            return await base.SendAsync(request, cancellationToken);

        // Check token expiration
        if (tokenEntry.IsNearExpiration) // < 5 minutes
        {
            await _authService.RefreshTokenAsync(activeProfile.Name);
            tokenEntry = await _tokenCache.GetTokenAsync(activeProfile.Name);
        }

        // Add Authorization header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenEntry.AccessToken);

        // Send request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized (token invalid)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Attempt token refresh
            var refreshed = await _authService.RefreshTokenAsync(activeProfile.Name);
            if (refreshed)
            {
                // Retry request with new token
                tokenEntry = await _tokenCache.GetTokenAsync(activeProfile.Name);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenEntry.AccessToken);
                response = await base.SendAsync(request, cancellationToken);
            }
            else
            {
                // Refresh failed - redirect to login
                // (handled by CustomAuthenticationStateProvider)
            }
        }

        return response;
    }
}
```

### Authorization Architecture: Defense in Depth

**Strategy:** Enforce authorization at **both client-side (UI) and backend (API)** layers for optimal security and user experience.

**Rationale:**
- ✅ **Client-Side UX:** Hide unauthorized features, prevent navigation to forbidden pages, improve user experience
- ✅ **Backend Security:** Enforce authorization at API Gateway and backend services (cannot be bypassed)
- ✅ **Performance:** Avoid unnecessary API calls for actions user is not authorized to perform
- ✅ **Defense in Depth:** Multiple security layers - if one fails, others still protect

**Authorization Enforcement Points:**

| Layer | Mechanism | Purpose | Bypassable? |
|-------|-----------|---------|-------------|
| **1. Client-Side (Blazor WASM)** | `[Authorize]` attribute on pages/components | UX - hide unauthorized features, route guards | ⚠️ Yes (client can be modified) |
| **2. API Gateway** | JWT validation, role extraction from claims | First line of backend defense | ❌ No (server-side) |
| **3. Backend Services** | Service-level authorization policies | Final enforcement, business logic protection | ❌ No (server-side) |

**Roles Defined:**
- **Administrator:** Full system access (user management, config, audit logs, all modules)
- **Designer:** Blueprint Designer access (create/edit blueprints, view explorer)
- **Viewer:** Read-only access (view blueprints, explorer, no editing)

---

#### Client-Side Authorization (UI Layer)

**Page-Level Authorization:**

```razor
<!-- Sorcha.UI.Designer/Pages/BlueprintEditor.razor -->
@page "/designer/blueprints/{id}"
@attribute [Authorize(Roles = "Administrator,Designer")]
@rendermode @(new InteractiveWebAssemblyRenderMode())

<!-- Only accessible to Administrator or Designer roles -->
<!-- If user lacks role, redirected to AccessDenied page -->
```

**Component-Level Authorization (AuthorizeView):**

```razor
<!-- Sorcha.UI.Shared/Layout/MainLayout.razor -->
<MudAppBar>
    <AuthorizeView>
        <Authorized>
            <!-- Show user menu when authenticated -->
            <UserProfileMenu />
        </Authorized>
        <NotAuthorized>
            <!-- Show login button when not authenticated -->
            <MudButton Href="/login" Color="Color.Primary">Sign In</MudButton>
        </NotAuthorized>
    </AuthorizeView>
</MudAppBar>
```

**Role-Based UI Hiding:**

```razor
<!-- Sorcha.UI.Shared/Components/Navigation/NavMenu.razor -->
<MudNavMenu>
    <!-- Everyone can see home -->
    <MudNavLink Href="/" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>

    <!-- Only Administrator sees Admin menu -->
    <AuthorizeView Roles="Administrator">
        <MudNavLink Href="/admin" Icon="@Icons.Material.Filled.AdminPanelSettings">
            Admin
        </MudNavLink>
    </AuthorizeView>

    <!-- Administrator and Designer see Designer menu -->
    <AuthorizeView Roles="Administrator,Designer">
        <MudNavLink Href="/designer" Icon="@Icons.Material.Filled.Design">
            Blueprint Designer
        </MudNavLink>
    </AuthorizeView>

    <!-- All authenticated users see Explorer -->
    <AuthorizeView>
        <Authorized>
            <MudNavLink Href="/explorer" Icon="@Icons.Material.Filled.Search">
                Register Explorer
            </MudNavLink>
        </Authorized>
    </AuthorizeView>
</MudNavMenu>
```

**Programmatic Authorization Checks:**

```csharp
// Sorcha.UI.Designer/Pages/BlueprintList.razor

@inject AuthenticationStateProvider AuthStateProvider

@code {
    private bool _canCreateBlueprints = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        // Check if user has Designer or Administrator role
        _canCreateBlueprints = user.IsInRole("Designer") || user.IsInRole("Administrator");
    }
}

<!-- Conditionally show "Create Blueprint" button -->
@if (_canCreateBlueprints)
{
    <MudButton Color="Color.Primary" OnClick="CreateBlueprint">
        Create Blueprint
    </MudButton>
}
else
{
    <MudAlert Severity="Severity.Info">
        You do not have permission to create blueprints. Contact an administrator.
    </MudAlert>
}
```

---

#### Backend Authorization (API Layer)

**API Gateway JWT Validation (YARP):**

```csharp
// Sorcha.ApiGateway/Program.cs

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"]; // Tenant Service
        options.Audience = "sorcha-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Define policies based on roles
    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole("Administrator"));

    options.AddPolicy("RequireDesigner", policy =>
        policy.RequireRole("Administrator", "Designer"));

    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Apply authorization globally
app.UseAuthentication();
app.UseAuthorization();
```

**Backend Service Authorization (Blueprint Service Example):**

```csharp
// Sorcha.Blueprint.Api/Endpoints/BlueprintEndpoints.cs

public static class BlueprintEndpoints
{
    public static void MapBlueprintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blueprints")
            .RequireAuthorization(); // All endpoints require authentication

        // GET /api/blueprints - Any authenticated user (Viewer, Designer, Administrator)
        group.MapGet("/", async (IBlueprintService service) =>
        {
            var blueprints = await service.GetAllBlueprintsAsync();
            return Results.Ok(blueprints);
        })
        .RequireAuthorization("RequireAuthenticated")
        .WithName("GetBlueprints")
        .WithOpenApi();

        // POST /api/blueprints - Designer or Administrator only
        group.MapPost("/", async (
            CreateBlueprintRequest request,
            IBlueprintService service,
            ClaimsPrincipal user) =>
        {
            // Backend enforces role check (defense in depth)
            if (!user.IsInRole("Designer") && !user.IsInRole("Administrator"))
            {
                return Results.Forbid(); // 403 Forbidden
            }

            var blueprint = await service.CreateBlueprintAsync(request);
            return Results.Created($"/api/blueprints/{blueprint.Id}", blueprint);
        })
        .RequireAuthorization("RequireDesigner") // Policy enforcement
        .WithName("CreateBlueprint")
        .WithOpenApi();

        // DELETE /api/blueprints/{id} - Administrator only
        group.MapDelete("/{id}", async (
            string id,
            IBlueprintService service,
            ClaimsPrincipal user) =>
        {
            // Double-check authorization in code (defense in depth)
            if (!user.IsInRole("Administrator"))
            {
                return Results.Forbid();
            }

            await service.DeleteBlueprintAsync(id);
            return Results.NoContent();
        })
        .RequireAuthorization("RequireAdministrator")
        .WithName("DeleteBlueprint")
        .WithOpenApi();
    }
}
```

**JWT Claims Structure:**

```json
{
  "sub": "user-123",
  "email": "admin@sorcha.local",
  "name": "Admin User",
  "org_id": "org-456",
  "role": ["Administrator"],
  "permissions": ["blueprints:read", "blueprints:write", "users:manage"],
  "iat": 1704556800,
  "exp": 1704558600,
  "iss": "https://sorcha.local/api/service-auth",
  "aud": "sorcha-api"
}
```

---

#### Authorization Flow Example: Creating a Blueprint

**Defense-in-Depth Enforcement:**

```
1. CLIENT-SIDE (Blazor WASM)
   ┌─────────────────────────────────────────────────────────┐
   │ User navigates to /designer/blueprints/create           │
   │ → [Authorize(Roles="Administrator,Designer")] checks:   │
   │    - User authenticated? ✓                               │
   │    - User has Designer or Administrator role? ✓          │
   │    - Allow navigation ✓                                  │
   └─────────────────────────────────────────────────────────┘
              ↓
   ┌─────────────────────────────────────────────────────────┐
   │ User clicks "Save Blueprint"                             │
   │ → Programmatic check: _canCreateBlueprints == true ✓     │
   │ → Button enabled, action proceeds                        │
   └─────────────────────────────────────────────────────────┘
              ↓
2. HTTP REQUEST (WASM → API Gateway)
   ┌─────────────────────────────────────────────────────────┐
   │ POST /api/blueprints                                     │
   │ Authorization: Bearer eyJhbGc...                         │
   └─────────────────────────────────────────────────────────┘
              ↓
3. API GATEWAY (YARP)
   ┌─────────────────────────────────────────────────────────┐
   │ JWT Validation:                                          │
   │  - Signature valid? ✓                                    │
   │  - Not expired? ✓                                        │
   │  - Audience matches "sorcha-api"? ✓                      │
   │  - Extract claims: role=["Designer"] ✓                   │
   └─────────────────────────────────────────────────────────┘
              ↓
   ┌─────────────────────────────────────────────────────────┐
   │ Authorization Policy: "RequireDesigner"                  │
   │  - User has "Designer" or "Administrator" role? ✓        │
   │  - Allow request to proceed ✓                            │
   └─────────────────────────────────────────────────────────┘
              ↓
4. BACKEND SERVICE (Blueprint Service)
   ┌─────────────────────────────────────────────────────────┐
   │ Endpoint: POST /api/blueprints                           │
   │ .RequireAuthorization("RequireDesigner")                 │
   │                                                           │
   │ Manual check in code:                                    │
   │  if (!user.IsInRole("Designer") &&                       │
   │      !user.IsInRole("Administrator"))                    │
   │      return Results.Forbid(); ✓                          │
   │                                                           │
   │ Business logic executes:                                 │
   │  - Create blueprint in database ✓                        │
   │  - Return 201 Created ✓                                  │
   └─────────────────────────────────────────────────────────┘
              ↓
5. RESPONSE
   Blueprint created successfully
```

**If a malicious user bypasses client-side checks:**

```
1. CLIENT-SIDE (Viewer user modifies JavaScript to enable button)
   ⚠️ Button enabled in browser DevTools (client-side bypass)
   → User clicks "Save Blueprint"
   → POST /api/blueprints with Viewer JWT token

2. API GATEWAY
   ✅ JWT valid, but role=["Viewer"]
   ✅ Policy "RequireDesigner" checks roles
   ❌ Viewer not in ["Designer", "Administrator"]
   → Return 403 Forbidden

3. BACKEND SERVICE
   (Not reached - API Gateway already blocked request)

Result: ✅ Attack prevented by backend authorization
```

---

#### Access Denied Handling

**Custom AccessDenied Page:**

```razor
<!-- Sorcha.UI.Web.Client/Pages/AccessDenied.razor -->
@page "/access-denied"

<MudContainer MaxWidth="MaxWidth.Small" Class="mt-8">
    <MudPaper Elevation="4" Class="pa-6">
        <MudStack Spacing="3" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.Lock" Size="Size.Large" Color="Color.Error" />
            <MudText Typo="Typo.h4">Access Denied</MudText>
            <MudText Typo="Typo.body1" Align="Align.Center">
                You do not have permission to view this page.
            </MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary" Align="Align.Center">
                If you believe this is an error, please contact your administrator.
            </MudText>
            <MudButton Href="/" Color="Color.Primary" Variant="Variant.Filled">
                Go to Home
            </MudButton>
        </MudStack>
    </MudPaper>
</MudContainer>
```

**Configure AccessDeniedPath:**

```csharp
// Sorcha.UI.Web.Client/Program.cs

builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Redirect unauthorized users
builder.Services.Configure<AuthorizationOptions>(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### Security Considerations

**1. Token Storage:**
- **Web (WASM):** Encrypted LocalStorage (Web Crypto API, AES-256-GCM)
- **MAUI:** Platform secure storage (Windows Credential Manager, macOS Keychain, iOS Keychain, Android KeyStore)
- **Server Cookie:** HTTP-only, Secure, SameSite=Strict

**2. Token Encryption (Web):**
- Algorithm: AES-256-GCM
- Key Derivation: PBKDF2-SHA-256 (100,000 iterations) from browser fingerprint
- IV: Random 12-byte initialization vector per encryption
- Fallback: Plaintext with "PLAINTEXT:" marker when Web Crypto unavailable (HTTP localhost)

**3. XSS Protection:**
- Content Security Policy (CSP) headers
- Subresource Integrity (SRI) for external libraries
- HTTP-only cookies (server-side)
- Sanitize user input

**4. CSRF Protection:**
- ASP.NET Core Antiforgery tokens for server-rendered forms
- SameSite=Strict cookies
- Validate Origin/Referer headers

**5. Token Lifetime:**
- Access Token: 30 minutes (default)
- Refresh Token: 7 days (default)
- Auto-refresh when <5 minutes remaining
- Logout on refresh failure

---

## Functional Components

### 1. Admin Module (Sorcha.UI.Admin)

**Purpose:** System administration, user management, configuration, health monitoring

**Features:**
- User Management (CRUD users, assign roles)
- Organization Management
- Profile Management (environment configurations)
- Service Health Dashboard (real-time health checks)
- Audit Log Viewer
- System Settings

**Pages:**
- `/admin` - Admin Dashboard
- `/admin/users` - User List
- `/admin/users/{id}` - User Edit
- `/admin/organizations` - Organization List
- `/admin/configuration` - Profile Editor
- `/admin/health` - Service Health Dashboard
- `/admin/audit-logs` - Audit Log Viewer

**Components:**
- `UserTable.razor` - User list with search/filter
- `UserEditDialog.razor` - User CRUD modal
- `ProfileEditorDialog.razor` - Environment profile editor
- `ServiceHealthCard.razor` - Health check status widget
- `AuditLogTable.razor` - Audit log viewer

**Services:**
- `IConfigurationService` / `ConfigurationService` - Profile management
- `IHealthCheckService` / `HealthCheckService` - Service health monitoring
- `IUserManagementService` (future) - User CRUD operations

### 2. Designer Module (Sorcha.UI.Designer)

**Purpose:** Blueprint design, visual workflow editor, schema management

**Features:**
- Visual Blueprint Editor (drag-and-drop workflow designer)
- Blueprint CRUD (Create, Read, Update, Delete)
- Template Gallery (pre-built blueprint templates)
- Schema Editor (JSON Schema builder for data validation)
- Blueprint Validation (real-time validation with portable engine)
- Blueprint Export/Import (JSON/YAML)
- Version Control (blueprint versioning, diff)

**Pages:**
- `/designer` - Blueprint List
- `/designer/blueprints/{id}` - Blueprint Editor
- `/designer/templates` - Template Gallery
- `/designer/schemas` - Schema Editor
- `/designer/validation` - Validation Dashboard

**Components:**
- `DiagramCanvas.razor` - Visual workflow diagram editor (Z.Blazor.Diagrams)
- `ActionEditor.razor` - Action/step configuration panel
- `ParticipantEditor.razor` - Participant configuration panel
- `SchemaBuilder.razor` - JSON Schema builder UI
- `ValidationPanel.razor` - Real-time validation results
- `TemplateCard.razor` - Template preview card
- `BlueprintExportDialog.razor` - Export (JSON/YAML) modal

**Services:**
- `IBlueprintEditorService` / `BlueprintEditorService` - Blueprint state management
- `IBlueprintValidationService` / `BlueprintValidationService` - Validation logic
- `ISchemaManagementService` (future) - Schema CRUD operations

**Dependencies:**
- `Sorcha.Blueprint.Models` - Blueprint domain models
- `Sorcha.Blueprint.Engine` - Portable validation engine
- `Sorcha.Blueprint.Schemas` - Schema management
- `Z.Blazor.Diagrams` - Diagram editor component

### 3. Explorer Module (Sorcha.UI.Explorer)

**Purpose:** Blockchain explorer, transaction viewer, register browser

**Features:**
- Register List (view all registers/blockchains)
- Register Detail (view register metadata, statistics)
- Transaction List (paginated transaction history)
- Transaction Detail (view full transaction data, signatures)
- Block Explorer (view blocks, merkle trees)
- Search Functionality (search by TX ID, wallet address, block hash)
- Data Visualization (transaction graphs, chain visualizations)

**Pages:**
- `/explorer` - Register List
- `/explorer/registers/{id}` - Register Detail
- `/explorer/transactions` - Transaction List
- `/explorer/transactions/{id}` - Transaction Detail
- `/explorer/blocks` - Block Explorer
- `/explorer/search` - Search Interface

**Components:**
- `RegisterTable.razor` - Register list table
- `TransactionTable.razor` - Transaction list with pagination
- `TransactionDetailCard.razor` - Transaction detail viewer
- `BlockViewer.razor` - Block detail viewer
- `ChainVisualizer.razor` - Blockchain visualization (graph/timeline)
- `SearchBar.razor` - Transaction/block/address search

**Services:**
- `IRegisterExplorerService` / `RegisterExplorerService` - Register data fetching
- `ITransactionSearchService` / `TransactionSearchService` - Search logic
- `IBlockchainVisualizationService` (future) - Graph generation

**Dependencies:**
- `Sorcha.Register.Models` - Register/transaction domain models
- `Sorcha.ServiceClients` - Register Service API client

---

## Technology Stack

### Frontend Frameworks

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET MAUI** | 10.0 | Cross-platform desktop/mobile app framework |
| **Blazor WebAssembly** | 10.0 | Web UI framework (browser execution) |
| **Blazor Server** | 10.0 | Server rendering for landing/login pages |
| **Razor Components** | 10.0 | Component model for UI |

### UI Component Libraries

| Library | Version | Purpose |
|---------|---------|---------|
| **MudBlazor** | 8.15.0 | Material Design component library |
| **Z.Blazor.Diagrams** | 3.0.2 | Visual workflow diagram editor (Blueprint Designer) |
| **Blazored.LocalStorage** | 4.5.0 | Browser LocalStorage abstraction |

### Authentication & Security

| Library | Purpose |
|---------|---------|
| **Microsoft.AspNetCore.Components.Authorization** | Blazor authentication/authorization |
| **Microsoft.AspNetCore.Authentication.Cookies** | Server-side cookie authentication |
| **Web Crypto API (SubtleCrypto)** | Browser-based encryption (WASM) |
| **MAUI SecureStorage** | Platform secure storage (desktop/mobile) |

### HTTP & API Integration

| Library | Purpose |
|---------|---------|
| **IHttpClientFactory** | HTTP client factory pattern |
| **System.Net.Http** | HTTP client for API calls |
| **Sorcha.ServiceClients** | Typed API clients for backend services |

### Data Serialization

| Library | Purpose |
|---------|---------|
| **System.Text.Json** | JSON serialization/deserialization |
| **YamlDotNet** | YAML serialization for blueprint export/import |

### Testing Frameworks

| Framework | Purpose |
|-----------|---------|
| **xUnit** | Unit testing framework |
| **bUnit** | Blazor component testing |
| **FluentAssertions** | Assertion library |
| **Moq** | Mocking framework |
| **Playwright** | E2E browser testing |

### Build & Deployment

| Tool | Purpose |
|------|---------|
| **Docker** | Containerization (WASM web host) |
| **docker-compose** | Local development orchestration |
| **.NET Aspire** | Cloud-native orchestration (integration with backend services) |
| **GitHub Actions** | CI/CD pipelines |

---

## Implementation Plan

### Phase 1: Project Setup & Core Infrastructure (Week 1)

**Goals:** Set up solution structure, authentication foundation

**Tasks:**
1. ✅ Create solution structure based on MAUI Blazor Hybrid + Web template
2. ✅ Create all projects (Sorcha.UI, Sorcha.UI.Web, Sorcha.UI.Web.Client, Sorcha.UI.Shared, Sorcha.UI.Core)
3. ✅ Set up project references and dependencies
4. ✅ Configure NuGet packages (MudBlazor, Blazored.LocalStorage, etc.)
5. ✅ Implement core models (LoginRequest, TokenResponse, Profile, etc.)
6. ✅ Implement IAuthenticationService and platform-specific implementations
7. ✅ Implement ITokenCache with BrowserTokenCache and SecureStorageTokenCache
8. ✅ Implement IEncryptionProvider with BrowserEncryptionProvider and MauiEncryptionProvider
9. ✅ Implement CustomAuthenticationStateProvider
10. ✅ Implement AuthenticatedHttpMessageHandler
11. ✅ Set up DI configuration in Program.cs (Web and WASM)
12. ✅ Test basic authentication flow (login, token storage, logout)

**Deliverables:**
- ✅ Compiling solution with all projects
- ✅ Authentication infrastructure functional (login/logout)
- ✅ JWT token storage working (LocalStorage for WASM, SecureStorage for MAUI)

### Phase 2: Shared Components & Layout (Week 2)

**Goals:** Migrate Sorcha.Admin shared components, implement layout

**Tasks:**
1. ✅ Create MainLayout.razor (AppBar, drawer, content area)
2. ✅ Create NavMenu.razor (side navigation)
3. ✅ Create Home.razor (landing page, anonymous access)
4. ✅ Create Login.razor (server-rendered login page)
5. ✅ Create Settings.razor (WASM settings page)
6. ✅ Create authentication components:
   - LoginDialog.razor
   - ProfileSelector.razor
   - UserProfileMenu.razor
   - RedirectToLogin.razor
7. ✅ Create dashboard components:
   - SystemStatusCard.razor
   - RecentActivityLog.razor
   - DashboardStatistics.razor
8. ✅ Implement IConfigurationService and ConfigurationService
9. ✅ Implement Profile management (CRUD operations)
10. ✅ Set up MudBlazor providers (Theme, Popover, Dialog, Snackbar)
11. ✅ Test navigation between anonymous (Home) and authenticated (Settings) pages
12. ✅ Verify authentication state persistence across navigation

**Deliverables:**
- ✅ Functional layout with navigation
- ✅ Landing page and login page working
- ✅ Authentication state displaying correctly in UI
- ✅ Profile management functional

### Phase 3: Admin Module (Week 3)

**Goals:** Implement Admin module with user management and health monitoring

**Tasks:**
1. ⏭️ Create Sorcha.UI.Admin project
2. ⏭️ Implement AdminDashboard.razor (admin home page)
3. ⏭️ Implement configuration pages:
   - ProfileList.razor (list all profiles)
   - ProfileEditor.razor (CRUD profile editor)
4. ⏭️ Implement health monitoring pages:
   - ServiceHealthDashboard.razor (health check dashboard)
   - ServiceHealthCard.razor (individual service health widget)
5. ⏭️ Implement IHealthCheckService / HealthCheckService
6. ⏭️ Implement user management pages (placeholders for MVP):
   - UserList.razor
   - UserEdit.razor
7. ⏭️ Configure lazy loading for Admin assembly
8. ⏭️ Test admin module in WASM context
9. ⏭️ Verify JWT Bearer authentication for admin API calls

**Deliverables:**
- ⏭️ Functional Admin module (lazy-loaded WASM assembly)
- ⏭️ Profile management UI complete
- ⏭️ Service health monitoring working

### Phase 4: Designer Module (Week 4-5)

**Goals:** Implement Blueprint Designer with visual editor

**Tasks:**
1. ⏭️ Create Sorcha.UI.Designer project
2. ⏭️ Implement BlueprintList.razor (list all blueprints)
3. ⏭️ Implement BlueprintEditor.razor (visual workflow editor)
4. ⏭️ Integrate Z.Blazor.Diagrams for visual diagramming
5. ⏭️ Implement editor panels:
   - ActionEditor.razor (action configuration)
   - ParticipantEditor.razor (participant configuration)
   - SchemaBuilder.razor (JSON Schema builder)
6. ⏭️ Implement validation:
   - ValidationPanel.razor (real-time validation results)
   - BlueprintValidationService (integrate portable engine)
7. ⏭️ Implement template gallery:
   - TemplateGallery.razor (template browser)
   - TemplateCard.razor (template preview)
8. ⏭️ Implement export/import:
   - BlueprintExportDialog.razor (export JSON/YAML)
   - Blueprint import functionality
9. ⏭️ Configure lazy loading for Designer assembly
10. ⏭️ Test Blueprint Designer end-to-end (create, edit, validate, save)

**Deliverables:**
- ⏭️ Functional Blueprint Designer module
- ⏭️ Visual workflow editor working
- ⏭️ Blueprint validation integrated
- ⏭️ Template gallery functional

### Phase 5: Explorer Module (Week 6)

**Goals:** Implement Register Explorer for blockchain browsing

**Tasks:**
1. ⏭️ Create Sorcha.UI.Explorer project
2. ⏭️ Implement RegisterList.razor (list all registers)
3. ⏭️ Implement RegisterDetail.razor (register metadata, statistics)
4. ⏭️ Implement transaction viewing:
   - TransactionList.razor (paginated transaction history)
   - TransactionDetail.razor (full transaction data viewer)
5. ⏭️ Implement block explorer:
   - BlockExplorer.razor (block browser)
   - BlockViewer.razor (block detail viewer)
6. ⏭️ Implement search:
   - SearchBar.razor (search interface)
   - TransactionSearchService (search logic)
7. ⏭️ Implement data visualization:
   - ChainVisualizer.razor (blockchain graph/timeline)
8. ⏭️ Configure lazy loading for Explorer assembly
9. ⏭️ Test Explorer module end-to-end

**Deliverables:**
- ⏭️ Functional Register Explorer module
- ⏭️ Transaction browsing working
- ⏭️ Block explorer functional
- ⏭️ Search functionality working

### Phase 6: Testing & Documentation (Week 7)

**Goals:** Comprehensive testing and documentation

**Tasks:**
1. ⏭️ Write unit tests for Sorcha.UI.Core services
2. ⏭️ Write component tests (bUnit) for shared components
3. ⏭️ Write integration tests for authentication flow
4. ⏭️ Write E2E tests (Playwright) for critical user workflows:
   - Login → Logout
   - Profile switching
   - Blueprint creation → Save
   - Transaction browsing
5. ⏭️ Performance testing (lazy loading, WASM bundle size)
6. ⏭️ Security testing (token encryption, XSS, CSRF)
7. ⏭️ Documentation:
   - Update README.md
   - Create Sorcha.UI User Guide
   - Update MASTER-TASKS.md
   - Update development-status.md
8. ⏭️ Code review and refactoring

**Deliverables:**
- ⏭️ Test coverage >85%
- ⏭️ Comprehensive documentation
- ⏭️ Production-ready codebase

### Phase 7: Deployment & Production Hardening (Week 8)

**Goals:** Deploy to production, harden security

**Tasks:**
1. ⏭️ Configure HTTPS for production deployment
2. ⏭️ Set up Docker containerization for WASM web host
3. ⏭️ Configure docker-compose.yml for Sorcha.UI.Web
4. ⏭️ Update .NET Aspire orchestration (Sorcha.AppHost) to include Sorcha.UI
5. ⏭️ Implement Content Security Policy (CSP) headers
6. ⏭️ Implement Subresource Integrity (SRI) for external libraries
7. ⏭️ Configure SSL certificate verification
8. ⏭️ Set up CI/CD pipelines (GitHub Actions)
9. ⏭️ Performance optimization (bundle size reduction, lazy loading tuning)
10. ⏭️ Production deployment to staging environment
11. ⏭️ Smoke testing in staging
12. ⏭️ Production deployment

**Deliverables:**
- ⏭️ Production deployment of Sorcha.UI (Web)
- ⏭️ Hardened security configuration
- ⏭️ CI/CD pipelines operational

### Phase 8 (Post-MVP): Desktop & Mobile Support

**Goals:** Enable MAUI desktop and mobile deployments

**Tasks:**
1. ⏭️ Test MAUI app on Windows Desktop
2. ⏭️ Test MAUI app on macOS Desktop
3. ⏭️ Test MAUI app on iOS
4. ⏭️ Test MAUI app on Android
5. ⏭️ Implement platform-specific features:
   - Biometric authentication (iOS Face ID, Android fingerprint)
   - Camera integration (QR code scanning)
   - Offline sync (local database)
6. ⏭️ Publish to app stores (Microsoft Store, macOS App Store, Google Play, Apple App Store)

**Deliverables:**
- ⏭️ Desktop apps (Windows, macOS)
- ⏭️ Mobile apps (iOS, Android)
- ⏭️ App store listings

---

## Security Considerations

### 1. Authentication Security

**OAuth2 Password Grant Flow:**
- ✅ Use TLS/HTTPS for all token requests
- ✅ Short token lifetimes (30 minutes)
- ✅ Automatic token refresh
- ✅ Secure token revocation on logout
- ⚠️ Password Grant deprecated in OAuth2.1 - consider migration to Authorization Code + PKCE post-MVP

**Token Storage:**
- **Web (WASM):**
  - ✅ Encrypted LocalStorage (AES-256-GCM via Web Crypto API)
  - ✅ Key derivation: PBKDF2-SHA-256 (100k iterations) from browser fingerprint
  - ⚠️ Vulnerable to XSS if malicious script executes
  - ⚠️ Mitigate with Content Security Policy (CSP)
- **MAUI (Desktop/Mobile):**
  - ✅ Platform secure storage (OS keychain/credential manager)
  - ✅ Windows: Credential Manager (DPAPI)
  - ✅ macOS: Keychain
  - ✅ iOS: iOS Keychain
  - ✅ Android: Android KeyStore
  - ✅ Superior security compared to browser LocalStorage

**Server Cookie Authentication:**
- ✅ HTTP-only cookies (not accessible to JavaScript)
- ✅ Secure flag (HTTPS only)
- ✅ SameSite=Strict (CSRF protection)
- ✅ Short expiration (30 minutes, sliding)

### 2. XSS Protection

**Content Security Policy (CSP):**
```http
Content-Security-Policy:
    default-src 'self';
    script-src 'self' 'wasm-unsafe-eval';
    style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
    font-src 'self' https://fonts.gstatic.com;
    img-src 'self' data: https:;
    connect-src 'self' https://localhost:7080 https://localhost:7082;
    frame-ancestors 'none';
    base-uri 'self';
    form-action 'self';
```

**Input Sanitization:**
- ✅ Use Blazor's built-in XSS protection (automatic HTML encoding)
- ✅ Validate all user input (client-side and server-side)
- ✅ Use `DataAnnotations` for model validation
- ⚠️ Be cautious with `@((MarkupString)userInput)` - only use for trusted content

**Subresource Integrity (SRI):**
- ✅ Use SRI hashes for external libraries (MudBlazor, fonts, etc.)
- ✅ Prevent CDN compromise attacks

### 3. CSRF Protection

**Server-side Forms:**
- ✅ ASP.NET Core Antiforgery tokens (automatic in Blazor Server)
- ✅ `@Html.AntiForgeryToken()` in forms
- ✅ Validate antiforgery token on POST requests

**WASM API Calls:**
- ✅ SameSite=Strict cookies prevent CSRF on cookie auth
- ✅ JWT Bearer tokens immune to CSRF (no cookies involved in WASM API calls)
- ✅ Validate Origin/Referer headers on backend

### 4. Token Lifetime & Refresh

**Access Token:**
- Lifetime: 30 minutes (default)
- Auto-refresh: When <5 minutes remaining
- Storage: Encrypted LocalStorage (WASM) or SecureStorage (MAUI)

**Refresh Token:**
- Lifetime: 7 days (default)
- Single-use: Backend invalidates after refresh
- Rotation: New refresh token issued on each refresh

**Logout:**
- ✅ Clear all tokens from LocalStorage/SecureStorage
- ✅ Clear server-side cookie
- ✅ Backend token revocation (invalidate refresh token)

### 5. HTTPS Enforcement

**Production:**
- ✅ Enforce HTTPS for all connections
- ✅ HSTS headers (Strict-Transport-Security)
- ✅ Redirect HTTP → HTTPS
- ✅ Valid CA-signed SSL certificates

**Development:**
- ⚠️ Self-signed certificates allowed (localhost)
- ⚠️ SSL verification can be disabled for dev profiles
- ✅ Use `mkcert` for locally-trusted dev certificates

### 6. Dependency Security

**Package Management:**
- ✅ Use only trusted NuGet packages
- ✅ Pin package versions (avoid wildcards)
- ✅ Regular security audits (`dotnet list package --vulnerable`)
- ✅ Keep dependencies up-to-date

**Web Crypto API:**
- ✅ Only available in secure contexts (HTTPS or localhost)
- ✅ Fallback to plaintext with warning if unavailable
- ✅ User notification when encryption disabled

---

## Migration from Sorcha.Admin

### Migration Strategy

**Approach:** Incremental migration with parallel operation

**Steps:**
1. ✅ Create new Sorcha.UI solution (separate from Sorcha.Admin)
2. ✅ Copy-paste core services (Authentication, Configuration, Encryption) to Sorcha.UI.Core
3. ✅ Refactor to remove Blazor Server-specific code (circuit workarounds, prerendering fixes)
4. ✅ Copy-paste Razor components to Sorcha.UI.Shared
5. ✅ Update render modes (`@rendermode` directives)
6. ✅ Test authentication flow in WASM context
7. ⏭️ Migrate Blueprint Designer components to Sorcha.UI.Designer
8. ⏭️ Test feature parity with Sorcha.Admin
9. ⏭️ Deploy Sorcha.UI alongside Sorcha.Admin (parallel operation)
10. ⏭️ User acceptance testing (UAT)
11. ⏭️ Cutover to Sorcha.UI (deprecate Sorcha.Admin)
12. ⏭️ Archive Sorcha.Admin repository

### Components to Migrate

| Sorcha.Admin Component | Sorcha.UI Destination | Changes Required |
|------------------------|----------------------|------------------|
| `Services/Authentication/AuthenticationService.cs` | `Sorcha.UI.Core/Services/Authentication/` | ✅ Remove circuit workarounds |
| `Services/Authentication/BrowserTokenCache.cs` | `Sorcha.UI.Core/Services/Authentication/` | ✅ No changes |
| `Services/Authentication/CustomAuthenticationStateProvider.cs` | `Sorcha.UI.Core/Services/Authentication/` | ✅ Remove `NotifyAuthenticationStateChanged()` workarounds |
| `Services/Encryption/BrowserEncryptionProvider.cs` | `Sorcha.UI.Core/Services/Encryption/` | ✅ No changes |
| `Services/Configuration/ConfigurationService.cs` | `Sorcha.UI.Core/Services/Configuration/` | ✅ No changes |
| `Services/Http/AuthenticatedHttpMessageHandler.cs` | `Sorcha.UI.Core/Services/Http/` | ✅ No changes |
| `Models/Authentication/*` | `Sorcha.UI.Core/Models/Authentication/` | ✅ No changes |
| `Models/Configuration/*` | `Sorcha.UI.Core/Models/Configuration/` | ✅ No changes |
| `Pages/Home.razor` | `Sorcha.UI.Shared/Pages/Home.razor` | ✅ Remove `@rendermode InteractiveServer`, add Static SSR |
| `Pages/Login.razor` | `Sorcha.UI.Shared/Pages/Login.razor` | ✅ Keep `@rendermode InteractiveServer` |
| `Pages/Settings.razor` | `Sorcha.UI.Shared/Pages/Settings.razor` | ✅ Change to `@rendermode InteractiveWebAssembly` |
| `Layout/MainLayout.razor` | `Sorcha.UI.Shared/Layout/MainLayout.razor` | ✅ Remove prerendering workarounds |
| `Layout/NavMenu.razor` | `Sorcha.UI.Shared/Layout/NavMenu.razor` | ✅ No changes |
| `Components/Authentication/*` | `Sorcha.UI.Shared/Components/Authentication/` | ✅ Remove circuit-related code |
| `Components/SystemStatusCard.razor` | `Sorcha.UI.Shared/Components/SystemStatusCard.razor` | ✅ Remove prerendering workarounds |
| `wwwroot/js/encryption.js` | `Sorcha.UI.Web/wwwroot/js/encryption.js` | ✅ No changes |

### Breaking Changes from Sorcha.Admin

**Architecture:**
- ❌ **Removed:** Blazor Server mode for authenticated pages
- ✅ **Added:** Blazor WASM for authenticated pages
- ❌ **Removed:** Circuit-based state management
- ✅ **Added:** Browser memory-based state management (WASM)

**Authentication:**
- ✅ **No Breaking Changes:** OAuth2 Password Grant flow identical
- ✅ **Improved:** Authentication state persistence (fixes Sorcha.Admin bug)
- ❌ **Removed:** Server-side circuit auth state workarounds

**Configuration:**
- ✅ **No Breaking Changes:** Profile model identical
- ✅ **Compatible:** Sorcha.Admin configuration can be imported

**Deployment:**
- ❌ **Removed:** Blazor Server hosting
- ✅ **Added:** Blazor WASM hosting (ASP.NET Core static file server)
- ✅ **Added:** MAUI desktop/mobile hosting (future)

### Migration Validation Checklist

**Authentication:**
- [ ] Login flow works (OAuth2 Password Grant)
- [ ] JWT tokens stored in encrypted LocalStorage
- [ ] Authentication state displays in UI (top bar, navigation)
- [ ] Authentication state persists across navigation
- [ ] Token auto-refresh works (<5 min expiration)
- [ ] Logout clears tokens and redirects to login
- [ ] Profile switching works (multi-environment support)

**Configuration:**
- [ ] Profile management CRUD works
- [ ] Default profiles initialized correctly
- [ ] Custom profiles can be created
- [ ] Configuration persists in LocalStorage
- [ ] Active profile selection works

**UI Components:**
- [ ] Landing page renders (anonymous access)
- [ ] Login page renders
- [ ] Authenticated pages require login
- [ ] Navigation works (drawer, top bar)
- [ ] MudBlazor components render correctly
- [ ] Responsive design works (mobile/tablet/desktop)

**Functional Modules:**
- [ ] Admin module loads (lazy-loaded)
- [ ] Designer module loads (lazy-loaded)
- [ ] Explorer module loads (lazy-loaded)
- [ ] All module features functional

**Performance:**
- [ ] Initial load time <3 seconds (WASM bundle)
- [ ] Lazy-loaded assemblies load on-demand
- [ ] No memory leaks (browser DevTools profiling)
- [ ] Smooth interactions (60fps)

**Security:**
- [ ] HTTPS enforced in production
- [ ] CSP headers configured
- [ ] Antiforgery tokens validated
- [ ] XSS protection verified
- [ ] Token encryption working (Web Crypto API)

---

## Architecture Decisions

### Resolved Questions (Implementation Approved)

1. **✅ Authentication: Cookie-to-JWT Bridge Implementation**
   - **Question:** How should we serialize authentication state from server cookie to WASM JWT?
   - **APPROVED DECISION:** **Option A** - Use `PersistentComponentState` to serialize claims
   - **Rationale:** Official .NET pattern for server → WASM state transfer, well-documented, robust
   - **Implementation:** Phase 1 (Week 1)

2. **✅ Authorization: Role-Based Access Control (RBAC)**
   - **Question:** How granular should role-based access be?
   - **APPROVED DECISION:** **Option A** - Simple roles (Administrator, Designer, Viewer)
   - **Rationale:** Sufficient for MVP, can be extended to fine-grained permissions post-MVP
   - **Implementation:** Phase 3 (Admin Module)

3. **✅ Module Lazy Loading: Assembly Size Limits**
   - **Question:** What is the acceptable WASM assembly size for each module?
   - **APPROVED DECISION:** No strict size limit for Designer module
   - **Rationale:** Designer is for limited users, Z.Blazor.Diagrams local functionality is valuable, performance trade-off acceptable
   - **Implementation:** Phase 4 (Designer Module)

4. **✅ Offline Support: Scope for MVP**
   - **Question:** Should MVP support offline mode for WASM?
   - **APPROVED DECISION:** **Option A** - No offline support in MVP (defer to post-MVP)
   - **Rationale:** Focus on online functionality first, offline support adds complexity without immediate benefit
   - **Implementation:** Post-MVP (Phase 8+)

5. **✅ Project Structure**
   - **Question:** How should projects be organized?
   - **APPROVED DECISION:** Web-first emphasis, MAUI app in `Sorcha.UI.App` subdirectory (deferred)
   - **Rationale:** Primary focus on web deployment for immediate use, MAUI desktop/mobile capabilities deferred to post-MVP
   - **Implementation:** Phase 1 (Project Setup)

### Open Questions (Deferred to Post-MVP)

6. **❓ MAUI Platform Priority**
   - **Question:** Which desktop/mobile platforms should be prioritized post-MVP?
   - **Options:**
     - A) Windows Desktop (most users)
     - B) macOS Desktop (developer preference)
     - C) iOS/Android Mobile (future vision)
   - **Decision Needed By:** Phase 8 (Post-MVP)

7. **❓ PWA Support**
   - **Question:** Should Sorcha.UI be a Progressive Web App?
   - **Benefits:** Installable, offline, push notifications
   - **Effort:** Medium (service worker, manifest.json)
   - **Decision Needed By:** Phase 7 (Deployment)

8. **❓ Real-Time Features**
   - **Question:** Should multi-user blueprint editing be real-time (SignalR)?
   - **Options:**
     - A) No real-time (defer to post-MVP)
     - B) Real-time notifications only (blueprint saved, user online)
     - C) Full real-time collaboration (OT/CRDT)
   - **Decision Needed By:** Post-MVP

9. **❓ Multi-Tenancy**
   - **Question:** How should multi-tenant isolation work in UI?
   - **Current State:** Tenant Service provides org_id in JWT
   - **UI Impact:** Data filtering, organization switcher
   - **Decision Needed By:** Phase 3 (Admin Module)

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| **MAUI** | .NET Multi-platform App UI - cross-platform framework for desktop/mobile apps |
| **WASM** | WebAssembly - binary instruction format for browser execution |
| **SSR** | Server-Side Rendering - HTML generated on server, sent to browser |
| **CSR** | Client-Side Rendering - HTML generated in browser (WASM) |
| **CSP** | Content Security Policy - HTTP header to prevent XSS attacks |
| **CSRF** | Cross-Site Request Forgery - attack where malicious site exploits user's cookies |
| **XSS** | Cross-Site Scripting - attack where malicious script executes in user's browser |
| **JWT** | JSON Web Token - compact token format for authentication |
| **OAuth2** | Authorization framework for delegated access (RFC 6749) |
| **PBKDF2** | Password-Based Key Derivation Function 2 - key derivation algorithm |
| **AES-GCM** | Advanced Encryption Standard - Galois/Counter Mode - authenticated encryption |
| **PKCE** | Proof Key for Code Exchange - OAuth2 extension for public clients |

---

## Appendix B: References

**Internal Documentation:**
- [Sorcha.Admin README](../../src/Apps/Sorcha.Admin/README.md)
- [Sorcha.Admin Feature Requirements](../../src/Apps/Sorcha.Admin/FEATURE-REQUIREMENTS.md)
- [Sorcha.Admin Known Issues](../../src/Apps/Sorcha.Admin/KNOWN-ISSUES.md)
- [Sorcha Constitution](../constitution.md)
- [Sorcha Architecture](../../docs/architecture.md)

**External References:**
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [Blazor WebAssembly Documentation](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [OAuth2 Password Grant (RFC 6749)](https://datatracker.ietf.org/doc/html/rfc6749#section-4.3)
- [Web Crypto API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Crypto_API)
- [MAUI SecureStorage](https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage)
- [MudBlazor Documentation](https://mudblazor.com/)
- [Z.Blazor.Diagrams Documentation](https://blazor-diagrams.zzzprojects.com/)

---

**Document Status:** ✅ Approved - Ready for Implementation

**Architecture Decisions Approved (2026-01-06):**
- ✅ Authentication: PersistentComponentState pattern
- ✅ Authorization: Simple roles (Administrator, Designer, Viewer)
- ✅ Module Size: No strict limit for Designer (Z.Blazor.Diagrams valuable)
- ✅ Offline Support: Not needed for MVP
- ✅ Project Structure: Web-first, MAUI in subdirectory (deferred)

**Next Steps:**
1. ✅ Architecture decisions approved
2. ⏭️ Begin Phase 1 implementation (Project setup & authentication infrastructure)
3. ⏭️ Create project structure in `src/Apps/Sorcha.UI/`
4. ⏭️ Migrate authentication services from Sorcha.Admin
5. ⏭️ Implement Cookie → JWT authentication bridge
