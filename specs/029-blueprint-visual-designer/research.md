# Research: Blueprint Visual Designer

**Feature**: 029-blueprint-visual-designer
**Date**: 2026-02-09

## Decision 1: Diagram Library for Readonly Viewer

**Decision**: Reuse Z.Blazor.Diagrams v3.0.4 (already integrated in Designer.razor)

**Rationale**: The library is already in the project, has custom node registration (`RegisterComponent<ActionNodeModel, ActionNodeWidget>()`), and supports configuration for read-only use via `node.Locked = true` + disabling link creation. Creating a new `BlueprintViewer` component that wraps `BlazorDiagram` with locked nodes avoids introducing a second diagram dependency.

**Alternatives Considered**:
- **Custom SVG rendering**: More control but requires implementing pan/zoom/layout from scratch. High effort, no existing node widgets to reuse.
- **Mermaid.js via JS interop**: Good for simple flowcharts but lacks interactive node detail on click, limited styling, and adds a JS dependency to WASM app.
- **Blazor.Diagrams fork/wrapper**: Considered a separate component library but unnecessary — the existing library API supports readonly mode through configuration.

## Decision 2: Readonly Mode Implementation

**Decision**: Create a new `BlueprintViewerDiagram` Razor component that creates its own `BlazorDiagram` instance with locked nodes and a new `ReadOnlyActionNodeWidget` that omits edit toolbar buttons.

**Rationale**: The existing `Designer.razor` is 850 lines with editing logic, save/load, LocalStorage, Properties Panel, and toolbar. Adding conditional readonly branches would make it fragile. A separate viewer component is cleaner: it accepts a `Blueprint` parameter, creates the diagram, auto-lays out nodes, and renders with locked interaction.

**Key Configuration**:
- `BlazorDiagramOptions { AllowMultiSelection = false }`
- `options.Zoom.Enabled = true` (keep zoom for large blueprints)
- `options.Links.DefaultColor = "#666666"`
- All nodes: `node.Locked = true` (prevent dragging)
- Register `ReadOnlyActionNodeWidget` instead of `ActionNodeWidget`
- No toolbar, no Properties Panel (use tooltip/popover for details)

**Alternatives Considered**:
- **Conditional `isReadOnly` flag in Designer.razor**: Simpler initially but increases complexity of an already large component. Editing and viewing have fundamentally different UX patterns.
- **Lock-only approach**: Just set `Locked = true` on nodes in the existing designer. Doesn't address toolbar, properties panel, or node widget edit buttons being visible.

## Decision 3: Auto-Layout Algorithm

**Decision**: Topological sort with layer assignment (Sugiyama-style simplified). Actions are placed in rows based on their topological depth from starting actions, with horizontal positioning to minimise edge crossings.

**Rationale**: Blueprint actions form a directed graph (potentially cyclic). The layout needs to handle:
1. **Linear chains** (ping-pong): simple vertical stack
2. **Branching** (approval with senior path): fan-out at branch points
3. **Parallel branches** (multiple nextActionIds): side-by-side placement
4. **Cycles** (back-edges in ping-pong): detected during layout, rendered as curved back-arrows

**Algorithm**:
1. Find starting actions (`isStartingAction == true`)
2. BFS/DFS from starting actions, assign depth layers
3. For cycles: assign the back-edge target to the earlier layer (don't revisit)
4. Within each layer: order nodes to minimise crossings (simple heuristic: order by parent position)
5. Position: layer * Y_SPACING vertically, evenly distribute horizontally within layer

**Node Dimensions**:
- Action nodes: 280px wide, ~120px tall (matching existing ActionNodeWidget min-width)
- Vertical spacing: 180px between layers
- Horizontal spacing: 320px between parallel nodes

**Alternatives Considered**:
- **Force-directed layout**: Good for general graphs but oscillates on cyclic graphs and doesn't produce clean top-to-bottom flow.
- **Manual position storage**: Templates don't include position data, so auto-layout is mandatory for template previews.
- **Dagre.js via JS interop**: Powerful graph layout library but adds JS dependency and WASM interop overhead.

## Decision 4: Template Preview Integration

**Decision**: Add the `BlueprintViewerDiagram` component to the Templates.razor detail panel (right side) as a visual tab alongside the existing parameter/metadata view.

**Rationale**: The Templates page already has a side-by-side layout (template list left, detail panel right). Adding a visual diagram tab in the detail panel provides preview without navigation away. For parameterised templates, use the default parameters to evaluate and render a preview blueprint.

**Integration Points**:
- **Templates.razor**: Add `BlueprintViewerDiagram` to the detail panel, below description/parameters. Load blueprint from template's default example.
- **Blueprints.razor**: Add "View" icon button on each card that opens a `MudDialog` with `BlueprintViewerDiagram`. Fetches full blueprint JSON via `BlueprintApiService.GetBlueprintAsync()`.

**Alternatives Considered**:
- **Separate /viewer route**: More space for diagram but requires navigation away from the list context.
- **Inline expansion**: Expand the card in-place on click. Disrupts grid layout and doesn't provide enough space.

## Decision 5: Node Detail on Click

**Decision**: Use `MudPopover` or `MudTooltip` to show action details (data schemas, disclosures, calculations) on node click in the readonly viewer.

**Rationale**: A full Properties Panel (like the designer) is overkill for readonly viewing. A popover provides quick detail inspection without a permanent sidebar, preserving diagram space. The popover shows: description, sender, data schemas (as JSON tree), disclosures, and route summary.

**Alternatives Considered**:
- **Read-only Properties Panel sidebar**: Takes 350px of width, reducing diagram space. Unnecessary for read-only browsing.
- **Full-screen dialog**: Too disruptive for quick inspection.

## Decision 6: Blueprint API for Full JSON

**Decision**: Extend `IBlueprintApiService` and `ITemplateApiService` to return full blueprint JSON (with participants, actions, routes) for the viewer — NOT just the list item view model.

**Rationale**: The current `BlueprintListItemViewModel` only contains summary fields (title, actionCount, participantCount). The viewer needs the full `Blueprint` object with all actions, participants, routes, and schemas. The `GetBlueprintAsync(id)` endpoint already returns full data from the backend — the client just needs to deserialise the full response instead of mapping to the summary ViewModel.

**Approach**: Add a `GetBlueprintDetailAsync(id)` method that returns `Blueprint` (the domain model) directly, alongside the existing `GetBlueprintAsync(id)` that returns the summary ViewModel. Similarly for templates: `EvaluateTemplateAsync` already returns a blueprint — use that for rendering.

## Decision 7: Route Rendering

**Decision**: Render routes as coloured directed arrows with small condition labels. Default routes are solid, conditional routes are dashed with a small label badge showing a simplified condition description. Terminal routes (empty nextActionIds) are shown as a small "END" terminator node.

**Rationale**: Routes are the primary flow indicator. Colour-coding and line styles make flow direction obvious at a glance. Small condition labels (e.g., "amount > 10000") provide context without cluttering. Back-edges for cycles use a curved path with a loop icon.

**Visual Encoding**:
- Default route: solid line, primary colour
- Conditional route: dashed line, secondary colour, small label
- Rejection route: red dashed line
- Back-edge (cycle): curved line with loop icon
- Terminal: small red circle with "END" label
