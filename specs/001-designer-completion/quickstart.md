# Quickstart: Blueprint Designer Completion

**Feature**: 001-designer-completion
**Date**: 2026-01-20

## Overview

This guide covers the completed Blueprint Designer features: Participant Editor, Condition Editor, Export/Import, and Server Persistence.

## Prerequisites

- Sorcha.Admin running locally or in Docker
- Blueprint Service accessible (for server persistence)
- User authenticated

## Adding Participants (BD-022)

### Opening the Participant Editor

```razor
@* In Designer.razor or PropertiesPanel.razor *@
<MudButton OnClick="OpenParticipantEditor">Add Participant</MudButton>

@code {
    private async Task OpenParticipantEditor()
    {
        var dialog = await DialogService.ShowAsync<ParticipantEditor>(
            "Add Participant",
            new DialogParameters<ParticipantEditor>
            {
                { x => x.Blueprint, _currentBlueprint },
                { x => x.ExistingParticipants, _currentBlueprint.Participants }
            });

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: ParticipantModel participant })
        {
            _currentBlueprint.Participants.Add(participant.ToParticipant());
            await SaveBlueprintAsync();
        }
    }
}
```

### Using the Wallet Selector

```razor
@* In ParticipantEditor.razor *@
<MudTextField @bind-Value="_model.WalletAddress"
              Label="Wallet Address"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.AccountBalanceWallet"
              OnAdornmentClick="OpenWalletSelector" />

@code {
    private async Task OpenWalletSelector()
    {
        var wallets = await WalletService.GetUserWalletsAsync();
        var selected = await DialogService.ShowAsync<WalletSelectorDialog>(
            "Select Wallet",
            new DialogParameters { { "Wallets", wallets } });

        if (selected.Result is { Data: WalletDto wallet })
        {
            _model.WalletAddress = wallet.Address;
        }
    }
}
```

## Building Routing Conditions (BD-023)

### Opening the Condition Editor

```razor
<MudButton OnClick="() => OpenConditionEditor(selectedAction)">
    Edit Routing Condition
</MudButton>

@code {
    private async Task OpenConditionEditor(ActionModel action)
    {
        var fields = GetAvailableFields(action);  // From action's input schema

        var dialog = await DialogService.ShowAsync<ConditionEditor>(
            "Routing Condition",
            new DialogParameters<ConditionEditor>
            {
                { x => x.ExistingCondition, action.Routing },
                { x => x.AvailableFields, fields },
                { x => x.AvailableParticipants, _blueprint.Participants }
            });

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: ConditionModel condition })
        {
            action.Routing = condition.ToJsonLogic();
            action.NextParticipantId = condition.TargetParticipantId;
        }
    }
}
```

### Condition Editor Usage

The condition editor provides a visual builder:

| Field | Operator | Value |
|-------|----------|-------|
| Select field | Select comparison | Enter value |

**Adding multiple clauses**:
1. Click "+ Add Clause"
2. Select AND or OR operator
3. Configure the new clause

**Testing conditions**:
The preview panel shows the generated JSON Logic:
```json
{"and":[{">":{"var":"/loanAmount"},50000},{"==":{"var":"/type"},"Premium"}]}
```

## Exporting Blueprints (BD-024)

### Export Dialog

```razor
<MudButton OnClick="OpenExportDialog">Export Blueprint</MudButton>

@code {
    private async Task OpenExportDialog()
    {
        var dialog = await DialogService.ShowAsync<ExportDialog>(
            "Export Blueprint",
            new DialogParameters<ExportDialog>
            {
                { x => x.Blueprint, _currentBlueprint }
            });
    }
}
```

### Export Options

| Format | Use Case |
|--------|----------|
| JSON | Exact round-trip, API integration |
| YAML | Human readable, version control |

### Programmatic Export

```csharp
@inject BlueprintSerializationService Serializer

// Export to JSON
var json = Serializer.ToJson(_blueprint);
await DownloadFile($"{_blueprint.Title}.json", json, "application/json");

// Export to YAML
var yaml = Serializer.ToYaml(_blueprint);
await DownloadFile($"{_blueprint.Title}.yaml", yaml, "text/yaml");
```

## Importing Blueprints (BD-024)

### Import Dialog

```razor
<MudButton OnClick="OpenImportDialog">Import Blueprint</MudButton>

@code {
    private async Task OpenImportDialog()
    {
        var dialog = await DialogService.ShowAsync<ImportDialog>("Import Blueprint");
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: Blueprint imported })
        {
            await StorageService.SaveBlueprintAsync(imported);
            NavigationManager.NavigateTo($"/designer/{imported.Id}");
        }
    }
}
```

### Handling Validation Errors

```csharp
private async Task HandleImport(IBrowserFile file)
{
    var content = await ReadFileAsync(file);
    var result = await Serializer.ValidateAndParse(content, file.Name);

    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            Snackbar.Add($"{error.Path}: {error.Message}", Severity.Error);
        }
        return;
    }

    foreach (var warning in result.Warnings)
    {
        Snackbar.Add($"Warning: {warning.Message}", Severity.Warning);
    }

    // Proceed with valid blueprint
    _importedBlueprint = result.Blueprint;
}
```

## Server Persistence (BD-025)

### Saving to Server

```csharp
@inject IBlueprintStorageService Storage

// Save automatically tries server, falls back to local
var result = await Storage.SaveBlueprintAsync(_blueprint);

if (result.SavedToServer)
{
    Snackbar.Add("Saved to server", Severity.Success);
}
else if (result.QueuedForSync)
{
    Snackbar.Add("Saved locally - will sync when online", Severity.Info);
}
```

### Loading Blueprints

```csharp
// Load prefers server, falls back to local cache
var blueprints = await Storage.GetBlueprintsAsync();

foreach (var bp in blueprints)
{
    Console.WriteLine($"{bp.Title} - Source: {bp.Source}");
    // Source: Server, LocalCache, or SyncPending
}
```

### Monitoring Sync Status

```razor
<OfflineSyncIndicator />

@code {
    @inject IOfflineSyncService SyncService

    private int _pendingCount;

    protected override void OnInitialized()
    {
        SyncService.OnQueueChanged += UpdatePendingCount;
    }

    private void UpdatePendingCount(int count)
    {
        _pendingCount = count;
        StateHasChanged();
    }
}
```

### Manual Sync Trigger

```csharp
// Force sync attempt
var results = await SyncService.SyncNowAsync();

foreach (var result in results)
{
    if (result.Success)
        Snackbar.Add($"Synced: {result.BlueprintTitle}", Severity.Success);
    else
        Snackbar.Add($"Failed: {result.Error}", Severity.Error);
}
```

## Calculated Fields

### Adding a Calculated Field

```razor
<MudButton OnClick="OpenCalculationEditor">Add Calculation</MudButton>

@code {
    private async Task OpenCalculationEditor()
    {
        var dialog = await DialogService.ShowAsync<CalculationEditor>(
            "Calculated Field",
            new DialogParameters<CalculationEditor>
            {
                { x => x.AvailableFields, GetNumericFields() }
            });

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: CalculationModel calc })
        {
            AddCalculatedField(_currentAction, calc);
        }
    }
}
```

### Testing Calculations

The calculation editor includes a test panel:

1. Enter sample values for referenced fields
2. Click "Test"
3. View calculated result

## Migration from LocalStorage

On first load, existing LocalStorage blueprints are migrated:

```csharp
// This happens automatically in StorageService
// To manually trigger:
await Storage.MigrateLocalBlueprintsAsync();

// To clear local cache after confirming server sync:
await Storage.ClearLocalCacheAsync();
```

## Testing Components

```csharp
[Fact]
public void ParticipantEditor_ValidatesRequiredFields()
{
    var cut = RenderComponent<ParticipantEditor>();

    // Try to save without required fields
    cut.Find("button[data-testid='save-button']").Click();

    // Should show validation error
    cut.Find(".validation-error").Should().NotBeNull();
}

[Fact]
public void ConditionEditor_GeneratesValidJsonLogic()
{
    var cut = RenderComponent<ConditionEditor>(p => p
        .Add(x => x.AvailableFields, new[] { "/amount", "/type" }));

    // Add a clause
    cut.Find("[data-testid='field-select']").Change("/amount");
    cut.Find("[data-testid='operator-select']").Change(">");
    cut.Find("[data-testid='value-input']").Change("1000");
    cut.Find("[data-testid='save-button']").Click();

    var result = cut.Instance.GetConditionModel();
    result.ToJsonLogic().ToString().Should().Contain("\">\",");
}
```
