# Research: Wallet Dashboard and Navigation Bugs

**Branch**: `033-fix-wallet-dashboard-bugs`
**Date**: 2026-02-13
**Status**: Complete

## Executive Summary

This research resolves all technical unknowns for fixing the dashboard wizard loop and navigation routing bugs. Key findings:

1. **Wallet type is NOT explicitly stored** - it's inferred from BIP44 derivation paths
2. **No default wallet preference** exists server-side or client-side
3. **Base href `/app/`** requires relative URLs in NavigationManager calls
4. **Dashboard lifecycle**: `OnInitializedAsync` is correct hook, but needs `IsLoaded` check
5. **Error handling**: Existing pattern uses `ServiceUnavailable` component for degraded service states

## Research Findings

### 1. Wallet Type Detection

**Question**: How does the Wallet Service distinguish between primary wallets (with seed phrase) and derived wallets (via derivation path)?

**Answer**:

#### Current Implementation

The system uses **hierarchical deterministic (HD) wallets** following BIP44 standard but does NOT explicitly track wallet type in `WalletDto`.

**Data Model**:
- `Wallet` entity = Primary wallet (stores encrypted root key)
  - Address field is the PRIMARY address (derived at `m/44'/0'/0'/0/0`)
  - Has Name, Algorithm, Status, CreatedAt
- `WalletAddress` entity = Derived addresses (child of Wallet)
  - Has DerivationPath (`m/44'/coin'/account'/change/index`)
  - Has IsChange (receive vs change address)
  - Has Index (position in sequence)

**System Derivation Paths** (`SorchaDerivationPaths.cs`):
- `m/44'/0'/0'/0/100` - Register Attestation
- `m/44'/0'/0'/0/101` - Register Control
- `m/44'/0'/0'/0/102` - Docket Signing

**Wallet Type Inference Logic**:
```csharp
// Primary wallet = Wallet entity itself (m/44'/0'/0'/0/0)
bool IsPrimary = wallet.Address == primaryDerivedAddress;

// System wallet = Uses system derivation paths
bool IsSystem = SorchaDerivationPaths.IsSystemPath(derivationPath);

// Derived wallet = WalletAddress entity with custom path
bool IsDerived = walletAddress != null && walletAddress.DerivationPath != null;
```

**API Methods Available**:
- `IWalletApiService.GetWalletsAsync()` - Returns all `Wallet` entities (primary wallets only)
- `IWalletApiService.GetAddressesAsync(address)` - Returns derived `WalletAddress` entities

**Decision**: For dashboard wizard detection, we only need to check if `GetWalletsAsync()` returns any wallets. The distinction between primary/derived/system is NOT required for this bug fix.

---

### 2. Default Wallet Preference

**Question**: Where is the default wallet preference stored?

**Answer**: **It is NOT currently stored anywhere.**

#### Investigation Results

**Server-Side**: No default wallet preference in Wallet Service
- `WalletDto` has no `IsDefault` field
- No `/api/wallets/default` endpoint exists
- No user preference table for wallet selection

**Client-Side**: Blazored.LocalStorage is available but not used for wallet preference
- `Program.cs` registers `Blazored.LocalStorage` (line 32)
- Used for blueprint drafts and schema cache
- **No `IUserPreferenceService` or similar service found**

**Temporary State**: `RegisterCreationState` model stores in-session wallet selection
- `SelectedWalletAddress` - Current wizard selection
- `AvailableWallets` - List for dropdown
- NOT persisted across sessions

**Decision**: Default wallet preference is OUT OF SCOPE for this bug fix. The spec's reference to "check for default wallet" is a misunderstanding - we only need to check if ANY wallet exists, not a specific default.

**Recommendation for Future**: If default wallet feature is needed, implement client-side using Blazored.LocalStorage:
```csharp
await localStorage.SetItemAsync("defaultWalletAddress", address);
var defaultAddress = await localStorage.GetItemAsync<string>("defaultWalletAddress");
```

---

### 3. Blazor Navigation Base Href

**Question**: Best practices for navigation in Blazor WASM with base href `/app/`?

**Answer**: Use **relative URLs** in `NavigationManager.NavigateTo()` to respect base href.

#### How Base Href Works

**Configuration** (`wwwroot/app/index.html` line 7):
```html
<base href="/app/" />
```

**Blazor Behavior**:
- **Relative URLs** (no leading `/`): Automatically prepend base href
  - `NavigateTo("wallets/123")` → `/app/wallets/123` ✅
- **Absolute URLs** (leading `/`): Ignore base href
  - `NavigateTo("/wallets/123")` → `/wallets/123` ❌ (404 error!)
- **Full URLs** (with scheme): Used as-is
  - `NavigateTo("https://example.com")` → external navigation

**MudBlazor Href Attribute**:
- `<MudButton Href="wallets">` - Uses relative path ✅
- `<MudButton Href="/wallets">` - Ignores base href ❌

**Current Bug**:
```csharp
// WRONG - ignores base href
Navigation.NavigateTo($"/wallets/{wallet.Address}");

// CORRECT - respects base href
Navigation.NavigateTo($"wallets/{wallet.Address}");
```

**Decision**: Fix MyWallet.razor line 134 by removing leading `/` to make path relative.

---

### 4. Dashboard Loading Lifecycle

**Question**: Is `OnInitializedAsync` the right hook for wallet detection? Why does it run on every dashboard load?

**Answer**: `OnInitializedAsync` is **correct** but needs defensive checks.

#### Blazor Component Lifecycle

**`OnInitializedAsync()`**:
- Runs once per component instantiation
- Ideal for data loading
- **Problem**: If dashboard is the home page (`/` or `/dashboard`), it runs EVERY time user navigates there

**`OnAfterRenderAsync()`**:
- Runs after component renders
- Not suitable for navigation logic (can cause render loops)

**`OnParametersSetAsync()`**:
- Runs when route parameters change
- Not applicable (dashboard has no parameters)

**Current Implementation Issue** (`Home.razor` lines 180-189):
```csharp
protected override async Task OnInitializedAsync()
{
    _stats = await DashboardService.GetDashboardStatsAsync();
    _isLoading = false;

    if (_stats.IsLoaded && _stats.TotalWallets == 0)  // ❌ MISSING IsLoaded check on old code
    {
        Navigation.NavigateTo("wallets/create?first-login=true");
        return;
    }
}
```

**Original Bug**:
- Line 185 checks `_stats.TotalWallets == 0` WITHOUT checking `_stats.IsLoaded`
- If API fails, `_stats.IsLoaded == false` AND `TotalWallets == 0` (default)
- Dashboard redirects to wizard even though user may have wallets
- User creates another wallet → returns to dashboard → stats fail again → redirects AGAIN (loop!)

**Fix**:
```csharp
if (_stats.IsLoaded && _stats.TotalWallets == 0)  // ✅ Only redirect if stats successfully loaded
{
    Navigation.NavigateTo("wallets/create?first-login=true");
    return;
}
```

**Decision**: Keep `OnInitializedAsync` but add `_stats.IsLoaded` check before redirecting.

---

### 5. Wallet Service Availability Handling

**Question**: How should dashboard handle Wallet Service being unavailable?

**Answer**: Use existing `ServiceUnavailable` component pattern.

#### Existing Error Handling Patterns

**MyWallet.razor** (lines 35-38):
```razor
@if (_serviceError)
{
    <ServiceUnavailable ServiceName="Wallet Service" OnRetry="LoadWalletsAsync" />
}
```

**ServiceUnavailable Component** (`Sorcha.UI.Core/Components/Shared/ServiceUnavailable.razor`):
- Shows error icon and message
- Provides "Retry" button
- Graceful degradation without blocking UI

**DashboardService.GetDashboardStatsAsync()** (lines 26-44):
```csharp
try
{
    var response = await _httpClient.GetAsync("/api/dashboard", cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("Failed to fetch dashboard stats: {StatusCode}", response.StatusCode);
        return new DashboardStatsViewModel { IsLoaded = false };  // ✅ Sets IsLoaded = false
    }
    // ... success path
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error fetching dashboard statistics");
    return new DashboardStatsViewModel { IsLoaded = false };  // ✅ Sets IsLoaded = false
}
```

**Decision**:
1. Keep existing error handling in `DashboardService`
2. Add `IsLoaded` check in `Home.razor` to prevent false redirects
3. Optionally show warning message when stats fail to load (see plan.md Phase 2A)

---

## Best Practices Research

### Blazor WASM State Management

**Question**: Should we persist "wizard already shown" state?

**Answer**: **No, not needed.** The fix is to check `IsLoaded` before redirecting, not to track wizard state.

**Rationale**:
- Wizard should show when `TotalWallets == 0` AND `IsLoaded == true`
- If user creates a wallet, `TotalWallets` becomes 1, wizard won't show
- If user deletes all wallets, wizard SHOULD reappear (expected behavior)
- No need for "wizard shown" flag

**If state persistence was needed** (future):
- **Local Storage**: `Blazored.LocalStorage` for cross-session
- **Scoped Service**: For within-session only
- **Cascading Parameter**: For component tree sharing

### MudBlazor Navigation Consistency

**Question**: `Href` attribute vs `NavigationManager.NavigateTo()`?

**Answer**: Use `Href` for static links, `NavigateTo()` for dynamic/conditional navigation.

**MudBlazor Buttons**:
```razor
@* Static navigation - use Href *@
<MudButton Href="wallets/create" Color="Color.Primary">Create Wallet</MudButton>

@* Dynamic navigation - use NavigationManager *@
<MudButton OnClick="() => NavigateToWallet(wallet.Address)">View Wallet</MudButton>

@code {
    private void NavigateToWallet(string address)
    {
        Navigation.NavigateTo($"wallets/{address}");  // Relative path
    }
}
```

**Both respect base href** when using relative paths.

### E2E Test Patterns

**Question**: How to simulate first-time vs returning user?

**Answer**: Use Playwright's browser context isolation.

**Existing Test Infrastructure** (`tests/Sorcha.UI.E2E.Tests/Fixtures/AuthenticatedDockerTestBase.cs`):
- Creates fresh browser context per test
- Handles authentication flow
- Provides `Page` object for navigation

**Test Pattern for Wallet Wizard**:
```csharp
[Test]
public async Task FirstLogin_NoWallets_ShowsWizard()
{
    // Arrange: Login as fresh user (no wallets)
    await LoginAsNewUser();

    // Act: Navigate to dashboard
    await Page.GotoAsync("/app/dashboard");

    // Assert: Should redirect to wizard
    await Expect(Page).ToHaveURLAsync(new Regex(@"/app/wallets/create\?first-login=true"));
}

[Test]
public async Task ExistingWallet_DashboardLoad_SkipsWizard()
{
    // Arrange: Login and create wallet
    await LoginAsNewUser();
    await CreateWallet("Test Wallet");

    // Act: Navigate to dashboard
    await Page.GotoAsync("/app/dashboard");

    // Assert: Should stay on dashboard
    await Expect(Page).ToHaveURLAsync(new Regex(@"/app/dashboard"));
    await Expect(Page.GetByText("Welcome back")).ToBeVisibleAsync();
}
```

---

## Decisions Summary

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Wallet Type Tracking | Use `GetWalletsAsync().Count > 0` for detection | Simpler than checking wallet types; meets requirement |
| Default Wallet | OUT OF SCOPE | Not currently implemented; not needed for bug fix |
| Navigation URLs | Use relative paths (remove leading `/`) | Respects base href `/app/` |
| Dashboard Lifecycle | Keep `OnInitializedAsync`, add `IsLoaded` check | Correct hook; fix is defensive check |
| Error Handling | Use existing `ServiceUnavailable` pattern | Consistent with other pages |
| State Persistence | NOT NEEDED | `TotalWallets` check is sufficient |

---

## Next Steps

All research complete. Ready for **Phase 1: Design & Contracts**.

### Remaining Artifacts to Generate

1. **data-model.md** - Document wallet entities and detection logic
2. **contracts/WalletDetectionService.http** - HTTP test examples
3. **quickstart.md** - Testing guide for developers
4. **Update agent context** - Run update script
