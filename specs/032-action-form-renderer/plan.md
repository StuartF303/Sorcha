# Implementation Plan: Action Form Renderer

**Branch**: `032-action-form-renderer` | **Date**: 2026-02-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/032-action-form-renderer/spec.md`

## Summary

Replace the basic `ActionForm.razor` stub with a full recursive form renderer that interprets blueprint `Control` trees, validates against JSON Schema Draft 2020-12, filters fields by participant disclosure rules, supports credential gating and file attachments, evaluates conditional display rules and calculated fields, and signs submissions with the participant's wallet. The renderer aligns with JSON Forms conventions (UISchema + DataSchema, JSON Pointer scopes, rule-based conditions) to enable future portability to React/Vue/Angular.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: MudBlazor (UI components), JsonSchema.Net v8.0.5 (schema validation), JsonLogic v5.5.0 (calculations), System.Text.Json (serialization)
**Storage**: N/A (client-side rendering, no persistence)
**Testing**: xUnit + FluentAssertions + Moq (unit), bUnit (component), NUnit + Playwright (E2E)
**Target Platform**: Blazor WASM (browser)
**Project Type**: Web — Blazor component library within existing Sorcha.UI.Core
**Performance Goals**: <200ms calculated field updates, <100ms conditional show/hide, <60s for 7-field invoice form completion
**Constraints**: WASM-compatible (no server-only APIs), JsonSchema.Net and JsonLogic pinned to pre-maintenance-fee versions
**Scale/Scope**: 10 control types, 4 layout types, ~20 new components, ~6 new service/model classes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | UI component library, no new services. Dependencies flow downward (UI.Core → Blueprint.Models). |
| II. Security First | PASS | Disclosure filtering removes undisclosed fields before rendering (not CSS hidden). Wallet signing on submit. |
| III. API Documentation | PASS | No new APIs — uses existing blueprint and wallet service endpoints. |
| IV. Testing Requirements | PASS | Unit tests for FormContext, schema parsing, disclosure filtering. Component tests with bUnit. E2E tests for sample blueprints. Target >85% for new code. |
| V. Code Quality | PASS | Async/await for wallet signing, DI for services, nullable enabled, C# 13 features. |
| VI. Blueprint Standards | PASS | Form definitions remain JSON on the Action model. No C# code for blueprint construction. |
| VII. Domain-Driven Design | PASS | Uses ubiquitous language: Action (not step), Participant (not user), Disclosure (not visibility), Blueprint (not workflow). |
| VIII. Observability | N/A | Client-side component — no server-side telemetry needed. Browser console logging for development. |

**Post-Phase 1 Re-check**: All gates still pass. Model changes to `Control.cs` (adding `Rule`, `Options`) are additive. Deprecation of `Conditions` property is backward-compatible.

## Project Structure

### Documentation (this feature)

```text
specs/032-action-form-renderer/
├── plan.md              # This file
├── research.md          # Phase 0: JSON Forms alignment decisions
├── data-model.md        # Phase 1: Entity definitions and state model
├── quickstart.md        # Phase 1: Usage guide and file layout
├── contracts/
│   └── renderer-api.md  # Phase 1: Component API and hierarchy
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/Common/Sorcha.Blueprint.Models/
├── Control.cs                          # MODIFY: add Rule, Options; deprecate Conditions
├── FormRule.cs                         # NEW: JSON Forms rule (effect + condition)
├── SchemaBasedCondition.cs             # NEW: scope + schema condition
└── RuleEffect.cs                       # NEW: SHOW/HIDE/ENABLE/DISABLE enum

src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Components/
│   ├── Forms/                          # NEW: form renderer component library
│   │   ├── SorchaFormRenderer.razor    # Root: orchestrates context, panels, submission
│   │   ├── ControlDispatcher.razor     # Recursive: disclosure check → rule eval → type dispatch
│   │   ├── Controls/                   # Input control renderers (10 types)
│   │   │   ├── TextLineRenderer.razor
│   │   │   ├── TextAreaRenderer.razor
│   │   │   ├── NumericRenderer.razor
│   │   │   ├── DateTimeRenderer.razor
│   │   │   ├── CheckboxRenderer.razor
│   │   │   ├── SelectionRenderer.razor
│   │   │   ├── ChoiceRenderer.razor
│   │   │   ├── FileRenderer.razor
│   │   │   ├── LabelRenderer.razor
│   │   │   └── UnsupportedControlRenderer.razor
│   │   ├── Layouts/                    # Layout renderers (4 types)
│   │   │   ├── VerticalLayoutRenderer.razor
│   │   │   ├── HorizontalLayoutRenderer.razor
│   │   │   ├── GroupRenderer.razor
│   │   │   └── CategorizationRenderer.razor
│   │   └── Panels/                     # Supporting panels
│   │       ├── PreviousDataPanel.razor
│   │       ├── CredentialGatePanel.razor
│   │       └── CalculatedFieldsPanel.razor
│   └── Workflows/
│       └── ActionForm.razor            # MODIFY: replace stub with SorchaFormRenderer usage
├── Models/
│   └── Forms/                          # NEW: form state models
│       ├── FormContext.cs
│       ├── FormSubmission.cs
│       ├── FileAttachmentInfo.cs
│       └── ProofAttachment.cs
└── Services/
    └── Forms/                          # NEW: form services
        ├── IFormSchemaService.cs
        ├── FormSchemaService.cs        # Schema merge, scope resolution, validation, auto-gen
        ├── IFormSigningService.cs
        └── FormSigningService.cs       # SHA-256 hash + wallet sign integration

src/Core/Sorcha.Blueprint.Fluent/
└── FormBuilder.cs                      # MODIFY: support Rule on ControlBuilder

tests/Sorcha.Blueprint.Models.Tests/
└── FormRuleTests.cs                    # NEW: rule serialization, effect enum

tests/Sorcha.UI.Core.Tests/             # NEW or extend existing
└── Components/Forms/
    ├── FormContextTests.cs             # Unit: data binding, validation, calculated fields
    ├── ControlDispatcherTests.cs       # Unit: type dispatch, disclosure filtering, rule eval
    ├── FormSchemaServiceTests.cs       # Unit: schema merge, scope resolution, auto-gen
    ├── DisclosureFilterTests.cs        # Unit: pointer matching, wildcard, nested
    ├── CalculatedFieldTests.cs         # Unit: JSON Logic evaluation
    └── AutoGenerationTests.cs          # Unit: schema-to-form fallback

samples/blueprints/
└── (9 existing files)                  # VERIFY: all render correctly with new renderer
```

**Structure Decision**: This feature adds components and models within the existing `Sorcha.UI.Core` project. No new projects are created — the form renderer is a component library within UI.Core, following the established pattern for Workflows, Designer, and Registers components. Model changes go in `Sorcha.Blueprint.Models` (existing). Tests go in existing test projects.
