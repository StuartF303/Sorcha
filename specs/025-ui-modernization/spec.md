# Feature Specification: Sorcha UI Modernization

**Feature Branch**: `025-ui-modernization`
**Created**: 2026-02-07
**Status**: Draft
**Input**: Comprehensive overhaul of the Sorcha.UI Blazor WASM application to close gaps between UI capabilities and backend service APIs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent Identifier Truncation (Priority: P1 — Cross-cutting)

All pages throughout the application that display long identifiers (wallet addresses, transaction IDs, register IDs, peer IDs, organization IDs, docket hashes) use a consistent truncation pattern: show the first few characters and at least the last 6 characters with an ellipsis in between (e.g., "0x3f8a...b4c2e1"). Users can click or hover to see the full value and copy it to clipboard. This is a reusable component that all other stories depend on.

**Why this priority**: P1 because this is a cross-cutting UX pattern that affects every other story. Implementing it first provides a reusable component for all subsequent pages.

**Independent Test**: Can be tested by rendering any page with long identifiers and verifying the truncation pattern is applied consistently, hover/click reveals the full value, and copy-to-clipboard works.

**Acceptance Scenarios**:

1. **Given** any page displaying a long identifier (>12 characters), **When** the page renders, **Then** the identifier shows the first few characters, an ellipsis, and at least the last 6 characters.
2. **Given** a truncated identifier, **When** the user hovers over it, **Then** a tooltip shows the full identifier value.
3. **Given** a truncated identifier, **When** the user clicks on it, **Then** the full value is copied to the clipboard and a brief confirmation is shown.
4. **Given** different types of identifiers (addresses with "0x" prefix, UUIDs, hex hashes), **When** truncation is applied, **Then** the pattern preserves any prefix (e.g., "0x") before truncating the remainder.
5. **Given** an identifier shorter than 12 characters, **When** the page renders, **Then** the identifier displays in full without truncation.

---

### User Story 2 - Administration & Navigation Restructure (Priority: P1)

An administrator navigates to the management section and finds a flattened navigation structure where each admin concern (System Health, Peer Network, Organizations, Validator, Service Principals) has its own direct navigation link instead of being buried inside a single tabbed "Administration" page. This story restructures navigation for easier access.

**Why this priority**: Admin functions are foundational — they control who can participate, which organizations exist, and how services are configured. The current tabbed structure requires extra clicks to reach specific functions.

**Independent Test**: Can be tested by logging in as an admin and verifying the sidebar shows direct links to each admin page, each page loads correctly, and the click count to reach any admin function is reduced by at least one.

**Acceptance Scenarios**:

1. **Given** a logged-in administrator, **When** they view the navigation sidebar, **Then** they see direct links for System Health, Peer Network, Organizations, Validator, and Service Principals.
2. **Given** the new navigation structure, **When** an admin clicks any admin page link, **Then** they go directly to that page without needing to select a tab.
3. **Given** the existing Administration page with tabs, **When** the restructure is complete, **Then** the tabbed Administration page is replaced by individual pages.

---

### User Story 3 - Organization Management (Priority: P1)

An administrator can create, view, edit, and deactivate organizations through a dedicated Organizations page. This enables multi-tenant setup — a prerequisite for workflows involving multiple participants from different organizations.

**Why this priority**: Without organization management, multi-tenant workflows cannot be properly set up. This is a foundational admin capability.

**Independent Test**: Can be tested by navigating to the Organizations page, creating a new organization, editing it, viewing the list, and deactivating it — all via the Tenant Service API.

**Acceptance Scenarios**:

1. **Given** an administrator on the Organizations page, **When** they click "Create Organization", fill in the form (name, description), and submit, **Then** a new organization is created via the Tenant Service API and appears in the list.
2. **Given** an administrator on the Organizations page, **When** they view the organization list, **Then** each organization shows its name, status, member count, and creation date. Organization IDs use the truncated display pattern.
3. **Given** an administrator on the Organizations page, **When** they select an organization and click "Edit", **Then** they can update its name, description, and status (active/suspended).
4. **Given** an administrator on the Organizations page, **When** they click "Deactivate" on an organization, **Then** a confirmation dialog appears, and upon confirmation the organization status changes to suspended.

---

### User Story 4 - Validator Admin Panel (Priority: P2)

An administrator can view validator service state including mempool status, consensus state, and recent validation activity through a dedicated Validator page.

**Why this priority**: Visibility into the validation pipeline is important for monitoring system health and diagnosing transaction processing issues.

**Independent Test**: Can be tested by navigating to the Validator page and verifying mempool counts, consensus state, and validation activity are displayed from the Validator Service API.

**Acceptance Scenarios**:

1. **Given** an administrator on the Validator page, **When** the page loads, **Then** they see mempool status showing pending transaction count and oldest entry age.
2. **Given** an administrator on the Validator page, **When** they view the consensus section, **Then** they see current consensus state and recent consensus activity.
3. **Given** an administrator on the Validator page, **When** validation activity occurs, **Then** the recent activity list updates to show new validation events.

---

### User Story 5 - Service Principal Management (Priority: P2)

An administrator can view and manage service-to-service credentials (service principals) through a dedicated page, seeing status, last usage, and expiration dates.

**Why this priority**: Service principals control inter-service authentication. Visibility into their status prevents authentication failures from expired or misconfigured credentials.

**Independent Test**: Can be tested by navigating to the Service Principals page and verifying the credential list displays correctly with status and metadata.

**Acceptance Scenarios**:

1. **Given** an administrator on the Service Principals page, **When** the page loads, **Then** they see all service-to-service credentials with name, status, last used date, and expiration.
2. **Given** a service principal nearing expiration, **When** the admin views it, **Then** a visual indicator warns of impending expiration.
3. **Given** an administrator, **When** they select a service principal, **Then** they can view its details and associated permissions.

---

### User Story 6 - Dashboard Live Statistics (Priority: P3)

A user lands on the dashboard and sees real-time statistics pulled from the gateway's dashboard endpoint instead of hardcoded placeholder values. Stat cards show live counts for active blueprints, recent transactions, connected peers, and active wallets.

**Why this priority**: The dashboard is the first page users see. Showing real data communicates system health and activity.

**Independent Test**: Can be tested by loading the dashboard page and verifying stat cards display live values that change when backend data changes.

**Acceptance Scenarios**:

1. **Given** a logged-in user, **When** they load the dashboard, **Then** stat cards display live values (active blueprints, recent transactions, connected peers, active wallets) fetched from the backend.
2. **Given** a user on the dashboard, **When** the backend data changes, **Then** the stat cards update on the next polling cycle or page refresh.
3. **Given** the dashboard endpoint is unreachable, **When** the user loads the dashboard, **Then** stat cards show a "data unavailable" indicator rather than stale or zero values.

---

### User Story 7 - Workflow Instance Management (Priority: P4)

A user navigates to "My Workflows" and sees their active workflow instances, can view workflow details including current action and participant status, and can take actions on workflows awaiting their input. The "My Actions" page shows all actions assigned to the current user across all workflows.

**Why this priority**: Workflows are the core value proposition of Sorcha. Users need to see and act on their pending workflow items.

**Independent Test**: Can be tested by creating a workflow instance via the API, verifying it appears in "My Workflows", and that pending actions appear in "My Actions" with the ability to submit data.

**Acceptance Scenarios**:

1. **Given** a user with active workflow instances, **When** they navigate to "My Workflows", **Then** they see a list with blueprint name, status, current step, and creation date.
2. **Given** a user viewing a workflow instance, **When** they click on it, **Then** they see participants, action history, current pending action, and data payloads.
3. **Given** a user with pending actions, **When** they navigate to "My Actions", **Then** they see all actions awaiting their input across all workflows, sorted by urgency.
4. **Given** a user on an action requiring data submission, **When** they fill in the required fields and submit, **Then** the action is submitted via the Blueprint Service orchestration API and the workflow advances.
5. **Given** long identifiers (workflow IDs, transaction IDs), **When** displayed, **Then** they use the truncated format.

---

### User Story 8 - Blueprint Cloud Persistence & Publishing (Priority: P5)

A user creates and edits blueprints in the Designer, and their work is saved to the Blueprint Service API rather than browser LocalStorage. Blueprints can be versioned, published, and browsed in the Blueprints library.

**Why this priority**: LocalStorage persistence means blueprints are lost when switching devices or clearing browser data. Cloud persistence is essential for production use.

**Independent Test**: Can be tested by creating a blueprint in the Designer, clearing browser data, refreshing, and verifying the blueprint is still available from the API.

**Acceptance Scenarios**:

1. **Given** a user in the Designer, **When** they create or edit a blueprint, **Then** changes are saved to the Blueprint Service API.
2. **Given** a user viewing their blueprints, **When** they navigate to the Blueprints page, **Then** they see all their blueprints from the API with name, version, status (draft/published), and last modified date.
3. **Given** a user with a draft blueprint, **When** they click "Publish", **Then** they see a validation review screen and can confirm publication.
4. **Given** a published blueprint, **When** a user views it, **Then** they see version history and can view previous versions.

---

### User Story 9 - Wallet Management (Priority: P6)

A user navigates to "My Wallet" and can create wallets, view wallet details including addresses, and perform signing operations. Replaces the current placeholder page with real Wallet Service integration.

**Why this priority**: Wallets are required for transaction signing and identity verification in workflows.

**Independent Test**: Can be tested by creating a wallet, verifying it appears in the list, viewing addresses, and performing a signing operation.

**Acceptance Scenarios**:

1. **Given** a user on "My Wallet", **When** they click "Create Wallet", **Then** they can create a new HD wallet with a chosen name, and the mnemonic is shown once for backup.
2. **Given** a user with wallets, **When** they view the wallet list, **Then** each wallet shows name, primary address (truncated), creation date, and key count.
3. **Given** a user viewing wallet details, **When** they open a wallet, **Then** they see derived addresses (truncated), public keys, and signing capabilities.
4. **Given** a user needing to sign, **When** they select a wallet and initiate signing, **Then** they can sign data and see the signature result.
5. **Given** the mnemonic display during creation, **When** the user dismisses it, **Then** the mnemonic is never shown again.

---

### User Story 10 - Transaction History (Priority: P7)

A user navigates to "My Transactions" and can view their transaction history from the Register Service with filtering, sorting, and detail views. Replaces the current placeholder page.

**Why this priority**: Transaction visibility is essential for auditing and verifying data has been properly recorded.

**Independent Test**: Can be tested by submitting transactions via the API, then verifying they appear in "My Transactions" with correct details and filtering.

**Acceptance Scenarios**:

1. **Given** a user on "My Transactions", **When** the page loads, **Then** they see a paginated list with date, register name, type, and status.
2. **Given** a user viewing transactions, **When** they click on a transaction, **Then** they see full details including payload summary, participants, timestamps, and chain position.
3. **Given** a user with many transactions, **When** they use filters, **Then** they can filter by register, date range, type, and status.
4. **Given** transaction IDs and register IDs, **When** displayed, **Then** they use the truncated format.

---

### User Story 11 - Template Library (Priority: P8)

A user browses the template library and can view, evaluate, and use blueprint templates fetched from the backend template API instead of hardcoded data.

**Why this priority**: Templates accelerate blueprint creation. Backend-driven templates enable centralized management.

**Independent Test**: Can be tested by verifying templates are fetched from the API and can be used to start a new blueprint.

**Acceptance Scenarios**:

1. **Given** a user on the Templates page, **When** the page loads, **Then** they see templates from the backend API with name, description, category, and usage count.
2. **Given** a user viewing a template, **When** they click "Use Template", **Then** a new blueprint is created from the template and opened in the Designer.
3. **Given** templates with different categories, **When** the user filters by category, **Then** only matching templates are shown.

---

### User Story 12 - Explorer Enhancements (Priority: P9)

A user exploring registers can view docket chains and individual docket details within register detail pages. An advanced OData query builder allows cross-register searches without requiring knowledge of OData syntax.

**Why this priority**: Deep ledger inspection is important for advanced users and debugging. The query builder makes the Register Service's query API accessible to non-technical users.

**Independent Test**: Can be tested by viewing a register's docket chain and building an OData query through the visual builder.

**Acceptance Scenarios**:

1. **Given** a user viewing a register detail page, **When** they navigate to the "Docket Chain" tab, **Then** they see dockets with version, hash (truncated), transaction count, and timestamps.
2. **Given** a user viewing a docket, **When** they click on it, **Then** they see docket details including transaction IDs (truncated), previous docket hash, and integrity status.
3. **Given** a user on the Explorer page, **When** they open the query builder, **Then** they can visually construct OData filter expressions by selecting fields, operators, and values.
4. **Given** a user has built a query, **When** they execute it, **Then** results display in a table with pagination, and the raw OData query is shown for reference.

---

### Edge Cases

- What happens when the backend API is unreachable? All pages show graceful "service unavailable" states with retry options, not blank screens or unhandled errors.
- What happens when a user has no data (no wallets, no transactions, no workflows)? Empty state pages show helpful prompts guiding the user to create their first item.
- What happens when paginated data returns zero results for a filter? The UI shows "No results match your filter" with an option to clear filters.
- What happens when a wallet mnemonic display is dismissed? It is not recoverable from the UI (security requirement — mnemonics are shown once).
- What happens when a blueprint publish validation fails? The user sees specific validation errors with guidance on how to fix them.
- What happens when identifiers are very short (< 12 characters)? They display in full without truncation.
- What happens when organization or blueprint names contain special characters? They display correctly with proper encoding.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a flattened navigation structure where admin functions (System Health, Peer Network, Organizations, Validator, Service Principals) each have their own navigation link rather than being consolidated into a single tabbed page.
- **FR-002**: System MUST provide an Organization Management page allowing administrators to create, view, edit, and deactivate organizations via the Tenant Service API.
- **FR-003**: System MUST provide a Validator Admin page showing mempool status, consensus state, and recent validation activity.
- **FR-004**: System MUST provide a Service Principal management page for viewing and managing service-to-service credentials.
- **FR-005**: System MUST wire dashboard stat cards to a live backend endpoint with graceful fallback when unavailable.
- **FR-006**: System MUST replace the "My Workflows" placeholder with a real workflow instance list fetched from the Blueprint Service orchestration API.
- **FR-007**: System MUST replace the "My Actions" placeholder with a real pending actions list showing all actions awaiting the current user's input.
- **FR-008**: System MUST allow users to submit action data through forms generated from the action's data schema.
- **FR-009**: System MUST persist Designer blueprints to the Blueprint Service API instead of browser LocalStorage.
- **FR-010**: System MUST provide a blueprint publishing flow with validation review before publication.
- **FR-011**: System MUST support blueprint version history viewing.
- **FR-012**: System MUST replace the "My Wallet" placeholder with real Wallet Service integration supporting create, view, and sign operations.
- **FR-013**: System MUST show wallet mnemonics exactly once during creation and never again.
- **FR-014**: System MUST replace the "My Transactions" placeholder with real transaction queries from the Register Service with filtering and pagination.
- **FR-015**: System MUST fetch templates from the backend template API instead of using hardcoded data.
- **FR-016**: System MUST provide docket chain inspection within register detail pages.
- **FR-017**: System MUST provide a visual OData query builder for cross-register searches.
- **FR-018**: System MUST implement a reusable identifier truncation component that shows the first few characters and at least the last 6 characters with an ellipsis, with hover-to-reveal and click-to-copy functionality.
- **FR-019**: System MUST apply the truncation pattern consistently to all long identifiers across all pages.
- **FR-020**: System MUST show graceful "service unavailable" states on all pages when backend APIs are unreachable.
- **FR-021**: System MUST show helpful empty states when users have no data, guiding them to create their first item.

### Key Entities

- **Organization**: Multi-tenant container with name, description, status (active/suspended), member count, and unique identifier. Managed via Tenant Service.
- **Workflow Instance**: A running execution of a blueprint, with current action, participants, status (active/completed/failed), and action history.
- **Pending Action**: An action within a workflow that requires user input, with data schema, deadline, and assignment.
- **Blueprint (Cloud)**: A workflow definition persisted in the Blueprint Service with version history, status (draft/published), and owner.
- **Template**: A reusable blueprint starting point with name, description, category, and evaluation metadata.
- **Wallet**: An HD wallet with derived addresses, signing keys, and creation metadata. Managed via Wallet Service.
- **Transaction**: A recorded ledger entry with register association, type, status, participants, and payload summary.
- **Docket**: A chain link in a register's integrity chain, referencing transactions and the previous docket hash.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 3 placeholder pages (My Wallet, My Transactions, My Workflows) display real data from their respective backend services.
- **SC-002**: Administrators can complete organization creation in under 2 minutes from the Organizations page.
- **SC-003**: Dashboard stat cards display live values that reflect actual backend state within 30 seconds of page load.
- **SC-004**: Users can find and take action on a pending workflow action within 3 clicks from the dashboard.
- **SC-005**: Blueprint persistence survives browser data clearing — blueprints are retrievable on a new device after login.
- **SC-006**: 100% of long identifiers across all pages use the consistent truncation pattern with hover-to-reveal and click-to-copy.
- **SC-007**: All pages show graceful degradation (meaningful messages, not blank screens) when any backend service is unavailable.
- **SC-008**: Navigation restructuring reduces the average number of clicks to reach any admin function by at least 1 click compared to the current tabbed structure.
- **SC-009**: The visual OData query builder allows users to construct and execute queries without typing raw OData syntax.
- **SC-010**: All new pages have accompanying Playwright E2E tests verifying core functionality.

## Assumptions

- The Tenant Service API already supports organization CRUD operations (or will be extended as needed).
- The Blueprint Service orchestration API exposes workflow instance listing and action submission endpoints.
- The Wallet Service API supports wallet creation, listing, address derivation, and signing.
- The Register Service exposes transaction query endpoints with OData filtering.
- The gateway dashboard endpoint exists or will be created to aggregate statistics.
- The template API exists in the Blueprint Service or will be created.
- JWT authentication is already in place for all service-to-service and user-to-service calls.
- MudBlazor is the established component library and should continue to be used for consistency.
