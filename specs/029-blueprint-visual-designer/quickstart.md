# Quickstart: Blueprint Visual Designer

**Feature**: 029-blueprint-visual-designer
**Date**: 2026-02-09

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for running Blueprint Service with templates)
- Node already has Z.Blazor.Diagrams v3.0.4 and MudBlazor

## Quick Verification Scenarios

### Scenario 1: Readonly Viewer Renders Ping-Pong Template

**Steps**:
1. Start services: `docker-compose up -d`
2. Navigate to `http://localhost/app/templates`
3. Select the "Ping-Pong" template from the list
4. Verify the visual diagram appears in the detail panel showing:
   - 2 action nodes ("Ping" and "Pong") in a vertical flow
   - Directed arrows between them (forward and back-edge for the cycle)
   - Sender labels showing participant names
   - Starting action badge on first action
   - Cycle indicator on the back-edge arrow

**Expected Result**: Visual diagram renders within 2 seconds with correct structure.

### Scenario 2: Approval Workflow with Branching

**Steps**:
1. Navigate to `http://localhost/app/templates`
2. Select the "Approval Workflow" template
3. Verify the diagram shows:
   - Linear path: Submit → Manager Review
   - Conditional branch: if high-value, branch to Senior Review
   - Merge point or terminal actions
   - Condition labels on branching routes

**Expected Result**: Branching routes display as fan-out with condition labels.

### Scenario 3: Blueprint Library View

**Steps**:
1. Navigate to `http://localhost/app/blueprints`
2. If no blueprints exist, create one from a template first
3. Click "View" on a blueprint card
4. Verify the dialog opens with the readonly diagram
5. Verify zoom works (mouse wheel) but no editing is possible

**Expected Result**: Dialog shows readonly diagram with zoom support.

### Scenario 4: End-to-End Deployment Pipeline

**Steps**:
1. Navigate to `http://localhost/app/templates`
2. Select "Ping-Pong" template → click "Use Template"
3. Complete template evaluation → navigate to Designer
4. From Designer: Save the blueprint → navigate to Blueprints
5. Click "Publish" on the saved blueprint
6. Verify publication succeeds (possibly with cycle warnings)
7. Create a workflow instance from the published blueprint

**Expected Result**: Blueprint goes from template → saved → published → instance created.

### Scenario 5: Large Blueprint Rendering

**Steps**:
1. Load the "Supply Chain Order" template (most complex built-in template)
2. Verify diagram renders all actions (5-7) with correct routing
3. Verify zoom out shows full diagram without overlap
4. Verify scroll works when diagram exceeds container bounds

**Expected Result**: All nodes visible, no overlap, zoom/scroll works smoothly.

## Manual Testing Checklist

- [ ] Ping-pong template renders 2 nodes with cycle arrow
- [ ] Approval workflow renders branching correctly
- [ ] Loan application renders conditional senior-officer path
- [ ] Supply-chain template renders all participants and routes
- [ ] Click action node shows detail popover
- [ ] Popover shows schemas, disclosures, routes
- [ ] Zoom in/out works via mouse wheel
- [ ] No nodes can be dragged in readonly mode
- [ ] No toolbar buttons visible in readonly mode
- [ ] No edit buttons visible on action nodes
- [ ] Templates page shows diagram in detail panel
- [ ] Blueprints page "View" button opens diagram dialog
- [ ] Blueprint publish with cycle warnings succeeds
- [ ] Template "Use Template" flow still works (no regression)
