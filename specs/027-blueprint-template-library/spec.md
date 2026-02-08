# Feature Specification: Blueprint Template Library & Ping-Pong Blueprint

**Feature Branch**: `027-blueprint-template-library`
**Created**: 2026-02-08
**Status**: Draft
**Input**: User description: "Establish a local blueprint template store and create the first working blueprint: a simple ping-pong workflow where a text message and an incrementing integer counter are passed back and forth between 2 participants."

## Clarifications

### Session 2026-02-08

- Q: How should the looping workflow be modeled? → A: Two actions in a cycle — the "Ping" action routes to the "Pong" action, and "Pong" routes back to "Ping", using existing route-based routing.
- Q: What are the canonical participant role names? → A: "Ping" and "Pong" — concise, domain-specific names matching the blueprint metaphor.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run the Ping-Pong Blueprint End-to-End (Priority: P1)

An administrator wants to demonstrate and validate the Sorcha workflow engine by running a simple two-participant ping-pong exchange. They select the built-in "Ping-Pong" blueprint template, create an instance with two named participants, and alternately submit actions carrying a text message and an incrementing counter. Each round-trip proves the validate-calculate-route-disclose pipeline works correctly.

**Why this priority**: This is the core deliverable — without a working blueprint that can be instantiated and executed, the template library has no value. The ping-pong is the simplest possible proof that the full action submission pipeline works end-to-end.

**Independent Test**: Can be fully tested by instantiating the Ping-Pong blueprint, submitting an action as participant "Ping", then submitting a response as participant "Pong". Delivers a verified working workflow loop.

**Acceptance Scenarios**:

1. **Given** the Ping-Pong blueprint is published, **When** an administrator creates a new instance and assigns two participants, **Then** the "Ping" participant is prompted to submit the first action.
2. **Given** the "Ping" participant submits an action with message "Hello" and counter 1, **When** the engine processes the submission, **Then** the action is routed to the "Pong" participant with the payload preserved.
3. **Given** the "Pong" participant receives the action, **When** they submit a response with message "World" and counter 2, **Then** the action is routed back to the "Ping" participant and the counter has incremented.
4. **Given** multiple round-trips have occurred, **When** reviewing the instance history, **Then** all submissions show correct counter values incrementing sequentially (1, 2, 3, 4, ...).

---

### User Story 2 - Browse and Select Templates from the Template Library (Priority: P2)

A user opens the template library in the application UI and sees a categorized list of available blueprint templates. They can browse by category, read descriptions, and preview the structure of each template before deciding to use one.

**Why this priority**: Users need a way to discover and understand available templates before they can instantiate them. This story provides the browsing and selection experience.

**Independent Test**: Can be tested by navigating to the template library page, verifying templates appear with correct details (name, description, category), and confirming the preview displays participant and action information.

**Acceptance Scenarios**:

1. **Given** the application has pre-installed templates, **When** a user navigates to the template library, **Then** they see a list of available templates with name, description, and category.
2. **Given** the template library is displayed, **When** a user selects the "Ping-Pong" template, **Then** they see a preview showing the two participants ("Ping" and "Pong"), the action flow, and the data schema (message + counter).
3. **Given** the user is viewing a template, **When** they click "Use Template" or equivalent, **Then** they are taken to the instance creation flow for that blueprint.

---

### User Story 3 - Ship Pre-Built Templates with the Installation (Priority: P3)

When the system starts for the first time (or when an administrator triggers a sync), the built-in blueprint templates are automatically loaded and published to the Blueprint Service. This ensures templates are available without manual setup.

**Why this priority**: Template seeding is infrastructure that enables the other stories. Without it, templates must be manually uploaded, which degrades the out-of-box experience.

**Independent Test**: Can be tested by starting the system fresh and verifying the Ping-Pong template (and any other shipped templates) appear in the template listing without any manual intervention.

**Acceptance Scenarios**:

1. **Given** the system is starting for the first time with no existing templates, **When** the startup process completes, **Then** all built-in templates are available in the template library.
2. **Given** built-in templates have already been loaded, **When** the system restarts, **Then** existing templates are not duplicated.
3. **Given** a new version ships with an updated template, **When** the system starts, **Then** the template is updated to the latest version while preserving any user-created templates.
4. **Given** an administrator wants to manually refresh templates, **When** they trigger a sync action, **Then** built-in templates are re-loaded from the local store.

---

### User Story 4 - Instantiate a Blueprint from a Template (Priority: P2)

A user selects a template and creates a new workflow instance by assigning real participants to the template's participant roles ("Ping" and "Pong"). The system creates a published blueprint from the template and starts the workflow.

**Why this priority**: This bridges the gap between browsing templates and actually running workflows. It's the conversion from template to live instance.

**Independent Test**: Can be tested by selecting the Ping-Pong template, assigning two participant identities to the "Ping" and "Pong" roles, and confirming the system creates a blueprint instance with the correct initial state.

**Acceptance Scenarios**:

1. **Given** a user has selected the Ping-Pong template, **When** they assign the "Ping" role to user "Alice" and the "Pong" role to user "Bob", **Then** a new blueprint instance is created with those participants.
2. **Given** a template requires two participants, **When** a user tries to create an instance with only one participant, **Then** the system rejects the request with a clear validation message.
3. **Given** an instance has been created, **When** the "Ping" participant checks their pending actions, **Then** they see the initial "Ping" action waiting for their submission.

---

### Edge Cases

- What happens when a participant submits an action with a counter value that doesn't match the expected next value? The system rejects the submission with a validation error indicating the expected counter.
- How does the system handle a participant submitting out of turn (e.g., "Ping" submits when it is "Pong"'s turn)? The routing engine directs actions to the correct participant; an out-of-turn submission is rejected because the action is not assigned to that participant.
- What happens if the template library is empty (no built-in templates available)? The UI shows an empty state with a message indicating no templates are available and suggesting administrator action.
- How does the system behave when a template references data schemas that fail validation? The template evaluation returns a validation error before any blueprint or instance is created.
- What happens when an instance of the ping-pong is abandoned mid-workflow? The instance remains in its current state indefinitely; no automatic timeout or cleanup occurs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST include at least one built-in blueprint template (Ping-Pong) that ships with the installation.
- **FR-002**: System MUST store built-in templates as local files that are loaded into the Blueprint Service on startup.
- **FR-003**: The Ping-Pong template MUST define exactly two participant roles named "Ping" and "Pong" that alternate submitting actions.
- **FR-004**: Each action in the Ping-Pong workflow MUST carry a text message (string, required) and a counter (integer, required, incrementing by 1 each turn).
- **FR-005**: The Ping-Pong workflow MUST use two actions in a cycle — the "Ping" action routes to the "Pong" action, and the "Pong" action routes back to the "Ping" action, looping indefinitely.
- **FR-006**: System MUST provide a user interface page where users can browse all available templates.
- **FR-007**: The template browsing page MUST display template name, description, category, and a preview of participant roles and action flow.
- **FR-008**: System MUST allow users to create a new workflow instance from a selected template by assigning participants to roles.
- **FR-009**: The counter value in the Ping-Pong workflow MUST be validated — submissions with incorrect counter values MUST be rejected.
- **FR-010**: System MUST automatically load built-in templates into the Blueprint Service when templates are not yet present (first startup or after data reset).
- **FR-011**: Template seeding MUST be idempotent — repeated startups MUST NOT create duplicate templates.
- **FR-012**: System MUST support the existing three example templates (approval workflow, loan application, supply chain) alongside the new Ping-Pong template.

### Key Entities

- **Blueprint Template**: A reusable workflow definition with parameterized roles and data schemas, stored as a local file and published to the Blueprint Service.
- **Ping-Pong Blueprint**: A specific template with two participants ("Ping" and "Pong"), two actions in a cycle ("Ping" action → "Pong" action → "Ping" action → ...), and a payload schema requiring message (string) and counter (integer).
- **Template Instance**: A running workflow created from a template, with real participants assigned to the template's abstract roles.
- **Action Payload**: The data submitted with each action — for Ping-Pong this contains the text message and the incrementing counter.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The Ping-Pong workflow completes at least 5 full round-trips (10 actions) without errors when tested end-to-end.
- **SC-002**: Users can browse available templates and create an instance in under 2 minutes from the template library page.
- **SC-003**: System starts with all built-in templates available within 30 seconds of first boot, with no manual intervention required.
- **SC-004**: Template seeding handles repeated restarts without data duplication — 5 consecutive restarts result in exactly the same template count.
- **SC-005**: All action submissions in the Ping-Pong workflow preserve payload integrity — the message and counter values stored in the register match what was submitted.

## Assumptions

- The existing Blueprint Template Service (with JSON-e evaluation, parameter schemas, and CRUD endpoints) provides the backend infrastructure for template storage and evaluation.
- The existing UI template components (TemplateList, TemplateEvaluator) can be extended or composed into the new template library page.
- The existing three example templates in `examples/templates/` will be included alongside the new Ping-Pong template.
- Participant assignment at instance creation uses existing participant identity or wallet-based identification.
- The Ping-Pong workflow does not require encryption of the message payload — it is a demonstration and testing blueprint.
- The looping workflow uses the existing route-based routing mechanism in the Blueprint Engine.

## Dependencies

- Blueprint Service template endpoints (already implemented — CRUD, evaluate, validate)
- Blueprint Engine pipeline: validate, calculate, route, disclose (already implemented)
- Register creation pipeline (verified working end-to-end in branch 026)
- UI template components: TemplateList, TemplateEvaluator (already exist in Sorcha.UI.Core)

## Scope Boundaries

### In Scope

- Ping-Pong blueprint template definition and validation
- Local template file store with startup seeding mechanism
- Template library browsing UI page
- Template-to-instance creation flow with participant assignment
- End-to-end action submission for the Ping-Pong workflow
- Including existing example templates in the library

### Out of Scope

- Template authoring or visual editing UI (templates are created via JSON files or API)
- Template marketplace or sharing between installations
- Complex workflow patterns beyond simple alternating actions (branching, parallel, conditional)
- Performance testing under high concurrent load
- Template versioning or migration strategy beyond simple overwrite-on-update
