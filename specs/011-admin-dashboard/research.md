# Research: Admin Dashboard and Management

**Feature Branch**: `011-admin-dashboard`
**Date**: 2026-01-19

## Executive Summary

The Admin Dashboard feature builds on well-established infrastructure. The Tenant Service already provides complete CRUD APIs for organizations and users. Health check endpoints follow standard ASP.NET Core patterns. The primary work is building Blazor WASM UI components to consume these existing APIs.

---

## Research Findings

### 1. Existing Organization API

**Decision**: Use existing Tenant Service Organization endpoints

**Rationale**: Complete CRUD operations already implemented with proper authorization

**Existing Endpoints** (via `OrganizationEndpoints.cs`):
| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/organizations` | Create organization |
| GET | `/api/organizations` | List all (admin only) |
| GET | `/api/organizations/{id}` | Get by ID |
| GET | `/api/organizations/by-subdomain/{subdomain}` | Get by subdomain |
| GET | `/api/organizations/stats` | Get KPI stats |
| PUT | `/api/organizations/{id}` | Update organization |
| DELETE | `/api/organizations/{id}` | Deactivate (soft delete) |
| GET | `/api/organizations/validate-subdomain/{subdomain}` | Validate subdomain |

**DTOs Available**:
- `CreateOrganizationRequest` (Name, Subdomain, Branding)
- `UpdateOrganizationRequest` (Name, Status, Branding)
- `OrganizationResponse` (Id, Name, Subdomain, Status, CreatedAt, Branding)
- `OrganizationListResponse` (Organizations[], TotalCount)
- `OrganizationStatsResponse` (TotalOrganizations, TotalUsers)

**Alternatives Considered**: Creating new endpoints → rejected (duplication)

---

### 2. Existing User Management API

**Decision**: Use existing Tenant Service User endpoints

**Rationale**: User CRUD already implemented per organization scope

**Existing Endpoints** (via `OrganizationEndpoints.cs`):
| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/organizations/{orgId}/users` | Add user to org |
| GET | `/api/organizations/{orgId}/users` | List org users |
| GET | `/api/organizations/{orgId}/users/{userId}` | Get specific user |
| PUT | `/api/organizations/{orgId}/users/{userId}` | Update user |
| DELETE | `/api/organizations/{orgId}/users/{userId}` | Remove user |

**DTOs Available**:
- `AddUserToOrganizationRequest` (Email, DisplayName, ExternalIdpUserId, Roles)
- `UpdateUserRequest` (DisplayName, Roles, Status)
- `UserResponse` (Id, OrganizationId, Email, DisplayName, Roles, Status, CreatedAt, LastLoginAt)
- `UserListResponse` (Users[], TotalCount)

**User Roles** (from `UserRole` enum):
- Administrator, SystemAdmin, Designer, Developer, User, Consumer, Auditor, Member

**User Statuses** (from `IdentityStatus` enum):
- Active, Suspended, Deleted

**Note**: Spec clarified to use 3 roles in UI: Administrator, Designer, Member

---

### 3. Health Check Infrastructure

**Decision**: Poll standard `/health` endpoints from each service

**Rationale**: All Sorcha services use `Sorcha.ServiceDefaults` which adds `/health` and `/alive` endpoints

**Implementation Details**:
- Endpoints: `/health` (ready), `/alive` (live)
- Response: Standard ASP.NET Core Health Check JSON
- Polling interval: 30 seconds (configurable)
- Services to monitor: Blueprint, Register, Wallet, Tenant, Validator, Peer, API Gateway

**Health Status Mapping**:
| ASP.NET Status | UI Display | Color |
|----------------|------------|-------|
| Healthy | Healthy | Green |
| Degraded | Degraded | Yellow |
| Unhealthy | Unhealthy | Red |
| (timeout/error) | Unknown | Gray |

**Alternatives Considered**: SignalR push → deferred (MVP uses polling)

---

### 4. Audit Logging

**Decision**: Extend existing `AuditLogEntry` model and `AuditEventType` enum

**Rationale**: Audit infrastructure exists but needs new event types for admin actions

**New Event Types Needed**:
```
OrganizationCreated
OrganizationUpdated
OrganizationDeactivated
UserAddedToOrganization
UserUpdatedInOrganization
UserRemovedFromOrganization
```

**Storage**: PostgreSQL via existing `TenantDbContext`

**Alternatives Considered**: Separate audit service → rejected (over-engineering for MVP)

---

### 5. Blazor Component Patterns

**Decision**: Follow existing Admin component patterns (BlueprintServiceAdmin.razor, PeerServiceAdmin.razor)

**Rationale**: Consistency with existing codebase

**Key Patterns**:
- Components in `Sorcha.UI.Core/Components/Admin/`
- Use MudBlazor components (MudDataGrid, MudDialog, MudForm)
- Inject services via `@inject`
- Use `@rendermode InteractiveWebAssembly`
- Follow established error handling with MudAlert

**Alternatives Considered**: Server-side Blazor → rejected (existing app is WASM)

---

### 6. Authorization in UI

**Decision**: Use existing JWT claims + client-side permission checks

**Rationale**: Authentication already implemented; extend for RBAC

**Implementation**:
- Check `AuthenticationStateProvider` for user roles
- Use `AuthorizeView` component to conditionally render UI
- Server-side validation via `RequireAuthorization` policies
- Hide tabs/buttons for unauthorized users

**Existing Policies**:
- `RequireAdministrator` - System admin access
- `RequireOrganizationMember` - Org member access

---

## Gaps Identified

| Gap | Resolution |
|-----|------------|
| Audit event types missing | Add 6 new AuditEventType values |
| No health aggregation endpoint | Create client-side service to poll multiple endpoints |
| Active sessions KPI not available | Defer to future (out of MVP scope) |

---

## Technology Decisions Summary

| Decision | Technology | Reason |
|----------|------------|--------|
| UI Framework | Blazor WASM + MudBlazor | Existing stack |
| API Client | Sorcha.ServiceClients | Consolidated HTTP clients |
| Health Polling | System.Timers.Timer | Simple, effective for 30s interval |
| State Management | Component state + cascading parameters | Standard Blazor pattern |
| Testing | Playwright E2E | Existing test infrastructure |
