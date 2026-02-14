# Data Model: Wallet Dashboard and Navigation

**Branch**: `033-fix-wallet-dashboard-bugs`
**Date**: 2026-02-13

## Overview

This document describes the data entities involved in wallet detection and dashboard navigation. These are **existing models** that will be used (not modified) for the bug fixes.

## Entities

### WalletDto

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Wallet/WalletDto.cs`

Represents a user's cryptographic wallet in the UI layer.

```csharp
public record WalletDto
{
    public required string Address { get; init; }
    public required string Name { get; init; }
    public required string PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public WalletStatus Status { get; init; }
    public required string Owner { get; init; }
    public required string Tenant { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
```

**Fields**:
- **Address** (string, required): Unique wallet identifier (Bech32 encoded public key hash). Primary key.
- **Name** (string, required): User-friendly display name (e.g., "My Main Wallet", "Trading Wallet")
- **PublicKey** (string, required): Hex or Base64 encoded public key
- **Algorithm** (string, required): Cryptographic algorithm - "ED25519", "NISTP256", or "RSA4096"
- **Status** (enum): Current wallet state - Active, Archived, Deleted, or Locked
- **Owner** (string, required): User ID from authentication system (JWT sub claim)
- **Tenant** (string, required): Multi-tenant organization identifier
- **CreatedAt** (DateTimeOffset): Timestamp when wallet was created
- **UpdatedAt** (DateTimeOffset): Timestamp of last modification
- **Metadata** (Dictionary<string, string>, nullable): Custom key-value pairs for extensibility

**Relationships**:
- Owned by one User (via Owner field)
- Belongs to one Tenant (via Tenant field)
- Can have many derived WalletAddress entities (not exposed in this DTO)

**Usage in Bug Fix**:
- Dashboard checks `List<WalletDto>.Count > 0` to determine if wizard should show
- MyWallet page displays list of WalletDto and navigates to detail page per wallet

---

### DashboardStatsViewModel

**Location**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Dashboard/DashboardStatsViewModel.cs`

Aggregated statistics displayed on the dashboard homepage.

```csharp
public record DashboardStatsViewModel
{
    public int ActiveBlueprints { get; init; }
    public int TotalWallets { get; init; }
    public int RecentTransactions { get; init; }
    public int ConnectedPeers { get; init; }
    public int ActiveRegisters { get; init; }
    public int TotalOrganizations { get; init; }
    public bool IsLoaded { get; init; }
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
```

**Fields**:
- **ActiveBlueprints** (int): Count of active workflow blueprints
- **TotalWallets** (int): **KEY FIELD** - Count of user's wallets. Used for wizard detection.
- **RecentTransactions** (int): Count of recent register transactions
- **ConnectedPeers** (int): Count of connected P2P network peers
- **ActiveRegisters** (int): Count of active distributed ledger registers
- **TotalOrganizations** (int): Count of multi-tenant organizations
- **IsLoaded** (bool): **CRITICAL** - `true` if stats loaded successfully, `false` if API failed
- **LastUpdated** (DateTimeOffset): Timestamp when stats were fetched

**Usage in Bug Fix**:
- Dashboard checks `IsLoaded == true AND TotalWallets == 0` before redirecting to wizard
- **Old Bug**: Missing `IsLoaded` check caused false redirects on API failures
- **Fix**: Add defensive check to ensure stats are valid before making decisions

**Data Flow**:
1. Dashboard calls `DashboardService.GetDashboardStatsAsync()`
2. Service calls API Gateway `/api/dashboard`
3. API Gateway aggregates stats from multiple services
4. Returns `DashboardStatsViewModel` with `IsLoaded = true` (or `false` on error)
5. Dashboard evaluates wizard redirect logic

---

## Wallet Detection Logic

### Decision Tree

```
┌─────────────────────────────────────┐
│ User navigates to /dashboard        │
└────────────────┬────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│ Call DashboardService.              │
│ GetDashboardStatsAsync()            │
└────────────────┬────────────────────┘
                 │
                 ▼
         ┌───────────────┐
         │ API Success?  │
         └───┬───────┬───┘
             │       │
        YES  │       │  NO
             │       │
             ▼       ▼
     ┌───────────┐  ┌──────────────────┐
     │ IsLoaded  │  │ IsLoaded = false │
     │ = true    │  │ TotalWallets = 0 │
     └─────┬─────┘  └────────┬─────────┘
           │                 │
           ▼                 ▼
   ┌────────────────┐  ┌──────────────────┐
   │ TotalWallets   │  │ Show dashboard   │
   │ == 0?          │  │ with error       │
   └───┬────────┬───┘  │ (don't redirect) │
       │        │      └──────────────────┘
  YES  │        │  NO
       │        │
       ▼        ▼
┌──────────┐  ┌─────────────┐
│ Redirect │  │ Show        │
│ to       │  │ dashboard   │
│ wizard   │  │ with stats  │
└──────────┘  └─────────────┘
```

### Pseudocode

```csharp
// Home.razor - OnInitializedAsync()
var stats = await DashboardService.GetDashboardStatsAsync();
_isLoading = false;

// BUG FIX: Check IsLoaded before redirecting
if (stats.IsLoaded && stats.TotalWallets == 0)
{
    // User has no wallets AND stats loaded successfully
    Navigation.NavigateTo("wallets/create?first-login=true");
    return;
}

// All other cases: Show dashboard
// - stats.IsLoaded == false (API error) → show error state
// - stats.TotalWallets > 0 → show stats
```

### Edge Cases

| Scenario | IsLoaded | TotalWallets | Behavior |
|----------|----------|--------------|----------|
| Fresh user, no wallets | `true` | `0` | Redirect to wizard ✅ |
| Existing user, has wallets | `true` | `>0` | Show dashboard ✅ |
| API failure (500 error) | `false` | `0` (default) | Show dashboard with error ✅ |
| Network timeout | `false` | `0` (default) | Show dashboard with error ✅ |
| Wallet Service down | `false` | `0` (default) | Show dashboard with error ✅ |
| User deletes all wallets | `true` | `0` | Redirect to wizard ✅ (expected) |

---

## Navigation URL Model

### Current Bug Pattern

**MyWallet.razor** - Line 134:
```csharp
private void NavigateToWallet(WalletDto wallet)
{
    Navigation.NavigateTo($"/wallets/{wallet.Address}");  // ❌ Absolute path
}
```

**Problem**:
- Leading `/` creates absolute URL: `/wallets/{address}`
- Blazor's base href (`/app/`) is ignored
- Browser navigates to `/wallets/{address}` → 404 error

### Fixed Pattern

```csharp
private void NavigateToWallet(WalletDto wallet)
{
    Navigation.NavigateTo($"wallets/{wallet.Address}");  // ✅ Relative path
}
```

**Solution**:
- Relative path respects base href
- Blazor automatically prepends `/app/`
- Final URL: `/app/wallets/{address}` ✅

### URL Patterns

| Input Pattern | Base Href | Resulting URL | Status |
|---------------|-----------|---------------|--------|
| `wallets/abc123` | `/app/` | `/app/wallets/abc123` | ✅ Correct |
| `/wallets/abc123` | `/app/` | `/wallets/abc123` | ❌ 404 |
| `./wallets/abc123` | `/app/` | `/app/wallets/abc123` | ✅ Correct (explicit relative) |
| `https://example.com` | `/app/` | `https://example.com` | ✅ External (full URL) |

**Rule**: Omit leading `/` for internal navigation within Blazor app.

---

## No New Models Required

This bug fix uses **existing entities** without modification. No new models, enums, or database schema changes are needed.

### Why No WalletType Enum?

**Initial Spec Mentioned**: Distinguishing Primary vs Derived wallets

**Research Finding**: Wallet type is implicit in the data model via BIP44 derivation paths, not explicit

**Decision**: The dashboard only needs to know "does user have ANY wallet?", not "what TYPE of wallet?"

- `GetWalletsAsync()` returns all primary wallets
- Checking count > 0 is sufficient
- No need to query derived addresses (WalletAddress entities)

### Why No Default Wallet Preference?

**Initial Spec Mentioned**: Checking for default wallet

**Research Finding**: No default wallet preference exists server-side or client-side

**Decision**: OUT OF SCOPE for this bug fix

- Default wallet is a future enhancement, not a bug fix requirement
- Current fix only needs wallet existence detection
- If needed later, can add `Metadata["isDefault"] = "true"` or client-side localStorage

---

## Summary

**Entities Used**:
1. **WalletDto** - Represents user wallets
2. **DashboardStatsViewModel** - Dashboard statistics with `IsLoaded` flag

**Key Fields**:
- `DashboardStatsViewModel.IsLoaded` - Defensive check before redirect
- `DashboardStatsViewModel.TotalWallets` - Wallet count for wizard logic
- `WalletDto.Address` - Used in navigation URL construction

**Data Flow**:
```
Dashboard → DashboardService → API Gateway → Wallet Service
                ↓
          DashboardStatsViewModel
                ↓
     Wizard redirect logic (if IsLoaded && TotalWallets == 0)
```

**No Schema Changes**: All fixes are UI logic only, no backend modifications required.
