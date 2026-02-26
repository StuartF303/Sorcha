# Feature Specification: UI & CLI Modernization

**Feature Branch**: `043-ui-cli-modernization`
**Created**: 2026-02-26
**Status**: Draft
**Input**: UI modernization (activity log, sidebar, footer, wallet management, settings, i18n) and CLI full API coverage audit

## User Scenarios & Testing

### User Story 1 — Activity Log Replaces Toasts (Priority: P1)

A user performing actions across Sorcha (submitting transactions, creating wallets, publishing blueprints) currently sees brief toast popups that disappear after 5 seconds. Important event information is lost. The user needs a persistent, real-time activity log that captures all events — both those triggered by the user and those arriving from the backend (e.g., transaction confirmations, action assignments).

The activity log appears as a bell icon with an unread count badge in the top application bar. Clicking the icon opens a right-side overlay panel (sliding in over existing content, not pushing it) showing events listed newest-first, grouped by date. When the panel is open, the unread count resets automatically. A "Mark All Read" button is also available. Events include a timestamp, severity icon, and descriptive message.

For administrators, the activity log widens scope to include events from all users within their organisation, enabling org-wide operational awareness.

**Why this priority**: Replaces a fundamentally broken UX pattern (transient toasts) with persistent, searchable event history. This is the single most impactful UI improvement for day-to-day usability.

**Independent Test**: Can be tested by performing any action that currently triggers a toast (e.g., creating a wallet) and verifying the event appears in the activity log panel.

**Acceptance Scenarios**:

1. **Given** a logged-in user, **When** they create a wallet, **Then** an event appears in the activity log with timestamp and details, and the unread badge increments by 1
2. **Given** an unread count of 5, **When** the user clicks the bell icon, **Then** the overlay panel slides in from the right showing 5 events newest-first, and the unread count resets to 0
3. **Given** the overlay is open, **When** a new event arrives via SignalR, **Then** it appears at the top of the list in real-time without refreshing
4. **Given** an administrator user, **When** they open the activity log, **Then** they see events from all users in their organisation, with each event showing the originating user
5. **Given** events spanning multiple days, **When** viewing the log, **Then** events are grouped under date headers (e.g., "Today", "Yesterday", "24 Feb 2026")

---

### User Story 2 — Sidebar Navigation Consolidation (Priority: P1)

The current sidebar has separate "Management" and "Admin" sections with inconsistent grouping. A user with admin rights has to look in two places for administrative tools. The sidebar should consolidate these into a single "Administration" section with items ordered alphabetically. Role-based visibility ensures system administrators see all options while organisation administrators see only their org-relevant subset (users, participants, register subscriptions — not organisation selection or system health).

Additionally, the sidebar currently sits beside the main content and fully disappears when toggled. Instead, it should be controlled by a hamburger menu icon and, when minimised, collapse to show only icons (not fully hide).

**Why this priority**: Navigation is the user's primary way of accessing features. Inconsistent or confusing navigation structure directly impacts every session.

**Independent Test**: Can be tested by logging in as different user roles and verifying the sidebar shows the correct items in alphabetical order, and that the hamburger toggle collapses to icon-only mode.

**Acceptance Scenarios**:

1. **Given** a system administrator, **When** they view the sidebar, **Then** they see a single "Administration" section containing: Organisations, Participants, Peer Network, Registers, Schema Providers, Schema Sectors, Service Principals, System Health, Users, Validator — in alphabetical order
2. **Given** an organisation administrator, **When** they view the sidebar, **Then** they see "Administration" containing only: Participants, Registers, Users — scoped to their organisation
3. **Given** the sidebar is expanded, **When** the user clicks the hamburger icon, **Then** the sidebar collapses to show only icons (not fully hidden)
4. **Given** the sidebar is collapsed to icons, **When** the user clicks the hamburger icon again, **Then** the sidebar expands to show full labels
5. **Given** a regular user with no admin rights, **When** they view the sidebar, **Then** no Administration section appears

---

### User Story 3 — Status Footer Bar (Priority: P1)

A thin, persistent footer spans the full width of the application bottom. It provides at-a-glance operational awareness: the application build version, backend connection health (or peer network health status), and a count of pending actions awaiting the user's attention. If the backend is unreachable, the footer clearly indicates "Offline" status.

**Why this priority**: Users currently have no passive indicator of system health or connectivity. Discovering the system is offline only happens when an action fails. The pending actions count provides a persistent nudge without requiring navigation to the actions page.

**Independent Test**: Can be tested by verifying the footer displays correct version, checking health indicator when services are up/down, and confirming pending action count matches the actions page.

**Acceptance Scenarios**:

1. **Given** a running system, **When** the user views any page, **Then** a thin footer is visible at the bottom showing: version string, connection status (green indicator + "Connected"), and pending action count
2. **Given** the backend is unreachable, **When** the health check fails, **Then** the footer shows a red indicator with "Offline" status
3. **Given** the user has 3 pending actions, **When** they view the footer, **Then** it shows "3 Pending Actions" as a clickable link to the actions page
4. **Given** no pending actions, **When** they view the footer, **Then** the pending actions area shows "No pending actions"

---

### User Story 4 — Wallet Management Improvements (Priority: P2)

Users managing multiple wallets need to designate one as their "default" wallet used for routine signing operations. From the wallet list, users can select any wallet as default. The wallet list supports two view modes: the existing card view and a new list/table view that uses full width to show more detail including expanded address data and algorithm type. Each wallet entry provides a QR code for the address (viewable in a dialog) and a share button that copies the address or opens a platform-appropriate sharing mechanism.

The wallet creation flow now supports the new quantum-safe cryptographic algorithms (ML-DSA-65, ML-KEM-768) alongside existing classical algorithms (ED25519, P-256, RSA-4096).

**Why this priority**: Wallet selection is a frequent operation. Having no default wallet means users must select a wallet for every signing action, adding friction to the most common workflow.

**Independent Test**: Can be tested by creating multiple wallets, setting one as default, verifying the default is pre-selected in signing flows, and switching between list and card views.

**Acceptance Scenarios**:

1. **Given** a user with multiple wallets, **When** they click "Set as Default" on a wallet, **Then** that wallet is marked as default with a visual indicator, and signing flows pre-select it
2. **Given** the wallet list in card view, **When** the user toggles to list view, **Then** wallets display in a full-width table showing: name, full address, algorithm, status, created date, and action buttons
3. **Given** any wallet entry, **When** the user clicks the QR icon, **Then** a dialog shows the wallet address as a QR code suitable for scanning
4. **Given** any wallet entry, **When** the user clicks share, **Then** on desktop the address is copied to clipboard with confirmation; on mobile the platform share sheet opens
5. **Given** the wallet creation form, **When** selecting an algorithm, **Then** ML-DSA-65 and ML-KEM-768 appear alongside ED25519, P-256, and RSA-4096

---

### User Story 5 — Dashboard Wizard Conditional Display (Priority: P2)

The first-time login wizard that guides new users through wallet creation should only appear when the user has no default wallet set. Once a default wallet exists, the dashboard shows the standard KPI view. If a user later deletes all wallets and has no default, the wizard reappears on next dashboard visit.

**Why this priority**: The wizard currently may show unnecessarily for returning users, creating a poor experience. Tying it to default wallet existence creates a clean, predictable flow.

**Independent Test**: Can be tested by logging in as a new user (no wallets), completing the wizard, verifying the dashboard shows normally, then deleting all wallets and verifying the wizard reappears.

**Acceptance Scenarios**:

1. **Given** a new user with no wallets, **When** they navigate to the dashboard, **Then** the wallet creation wizard is displayed
2. **Given** a user who has just created their first wallet via the wizard, **When** the wizard completes, **Then** that wallet is set as default and the dashboard displays the standard KPI view
3. **Given** a returning user with a default wallet, **When** they navigate to the dashboard, **Then** the standard KPI view displays immediately (no wizard)
4. **Given** a user who deletes their last wallet, **When** they next visit the dashboard, **Then** the wizard reappears

---

### User Story 6 — Real-time Validator Dashboard (Priority: P2)

The administrator validator page currently shows static data. It should update in real-time using SignalR to display: which registers are being monitored, whether local validation is actively processing dockets, throughput statistics (dockets processed per minute), queue depth, and last-processed timestamps.

**Why this priority**: Validators are critical infrastructure. Real-time visibility into their operation helps administrators identify processing delays or failures immediately rather than discovering them after the fact.

**Independent Test**: Can be tested by navigating to the validator admin page while transactions are being submitted and verifying stats update without manual refresh.

**Acceptance Scenarios**:

1. **Given** an admin on the validator page, **When** the validator processes a docket, **Then** the statistics update in real-time (no page refresh)
2. **Given** a monitored register, **When** viewing the validator dashboard, **Then** the admin sees: register name, chain height, last validated block, processing status, and dockets/minute throughput
3. **Given** the validator is idle, **When** viewing the dashboard, **Then** status shows "Idle" with time since last activity

---

### User Story 7 — User Settings Expansion (Priority: P3)

The settings page expands beyond connection profiles to include: colour scheme preference (Light, Dark, System — where System follows the OS setting), time display preference (UTC or Local time with timezone display), and language selection. The default language is detected from the browser but can be overridden. Supported languages at launch: English, French, German, Spanish.

Two-factor authentication setup and push notification preferences for pending actions are also available in settings.

**Why this priority**: While important for user comfort and accessibility, these are enhancement features that don't block core workflows. Localisation enables broader adoption but English-first is sufficient for initial users.

**Independent Test**: Can be tested by changing theme to dark mode and verifying the entire UI renders correctly, switching language and verifying all visible text changes, and enabling 2FA.

**Acceptance Scenarios**:

1. **Given** the settings page, **When** the user selects "Dark" theme, **Then** the entire application renders in dark mode immediately without page reload
2. **Given** theme set to "System", **When** the OS switches from light to dark mode, **Then** the application follows automatically
3. **Given** English is active, **When** the user selects French, **Then** all navigation labels, button text, and system messages display in French
4. **Given** the browser language is German, **When** a new user first logs in, **Then** the UI defaults to German
5. **Given** the settings page, **When** the user enables 2FA, **Then** they are guided through TOTP authenticator setup with QR code and backup codes
6. **Given** the settings page, **When** the user enables push notifications, **Then** they receive browser notifications when new pending actions are assigned
7. **Given** time display set to "Local", **When** viewing any timestamp in the application, **Then** times display in the user's local timezone with timezone abbreviation shown

---

### User Story 8 — CLI Full API Coverage (Priority: P2)

The CLI tool currently covers Tenant, Wallet, Register, Transaction, Peer, and Docket service APIs. It must be extended to provide complete coverage of all backend APIs, ensuring that any operation possible through the UI or MCP server is also available via command line. This enables scripting, automation, and tooling integration.

**Why this priority**: The CLI is a key interface for developers, DevOps, and automated workflows. Incomplete coverage means users must fall back to raw HTTP calls for operations the CLI doesn't support, undermining its value.

**Independent Test**: Can be tested by auditing every backend API endpoint and confirming a corresponding CLI command exists with equivalent parameters and output.

**Acceptance Scenarios**:

1. **Given** the Blueprint Service API, **When** a user runs `sorcha blueprint list`, **Then** they see all blueprints; similar commands exist for create, get, publish, delete, and version management
2. **Given** the Participant Identity API, **When** a user runs `sorcha participant register`, **Then** they can register a participant with wallet linking; similar commands exist for list, get, update, search, and wallet-link management
3. **Given** the Credential/VC API, **When** a user runs `sorcha credential list`, **Then** they see all credentials; similar commands exist for issue, present, verify, and revoke
4. **Given** the Validator Service API, **When** a user runs `sorcha validator status`, **Then** they see validator status for all monitored registers; similar commands exist for start, stop, process, and chain integrity checks
5. **Given** the Admin APIs, **When** a user runs `sorcha admin health`, **Then** they see service health status; similar commands exist for schema sectors, schema providers, and system alerts
6. **Given** any CLI command, **When** the user adds `--output json`, **Then** the output is valid JSON suitable for piping to other tools

---

### Edge Cases

- What happens when the SignalR connection drops while the activity log is open? Events are queued locally and reconciled on reconnection. A "Reconnecting..." indicator is shown.
- What happens when the user has thousands of activity log events? The log implements virtual scrolling and only loads events on demand (paginated from the backend). Older events can be fetched by scrolling.
- What happens when the OS theme changes while the application is running? The System theme preference listens for the operating system preference change and updates immediately.
- What happens when a translation is missing for a UI string? The system falls back to English for any untranslated strings.
- What happens when the CLI is used with an older backend version that doesn't support new endpoints? The CLI reports a clear error message indicating the backend version doesn't support the requested operation, with the minimum required version.

## Requirements

### Functional Requirements

**Activity Log:**
- **FR-001**: System MUST capture all user-initiated events (wallet operations, transaction submissions, blueprint actions) and backend-pushed events (confirmations, action assignments) in a persistent activity log with 90-day server-side retention and automatic purge of older events
- **FR-002**: System MUST display an unread event count badge on the activity log icon in the top application bar, updating in real-time
- **FR-003**: System MUST show events grouped by date (Today, Yesterday, specific dates) with newest events first
- **FR-004**: System MUST reset the unread count when the activity log overlay is opened
- **FR-005**: System MUST provide a "Mark All Read" action in the activity log
- **FR-006**: System MUST show organisation-wide events to administrator users, with each event attributed to the originating user

**Sidebar Navigation:**
- **FR-007**: System MUST consolidate Management and Admin sections into a single "Administration" section with items in alphabetical order
- **FR-008**: System MUST show administration items based on user role: full set for system admins, org-scoped subset for organisation admins, none for regular users
- **FR-009**: System MUST support a collapsed sidebar mode showing only icons, controlled by a hamburger menu toggle

**Status Footer:**
- **FR-010**: System MUST display a persistent footer showing: application version, backend connection health status, and pending action count
- **FR-011**: System MUST indicate "Offline" state when backend services are unreachable
- **FR-012**: System MUST make the pending action count a clickable link to the pending actions page

**Wallet Management:**
- **FR-013**: System MUST allow users to designate any wallet as their default wallet
- **FR-014**: System MUST pre-select the default wallet in all signing flows
- **FR-015**: System MUST support both card view and list/table view for the wallet list, with a toggle to switch between them
- **FR-016**: System MUST provide a QR code display for each wallet address
- **FR-017**: System MUST provide a share/copy mechanism for wallet addresses
- **FR-018**: System MUST support ML-DSA-65 and ML-KEM-768 algorithm selection during wallet creation

**Dashboard:**
- **FR-019**: System MUST display the wallet creation wizard only when the user has no default wallet
- **FR-020**: System MUST set the first created wallet as default upon wizard completion

**Validator Dashboard:**
- **FR-021**: System MUST display real-time validator statistics including: monitored registers, processing status, docket throughput, queue depth, and last-processed timestamps

**Settings:**
- **FR-022**: System MUST support Light, Dark, and System colour scheme preferences, with immediate application
- **FR-023**: System MUST support UTC and Local time display preferences, affecting all timestamps throughout the application
- **FR-024**: System MUST support English, French, German, and Spanish languages, with browser-detected default and manual override
- **FR-025**: System MUST support two-factor authentication setup via TOTP with QR code and backup codes, including backend secret storage, verification endpoint, and login enforcement
- **FR-026**: System MUST support push notification preferences for pending action alerts

**CLI Coverage:**
- **FR-027**: CLI MUST provide commands for the full Blueprint Service API: list, get, create, publish, delete, version management
- **FR-028**: CLI MUST provide commands for the full Participant Identity API: register, list, get, update, search, wallet-link, and wallet-link verification
- **FR-029**: CLI MUST provide commands for the Credential/VC API: list, get, issue, present, verify, revoke
- **FR-030**: CLI MUST provide commands for the Validator Service admin API: status, start, stop, process, chain integrity
- **FR-031**: CLI MUST provide commands for admin operations: health checks, schema sectors, schema providers, system alerts
- **FR-032**: CLI MUST support `--output json`, `--output table`, and `--output csv` for all commands

### Key Entities

- **ActivityEvent**: Represents a user or system event with timestamp, severity, message, source user, and read/unread status
- **UserPreferences**: Stores per-user settings server-side in the Tenant Service user profile, including theme, language, time format, default wallet, notification preferences, and 2FA status — consistent across all devices and browsers
- **WalletDefault**: Links a user to their designated default wallet address

## Clarifications

### Session 2026-02-26

- Q: Should activity log events be stored client-side, server-side, or hybrid? → A: Server-side with API — new events endpoint on Blueprint Service, all events persisted, queried via REST. Admin org-wide views and cross-device access require server persistence.
- Q: Is 2FA backend implementation (Tenant Service) in scope or deferred? → A: Full scope — UI setup flow + Tenant Service backend including TOTP secret storage, verification endpoint, backup code generation, and login enforcement.
- Q: How long should activity log events be retained server-side? → A: 90 days with automatic purge.
- Q: Should default wallet preference be client-side or server-side? → A: Server-side — persist in Tenant Service user profile, consistent across devices and browsers.
- Q: Should all user preferences be server-side or split between browser and server? → A: All server-side — theme, language, time format, wallet, notifications all in Tenant Service user profile for cross-device consistency.

## Assumptions

- The existing SignalR hub infrastructure (actions, register, chat) will be extended with an events hub for real-time push of new activity events to connected clients
- Activity log events are persisted server-side via a new events API endpoint on the Blueprint Service, enabling admin org-wide queries, cross-device access, and durable event history
- The default wallet preference will be persisted server-side via the Tenant Service user profile, ensuring consistency across devices and browsers
- QR code generation will happen client-side to avoid additional backend dependencies
- All user preferences (theme, language, time format, default wallet, notification settings) are persisted server-side in the Tenant Service user profile for cross-device consistency
- Localisation resource files will use standard resource file format with one file per language per component area
- The CLI already supports the output format infrastructure (table, json, csv) — new commands will use the existing formatters
- 2FA implementation will use standard TOTP (RFC 6238) compatible with Google Authenticator, Authy, and similar apps. Backend changes to the Tenant Service are in scope: TOTP secret storage per user, verification endpoint, backup code generation, and enforced 2FA check during login
- Push notifications will use the Web Push API with service worker registration

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can access any past event from the activity log within 3 clicks from any page
- **SC-002**: Real-time events appear in the activity log within 2 seconds of occurrence
- **SC-003**: Navigation to any administrative function requires at most 2 clicks from the sidebar
- **SC-004**: System health status is visible at all times without any clicks (footer)
- **SC-005**: Wallet address sharing completes in under 3 seconds (copy to clipboard or share sheet)
- **SC-006**: Theme switching applies immediately with no visible flash or reload
- **SC-007**: All user-visible text is translatable, with 4 complete language packs at launch
- **SC-008**: 100% of backend API endpoints have corresponding CLI commands
- **SC-009**: CLI commands produce valid, parseable JSON when `--output json` is specified
- **SC-010**: The validator dashboard updates statistics within 3 seconds of a docket being processed
- **SC-011**: First-time users complete wallet setup within 2 minutes via the guided wizard
