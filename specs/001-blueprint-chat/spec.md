# Feature Specification: AI-Assisted Blueprint Design Chat

**Feature Branch**: `001-blueprint-chat`
**Created**: 2026-02-01
**Status**: Draft
**Input**: User description: "AI-assisted blueprint design chat service that integrates with Sorcha.UI to allow users to design workflows through natural language conversation with an AI assistant. The AI can create blueprints, add participants, define actions with data schemas, configure routing logic, and validate designs in real-time. Uses SignalR for streaming responses and live blueprint preview updates."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Design Blueprint Through Conversation (Priority: P1)

A workflow designer opens the blueprint design assistant and describes their business process in plain language. The AI assistant asks clarifying questions, then progressively builds the blueprint structure - adding participants, actions, data fields, and routing logic. The designer sees the blueprint update in real-time as the AI works.

**Why this priority**: This is the core value proposition - enabling non-technical users to create complex workflow blueprints through natural conversation rather than manual configuration.

**Independent Test**: Can be tested by opening the chat interface, describing a simple two-party approval workflow, and verifying the resulting blueprint contains the correct participants, actions, and routing.

**Acceptance Scenarios**:

1. **Given** a designer is on the blueprint design page, **When** they describe "I need a document approval workflow where Alice submits documents and Bob approves them", **Then** the AI creates a blueprint with two participants (Alice, Bob) and appropriate actions (Submit, Approve).

2. **Given** an active chat session with a partial blueprint, **When** the designer says "Add a rejection option for Bob", **Then** the AI adds conditional routing logic allowing Bob to approve or reject.

3. **Given** a chat session, **When** the AI makes changes to the blueprint, **Then** the blueprint preview panel updates in real-time showing the current state.

---

### User Story 2 - Real-Time Blueprint Validation (Priority: P1)

As the AI builds the blueprint, the system continuously validates the structure and displays any issues. The designer can ask the AI to explain validation errors and fix them through conversation.

**Why this priority**: Validation feedback is essential to ensure the resulting blueprint is valid and executable - without it, users may create unusable workflows.

**Independent Test**: Can be tested by intentionally creating an invalid blueprint (e.g., action with missing sender) and verifying the validation panel shows the error with a clear explanation.

**Acceptance Scenarios**:

1. **Given** a blueprint under construction, **When** it lacks required elements (e.g., fewer than 2 participants), **Then** the validation panel displays specific error messages.

2. **Given** validation errors are displayed, **When** the designer asks "Why is this invalid?", **Then** the AI explains the issue and suggests how to fix it.

3. **Given** validation errors exist, **When** the designer says "Fix the validation errors", **Then** the AI modifies the blueprint to resolve the issues and the validation panel updates.

---

### User Story 3 - Define Data Schemas Through Conversation (Priority: P2)

A designer describes what information should be collected at each workflow step. The AI translates these requirements into proper data schemas with appropriate field types, validation rules, and constraints.

**Why this priority**: Data schemas are essential for functional workflows but are technically complex - AI assistance significantly reduces the learning curve.

**Independent Test**: Can be tested by requesting "the application form should collect name, email, and a loan amount between 1000 and 50000" and verifying the generated schema has correct field types and constraints.

**Acceptance Scenarios**:

1. **Given** an action is being defined, **When** the designer says "collect the applicant's name, email address, and phone number", **Then** the AI creates a data schema with string fields and appropriate formats (email validation).

2. **Given** data requirements are described, **When** the designer specifies "loan amount must be between 1000 and 100000", **Then** the schema includes numeric constraints (minimum/maximum values).

3. **Given** a data schema exists, **When** the designer says "make the phone number optional", **Then** the AI updates the schema to remove the required constraint for that field.

---

### User Story 4 - Configure Disclosure Rules (Priority: P2)

A designer specifies which participants should see which data fields at each step. The AI configures the appropriate disclosure rules to enforce data privacy within the workflow.

**Why this priority**: Disclosures are a core Sorcha differentiator for privacy-preserving workflows - making them easy to configure is important for adoption.

**Independent Test**: Can be tested by specifying "only the manager should see the salary field" and verifying the disclosure rules restrict that field to the specified participant.

**Acceptance Scenarios**:

1. **Given** a multi-participant blueprint, **When** the designer says "the reviewer should only see the application summary, not the financial details", **Then** the AI configures disclosures to expose only specified fields to the reviewer.

2. **Given** existing disclosures, **When** the designer says "actually, let the reviewer also see the credit score", **Then** the AI adds the field to the reviewer's disclosure list.

---

### User Story 5 - Edit Existing Blueprints (Priority: P3)

A designer loads an existing blueprint into the chat interface to modify it through conversation. The AI understands the current structure and can make targeted changes.

**Why this priority**: Editing existing blueprints is a common need but can be done through manual editing initially - conversational editing is an enhancement.

**Independent Test**: Can be tested by loading a saved blueprint, asking to "add a new approval step after the initial review", and verifying the modification is correctly applied.

**Acceptance Scenarios**:

1. **Given** an existing blueprint ID, **When** the designer starts a session with "edit blueprint [ID]", **Then** the current blueprint loads into the preview and the AI acknowledges its structure.

2. **Given** a loaded blueprint, **When** the designer says "add a legal review step between submission and approval", **Then** the AI inserts a new action with appropriate routing adjustments.

---

### User Story 6 - Export and Save Blueprints (Priority: P3)

After designing a blueprint through conversation, the designer can save it to the system or export it as JSON/YAML for external use.

**Why this priority**: Essential for completing the workflow but relies on existing blueprint storage infrastructure.

**Independent Test**: Can be tested by completing a blueprint design and clicking save, then verifying it appears in the blueprint list.

**Acceptance Scenarios**:

1. **Given** a valid blueprint in the chat session, **When** the designer says "save this blueprint", **Then** the blueprint is persisted and the designer receives confirmation with the blueprint ID.

2. **Given** a completed blueprint, **When** the designer clicks the export button, **Then** they can download the blueprint as JSON or YAML.

---

### Edge Cases

- What happens when the AI service is unavailable? System should display a clear error message and allow the user to retry or fall back to manual editing.
- How does the system handle conflicting instructions (e.g., "remove all participants" when minimum 2 are required)? AI should explain the constraint and refuse the invalid operation.
- What happens if the user's session times out mid-design? The in-progress blueprint should be recoverable from session storage.
- How does the system handle very long conversations? Chat history should be scrollable with the ability to see the full conversation.
- What happens when the user describes an ambiguous requirement? AI should ask clarifying questions rather than making assumptions that may be incorrect.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a chat interface where users can describe workflow requirements in natural language
- **FR-002**: System MUST display AI responses as they are generated (streaming) rather than waiting for complete responses
- **FR-003**: System MUST show a real-time preview of the blueprint being constructed, updating as the AI makes changes
- **FR-004**: System MUST validate the blueprint continuously and display validation errors in a dedicated panel
- **FR-005**: AI MUST be able to create new blueprints with title, description, and metadata
- **FR-006**: AI MUST be able to add, modify, and remove participants from the blueprint
- **FR-007**: AI MUST be able to add, modify, and remove actions including data schemas, disclosures, and routing
- **FR-008**: AI MUST be able to configure conditional routing logic based on data values
- **FR-009**: AI MUST be able to define data schemas with appropriate field types, constraints, and validation rules
- **FR-010**: AI MUST be able to configure disclosure rules specifying which participants can see which data fields
- **FR-011**: System MUST allow users to save completed blueprints to the blueprint storage
- **FR-012**: System MUST allow users to export blueprints as JSON or YAML files
- **FR-013**: System MUST allow users to load existing blueprints for editing through conversation
- **FR-014**: System MUST maintain conversation history within a session for context continuity
- **FR-015**: System MUST require user authentication before accessing the chat interface
- **FR-016**: System MUST handle AI service unavailability by retrying up to 3 times with exponential backoff, then displaying a clear error message if all retries fail
- **FR-017**: AI MUST explain validation errors when asked and suggest fixes
- **FR-018**: System MUST preserve in-progress blueprints if the user navigates away or the session is interrupted
- **FR-019**: System MUST retain the last 100 messages per chat session and warn the user when approaching this limit

### Key Entities

- **Chat Session**: A conversation context containing message history, the in-progress blueprint draft, and session metadata (user, timestamps, status)
- **Chat Message**: An individual message in the conversation with sender (user/assistant), content, timestamp, and optional tool execution results
- **Blueprint Draft**: The working copy of the blueprint being designed, updated in real-time as the AI makes changes
- **Tool Execution**: A record of AI tool invocations (create_blueprint, add_participant, etc.) with inputs and outputs

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a valid two-participant, two-action blueprint through conversation in under 5 minutes
- **SC-002**: 80% of users successfully create their first blueprint without requiring manual JSON editing
- **SC-003**: AI responses begin streaming within 2 seconds of user message submission
- **SC-004**: Blueprint preview updates within 1 second of AI tool execution
- **SC-005**: Validation feedback appears within 500ms of blueprint changes
- **SC-006**: System supports at least 50 concurrent chat sessions without degradation
- **SC-007**: 90% of validation errors can be explained and resolved through AI conversation
- **SC-008**: Session recovery restores in-progress blueprints for users who navigate away and return within 24 hours

## Clarifications

### Session 2026-02-01

- Q: What are the conversation history limits per session? → A: Last 100 messages per session, warn user when approaching limit
- Q: How should the system handle AI service failures? → A: 3 retries with exponential backoff, then show error

## Assumptions

- Users have basic understanding of workflow concepts (participants, steps, approvals) even if they don't know blueprint syntax
- The existing Blueprint Service and real-time infrastructure can be extended to support the chat functionality
- An AI provider will be configured server-side with appropriate rate limits and cost controls
- The existing authentication system will be used to secure the chat interface
- Blueprint validation logic already exists and can be invoked programmatically
- The Fluent builder provides all necessary operations for AI-driven blueprint construction
