# Data Model: Blueprint Visual Designer

**Feature**: 029-blueprint-visual-designer
**Date**: 2026-02-09

## Overview

This feature primarily consumes existing data models (Blueprint, Action, Participant, Route) rather than creating new domain entities. The new models are UI-specific layout and rendering models.

## Existing Entities (Consumed, Not Modified)

### Blueprint
The top-level workflow definition. Properties relevant to the visual viewer:
- `id` (string): Unique identifier
- `title` (string): Display name
- `description` (string): Summary text
- `participants` (List\<Participant\>): Workflow actors
- `actions` (List\<Action\>): Workflow steps
- `version` (int): Version number
- `dataSchemas` (List\<JsonDocument\>): Shared schemas

### Action
A discrete workflow step. Properties relevant to the visual viewer:
- `id` (int): Sequence number (0-based)
- `title` (string): Display name
- `sender` (string): Participant ID who performs this action
- `isStartingAction` (bool): Entry point indicator
- `routes` (IEnumerable\<Route\>): Transition rules (modern routing)
- `condition` (JsonNode?): Legacy routing condition
- `participants` (IEnumerable\<Condition\>?): Legacy routing via participants
- `dataSchemas` (IEnumerable\<JsonDocument\>?): Input/output schemas
- `disclosures` (IEnumerable\<Disclosure\>): Visibility rules
- `calculations` (Dictionary\<string, JsonNode\>?): Computed fields
- `rejectionConfig` (RejectionConfig?): Rejection routing

### Participant
A named workflow actor:
- `id` (string): Unique identifier
- `name` (string): Display name
- `walletAddress` (string): Crypto address
- `organisation` (string): Org affiliation

### Route
A directed transition between actions:
- `id` (string): Route identifier
- `nextActionIds` (IEnumerable\<int\>): Target action(s); empty = workflow end
- `condition` (JsonNode?): When this route applies
- `isDefault` (bool): Fallback route
- `description` (string?): Human-readable label

### Disclosure
Data visibility rule:
- `participantAddress` (string): Who can see the data
- `dataPointers` (List\<string\>): JSON Pointers to visible fields

## New Entities (UI Layout Models)

### DiagramLayout
The computed layout for a blueprint's visual representation:
- `nodes` (List\<DiagramNode\>): Positioned action nodes
- `edges` (List\<DiagramEdge\>): Route connections
- `participantLegend` (List\<ParticipantInfo\>): Participant colour mapping
- `width` (double): Total diagram width
- `height` (double): Total diagram height

### DiagramNode
A positioned action node in the layout:
- `actionId` (int): Source action ID
- `title` (string): Display title
- `senderParticipantId` (string): Who performs this action
- `layer` (int): Depth layer (0 = starting actions)
- `position` (Point): X, Y coordinates in diagram
- `isStarting` (bool): Entry point indicator
- `isTerminal` (bool): No outgoing routes
- `isCycleTarget` (bool): Target of a back-edge
- `detailSummary` (string): Short summary for display (schema count, disclosure count)

### DiagramEdge
A directed connection between nodes:
- `sourceActionId` (int): From action
- `targetActionId` (int): To action
- `routeId` (string?): Source route identifier
- `edgeType` (EdgeType): Visual style (Default, Conditional, Rejection, BackEdge, Terminal)
- `label` (string?): Condition description or route name
- `isBackEdge` (bool): Part of a cycle

### ParticipantInfo
Participant metadata for the legend:
- `id` (string): Participant ID
- `name` (string): Display name
- `colour` (string): Assigned colour for visual coding

### EdgeType (enum)
- `Default`: Solid line, primary colour — unconditional forward route
- `Conditional`: Dashed line, secondary colour — conditional branch
- `Rejection`: Red dashed line — rejection routing
- `BackEdge`: Curved line with loop icon — cycle back-reference
- `Terminal`: Line to END marker — workflow completion

## State Transitions

No state transitions for this feature — the viewer is stateless (renders a snapshot of a blueprint).

## Relationships

```
Blueprint 1──* Participant
Blueprint 1──* Action
Action    1──* Route
Action    *──1 Participant (via sender)
Action    1──* Disclosure
Route     *──* Action (via nextActionIds)

DiagramLayout 1──* DiagramNode
DiagramLayout 1──* DiagramEdge
DiagramLayout 1──* ParticipantInfo
DiagramNode   *──1 Action
DiagramEdge   *──1 Route
```
