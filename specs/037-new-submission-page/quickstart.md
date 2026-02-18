# Quickstart: New Submission Page (037)

**Date**: 2026-02-18
**Branch**: `037-new-submission-page`

## Build & Test

```bash
# Build the full solution
dotnet build

# Run affected test suites
dotnet test tests/Sorcha.UI.Core.Tests/
dotnet test tests/Sorcha.Blueprint.Service.Tests/

# Build UI projects specifically
dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/
dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/
dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web/
```

## Manual Verification (Docker)

### Prerequisites
```bash
# Start all services
docker-compose up -d

# Verify services healthy
docker-compose ps
```

### Test Scenario 1: Browse Available Services

1. Navigate to `http://localhost/app/my-workflows`
2. Verify the page shows registers grouped as sections
3. Verify each register section lists available blueprints
4. Verify empty registers are hidden
5. Verify "New Submission" appears before "Pending Actions" in sidebar

### Test Scenario 2: Start a New Submission

1. Navigate to `http://localhost/app/my-workflows`
2. Click "Start" on a blueprint card
3. Verify the submission dialog opens with a form
4. Verify wallet is pre-selected (if user has one wallet)
5. Fill in form fields
6. Click "Submit"
7. Verify success notification with workflow instance reference
8. Navigate to "Pending Actions" — the next participant should see the action

### Test Scenario 3: Pending Action Submission (Fix)

1. Navigate to `http://localhost/app/my-actions`
2. Click "Take Action" on a pending action
3. Verify signing wallet is pre-populated
4. Fill in the form
5. Click "Submit"
6. Verify the action is submitted to the backend (check logs)
7. Verify the action disappears from the pending list

### Test Scenario 4: Multi-Wallet Default

1. Create a second wallet via My Wallet page
2. Navigate to New Submission and click "Start"
3. Verify wallet selector appears with both wallets
4. Select the second wallet and mark as default
5. Cancel and start again — verify second wallet is pre-selected

## Expected Test Counts

| Test Suite | Expected |
|------------|----------|
| UI Core Tests | Baseline + new tests for WalletPreferenceService, submission flow |
| Blueprint Service Tests | Baseline (no backend changes expected) |

## Files Changed

### New Files
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/WalletPreferenceService.cs`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IWalletPreferenceService.cs`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/NewSubmissionDialog.razor`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/WalletSelector.razor`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/StartableBlueprintViewModel.cs`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/RegisterBlueprintGroup.cs`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/AvailableBlueprintsViewModel.cs`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/ActionSubmissionResultViewModel.cs`
- `tests/Sorcha.UI.Core.Tests/Services/WalletPreferenceServiceTests.cs`

### Modified Files
- `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyWorkflows.razor` — complete redesign
- `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyActions.razor` — wire submission flow
- `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor` — swap nav order
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IBlueprintApiService.cs` — add GetAvailableBlueprintsAsync
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/BlueprintApiService.cs` — implement new method
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IWorkflowService.cs` — add CreateInstanceAsync, update SubmitActionAsync
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/WorkflowService.cs` — implement new/updated methods
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/ActionForm.razor` — wire wallet address
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` — register WalletPreferenceService
