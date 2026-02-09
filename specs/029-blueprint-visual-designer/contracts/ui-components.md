# UI Component Contracts: Blueprint Visual Designer

**Feature**: 029-blueprint-visual-designer
**Date**: 2026-02-09

## Overview

This feature is UI-only — no new backend API endpoints are required. All data comes from existing Blueprint Service and Template Service endpoints. The contracts below define the new Blazor component interfaces.

## New Components

### BlueprintViewerDiagram

**Purpose**: Readonly visual diagram renderer for any blueprint JSON.

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/BlueprintViewerDiagram.razor`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Blueprint` | `Blueprint` | Yes | The blueprint to render |
| `Height` | `string` | No | CSS height (default: "400px") |
| `OnActionClicked` | `EventCallback<Action>` | No | Callback when action node is clicked |

**Behaviour**:
- On parameter set: runs auto-layout algorithm on Blueprint
- Creates `BlazorDiagram` with locked nodes (readonly)
- Registers `ReadOnlyActionNodeWidget` for rendering
- Supports zoom/scroll via mouse
- Prevents all edit interactions (no node add/delete/move, no link creation)

---

### ReadOnlyActionNodeWidget

**Purpose**: Simplified action node rendering without edit toolbar.

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/ReadOnlyActionNodeWidget.razor`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Node` | `ActionNodeModel` | Yes | The action node to render |

**Behaviour**:
- Renders action title, ID chip, and sender label
- Shows starting action badge (green) and terminal indicator (red)
- Shows cycle indicator on back-edge targets
- Colour-coded border matching sender participant colour
- No edit buttons (Add Participant, Add Condition, Properties)
- Click triggers `OnActionClicked` via diagram selection

---

### ActionDetailPopover

**Purpose**: Read-only detail panel shown on action node click.

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/ActionDetailPopover.razor`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Action` | `Action` | Yes | The action to display details for |
| `Participants` | `List<Participant>` | Yes | Blueprint participants for name resolution |
| `IsOpen` | `bool` | Yes | Controls visibility |
| `OnClose` | `EventCallback` | No | Callback when popover is dismissed |

**Sections Displayed**:
1. Description
2. Sender (participant name + ID)
3. Data Schemas (count + expandable JSON tree)
4. Disclosures (list of participant → data pointer mappings)
5. Routes (list of route ID, condition, next actions)
6. Calculations (if any)
7. Rejection Config (if any)

---

### BlueprintViewerDialog

**Purpose**: Full-screen dialog wrapping BlueprintViewerDiagram for the Blueprints page.

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Designer/BlueprintViewerDialog.razor`

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Blueprint` | `Blueprint` | Yes | Full blueprint to display |
| `Title` | `string` | No | Dialog title (default: blueprint title) |

**Behaviour**:
- MudDialog with fullscreen option
- Contains BlueprintViewerDiagram with Height="600px"
- Participant legend at top
- Close button

---

## Modified Components

### Templates.razor (Modified)

**Changes**:
- Add `BlueprintViewerDiagram` to the template detail panel (right side)
- Below description and parameters, above "Use Template" button
- For simple templates: extract blueprint from template JSON directly
- For parameterised templates: evaluate with default parameters to get preview blueprint
- Loading state while evaluating template

**New Data Flow**:
```
User selects template
  → LoadTemplatePreview(template)
    → For simple: parse template.template as Blueprint
    → For parameterised: call TemplateApi.EvaluateTemplateAsync(id, defaultParameters)
    → Set _previewBlueprint
  → BlueprintViewerDiagram renders _previewBlueprint
```

---

### Blueprints.razor (Modified)

**Changes**:
- Add "View" icon button to each blueprint card's actions row
- On click: fetch full blueprint via new `GetBlueprintDetailAsync(id)`
- Open `BlueprintViewerDialog` with the full blueprint

**New Data Flow**:
```
User clicks "View" on blueprint card
  → ViewBlueprint(blueprint)
    → call BlueprintApi.GetBlueprintDetailAsync(blueprint.Id)
    → Open BlueprintViewerDialog with full Blueprint object
```

---

## Modified Services

### IBlueprintApiService (Extended)

**New Method**:
```
GetBlueprintDetailAsync(id, cancellationToken) → Blueprint?
```
- Calls `GET /api/blueprints/{id}` (same endpoint)
- Deserialises full response to `Blueprint` domain model instead of `BlueprintListItemViewModel`

---

## Existing Endpoints Used (No Changes)

| Method | Endpoint | Used By |
|--------|----------|---------|
| GET | `/api/templates` | Templates.razor (list) |
| GET | `/api/templates/{id}` | Templates.razor (detail) |
| POST | `/api/templates/{id}/evaluate` | Templates.razor (preview) |
| GET | `/api/blueprints` | Blueprints.razor (list) |
| GET | `/api/blueprints/{id}` | Blueprints.razor (detail for viewer) |
| POST | `/api/blueprints/{id}/publish` | Blueprints.razor (publish) |
| POST | `/api/blueprints` | Designer.razor (save) |

---

## Layout Service

### BlueprintLayoutService

**Purpose**: Computes node positions for auto-layout of a blueprint's action graph.

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/BlueprintLayoutService.cs`

**Interface**:
```
ComputeLayout(blueprint) → DiagramLayout
```

**Algorithm** (simplified Sugiyama):
1. Build adjacency graph from Action.routes
2. Find starting actions (isStartingAction == true)
3. BFS from starting actions to assign layer depths
4. Handle cycles: detect back-edges, assign target to earlier layer
5. Order nodes within each layer to minimise crossings
6. Assign X, Y positions based on layer and order
7. Generate edges with types (Default, Conditional, BackEdge, Terminal)

**Constants**:
- Node width: 280px
- Node height: 120px
- Vertical spacing: 180px
- Horizontal spacing: 320px
- Start X offset: 50px
- Start Y offset: 50px
