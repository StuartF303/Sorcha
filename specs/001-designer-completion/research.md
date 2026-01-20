# Research: Blueprint Designer Completion

**Feature**: 001-designer-completion
**Date**: 2026-01-20

## Research Tasks

### 1. Participant Editor Design (BD-022)

**Decision**: Dialog-based editor with wallet selector and role assignment

**Rationale**: Follows existing pattern of dialog-based editing (BlueprintPropertiesDialog, etc.). Participants need:
- Unique identifier (wallet address preferred)
- Display name
- Role assignment (Initiator, Approver, Observer, etc.)
- Metadata (optional additional properties)

**UI Pattern**:
```
┌─────────────────────────────────────────────┐
│ Add Participant                         [X] │
├─────────────────────────────────────────────┤
│ Identifier*  [____________________] [Wallet]│
│ Display Name [____________________]         │
│ Role*        [Approver           ▼]         │
│ Description  [____________________]         │
├─────────────────────────────────────────────┤
│                      [Cancel] [Add]         │
└─────────────────────────────────────────────┘
```

**Wallet Integration**: "Wallet" button opens wallet picker to select from user's registered wallets.

### 2. Condition Editor Design (BD-023)

**Decision**: Visual clause builder with AND/OR logic

**Rationale**: JSON Logic expressions are powerful but hard to write by hand. A visual builder makes conditions accessible.

**Visual Pattern**:
```
┌─────────────────────────────────────────────────────────────┐
│ Routing Condition                                       [X] │
├─────────────────────────────────────────────────────────────┤
│ When:                                                       │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [/loanAmount    ▼] [>     ▼] [50000    ]          [🗑] │ │
│ └─────────────────────────────────────────────────────────┘ │
│         [AND ▼]                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [/applicantType ▼] [==    ▼] [Premium  ]          [🗑] │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                [+ Add Clause]               │
│                                                             │
│ Then route to: [Senior Approver    ▼]                       │
├─────────────────────────────────────────────────────────────┤
│ Generated: {"and":[{">":...},{==":...}]}                    │
├─────────────────────────────────────────────────────────────┤
│                              [Cancel] [Save]                │
└─────────────────────────────────────────────────────────────┘
```

**Operators Supported**:
| Operator | Label | Types |
|----------|-------|-------|
| == | equals | all |
| != | not equals | all |
| > | greater than | number |
| < | less than | number |
| >= | at least | number |
| <= | at most | number |
| contains | contains | string |
| startsWith | starts with | string |
| endsWith | ends with | string |

### 3. Export/Import Format (BD-024)

**Decision**: JSON (primary) and YAML (secondary) formats

**Rationale**:
- JSON: Native to JavaScript/Blazor, exact round-trip
- YAML: Human-readable, preferred for version control

**Serialization Library**: YamlDotNet (widely used, well-maintained)

**File Structure**:
```yaml
# blueprint.yaml
title: Loan Application Workflow
version: "1.0"
description: Multi-stage loan approval process

participants:
  - id: applicant
    name: Loan Applicant
    role: Initiator
  - id: underwriter
    name: Underwriter
    role: Approver

actions:
  - id: submit_application
    title: Submit Application
    # ... action details
```

**Validation on Import**:
1. Schema validation (required fields)
2. Reference validation (participants referenced exist)
3. Circular dependency detection
4. Schema reference availability check

### 4. Backend Integration Pattern (BD-025)

**Decision**: Hybrid storage with offline queue

**Rationale**: Users expect both offline capability (designer works without network) and durability (work is saved to server).

**Architecture**:
```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Designer UI   │────▶│ StorageService  │────▶│ Blueprint API   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                              │
                              ▼
                        ┌─────────────────┐
                        │  LocalStorage   │
                        │  (offline cache │
                        │   + sync queue) │
                        └─────────────────┘
```

**Operations Flow**:
1. **Save**: Try server first, fall back to LocalStorage + queue
2. **Load**: Try server first, fall back to LocalStorage cache
3. **Sync**: Background process retries queued operations

**Conflict Resolution**: Last-write-wins with user notification

### 5. LocalStorage Migration Strategy

**Decision**: Gradual migration with compatibility layer

**Rationale**: Don't break existing users with blueprints in LocalStorage.

**Migration Steps**:
1. On first load with server available, upload LocalStorage blueprints
2. Mark as "migrated" in LocalStorage
3. Keep LocalStorage as cache until explicit cleanup
4. Provide "Clear Local Cache" option in settings

### 6. Calculated Field Expression Builder

**Decision**: Visual formula builder for computed fields

**Pattern**:
```
┌─────────────────────────────────────────────────────────────┐
│ Calculated Field                                        [X] │
├─────────────────────────────────────────────────────────────┤
│ Field: [/monthlyPayment           ]                         │
│                                                             │
│ Formula:                                                    │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [/loanAmount ▼] [*] [/interestRate ▼] [/] [12]          │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ Test Values:                                                │
│   /loanAmount: [50000   ] /interestRate: [0.05  ]          │
│   Result: 208.33                                            │
├─────────────────────────────────────────────────────────────┤
│                              [Cancel] [Save]                │
└─────────────────────────────────────────────────────────────┘
```

**Supported Operations**: +, -, *, /, parentheses, field references

## Dependencies Confirmed

| Dependency | Version | Status |
|------------|---------|--------|
| MudBlazor | 8.15.0 | ✅ In project |
| Z.Blazor.Diagrams | 3.0.4 | ✅ In project |
| Blazored.LocalStorage | 4.5.0 | ✅ In project |
| YamlDotNet | - | ⚠️ Need to add |
| JsonLogic | 5.5.0 | ✅ In project |
| Sorcha.Blueprint.Models | - | ✅ Referenced |

## API Endpoints Required

Blueprint Service needs these endpoints (check if exist):

```
GET    /api/v1/blueprints              → List user's blueprints
GET    /api/v1/blueprints/{id}         → Get blueprint details
POST   /api/v1/blueprints              → Create new blueprint
PUT    /api/v1/blueprints/{id}         → Update blueprint
DELETE /api/v1/blueprints/{id}         → Delete blueprint
```

## Open Questions Resolved

1. **Where should new components live?** → Components/Designer/ subdirectory
2. **How to handle offline?** → LocalStorage queue with background sync
3. **What import validation?** → Schema, references, cycles
4. **Conflict resolution?** → Last-write-wins + user notification
5. **YAML library?** → YamlDotNet
