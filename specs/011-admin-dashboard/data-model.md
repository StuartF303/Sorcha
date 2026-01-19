# Data Model: Admin Dashboard and Management

**Feature Branch**: `011-admin-dashboard`
**Date**: 2026-01-19

## Overview

This feature primarily consumes existing data models from the Tenant Service. New models are limited to UI-specific view models for health monitoring and client-side state.

---

## Existing Entities (Tenant Service)

### Organization

**Table**: `organizations`
**Source**: `Sorcha.Tenant.Service.Models.Organization`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique identifier |
| Name | string | Required | Display name |
| Subdomain | string | Unique, 3-50 chars | URL subdomain |
| Status | OrganizationStatus | Enum | Active/Suspended/Deleted |
| CreatorIdentityId | Guid? | FK | User who created org |
| CreatedAt | DateTimeOffset | Default: UtcNow | Creation timestamp |
| Branding | BrandingConfiguration? | JSON | Logo, colors, tagline |

**State Transitions**:
```
[Created] → Active
Active → Suspended (admin action)
Active → Deleted (soft delete)
Suspended → Active (reactivation)
Suspended → Deleted (escalation)
```

---

### UserIdentity (User)

**Table**: `user_identities` (per-org schema)
**Source**: `Sorcha.Tenant.Service.Models.UserIdentity`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique identifier |
| OrganizationId | Guid | FK, Required | Parent organization |
| ExternalIdpUserId | string? | Unique per org | External IDP user ID |
| PasswordHash | string? | - | BCrypt hash (local auth) |
| Email | string | Required | User email |
| DisplayName | string | Required | Friendly name |
| Roles | UserRole[] | Default: [Member] | Assigned roles |
| Status | IdentityStatus | Enum | Active/Suspended/Deleted |
| CreatedAt | DateTimeOffset | Default: UtcNow | Creation timestamp |
| LastLoginAt | DateTimeOffset? | - | Last login time |

**Roles for Admin UI** (subset of UserRole enum):
- `Administrator` - Full org access including user management
- `Designer` - Create/edit blueprints and workflows
- `Member` - Read-only access

---

### AuditLogEntry

**Table**: `audit_log` (per-org schema)
**Source**: `Sorcha.Tenant.Service.Models.AuditLogEntry`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | long | PK, Auto-increment | Log entry ID |
| Timestamp | DateTimeOffset | Indexed | Event time (UTC) |
| EventType | AuditEventType | Enum | Type of event |
| IdentityId | Guid? | FK | Actor identity |
| OrganizationId | Guid | FK | Context organization |
| IpAddress | string? | - | Client IP |
| UserAgent | string? | - | Client user agent |
| Success | bool | - | Operation success |
| Details | Dictionary<string,object>? | JSONB | Event-specific data |

**New Event Types Needed**:
```csharp
// Add to AuditEventType enum
OrganizationCreated,      // New org created
OrganizationUpdated,      // Org details modified
OrganizationDeactivated,  // Org soft-deleted
UserAddedToOrganization,  // User added to org
UserUpdatedInOrganization,// User details/role changed
UserRemovedFromOrganization // User removed from org
```

---

## New UI View Models

### ServiceHealthStatus

**Purpose**: Client-side representation of service health
**Location**: `Sorcha.UI.Core/Models/ServiceHealthStatus.cs`

| Field | Type | Description |
|-------|------|-------------|
| ServiceName | string | Display name (e.g., "Blueprint Service") |
| ServiceKey | string | Internal key (e.g., "blueprint") |
| Status | HealthStatus | Healthy/Degraded/Unhealthy/Unknown |
| LastCheckTime | DateTimeOffset? | Last successful check |
| LastCheckDuration | TimeSpan? | Response time |
| ErrorMessage | string? | Error details if unhealthy |
| Endpoint | string | Health check URL |

```csharp
public enum HealthStatus
{
    Unknown,    // Not yet checked or timeout
    Healthy,    // All checks passed
    Degraded,   // Some checks failing
    Unhealthy   // Critical failure
}
```

---

### PlatformKpis

**Purpose**: Aggregated platform metrics for dashboard
**Location**: `Sorcha.UI.Core/Models/PlatformKpis.cs`

| Field | Type | Description |
|-------|------|-------------|
| TotalOrganizations | int | Active organization count |
| TotalUsers | int | Total users across all orgs |
| HealthyServices | int | Services in healthy state |
| TotalServices | int | Total monitored services |
| LastUpdated | DateTimeOffset | Metrics freshness |

---

## Entity Relationships

```
┌─────────────────────┐
│   Organization      │
│   (tenant root)     │
└─────────┬───────────┘
          │ 1:N
          ▼
┌─────────────────────┐     ┌─────────────────────┐
│    UserIdentity     │────▶│    AuditLogEntry    │
│   (org members)     │     │   (activity log)    │
└─────────────────────┘     └─────────────────────┘
          │                           ▲
          │                           │
          └───────────────────────────┘
                  (actor reference)
```

---

## Validation Rules

### Organization
- Name: 1-200 characters, non-empty
- Subdomain: 3-50 characters, lowercase alphanumeric + hyphens, unique, not reserved

### User
- Email: Valid email format, unique per organization
- DisplayName: 1-100 characters, non-empty
- Roles: At least one role required, valid enum values only

### Business Rules
- Organization creator automatically becomes Administrator
- Cannot remove last Administrator from organization
- Cannot remove yourself from organization
- Deactivated org users cannot authenticate

---

## Indexes (existing)

| Entity | Index | Purpose |
|--------|-------|---------|
| Organization | IX_Subdomain | Subdomain lookup |
| UserIdentity | IX_Email | Email uniqueness per org |
| UserIdentity | IX_OrganizationId | Org member queries |
| AuditLogEntry | IX_Timestamp | Time-range queries |
| AuditLogEntry | IX_IdentityId | User activity queries |
