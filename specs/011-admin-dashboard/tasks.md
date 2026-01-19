# Tasks: Admin Dashboard and Management

**Input**: Design documents from `/specs/011-admin-dashboard/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: E2E tests with Playwright included (per Testing Requirements in constitution)

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- All paths relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and view model creation

- [x] T001 Create ServiceHealthStatus model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Admin/ServiceHealthStatus.cs
- [x] T002 [P] Create HealthStatus enum in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Admin/HealthStatus.cs
- [x] T003 [P] Create PlatformKpis model in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Admin/PlatformKpis.cs
- [x] T004 [P] Create HealthCheckOptions configuration class in src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Admin/HealthCheckOptions.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core services that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create IHealthCheckService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IHealthCheckService.cs
- [x] T006 Implement HealthCheckService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/HealthCheckService.cs
- [x] T007 [P] Create IOrganizationAdminService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IOrganizationAdminService.cs
- [x] T008 Implement OrganizationAdminService in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/OrganizationAdminService.cs
- [x] T009 [P] Create IAuditService interface in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IAuditService.cs
- [x] T010 Implement AuditService for client-side audit logging in src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/AuditService.cs
- [x] T011 Register all new services in DI container in src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs
- [x] T012 Add new AuditEventType values to Tenant Service enum in src/Services/Sorcha.Tenant.Service/Models/AuditLogEntry.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Service Status Dashboard (Priority: P1) 🎯 MVP

**Goal**: System administrators can view real-time health status and KPIs of all platform services

**Independent Test**: Login as system admin, navigate to Admin dashboard, verify all 7 service statuses and KPIs are displayed with current data

### E2E Tests for User Story 1

- [x] T013 [P] [US1] Create AdminDashboardTests.cs scaffold in tests/Sorcha.UI.E2E.Tests/AdminDashboardTests.cs
- [x] T014 [P] [US1] Add test: Dashboard displays all 7 service health cards in tests/Sorcha.UI.E2E.Tests/AdminDashboardTests.cs
- [x] T015 [P] [US1] Add test: KPI panel shows organization and user counts in tests/Sorcha.UI.E2E.Tests/AdminDashboardTests.cs
- [x] T016 [P] [US1] Add test: Clicking service card shows detail dialog in tests/Sorcha.UI.E2E.Tests/AdminDashboardTests.cs

### Implementation for User Story 1

- [x] T017 [P] [US1] Create ServiceHealthCard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/ServiceHealthCard.razor
- [x] T018 [P] [US1] Create KpiSummaryPanel.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/KpiSummaryPanel.razor
- [x] T019 [US1] Create ServiceHealthDashboard.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/ServiceHealthDashboard.razor
- [x] T020 [US1] Add "System Health" tab content to Administration.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Administration.razor
- [x] T021 [US1] Implement health polling timer start/stop in ServiceHealthDashboard.razor
- [x] T022 [US1] Add service detail dialog (MudDialog) to ServiceHealthCard.razor

**Checkpoint**: User Story 1 complete - health dashboard functional and independently testable

---

## Phase 4: User Story 2 - Tenant Organization Management (Priority: P2)

**Goal**: System administrators can list, create, and modify tenant organizations

**Independent Test**: Create a new organization, view it in the list, update its details, and deactivate it

### E2E Tests for User Story 2

- [x] T023 [P] [US2] Create OrganizationManagementTests.cs scaffold in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T024 [P] [US2] Add test: Organization list displays paginated data in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T025 [P] [US2] Add test: Create organization with valid subdomain in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T026 [P] [US2] Add test: Edit organization updates name and branding in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T027 [P] [US2] Add test: Deactivate organization changes status in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T028 [P] [US2] Add test: Subdomain validation shows real-time feedback in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs
- [x] T029 [P] [US2] Add test: Non-admin users see access denied in tests/Sorcha.UI.E2E.Tests/OrganizationManagementTests.cs

### Implementation for User Story 2

- [x] T030 [P] [US2] Create OrganizationList.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/OrganizationList.razor
- [x] T031 [P] [US2] Create OrganizationForm.razor dialog component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/OrganizationForm.razor
- [x] T032 [US2] Implement MudDataGrid with server-side pagination in OrganizationList.razor
- [x] T033 [US2] Implement subdomain validation with debounce in OrganizationForm.razor
- [x] T034 [US2] Implement branding configuration fields in OrganizationForm.razor
- [x] T035 [US2] Add deactivation confirmation dialog (MudDialog) to OrganizationList.razor
- [x] T036 [US2] Add "Organizations" tab to Administration.razor in src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Administration.razor
- [x] T037 [US2] Implement RBAC check to hide Organizations tab for non-admins in Administration.razor
- [x] T038 [US2] Wire audit logging for organization CRUD operations in OrganizationAdminService.cs

**Checkpoint**: User Story 2 complete - organization management functional and independently testable

---

## Phase 5: User Story 3 - Organization Participant Management (Priority: P3)

**Goal**: Organization administrators can list, create, and modify users within their organization

**Independent Test**: Navigate to an organization's user list, add a new user, update their role, and remove them

### E2E Tests for User Story 3

- [x] T039 [P] [US3] Create UserManagementTests.cs scaffold in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T040 [P] [US3] Add test: User list displays all organization users in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T041 [P] [US3] Add test: Add user with email, name, and role in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T042 [P] [US3] Add test: Edit user role changes from Member to Administrator in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T043 [P] [US3] Add test: Remove user with confirmation in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T044 [P] [US3] Add test: Cannot remove yourself shows error in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs
- [x] T045 [P] [US3] Add test: Standard members see read-only user list in tests/Sorcha.UI.E2E.Tests/UserManagementTests.cs

### Implementation for User Story 3

- [x] T046 [P] [US3] Create UserList.razor component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/UserList.razor
- [x] T047 [P] [US3] Create UserForm.razor dialog component in src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/UserForm.razor
- [x] T048 [US3] Implement MudDataGrid with user data in UserList.razor
- [x] T049 [US3] Implement role dropdown (Administrator/Designer/Member) in UserForm.razor
- [x] T050 [US3] Implement self-removal prevention check in UserList.razor
- [x] T051 [US3] Add user removal confirmation dialog (MudDialog) to UserList.razor
- [x] T052 [US3] Add "Users" tab or inline section to organization detail view
- [x] T053 [US3] Implement RBAC check to disable edit/delete for non-admin users in UserList.razor
- [x] T054 [US3] Wire audit logging for user CRUD operations in OrganizationAdminService.cs

**Checkpoint**: User Story 3 complete - user management functional and independently testable

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T055 [P] Add loading skeletons to all list components (OrganizationList, UserList, ServiceHealthDashboard)
- [x] T056 [P] Add error state handling with MudAlert to all components
- [x] T057 [P] Add empty state messages to list components
- [x] T058 Verify all components follow MudBlazor theming consistency
- [x] T059 [P] Add XML documentation to all public service interfaces
- [ ] T060 Run quickstart.md validation - verify all documented commands work
- [x] T061 Update Administration.razor to set default tab based on user role
- [ ] T062 Performance test: Verify organization list handles 100+ items without degradation
- [ ] T063 Performance test: Verify user list handles 500 users within 2 seconds
- [ ] T064 Run all E2E tests and fix any failures

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

| Story | Depends On | Can Start After |
|-------|------------|-----------------|
| US1 (Dashboard) | Foundational | Phase 2 complete |
| US2 (Organizations) | Foundational | Phase 2 complete |
| US3 (Users) | Foundational, US2* | Phase 2 complete |

*US3 navigation typically accessed from organization detail, but can be tested independently

### Parallel Opportunities

**Within Phase 1 (Setup)**:
```
T001, T002, T003, T004 - All model classes can be created in parallel
```

**Within Phase 2 (Foundational)**:
```
T005+T006 (Health), T007+T008 (Org), T009+T010 (Audit) - Service pairs can parallelize
```

**Within User Story 1**:
```
T013, T014, T015, T016 - All E2E test scaffolds in parallel
T017, T018 - Card and KPI components in parallel
```

**Within User Story 2**:
```
T023-T029 - All E2E tests in parallel
T030, T031 - List and Form components in parallel
```

**Within User Story 3**:
```
T039-T045 - All E2E tests in parallel
T046, T047 - List and Form components in parallel
```

**Cross-Story Parallelism** (with multiple developers):
```
Developer A: US1 (T013-T022)
Developer B: US2 (T023-T038)
Developer C: US3 (T039-T054)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T012) - **CRITICAL BLOCKER**
3. Complete Phase 3: User Story 1 (T013-T022)
4. **STOP and VALIDATE**: Run AdminDashboardTests.cs
5. Deploy/demo - basic health monitoring available

### Incremental Delivery

| Increment | Stories | Value Delivered |
|-----------|---------|-----------------|
| MVP | US1 | Health monitoring dashboard |
| +1 | US1 + US2 | + Organization management |
| Full | US1 + US2 + US3 | + User management |

### Task Summary

| Phase | Task Count | Parallel Tasks |
|-------|------------|----------------|
| Setup | 4 | 3 |
| Foundational | 8 | 4 |
| US1 (P1) | 10 | 6 |
| US2 (P2) | 16 | 9 |
| US3 (P3) | 16 | 9 |
| Polish | 10 | 5 |
| **Total** | **64** | **36** |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- E2E tests use Playwright with existing Sorcha.UI.E2E.Tests project
- Commit after each task or logical group
- All components use MudBlazor for UI consistency
- Services consume existing Tenant Service APIs via Sorcha.ServiceClients
