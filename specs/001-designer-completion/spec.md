# Feature Specification: Blueprint Designer Completion

**Feature Branch**: `001-designer-completion`
**Created**: 2026-01-20
**Status**: Draft
**Input**: Complete the remaining Blueprint Designer features (BD-022 to BD-025): Participant Editor, Condition Editor, Export/Import, and Backend Integration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Blueprint Participants (Priority: P1)

A blueprint designer needs to define who participates in a workflow. They open the participant editor, add participants by selecting wallet addresses or entering identifiers, assign roles to each participant, and configure participant metadata. The blueprint now has a complete participant list that can be referenced in action routing.

**Why this priority**: Participants are fundamental to workflow execution. Without defined participants, actions cannot be routed and the blueprint cannot be executed. This completes a critical gap in the designer.

**Independent Test**: Can be fully tested by opening the designer, adding participants with wallet addresses and roles, and verifying they appear in the blueprint JSON structure correctly.

**Acceptance Scenarios**:

1. **Given** a blueprint is open in the designer, **When** the user clicks "Add Participant", **Then** they see a dialog to configure a new participant.
2. **Given** the participant dialog is open, **When** the user selects a wallet address from their available wallets or enters an external address, **Then** the address is validated and associated with the participant.
3. **Given** a participant is being configured, **When** the user assigns a role (e.g., Initiator, Approver, Observer), **Then** the role is stored with the participant definition.
4. **Given** participants have been added, **When** the user views the participant list, **Then** they see all participants with their addresses and roles, and can edit or remove them.

---

### User Story 2 - Build Routing Conditions Visually (Priority: P2)

A blueprint designer needs to define conditional routing logic without writing raw JSON. They open the condition editor for an action, use a visual builder to construct rules (e.g., "if loan amount > 50000 then route to Senior Approver"), and the system generates valid condition expressions.

**Why this priority**: Routing conditions determine workflow flow. A visual builder makes the designer accessible to non-technical users and reduces errors from manual JSON editing.

**Independent Test**: Can be tested by opening the condition editor, building a multi-clause condition using the visual interface, and verifying the generated expression works correctly.

**Acceptance Scenarios**:

1. **Given** an action is selected in the designer, **When** the user clicks "Edit Routing Condition", **Then** they see a visual condition builder interface.
2. **Given** the condition builder is open, **When** the user selects a field, operator (equals, greater than, contains, etc.), and value, **Then** a condition clause is created and displayed visually.
3. **Given** one condition clause exists, **When** the user adds another clause with AND/OR logic, **Then** the clauses are combined with the selected logical operator.
4. **Given** a complex condition has been built, **When** the user saves, **Then** the condition is converted to valid expression format and stored with the action.
5. **Given** an action already has a condition, **When** the user opens the condition editor, **Then** the existing condition is parsed and displayed in the visual builder for editing.

---

### User Story 3 - Export and Import Blueprints (Priority: P3)

A blueprint designer wants to share a blueprint with a colleague or back up their work. They export the blueprint as a file (JSON or YAML format), send it to others, and recipients can import the file into their own designer to view or modify the blueprint.

**Why this priority**: Blueprint portability enables collaboration, backup, and template sharing. This is essential for team workflows and disaster recovery.

**Independent Test**: Can be tested by exporting a blueprint to file, closing the designer, importing the file, and verifying the blueprint is restored correctly.

**Acceptance Scenarios**:

1. **Given** a blueprint is open in the designer, **When** the user clicks "Export", **Then** they see options to download as JSON or YAML format.
2. **Given** the user selects JSON export, **When** the export completes, **Then** a valid JSON file is downloaded containing the complete blueprint definition.
3. **Given** the user selects YAML export, **When** the export completes, **Then** a valid YAML file is downloaded containing the complete blueprint definition.
4. **Given** the user has a blueprint file, **When** they click "Import" and select the file, **Then** the blueprint is loaded into the designer ready for editing.
5. **Given** an imported blueprint has validation errors, **When** the import is attempted, **Then** the user sees specific error messages explaining what needs to be fixed.

---

### User Story 4 - Save Blueprints to Server (Priority: P4)

A blueprint designer wants their blueprints stored securely on the server rather than only in browser storage. They save a blueprint, and it is persisted to the Blueprint Service. When they return later or use a different device, their blueprints are available from the server.

**Why this priority**: Server-side persistence provides durability, enables collaboration, and allows blueprints to be accessed across devices and sessions. This replaces the temporary LocalStorage approach.

**Independent Test**: Can be tested by saving a blueprint, clearing browser data, logging back in, and verifying the blueprint is retrieved from the server.

**Acceptance Scenarios**:

1. **Given** a blueprint is open in the designer, **When** the user clicks "Save", **Then** the blueprint is persisted to the server and a success confirmation is shown.
2. **Given** the user opens the blueprint library, **When** the page loads, **Then** they see blueprints fetched from the server (not just LocalStorage).
3. **Given** a blueprint exists on the server, **When** the user edits and saves it, **Then** the server version is updated with the changes.
4. **Given** the user creates a new blueprint, **When** they save for the first time, **Then** the blueprint is created on the server with a unique identifier.
5. **Given** the network is unavailable, **When** the user tries to save, **Then** the save is queued locally and synced when connectivity is restored.
6. **Given** a blueprint was saved while offline, **When** connectivity is restored, **Then** the queued changes are automatically synced to the server.

---

### User Story 5 - Build Calculated Field Expressions (Priority: P5)

A blueprint designer needs to define calculated values that are computed from other fields. They open the calculation editor, use the visual builder to construct expressions (e.g., "monthly_payment = loan_amount * (rate / 12)"), and the system generates valid calculation rules.

**Why this priority**: Calculated fields enhance form functionality by automating computations. A visual builder makes this accessible without requiring knowledge of the expression syntax.

**Independent Test**: Can be tested by defining a calculated field using the visual builder and verifying the calculation works correctly when the form is rendered.

**Acceptance Scenarios**:

1. **Given** a form field is selected, **When** the user chooses to make it a calculated field, **Then** they see a calculation expression builder.
2. **Given** the expression builder is open, **When** the user selects source fields and mathematical operators, **Then** an expression is built showing the formula visually.
3. **Given** an expression is built, **When** the user clicks "Test", **Then** they can enter sample values and see the calculated result.
4. **Given** the expression is complete, **When** the user saves, **Then** the calculation rule is stored with the field definition.

---

### Edge Cases

- What happens when importing a blueprint with participants that don't exist in the current system?
- How does the system handle circular routing conditions?
- What happens when export/import involves schema references that aren't available?
- How does offline sync handle conflicting changes made on multiple devices?
- What happens when a calculated expression references a field that is deleted?
- How does the condition builder handle deeply nested AND/OR logic?

## Requirements *(mandatory)*

### Functional Requirements

**Participant Editor (BD-022):**
- **FR-001**: System MUST provide a dialog to add, edit, and remove blueprint participants.
- **FR-002**: System MUST allow selection of wallet addresses from user's available wallets.
- **FR-003**: System MUST allow entry of external wallet addresses with validation.
- **FR-004**: System MUST allow assignment of participant roles (Initiator, Approver, Observer, etc.).
- **FR-005**: System MUST display the participant list with address, role, and metadata.

**Condition Editor (BD-023):**
- **FR-006**: System MUST provide a visual builder for routing conditions.
- **FR-007**: System MUST support condition operators: equals, not equals, greater than, less than, contains, starts with, ends with.
- **FR-008**: System MUST support combining conditions with AND/OR logic.
- **FR-009**: System MUST support nested condition groups.
- **FR-010**: System MUST parse existing conditions into the visual builder for editing.
- **FR-011**: System MUST generate valid expression format from visual builder state.

**Export/Import (BD-024):**
- **FR-012**: System MUST export blueprints as valid JSON files.
- **FR-013**: System MUST export blueprints as valid YAML files.
- **FR-014**: System MUST import blueprints from JSON files.
- **FR-015**: System MUST import blueprints from YAML files.
- **FR-016**: System MUST validate imported blueprints and display specific errors.
- **FR-017**: System MUST preserve all blueprint properties during export/import round-trip.

**Backend Integration (BD-025):**
- **FR-018**: System MUST save blueprints to the Blueprint Service for persistence.
- **FR-019**: System MUST retrieve blueprints from the Blueprint Service on load.
- **FR-020**: System MUST support creating new blueprints on the server.
- **FR-021**: System MUST support updating existing blueprints on the server.
- **FR-022**: System MUST queue saves when offline and sync when connectivity restores.
- **FR-023**: System MUST handle sync conflicts with user notification and resolution options.
- **FR-024**: System MUST maintain backward compatibility with LocalStorage blueprints during migration.

### Key Entities

- **Participant**: A workflow participant with wallet address, role, display name, and optional metadata.
- **Routing Condition**: A logical expression that determines which participant receives the next action, composed of field comparisons and logical operators.
- **Calculation Expression**: A mathematical formula that computes a field value from other field values.
- **Blueprint File**: An exportable representation of a blueprint in JSON or YAML format.
- **Sync Queue**: A local queue of pending save operations for offline support.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can add a participant with wallet address and role in under 1 minute.
- **SC-002**: Users can build a 3-clause routing condition using the visual builder in under 2 minutes.
- **SC-003**: Blueprint export completes in under 3 seconds for blueprints with up to 50 actions.
- **SC-004**: Blueprint import validates and loads in under 5 seconds for files up to 1MB.
- **SC-005**: Server save operations complete in under 2 seconds under normal network conditions.
- **SC-006**: 95% of offline saves sync successfully within 30 seconds of connectivity restoration.
- **SC-007**: 90% of users can build a valid routing condition without documentation on first attempt.
- **SC-008**: Export/import round-trip preserves 100% of blueprint data with no loss or corruption.

## Assumptions

- The Blueprint Service backend APIs for CRUD operations are available and documented.
- User wallets are accessible through existing wallet service integration.
- The existing designer infrastructure (diagram canvas, properties panel) is stable.
- Browser supports File System Access API or fallback download/upload mechanisms.
- JSON and YAML libraries for serialization are available.

## Dependencies

- Blueprint Service: Provides persistence APIs for blueprints.
- Wallet Service: Provides access to user wallet addresses.
- Existing Designer: Diagram canvas, properties panel, and action management.
- LocalStorage: Migration from current storage during transition period.

## Out of Scope

- Real-time collaborative editing of blueprints.
- Version history and rollback for blueprints.
- Blueprint marketplace or public sharing.
- Advanced expression functions beyond basic arithmetic and comparisons.
- Visual workflow simulation/preview.
