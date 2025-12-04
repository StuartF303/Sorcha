# Feature Specification: Blueprint Service

**Feature Branch**: `blueprint-service`
**Created**: 2025-12-03
**Status**: 95% Complete (Unified Blueprint-Action Service)
**Input**: Derived from `docs/blueprint-architecture.md` and existing codebase analysis

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Manage Blueprints (Priority: P1)

As a workflow designer, I need to create, update, and manage multi-party workflow blueprints so that I can define declarative business processes.

**Why this priority**: Core functionality - without blueprints, no workflows can be executed on the platform.

**Independent Test**: Can be fully tested by creating a blueprint via API and retrieving it, delivering the foundation for workflow automation.

**Acceptance Scenarios**:

1. **Given** a valid blueprint definition with participants and actions, **When** I POST to `/api/blueprints`, **Then** the blueprint is created with a unique ID and returned with 201 Created.
2. **Given** an existing blueprint ID, **When** I GET `/api/blueprints/{id}`, **Then** the complete blueprint definition is returned including all participants, actions, schemas, and disclosures.
3. **Given** a blueprint with validation errors (missing required fields), **When** I POST to `/api/blueprints`, **Then** a 400 Bad Request is returned with detailed validation errors.
4. **Given** an existing blueprint, **When** I PUT to `/api/blueprints/{id}` with updated fields, **Then** the blueprint version is incremented and the updated blueprint is returned.

---

### User Story 2 - Execute Blueprint Actions (Priority: P1)

As a workflow participant, I need to execute actions within a blueprint instance so that I can progress the workflow and submit data.

**Why this priority**: Core functionality - blueprints must be executable to provide value.

**Independent Test**: Can be tested by instantiating a blueprint and executing its first action.

**Acceptance Scenarios**:

1. **Given** a valid blueprint, **When** I POST to `/api/blueprints/{id}/instances`, **Then** a new instance is created with state tracking initialized.
2. **Given** an active blueprint instance and I am the designated sender, **When** I POST action data to `/api/instances/{id}/actions/{actionId}`, **Then** the action is executed, data is validated against schemas, and the next participant is determined via JSON Logic routing.
3. **Given** action data that fails schema validation, **When** I submit the action, **Then** detailed validation errors are returned indicating which fields failed.
4. **Given** I am not the designated sender for the current action, **When** I attempt to execute it, **Then** a 403 Forbidden is returned.

---

### User Story 3 - JSON Schema Validation (Priority: P2)

As a workflow designer, I need to define and validate data schemas for each action so that data quality is enforced throughout the workflow.

**Why this priority**: Data integrity is essential but depends on basic blueprint functionality.

**Independent Test**: Can be tested by creating a blueprint with JSON Schema definitions and validating input data.

**Acceptance Scenarios**:

1. **Given** an action with JSON Schema definitions, **When** action data is submitted, **Then** the data is validated against the schema before processing.
2. **Given** a schema with required fields, format constraints, and enums, **When** invalid data is submitted, **Then** specific validation errors are returned for each violation.
3. **Given** external schema references (`$ref`), **When** the blueprint is loaded, **Then** schemas are resolved and cached for performance.

---

### User Story 4 - JSON Logic Routing and Calculations (Priority: P2)

As a workflow designer, I need to configure conditional routing and calculated fields using JSON Logic so that workflows can adapt dynamically based on data.

**Why this priority**: Enables dynamic workflows but requires basic action execution to be working.

**Independent Test**: Can be tested by creating a blueprint with JSON Logic conditions and submitting data that triggers different routing paths.

**Acceptance Scenarios**:

1. **Given** an action with a JSON Logic routing condition, **When** action data is submitted, **Then** the condition is evaluated and the next participant is determined accordingly.
2. **Given** an action with calculations, **When** action data is submitted, **Then** calculated fields are computed and included in the action result.
3. **Given** a complex routing condition with nested if/else, **When** various test data is submitted, **Then** the correct routing path is selected for each scenario.

---

### User Story 5 - Disclosure Management (Priority: P2)

As a workflow participant, I need fine-grained control over which data fields are visible to each participant so that sensitive information is protected.

**Why this priority**: Privacy control is important for multi-party workflows but depends on core execution.

**Independent Test**: Can be tested by creating a blueprint with disclosures and verifying data visibility per participant.

**Acceptance Scenarios**:

1. **Given** an action with disclosure rules, **When** a participant retrieves workflow data, **Then** only fields specified in their disclosures are visible.
2. **Given** JSON Pointer-based disclosures (`/field`, `/*`, `/nested/path`), **When** data is retrieved, **Then** the correct fields are filtered based on pointer paths.

---

### User Story 6 - Form Generation (Priority: P3)

As a UI developer, I need form definitions generated from action schemas so that dynamic forms can be rendered for data entry.

**Why this priority**: UI generation is a convenience feature that enhances usability.

**Independent Test**: Can be tested by creating an action with form definitions and retrieving the form schema.

**Acceptance Scenarios**:

1. **Given** an action with form definitions (Control types, layouts), **When** I GET the action form schema, **Then** a complete form definition is returned with field types, labels, and layout information.
2. **Given** form controls with conditional display rules (JSON Logic), **When** the form is rendered with data, **Then** controls are shown/hidden based on the conditions.

---

### Edge Cases

- What happens when a blueprint has circular routing conditions?
- How does the system handle missing Participants in routing?
- What happens when schema validation timeout occurs on large payloads?
- How does concurrent action execution on the same instance behave?

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" throughout this specification.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow creation of blueprints with unique IDs, titles, and descriptions
- **FR-002**: System MUST support at least 2 participants per blueprint
- **FR-003**: System MUST support at least 1 action per blueprint
- **FR-004**: System MUST validate action data against JSON Schema definitions
- **FR-005**: System MUST evaluate JSON Logic routing conditions to determine next participant
- **FR-006**: System MUST compute calculated fields using JSON Logic expressions
- **FR-007**: System MUST enforce disclosure rules when returning data to participants
- **FR-008**: System MUST support workflow instance lifecycle (create, progress, complete)
- **FR-009**: System MUST track previous transaction IDs for blockchain chaining
- **FR-010**: System MUST provide form definitions for UI rendering
- **FR-011**: System MUST support blueprint versioning
- **FR-012**: System MUST provide OpenAPI documentation for all endpoints
- **FR-013**: System MUST support JSON-LD context for semantic interoperability (optional)
- **FR-014**: System MUST support JSON-e template evaluation for blueprint generation (optional)

### Key Entities

- **Blueprint**: Workflow definition with participants, actions, and schemas
- **Participant**: Party involved in workflow with identity (DID/wallet) and organization
- **Action**: Workflow step with sender, data schemas, disclosures, routing conditions, and form
- **Disclosure**: Data access rule mapping participant to JSON Pointer paths
- **Instance**: Runtime execution of a blueprint with state tracking
- **Control**: UI form element with type, binding, and display conditions

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create blueprints in under 5 seconds via API
- **SC-002**: Action data validation completes in under 100ms for typical payloads
- **SC-003**: JSON Logic routing evaluation completes in under 50ms
- **SC-004**: Schema validation achieves 100% compliance with JSON Schema Draft 7
- **SC-005**: API achieves <200ms P95 latency for standard CRUD operations
- **SC-006**: Test coverage exceeds 85% for core blueprint logic
- **SC-007**: All public APIs documented with OpenAPI and Scalar UI
