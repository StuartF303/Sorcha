# Feature Specification: Admin Dashboard and Management

**Feature Branch**: `011-admin-dashboard`
**Created**: 2026-01-19
**Status**: Draft
**Input**: User description: "Admin Dashboard and Management Features for Sorcha.UI - includes: 1) Service status dashboard with health monitoring and KPIs for system administrators, 2) Tenant organization CRUD operations for system administrators, 3) Organizational participant management for org administrators. Role-based access control required."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Service Status Dashboard (Priority: P1)

As a **System Administrator**, I need to view the health status and key performance indicators of all Sorcha platform services so that I can monitor system availability and respond quickly to issues.

**Why this priority**: Real-time visibility into system health is critical for maintaining platform reliability. Without this, administrators cannot proactively detect and respond to issues before they impact users.

**Independent Test**: Can be fully tested by logging in as a system admin, navigating to the Admin dashboard, and verifying all service statuses and KPIs are displayed with current data.

**Acceptance Scenarios**:

1. **Given** a system administrator is logged in, **When** they navigate to the Admin dashboard, **Then** they see the health status (healthy/degraded/unhealthy) of all platform services.

2. **Given** a system administrator is viewing the dashboard, **When** a service becomes unhealthy, **Then** the dashboard reflects the status change within 30 seconds (polling interval).

3. **Given** a system administrator is viewing the dashboard, **When** they look at the KPI section, **Then** they see metrics including: total organizations, total users, active sessions, and service uptime percentages.

4. **Given** a system administrator is viewing the dashboard, **When** they click on a service card, **Then** they see detailed health information including last check time and any error messages.

---

### User Story 2 - Tenant Organization Management (Priority: P2)

As a **System Administrator**, I need to list, create, and modify tenant organizations so that I can onboard new customers and manage the multi-tenant platform.

**Why this priority**: Organization management is essential for platform operations but can be deferred behind basic monitoring. New customer onboarding is a core business operation.

**Independent Test**: Can be fully tested by creating a new organization, viewing it in the list, updating its details, and deactivating it.

**Acceptance Scenarios**:

1. **Given** a system administrator is on the Organizations tab, **When** the page loads, **Then** they see a paginated list of all organizations with name, subdomain, status, and creation date.

2. **Given** a system administrator clicks "Create Organization", **When** they fill in required fields (name, subdomain) and submit, **Then** a new organization is created and appears in the list.

3. **Given** a system administrator views an organization, **When** they click "Edit", **Then** they can modify the organization name, status, and branding configuration.

4. **Given** a system administrator views an active organization, **When** they click "Deactivate" and confirm, **Then** the organization status changes to "Deleted" and users can no longer authenticate.

5. **Given** a system administrator is creating an organization, **When** they enter a subdomain, **Then** the system validates availability and format in real-time.

6. **Given** a non-administrator user attempts to access organization management, **When** they navigate to the Organizations tab, **Then** they see an "Access Denied" message or the tab is hidden.

---

### User Story 3 - Organization Participant Management (Priority: P3)

As an **Organization Administrator**, I need to list, create, and modify participants (users) within my organization so that I can control who has access to our organization's resources.

**Why this priority**: User management is important for day-to-day operations but requires organizations to exist first. This builds on the organization management foundation.

**Independent Test**: Can be fully tested by navigating to an organization's user list, adding a new user, updating their role, and removing them.

**Acceptance Scenarios**:

1. **Given** an organization administrator is viewing their organization, **When** they navigate to the Users tab, **Then** they see a list of all users with email, display name, role, and status.

2. **Given** an organization administrator clicks "Add User", **When** they enter an email address, display name, and role, **Then** the user is added to the organization.

3. **Given** an organization administrator views a user, **When** they click "Edit", **Then** they can modify the user's display name, role, and active status.

4. **Given** an organization administrator views a user (not themselves), **When** they click "Remove" and confirm, **Then** the user is removed from the organization.

5. **Given** an organization administrator attempts to remove themselves, **When** they try to confirm removal, **Then** they see an error message preventing self-removal.

6. **Given** a standard member views their organization, **When** they look at the Users tab, **Then** they can view the user list but cannot add, edit, or remove users.

---

### Edge Cases

- What happens when a system administrator deactivates an organization with active user sessions? (Sessions should be invalidated immediately)
- How does the system handle network timeouts when fetching service health? (Show "Unknown" status with last successful check time)
- What happens when an organization administrator is demoted to standard member while editing a user? (Show permission error on save attempt)
- What happens when two administrators simultaneously edit the same organization? (Last write wins with optimistic concurrency)
- How does the system handle creating an organization with a subdomain that becomes unavailable during form completion? (Re-validate on submit, show conflict error)

## Requirements *(mandatory)*

### Functional Requirements

**Dashboard & Monitoring**
- **FR-001**: System MUST display real-time health status for all Sorcha platform services (Blueprint, Register, Wallet, Tenant, Validator, Peer, API Gateway)
- **FR-002**: System MUST poll service health endpoints at configurable intervals (default: 30 seconds)
- **FR-003**: System MUST display KPIs including: total organizations, total users, and service uptime percentages
- **FR-004**: System MUST visually differentiate between Healthy (green), Degraded (yellow), and Unhealthy (red) service states
- **FR-005**: System MUST show the timestamp of the last successful health check for each service

**Organization Management**
- **FR-006**: System MUST allow system administrators to view a paginated list of all organizations
- **FR-007**: System MUST allow system administrators to create new organizations with name and subdomain
- **FR-008**: System MUST validate subdomain format (3-50 characters, alphanumeric + hyphens, lowercase) and availability in real-time
- **FR-009**: System MUST allow system administrators to update organization name, status, and branding
- **FR-010**: System MUST allow system administrators to deactivate organizations (soft delete)
- **FR-011**: System MUST support organization statuses: Active, Suspended, Deleted
- **FR-012**: System MUST display organization creator and creation date

**Participant Management**
- **FR-013**: System MUST allow organization administrators to view all users in their organization
- **FR-014**: System MUST allow organization administrators to add users with email, display name, and role (Administrator, Designer, or Member)
- **FR-015**: System MUST allow organization administrators to modify user display name, role, and active status
- **FR-016**: System MUST allow organization administrators to remove users from the organization
- **FR-017**: System MUST prevent organization administrators from removing themselves
- **FR-018**: System MUST allow standard members to view (read-only) the user list

**Access Control**
- **FR-019**: System MUST enforce role-based access control with organization roles: Administrator (full organization access including user management), Designer (create/edit blueprints and workflows), Member (read-only access); System Administrators can manage all organizations
- **FR-020**: System MUST hide or disable UI elements based on user permissions
- **FR-021**: System MUST validate permissions on both client and server side
- **FR-022**: System MUST log all admin actions (organization create/edit/delete, user add/edit/remove) to audit storage with timestamp, actor, action type, and affected entity

### Key Entities

- **Service Health**: Represents the operational status of a platform service (service name, status, last check time, error details)
- **Organization**: A tenant entity with name, subdomain, status, branding, and identity provider configuration
- **User**: A user belonging to an organization with email, display name, role (Administrator/Designer/Member), and active status
- **KPI Metrics**: Aggregated platform statistics (organization count, user count, uptime percentages)
- **Audit Entry**: Record of an admin action with timestamp, actor identity, action type, and affected entity reference

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administrators can view health status of all 7 services within 3 seconds of page load
- **SC-002**: Health status updates reflect actual service state within 30 seconds of a change
- **SC-003**: Administrators can create a new organization in under 2 minutes
- **SC-004**: Organization list supports browsing 100+ organizations without performance degradation
- **SC-005**: User list loads within 2 seconds for organizations with up to 500 users
- **SC-006**: Subdomain validation provides feedback within 500 milliseconds of user input
- **SC-007**: 100% of unauthorized access attempts are blocked with appropriate error messages
- **SC-008**: Administrators can complete user add/edit/remove operations in under 30 seconds each

## Assumptions

- The Tenant Service API endpoints for organization and user management already exist and are functional
- Each service exposes a standard `/health` endpoint compatible with ASP.NET Core Health Checks
- The existing authentication system (JWT-based) correctly populates user roles and permissions in token claims
- MudBlazor component library is available and should be used for UI consistency
- The polling approach for health checks is acceptable (vs. real-time push via SignalR) for the initial implementation
- Branding configuration (logo, colors) management is optional for MVP and can use simple text inputs

## Clarifications

### Session 2026-01-19

- Q: What roles can be assigned to users within an organization? → A: Three roles: Administrator, Designer, Member
- Q: Should admin actions be logged for audit purposes? → A: Yes, log all admin actions (create/edit/delete) to audit storage; viewer UI deferred

## Out of Scope

- Real-time health notifications via SignalR (polling is sufficient for MVP)
- Advanced analytics and historical trend charts
- Audit log viewer (placeholder exists, separate feature)
- Identity provider (IDP) configuration UI
- Bulk user import/export functionality
- Organization billing and subscription management
- Custom permission configuration beyond predefined roles
