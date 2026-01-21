# Feature Specification: UI Consolidation - Admin to Main UI Migration

**Feature Branch**: `ui-consolidation`
**Created**: 2026-01-21
**Status**: Planning
**Priority**: P0 - Critical Infrastructure

## Executive Summary

This specification defines the consolidation of two parallel UI applications (Sorcha.Admin and Sorcha.UI) into a single, unified Sorcha.UI application. The goal is to migrate advanced features from Admin (particularly the Blueprint Designer components) to the main UI, then deprecate and remove Sorcha.Admin.

---

## Background & Rationale

### Current State

Two separate Blazor applications exist:

| Project | Status | Primary Features |
|---------|--------|------------------|
| **Sorcha.Admin** | Advanced Designer | Blueprint Designer (full), Export/Import, Offline Sync, Multi-Profile Auth |
| **Sorcha.UI** | Complete Platform | Registers, Transactions, Wallets, Admin Dashboard, Health Monitoring |

### Problems with Dual UIs

1. **Maintenance Burden**: Two codebases to maintain with overlapping features
2. **Inconsistent UX**: Different styling, auth flows, and navigation patterns
3. **Feature Fragmentation**: Best features split across both applications
4. **Testing Duplication**: Tests partially duplicated across projects
5. **Deployment Complexity**: Two apps to deploy and configure

### Solution

Migrate unique, valuable features from Sorcha.Admin to Sorcha.UI, then deprecate Sorcha.Admin.

---

## Feature Comparison Matrix

### Blueprint Designer Components

| Component | Admin | UI | Migration Priority |
|-----------|-------|-----|-------------------|
| Designer.razor (basic) | ✅ Full | ✅ Basic | P0 - Enhance |
| PropertiesPanel.razor | ✅ | ✅ | None - Exists |
| ActionNodeWidget.razor | ✅ | ✅ | None - Exists |
| BlueprintJsonView.razor | ✅ | ✅ | None - Exists |
| LoadBlueprintDialog.razor | ✅ | ✅ | None - Exists |
| **CalculationEditor.razor** | ✅ | ❌ | **P0 - Migrate** |
| **ConditionEditor.razor** | ✅ | ❌ | **P0 - Migrate** |
| **ConditionClause.razor** | ✅ | ❌ | **P0 - Migrate** |
| **ParticipantEditor.razor** | ✅ | ❌ | **P0 - Migrate** |
| **ParticipantList.razor** | ✅ | ❌ | **P0 - Migrate** |
| **ExportDialog.razor** | ✅ | ❌ | **P1 - Migrate** |
| **ImportDialog.razor** | ✅ | ❌ | **P1 - Migrate** |
| **WalletSelectorDialog.razor** | ✅ | ❌ | **P1 - Migrate** |
| ConflictResolutionDialog.razor | ✅ | ❌ | P2 - Migrate |
| SyncQueueDialog.razor | ✅ | ❌ | P2 - Migrate |
| OfflineSyncIndicator.razor | ✅ | ❌ | P2 - Migrate |
| SelectParticipantDialog.razor | ✅ | ❌ | P1 - Migrate |

### Services

| Service | Admin | UI | Migration Priority |
|---------|-------|-----|-------------------|
| AuthenticationService | ✅ | ✅ | Compare & Merge |
| BrowserTokenCache | ✅ | ✅ | Compare |
| BlueprintStorageService | ✅ Dual-mode | ✅ Local only | **P1 - Enhance** |
| **BlueprintSerializationService** | ✅ | ❌ | **P1 - Migrate** |
| **OfflineSyncService** | ✅ | ❌ | **P2 - Migrate** |
| **ConfigurationService (profiles)** | ✅ Multi-profile | ✅ Basic | **P1 - Enhance** |
| EventLogService | ✅ | ✅ | Compare |
| RegisterService | ❌ | ✅ | None |
| TransactionService | ❌ | ✅ | None |
| WalletApiService | ❌ | ✅ | None |
| HealthCheckService | ❌ | ✅ | None |
| OrganizationAdminService | ❌ | ✅ | None |
| RegisterHubConnection (SignalR) | ❌ | ✅ | None |

### Models

| Model | Admin | UI | Migration Priority |
|-------|-------|-----|-------------------|
| **CalculationModel** | ✅ | ❌ | **P0 - Migrate** |
| **ConditionModel** | ✅ | ❌ | **P0 - Migrate** |
| **ParticipantModel** | ✅ | ❌ | **P0 - Migrate** |
| **SyncQueueItem** | ✅ | ❌ | **P2 - Migrate** |
| **ImportValidationResult** | ✅ | ❌ | **P1 - Migrate** |
| BlueprintNodeModel | ✅ | ✅ | Compare |
| Profile | ✅ Multi-profile | ✅ Basic | P1 - Enhance |
| TokenCacheEntry | ✅ | ✅ | Compare |

### Pages

| Page | Admin | UI | Migration Priority |
|------|-------|-----|-------------------|
| Designer.razor | ✅ Advanced | ✅ Basic | **P0 - Enhance** |
| Blueprints.razor | ✅ | ✅ | Compare |
| SchemaLibrary.razor | ✅ | ✅ | Compare |
| Administration.razor | ✅ | ✅ | Compare |
| Login.razor | ✅ | ✅ | Compare |
| Index/Dashboard | ✅ | ✅ | Compare |
| Registers (Index/Detail) | ❌ | ✅ | None |
| Wallets (List/Create/Recover/Detail) | ❌ | ✅ | None |
| **MyWorkflows.razor** | ✅ | ❌ | P2 - Migrate |
| **MyTransactions.razor** | ✅ | ❌ | P2 - Migrate (merge with Registers) |
| **MyActions.razor** | ✅ | ❌ | P2 - Migrate |
| **Templates.razor** | ✅ | ❌ | P2 - Migrate |
| **Help.razor** | ✅ | ❌ | P3 - Migrate |
| **Settings.razor** | ✅ | ❌ | P2 - Migrate |
| MyWallet.razor | ✅ Placeholder | ✅ Full | None - UI better |
| ApiDocs.razor | ✅ | ❌ | P3 - Optional |
| Events.razor | ✅ | ❌ | P3 - Optional |

### Tests

| Test Suite | Admin | UI | Migration Priority |
|------------|-------|-----|-------------------|
| **ConditionEditorTests** | ✅ | ❌ | **P0 - Migrate** |
| **ExportImportTests** | ✅ | ❌ | **P1 - Migrate** |
| **ParticipantEditorTests** | ✅ | ❌ | **P0 - Migrate** |
| BlueprintStorageServiceTests | ✅ | ❌ | P1 - Migrate |
| BrowserTokenCacheTests | ✅ | ❌ | P2 - Migrate |
| ConfigurationServiceTests | ✅ | ❌ | P2 - Migrate |
| ProfileDefaultsTests | ✅ | ❌ | P2 - Migrate |
| RegisterCardTests | ❌ | ✅ | None |
| TransactionDetailTests | ❌ | ✅ | None |
| TransactionListTests | ❌ | ✅ | None |
| E2E Tests (Admin Dashboard) | ❌ | ✅ | None |

---

## User Scenarios & Testing

### User Story 1 - Complete Participant Management (Priority: P0)

As a workflow designer, I need to add and configure participants for each action so that I can define who performs each step.

**Acceptance Scenarios**:

1. **Given** an action in the designer, **When** I click "Add Participant", **Then** a participant editor dialog opens.
2. **Given** the participant editor, **When** I enter name, role, and wallet address, **Then** the participant is added.
3. **Given** participants on an action, **When** I view the action node, **Then** the participant count is displayed.
4. **Given** a participant, **When** I click edit, **Then** I can modify all participant properties.

---

### User Story 2 - Condition Builder (Priority: P0)

As a workflow designer, I need to define conditions for actions using a visual builder so that I can control workflow logic without writing JSON.

**Acceptance Scenarios**:

1. **Given** an action, **When** I click "Add Condition", **Then** the condition editor opens.
2. **Given** the condition editor, **When** I select operators and values, **Then** valid JSON Logic is generated.
3. **Given** complex conditions, **When** I add AND/OR clauses, **Then** they are nested correctly.
4. **Given** a condition, **When** I view JSON, **Then** it shows valid JSON Logic format.

---

### User Story 3 - Calculation Builder (Priority: P0)

As a workflow designer, I need to define calculations for computed fields so that I can specify data transformations.

**Acceptance Scenarios**:

1. **Given** an action, **When** I add a calculation, **Then** the calculation editor opens.
2. **Given** the calculation editor, **When** I enter expressions, **Then** they are parsed correctly.
3. **Given** infix notation (e.g., `a + b * c`), **When** I save, **Then** it converts to proper JSON Logic.
4. **Given** a test value, **When** I preview, **Then** I see the computed result.

---

### User Story 4 - Blueprint Export/Import (Priority: P1)

As a workflow designer, I need to export blueprints to files and import them so that I can share and backup my work.

**Acceptance Scenarios**:

1. **Given** a blueprint, **When** I click Export, **Then** I can choose JSON or YAML format.
2. **Given** the export dialog, **When** I confirm, **Then** a file downloads.
3. **Given** a JSON/YAML file, **When** I import, **Then** the blueprint is validated and loaded.
4. **Given** an invalid file, **When** I import, **Then** I see meaningful error messages.

---

### User Story 5 - Wallet Selection for Signing (Priority: P1)

As a workflow designer, I need to select wallets for participants so that transactions can be signed properly.

**Acceptance Scenarios**:

1. **Given** a participant, **When** I click "Select Wallet", **Then** I see my available wallets.
2. **Given** the wallet selector, **When** I choose a wallet, **Then** the address is assigned.
3. **Given** a selected wallet, **Then** I see the address preview and algorithm type.

---

### User Story 6 - Enhanced Designer Page (Priority: P0)

As a workflow designer, I need the enhanced Designer with all editor components so that I have full control over blueprint creation.

**Acceptance Scenarios**:

1. **Given** the Designer, **When** I select an action, **Then** I can access participant, condition, and calculation editors.
2. **Given** the properties panel, **When** I click "Add Participant", **Then** the ParticipantEditor dialog opens.
3. **Given** action metadata changes, **When** saved, **Then** the node widget updates with new counts.

---

## Requirements

### Functional Requirements

- **FR-001**: System MUST migrate ParticipantEditor to Sorcha.UI
- **FR-002**: System MUST migrate ConditionEditor to Sorcha.UI
- **FR-003**: System MUST migrate CalculationEditor to Sorcha.UI
- **FR-004**: System MUST migrate Export/Import dialogs to Sorcha.UI
- **FR-005**: System MUST migrate BlueprintSerializationService to Sorcha.UI
- **FR-006**: System MUST migrate WalletSelectorDialog to Sorcha.UI
- **FR-007**: System MUST migrate all designer-related tests to Sorcha.UI
- **FR-008**: System SHOULD migrate OfflineSyncService for offline support
- **FR-009**: System SHOULD migrate multi-profile authentication
- **FR-010**: System COULD migrate developer tools pages (ApiDocs, Events)
- **FR-011**: System MUST deprecate Sorcha.Admin after migration complete
- **FR-012**: System MUST maintain backward compatibility with existing blueprints

### Non-Functional Requirements

- **NFR-001**: Migration must not break existing Sorcha.UI functionality
- **NFR-002**: All migrated tests must pass
- **NFR-003**: UI styling must remain consistent with Sorcha.UI theme
- **NFR-004**: No performance regression in Designer page
- **NFR-005**: Migration should be completed in phases to reduce risk

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: All P0 components migrated and functional
- **SC-002**: All P1 components migrated and functional
- **SC-003**: 100% of Admin designer tests pass in UI
- **SC-004**: User acceptance testing passes for Designer features
- **SC-005**: No regressions in existing Sorcha.UI functionality
- **SC-006**: Sorcha.Admin marked deprecated with removal date
- **SC-007**: Documentation updated for unified UI

---

## Out of Scope

1. Backend service changes (migration is UI-only)
2. New feature development during migration
3. Complete UI redesign or rebranding
4. Performance optimization (unless regressions found)

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Breaking existing UI | High | Medium | Phased migration with tests |
| Style inconsistencies | Medium | Medium | Use existing UI theme/components |
| Missing edge cases | Medium | Low | Migrate tests alongside components |
| Authentication conflicts | High | Low | Compare implementations carefully |

---

## Timeline & Phases

### Phase 1: P0 Designer Components (Week 1-2)
- ParticipantEditor, ConditionEditor, CalculationEditor
- Related models and tests
- Designer page enhancements

### Phase 2: P1 Export/Import & Services (Week 2-3)
- Export/Import dialogs and BlueprintSerializationService
- WalletSelectorDialog and SelectParticipantDialog
- BlueprintStorageService enhancements

### Phase 3: P2 Offline & Sync (Week 3-4)
- OfflineSyncService and related dialogs
- Multi-profile authentication enhancements
- Additional pages (MyWorkflows, Settings)

### Phase 4: Deprecation & Cleanup (Week 4-5)
- Mark Sorcha.Admin as deprecated
- Update documentation
- User acceptance testing
- Remove Sorcha.Admin from solution

---

## Appendix: File Mapping

### Components to Migrate

| Source (Admin) | Target (UI) |
|----------------|-------------|
| `Components/Designer/CalculationEditor.razor` | `Sorcha.UI.Core/Components/Designer/CalculationEditor.razor` |
| `Components/Designer/ConditionEditor.razor` | `Sorcha.UI.Core/Components/Designer/ConditionEditor.razor` |
| `Components/Designer/ConditionClause.razor` | `Sorcha.UI.Core/Components/Designer/ConditionClause.razor` |
| `Components/Designer/ParticipantEditor.razor` | `Sorcha.UI.Core/Components/Designer/ParticipantEditor.razor` |
| `Components/Designer/ParticipantList.razor` | `Sorcha.UI.Core/Components/Designer/ParticipantList.razor` |
| `Components/Designer/ExportDialog.razor` | `Sorcha.UI.Core/Components/Designer/ExportDialog.razor` |
| `Components/Designer/ImportDialog.razor` | `Sorcha.UI.Core/Components/Designer/ImportDialog.razor` |
| `Components/Designer/WalletSelectorDialog.razor` | `Sorcha.UI.Core/Components/Designer/WalletSelectorDialog.razor` |
| `Components/Designer/SelectParticipantDialog.razor` | `Sorcha.UI.Core/Components/Designer/SelectParticipantDialog.razor` |
| `Components/Designer/OfflineSyncIndicator.razor` | `Sorcha.UI.Core/Components/Designer/OfflineSyncIndicator.razor` |
| `Components/Designer/SyncQueueDialog.razor` | `Sorcha.UI.Core/Components/Designer/SyncQueueDialog.razor` |
| `Components/Designer/ConflictResolutionDialog.razor` | `Sorcha.UI.Core/Components/Designer/ConflictResolutionDialog.razor` |

### Services to Migrate

| Source (Admin) | Target (UI) |
|----------------|-------------|
| `Services/BlueprintSerializationService.cs` | `Sorcha.UI.Core/Services/BlueprintSerializationService.cs` |
| `Services/OfflineSyncService.cs` | `Sorcha.UI.Core/Services/OfflineSyncService.cs` |
| `Services/IOfflineSyncService.cs` | `Sorcha.UI.Core/Services/IOfflineSyncService.cs` |

### Models to Migrate

| Source (Admin) | Target (UI) |
|----------------|-------------|
| `Models/CalculationModel.cs` | `Sorcha.UI.Core/Models/Designer/CalculationModel.cs` |
| `Models/ConditionModel.cs` | `Sorcha.UI.Core/Models/Designer/ConditionModel.cs` |
| `Models/ParticipantModel.cs` | `Sorcha.UI.Core/Models/Designer/ParticipantModel.cs` |
| `Models/SyncQueueItem.cs` | `Sorcha.UI.Core/Models/Designer/SyncQueueItem.cs` |
| `Models/ImportValidationResult.cs` | `Sorcha.UI.Core/Models/Designer/ImportValidationResult.cs` |

### Tests to Migrate

| Source (Admin) | Target (UI) |
|----------------|-------------|
| `Tests/Components/Designer/ConditionEditorTests.cs` | `tests/Sorcha.UI.Core.Tests/Components/Designer/ConditionEditorTests.cs` |
| `Tests/Components/Designer/ExportImportTests.cs` | `tests/Sorcha.UI.Core.Tests/Components/Designer/ExportImportTests.cs` |
| `Tests/Components/Designer/ParticipantEditorTests.cs` | `tests/Sorcha.UI.Core.Tests/Components/Designer/ParticipantEditorTests.cs` |
| `Tests/Components/Designer/BlueprintStorageServiceTests.cs` | `tests/Sorcha.UI.Core.Tests/Services/BlueprintStorageServiceTests.cs` |
