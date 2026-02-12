# Research: Action Form Renderer

**Feature**: 032-action-form-renderer
**Date**: 2026-02-12

## Decision 1: Control Type Vocabulary — Keep Sorcha Enums, Map at Render Time

**Decision**: Keep the existing `ControlTypes` and `LayoutTypes` enums in the model layer. Add a JSON Forms compatibility mapping at the renderer level rather than breaking the model.

**Rationale**:
- Sorcha's explicit control types (`TextLine`, `TextArea`, `Numeric`, `DateTime`, `File`, `Choice`, `Checkbox`, `Selection`) are more prescriptive than JSON Forms' single `"Control"` type where rendering is inferred from the data schema. This is a feature, not a bug — blueprint authors explicitly declare what UI they want.
- JSON Forms' approach of inferring control type from schema (e.g., `string` → text input, `boolean` → checkbox) is elegant but loses intentionality. A blueprint author choosing `TextArea` vs `TextLine` for a string field is a deliberate UX decision.
- Breaking the model would require migrating 9 sample blueprints, the FormBuilder fluent API, all engine validation code, and any external blueprints.
- JSON Forms compatibility is achieved by mapping at render time: the renderer accepts both Sorcha-native and JSON Forms vocabulary.

**Alternatives considered**:
- Replace enums with JSON Forms string vocabulary — rejected due to high migration cost and loss of explicit control type semantics.
- Dual vocabulary with custom JSON converter — rejected as over-complex for the current need.

## Decision 2: Conditions — Adopt JSON Forms `rule` Model, Deprecate `conditions`

**Decision**: Add a `Rule` property (JSON Forms format: effect + SchemaBasedCondition) to the `Control` model. Deprecate the existing `Conditions` property (JSON Logic array).

**Rationale**:
- The existing `Control.Conditions` property is **unused in production** — no sample blueprints, no engine code, and only one test references it.
- JSON Forms rules are more expressive: they support SHOW, HIDE, ENABLE, and DISABLE effects.
- JSON Forms rules use JSON Schema for condition evaluation, which Sorcha already has (`JsonSchema.Net`). This avoids needing JSON Logic evaluation on the client for form display logic.
- JSON Logic remains the engine for routing (`Action.Condition`) and calculations (`Action.Calculations`) — those are server-side concerns.

**Alternatives considered**:
- Keep JSON Logic for form conditions — rejected because it requires client-side JSON Logic evaluation and doesn't support ENABLE/DISABLE effects.
- Support both simultaneously — rejected as unnecessary complexity given `conditions` is unused.

## Decision 3: Scope Format — Keep Bare JSON Pointers, Normalize Internally

**Decision**: Keep the existing bare JSON Pointer format (`/invoiceNumber`) in blueprints. Add internal normalization to `#/properties/invoiceNumber` when interfacing with JSON Schema validation.

**Rationale**:
- Bare pointers are simpler for blueprint authors: `/amount` vs `#/properties/amount`.
- All 9 sample blueprints and the fluent API use bare pointers.
- The `#/properties/` prefix is a JSON Schema reference format, not a user-facing concern.
- A normalization utility at the renderer boundary handles the translation transparently.
- If a future JSON Forms renderer needs the prefix format, the normalizer works bidirectionally.

**Alternatives considered**:
- Adopt `#/properties/` everywhere — rejected due to migration cost and reduced readability for blueprint authors.
- Accept both formats with auto-detection — this is effectively what the normalizer does.

## Decision 4: UISchema/DataSchema Separation — Already Compatible

**Decision**: No structural changes needed. Sorcha's existing `Action.Form` (UISchema) and `Action.DataSchemas` (DataSchema) already follow the JSON Forms dual-schema pattern.

**Rationale**:
- `Action.Form` maps to JSON Forms `uischema` parameter.
- `Action.DataSchemas[0]` maps to JSON Forms `schema` parameter.
- The renderer receives both and passes them to the rendering pipeline independently.
- When `Form` is null, auto-generate a VerticalLayout from `DataSchemas` (JSON Forms default behavior).

## Decision 5: JSON Logic Library — Keep `JsonLogic` v5.5.0 (json-everything)

**Decision**: Continue using `JsonLogic` v5.5.0 from the json-everything suite. Pin the version.

**Rationale**:
- Already in use across Blueprint Engine, Fluent, Models, and UI Web Client.
- Confirmed WASM-compatible (json-everything's own learning site runs in Blazor WASM).
- Built on `System.Text.Json` — consistent with the rest of Sorcha.
- Version 6.0.0+ introduces a maintenance fee EULA. Version 5.5.0 supports .NET 10 without the fee.

**Alternatives considered**:
- `JsonLogic.Net` v1.1.11 — rejected, uses Newtonsoft.Json, last updated 2021.
- Custom implementation — rejected, unnecessary when existing library works.

## Decision 6: JSON Schema Validation — Keep `JsonSchema.Net` v8.0.5

**Decision**: Continue using `JsonSchema.Net` v8.0.5 for both server-side and client-side (WASM) validation. Pin the version.

**Rationale**:
- Already in use across 6 projects including UI Web Client.
- Supports JSON Schema Draft 2020-12 (required by Sorcha blueprints).
- Confirmed WASM-compatible.
- Version 9.x+ introduces maintenance fee. v8.0.5 supports .NET 10.

**Alternatives considered**:
- `Corvus.JsonSchema` — high-performance source-gen approach, but significant migration effort. Track for future.
- `NJsonSchema` — uses Newtonsoft.Json, only supports Draft 4. Rejected.

## Decision 7: Renderer Architecture — Recursive Component Tree

**Decision**: Build the renderer as a recursive Blazor component tree. A root `SorchaFormRenderer` component dispatches to specialized renderers per control type.

**Rationale**:
- The `Control` model is already a tree (Elements contain child Controls).
- Blazor's component model naturally supports recursive rendering via `RenderFragment`.
- Each control type gets its own component (e.g., `TextLineRenderer`, `NumericRenderer`, `SelectionRenderer`).
- Layout components (`VerticalLayoutRenderer`, `HorizontalLayoutRenderer`, `GroupRenderer`, `CategorizationRenderer`) arrange their children.
- A `FormContext` cascading value holds shared state: form data dictionary, validation errors, disclosure filter, schema reference.

**Alternatives considered**:
- Single monolithic switch-case component — rejected, unmaintainable for 10+ control types.
- JS interop with existing JSON Forms React renderer — rejected, adds JS dependency and loses MudBlazor integration.

## Decision 8: Form State Management — Centralized FormContext

**Decision**: Use a centralized `FormContext` object passed as a `CascadingValue` to all renderer components.

**Rationale**:
- Multiple controls may bind to the same data (synchronized fields).
- Calculated fields need access to all form values.
- Validation needs the complete data dictionary + schema.
- Disclosure filtering needs the participant's allowed pointers.
- A single context avoids prop-drilling through deep component trees.

**Key properties of FormContext**:
- `FormData`: Dictionary of current values keyed by JSON Pointer scope.
- `DataSchema`: The JSON Schema for validation.
- `DisclosureFilter`: Set of allowed JSON Pointer paths for the current participant.
- `ValidationErrors`: Dictionary of scope → error messages.
- `PreviousData`: Read-only data from prior actions.
- `Calculations`: Dictionary of calculated values.
- `IsReadOnly`: Global read-only flag.
- `OnDataChanged`: Event for notifying dependents of value changes.

## Decision 9: Wallet Signing Integration

**Decision**: Use the existing `IWalletApiService.SignDataAsync()` method. The form serializes data to JSON, hashes with SHA-256, and sends to the wallet service for signing before submission.

**Rationale**:
- `IWalletApiService` already exists in `Sorcha.UI.Core/Services/Wallet/` with `SignDataAsync(address, SignTransactionRequest)`.
- The current `ActionForm.razor` does NOT sign — it just submits raw data. The new renderer must add signing.
- The active wallet address comes from the UI session state (already tracked).

## Decision 10: Credential Presentation Flow

**Decision**: When an action has `CredentialRequirements`, the form fetches the participant's credentials from the wallet service, filters by requirement type, and presents a selection UI. Selected credentials are included as `CredentialPresentation` objects in the submission.

**Rationale**:
- `CredentialPresentation` model already exists with `CredentialId`, `DisclosedClaims`, `RawPresentation`, `KeyBindingProof`.
- `CredentialRequirement` model already exists with `Type`, `AcceptedIssuers`, `RequiredClaims`, `RevocationCheckPolicy`.
- The blueprint service endpoint already accepts `CredentialPresentations` in `ActionSubmissionRequest`.
- UI needs to: fetch credentials → filter by type → show selection → validate against requirement → include in submission.
