# Tasks: Action Form Renderer

**Input**: Design documents from `/specs/032-action-form-renderer/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/renderer-api.md, quickstart.md

**Tests**: Included — the constitution requires >85% coverage for new code, and the spec defines testable acceptance scenarios.

**Organization**: Tasks grouped by user story. 9 user stories mapped from spec.md priorities.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Model layer changes and project scaffolding

- [x] T001 Add FormRule.cs, SchemaBasedCondition.cs, RuleEffect.cs to `src/Common/Sorcha.Blueprint.Models/`
- [x] T002 Modify Control.cs — add `Rule` (FormRule?) and `Options` (JsonDocument?) properties, mark `Conditions` as `[Obsolete]` in `src/Common/Sorcha.Blueprint.Models/Control.cs`
- [x] T003 Add `WithRule()` method to ControlBuilder in `src/Core/Sorcha.Blueprint.Fluent/FormBuilder.cs`
- [x] T004 Create `Components/Forms/`, `Models/Forms/`, `Services/Forms/` directory structure under `src/Apps/Sorcha.UI/Sorcha.UI.Core/`
- [x] T005 [P] Create FormContext.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Forms/FormContext.cs` — data dictionary, validation errors, disclosure filter, calculated values, OnDataChanged event, SetValue/GetValue/Validate/ValidateField/IsDisclosed/GetSchemaForScope methods
- [x] T006 [P] Create FormSubmission.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Forms/FormSubmission.cs` — Data, Signature, SigningWalletAddress, CredentialPresentations, FileAttachments, ProofAttachments
- [x] T007 [P] Create FileAttachmentInfo.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Forms/FileAttachmentInfo.cs`
- [x] T008 [P] Create ProofAttachment.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Forms/ProofAttachment.cs`
- [x] T009 Create IFormSchemaService.cs and FormSchemaService.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Forms/` — MergeSchemas, GetSchemaForScope, NormalizeScope, ValidateData, ValidateField, GetEnumValues, IsRequired, AutoGenerateForm methods
- [x] T010 Create IFormSigningService.cs and FormSigningService.cs in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Forms/` — SerializeFormData, HashData (SHA-256), SignWithWallet (delegates to IWalletApiService)
- [x] T011 Register IFormSchemaService and IFormSigningService in DI — update `src/Apps/Sorcha.UI/Sorcha.UI.Core/` service registration (or the Web.Client Program.cs)
- [x] T012 Build and verify zero warnings/errors across Sorcha.Blueprint.Models, Sorcha.Blueprint.Fluent, and Sorcha.UI.Core

**Checkpoint**: Model layer and services ready — component implementation can begin

---

## Phase 2: Foundational (Core Renderer Infrastructure)

**Purpose**: Root component, dispatcher, and layout renderers that ALL user stories depend on

- [x] T013 Create SorchaFormRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/SorchaFormRenderer.razor` — accepts Action, PreviousData, ParticipantAddress, SigningWalletAddress, OnSubmit/OnReject/OnCancel EventCallbacks, IsReadOnly. Initializes FormContext, builds disclosure filter, auto-generates form if Form is null. Provides FormContext as CascadingValue.
- [x] T014 Create ControlDispatcher.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/ControlDispatcher.razor` — accepts Control + Depth. Checks disclosure filter → evaluates Rule → dispatches to type-specific renderer by ControlType switch. Recursively renders Elements for Layout types.
- [x] T015 [P] Create VerticalLayoutRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Layouts/VerticalLayoutRenderer.razor` — MudStack Spacing=3, iterates Elements via ControlDispatcher
- [x] T016 [P] Create HorizontalLayoutRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Layouts/HorizontalLayoutRenderer.razor` — MudStack Row=true Spacing=3, equal-width children
- [x] T017 [P] Create LabelRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/LabelRenderer.razor` — read-only MudText displaying Control.Title
- [x] T018 [P] Create UnsupportedControlRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/UnsupportedControlRenderer.razor` — MudAlert warning with control type name
- [x] T019 Build and verify zero warnings/errors in Sorcha.UI.Core

**Checkpoint**: Foundation ready — can render basic Layout + Label forms. User story implementation begins.

---

## Phase 3: User Story 1 — Participant Completes an Action Form (Priority: P1) MVP

**Goal**: Render all 10 control types from a Control tree, validate against JSON Schema, collect data by scope, and submit.

**Independent Test**: Load simple-invoice-approval blueprint, render form, fill fields, submit. All controls render correctly with validation.

### Tests for User Story 1

- [x] T020 [P] [US1] Create FormContextTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/FormContextTests.cs` — test SetValue/GetValue, scope-based data binding, OnDataChanged event, Validate returns errors for required missing fields
- [x] T021 [P] [US1] Create FormSchemaServiceTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/FormSchemaServiceTests.cs` — test MergeSchemas, GetSchemaForScope, NormalizeScope, ValidateField (minLength, max, pattern, enum, required), GetEnumValues, IsRequired, AutoGenerateForm fallback
- [x] T022 [P] [US1] Create AutoGenerationTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/AutoGenerationTests.cs` — test auto-generation from DataSchema: string→TextLine, number→Numeric, boolean→Checkbox, date format→DateTime, enum→Selection, long string→TextArea

### Implementation for User Story 1

- [x] T023 [P] [US1] Create TextLineRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/TextLineRenderer.razor` — MudTextField<string>, reads value from FormContext by scope, writes on change, shows validation errors, reads Required from schema
- [x] T024 [P] [US1] Create TextAreaRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/TextAreaRenderer.razor` — MudTextField<string> Lines=5, same binding pattern as TextLine
- [x] T025 [P] [US1] Create NumericRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/NumericRenderer.razor` — MudNumericField<decimal>, min/max from schema, validation on blur
- [x] T026 [P] [US1] Create DateTimeRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/DateTimeRenderer.razor` — MudDatePicker for format=date, MudDatePicker+MudTimePicker for date-time
- [x] T027 [P] [US1] Create CheckboxRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/CheckboxRenderer.razor` — MudSwitch<bool>, label from Control.Title
- [x] T028 [P] [US1] Create SelectionRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/SelectionRenderer.razor` — MudSelect<string>, populates options from schema enum values via FormSchemaService.GetEnumValues
- [x] T029 [P] [US1] Create ChoiceRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/ChoiceRenderer.razor` — MudRadioGroup<string>, populates from schema enum values
- [x] T030 [US1] Implement auto-generation fallback in FormSchemaService.AutoGenerateForm — when Action.Form is null, create VerticalLayout with controls inferred from DataSchema properties (string→TextLine, number→Numeric, boolean→Checkbox, date→DateTime, enum→Selection)
- [x] T031 [US1] Implement inline validation in SorchaFormRenderer — on blur calls FormContext.ValidateField, on submit calls FormContext.Validate, validation errors displayed via each renderer's ErrorText property
- [x] T032 [US1] Implement form submission flow in SorchaFormRenderer — collect all FormData by scope, package into FormSubmission, invoke OnSubmit callback. Add Submit/Reject/Cancel buttons as MudButton group.
- [x] T033 [US1] Update ActionForm.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/ActionForm.razor` — replace stub implementation with SorchaFormRenderer component, map PendingActionViewModel to Action, wire OnSubmit to DialogResult.Ok(FormSubmission)
- [x] T034 [US1] Build and verify: load simple-invoice-approval.json form definition, confirm all 7 fields render (TextLine, DateTime x2, TextArea, Numeric x2, Selection), fill with valid data, submit succeeds

**Checkpoint**: MVP complete — participants can fill and submit action forms with full validation.

---

## Phase 4: User Story 2 — Read-Only Previous Data Display (Priority: P1)

**Goal**: Display data from prior workflow actions as read-only alongside editable input fields.

**Independent Test**: Load approval action with previousData, verify read-only fields render with values and cannot be edited.

### Implementation for User Story 2

- [x] T035 [US2] Create PreviousDataPanel.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Panels/PreviousDataPanel.razor` — accepts JsonDocument, parses into key-value pairs, renders as read-only MudTextField fields with ReadOnly=true, nested objects displayed with structured indentation, collapsible header "Previous Submission"
- [x] T036 [US2] Integrate PreviousDataPanel into SorchaFormRenderer — render above editable form section when PreviousData is not null, with MudDivider separator
- [x] T037 [US2] Handle nested previousData objects — if value is object/array, render as expandable MudTreeView or formatted key-value cards rather than raw JSON
- [x] T038 [US2] Verify with simple-invoice-approval action 1 (Approve Payment) — load with previousData containing invoice fields, confirm read-only display above editable approval fields

**Checkpoint**: Approval-type actions show context from prior submissions.

---

## Phase 5: User Story 3 — Disclosure-Filtered Form Rendering (Priority: P1)

**Goal**: Filter rendered controls based on participant's disclosure permissions. Undisclosed fields never appear in DOM.

**Independent Test**: Render same action for two different participants with different disclosures, verify each sees only permitted fields.

### Tests for User Story 3

- [x] T039 [P] [US3] Create DisclosureFilterTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/DisclosureFilterTests.cs` — test BuildDisclosureFilter from Disclosures + ParticipantAddress, IsDisclosed for exact match, wildcard /*, nested paths, no matching disclosure returns empty set

### Implementation for User Story 3

- [x] T040 [US3] Implement BuildDisclosureFilter in FormContext or FormSchemaService — takes Action.Disclosures + ParticipantAddress, returns HashSet<string> of allowed scopes. Handle wildcard `/*` as allow-all. Handle nested paths `/address/city` matching `/address/*`.
- [x] T041 [US3] Integrate disclosure check into ControlDispatcher.razor — before rendering any control with a scope, call FormContext.IsDisclosed(scope). If not disclosed, return empty fragment (no DOM output). Layout controls without scope are always rendered.
- [x] T042 [US3] Apply disclosure filter to PreviousDataPanel — filter previousData key-value pairs by disclosure rules before rendering. Only show disclosed previous fields.
- [x] T043 [US3] Handle sender-action disclosure — when the current participant is the action sender (Action.Sender matches ParticipantAddress), they see all fields for their own action (implicit full disclosure for data entry).
- [x] T044 [US3] Verify with simple-invoice-approval — render action 1 (Approve Payment) as vendor (disclosures: /approved, /paymentDate, /paymentMethod, /notes) and verify all 4 fields visible. Then render as a non-disclosed participant and verify no fields rendered.

**Checkpoint**: DAD security model enforced — disclosure filtering is structural, not cosmetic.

---

## Phase 6: User Story 4 — Conditional Display and Calculated Fields (Priority: P2)

**Goal**: JSON Forms rules show/hide/enable/disable controls dynamically. Calculated fields update in real-time.

**Independent Test**: Load form with conditional control and calculations, enter triggering data, verify show/hide and calculated values.

### Tests for User Story 4

- [x] T045 [P] [US4] Create CalculatedFieldTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/CalculatedFieldTests.cs` — test JSON Logic evaluation for calculations (add, subtract, var, comparisons), handling of missing variables, update on data change

### Implementation for User Story 4

- [x] T046 [US4] Implement rule evaluation in ControlDispatcher — when Control.Rule is set, evaluate SchemaBasedCondition (use JsonSchema.Net to validate FormData at scope against condition schema). Apply effect: SHOW/HIDE controls visibility, ENABLE/DISABLE controls interactivity.
- [x] T047 [US4] Implement calculated fields in SorchaFormRenderer — subscribe to FormContext.OnDataChanged, re-evaluate Action.Calculations using JsonLogic.Apply on current FormData, update FormContext.CalculatedValues
- [x] T048 [US4] Create CalculatedFieldsPanel.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Panels/CalculatedFieldsPanel.razor` — displays each calculated field as read-only MudTextField with calculator icon, updates reactively when CalculatedValues change
- [x] T049 [US4] Wire rule re-evaluation to data changes — when any value changes via FormContext.SetValue, re-evaluate all rules on all controls. Controls subscribe to FormContext.OnDataChanged to trigger re-render.
- [x] T050 [US4] Verify with simple-invoice-approval — the totalAmount calculation ({"+": [{"var": "amount"}, {"var": "taxAmount"}]}) should update live as amount and taxAmount are entered

**Checkpoint**: Smart forms with dynamic behavior and live calculations.

---

## Phase 7: User Story 5 — File Attachment Support (Priority: P2)

**Goal**: File upload control with drag-and-drop, type filtering, size limits.

**Independent Test**: Render form with File control, upload a file, verify it appears in submission payload.

### Implementation for User Story 5

- [x] T051 [US5] Create FileRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Controls/FileRenderer.razor` — MudFileUpload with drag-and-drop, displays file name/size/type after selection, reads constraints from Control.Options or schema (maxFileSize, acceptedTypes), validates on select
- [x] T052 [US5] Integrate file attachments into FormContext — add FileAttachments list, FileRenderer adds/removes files, files converted to base64 on submission
- [x] T053 [US5] Include FileAttachments in FormSubmission — when SorchaFormRenderer packages the submission, map FileAttachmentInfo to FileAttachment objects for the ActionSubmissionRequest
- [x] T054 [US5] Verify file upload — render a form with File control, attach a PDF, confirm file info displays, submit and verify FileAttachments in FormSubmission output

**Checkpoint**: Document-heavy workflows (invoices, medical records) can attach supporting files.

---

## Phase 8: User Story 6 — Credential Presentation and Gating (Priority: P2)

**Goal**: Display credential requirements, allow selection from wallet credentials, validate and block submission until requirements met.

**Independent Test**: Load action with credentialRequirements, verify credential selection UI, verify submit blocked without credential.

### Implementation for User Story 6

- [x] T055 [US6] Create CredentialGatePanel.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Panels/CredentialGatePanel.razor` — for each CredentialRequirement: display description, fetch credentials from IWalletApiService for the participant's wallet, filter by Type and AcceptedIssuers, render MudSelect for credential selection, show success/failure chip
- [x] T056 [US6] Implement credential validation in CredentialGatePanel — when a credential is selected, verify Type matches, issuer matches AcceptedIssuers (if specified), required claims are present. Show validation result.
- [x] T057 [US6] Gate form submission on credential satisfaction — SorchaFormRenderer checks all CredentialRequirements are satisfied before enabling Submit button. Show MudAlert explaining missing credentials.
- [x] T058 [US6] Include CredentialPresentations in FormSubmission — map selected credentials to CredentialPresentation objects (CredentialId, DisclosedClaims from RequiredClaims, RawPresentation token)
- [x] T059 [US6] Verify credential gating — load action with CredentialRequirement type="LicenseCredential", confirm submit is disabled, select matching credential, confirm submit enables

**Checkpoint**: Trust-verified workflows with credential gating operational.

---

## Phase 9: User Story 8 — Wallet Signing on Submit (Priority: P2)

**Goal**: Hash form data and sign with participant's wallet before submission.

**Independent Test**: Submit form, verify FormSubmission includes valid signature from active wallet.

### Tests for User Story 8

- [x] T060 [P] [US8] Create FormSigningServiceTests.cs in `tests/Sorcha.UI.Core.Tests/Components/Forms/FormSigningServiceTests.cs` — test SerializeFormData produces deterministic JSON, HashData produces correct SHA-256, SignWithWallet calls IWalletApiService.SignDataAsync with correct parameters, handles wallet unavailable gracefully

### Implementation for User Story 8

- [x] T061 [US8] Implement FormSigningService.SignSubmissionAsync — serialize FormData to canonical JSON (sorted keys), compute SHA-256 hash, call IWalletApiService.SignDataAsync(walletAddress, request), return signature bytes
- [x] T062 [US8] Integrate signing into SorchaFormRenderer submit flow — after validation passes and credentials are satisfied, call FormSigningService.SignSubmissionAsync, include signature in FormSubmission. Show loading indicator during signing. On failure, show error and preserve form data.
- [x] T063 [US8] Handle signing errors — if wallet service unavailable or signing fails, display MudAlert with error, keep Submit button enabled for retry, never lose form data
- [x] T064 [US8] Verify end-to-end signing — submit invoice form, confirm FormSubmission.Signature is non-null and FormSubmission.SigningWalletAddress matches the active wallet

**Checkpoint**: All submissions are cryptographically signed.

---

## Phase 10: User Story 9 — Categorization (Tab) Layout (Priority: P3)

**Goal**: Tabbed form layout for complex multi-section forms.

**Independent Test**: Load form with Categorization layout, verify tabs render, data preserved across tab switches.

### Implementation for User Story 9

- [x] T065 [US9] Create GroupRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Layouts/GroupRenderer.razor` — MudPaper with elevation, MudText title header, renders child Elements via ControlDispatcher
- [x] T066 [US9] Create CategorizationRenderer.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Forms/Layouts/CategorizationRenderer.razor` — MudTabs, each child Element becomes a MudTabPanel with title from Element.Title, renders Element.Elements via ControlDispatcher. Data preserved across tab switches (stored in FormContext).
- [x] T067 [US9] Implement validation-aware tab switching — on submit with errors, activate the tab containing the first validation error. Show error count badge on tabs with errors.
- [x] T068 [US9] Verify with a multi-tab test — create a test blueprint with Categorization layout (3 tabs), fill data in tab 1, switch to tab 2, switch back, verify tab 1 data preserved. Submit with missing required field in tab 2, verify tab 2 activates.

**Checkpoint**: Complex multi-section forms with tabbed navigation.

---

## Phase 11: User Story 7 — Zero-Knowledge Proof Submission (Priority: P3)

**Goal**: UI affordance for ZKP proof attachment and display.

**Independent Test**: Load form with ZKP proof field, attach proof data, verify included in submission.

### Implementation for User Story 7

- [x] T069 [US7] Add ZKP control support — define ZKP as a control option (e.g., Control.Options containing `{"format": "zkp-proof"}`) that FileRenderer or a new dedicated renderer recognizes
- [x] T070 [US7] Create ZKP proof UI — display human-readable claim description from Control.Title, provide file attachment for proof data, show proof type and public inputs fields
- [x] T071 [US7] Include ProofAttachments in FormSubmission — map attached proofs to ProofAttachment objects with ClaimDescription, ProofType, ProofData, PublicInputs
- [x] T072 [US7] Verify ZKP submission — load action with ZKP proof field, attach proof file, confirm ProofAttachments in FormSubmission

**Checkpoint**: Privacy-preserving proof submission UI operational.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Integration, verification across all sample blueprints, documentation.

- [x] T073 Verify all 8 sample blueprints render correctly — iterate through `samples/blueprints/` (finance, healthcare, benefits, supply-chain), load each, confirm forms render without errors
- [x] T074 [P] Add FormRule serialization tests in `tests/Sorcha.Blueprint.Models.Tests/FormRuleTests.cs` — test FormRule, SchemaBasedCondition, RuleEffect JSON round-trip, test backward compat with deprecated Conditions property
- [x] T075 [P] Update sample blueprints that need Rule examples — add at least one conditional field (with `rule`) to `samples/blueprints/finance/moderate-purchase-order-approval.json` to demonstrate the feature
- [x] T076 Build entire solution — `dotnet build` with zero warnings, zero errors
- [x] T077 Run all existing tests — verify no regressions in Blueprint.Models.Tests, Blueprint.Fluent.Tests, Blueprint.Engine.Tests, UI.Core (if existing tests exist)
- [x] T078 Run E2E tests — `dotnet test tests/Sorcha.UI.E2E.Tests/` — verify no new failures beyond pre-existing baseline

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — starts immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1 - Core Form)**: Depends on Phase 2 — MVP target
- **Phase 4 (US2 - Previous Data)**: Depends on Phase 3 (needs SorchaFormRenderer)
- **Phase 5 (US3 - Disclosure)**: Depends on Phase 3 (needs ControlDispatcher)
- **Phase 6 (US4 - Conditions/Calcs)**: Depends on Phase 3 (needs FormContext data binding)
- **Phase 7 (US5 - Files)**: Depends on Phase 3 (needs control pattern)
- **Phase 8 (US6 - Credentials)**: Depends on Phase 3 (needs SorchaFormRenderer submit flow)
- **Phase 9 (US8 - Signing)**: Depends on Phase 3 (needs submit flow)
- **Phase 10 (US9 - Tabs)**: Depends on Phase 2 (layout renderers)
- **Phase 11 (US7 - ZKP)**: Depends on Phase 7 (file attachment pattern)
- **Phase 12 (Polish)**: Depends on all desired user stories

### User Story Independence

After Phase 3 (MVP), the following can proceed **in parallel**:
- US2 (Previous Data) + US3 (Disclosure) + US4 (Conditions) + US5 (Files) + US6 (Credentials) + US8 (Signing) + US9 (Tabs)

US7 (ZKP) depends on US5 (Files) for the attachment pattern.

### Within Each User Story

- Tests written first (where included)
- Models/services before components
- Components before integration
- Verification at checkpoint

### Parallel Opportunities

**Phase 1** — T005, T006, T007, T008 (four model files) can run in parallel
**Phase 2** — T015, T016, T017, T018 (layout + simple renderers) can run in parallel
**Phase 3** — T020, T021, T022 (tests) parallel; T023-T029 (7 control renderers) all parallel
**Phase 6+** — US2, US3, US4, US5, US6, US8, US9 can all run in parallel after Phase 3

---

## Parallel Example: Phase 3 (User Story 1)

```
# Launch all tests in parallel:
T020: FormContextTests.cs
T021: FormSchemaServiceTests.cs
T022: AutoGenerationTests.cs

# Launch all control renderers in parallel:
T023: TextLineRenderer.razor
T024: TextAreaRenderer.razor
T025: NumericRenderer.razor
T026: DateTimeRenderer.razor
T027: CheckboxRenderer.razor
T028: SelectionRenderer.razor
T029: ChoiceRenderer.razor

# Then sequential (depends on above):
T030: Auto-generation fallback
T031: Inline validation
T032: Submit flow
T033: Update ActionForm.razor
T034: Verify with sample blueprint
```

---

## Implementation Strategy

### MVP First (Phases 1-3)

1. Phase 1: Setup models, services, scaffolding (T001-T012)
2. Phase 2: Root renderer, dispatcher, layouts (T013-T019)
3. Phase 3: All 10 control types + validation + submit (T020-T034)
4. **STOP and VALIDATE**: Load simple-invoice-approval, fill form, submit
5. Deploy — participants can now interact with real workflows

### Incremental Delivery

1. Phase 1-3 → MVP: Form rendering + validation + submit
2. Phase 4 → Add previous data display (approval workflows)
3. Phase 5 → Add disclosure filtering (DAD security)
4. Phase 6 → Add conditional display + calculations (smart forms)
5. Phase 7-8 → Add files + credentials (document-heavy + regulated workflows)
6. Phase 9 → Add wallet signing (cryptographic proof)
7. Phase 10-11 → Add tabs + ZKP (complex forms + privacy)
8. Phase 12 → Polish, verify all blueprints, run tests

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable at its checkpoint
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- The 8 sample blueprints in `samples/blueprints/` are the primary verification targets
- Pin JsonSchema.Net v8.0.5 and JsonLogic v5.5.0 — do NOT upgrade to versions with maintenance fee
