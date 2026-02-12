# Data Model: UI Register Management

**Feature**: 017-ui-register-management
**Date**: 2026-01-28
**Status**: Complete

## Overview

This document defines the new and enhanced ViewModels for the UI Register Management feature. All models are C# records for immutability and follow the existing patterns in `Sorcha.UI.Core/Models/Registers/`.

## New Models

### RegisterFilterState

**File**: `Sorcha.UI.Core/Models/Registers/RegisterFilterState.cs`

**Purpose**: Manages the state of search and filter controls on the register list page.

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Registers;

/// <summary>
/// State for register list filtering and search.
/// </summary>
public record RegisterFilterState
{
    /// <summary>
    /// Text to search for in register names.
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Selected status filters. Empty means all statuses shown.
    /// </summary>
    public IReadOnlySet<RegisterStatus> SelectedStatuses { get; init; } =
        new HashSet<RegisterStatus>();

    /// <summary>
    /// Returns true if any filters are active.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) || SelectedStatuses.Count > 0;

    /// <summary>
    /// Applies filters to a collection of registers.
    /// </summary>
    public IEnumerable<RegisterViewModel> Apply(IEnumerable<RegisterViewModel> registers)
    {
        var filtered = registers;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(r =>
                r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatuses.Count > 0)
        {
            filtered = filtered.Where(r => SelectedStatuses.Contains(r.Status));
        }

        return filtered;
    }

    /// <summary>
    /// Returns a new state with updated search text.
    /// </summary>
    public RegisterFilterState WithSearchText(string text) =>
        this with { SearchText = text };

    /// <summary>
    /// Returns a new state with toggled status filter.
    /// </summary>
    public RegisterFilterState ToggleStatus(RegisterStatus status)
    {
        var newStatuses = new HashSet<RegisterStatus>(SelectedStatuses);
        if (!newStatuses.Remove(status))
        {
            newStatuses.Add(status);
        }
        return this with { SelectedStatuses = newStatuses };
    }

    /// <summary>
    /// Returns a new state with all filters cleared.
    /// </summary>
    public RegisterFilterState Clear() => new();
}
```

### TransactionQueryState

**File**: `Sorcha.UI.Core/Models/Registers/TransactionQueryState.cs`

**Purpose**: Manages the state of the cross-register transaction query form.

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Registers;

/// <summary>
/// State for cross-register transaction query form.
/// </summary>
public record TransactionQueryState
{
    /// <summary>
    /// Wallet address to search for.
    /// </summary>
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>
    /// Whether a query is currently executing.
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Query results. Null if query not yet executed.
    /// </summary>
    public IReadOnlyList<TransactionQueryResult>? Results { get; init; }

    /// <summary>
    /// Returns true if wallet address is valid for query.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(WalletAddress) &&
        WalletAddress.Length >= 26 &&
        WalletAddress.Length <= 58;

    /// <summary>
    /// Returns true if can submit query.
    /// </summary>
    public bool CanSubmit => IsValid && !IsLoading;

    /// <summary>
    /// Returns true if results are empty.
    /// </summary>
    public bool HasNoResults => Results is { Count: 0 };

    /// <summary>
    /// Returns true if has results to display.
    /// </summary>
    public bool HasResults => Results is { Count: > 0 };

    /// <summary>
    /// Returns a new state with updated wallet address.
    /// </summary>
    public TransactionQueryState WithWalletAddress(string address) =>
        this with { WalletAddress = address, ErrorMessage = null };

    /// <summary>
    /// Returns a new state indicating loading started.
    /// </summary>
    public TransactionQueryState StartLoading() =>
        this with { IsLoading = true, ErrorMessage = null };

    /// <summary>
    /// Returns a new state with query results.
    /// </summary>
    public TransactionQueryState WithResults(IReadOnlyList<TransactionQueryResult> results) =>
        this with { IsLoading = false, Results = results, ErrorMessage = null };

    /// <summary>
    /// Returns a new state with error.
    /// </summary>
    public TransactionQueryState WithError(string error) =>
        this with { IsLoading = false, ErrorMessage = error };

    /// <summary>
    /// Returns a new state cleared for new query.
    /// </summary>
    public TransactionQueryState Clear() => new();
}

/// <summary>
/// Single result from cross-register transaction query.
/// </summary>
public record TransactionQueryResult
{
    /// <summary>
    /// The transaction data.
    /// </summary>
    public required TransactionViewModel Transaction { get; init; }

    /// <summary>
    /// Register name for context (not just ID).
    /// </summary>
    public required string RegisterName { get; init; }

    /// <summary>
    /// Register ID for navigation.
    /// </summary>
    public required string RegisterId { get; init; }
}
```

### WalletViewModel

**File**: `Sorcha.UI.Core/Models/Registers/WalletViewModel.cs`

**Purpose**: Represents a wallet for selection in the register creation wizard.

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Registers;

/// <summary>
/// Wallet information for selection dropdown in register creation wizard.
/// </summary>
public record WalletViewModel
{
    /// <summary>
    /// Wallet unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Wallet address (Base58 encoded).
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// User-friendly wallet name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Cryptographic algorithm (ED25519, P-256, etc.).
    /// </summary>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Whether this wallet can be used for signing.
    /// </summary>
    public bool CanSign { get; init; } = true;

    /// <summary>
    /// Truncated address for display (first 8...last 4).
    /// </summary>
    public string AddressTruncated =>
        Address.Length > 16
            ? $"{Address[..8]}...{Address[^4..]}"
            : Address;

    /// <summary>
    /// Display text for dropdown.
    /// </summary>
    public string DisplayText => $"{Name} ({AddressTruncated})";
}
```

## Enhanced Models

### RegisterCreationState (Enhanced)

**File**: `Sorcha.UI.Core/Models/Registers/RegisterCreationState.cs`

**Changes**: Add wallet selection step (new Step 2), adjust step numbering.

**New Properties**:
```csharp
/// <summary>
/// Selected wallet ID for signing attestation.
/// </summary>
public string? SelectedWalletId { get; init; }

/// <summary>
/// Selected wallet details for display.
/// </summary>
public WalletViewModel? SelectedWallet { get; init; }

/// <summary>
/// Available wallets for selection.
/// </summary>
public IReadOnlyList<WalletViewModel> AvailableWallets { get; init; } = [];
```

**Updated Computed Properties**:
```csharp
/// <summary>
/// Total number of steps (now 4: Name, Wallet, Options, Review).
/// </summary>
public const int TotalSteps = 4;

/// <summary>
/// Whether wallet selection is valid (Step 2).
/// </summary>
public bool IsWalletValid => SelectedWalletId is not null;

/// <summary>
/// Whether current step can proceed.
/// </summary>
public bool CanProceed => CurrentStep switch
{
    1 => IsNameValid,
    2 => IsWalletValid,
    3 => true, // Options step always valid
    4 => !IsProcessing,
    _ => false
};

/// <summary>
/// Human-readable step name.
/// </summary>
public string CurrentStepName => CurrentStep switch
{
    1 => "Register Name",
    2 => "Select Wallet",
    3 => "Options",
    4 => "Review & Create",
    _ => "Unknown"
};
```

**New Methods**:
```csharp
/// <summary>
/// Returns a new state with selected wallet.
/// </summary>
public RegisterCreationState WithWallet(WalletViewModel wallet) =>
    this with { SelectedWalletId = wallet.Id, SelectedWallet = wallet };

/// <summary>
/// Returns a new state with available wallets loaded.
/// </summary>
public RegisterCreationState WithAvailableWallets(IReadOnlyList<WalletViewModel> wallets) =>
    this with { AvailableWallets = wallets };
```

## Entity Relationships

```
RegisterFilterState
├── SearchText (string)
├── SelectedStatuses (IReadOnlySet<RegisterStatus>)
└── Apply(registers) → filtered registers

TransactionQueryState
├── WalletAddress (string)
├── IsLoading (bool)
├── ErrorMessage (string?)
└── Results (IReadOnlyList<TransactionQueryResult>?)
    └── TransactionQueryResult
        ├── Transaction (TransactionViewModel)
        ├── RegisterName (string)
        └── RegisterId (string)

WalletViewModel
├── Id (string)
├── Address (string)
├── Name (string)
├── Algorithm (string)
└── CanSign (bool)

RegisterCreationState (Enhanced)
├── CurrentStep (int) [1-4 now]
├── Name (string)
├── SelectedWalletId (string?) [NEW]
├── SelectedWallet (WalletViewModel?) [NEW]
├── AvailableWallets (IReadOnlyList<WalletViewModel>) [NEW]
├── Advertise (bool)
├── IsFullReplica (bool)
├── TenantId (string)
├── RegisterId (string?)
├── UnsignedControlRecord (string?)
├── SignedControlRecord (string?)
├── IsProcessing (bool)
└── ErrorMessage (string?)
```

## Usage Examples

### RegisterFilterState

```csharp
// Initialize filter state
private RegisterFilterState _filterState = new();

// Handle search text change
private void OnSearchChanged(string text)
{
    _filterState = _filterState.WithSearchText(text);
    UpdateFilteredList();
}

// Handle status chip toggle
private void OnStatusToggle(RegisterStatus status)
{
    _filterState = _filterState.ToggleStatus(status);
    UpdateFilteredList();
}

// Apply filters to register list
private void UpdateFilteredList()
{
    _filteredRegisters = _filterState.Apply(_allRegisters).ToList();
}
```

### TransactionQueryState

```csharp
// Initialize query state
private TransactionQueryState _queryState = new();

// Handle wallet address input
private void OnWalletAddressChanged(string address)
{
    _queryState = _queryState.WithWalletAddress(address);
}

// Execute query
private async Task ExecuteQuery()
{
    _queryState = _queryState.StartLoading();

    try
    {
        var results = await TransactionService.QueryByWalletAsync(_queryState.WalletAddress);
        _queryState = _queryState.WithResults(results);
    }
    catch (Exception ex)
    {
        _queryState = _queryState.WithError($"Query failed: {ex.Message}");
    }
}
```

### WalletViewModel in Wizard

```csharp
// Load wallets on wizard open
protected override async Task OnInitializedAsync()
{
    var wallets = await WalletService.GetWalletsAsync();
    _creationState = _creationState.WithAvailableWallets(wallets);
}

// Handle wallet selection
private void OnWalletSelected(WalletViewModel wallet)
{
    _creationState = _creationState.WithWallet(wallet);
}
```
