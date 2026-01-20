# Implementation Plan: Blueprint Designer Completion

**Branch**: `001-designer-completion` | **Date**: 2026-01-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-designer-completion/spec.md`

## Summary

Complete the remaining Blueprint Designer features (BD-022 to BD-025): Participant Editor for visual participant management, Condition Editor for visual JSON Logic building, Export/Import for blueprint file exchange, and Backend Integration for server-side persistence replacing LocalStorage.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: MudBlazor 8.15.0, Z.Blazor.Diagrams 3.0.4, Blazored.LocalStorage, YamlDotNet
**Storage**: LocalStorage (migration to Blueprint Service), Blueprint Service API
**Testing**: xUnit, FluentAssertions, bUnit for component testing
**Target Platform**: Blazor WebAssembly (browser)
**Project Type**: Web application - Sorcha.Admin project
**Performance Goals**: Export <3s for 50 actions, import validation <5s for 1MB files
**Constraints**: Offline-capable save queue, YAML serialization compatibility
**Scale/Scope**: Blueprints with up to 50 actions, complex nested conditions

## Existing Infrastructure

The following already exists in Sorcha.Admin:

| Component | Location | Status |
|-----------|----------|--------|
| Designer.razor | Pages/ | ✅ Visual canvas with diagram |
| PropertiesPanel.razor | Components/ | ✅ Edit blueprint/action properties |
| ActionNodeWidget.razor | Components/ | ✅ Action nodes in diagram |
| BlueprintJsonView.razor | Components/ | ✅ JSON view toggle |
| LoadBlueprintDialog.razor | Components/ | ✅ Load from LocalStorage |
| BlueprintPropertiesDialog.razor | Components/ | ✅ Edit blueprint metadata |
| Blueprints.razor | Pages/ | ✅ Blueprint library (LocalStorage) |

**Stub Buttons (to be implemented)**:
- "Add Participant" → Shows "Coming soon!" snackbar
- "Add Condition" → Shows "Coming soon!" snackbar

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ Pass | UI calls Blueprint Service API for persistence |
| II. Security First | ✅ Pass | Input validation on import, no secrets |
| III. API Documentation | ✅ Pass | Blueprint Service APIs documented |
| IV. Testing Requirements | ✅ Pass | bUnit tests for new dialogs |
| V. Code Quality | ✅ Pass | Follows existing Admin patterns |
| VI. Blueprint Creation Standards | ✅ Pass | JSON/YAML export, Fluent API only for programmatic scenarios |
| VII. Domain-Driven Design | ✅ Pass | Uses Blueprint, Action, Participant terminology |
| VIII. Observability | ✅ Pass | Logging via existing infrastructure |

**Gate Result**: PASS - No violations

## Project Structure

### Documentation (this feature)

```text
specs/001-designer-completion/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/Apps/Sorcha.Admin/
├── Components/
│   ├── Designer/                        # NEW: Designer-specific components
│   │   ├── ParticipantEditor.razor      # NEW: BD-022 - Visual participant management
│   │   ├── ParticipantList.razor        # NEW: List of participants in blueprint
│   │   ├── ConditionEditor.razor        # NEW: BD-023 - Visual JSON Logic builder
│   │   ├── ConditionClause.razor        # NEW: Single condition clause
│   │   ├── CalculationEditor.razor      # NEW: Calculated field expressions
│   │   ├── ExportDialog.razor           # NEW: BD-024 - Export format selection
│   │   └── ImportDialog.razor           # NEW: Import with validation
│   ├── PropertiesPanel.razor            # EXTEND: Add participant/condition buttons
│   └── BlueprintJsonView.razor          # EXTEND: Export button
├── Services/
│   ├── IBlueprintStorageService.cs      # NEW: BD-025 - Storage abstraction
│   ├── BlueprintStorageService.cs       # NEW: Server + LocalStorage hybrid
│   ├── IOfflineSyncService.cs           # NEW: Offline queue management
│   ├── OfflineSyncService.cs            # NEW: Sync queue implementation
│   └── BlueprintSerializationService.cs # NEW: JSON/YAML conversion
├── Models/
│   ├── ParticipantModel.cs              # NEW: UI model for participant editing
│   ├── ConditionModel.cs                # NEW: UI model for condition building
│   └── SyncQueueItem.cs                 # NEW: Offline sync queue item
├── Pages/
│   ├── Designer.razor                   # EXTEND: Wire up new dialogs
│   └── Blueprints.razor                 # EXTEND: Server-side loading
└── Sorcha.Admin.Client/
    └── wwwroot/
        └── sample-blueprints/           # NEW: Example blueprints for import

tests/Sorcha.Admin.Tests/
└── Components/Designer/
    ├── ParticipantEditorTests.cs        # NEW
    ├── ConditionEditorTests.cs          # NEW
    ├── ExportImportTests.cs             # NEW
    └── BlueprintStorageServiceTests.cs  # NEW
```

**Structure Decision**: New components under Components/Designer/ subdirectory for organization. Services for storage abstraction. Extends existing Designer.razor and PropertiesPanel.razor.

## Complexity Tracking

> No violations requiring justification - follows existing patterns.
