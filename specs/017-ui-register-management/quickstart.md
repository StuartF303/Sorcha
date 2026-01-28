# Quickstart: UI Register Management E2E Test Scenarios

**Feature**: 017-ui-register-management
**Date**: 2026-01-28
**Status**: Complete

## Overview

This document defines the end-to-end test scenarios for the UI Register Management feature. Tests use Playwright with the Docker test infrastructure.

## Test Setup

### Prerequisites

```bash
# Start Docker environment
docker-compose up -d

# Navigate to test project
cd tests/Sorcha.UI.E2E.Tests

# Run tests
dotnet test
```

### Test User

- **Username**: `admin@sorcha.local`
- **Password**: `Dev_Pass_2025!`
- **Role**: Administrator (has register creation permission)

### Base URL

- **Local Development**: `http://localhost:5171`
- **Docker**: `http://localhost:80`

## User Story 1: Register List (P1)

### Test File: `Tests/Registers/RegisterListTests.cs`

#### Scenario 1.1: View register list with multiple registers

```csharp
[Fact]
public async Task RegisterList_WithMultipleRegisters_DisplaysAllRegisters()
{
    // Given: Authenticated user with 5 registers
    await LoginAsAdminAsync();
    await EnsureRegistersExistAsync(5);

    // When: Navigate to Registers page
    await Page.GotoAsync("/registers");

    // Then: See all 5 registers with name, status, count, and time
    var cards = Page.Locator("[data-testid='register-card']");
    await Expect(cards).ToHaveCountAsync(5);

    // Verify each card has required elements
    var firstCard = cards.First;
    await Expect(firstCard.Locator("[data-testid='register-name']")).ToBeVisibleAsync();
    await Expect(firstCard.Locator("[data-testid='register-status']")).ToBeVisibleAsync();
    await Expect(firstCard.Locator("[data-testid='register-height']")).ToBeVisibleAsync();
    await Expect(firstCard.Locator("[data-testid='register-updated']")).ToBeVisibleAsync();
}
```

#### Scenario 1.2: Empty state with no registers

```csharp
[Fact]
public async Task RegisterList_WithNoRegisters_DisplaysEmptyState()
{
    // Given: Authenticated user with no registers
    await LoginAsNewUserAsync();

    // When: Navigate to Registers page
    await Page.GotoAsync("/registers");

    // Then: See empty state message with guidance
    await Expect(Page.Locator("[data-testid='empty-state']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='empty-state-message']"))
        .ToContainTextAsync("No registers found");
    await Expect(Page.Locator("[data-testid='create-register-hint']")).ToBeVisibleAsync();
}
```

#### Scenario 1.3: Navigate to register detail

```csharp
[Fact]
public async Task RegisterList_ClickRegisterCard_NavigatesToDetail()
{
    // Given: User viewing register list
    await LoginAsAdminAsync();
    await EnsureRegistersExistAsync(1);
    await Page.GotoAsync("/registers");

    // When: Click on a register card
    var firstCard = Page.Locator("[data-testid='register-card']").First;
    var registerId = await firstCard.GetAttributeAsync("data-register-id");
    await firstCard.ClickAsync();

    // Then: Navigated to register detail page
    await Expect(Page).ToHaveURLAsync(new Regex($"/registers/{registerId}"));
}
```

#### Scenario 1.4: Status badges display correctly

```csharp
[Fact]
public async Task RegisterList_DifferentStatuses_ShowDistinctBadges()
{
    // Given: Registers with different statuses
    await LoginAsAdminAsync();
    await EnsureRegisterWithStatusAsync("Online");
    await EnsureRegisterWithStatusAsync("Offline");

    // When: View the list
    await Page.GotoAsync("/registers");

    // Then: Each status has distinct visual styling
    var onlineBadge = Page.Locator("[data-testid='status-badge'][data-status='Online']");
    var offlineBadge = Page.Locator("[data-testid='status-badge'][data-status='Offline']");

    await Expect(onlineBadge).ToHaveClassAsync(/success/);
    await Expect(offlineBadge).ToHaveClassAsync(/default/);
}
```

## User Story 2: Register Details & Transactions (P1)

### Test File: `Tests/Registers/RegisterDetailTests.cs`

#### Scenario 2.1: View register with transactions

```csharp
[Fact]
public async Task RegisterDetail_WithTransactions_DisplaysMetadataAndList()
{
    // Given: Register with 50 transactions
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(50);

    // When: Navigate to detail page
    await Page.GotoAsync($"/registers/{registerId}");

    // Then: See register metadata and first page of transactions
    await Expect(Page.Locator("[data-testid='register-name']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='register-height']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='register-status']")).ToBeVisibleAsync();

    var transactionRows = Page.Locator("[data-testid='transaction-row']");
    await Expect(transactionRows).ToHaveCountAsync(20); // First page
}
```

#### Scenario 2.2: Load more transactions

```csharp
[Fact]
public async Task RegisterDetail_LoadMore_LoadsAdditionalTransactions()
{
    // Given: Register with 50 transactions, viewing first page
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(50);
    await Page.GotoAsync($"/registers/{registerId}");

    // When: Click "Load More"
    await Page.Locator("[data-testid='load-more-btn']").ClickAsync();

    // Then: Additional transactions loaded
    var transactionRows = Page.Locator("[data-testid='transaction-row']");
    await Expect(transactionRows).ToHaveCountAsync(40); // Two pages
}
```

#### Scenario 2.3: Real-time update notification

```csharp
[Fact]
public async Task RegisterDetail_NewTransaction_ShowsNotificationBanner()
{
    // Given: User viewing register detail with real-time enabled
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(10);
    await Page.GotoAsync($"/registers/{registerId}");

    // When: New transaction confirmed (simulated via API)
    await CreateTransactionAsync(registerId);
    await Page.WaitForTimeoutAsync(5000); // Wait for SignalR

    // Then: Notification banner appears
    await Expect(Page.Locator("[data-testid='new-tx-banner']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='new-tx-banner']"))
        .ToContainTextAsync("1 new transaction");
}
```

#### Scenario 2.4: Select transaction to view details

```csharp
[Fact]
public async Task RegisterDetail_ClickTransaction_OpensDetailPanel()
{
    // Given: User viewing transaction list
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(10);
    await Page.GotoAsync($"/registers/{registerId}");

    // When: Click on a transaction row
    await Page.Locator("[data-testid='transaction-row']").First.ClickAsync();

    // Then: Detail panel opens with full info
    await Expect(Page.Locator("[data-testid='transaction-detail-panel']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-id']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-sender']")).ToBeVisibleAsync();
}
```

## User Story 3: Transaction Details (P2)

### Test File: `Tests/Registers/TransactionDetailTests.cs`

#### Scenario 3.1: View all transaction fields

```csharp
[Fact]
public async Task TransactionDetail_AllFieldsDisplayed()
{
    // Given: Selected transaction in detail panel
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(1);
    await Page.GotoAsync($"/registers/{registerId}");
    await Page.Locator("[data-testid='transaction-row']").First.ClickAsync();

    // Then: All required fields visible
    await Expect(Page.Locator("[data-testid='tx-detail-id']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-timestamp']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-status']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-block']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-sender']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='tx-detail-signature']")).ToBeVisibleAsync();
}
```

#### Scenario 3.2: Copy wallet address to clipboard

```csharp
[Fact]
public async Task TransactionDetail_CopyAddress_CopiesAndShowsConfirmation()
{
    // Given: Transaction detail with long wallet address
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(1);
    await Page.GotoAsync($"/registers/{registerId}");
    await Page.Locator("[data-testid='transaction-row']").First.ClickAsync();

    // When: Click copy button
    await Page.Locator("[data-testid='copy-sender-btn']").ClickAsync();

    // Then: Visual confirmation shown (snackbar)
    await Expect(Page.Locator(".mud-snackbar")).ToContainTextAsync("Copied");
}
```

#### Scenario 3.3: Close detail panel

```csharp
[Fact]
public async Task TransactionDetail_ClickClose_ClosesPanel()
{
    // Given: Detail panel is open
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithTransactionsAsync(1);
    await Page.GotoAsync($"/registers/{registerId}");
    await Page.Locator("[data-testid='transaction-row']").First.ClickAsync();
    await Expect(Page.Locator("[data-testid='transaction-detail-panel']")).ToBeVisibleAsync();

    // When: Click close button
    await Page.Locator("[data-testid='close-detail-btn']").ClickAsync();

    // Then: Panel closes
    await Expect(Page.Locator("[data-testid='transaction-detail-panel']")).Not.ToBeVisibleAsync();
}
```

#### Scenario 3.4: Pending transaction shows status

```csharp
[Fact]
public async Task TransactionDetail_PendingTransaction_ShowsPendingStatus()
{
    // Given: Pending transaction (no block number)
    await LoginAsAdminAsync();
    var registerId = await EnsureRegisterWithPendingTransactionAsync();
    await Page.GotoAsync($"/registers/{registerId}");
    await Page.Locator("[data-testid='transaction-row']").First.ClickAsync();

    // Then: Shows Pending status, no block number
    await Expect(Page.Locator("[data-testid='tx-detail-status']"))
        .ToContainTextAsync("Pending");
    await Expect(Page.Locator("[data-testid='tx-detail-block']")).Not.ToBeVisibleAsync();
}
```

## User Story 4: Create Register (P2)

### Test File: `Tests/Registers/RegisterCreationTests.cs`

#### Scenario 4.1: Open creation wizard

```csharp
[Fact]
public async Task CreateRegister_ClickButton_OpensWizard()
{
    // Given: Authorized user on Registers page
    await LoginAsAdminAsync();
    await Page.GotoAsync("/registers");

    // When: Click "Create Register"
    await Page.Locator("[data-testid='create-register-btn']").ClickAsync();

    // Then: Multi-step wizard opens
    await Expect(Page.Locator("[data-testid='create-wizard']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='wizard-step-1']")).ToBeVisibleAsync();
}
```

#### Scenario 4.2: Enter valid name and proceed

```csharp
[Fact]
public async Task CreateRegister_ValidName_CanProceed()
{
    // Given: User in step 1 (Basic Info)
    await LoginAsAdminAsync();
    await Page.GotoAsync("/registers");
    await Page.Locator("[data-testid='create-register-btn']").ClickAsync();

    // When: Enter valid name (1-38 characters)
    await Page.Locator("[data-testid='register-name-input']").FillAsync("Test Register");

    // Then: Can proceed to next step
    await Expect(Page.Locator("[data-testid='next-step-btn']")).ToBeEnabledAsync();
    await Page.Locator("[data-testid='next-step-btn']").ClickAsync();
    await Expect(Page.Locator("[data-testid='wizard-step-2']")).ToBeVisibleAsync();
}
```

#### Scenario 4.3: Select wallet for signing

```csharp
[Fact]
public async Task CreateRegister_SelectWallet_CanProceed()
{
    // Given: User in step 2 (Select Wallet)
    await LoginAsAdminAsync();
    await EnsureWalletExistsAsync();
    await Page.GotoAsync("/registers");
    await Page.Locator("[data-testid='create-register-btn']").ClickAsync();
    await Page.Locator("[data-testid='register-name-input']").FillAsync("Test Register");
    await Page.Locator("[data-testid='next-step-btn']").ClickAsync();

    // When: Select a wallet
    await Page.Locator("[data-testid='wallet-select']").ClickAsync();
    await Page.Locator(".mud-list-item").First.ClickAsync();

    // Then: Can proceed to next step
    await Expect(Page.Locator("[data-testid='next-step-btn']")).ToBeEnabledAsync();
}
```

#### Scenario 4.4: Complete creation flow

```csharp
[Fact]
public async Task CreateRegister_CompleteFlow_RegisterAppears()
{
    // Given: User has completed all steps
    await LoginAsAdminAsync();
    await EnsureWalletExistsAsync();
    await Page.GotoAsync("/registers");

    var initialCount = await Page.Locator("[data-testid='register-card']").CountAsync();

    // Complete wizard steps
    await Page.Locator("[data-testid='create-register-btn']").ClickAsync();
    await Page.Locator("[data-testid='register-name-input']").FillAsync("E2E Test Register");
    await Page.Locator("[data-testid='next-step-btn']").ClickAsync();
    await Page.Locator("[data-testid='wallet-select']").ClickAsync();
    await Page.Locator(".mud-list-item").First.ClickAsync();
    await Page.Locator("[data-testid='next-step-btn']").ClickAsync();
    await Page.Locator("[data-testid='next-step-btn']").ClickAsync();

    // When: Click "Create" in review step
    await Page.Locator("[data-testid='create-btn']").ClickAsync();

    // Then: Wizard closes and new register appears
    await Expect(Page.Locator("[data-testid='create-wizard']")).Not.ToBeVisibleAsync();
    var newCount = await Page.Locator("[data-testid='register-card']").CountAsync();
    Assert.Equal(initialCount + 1, newCount);
}
```

#### Scenario 4.5: Handle creation error

```csharp
[Fact]
public async Task CreateRegister_NetworkError_ShowsRetryOption()
{
    // Given: User attempting to create (network will fail)
    await LoginAsAdminAsync();
    await EnsureWalletExistsAsync();
    await SimulateNetworkErrorAsync(); // Setup network interception

    // When: Error occurs during creation
    await Page.GotoAsync("/registers");
    await Page.Locator("[data-testid='create-register-btn']").ClickAsync();
    // ... complete steps ...
    await Page.Locator("[data-testid='create-btn']").ClickAsync();

    // Then: Error message and retry option shown
    await Expect(Page.Locator("[data-testid='creation-error']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='retry-btn']")).ToBeVisibleAsync();
}
```

## User Story 5: Filter & Search (P3)

### Test File: `Tests/Registers/RegisterFilterTests.cs`

#### Scenario 5.1: Search by name

```csharp
[Fact]
public async Task RegisterFilter_SearchByName_FiltersResults()
{
    // Given: User with 20 registers, one named "UniqueTestRegister"
    await LoginAsAdminAsync();
    await EnsureRegistersExistAsync(20);
    await EnsureRegisterExistsAsync("UniqueTestRegister");
    await Page.GotoAsync("/registers");

    // When: Type in search box
    await Page.Locator("[data-testid='register-search']").FillAsync("UniqueTest");

    // Then: Only matching registers shown
    var cards = Page.Locator("[data-testid='register-card']");
    await Expect(cards).ToHaveCountAsync(1);
    await Expect(cards.First.Locator("[data-testid='register-name']"))
        .ToContainTextAsync("UniqueTestRegister");
}
```

#### Scenario 5.2: Filter by status

```csharp
[Fact]
public async Task RegisterFilter_SelectOnlineStatus_ShowsOnlyOnline()
{
    // Given: Mix of Online and Offline registers
    await LoginAsAdminAsync();
    await EnsureRegisterWithStatusAsync("Online");
    await EnsureRegisterWithStatusAsync("Online");
    await EnsureRegisterWithStatusAsync("Offline");
    await Page.GotoAsync("/registers");

    // When: Select "Online" filter
    await Page.Locator("[data-testid='status-filter-Online']").ClickAsync();

    // Then: Only online registers displayed
    var cards = Page.Locator("[data-testid='register-card']");
    await Expect(cards).ToHaveCountAsync(2);
}
```

#### Scenario 5.3: Clear filters

```csharp
[Fact]
public async Task RegisterFilter_ClearFilters_ShowsAllRegisters()
{
    // Given: Active filters reducing results
    await LoginAsAdminAsync();
    await EnsureRegistersExistAsync(5);
    await Page.GotoAsync("/registers");
    await Page.Locator("[data-testid='register-search']").FillAsync("NonExistent");
    await Expect(Page.Locator("[data-testid='register-card']")).ToHaveCountAsync(0);

    // When: Clear filters
    await Page.Locator("[data-testid='clear-filters-btn']").ClickAsync();

    // Then: All registers displayed
    await Expect(Page.Locator("[data-testid='register-card']")).ToHaveCountAsync(5);
}
```

## User Story 6: Transaction Query (P3)

### Test File: `Tests/Registers/TransactionQueryTests.cs`

#### Scenario 6.1: Query by wallet address

```csharp
[Fact]
public async Task TransactionQuery_ValidWallet_ShowsResults()
{
    // Given: User on transaction query page
    await LoginAsAdminAsync();
    var walletAddress = await EnsureWalletWithTransactionsAsync();
    await Page.GotoAsync("/registers/query");

    // When: Enter wallet address and submit
    await Page.Locator("[data-testid='wallet-address-input']").FillAsync(walletAddress);
    await Page.Locator("[data-testid='query-btn']").ClickAsync();

    // Then: Matching transactions across registers shown
    await Expect(Page.Locator("[data-testid='query-results']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='query-result-row']").First).ToBeVisibleAsync();
}
```

#### Scenario 6.2: No results found

```csharp
[Fact]
public async Task TransactionQuery_NoMatches_ShowsNoResultsMessage()
{
    // Given: User querying with wallet that has no transactions
    await LoginAsAdminAsync();
    await Page.GotoAsync("/registers/query");

    // When: Query executed
    await Page.Locator("[data-testid='wallet-address-input']").FillAsync("srch1nonexistent123456");
    await Page.Locator("[data-testid='query-btn']").ClickAsync();

    // Then: "No results found" message
    await Expect(Page.Locator("[data-testid='no-results']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-testid='no-results']"))
        .ToContainTextAsync("No transactions found");
}
```

#### Scenario 6.3: Paginate results

```csharp
[Fact]
public async Task TransactionQuery_ManyResults_SupportsPagination()
{
    // Given: Query returns multiple pages
    await LoginAsAdminAsync();
    var walletAddress = await EnsureWalletWithManyTransactionsAsync(50);
    await Page.GotoAsync("/registers/query");

    // When: Execute query and scroll/page
    await Page.Locator("[data-testid='wallet-address-input']").FillAsync(walletAddress);
    await Page.Locator("[data-testid='query-btn']").ClickAsync();
    await Page.Locator("[data-testid='load-more-results']").ClickAsync();

    // Then: Additional results loaded
    var rows = Page.Locator("[data-testid='query-result-row']");
    await Expect(rows).ToHaveCountAsync(40);
}
```

## Accessibility Tests

### Keyboard Navigation

```csharp
[Fact]
public async Task Accessibility_KeyboardNavigation_AllElementsAccessible()
{
    await LoginAsAdminAsync();
    await EnsureRegistersExistAsync(3);
    await Page.GotoAsync("/registers");

    // Tab through elements
    await Page.Keyboard.PressAsync("Tab");
    await Expect(Page.Locator("[data-testid='create-register-btn']")).ToBeFocusedAsync();

    await Page.Keyboard.PressAsync("Tab");
    await Expect(Page.Locator("[data-testid='register-search']")).ToBeFocusedAsync();

    await Page.Keyboard.PressAsync("Tab");
    await Expect(Page.Locator("[data-testid='register-card']").First).ToBeFocusedAsync();

    // Enter to select
    await Page.Keyboard.PressAsync("Enter");
    await Expect(Page).ToHaveURLAsync(new Regex("/registers/"));
}
```

## Test Data Helpers

```csharp
private async Task EnsureRegistersExistAsync(int count)
{
    // Create test registers via API if needed
}

private async Task<string> EnsureRegisterWithTransactionsAsync(int transactionCount)
{
    // Create register and transactions, return register ID
}

private async Task EnsureWalletExistsAsync()
{
    // Ensure test user has at least one wallet
}

private async Task<string> EnsureWalletWithTransactionsAsync()
{
    // Create wallet with transactions, return wallet address
}
```
