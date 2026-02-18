# Feature Specification: New Submission Page

**Feature Branch**: `037-new-submission-page`
**Created**: 2026-02-18
**Status**: Draft
**Input**: User description: "Redesign the New Submission page to serve as a service directory where users can browse available blueprints across their accessible registers and start new submissions"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Available Services (Priority: P1)

A user navigates to the "New Submission" page and sees a directory of services (blueprints) they can start, organised by register (authority/jurisdiction). Each register section shows its name and description, with the available blueprints listed beneath it. The user can scan across multiple registers to find the service they need.

For example, a citizen might see "City of Edinburgh - Street Vendors" with a "Street Vendor Permit Application" blueprint, and "SEPA - Environmental" with an "EPA Septic Tank Permit" blueprint — both using the same underlying template but published to different authority registers.

**Why this priority**: Without the ability to discover available services, no submissions can be made. This is the foundation — users must see what they can apply for before they can apply.

**Independent Test**: Navigate to `/my-workflows`, verify the page displays registers the user has access to with their published blueprints listed under each. Verify that blueprints requiring specific participant roles are only shown to matching users.

**Acceptance Scenarios**:

1. **Given** a user with access to two registers, each with published blueprints, **When** they navigate to "New Submission", **Then** they see both registers with their respective blueprints grouped beneath each register heading.
2. **Given** a user with a linked wallet, **When** the page loads, **Then** only blueprints where the user's participant role matches Action 0's required participant (or Action 0 is marked as "public") are shown.
3. **Given** a register with no published blueprints accessible to the user, **When** the page loads, **Then** that register section is not displayed (empty registers are hidden).
4. **Given** a user with no accessible registers or no startable blueprints, **When** the page loads, **Then** a clear empty state message is shown explaining that no services are currently available.

---

### User Story 2 - Start a New Submission (Priority: P1)

A user finds the service they want, clicks "Start", and is presented with a form for Action 0 of the blueprint. The form is dynamically generated from the blueprint's data schemas. The user fills in the required fields, the data is signed with their wallet, and submitted into the validation pipeline. This creates a new workflow instance and executes the first action in one seamless step.

**Why this priority**: This is the core purpose of the page — starting a new submission is the primary user action and delivers the main value alongside US1.

**Independent Test**: Click "Start" on a blueprint, fill in the rendered form, submit, and verify a new workflow instance is created with the first action executed and the transaction submitted to the validator.

**Acceptance Scenarios**:

1. **Given** a user clicks "Start" on a blueprint, **When** the submission dialog opens, **Then** a form is rendered based on Action 0's data schemas with all fields matching the schema definitions.
2. **Given** a user with exactly one linked wallet, **When** the submission dialog opens, **Then** that wallet is automatically selected for signing with no wallet picker shown.
3. **Given** a user with multiple linked wallets, **When** the submission dialog opens, **Then** a wallet selector is shown at the top of the form, defaulting to their preferred wallet.
4. **Given** a user fills in all required fields and submits, **When** the form is submitted, **Then** a new workflow instance is created, Action 0 is executed with the signed form data, and a confirmation is shown with the submission reference.
5. **Given** a user fills in the form but leaves required fields empty, **When** they attempt to submit, **Then** validation errors are shown on the relevant fields and submission is blocked.
6. **Given** the submission succeeds, **When** the dialog closes, **Then** the user sees a success notification with the workflow instance reference and can navigate to track it.

---

### User Story 3 - Wallet Selection with Default Preference (Priority: P2)

A user with multiple linked wallets can choose which wallet to use for signing submissions. Their preference is remembered so they don't have to select a wallet each time. If they want to change their default, they can do so from the wallet selector.

**Why this priority**: Most users will have a single wallet (auto-selected by US2). Multi-wallet support is important but secondary to the core browse-and-submit flow.

**Independent Test**: With two linked wallets, start a submission, change the selected wallet, mark it as default, start another submission, and verify the previously selected wallet is pre-selected.

**Acceptance Scenarios**:

1. **Given** a user with multiple wallets and no default set, **When** the wallet selector appears, **Then** the first wallet is selected by default.
2. **Given** a user selects a wallet and marks it as default, **When** they start a new submission later, **Then** the previously defaulted wallet is pre-selected.
3. **Given** a user changes their default wallet preference, **When** they start subsequent submissions, **Then** the new default is used.
4. **Given** a user's default wallet is no longer linked (revoked), **When** they start a new submission, **Then** the system falls back to the first available wallet and clears the stale default.

---

### User Story 4 - Fix Pending Actions Submission Flow (Priority: P2)

The existing "Pending Actions" page (`/my-actions`) has the correct layout for showing actions awaiting user input, but the action execution flow has two gaps: the signing wallet is not populated when the action form opens, and the submission is not actually sent to the backend after the dialog closes. These must be fixed for the end-to-end action flow to work.

**Why this priority**: The Pending Actions page is the complement to New Submission — new submissions create work that appears as pending actions for other participants. Without a working action flow, the pipeline stalls after the first action.

**Independent Test**: Open a pending action, fill in the form, submit, and verify the action is actually submitted to the backend and the action disappears from the pending list.

**Acceptance Scenarios**:

1. **Given** a user opens a pending action, **When** the action form dialog appears, **Then** their wallet is pre-populated (smart default: single wallet auto-selected, multiple wallets use default preference).
2. **Given** a user fills in and submits an action form, **When** the dialog closes with success, **Then** the system calls the submit action endpoint with the signed form data and wallet information.
3. **Given** a successful action submission, **When** the backend confirms, **Then** the action is removed from the pending list and a success notification is shown.
4. **Given** the submission fails (network error, validation failure), **When** the error occurs, **Then** an error notification is shown and the action remains in the pending list for retry.

---

### User Story 5 - Navigation Order Update (Priority: P1)

The "My Activity" navigation section should place "New Submission" before "Pending Actions" to reflect the natural workflow order: users first create submissions, then respond to pending actions that arise from the workflow.

**Why this priority**: This is a trivial change but aligns the navigation with the user's mental model of the workflow lifecycle. Grouped with P1 because it should ship with the page redesign.

**Independent Test**: Open the application and verify "New Submission" appears above "Pending Actions" in the sidebar navigation.

**Acceptance Scenarios**:

1. **Given** a user opens the application, **When** they look at the "My Activity" section in the sidebar, **Then** "New Submission" appears before "Pending Actions".

---

### Edge Cases

- What happens when a user has no linked wallets? The "Start" button should be disabled with a tooltip directing them to link a wallet first.
- What happens when a register becomes unavailable while the user is browsing? Show a graceful error for that register section without affecting others.
- What happens when a blueprint is unpublished between the user browsing and clicking "Start"? Show a clear error message that the service is no longer available.
- What happens when the instance creation succeeds but Action 0 execution fails? The instance exists but is in a recoverable state — the action will appear in "Pending Actions" for the user to retry.
- What happens when the user is mid-form and their session expires? Standard session handling — redirect to login and allow them to return.
- What happens when a register has many blueprints (e.g., 50+)? The register section should be scrollable or paginated, not overwhelming the page.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a service directory on the "New Submission" page, showing available blueprints grouped by register.
- **FR-002**: System MUST query the user's accessible registers and, for each register, retrieve the blueprints available to the user's wallet address.
- **FR-003**: System MUST only display blueprints where the user can fulfil Action 0's participant requirements (matching participant role or "public" role).
- **FR-004**: System MUST render Action 0's data schema as a dynamic form when the user starts a new submission, using the existing form rendering components.
- **FR-005**: System MUST auto-select the user's wallet when they have exactly one linked wallet, showing no wallet picker.
- **FR-006**: System MUST show a wallet selector when the user has multiple linked wallets, defaulting to their preferred wallet.
- **FR-007**: System MUST persist the user's default wallet preference in browser local storage.
- **FR-008**: System MUST create a new workflow instance and execute Action 0 in sequence when the user submits the form, making it feel like a single operation.
- **FR-009**: System MUST sign the form data with the selected wallet before submission, using the canonical JSON serialisation and pre-hashed signing flow.
- **FR-010**: System MUST display "New Submission" before "Pending Actions" in the sidebar navigation.
- **FR-011**: System MUST populate the signing wallet in the Pending Actions form dialog using the same smart default logic (single auto-select, multi use default preference).
- **FR-012**: System MUST actually submit the action to the backend when the Pending Actions form dialog closes with a successful result.
- **FR-013**: System MUST disable the "Start" button and show guidance when the user has no linked wallets.
- **FR-014**: System MUST hide register sections that have no blueprints available to the user.
- **FR-015**: System MUST show a confirmation with the submission reference (workflow instance ID) after a successful new submission.

### Key Entities

- **Register (existing)**: A domain/authority context (e.g., "City of Edinburgh - Street Vendors"). Has a name, description, and metadata. Users discover services by browsing registers.
- **Blueprint (existing)**: A published workflow template within a register. Defines actions, participants, data schemas, and routing rules. The first action (Action 0) determines who can start the workflow.
- **Participant Role (existing)**: Defines who can execute an action. A special "public" role indicates anyone with a wallet can start. Future: will support verifiable credential requirements.
- **Workflow Instance (existing)**: A running instance of a blueprint, created when a user starts a submission. Tracks state, current actions, and transaction history.
- **Default Wallet Preference (new)**: A user-specific preference stored in browser local storage, recording which wallet address should be pre-selected for signing operations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can discover available services and start a new submission in under 3 clicks from the "New Submission" page (browse, start, submit).
- **SC-002**: The "New Submission" page loads and displays all accessible registers with their blueprints within 3 seconds under normal conditions.
- **SC-003**: A new submission (form fill, sign, submit) completes within 30 seconds for a typical form with 5-10 fields.
- **SC-004**: Users with a single wallet experience zero additional steps for wallet selection — it is fully automatic.
- **SC-005**: The Pending Actions submission flow completes end-to-end: action form, signed submission, backend confirmation, action removed from list.
- **SC-006**: All existing form rendering, schema validation, and signing capabilities are reused without duplication.
- **SC-007**: Unit tests cover the new submission flow: blueprint discovery, form rendering, wallet selection, instance creation, and action execution.
- **SC-008**: The page gracefully handles edge cases (no wallets, no registers, no blueprints, failed submissions) with clear user guidance.

## Assumptions

- The existing `GET /api/actions/{wallet}/{register}/blueprints` endpoint returns published blueprints for a wallet+register pair. Participant-level filtering may need enhancement but the endpoint structure exists.
- The existing form rendering pipeline (`SorchaFormRenderer`, `ControlDispatcher`, `FormSchemaService`, `FormSigningService`) is complete and production-ready from branch 032.
- Users have already linked at least one wallet via the Participant Identity system before attempting submissions.
- Register access is determined by the user's organisation membership (JWT-scoped).
- The `WalletSelectorDialog` component can be adapted from the Designer context to work inline in the submission flow.
- Instance creation (`POST /api/instances`) and action execution (`POST /api/instances/{id}/actions/{actionId}/execute`) endpoints are functional.

## Future Enhancements (Out of Scope)

- Blueprint category/tag metadata for filtering within a register
- Combined "create instance + execute first action" backend endpoint
- Server-side participant credential matching (verifiable credentials)
- Default wallet preference persisted server-side on participant profile
- Search across registers by service name/keyword
- Register-level metadata display (geographic area, authority type)
