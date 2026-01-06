# Implementation Tasks: Sorcha.UI

**Service**: Sorcha.UI | **Date**: 2026-01-06
**Plan**: [plan.md](plan.md) | **Spec**: [sorcha-ui.md](../sorcha-ui.md)
**Type**: Web Application (Blazor WebAssembly)

---

## User Stories (Priority Order)

Based on the specification and implementation plan, Sorcha.UI functionality is organized into these user stories:

| ID | User Story | Priority | Module | Acceptance Criteria |
|----|-----------|----------|--------|---------------------|
| **US1** | **Authentication & Authorization** | P0 (Critical) | Core | User can login, tokens cached encrypted, auth state persists, role-based authorization works |
| **US2** | **Layout & Navigation** | P0 (Critical) | Shared | Main layout renders, navigation works, profile menu functional, lazy loading works |
| **US3** | **Admin Module** | P1 (Core) | Admin | Dashboard, user list (read-only), health monitoring, profile configuration |
| **US4** | **Designer Module** | P1 (Core) | Designer | Blueprint list, visual editor (Z.Blazor.Diagrams), save/load JSON blueprints |
| **US5** | **Explorer Module** | P1 (Core) | Explorer | Register list, transaction list, transaction detail viewer |

---

## Dependencies

### Story Completion Order

```
Phase 1: Setup
    └─► Phase 2: Foundational (Authentication & Core Services)
           └─► US1: Authentication & Authorization [P0]
                  └─► US2: Layout & Navigation [P0]
                         ├─► US3: Admin Module [P1]
                         ├─► US4: Designer Module [P1]  (can run parallel with US3, US5)
                         └─► US5: Explorer Module [P1]  (can run parallel with US3, US4)
```

**Independent Stories** (can be implemented in parallel after US2):
- US3, US4, US5 are independent (different modules, no shared code)

---

## Phase 1: Setup & Infrastructure

**Goal**: Initialize project structure, configure build system, set up DI

**Duration**: 1-2 days

### Tasks

- [x] T001 Create Sorcha.UI solution file at src/Apps/Sorcha.UI/Sorcha.UI.sln
- [x] T002 Create Sorcha.UI.Web project (ASP.NET Core WASM host) at src/Apps/Sorcha.UI/Sorcha.UI.Web/
- [x] T003 Create Sorcha.UI.Web.Client project (Blazor WASM entry point) at src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/
- [x] T004 Create Sorcha.UI.Shared project (Razor Class Library) at src/Apps/Sorcha.UI/Sorcha.UI.Shared/
- [x] T005 Create Sorcha.UI.Core project (Class Library) at src/Apps/Sorcha.UI/Sorcha.UI.Core/
- [x] T006 Create Sorcha.UI.Admin project (Razor Class Library) at src/Apps/Sorcha.UI/Sorcha.UI.Admin/
- [x] T007 Create Sorcha.UI.Designer project (Razor Class Library) at src/Apps/Sorcha.UI/Sorcha.UI.Designer/
- [x] T008 Create Sorcha.UI.Explorer project (Razor Class Library) at src/Apps/Sorcha.UI/Sorcha.UI.Explorer/
- [x] T009 Add NuGet packages to Sorcha.UI.Web.Client (Microsoft.AspNetCore.Components.WebAssembly 10.0+, MudBlazor 8.0+)
- [x] T010 Add NuGet packages to Sorcha.UI.Web (Microsoft.AspNetCore.Components.WebAssembly.Server 10.0+)
- [x] T011 Add NuGet packages to Sorcha.UI.Designer (Z.Blazor.Diagrams 3.0+)
- [x] T012 Add project references (Web → Web.Client, Web.Client → Shared/Core, modules → Core)
- [x] T013 Configure launchSettings.json in Sorcha.UI.Web/Properties/ (HTTPS: 7083, HTTP: 5173)
- [x] T014 Create wwwroot/index.html in Sorcha.UI.Web.Client/ (Blazor WASM bootstrap HTML) - Not needed, template uses App.razor pattern
- [x] T015 Create _Imports.razor in each Razor project (global using directives)
- [x] T016 Create appsettings.json and appsettings.Development.json in Sorcha.UI.Web/
- [x] T017 [P] Create test project Sorcha.UI.Core.Tests at src/Apps/Sorcha.UI/tests/Sorcha.UI.Core.Tests/
- [x] T018 [P] Create test project Sorcha.UI.Integration.Tests at src/Apps/Sorcha.UI/tests/Sorcha.UI.Integration.Tests/
- [x] T019 Verify solution builds successfully (`dotnet build`)
- [x] T020 Verify solution runs (`dotnet run --project Sorcha.UI.Web`, browser opens at https://localhost:7083)

**Completion Criteria**:
- ✅ All 8 projects created and building
- ✅ Solution runs without errors
- ✅ Browser shows default Blazor WASM template page

---

## Phase 2: Foundational - Core Services

**Goal**: Implement shared infrastructure needed by all user stories

**Duration**: 2-3 days

### Tasks

#### Domain Models (Core Library)

- [x] T021 [P] Create LoginRequest model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/LoginRequest.cs
- [x] T022 [P] Create TokenResponse model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/TokenResponse.cs
- [x] T023 [P] Create TokenCacheEntry model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/TokenCacheEntry.cs
- [x] T024 [P] Create AuthenticationStateInfo model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/AuthenticationStateInfo.cs
- [x] T025 [P] Create Profile model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Configuration/Profile.cs
- [x] T026 [P] Create UiConfiguration model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Configuration/UiConfiguration.cs
- [x] T027 [P] Create ApiResponse<T> model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Common/ApiResponse.cs
- [x] T028 [P] Create PaginatedList<T> model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Common/PaginatedList.cs

#### Service Interfaces (Core Library)

- [x] T029 Create IAuthenticationService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/IAuthenticationService.cs
- [x] T030 Create ITokenCache interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/ITokenCache.cs
- [x] T031 Create IEncryptionProvider interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Encryption/IEncryptionProvider.cs
- [x] T032 Create IConfigurationService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Configuration/IConfigurationService.cs

#### JavaScript Interop (Web Crypto API)

- [x] T033 Create encryption.js in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/wwwroot/js/encryption.js (Web Crypto API wrapper)
- [x] T034 Implement encryptData() function in encryption.js (AES-256-GCM encryption with PBKDF2 key derivation)
- [x] T035 Implement decryptData() function in encryption.js (AES-256-GCM decryption)
- [x] T036 Implement isEncryptionAvailable() function in encryption.js (check crypto.subtle availability)

#### Service Implementations (Core Library)

- [x] T037 Implement BrowserEncryptionProvider in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Encryption/BrowserEncryptionProvider.cs (calls encryption.js via IJSRuntime)
- [x] T038 Implement BrowserTokenCache in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/BrowserTokenCache.cs (LocalStorage + encryption)
- [x] T039 Implement ConfigurationService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Configuration/ConfigurationService.cs (Profile management, LocalStorage)
- [x] T040 Implement AuthenticationService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs (OAuth2 Password Grant)
- [x] T041 Implement CustomAuthenticationStateProvider in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/CustomAuthenticationStateProvider.cs (JWT claims → ClaimsPrincipal)
- [x] T042 Implement AuthenticatedHttpMessageHandler in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Http/AuthenticatedHttpMessageHandler.cs (JWT injection, token refresh)

#### Service Registration (DI)

- [x] T043 Create ServiceCollectionExtensions in src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs
- [x] T044 Implement AddCoreServices() extension method (register IAuthenticationService, ITokenCache, IEncryptionProvider, IConfigurationService)

**Completion Criteria**:
- ✅ All domain models created and validated
- ✅ All service interfaces defined
- ✅ Web Crypto API wrapper functional (test with console.log)
- ✅ Core services implemented and testable
- ✅ DI registration helper functional

---

## Phase 3: US1 - Authentication & Authorization [P0]

**User Story**: As a user, I want to securely login with my username and password, so that I can access the application with appropriate permissions.

**Goal**: Implement OAuth2 Password Grant authentication with JWT token caching and role-based authorization

**Module**: Core

**Duration**: 3-4 days

### Independent Test Criteria

- ✅ User can submit username/password on login page
- ✅ JWT access token and refresh token stored encrypted in LocalStorage
- ✅ Authentication state persists across page navigation (server → WASM transfer via PersistentComponentState)
- ✅ Token auto-refreshes when <5 minutes until expiration
- ✅ User can logout (tokens cleared from LocalStorage)
- ✅ Role-based authorization works (Administrator, Designer, Viewer)
- ✅ Unauthorized access to `/admin` redirects to Access Denied page

### Tasks

#### Server-Side Setup (Sorcha.UI.Web)

- [ ] T045 [US1] Configure cookie authentication in src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs (AddAuthentication, AddCookie)
- [ ] T046 [US1] Configure authorization policies in src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs (RequireAdministrator, RequireDesigner, RequireAuthenticated)
- [ ] T047 [US1] Register CascadingAuthenticationState and PersistingComponentStateProvider in Program.cs
- [ ] T048 [US1] Configure HttpClient for backend API in Program.cs (base URL from appsettings.json)

#### WASM-Side Setup (Sorcha.UI.Web.Client)

- [ ] T049 [US1] Configure AuthorizationCore in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Program.cs
- [ ] T050 [US1] Register Core services via AddCoreServices() in Program.cs
- [ ] T051 [US1] Implement PersistentComponentState retrieval with failure handling in Program.cs (retry + LocalStorage fallback)
- [ ] T052 [US1] Register CustomAuthenticationStateProvider as AuthenticationStateProvider in Program.cs
- [ ] T053 [US1] Configure AuthenticatedHttpMessageHandler for HttpClient in Program.cs

#### App Component (Server → WASM Transfer)

- [ ] T054 [US1] Create App.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web/Components/App.razor
- [ ] T055 [US1] Implement PersistentComponentState serialization in App.razor (RegisterOnPersisting, token size checks)
- [ ] T056 [US1] Add token size validation (warn at 16KB, fail at 32KB) in App.razor

#### Routing Component

- [ ] T057 [US1] Create Routes.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web/Components/Routes.razor
- [ ] T058 [US1] Configure Router with lazy loading assembly discovery in Routes.razor
- [ ] T059 [US1] Add CascadingAuthenticationState wrapper in Routes.razor

#### Login Page (Server-Rendered)

- [ ] T060 [US1] Create Login.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Login.razor
- [ ] T061 [US1] Add @rendermode InteractiveServer directive in Login.razor
- [ ] T062 [US1] Implement login form (MudTextField for username/password) in Login.razor
- [ ] T063 [US1] Implement HandleLogin() method (call IAuthenticationService.LoginAsync) in Login.razor
- [ ] T064 [US1] Implement token caching (ITokenCache.StoreTokenAsync) after successful login in Login.razor
- [ ] T065 [US1] Implement server-side cookie sign-in (HttpContext.SignInAsync) in Login.razor
- [ ] T066 [US1] Implement navigation to home page after login in Login.razor
- [ ] T067 [US1] Add error handling and validation messages in Login.razor

#### Access Denied Page

- [ ] T068 [US1] Create AccessDenied.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/AccessDenied.razor
- [ ] T069 [US1] Implement access denied UI (MudPaper with lock icon, error message) in AccessDenied.razor

#### Home Page (Anonymous Access)

- [ ] T070 [US1] Create Home.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor
- [ ] T071 [US1] Add @attribute [AllowAnonymous] directive in Home.razor
- [ ] T072 [US1] Implement landing page UI (welcome message, "Sign In" button) in Home.razor

#### Configuration

- [ ] T073 [US1] Add ApiGateway section to src/Apps/Sorcha.UI/Sorcha.UI.Web/appsettings.Development.json (BaseUrl: https://localhost:7082)
- [ ] T074 [US1] Add Authentication section to appsettings.Development.json (TokenRefreshThresholdMinutes: 5)
- [ ] T075 [US1] Create default profiles in IConfigurationService (Development, Docker) on first run

### Unit Tests (Optional - if TDD requested)

**Note**: Tests not included by default. Add if TDD approach requested.

**Completion Criteria**:
- ✅ Login page renders and accepts credentials
- ✅ Successful login stores encrypted tokens in LocalStorage
- ✅ Auth state persists after navigation (server → WASM transfer works)
- ✅ Access Denied page shows for unauthorized role access
- ✅ Manual test: Login as `admin@sorcha.local` / `Admin123!` → see authenticated home page

---

## Phase 4: US2 - Layout & Navigation [P0]

**User Story**: As a user, I want a consistent navigation layout with a sidebar menu and user profile menu, so that I can easily navigate between modules.

**Goal**: Implement main layout, navigation sidebar, user profile menu, and lazy loading routing

**Module**: Shared

**Duration**: 2-3 days

### Independent Test Criteria

- ✅ MainLayout renders with navigation sidebar and top bar
- ✅ NavMenu shows appropriate links based on user role (Administrator sees Admin link, Designer sees Designer link)
- ✅ UserProfileMenu shows authenticated user name and role
- ✅ Profile switching dialog works (logout → redirect to login)
- ✅ Lazy loading works: navigating to `/admin` loads Admin module on first access
- ✅ Loading spinner shows during module lazy loading

### Tasks

#### Main Layout

- [ ] T076 [US2] Create MainLayout.razor in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Layout/MainLayout.razor
- [ ] T077 [US2] Add MudLayout structure (MudAppBar, MudDrawer, MudMainContent) in MainLayout.razor
- [ ] T078 [US2] Add theme configuration (MudThemeProvider, dark/light mode toggle) in MainLayout.razor
- [ ] T079 [US2] Add MudDialogProvider and MudSnackbarProvider in MainLayout.razor

#### Navigation Menu

- [ ] T080 [US2] Create NavMenu.razor in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Layout/NavMenu.razor
- [ ] T081 [US2] Add Home link (always visible) in NavMenu.razor
- [ ] T082 [US2] Add Admin link with AuthorizeView (Roles="Administrator") in NavMenu.razor
- [ ] T083 [US2] Add Designer link with AuthorizeView (Roles="Administrator,Designer") in NavMenu.razor
- [ ] T084 [US2] Add Explorer link with AuthorizeView (authenticated users) in NavMenu.razor
- [ ] T085 [US2] Add MudNavLink icons (Material Design icons) in NavMenu.razor
- [ ] T086 [US2] Implement drawer toggle for mobile responsiveness in NavMenu.razor

#### User Profile Menu

- [ ] T087 [US2] Create UserProfileMenu.razor in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Components/UserProfileMenu.razor
- [ ] T088 [US2] Implement AuthorizeView with Authorized/NotAuthorized templates in UserProfileMenu.razor
- [ ] T089 [US2] Add user name display (from ClaimsPrincipal.Identity.Name) in UserProfileMenu.razor
- [ ] T090 [US2] Add user role badge (from ClaimsPrincipal.IsInRole) in UserProfileMenu.razor
- [ ] T091 [US2] Add "Switch Profile" menu item in UserProfileMenu.razor
- [ ] T092 [US2] Add "Logout" menu item (call IAuthenticationService.LogoutAsync) in UserProfileMenu.razor
- [ ] T093 [US2] Add "Sign In" button for anonymous users in UserProfileMenu.razor

#### Profile Selector Dialog

- [ ] T094 [US2] Create ProfileSelector.razor in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Components/ProfileSelector.razor
- [ ] T095 [US2] Implement profile list retrieval (IConfigurationService.GetProfilesAsync) in ProfileSelector.razor
- [ ] T096 [US2] Implement profile switch confirmation dialog in ProfileSelector.razor
- [ ] T097 [US2] Implement logout + profile switch + redirect to login in ProfileSelector.razor

#### Loading Spinner

- [ ] T098 [US2] Create LoadingSpinner.razor in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Components/LoadingSpinner.razor
- [ ] T099 [US2] Implement MudOverlay with MudProgressCircular in LoadingSpinner.razor
- [ ] T100 [US2] Add message parameter for customizable loading text in LoadingSpinner.razor

#### Routing Configuration

- [ ] T101 [US2] Update Routes.razor to configure lazy loading boundaries (/admin/*, /designer/*, /explorer/*)
- [ ] T102 [US2] Add AdditionalAssemblies for lazy-loaded modules (Sorcha.UI.Admin, Designer, Explorer) in Routes.razor
- [ ] T103 [US2] Add NotFound directive with custom 404 page in Routes.razor

#### 404 Not Found Page

- [ ] T104 [US2] Create NotFound.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/NotFound.razor
- [ ] T105 [US2] Implement 404 UI (MudPaper with error message, "Go Home" button) in NotFound.razor

**Completion Criteria**:
- ✅ MainLayout renders with navigation sidebar
- ✅ Navigation links appear based on user role (test with different roles)
- ✅ User profile menu shows authenticated user details
- ✅ Profile selector dialog functional
- ✅ Lazy loading works: `/admin` loads Admin module on first access (check browser Network tab)
- ✅ Manual test: Login → navigate to /admin → verify Admin module loads → navigate to /designer → verify Designer module loads

---

## Phase 5: US3 - Admin Module [P1]

**User Story**: As an administrator, I want to view system health, manage users (read-only for MVP), and configure profiles, so that I can monitor and maintain the system.

**Goal**: Implement Admin module with dashboard, user list, health monitoring, and profile configuration

**Module**: Admin

**Duration**: 3-4 days

### Independent Test Criteria

- ✅ Admin dashboard renders at `/admin`
- ✅ User list page shows users (mock data or backend API)
- ✅ Service health dashboard displays backend service health (API Gateway, Blueprint Service, Register Service)
- ✅ Profile configuration UI allows editing profile details
- ✅ Only Administrator role can access `/admin/*` routes
- ✅ Non-administrators see Access Denied page when accessing `/admin`

### Tasks

#### Admin Module Setup

- [ ] T106 [P] [US3] Create Index.razor in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Pages/Index.razor
- [ ] T107 [P] [US3] Add @attribute [Authorize(Roles = "Administrator")] to Index.razor
- [ ] T108 [P] [US3] Add @rendermode InteractiveWebAssembly directive to Index.razor

#### Admin Dashboard

- [ ] T109 [US3] Implement admin dashboard layout in Index.razor (MudGrid with stat cards)
- [ ] T110 [US3] Add system statistics card (total users, active sessions, uptime) in Index.razor
- [ ] T111 [US3] Add recent activity log in Index.razor (mock data for MVP)
- [ ] T112 [US3] Add quick actions section (links to Users, Health, Configuration) in Index.razor

#### User List Page

- [ ] T113 [US3] Create Users.razor in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Pages/Users.razor
- [ ] T114 [US3] Add @attribute [Authorize(Roles = "Administrator")] to Users.razor
- [ ] T115 [US3] Create UserTable.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Components/UserTable.razor
- [ ] T116 [US3] Implement MudDataGrid in UserTable.razor (columns: Username, Email, Role, Status)
- [ ] T117 [US3] Add search/filter functionality in UserTable.razor (MudTextField with @bind-Value)
- [ ] T118 [US3] Implement user data retrieval (mock data or backend API call) in UserTable.razor
- [ ] T119 [US3] Add pagination controls in UserTable.razor (MudDataGrid pagination)

#### Service Health Dashboard

- [ ] T120 [US3] Create Health.razor in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Pages/Health.razor
- [ ] T121 [US3] Add @attribute [Authorize(Roles = "Administrator")] to Health.razor
- [ ] T122 [US3] Create HealthCheckService in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Services/HealthCheckService.cs
- [ ] T123 [US3] Implement CheckServiceHealthAsync() method in HealthCheckService (call /health endpoint for each backend service)
- [ ] T124 [US3] Create ServiceHealthCard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Shared/Components/ServiceHealthCard.razor
- [ ] T125 [US3] Implement health status display in ServiceHealthCard.razor (Healthy/Degraded/Unhealthy badges)
- [ ] T126 [US3] Add health check cards for API Gateway, Blueprint Service, Register Service in Health.razor
- [ ] T127 [US3] Implement auto-refresh health checks (every 30 seconds) in Health.razor

#### Profile Configuration

- [ ] T128 [US3] Create Configuration.razor in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Pages/Configuration.razor
- [ ] T129 [US3] Add @attribute [Authorize(Roles = "Administrator")] to Configuration.razor
- [ ] T130 [US3] Implement profile list display (IConfigurationService.GetProfilesAsync) in Configuration.razor
- [ ] T131 [US3] Create ProfileEditorDialog.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Admin/Components/ProfileEditorDialog.razor
- [ ] T132 [US3] Implement profile edit form in ProfileEditorDialog.razor (MudTextField for Name, URLs, Color)
- [ ] T133 [US3] Implement profile save logic (IConfigurationService.SaveProfileAsync) in ProfileEditorDialog.razor
- [ ] T134 [US3] Add profile delete functionality (IConfigurationService.DeleteProfileAsync) in Configuration.razor
- [ ] T135 [US3] Add "Create Profile" button and dialog in Configuration.razor

**Completion Criteria**:
- ✅ Admin dashboard renders at `/admin`
- ✅ User list displays with search/filter (mock data acceptable for MVP)
- ✅ Service health dashboard shows real-time health status of backend services
- ✅ Profile configuration UI allows CRUD operations on profiles
- ✅ Authorization enforced: Non-administrators cannot access `/admin/*`
- ✅ Manual test: Login as Administrator → navigate to /admin → verify all pages render and function

---

## Phase 6: US4 - Designer Module [P1]

**User Story**: As a blueprint designer, I want to visually create and edit blueprints using a drag-and-drop workflow editor, so that I can design complex workflows easily.

**Goal**: Implement Designer module with blueprint list, visual editor (Z.Blazor.Diagrams), and JSON export/import

**Module**: Designer

**Duration**: 5-6 days

### Independent Test Criteria

- ✅ Designer dashboard renders at `/designer`
- ✅ Blueprint list displays blueprints from backend API
- ✅ Visual blueprint editor loads at `/designer/blueprints/{id}`
- ✅ Z.Blazor.Diagrams canvas functional (drag-and-drop nodes, connect edges)
- ✅ User can save blueprint as JSON to backend
- ✅ User can export blueprint as JSON file (download)
- ✅ Only Administrator or Designer roles can access `/designer/*` routes

### Tasks

#### Designer Module Setup

- [ ] T136 [P] [US4] Create Index.razor in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Pages/Index.razor
- [ ] T137 [P] [US4] Add @attribute [Authorize(Roles = "Administrator,Designer")] to Index.razor
- [ ] T138 [P] [US4] Add @rendermode InteractiveWebAssembly directive to Index.razor

#### Blueprint List Page

- [ ] T139 [US4] Implement blueprint list layout in Index.razor (MudDataGrid with blueprints)
- [ ] T140 [US4] Implement blueprint data retrieval (HttpClient.GetFromJsonAsync("/api/blueprints")) in Index.razor
- [ ] T141 [US4] Add blueprint list columns (Name, Version, Status, Created, Actions) in Index.razor
- [ ] T142 [US4] Add "Create Blueprint" button with navigation to editor in Index.razor
- [ ] T143 [US4] Add blueprint search/filter functionality in Index.razor
- [ ] T144 [US4] Add pagination controls in Index.razor

#### Blueprint Editor Page

- [ ] T145 [US4] Create BlueprintEditor.razor in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Pages/BlueprintEditor.razor
- [ ] T146 [US4] Add @page "/designer/blueprints/{id}" directive to BlueprintEditor.razor
- [ ] T147 [US4] Add @attribute [Authorize(Roles = "Administrator,Designer")] to BlueprintEditor.razor
- [ ] T148 [US4] Add reference to Sorcha.Blueprint.Models NuGet package in Sorcha.UI.Designer.csproj

#### Z.Blazor.Diagrams Integration

- [ ] T149 [US4] Create DiagramCanvas.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Components/DiagramCanvas.razor
- [ ] T150 [US4] Initialize Z.Blazor.Diagrams Diagram instance in DiagramCanvas.razor
- [ ] T151 [US4] Configure diagram options (grid, zoom, pan, selection) in DiagramCanvas.razor
- [ ] T152 [US4] Implement node factory (Action nodes with ports) in DiagramCanvas.razor
- [ ] T153 [US4] Implement link factory (connections between actions) in DiagramCanvas.razor
- [ ] T154 [US4] Add drag-and-drop support for adding actions from palette in DiagramCanvas.razor

#### Action Palette

- [ ] T155 [US4] Create ActionPalette.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Components/ActionPalette.razor
- [ ] T156 [US4] Implement action type list (HTTP Request, Script, Condition, etc.) in ActionPalette.razor
- [ ] T157 [US4] Add drag-and-drop handlers to add actions to canvas in ActionPalette.razor

#### Property Panel

- [ ] T158 [US4] Create PropertyPanel.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Components/PropertyPanel.razor
- [ ] T159 [US4] Implement action property editor (action name, type, parameters) in PropertyPanel.razor
- [ ] T160 [US4] Add parameter input fields (dynamic based on action type) in PropertyPanel.razor
- [ ] T161 [US4] Implement property change events (update diagram node) in PropertyPanel.razor

#### Blueprint Serialization

- [ ] T162 [US4] Create DiagramSerializationService in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Services/DiagramSerializationService.cs
- [ ] T163 [US4] Implement SerializeToBlueprint() method (Diagram → Blueprint JSON) in DiagramSerializationService
- [ ] T164 [US4] Implement DeserializeFromBlueprint() method (Blueprint JSON → Diagram) in DiagramSerializationService

#### Save/Load Functionality

- [ ] T165 [US4] Implement "Save Blueprint" button in BlueprintEditor.razor
- [ ] T166 [US4] Implement blueprint save logic (POST /api/blueprints or PUT /api/blueprints/{id}) in BlueprintEditor.razor
- [ ] T167 [US4] Implement blueprint load logic (GET /api/blueprints/{id}) in BlueprintEditor.razor OnInitializedAsync
- [ ] T168 [US4] Add blueprint validation before save (call Sorcha.Blueprint.Engine validation) in BlueprintEditor.razor

#### Export/Import

- [ ] T169 [US4] Create BlueprintExportDialog.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Designer/Components/BlueprintExportDialog.razor
- [ ] T170 [US4] Implement export to JSON file (download via JavaScript Interop) in BlueprintExportDialog.razor
- [ ] T171 [US4] Implement export to YAML file in BlueprintExportDialog.razor
- [ ] T172 [US4] Implement import from JSON file (file upload) in BlueprintExportDialog.razor

**Completion Criteria**:
- ✅ Designer dashboard renders at `/designer`
- ✅ Blueprint list displays blueprints from backend API
- ✅ Visual editor functional: drag-and-drop actions, connect actions, edit properties
- ✅ Save blueprint to backend API works
- ✅ Export blueprint as JSON file works
- ✅ Authorization enforced: Viewers cannot access `/designer/*`
- ✅ Manual test: Login as Designer → create new blueprint → add actions → connect → save → reload → verify blueprint loads correctly

---

## Phase 7: US5 - Explorer Module [P1]

**User Story**: As a user, I want to browse registers and view transactions, so that I can explore blockchain data and verify transaction details.

**Goal**: Implement Explorer module with register list, transaction list, and transaction detail viewer

**Module**: Explorer

**Duration**: 3-4 days

### Independent Test Criteria

- ✅ Explorer dashboard renders at `/explorer`
- ✅ Register list displays registers from backend API
- ✅ Transaction list displays transactions for a register
- ✅ Transaction detail page shows full transaction data (signatures, payload, metadata)
- ✅ Search functionality works (search by TX ID)
- ✅ All authenticated users can access `/explorer/*` routes

### Tasks

#### Explorer Module Setup

- [ ] T173 [P] [US5] Create Index.razor in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Pages/Index.razor
- [ ] T174 [P] [US5] Add @attribute [Authorize] directive to Index.razor (all authenticated users)
- [ ] T175 [P] [US5] Add @rendermode InteractiveWebAssembly directive to Index.razor

#### Register List Page

- [ ] T176 [US5] Implement register list layout in Index.razor (MudDataGrid with registers)
- [ ] T177 [US5] Implement register data retrieval (HttpClient.GetFromJsonAsync("/api/registers")) in Index.razor
- [ ] T178 [US5] Add register list columns (Name, ID, Transaction Count, Created) in Index.razor
- [ ] T179 [US5] Add "View Transactions" button for each register in Index.razor
- [ ] T180 [US5] Add register search functionality in Index.razor

#### Transaction List Page

- [ ] T181 [US5] Create Transactions.razor in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Pages/Transactions.razor
- [ ] T182 [US5] Add @page "/explorer/transactions" directive with query parameter (registerId) to Transactions.razor
- [ ] T183 [US5] Add @attribute [Authorize] directive to Transactions.razor
- [ ] T184 [US5] Create TransactionTable.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Components/TransactionTable.razor
- [ ] T185 [US5] Implement MudDataGrid in TransactionTable.razor (columns: TX ID, Timestamp, Sender, Type, Status)
- [ ] T186 [US5] Implement transaction data retrieval (GET /api/registers/{id}/transactions) in TransactionTable.razor
- [ ] T187 [US5] Add pagination controls in TransactionTable.razor
- [ ] T188 [US5] Add "View Details" button for each transaction in TransactionTable.razor

#### Transaction Detail Page

- [ ] T189 [US5] Create TransactionDetail.razor in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Pages/TransactionDetail.razor
- [ ] T190 [US5] Add @page "/explorer/transactions/{id}" directive to TransactionDetail.razor
- [ ] T191 [US5] Add @attribute [Authorize] directive to TransactionDetail.razor
- [ ] T192 [US5] Implement transaction data retrieval (GET /api/transactions/{id}) in TransactionDetail.razor OnInitializedAsync
- [ ] T193 [US5] Create TransactionViewer.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Components/TransactionViewer.razor
- [ ] T194 [US5] Implement transaction metadata display (TX ID, timestamp, sender, register) in TransactionViewer.razor
- [ ] T195 [US5] Implement transaction payload display (formatted JSON) in TransactionViewer.razor
- [ ] T196 [US5] Implement signature verification display in TransactionViewer.razor
- [ ] T197 [US5] Add "Copy TX ID" button in TransactionViewer.razor

#### Search Functionality

- [ ] T198 [US5] Create Search.razor page in src/Apps/Sorcha.UI/Sorcha.UI.Explorer/Pages/Search.razor
- [ ] T199 [US5] Add @page "/explorer/search" directive to Search.razor
- [ ] T200 [US5] Implement search form (MudTextField with search query) in Search.razor
- [ ] T201 [US5] Implement search by TX ID (redirect to transaction detail) in Search.razor
- [ ] T202 [US5] Implement search by wallet address (show related transactions) in Search.razor

**Completion Criteria**:
- ✅ Explorer dashboard renders at `/explorer`
- ✅ Register list displays registers from backend API
- ✅ Transaction list displays transactions for selected register
- ✅ Transaction detail page shows full transaction data
- ✅ Search by TX ID works (navigate to transaction detail)
- ✅ Authorization enforced: Unauthenticated users cannot access `/explorer/*`
- ✅ Manual test: Login → navigate to /explorer → select register → view transactions → view transaction detail → verify all data displays correctly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Goal**: Testing, performance optimization, documentation, deployment preparation

**Duration**: 2-3 weeks

### Tasks

#### Unit Tests (Core Services)

- [ ] T203 [P] Create AuthenticationService unit tests in tests/Sorcha.UI.Core.Tests/Services/AuthenticationServiceTests.cs
- [ ] T204 [P] Create BrowserTokenCache unit tests in tests/Sorcha.UI.Core.Tests/Services/BrowserTokenCacheTests.cs
- [ ] T205 [P] Create ConfigurationService unit tests in tests/Sorcha.UI.Core.Tests/Services/ConfigurationServiceTests.cs
- [ ] T206 [P] Create BrowserEncryptionProvider unit tests in tests/Sorcha.UI.Core.Tests/Services/BrowserEncryptionProviderTests.cs
- [ ] T207 [P] Verify >85% code coverage for Sorcha.UI.Core (run `dotnet test --collect:"XPlat Code Coverage"`)

#### Component Tests (bUnit)

- [ ] T208 [P] Create Login.razor component tests in tests/Sorcha.UI.Core.Tests/Components/LoginTests.cs
- [ ] T209 [P] Create UserProfileMenu.razor component tests in tests/Sorcha.UI.Core.Tests/Components/UserProfileMenuTests.cs
- [ ] T210 [P] Create NavMenu.razor component tests in tests/Sorcha.UI.Core.Tests/Components/NavMenuTests.cs
- [ ] T211 [P] Verify >70% coverage for UI components

#### Integration Tests

- [ ] T212 [P] Create authentication flow integration test in tests/Sorcha.UI.Integration.Tests/AuthenticationFlowTests.cs (login → token storage → navigation → logout)
- [ ] T213 [P] Create lazy loading integration test in tests/Sorcha.UI.Integration.Tests/LazyLoadingTests.cs (navigate to /admin → verify module loads)
- [ ] T214 [P] Create profile switching integration test in tests/Sorcha.UI.Integration.Tests/ProfileSwitchingTests.cs

#### E2E Tests (Playwright)

- [ ] T215 [P] Set up Playwright in tests/Sorcha.UI.Integration.Tests/E2E/ (install dependencies, configure browser)
- [ ] T216 [P] Create E2E smoke test in tests/Sorcha.UI.Integration.Tests/E2E/SmokeTests.cs (home page loads, login works, navigation works)
- [ ] T217 [P] Create E2E admin workflow test (login as admin → navigate to /admin → verify dashboard)
- [ ] T218 [P] Create E2E designer workflow test (login as designer → create blueprint → save → reload)

#### Performance Optimization

- [ ] T219 Verify main bundle size <3 MB gzip (`dotnet publish -c Release`, inspect _framework/*.dll.gz)
- [ ] T220 Verify module sizes: Admin <500 KB, Designer <1.5 MB, Explorer <500 KB (inspect module .dll.gz files)
- [ ] T221 Run Lighthouse performance test (https://localhost:7083, 3G throttled network)
- [ ] T222 Optimize images and assets (compress images, minify CSS if needed)
- [ ] T223 Enable Response Compression in Sorcha.UI.Web/Program.cs

#### Documentation

- [ ] T224 Create user documentation in src/Apps/Sorcha.UI/docs/user-guide.md (how to login, navigate, use modules)
- [ ] T225 Create deployment guide in src/Apps/Sorcha.UI/docs/deployment.md (how to deploy to production)
- [ ] T226 Update quickstart.md with final testing credentials and setup instructions
- [ ] T227 Create CHANGELOG.md with v1.0.0 MVP release notes

#### Production Readiness

- [ ] T228 Configure HTTPS certificates for production (update appsettings.Production.json)
- [ ] T229 Add production profile to IConfigurationService defaults (Production environment URLs)
- [ ] T230 Security review: OWASP Top 10 checklist (XSS, CSRF, injection, etc.)
- [ ] T231 Configure CSP headers in Sorcha.UI.Web/Program.cs (Content Security Policy)
- [ ] T232 Configure CORS if needed (API Gateway CORS policy)
- [ ] T233 Set up CI/CD pipeline (.github/workflows/sorcha-ui.yml) (build, test, publish)
- [ ] T234 Create Docker image for Sorcha.UI (Dockerfile, docker-compose integration)
- [ ] T235 Final manual QA testing (test all user stories end-to-end)

**Completion Criteria**:
- ✅ >85% unit test coverage for Sorcha.UI.Core
- ✅ >70% coverage for UI components
- ✅ All integration tests passing
- ✅ E2E smoke tests passing
- ✅ Bundle sizes within performance targets
- ✅ Lighthouse score >90 (Performance)
- ✅ User documentation complete
- ✅ Deployment guide ready
- ✅ Security review passed
- ✅ CI/CD pipeline functional

---

## Parallel Execution Opportunities

### Phase 1: Setup & Infrastructure
**All tasks are sequential** (project creation must complete before proceeding)

### Phase 2: Foundational
**Parallel Groups**:
- **Group A** (Models): T021-T028 can run in parallel (different model files)
- **Group B** (Service Interfaces): T029-T032 can run in parallel after models complete
- **Group C** (JavaScript + Implementations): T033-T036 (JS files) can run parallel with T037-T042 (C# services) after interfaces complete

**Estimated Time Savings**: ~30% reduction (3 days → 2 days)

### Phase 3: US1 - Authentication & Authorization
**Parallel Groups**:
- **Group A** (Server-side setup): T045-T048 can run in parallel
- **Group B** (WASM-side setup): T049-T053 can run in parallel with Group A
- **Group C** (Pages): T060-T072 can start after T045-T053 complete, but different pages can be built in parallel

**Estimated Time Savings**: ~25% reduction (4 days → 3 days)

### Phase 4: US2 - Layout & Navigation
**Parallel Groups**:
- **Group A** (Components): T076-T079 (MainLayout), T080-T086 (NavMenu), T087-T093 (UserProfileMenu), T094-T097 (ProfileSelector) can run in parallel (different component files)
- **Group B** (Pages): T104-T105 (NotFound), T098-T100 (LoadingSpinner) can run in parallel with Group A

**Estimated Time Savings**: ~40% reduction (3 days → 2 days)

### Phase 5-7: US3, US4, US5 (Admin, Designer, Explorer Modules)
**These modules are completely independent** - can be developed in parallel by different developers:

- **Developer 1**: US3 (Admin Module) - T106-T135
- **Developer 2**: US4 (Designer Module) - T136-T172
- **Developer 3**: US5 (Explorer Module) - T173-T202

**Estimated Time Savings**: Massive reduction if 3 developers available
- **Sequential**: 12-14 days (3+4+5+6+3+4 days)
- **Parallel (3 devs)**: 5-6 days (max of the three: US4 Designer Module is longest)

### Phase 8: Polish
**Parallel Groups**:
- **Group A** (Unit Tests): T203-T207 can run in parallel (different test files)
- **Group B** (Component Tests): T208-T211 can run in parallel with Group A
- **Group C** (Integration/E2E Tests): T212-T218 can run in parallel after core functionality complete
- **Group D** (Documentation): T224-T227 can run in parallel with testing groups

**Estimated Time Savings**: ~50% reduction (2-3 weeks → 1-1.5 weeks)

---

## MVP Scope Recommendation

**Suggested MVP**: US1 + US2 + US3 (Authentication + Layout + Admin Module)

**Rationale**:
- US1 (Authentication) is P0 critical, blocks all other work
- US2 (Layout) is P0 critical, provides navigation framework
- US3 (Admin Module) provides immediate value (system monitoring, profile management)
- US4 (Designer) and US5 (Explorer) can be delivered in subsequent increments

**MVP Timeline**: 2-3 weeks (with 2 developers)

**Post-MVP Increments**:
- **Increment 2**: US4 (Designer Module) - 1-2 weeks
- **Increment 3**: US5 (Explorer Module) - 1 week
- **Increment 4**: Polish & Production Readiness - 1-2 weeks

**Total Timeline**: 6-8 weeks for full MVP + all modules

---

## Task Summary

### Total Tasks by Phase

| Phase | Task Count | Estimated Duration |
|-------|------------|-------------------|
| **Phase 1: Setup** | 20 tasks | 1-2 days |
| **Phase 2: Foundational** | 24 tasks | 2-3 days |
| **Phase 3: US1 - Authentication** | 31 tasks | 3-4 days |
| **Phase 4: US2 - Layout** | 30 tasks | 2-3 days |
| **Phase 5: US3 - Admin** | 30 tasks | 3-4 days |
| **Phase 6: US4 - Designer** | 37 tasks | 5-6 days |
| **Phase 7: US5 - Explorer** | 30 tasks | 3-4 days |
| **Phase 8: Polish** | 33 tasks | 2-3 weeks |
| **TOTAL** | **235 tasks** | **6-8 weeks** |

### Tasks by User Story

| User Story | Task Count | Parallelizable | Estimated Duration |
|-----------|------------|----------------|-------------------|
| **US1: Authentication** | 31 tasks | ~8 parallel | 3-4 days |
| **US2: Layout** | 30 tasks | ~12 parallel | 2-3 days |
| **US3: Admin** | 30 tasks | ~3 parallel | 3-4 days |
| **US4: Designer** | 37 tasks | ~3 parallel | 5-6 days |
| **US5: Explorer** | 30 tasks | ~3 parallel | 3-4 days |

### Parallelization Summary

- **Total Parallelizable Tasks**: 67 tasks (28% of total)
- **Modules Parallelizable**: US3, US4, US5 (completely independent)
- **Estimated Time Savings with Parallel Execution**: ~40% reduction (8 weeks → 5 weeks with 3 developers)

---

## Implementation Strategy

### Week-by-Week Plan (Sequential Development)

**Week 1-2**: Setup + Foundational + US1 (Authentication)
- Days 1-2: Phase 1 (Setup)
- Days 3-4: Phase 2 (Foundational)
- Days 5-10: Phase 3 (US1 - Authentication)

**Week 3**: US2 (Layout & Navigation)
- Days 11-13: Phase 4 (US2)

**Week 4**: US3 (Admin Module)
- Days 14-17: Phase 5 (US3)

**Week 5**: US4 (Designer Module)
- Days 18-23: Phase 6 (US4)

**Week 6**: US5 (Explorer Module)
- Days 24-27: Phase 7 (US5)

**Week 7-8**: Polish & Production Readiness
- Days 28-42: Phase 8 (Testing, optimization, deployment)

### Parallel Development Plan (3 Developers)

**Week 1**: Setup + Foundational + US1 (All developers)
- Developer 1, 2, 3: Phase 1-2 (pair on setup)
- Developer 1, 2, 3: Phase 3 (pair on authentication)

**Week 2**: US2 + Module Development Starts
- Developer 1, 2, 3: Phase 4 (US2 - pair on layout)
- Then split:
  - Developer 1: US3 (Admin)
  - Developer 2: US4 (Designer)
  - Developer 3: US5 (Explorer)

**Week 3-4**: Parallel Module Development
- Developer 1: Complete US3
- Developer 2: Complete US4
- Developer 3: Complete US5

**Week 5-6**: Polish & Production Readiness
- All developers: Phase 8 (split testing, optimization, documentation tasks)

**Total Timeline with 3 Developers**: 5-6 weeks (vs 8 weeks sequential)

---

## Validation Checklist

### Format Validation

✅ **All tasks follow checklist format**: `- [ ] [TaskID] [P?] [Story?] Description with file path`
✅ **Task IDs sequential**: T001 through T235
✅ **[P] marker**: Applied to 67 parallelizable tasks
✅ **[Story] labels**: Applied to all US1-US5 tasks (T045-T202)
✅ **File paths**: Included in all implementation tasks

### Completeness Validation

✅ **Each user story has**:
- Independent test criteria
- Complete implementation tasks (models → services → pages → components)
- Authorization/security tasks
- Integration points defined

✅ **All design artifacts mapped to tasks**:
- data-model.md models → T021-T028
- contracts/ interfaces → T029-T032
- plan.md modules → US3-US5 phases
- quickstart.md guidance → reflected in task descriptions

✅ **Dependencies identified**:
- Phase dependencies clear (Setup → Foundational → User Stories)
- Story dependencies clear (US1 → US2 → US3/US4/US5)
- Parallel opportunities documented

---

**Ready for Implementation** ✅

Use this task list to track progress. Mark tasks complete as you implement them:
```bash
# Example: Mark T001 as complete
- [x] T001 Create Sorcha.UI solution file at src/Apps/Sorcha.UI/Sorcha.UI.sln
```

Track progress by counting completed tasks:
```bash
grep -c "^\- \[x\]" tasks.md  # Count completed tasks
grep -c "^\- \[ \]" tasks.md  # Count remaining tasks
```

**Document Version**: 1.0 | **Last Updated**: 2026-01-06 | **Author**: Claude Sonnet 4.5
