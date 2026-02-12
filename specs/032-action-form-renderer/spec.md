# Feature Specification: Action Form Renderer

**Feature Branch**: `032-action-form-renderer`
**Created**: 2026-02-12
**Status**: Draft
**Input**: Build a JSON Forms-aligned action form renderer that dynamically generates interactive forms from blueprint Action definitions, supporting all control types, disclosure filtering, credential gating, and wallet signing — designed for portability across web frameworks.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Participant Completes an Action Form (Priority: P1)

A workflow participant receives a pending action (e.g., "Submit Invoice"). The system renders an interactive form based on the blueprint's `form` Control tree and `dataSchemas`. The participant fills in text fields, selects dates, picks from dropdowns, enters numbers, and uploads files. Client-side validation highlights errors inline before submission. On submit, the data is packaged and sent to the blueprint engine.

**Why this priority**: This is the core value proposition — without form rendering, participants cannot interact with workflows. Every other feature depends on this working correctly.

**Independent Test**: Can be fully tested by loading a sample blueprint (e.g., simple-invoice-approval) and rendering its form definition. Delivers immediate value: participants can fill and submit action data.

**Acceptance Scenarios**:

1. **Given** a blueprint action with a `form` Control tree containing TextLine, Numeric, DateTime, Selection, TextArea, and Checkbox controls in nested Vertical/Horizontal layouts, **When** the form renders, **Then** each control type displays as the appropriate interactive input element, respecting the layout hierarchy.
2. **Given** a form with required fields defined in `dataSchemas`, **When** the participant submits without filling required fields, **Then** inline validation errors appear next to each missing field and submission is blocked.
3. **Given** a form with a Selection control whose `scope` points to a field with `enum` values in the data schema, **When** the form renders, **Then** the dropdown is populated with the enum values from the schema.
4. **Given** a form with `dataSchemas` validation constraints (minLength, maximum, pattern, format), **When** the participant enters invalid data, **Then** real-time validation feedback appears as they type or on blur.
5. **Given** a completed form, **When** the participant clicks Submit, **Then** all field values are collected according to their JSON Pointer scopes and packaged as a structured data dictionary for the action submission.

---

### User Story 2 - Participant Views Read-Only Data from Previous Actions (Priority: P1)

When an action has `previousData` (data from earlier workflow steps), the form displays this data as read-only alongside the editable input fields. The participant can see what was submitted before (e.g., the invoice details) while filling in their own response (e.g., approval decision and payment date).

**Why this priority**: Most workflow actions beyond the first step need context from previous submissions. Without this, approvers and reviewers cannot make informed decisions.

**Independent Test**: Can be tested by loading an action that has `previousData` populated and verifying read-only fields render with the correct values and cannot be edited.

**Acceptance Scenarios**:

1. **Given** an action with `previousData` containing values for fields defined in a prior action's schema, **When** the form renders, **Then** those fields display as read-only with their values visible and clearly distinguished from editable fields.
2. **Given** an action with both `previousData` and its own editable `form` controls, **When** the form renders, **Then** the previous data section appears above or alongside the input section with a clear visual separation.
3. **Given** read-only data containing nested objects or arrays, **When** displayed, **Then** the data is presented in a structured, readable format (not raw JSON).

---

### User Story 3 - Disclosure-Filtered Form Rendering (Priority: P1)

The form respects disclosure rules. Each participant only sees the fields they are permitted to view based on the action's `disclosures` configuration and their wallet address or participant ID. Fields outside the participant's disclosure scope are hidden entirely — they never appear in the rendered output.

**Why this priority**: Disclosure is a core tenet of the DAD security model. Rendering fields a participant should not see — even if hidden via CSS — would violate the security model. This must be correct from the start.

**Independent Test**: Can be tested by rendering the same action form for two different participant addresses with different disclosure rules and verifying each sees only their permitted fields.

**Acceptance Scenarios**:

1. **Given** an action with disclosures granting participant A access to `/invoiceNumber` and `/amount` only, **When** participant A views the form, **Then** only "Invoice Number" and "Amount" fields are rendered; no other fields exist in the output.
2. **Given** an action with disclosures granting `/*` (all fields) to a participant, **When** that participant views the form, **Then** all form controls are rendered.
3. **Given** a participant with no matching disclosure entry for an action, **When** they view the form, **Then** no data fields are rendered and a message indicates they do not have access to this action's data.
4. **Given** read-only `previousData` display, **When** filtered by disclosure, **Then** only the disclosed data pointers from the previous submission are shown.

---

### User Story 4 - Conditional Display and Calculated Fields (Priority: P2)

Form controls with `conditions` (JSON Logic rules) show or hide dynamically based on the current form data. Calculated fields defined in the action's `calculations` property update in real-time as the participant enters data.

**Why this priority**: Conditional display makes forms smarter and less cluttered. Calculated fields (e.g., totalAmount = amount + taxAmount) reduce manual entry and errors. These enhance usability but the core form works without them.

**Independent Test**: Can be tested by loading a form with conditional controls and calculations, then entering data that triggers show/hide and verifying calculated values update.

**Acceptance Scenarios**:

1. **Given** a control with a condition `{">=": [{"var": "amount"}, 5000]}`, **When** the participant enters an amount of 6000, **Then** the conditional control becomes visible.
2. **Given** the same conditional control, **When** the amount is changed to 3000, **Then** the conditional control is hidden and its value is cleared.
3. **Given** an action with calculations `{"totalAmount": {"+": [{"var": "amount"}, {"var": "taxAmount"}]}}`, **When** the participant enters amount=1000 and taxAmount=200, **Then** a read-only "Total Amount" field displays 1200 and updates live as values change.
4. **Given** a calculated field that depends on a field the participant has not yet filled, **When** the form renders, **Then** the calculated field shows a placeholder or empty state rather than an error.

---

### User Story 5 - File Attachment Support (Priority: P2)

Participants can attach files to their action submissions using the File control type. The form displays upload controls with drag-and-drop support, file type filtering, size limits, and upload progress indication.

**Why this priority**: Many real-world workflows (invoices with supporting documents, medical records, shipping manifests) require file attachments. This extends the core form with a common need.

**Independent Test**: Can be tested by rendering a form with a File control, attaching a file, and verifying it is included in the submission payload.

**Acceptance Scenarios**:

1. **Given** a form with a File control, **When** the participant drags a file onto the upload area or clicks to browse, **Then** the file is accepted and its name, size, and type are displayed.
2. **Given** a File control with schema constraints (e.g., maxFileSize, accepted MIME types), **When** the participant selects an invalid file, **Then** a validation error explains the constraint (e.g., "File must be under 10MB" or "Only PDF and PNG files are accepted").
3. **Given** a submitted form with file attachments, **When** processed, **Then** the files are included in the action submission payload alongside the form data.

---

### User Story 6 - Credential Presentation and Gating (Priority: P2)

When an action has `credentialRequirements`, the form displays what credentials the participant must present before they can submit. The participant selects from their wallet's stored verifiable credentials. The form validates that the presented credential meets the requirement (type, issuer, required claims) before enabling submission.

**Why this priority**: Credential gating is a key differentiator for Sorcha — it enables trust-verified workflows. It builds on the core form but is essential for regulated industries.

**Independent Test**: Can be tested by loading an action with credential requirements and verifying the credential selection UI appears and blocks submission until requirements are met.

**Acceptance Scenarios**:

1. **Given** an action with a `credentialRequirement` of type "LicenseCredential", **When** the form renders, **Then** a credential section displays the requirement description and prompts the participant to present a matching credential.
2. **Given** the participant has a matching credential in their wallet, **When** they select it, **Then** the form validates the credential type and issuer against the requirement and shows a success indicator.
3. **Given** the participant does not have a matching credential, **When** they view the form, **Then** the submit button is disabled and a message explains what credential is needed.
4. **Given** a credential with selective disclosure, **When** presented, **Then** only the required claims are disclosed (not the full credential).

---

### User Story 7 - Zero-Knowledge Proof Submission (Priority: P3)

For workflows requiring privacy-preserving verification, participants can submit zero-knowledge proofs that attest to facts about their data without revealing the underlying values (e.g., proving income exceeds a threshold without disclosing exact income).

**Why this priority**: ZKP is an advanced privacy feature. The form infrastructure must support it, but implementation can follow the core form and credential features.

**Independent Test**: Can be tested by loading an action that requests a ZKP-type proof field and verifying the proof submission UI renders and accepts proof data.

**Acceptance Scenarios**:

1. **Given** a form control configured as a ZKP proof field (indicated by control properties), **When** rendered, **Then** the participant sees a proof generation/attachment interface explaining what claim must be proven.
2. **Given** a participant has a pre-generated proof, **When** they attach it, **Then** the form includes the proof in the submission payload for server-side verification.
3. **Given** a ZKP field with a description of the required proof, **When** the form renders, **Then** a human-readable explanation of what is being proven is displayed (e.g., "Prove your annual income exceeds $50,000 without disclosing the exact amount").

---

### User Story 8 - Wallet Signing on Submit (Priority: P2)

When the participant submits a completed form, the data is hashed and signed with their wallet's private key. The signature is included in the submission, providing cryptographic proof of authorship and data integrity.

**Why this priority**: Every transaction in Sorcha is cryptographically signed. The form must integrate with the wallet service to produce a valid signature on submission.

**Independent Test**: Can be tested by submitting a form and verifying the submission includes a valid cryptographic signature from the participant's active wallet.

**Acceptance Scenarios**:

1. **Given** a completed form ready for submission, **When** the participant clicks Submit, **Then** the form data is serialized, hashed (SHA-256), and sent to the wallet service for signing.
2. **Given** the wallet service returns a signature, **When** the submission is sent to the blueprint engine, **Then** the payload includes the data, the signature, and the signing wallet address.
3. **Given** the wallet service is unavailable or signing fails, **When** submission is attempted, **Then** an error message is displayed and the form data is preserved (not lost).
4. **Given** a participant with multiple wallets, **When** submitting, **Then** the active wallet (as selected in the UI session) is used for signing.

---

### User Story 9 - Form Rendering in Categorization (Tab) Layout (Priority: P3)

Complex actions with many fields use the Categorization layout to organize controls into tabbed sections. Each category (tab) contains its own group of controls. Participants navigate between tabs to complete different sections of a large form.

**Why this priority**: Categorization is important for complex real-world forms (e.g., medical referrals with patient info, clinical details, and attachments as separate tabs) but basic forms work fine without it.

**Independent Test**: Can be tested by loading a form with Categorization layout and verifying tabs render with correct content.

**Acceptance Scenarios**:

1. **Given** a form with Categorization layout containing three categories ("Patient Info", "Clinical Details", "Attachments"), **When** rendered, **Then** three tabs appear with the correct labels.
2. **Given** a multi-tab form, **When** the participant switches tabs, **Then** previously entered data in other tabs is preserved.
3. **Given** a multi-tab form with validation errors, **When** the participant tries to submit, **Then** the tab containing the first error is activated and the error is highlighted.

---

### Edge Cases

- What happens when a blueprint action has no `form` property? The renderer falls back to auto-generating a form from `dataSchemas` properties (current behavior as baseline).
- What happens when a `form` control references a `scope` that does not exist in the data schema? The control renders but shows a warning indicator to blueprint designers during development.
- What happens when the same field appears in multiple controls? The values stay synchronized — editing in one location updates all bound controls.
- How does the form handle deeply nested data objects (e.g., `/address/street/line1`)? Scope binding resolves the full JSON Pointer path into the data dictionary.
- What happens when a form is loaded on a slow connection? The form skeleton renders immediately with loading indicators for any asynchronous data (credentials, previous data).
- What happens when a blueprint specifies a control type the renderer does not recognize? An "unsupported control" placeholder is displayed with the control type name, rather than crashing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST recursively render a `Control` tree into interactive form elements, supporting all 10 control types: Layout, Label, TextLine, TextArea, Numeric, DateTime, File, Choice, Checkbox, Selection.
- **FR-002**: System MUST support all 4 layout types: VerticalLayout (stacked), HorizontalLayout (side-by-side), Group (bordered section with title), and Categorization (tabbed sections).
- **FR-003**: System MUST bind each control to its data field using JSON Pointer syntax in the `scope` property (e.g., `/invoiceNumber`, `/address/city`).
- **FR-004**: System MUST validate form data against JSON Schema Draft 2020-12 constraints from `dataSchemas`, including: type, required, minLength, maxLength, minimum, maximum, pattern, format, enum.
- **FR-005**: System MUST display inline validation errors next to the offending field, triggered on blur or on submit attempt.
- **FR-006**: System MUST filter rendered controls based on the current participant's disclosure permissions — undisclosed fields MUST NOT appear in the rendered output.
- **FR-007**: System MUST render `previousData` as read-only fields, visually distinguished from editable inputs, and filtered by disclosure rules.
- **FR-008**: System MUST evaluate `conditions` (JSON Logic) on controls to dynamically show/hide them as data changes.
- **FR-009**: System MUST compute and display `calculations` (JSON Logic) as read-only derived fields that update in real-time.
- **FR-010**: System MUST support file upload controls with drag-and-drop, file type filtering, and size limit enforcement.
- **FR-011**: System MUST support credential presentation — displaying credential requirements, allowing selection from the participant's wallet credentials, and validating against the requirement before enabling submission.
- **FR-012**: System MUST support zero-knowledge proof fields — displaying what claim must be proven and accepting proof attachments.
- **FR-013**: System MUST integrate with the wallet service to cryptographically sign submitted data (SHA-256 hash + wallet signature) before sending to the engine.
- **FR-014**: When no `form` property exists on an action, the system MUST auto-generate a form from `dataSchemas` properties as a fallback.
- **FR-015**: System MUST populate Selection and Choice controls with `enum` values from the corresponding field in `dataSchemas`.
- **FR-016**: System MUST render an "unsupported control" placeholder for any unrecognized control types rather than failing.
- **FR-017**: The form specification MUST align with JSON Forms conventions (UISchema + DataSchema pattern, JSON Pointer scopes, rule-based conditions) to enable future portability to other rendering frameworks.
- **FR-018**: System MUST support Group layout with a visible border and title header for organizing related fields.
- **FR-019**: System MUST block form submission until all credential requirements are satisfied and all required fields pass validation.
- **FR-020**: System MUST preserve form state when navigating between Categorization tabs.

### Key Entities

- **Control Tree**: Hierarchical definition of form layout and input controls, with each node specifying type, scope binding, layout, child elements, properties, and conditional display rules. Aligns with JSON Forms UISchema vocabulary.
- **Data Schema**: JSON Schema Draft 2020-12 document defining the structure, types, constraints, and required fields for action data. Aligns with JSON Forms DataSchema.
- **Disclosure Filter**: Per-participant list of JSON Pointer paths defining which data fields are visible. Applied before rendering to exclude non-disclosed controls.
- **Form Data**: Dictionary of field values keyed by JSON Pointer path, collected from user input, merged with calculated values, and submitted with a cryptographic signature.
- **Credential Presentation**: A verifiable credential selected from the participant's wallet that satisfies an action's credential requirement, submitted alongside form data.
- **Proof Attachment**: A zero-knowledge proof artifact proving a claim about data without revealing the underlying value.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 control types render correctly and accept valid user input across all supported form configurations.
- **SC-002**: Forms with nested layouts (3+ levels deep) render with correct visual hierarchy — horizontal layouts display side-by-side, vertical layouts stack, groups have borders, categorization shows tabs.
- **SC-003**: 100% of sample blueprints (8 existing samples) render correctly without errors, and each form can be submitted with valid data.
- **SC-004**: Disclosure filtering is enforced: when two participants with different disclosure rules view the same action, each sees only their permitted fields and the other fields are absent from the rendered output.
- **SC-005**: Client-side validation catches 100% of JSON Schema constraint violations (required, type, min/max, pattern, enum) before server submission.
- **SC-006**: Calculated fields update within 200ms of a dependent input change, providing responsive real-time feedback.
- **SC-007**: Conditional show/hide transitions occur within 100ms of the triggering data change.
- **SC-008**: Form submission includes a valid cryptographic signature that can be verified by the validation engine.
- **SC-009**: The form specification uses JSON Forms-compatible vocabulary (scope as JSON Pointer, rule conditions, layout types) so that the same blueprint form definitions could be rendered by a standard JSON Forms renderer with Sorcha-specific extensions.
- **SC-010**: Participants can complete a standard 7-field invoice form (text, date, number, dropdown, checkbox) in under 60 seconds from form load to successful submission.

## Assumptions

- The existing `Control` class, `ControlTypes` enum, and `LayoutTypes` enum in `Sorcha.Blueprint.Models` are the canonical form definition and will be evolved (not replaced) for JSON Forms alignment.
- JSON Logic is the condition evaluation engine for both conditional display and calculated fields (consistent with the existing blueprint engine).
- The wallet service's signing API is available and functional for form submission signing.
- Credential presentation uses the existing Verifiable Credentials infrastructure built in branch 031.
- ZKP support in this phase covers the UI affordance (proof attachment/display) — the actual proof generation and verification happen server-side and are out of scope.
- File uploads are transmitted as part of the action submission payload (base64 or multipart) — the specific transport mechanism is an implementation detail.
- The initial implementation targets Blazor WASM with MudBlazor, but the form specification (JSON contract) is framework-agnostic by design.

## Scope Boundaries

**In Scope:**
- Recursive Control tree rendering with all 10 control types and 4 layout types
- JSON Pointer scope binding and data collection
- JSON Schema validation (client-side)
- Disclosure-based field filtering
- Read-only previous data display
- Conditional display (JSON Logic)
- Calculated fields (JSON Logic, real-time)
- File attachment controls
- Credential requirement display and selection
- ZKP proof attachment UI
- Wallet signing integration on submit
- Auto-generation fallback from DataSchema when no Form is defined
- Alignment with JSON Forms specification vocabulary

**Out of Scope:**
- Server-side form rendering (SSR)
- ZKP proof generation logic (server-side concern)
- Credential issuance (separate workflow output, not form input)
- Blueprint designer changes (the visual designer is a separate feature)
- Non-Blazor renderer implementations (React, Vue — future work enabled by the specification alignment)
- Offline form submission and sync
- Form versioning or migration between blueprint versions
