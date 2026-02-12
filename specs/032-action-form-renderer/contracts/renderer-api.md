# Renderer Component API Contract

**Feature**: 032-action-form-renderer
**Date**: 2026-02-12

## Component Hierarchy

```
SorchaFormRenderer (root)
├── FormContext (CascadingValue)
├── PreviousDataPanel (read-only section)
├── CredentialGatePanel (credential requirements)
├── ControlDispatcher (recursive)
│   ├── VerticalLayoutRenderer
│   ├── HorizontalLayoutRenderer
│   ├── GroupRenderer
│   ├── CategorizationRenderer
│   ├── LabelRenderer
│   ├── TextLineRenderer
│   ├── TextAreaRenderer
│   ├── NumericRenderer
│   ├── DateTimeRenderer
│   ├── FileRenderer
│   ├── ChoiceRenderer
│   ├── CheckboxRenderer
│   ├── SelectionRenderer
│   └── UnsupportedControlRenderer (fallback)
├── CalculatedFieldsPanel (derived values)
└── FormActions (Submit / Reject / Cancel)
```

## Root Component: SorchaFormRenderer

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Action` | `Action` | Yes | The blueprint action containing Form, DataSchemas, Disclosures, Calculations, CredentialRequirements |
| `PreviousData` | `JsonDocument?` | No | Data from prior workflow actions |
| `ParticipantAddress` | `string` | Yes | Current participant's wallet address or ID (for disclosure filtering) |
| `SigningWalletAddress` | `string` | Yes | Active wallet address for signing |
| `OnSubmit` | `EventCallback<FormSubmission>` | Yes | Callback when form is submitted with signed data |
| `OnReject` | `EventCallback<string>` | No | Callback when action is rejected (with reason) |
| `OnCancel` | `EventCallback` | No | Callback when form is cancelled |
| `IsReadOnly` | `bool` | No | Render entire form as read-only (default: false) |

### Behavior

1. On initialization:
   - Merge `Action.DataSchemas` into a single schema
   - Build disclosure filter from `Action.Disclosures` for `ParticipantAddress`
   - Initialize `FormContext` with empty data, schema, and filter
   - If `PreviousData` provided, parse into read-only values
   - If `Action.Form` is null, auto-generate from DataSchemas

2. On submit:
   - Run full validation against schema
   - If valid and all credential requirements met:
     - Serialize form data to canonical JSON
     - Hash with SHA-256
     - Call wallet service to sign hash
     - Package into `FormSubmission` and invoke `OnSubmit`
   - If invalid, show errors and activate first error tab (if categorized)

## ControlDispatcher Component

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Control` | `Control` | Yes | The control definition to render |
| `Depth` | `int` | No | Nesting depth for indentation (default: 0) |

### Behavior

1. Check if control's scope is in disclosure filter (if scope is set)
   - If not disclosed → render nothing (return empty)
2. Evaluate rule (if `Rule` is set)
   - If HIDE and condition true → render nothing
   - If SHOW and condition false → render nothing
   - If DISABLE and condition true → render as disabled
   - If ENABLE and condition false → render as disabled
3. Dispatch to type-specific renderer based on `ControlType`
4. For Layout types, recursively render `Elements`

## Input Control Renderers

All input controls share this interface:

### Common Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Control` | `Control` | Yes | Control definition (title, scope, properties, options) |
| `IsDisabled` | `bool` | No | Disabled state from rule evaluation |

### Common Behavior

1. Read current value from `FormContext.GetValue<T>(scope)`
2. On value change → `FormContext.SetValue(scope, value)`
3. On blur → `FormContext.ValidateField(scope)` → display errors
4. Read schema constraints from `FormContext.GetSchemaForScope(scope)` for:
   - Placeholder text (from `description`)
   - Required indicator (from `required` array)
   - Validation rules (min/max, pattern, etc.)
   - Enum values (for Selection/Choice)

### Type-Specific Rendering

| Renderer | MudBlazor Component | Schema Mapping |
|----------|-------------------|----------------|
| `TextLineRenderer` | `MudTextField<string>` | `type: "string"` |
| `TextAreaRenderer` | `MudTextField<string>` Lines=5 | `type: "string"` (multi) |
| `NumericRenderer` | `MudNumericField<decimal>` | `type: "number"` or `"integer"` |
| `DateTimeRenderer` | `MudDatePicker` / `MudTimePicker` | `format: "date"` or `"date-time"` |
| `CheckboxRenderer` | `MudSwitch<bool>` | `type: "boolean"` |
| `SelectionRenderer` | `MudSelect<string>` | `enum: [...]` |
| `ChoiceRenderer` | `MudRadioGroup<string>` | `enum: [...]` |
| `FileRenderer` | `MudFileUpload` | Custom (via options) |
| `LabelRenderer` | `MudText` (read-only) | N/A |

## Layout Renderers

| Renderer | MudBlazor Component | Behavior |
|----------|-------------------|----------|
| `VerticalLayoutRenderer` | `MudStack Spacing=3` | Stack children vertically |
| `HorizontalLayoutRenderer` | `MudStack Row=true Spacing=3` | Arrange children side-by-side, equal width |
| `GroupRenderer` | `MudPaper` + `MudText` title | Bordered section with header |
| `CategorizationRenderer` | `MudTabs` | Each child element becomes a tab |

## Credential Gate Panel

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Requirements` | `IEnumerable<CredentialRequirement>` | Yes | Credential requirements from the action |
| `WalletAddress` | `string` | Yes | Participant's wallet address to fetch credentials |

### Behavior

1. For each requirement:
   - Fetch participant's credentials from wallet service
   - Filter by requirement `Type` and `AcceptedIssuers`
   - Display requirement description and selection dropdown
   - On selection, validate claims match `RequiredClaims`
   - Show success/failure indicator
2. All requirements must be satisfied before form submission is enabled

## Calculated Fields Panel

### Behavior

1. Subscribe to `FormContext.OnDataChanged`
2. On any data change, re-evaluate all `Action.Calculations` using JSON Logic
3. Display each calculated field as a read-only `MudTextField` with a calculator icon
4. Update `FormContext.CalculatedValues`

## Previous Data Panel

### Behavior

1. Parse `PreviousData` JSON into displayable key-value pairs
2. Filter by disclosure rules (only show disclosed previous fields)
3. Render as read-only fields using `MudTextField` with `ReadOnly=true`
4. Group under a collapsible header "Previous Submission Data"
