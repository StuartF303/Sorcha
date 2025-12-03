# Feature Specification: Blueprint Designer

**Feature Branch**: `blueprint-designer`
**Created**: 2025-12-03
**Status**: Complete (85%)
**Input**: Derived from Sorcha.Blueprint.Designer.Client source code

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visual Blueprint Design (Priority: P0)

As a workflow designer, I need to visually create blueprints using a drag-and-drop interface so that I can define workflows without writing code.

**Why this priority**: Core value proposition - visual workflow design.

**Independent Test**: Can be tested by creating a blueprint with actions in the designer.

**Acceptance Scenarios**:

1. **Given** the designer page, **When** I click "Add Action", **Then** a new action node appears on the canvas.
2. **Given** action nodes, **When** I drag them, **Then** they move to the new position.
3. **Given** multiple actions, **When** I add a new action, **Then** it automatically links to the previous action.
4. **Given** an action node, **When** I click its properties button, **Then** the properties panel shows action details.

---

### User Story 2 - Blueprint Properties (Priority: P0)

As a workflow designer, I need to edit blueprint metadata so that I can describe my workflow's purpose and configuration.

**Why this priority**: Essential for blueprint identification and management.

**Independent Test**: Can be tested by editing blueprint title and description.

**Acceptance Scenarios**:

1. **Given** a blueprint, **When** I click "Properties", **Then** I can edit title and description.
2. **Given** the properties panel, **When** no node is selected, **Then** blueprint properties are shown.
3. **Given** edited properties, **When** I save, **Then** changes are applied to the blueprint.
4. **Given** a new blueprint, **Then** it has default title "New Blueprint" and version 1.

---

### User Story 3 - Action Properties (Priority: P0)

As a workflow designer, I need to configure action details so that I can define what each step does.

**Why this priority**: Actions are the building blocks of workflows.

**Independent Test**: Can be tested by selecting an action and editing its properties.

**Acceptance Scenarios**:

1. **Given** a selected action, **When** I view properties, **Then** I see title, description, participants, and conditions.
2. **Given** action properties, **When** I edit title, **Then** the node label updates on the canvas.
3. **Given** an action, **When** I view its summary, **Then** I see counts for Participants, Disclosures, Calculations, and DataSchemas.
4. **Given** action data schemas, **When** I add a schema, **Then** it appears in the action configuration.

---

### User Story 4 - Save and Load Blueprints (Priority: P0)

As a workflow designer, I need to save and load blueprints so that I can persist my work.

**Why this priority**: Core functionality - users need to save their work.

**Independent Test**: Can be tested by saving a blueprint and reloading it.

**Acceptance Scenarios**:

1. **Given** a blueprint, **When** I click "Save", **Then** it is stored in browser local storage.
2. **Given** saved blueprints, **When** I click "Load", **Then** I see a list of available blueprints.
3. **Given** the load dialog, **When** I select a blueprint, **Then** it loads into the designer.
4. **Given** a loaded blueprint, **Then** all actions and connections are restored.

---

### User Story 5 - JSON View (Priority: P1)

As a workflow designer, I need to view the blueprint as JSON so that I can see the raw data structure.

**Why this priority**: Useful for debugging and advanced users.

**Independent Test**: Can be tested by toggling the JSON view.

**Acceptance Scenarios**:

1. **Given** the designer, **When** I toggle JSON view, **Then** I see the blueprint JSON.
2. **Given** JSON view, **When** I toggle back, **Then** I see the diagram view.
3. **Given** JSON view, **Then** the JSON is formatted and readable.
4. **Given** blueprint changes, **When** I view JSON, **Then** changes are reflected.

---

### User Story 6 - Schema Library (Priority: P1)

As a workflow designer, I need to browse and select JSON schemas so that I can define data structures for actions.

**Why this priority**: Schemas define the data contracts for workflow steps.

**Independent Test**: Can be tested by navigating to Schema Library and browsing schemas.

**Acceptance Scenarios**:

1. **Given** the Schema Library page, **When** I load it, **Then** I see available schemas.
2. **Given** schemas, **When** I search, **Then** results are filtered by title and description.
3. **Given** a schema, **When** I click "View Details", **Then** I see the full schema definition.
4. **Given** a schema, **When** I click "Use Schema", **Then** it's added to my current action.

---

### User Story 7 - Administration Dashboard (Priority: P1)

As a system administrator, I need to view service health and configuration so that I can monitor the platform.

**Why this priority**: Operational visibility for administrators.

**Independent Test**: Can be tested by navigating to Administration page.

**Acceptance Scenarios**:

1. **Given** the Admin page, **When** I view Blueprint Service tab, **Then** I see service status.
2. **Given** the Admin page, **When** I view Peer Service tab, **Then** I see peer network status.
3. **Given** a service, **When** it's healthy, **Then** green status indicator is shown.
4. **Given** a service error, **When** viewing admin, **Then** error details are displayed.

---

### User Story 8 - Example Blueprints (Priority: P2)

As a new user, I need example blueprints so that I can learn how to design workflows.

**Why this priority**: Improves onboarding experience.

**Independent Test**: Can be tested by verifying examples exist on first load.

**Acceptance Scenarios**:

1. **Given** first launch (no saved blueprints), **When** app loads, **Then** example blueprints are created.
2. **Given** example "Loan Application Workflow", **Then** it has proper participants and actions.
3. **Given** example "Supply Chain Verification", **Then** it demonstrates multi-party workflows.
4. **Given** example "Document Approval Flow", **Then** it shows review/approval patterns.

---

### Edge Cases

- What happens when local storage is full?
- How does the designer handle very large blueprints?
- What happens when schema library is unavailable?
- How are browser compatibility issues handled?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide visual drag-and-drop blueprint design
- **FR-002**: System MUST allow action creation and linking
- **FR-003**: System MUST support blueprint property editing
- **FR-004**: System MUST support action property editing
- **FR-005**: System MUST save blueprints to browser local storage
- **FR-006**: System MUST load blueprints from local storage
- **FR-007**: System MUST provide JSON view of blueprints
- **FR-008**: System MUST integrate with Schema Library
- **FR-009**: System MUST provide administration dashboard
- **FR-010**: System SHOULD provide example blueprints for learning
- **FR-011**: System SHOULD support schema favorites
- **FR-012**: System COULD export blueprints as JSON files

### Key Entities

- **Blueprint**: Workflow definition with actions and participants
- **Action**: Single step in a workflow with conditions and data
- **Participant**: Entity involved in workflow execution
- **Schema**: JSON Schema defining data structure
- **Node**: Visual representation of an action on the canvas

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Blueprint creation takes less than 5 clicks
- **SC-002**: Action properties edit latency under 100ms
- **SC-003**: Blueprint save/load under 500ms
- **SC-004**: Schema library loads in under 2 seconds
- **SC-005**: UI responsive on mobile devices
- **SC-006**: Example blueprints demonstrate all major features
- **SC-007**: Admin dashboard shows real-time service status
- **SC-008**: Zero data loss on browser refresh (auto-save)
