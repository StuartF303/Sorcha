# Research: UI Register Management

**Feature**: 017-ui-register-management
**Date**: 2026-01-28
**Status**: Complete

## 1. Existing Component Analysis

### Register Components (Sorcha.UI.Core/Components/Registers/)

| Component | Status | Spec Coverage | Enhancements Needed |
|-----------|--------|---------------|---------------------|
| CreateRegisterWizard.razor | Exists | Partial (US4) | Add wallet selection step, progress indication |
| RegisterCard.razor | Exists | Full (US1) | No changes needed |
| RegisterStatusBadge.razor | Exists | Full (US1) | No changes needed |
| TransactionList.razor | Exists | Full (US2) | No changes needed |
| TransactionRow.razor | Exists | Full (US2) | No changes needed |
| TransactionDetail.razor | Exists | Partial (US3) | Add copy-to-clipboard functionality |
| RealTimeIndicator.razor | Exists | Full (US2) | No changes needed |
| RegisterSearchBar.razor | **Missing** | None (US5) | NEW: Create for search/filter |
| TransactionQueryForm.razor | **Missing** | None (US6) | NEW: Create for wallet query |

### Pages (Sorcha.UI.Web.Client/Pages/Registers/)

| Page | Status | Spec Coverage | Enhancements Needed |
|------|--------|---------------|---------------------|
| Index.razor | Exists | Partial (US1, US5) | Add search bar, status filter, empty state |
| Detail.razor | Exists | Full (US2) | Already has real-time updates, transaction detail |
| Query.razor | **Missing** | None (US6) | NEW: Create for cross-register query |

### Services (Sorcha.UI.Core/Services/)

| Service | Status | Spec Coverage | Enhancements Needed |
|---------|--------|---------------|---------------------|
| IRegisterService.cs | Exists | Full | No changes needed |
| RegisterService.cs | Exists | Full | No changes needed |
| ITransactionService.cs | Exists | Partial (US6) | Add QueryByWalletAsync method |
| TransactionService.cs | Exists | Partial (US6) | Add QueryByWalletAsync implementation |
| RegisterHubConnection.cs | Exists | Full | No changes needed |
| IWalletService.cs | **Missing** | None (US4) | Needed for wallet selection in wizard |

### Models (Sorcha.UI.Core/Models/Registers/)

| Model | Status | Spec Coverage | Enhancements Needed |
|-------|--------|---------------|---------------------|
| RegisterViewModel.cs | Exists | Full | No changes needed |
| TransactionViewModel.cs | Exists | Full | No changes needed |
| ConnectionState.cs | Exists | Full | No changes needed |
| RegisterCreationState.cs | Exists | Partial (US4) | Add SelectedWalletId property |
| TransactionListResponse.cs | Exists | Full | No changes needed |
| RegisterFilterState.cs | **Missing** | None (US5) | NEW: For search/filter state |
| TransactionQueryState.cs | **Missing** | None (US6) | NEW: For query form state |
| WalletViewModel.cs | **Missing** | None (US4) | NEW: For wallet selection dropdown |

## 2. API Endpoints Analysis

### Existing Endpoints (via API Gateway)

| Endpoint | Method | Service | Used By | Status |
|----------|--------|---------|---------|--------|
| `/api/registers` | GET | Register Service | Index.razor | ✅ Exists |
| `/api/registers/{id}` | GET | Register Service | Detail.razor | ✅ Exists |
| `/api/registers/initiate` | POST | Register Service | CreateRegisterWizard | ✅ Exists |
| `/api/registers/finalize` | POST | Register Service | CreateRegisterWizard | ✅ Exists |
| `/api/registers/{id}/transactions` | GET | Register Service | Detail.razor | ✅ Exists |
| `/api/registers/{id}/transactions/{txId}` | GET | Register Service | TransactionDetail | ✅ Exists |

### Required Endpoints (need verification)

| Endpoint | Method | Purpose | User Story |
|----------|--------|---------|------------|
| `/api/registers?search={term}` | GET | Search by name | US5 |
| `/api/registers?status={status}` | GET | Filter by status | US5 |
| `/api/transactions/query?wallet={address}` | GET | Query by wallet | US6 |
| `/api/wallets` | GET | List user wallets | US4 |

### SignalR Hub Events (RegisterHubConnection)

| Event | Direction | Data | Used By | Status |
|-------|-----------|------|---------|--------|
| SubscribeToRegister | Client→Server | registerId | Detail.razor | ✅ Exists |
| UnsubscribeFromRegister | Client→Server | registerId | Detail.razor | ✅ Exists |
| OnTransactionConfirmed | Server→Client | (registerId, txId) | Detail.razor | ✅ Exists |
| OnRegisterCreated | Server→Client | (registerId, name) | Index.razor | ✅ Exists |
| OnRegisterHeightUpdated | Server→Client | (registerId, height) | Detail.razor | ✅ Exists |
| OnConnectionStateChanged | Local Event | ConnectionState | RealTimeIndicator | ✅ Exists |

## 3. Gap Analysis

### Gap 1: Wallet Selection in Register Creation (US4)

**Current State**: CreateRegisterWizard has 3 steps (Name → Options → Review) but does not include wallet selection. The finalize step passes the unsigned control record directly without wallet signing.

**Required State**: Need 4 steps (Name → Select Wallet → Options → Review & Sign) with actual wallet integration for signing the attestation.

**Solution**:
1. Add IWalletService dependency for listing user wallets
2. Create WalletViewModel for dropdown display
3. Add WalletSelection step to wizard (Step 2)
4. Enhance RegisterCreationState with SelectedWalletId
5. Call wallet signing API during finalize

### Gap 2: Copy-to-Clipboard in Transaction Detail (US3)

**Current State**: TransactionDetail displays all fields but lacks copy functionality.

**Required State**: Click-to-copy for TxId, wallet addresses, and signature.

**Solution**:
1. Add IJSRuntime for clipboard API access
2. Create CopyButton component or use MudIconButton with copy icon
3. Add visual feedback (snackbar notification)
4. Implement copy handler for each copyable field

### Gap 3: Register Search/Filter (US5)

**Current State**: Index.razor displays all registers without filtering capability.

**Required State**: Text search by name, status filter chips.

**Solution**:
1. Create RegisterSearchBar component
2. Create RegisterFilterState model
3. Implement client-side filtering (registers already loaded)
4. Add MudChipSet for status filters
5. Add MudTextField for search input

### Gap 4: Cross-Register Transaction Query (US6)

**Current State**: Transaction viewing is register-scoped only.

**Required State**: Query transactions by wallet address across all accessible registers.

**Solution**:
1. Create Query.razor page at `/registers/query`
2. Create TransactionQueryForm component
3. Create TransactionQueryState model
4. Add QueryByWalletAsync to ITransactionService
5. Display results in reusable TransactionList

### Gap 5: Empty State Messaging (US1)

**Current State**: Index.razor may not handle empty register list gracefully.

**Required State**: Clear empty state message with guidance to create first register.

**Solution**:
1. Add empty state check in Index.razor
2. Display MudAlert with helpful message
3. Include "Create Register" CTA button (if authorized)

## 4. Technical Decisions

### Decision 1: Client-Side vs Server-Side Filtering

**Choice**: Client-side filtering for registers (US5)

**Rationale**:
- Registers are already loaded for display
- Expected volume is < 100 registers per user
- Instant filtering without API calls
- Consistent with MudBlazor patterns

### Decision 2: Copy-to-Clipboard Implementation

**Choice**: JavaScript interop via IJSRuntime

**Rationale**:
- Blazor WASM requires JS for clipboard access
- MudBlazor doesn't have built-in copy component
- Pattern already used elsewhere in application

### Decision 3: Wallet Service Integration

**Choice**: Add IWalletService to UI, reuse existing backend endpoint

**Rationale**:
- Wallet list endpoint already exists in Wallet Service
- Need wallet ID and address for selection
- Signing will be done by calling wallet service sign endpoint

### Decision 4: Query Results Display

**Choice**: Reuse TransactionList component

**Rationale**:
- Consistent UI for transaction display
- Pagination already implemented
- Only need to add register name column for context

## 5. Existing Code Patterns

### MudBlazor Dialog Pattern (from CreateRegisterWizard)

```csharp
[CascadingParameter]
private IMudDialogInstance? MudDialog { get; set; }

private void Cancel() => MudDialog?.Cancel();
private void Submit(RegisterViewModel result) => MudDialog?.Close(DialogResult.Ok(result));
```

### Service Injection Pattern (from pages)

```csharp
@inject IRegisterService RegisterService
@inject ITransactionService TransactionService
@inject RegisterHubConnection HubConnection
@inject IDialogService DialogService
@inject ISnackbar Snackbar
```

### SignalR Subscription Pattern (from Detail.razor)

```csharp
protected override async Task OnInitializedAsync()
{
    HubConnection.OnTransactionConfirmed += OnTransactionConfirmedAsync;
    await HubConnection.StartAsync();
    await HubConnection.SubscribeToRegisterAsync(RegisterId);
}

public async ValueTask DisposeAsync()
{
    HubConnection.OnTransactionConfirmed -= OnTransactionConfirmedAsync;
    await HubConnection.UnsubscribeFromRegisterAsync(RegisterId);
}
```

### Pagination Pattern (from TransactionService)

```csharp
public async Task<TransactionListResponse> GetTransactionsAsync(
    string registerId, int page = 1, int pageSize = 20)
{
    var response = await _httpClient.GetAsync(
        $"api/registers/{registerId}/transactions?page={page}&pageSize={pageSize}");
    // ...
}
```

## 6. Dependencies

### NuGet Packages (already installed)

- MudBlazor (8.x) - UI component library
- Microsoft.AspNetCore.SignalR.Client - Real-time updates
- Microsoft.AspNetCore.Components.WebAssembly.Authentication - Auth

### Service Dependencies

- Register Service (via API Gateway)
- Transaction Service (via API Gateway)
- Wallet Service (via API Gateway) - **NEW dependency for US4**
- SignalR Hub (`/hubs/register`)

## 7. Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Wallet signing in WASM | High | Medium | Use backend signing via API call |
| API query endpoint missing | Medium | Low | Verify endpoints exist, document if not |
| Mobile layout issues | Low | Medium | Test responsive breakpoints early |
| SignalR connection instability | Medium | Low | Existing reconnection logic handles this |

## 8. Recommendations

1. **Start with US1 & US2** (P1) - These are mostly verification and minor enhancements
2. **US4 wallet integration** requires backend verification - ensure signing endpoint works
3. **US5 & US6 can be parallel** - Independent components with no overlap
4. **E2E tests should use Docker test infrastructure** - Consistent with existing Playwright tests
