# Feature Specification: Blueprint Visual Designer

**Feature Branch**: `029-blueprint-visual-designer`
**Created**: 2026-02-09
**Status**: Draft
**Input**: User description: "Upgrade the UI visual designer control to display a readonly graphical representation of a blueprint JSON as loaded from a template or the blueprint store. Also review and ensure blueprint deployment works end-to-end."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Blueprint as Visual Diagram (Priority: P1)

A user browsing the blueprint library or template catalogue wants to see a graphical flow diagram of a blueprint without reading raw JSON. When they select a blueprint or template, the system renders participants as labelled nodes, actions as step cards, and routes as directed arrows between actions. The diagram shows the complete workflow at a glance: who does what, in what order, and under what conditions.

**Why this priority**: This is the core feature - without visual rendering, users cannot understand blueprint structure without reading JSON. It delivers immediate value to every user who interacts with blueprints.

**Independent Test**: Can be fully tested by loading the built-in ping-pong template and verifying that 2 participant indicators, 2 action nodes, and 2 route arrows are rendered in the correct flow order. Delivers instant understanding of workflow structure.

**Acceptance Scenarios**:

1. **Given** a valid blueprint JSON with 2 participants and 2 actions, **When** the user opens the visual viewer, **Then** the diagram renders 2 participant indicators, 2 action nodes with titles and sender labels, and directed arrows showing route flow between actions.
2. **Given** a blueprint with conditional routing (e.g. approval workflow with senior-officer branch), **When** the user views the diagram, **Then** branching routes display with condition labels and the default route is visually distinguished.
3. **Given** a blueprint with a cycle (ping-pong loop), **When** the user views the diagram, **Then** the cycle is rendered as a back-edge arrow with a visual indicator so the user understands the workflow repeats.
4. **Given** the visual viewer is displaying a blueprint, **When** the user interacts with it, **Then** the diagram is readonly - no nodes can be moved, added, or deleted.

---

### User Story 2 - Browse Templates with Visual Preview (Priority: P2)

A user on the Templates page wants to quickly preview what a template's workflow looks like before deciding to use it. When they select a template from the list, a visual diagram appears alongside the template details, giving them an immediate understanding of the workflow structure, participants, and action flow.

**Why this priority**: Builds on US1 to integrate the viewer into the existing template browsing experience. Templates are the primary entry point for new users discovering blueprints.

**Independent Test**: Can be tested by navigating to the Templates page, selecting the ping-pong template, and verifying the visual preview renders alongside template metadata. The user can understand the workflow without reading the JSON.

**Acceptance Scenarios**:

1. **Given** the user is on the Templates page, **When** they select a template from the list, **Then** a visual diagram of the template's blueprint appears in the detail panel.
2. **Given** a parameterised template (e.g. approval workflow), **When** the user selects it, **Then** the diagram shows the default/example configuration with participant placeholders clearly labelled.
3. **Given** the user is viewing a template preview, **When** they click "Use Template", **Then** the existing template evaluation flow opens as before (no regression).

---

### User Story 3 - View Published Blueprints Visually (Priority: P3)

A user on the Blueprints page wants to view the visual structure of a saved or published blueprint. When they click a "View" action on a blueprint card, the system shows the readonly visual diagram so they can review its structure before publishing or sharing.

**Why this priority**: Extends the viewer to the blueprint library, completing coverage across all blueprint access points. Less critical than templates since users who save blueprints may already know the structure.

**Independent Test**: Can be tested by navigating to the Blueprints page, selecting a saved blueprint, and verifying the visual diagram opens in a readonly view with correct participants, actions, and routes.

**Acceptance Scenarios**:

1. **Given** the user is on the Blueprints page with saved blueprints, **When** they click a "View" action on a blueprint card, **Then** a dialog or panel displays the readonly visual diagram.
2. **Given** a published blueprint with multiple versions, **When** the user views a specific version, **Then** the diagram reflects that version's structure.

---

### User Story 4 - Verify Blueprint Deployment Pipeline (Priority: P2)

An administrator wants to confirm that the end-to-end blueprint deployment pipeline works: loading a template, publishing it to the Blueprint Service, creating a workflow instance, and confirming the instance is ready for execution. This ensures the platform is operational for workflow participants.

**Why this priority**: Equal to US2 because visual design is meaningless if blueprints cannot be deployed. This validates the underlying infrastructure that makes blueprints useful.

**Independent Test**: Can be tested by executing the deployment pipeline via the UI or API: load template, publish, create instance, verify instance state is "Active" with correct initial action.

**Acceptance Scenarios**:

1. **Given** a template exists in the template library, **When** the administrator evaluates the template and publishes the resulting blueprint, **Then** the blueprint appears in the library with "published" status and no validation errors.
2. **Given** a published blueprint, **When** the administrator creates a new workflow instance with participant wallet addresses, **Then** the instance is created with state "Active" and the starting action is identified.
3. **Given** published blueprint templates are seeded on startup, **When** the services start fresh, **Then** the built-in templates are available for browsing and deployment without manual intervention.
4. **Given** a published blueprint with cycle warnings (e.g. ping-pong), **When** the system publishes it, **Then** publication succeeds with warnings displayed to the user (cycles are informational, not blocking).

---

### Edge Cases

- What happens when a blueprint has no routes defined on actions (legacy condition-based routing only)? The viewer should still render action flow based on participant conditions.
- How does the viewer handle a blueprint with 20+ actions? The diagram should be scrollable and zoomable without performance degradation.
- What happens when a template evaluation fails (invalid parameters)? The system should show a clear error message without crashing the visual preview.
- How does the viewer handle a blueprint with parallel branches (multiple nextActionIds on a route)? Branches should fan out visually with clear separation.
- What happens when a blueprint references a sender participant that does not exist in the participant list? The viewer should display a warning indicator on the affected actions.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST render a readonly visual diagram from any valid blueprint JSON, showing participants, actions, and route flow.
- **FR-002**: System MUST display actions as labelled nodes showing action title, action ID, and sender participant.
- **FR-003**: System MUST display routes between actions as directed arrows, with condition labels where applicable.
- **FR-004**: System MUST visually distinguish starting actions (workflow entry points) from intermediate and terminal actions (workflow endpoints).
- **FR-005**: System MUST render cyclic routes (back-edges) with a visual loop indicator so users understand the workflow repeats.
- **FR-006**: System MUST support branching routes (conditional and parallel) with visually separated paths.
- **FR-007**: System MUST display participant information associated with each action (who performs the step).
- **FR-008**: System MUST integrate the visual viewer into the Templates page, showing a diagram when a template is selected.
- **FR-009**: System MUST integrate the visual viewer into the Blueprints page, allowing users to view saved/published blueprints as diagrams.
- **FR-010**: System MUST prevent all editing interactions in the readonly viewer - no node creation, deletion, movement, or property changes.
- **FR-011**: System MUST support zoom and scroll for large blueprints with many actions.
- **FR-012**: System MUST allow users to click an action node to see additional details (data schemas, disclosures, calculations) in a read-only panel or tooltip.
- **FR-013**: System MUST support the end-to-end deployment pipeline: template evaluation, blueprint publishing (with cycle warning handling), and instance creation.
- **FR-014**: System MUST display publish validation results (errors and warnings) clearly to the user before completing publication.
- **FR-015**: System MUST auto-layout the diagram nodes in a readable arrangement (top-to-bottom or left-to-right flow) without manual positioning.

### Key Entities

- **Blueprint**: The workflow definition containing participants, actions, routes, schemas, and disclosures. Rendered as the complete diagram.
- **Participant**: A named role in the workflow with an optional wallet address. Displayed as a label or lane associated with actions.
- **Action**: A discrete step in the workflow with sender, data schemas, routes, and disclosures. Rendered as a node in the diagram.
- **Route**: A directed transition between actions, optionally conditional. Rendered as an arrow/edge in the diagram.
- **WorkflowInstance**: A runtime execution of a published blueprint with bound participants and active state tracking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can understand a blueprint's participant flow and action sequence within 10 seconds of viewing the diagram, without reading any JSON.
- **SC-002**: The visual diagram renders correctly for all 4 built-in template types (ping-pong, approval, loan, supply-chain) with accurate participant, action, and route representation.
- **SC-003**: 100% of blueprint structures supported by the engine (linear, branching, parallel, cyclic) render correctly in the viewer.
- **SC-004**: The end-to-end deployment pipeline (template load, publish, create instance) completes successfully for at least 2 different template types.
- **SC-005**: The readonly viewer prevents all modification attempts - zero editing operations are possible through the viewer interface.
- **SC-006**: Diagrams with up to 20 actions and 10 participants render within 2 seconds on standard hardware.
- **SC-007**: Users can access visual blueprint previews from both the Templates page and the Blueprints page within 1 click from the list view.

## Assumptions

- The existing diagram library already integrated in the Designer page can be reused or adapted for readonly rendering - no new third-party diagram library is needed.
- The existing action node and participant node visual components can be extended or wrapped for readonly use without breaking the editable designer.
- Blueprint JSON loaded from templates and the store follows the same data model, so a single viewer component works for both sources.
- Auto-layout will use a simple topological ordering algorithm (top-to-bottom for sequential, fan-out for branches) rather than a sophisticated graph layout library.
- The 4 built-in templates (ping-pong, approval, loan, supply-chain) serve as the primary test cases for visual correctness.
- Blueprint deployment verification is done through the existing service endpoints - no new backend endpoints are needed for this feature.

## Dependencies

- Existing diagram rendering library (already integrated in Designer page)
- Existing component library for UI elements (already integrated)
- Blueprint Service endpoints for publishing, templates, and instances (already implemented)
- Template seeding service (already seeding 4 built-in templates on startup)

## Out of Scope

- Editable visual designer improvements (the existing Designer page handles editing)
- Runtime instance execution visualisation (showing live action progress)
- New backend service endpoints
- Mobile-specific layout optimisations
- Print/export of visual diagrams
- Animated transitions or real-time updates
