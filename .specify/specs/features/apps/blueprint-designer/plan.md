# Implementation Plan: Blueprint Designer

**Feature Branch**: `blueprint-designer`
**Created**: 2025-12-03
**Status**: 85% Complete

## Summary

The Blueprint Designer is a Blazor WebAssembly application that provides a visual interface for designing Sorcha workflows. It features a drag-and-drop canvas, property editors, schema library integration, and administration dashboard.

## Design Decisions

### Decision 1: Blazor WebAssembly

**Approach**: Use Blazor WASM for client-side rendering.

**Rationale**:
- Full .NET runtime in browser
- Share models with backend services
- No server round-trips for UI updates
- Offline-capable with service workers

### Decision 2: MudBlazor Component Library

**Approach**: Use MudBlazor for UI components.

**Rationale**:
- Material Design consistency
- Rich component library
- Good documentation
- Active community

### Decision 3: Blazor.Diagrams for Canvas

**Approach**: Use Blazor.Diagrams for the visual diagram canvas.

**Rationale**:
- Native Blazor implementation
- Customizable node widgets
- Link/port support
- Zoom and pan built-in

### Decision 4: Local Storage Persistence

**Approach**: Store blueprints in browser local storage.

**Rationale**:
- No backend dependency for MVP
- Instant save/load
- Offline support
- User privacy (data stays local)

### Decision 5: Schema Library with Caching

**Approach**: Cache schemas in local storage with background refresh.

**Rationale**:
- Fast initial load from cache
- Background refresh keeps data current
- Reduces API calls
- Works offline

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│           Sorcha.Blueprint.Designer.Client                   │
│                (Blazor WebAssembly)                          │
├─────────────────────────────────────────────────────────────┤
│  Pages/                                                      │
│  ├── Home.razor           (Blueprint Designer Canvas)       │
│  ├── SchemaLibrary.razor  (Schema Browser)                  │
│  ├── Administration.razor (Service Admin Dashboard)         │
│  ├── Settings.razor       (User Preferences)                │
│  └── Index.razor          (Landing Page)                    │
├─────────────────────────────────────────────────────────────┤
│  Components/                                                 │
│  ├── ActionNodeWidget.razor     (Custom diagram node)       │
│  ├── PropertiesPanel.razor      (Right-side editor)         │
│  ├── BlueprintJsonView.razor    (JSON viewer)               │
│  ├── LoadBlueprintDialog.razor  (Blueprint picker)          │
│  ├── JsonTreeView.razor         (Tree structure view)       │
│  └── Admin/                                                 │
│      ├── BlueprintServiceAdmin.razor                        │
│      └── PeerServiceAdmin.razor                             │
├─────────────────────────────────────────────────────────────┤
│  Services/                                                   │
│  ├── EventLogService.cs         (Activity logging)          │
│  ├── SchemaLibraryService.cs    (Schema management)         │
│  └── ApiConfiguration.cs        (Backend API config)        │
├─────────────────────────────────────────────────────────────┤
│  Models/                                                     │
│  ├── BlueprintNodeModel.cs      (Diagram node model)        │
│  └── SchemaProvider.cs          (Schema source config)      │
└─────────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Blueprint Designer (Home) | 100% | Canvas, actions, properties |
| Action Node Widget | 100% | Custom node rendering |
| Properties Panel | 100% | Blueprint and action editing |
| Blueprint JSON View | 100% | Formatted JSON display |
| Load/Save Dialogs | 100% | Local storage integration |
| Schema Library | 90% | Search, filter, cache |
| Administration | 80% | Blueprint and Peer tabs |
| Event Log | 100% | Activity tracking |
| Example Blueprints | 100% | Loan, Supply Chain, Document |

### Page Structure

| Page | Route | Purpose | Status |
|------|-------|---------|--------|
| Index | `/` | Landing page | 100% |
| Designer | `/designer` | Blueprint canvas | 100% |
| Schema Library | `/schemas` | Schema browser | 90% |
| Administration | `/admin` | Service dashboard | 80% |
| Settings | `/settings` | User preferences | 70% |

### Key Dependencies

```xml
<PackageReference Include="MudBlazor" />
<PackageReference Include="Blazor.Diagrams" />
<PackageReference Include="Blazored.LocalStorage" />
```

## Dependencies

### NuGet Packages

- `MudBlazor` - Material Design components
- `Blazor.Diagrams` - Diagram/flowchart canvas
- `Blazored.LocalStorage` - Browser storage API

### Project References

- `Sorcha.Blueprint.Models` - Blueprint domain models
- `Sorcha.Blueprint.Schemas` - Schema library and built-ins

### External Services

- Schema Store (SchemaStore.org) - External JSON schemas
- Blueprint Service API - Backend operations (future)
- Peer Service API - Network status (admin)

## Migration/Integration Notes

### Local Storage Keys

```
sorcha:blueprints     - Saved blueprint list
sorcha:settings       - User preferences
sorcha:schema-cache   - Cached schema data
sorcha:favorites      - Favorite schemas
```

### Example Blueprints Created on First Load

1. **Loan Application Workflow** - Multi-party loan process
2. **Supply Chain Verification** - Product tracking
3. **Document Approval Flow** - Review process

### Breaking Changes

- None (new application)

## Open Questions

1. Should blueprints sync to backend service for collaboration?
2. How to handle blueprint version conflicts?
3. Should we add real-time collaboration (SignalR)?
4. How to export blueprints for deployment?
