# Data Model: Action Form Renderer

**Feature**: 032-action-form-renderer
**Date**: 2026-02-12

## Entity Relationship Overview

```
Action (existing)
├── Form: Control (existing — UISchema)
│   ├── Type (ControlTypes enum)
│   ├── Scope (JSON Pointer → binds to DataSchema field)
│   ├── Rule (NEW — JSON Forms rule for conditional display)
│   └── Elements: List<Control> (recursive children)
├── DataSchemas: List<JsonDocument> (existing — JSON Schema)
├── Disclosures: List<Disclosure> (existing — field visibility)
├── Calculations: Dict<string, JsonNode> (existing — JSON Logic)
├── CredentialRequirements: List<CredentialRequirement> (existing)
└── PreviousData: JsonDocument (existing — prior action data)

FormContext (NEW — renderer state)
├── FormData: Dict<string, object> (live user input)
├── DataSchema: JsonDocument (merged from DataSchemas)
├── DisclosureFilter: HashSet<string> (allowed scopes)
├── ValidationErrors: Dict<string, List<string>>
├── PreviousData: Dict<string, object> (read-only)
├── CalculatedValues: Dict<string, object> (derived)
├── CredentialPresentations: List<CredentialPresentation>
├── FileAttachments: List<FileAttachment>
└── SigningWalletAddress: string

FormSubmission (NEW — output of form)
├── FormData: Dict<string, object>
├── CalculatedValues: Dict<string, object>
├── Signature: byte[]
├── SigningWalletAddress: string
├── CredentialPresentations: List<CredentialPresentation>
├── FileAttachments: List<FileAttachment>
└── ProofAttachments: List<ProofAttachment>
```

## Model Changes to Existing Entities

### Control (modify existing)

**File**: `src/Common/Sorcha.Blueprint.Models/Control.cs`

| Property | Change | Type | Description |
|----------|--------|------|-------------|
| `Rule` | ADD | `FormRule?` | JSON Forms rule (effect + condition) for conditional display |
| `Conditions` | DEPRECATE | `List<JsonNode>` | Mark `[Obsolete]`, keep for backward compat, unused in production |
| `Options` | ADD | `JsonDocument?` | Extensible options bag (JSON Forms convention for renderer hints) |

### FormRule (new entity)

Follows JSON Forms rule specification.

| Property | Type | Description |
|----------|------|-------------|
| `Effect` | `RuleEffect` | SHOW, HIDE, ENABLE, or DISABLE |
| `Condition` | `SchemaBasedCondition` | When this condition is true, the effect is applied |

### SchemaBasedCondition (new entity)

| Property | Type | Description |
|----------|------|-------------|
| `Scope` | `string` | JSON Pointer to the data field being evaluated |
| `Schema` | `JsonNode` | JSON Schema that the scoped data must match |

### RuleEffect (new enum)

| Value | Behavior |
|-------|----------|
| `SHOW` | Control is visible when condition is true, hidden otherwise |
| `HIDE` | Control is hidden when condition is true, visible otherwise |
| `ENABLE` | Control is enabled when condition is true, disabled otherwise |
| `DISABLE` | Control is disabled when condition is true, enabled otherwise |

## New UI Entities

### FormContext (renderer state object)

Passed as `CascadingValue` through the component tree.

| Property | Type | Description |
|----------|------|-------------|
| `FormData` | `Dictionary<string, object>` | Current form values keyed by JSON Pointer scope |
| `DataSchema` | `JsonDocument` | Merged JSON Schema from Action.DataSchemas |
| `DisclosureFilter` | `HashSet<string>` | Allowed JSON Pointer paths for current participant |
| `ValidationErrors` | `Dictionary<string, List<string>>` | Scope → list of error messages |
| `PreviousData` | `Dictionary<string, object>` | Read-only data from prior actions |
| `CalculatedValues` | `Dictionary<string, object>` | JSON Logic computed values |
| `CredentialPresentations` | `List<CredentialPresentation>` | Selected credentials |
| `FileAttachments` | `List<FileAttachmentInfo>` | Uploaded files |
| `ProofAttachments` | `List<ProofAttachment>` | ZKP proofs |
| `IsReadOnly` | `bool` | Global read-only mode |
| `SigningWalletAddress` | `string` | Active wallet for signing |

**Methods**:
- `SetValue(scope, value)` — Updates form data, triggers recalculation and revalidation
- `GetValue<T>(scope)` — Retrieves typed value by scope
- `Validate()` — Runs full schema validation, populates ValidationErrors
- `ValidateField(scope)` — Validates single field on blur
- `IsDisclosed(scope)` — Checks if scope is in disclosure filter
- `GetSchemaForScope(scope)` — Extracts sub-schema for a specific field
- `OnDataChanged` — Event invoked when any value changes

### FormSubmission (form output)

Returned when the form is submitted. Maps to `ActionSubmissionRequest`.

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `Dictionary<string, object>` | User-entered form values |
| `CalculatedValues` | `Dictionary<string, object>` | Engine-calculated values |
| `Signature` | `byte[]` | Wallet signature over data hash |
| `SigningWalletAddress` | `string` | Address of signing wallet |
| `CredentialPresentations` | `List<CredentialPresentation>` | Presented credentials |
| `FileAttachments` | `List<FileAttachment>` | Uploaded files (base64) |
| `ProofAttachments` | `List<ProofAttachment>` | ZKP proofs |

### FileAttachmentInfo (UI file state)

| Property | Type | Description |
|----------|------|-------------|
| `FileName` | `string` | Original file name |
| `ContentType` | `string` | MIME type |
| `Size` | `long` | File size in bytes |
| `Content` | `byte[]` | File content |

### ProofAttachment (ZKP proof)

| Property | Type | Description |
|----------|------|-------------|
| `ClaimDescription` | `string` | Human-readable description of what is proven |
| `ProofType` | `string` | Proof system identifier |
| `ProofData` | `byte[]` | Serialized proof |
| `PublicInputs` | `Dictionary<string, object>` | Public inputs to the proof |

## Scope Resolution

JSON Pointer scopes bind controls to data fields:

| Scope | Data Path | Schema Path |
|-------|-----------|-------------|
| `/invoiceNumber` | `FormData["invoiceNumber"]` | `schema.properties.invoiceNumber` |
| `/address/city` | `FormData["address"]["city"]` | `schema.properties.address.properties.city` |
| `/*` | All fields (disclosure wildcard) | N/A |

## Validation Rules

| Rule | Source | Applied |
|------|--------|---------|
| Required fields | `dataSchemas[].required` | On submit + on blur |
| Type constraints | `dataSchemas[].properties.*.type` | On value change |
| String constraints | `minLength`, `maxLength`, `pattern` | On blur |
| Number constraints | `minimum`, `maximum`, `multipleOf` | On blur |
| Enum constraints | `enum` | Populates Selection/Choice options |
| Format constraints | `format` (date, date-time, email, uri) | On blur |
| File constraints | `maxFileSize`, `acceptedTypes` (via options) | On file select |
| Credential match | `CredentialRequirement.Type`, `AcceptedIssuers` | On credential select |

## State Transitions

```
Form States:
  Loading → Ready → Editing → Validating → Signing → Submitting → Submitted
                  ↑                ↓
                  └── InvalidData ──┘

Control States:
  Hidden (rule=HIDE or not disclosed)
  Disabled (rule=DISABLE)
  ReadOnly (previousData or calculated)
  Editable (default for input controls)
  Error (validation failed)
```
