# Implementation Plan: Fix Wallet Dashboard and Navigation Bugs

**Branch**: `033-fix-wallet-dashboard-bugs` | **Date**: 2026-02-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/033-fix-wallet-dashboard-bugs/spec.md`

## Summary

Fix two critical bugs in the Sorcha UI wallet experience:
1. **Dashboard wizard recurring**: The wallet creation wizard repeatedly appears on the dashboard even after a wallet has been created. Root cause: wallet detection logic checks only `TotalWallets == 0` without distinguishing between primary wallets (with seed phrase), derived wallets (via derivation path), or default wallet preferences.
2. **Navigation routing error**: Clicking a wallet in "My Activity - My Wallet" section uses incorrect URL (`/wallets/{address}` instead of `/app/wallets/{address}`), breaking navigation due to missing `/app/` base href prefix.

**Technical Approach**: Modify dashboard initialization logic to properly detect all wallet types before showing the wizard, and fix navigation calls to use relative URLs that respect the Blazor app's base href configuration.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Blazor WebAssembly, MudBlazor 8.15.0, Sorcha.UI.Core.Services
**Storage**: Browser local storage + Wallet Service API (via HTTP client)
**Testing**: xUnit for unit tests, Playwright for E2E navigation tests
**Target Platform**: Modern web browsers (Chrome, Firefox, Safari, Edge)
**Project Type**: Web application (Blazor WASM with InteractiveWebAssemblyRenderMode)
**Performance Goals**: Dashboard load < 2 seconds, wallet detection < 500ms
**Constraints**: Must not break existing first-time user flow, maintain backward compatibility with existing wallets
**Scale/Scope**: Single-user UI fixes affecting 2 Razor components, ~50-100 lines of code changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Microservices-First Architecture** | ✅ PASS | No new services created; uses existing Wallet Service API |
| **Security First** | ✅ PASS | No security changes; wallet data already encrypted via existing services |
| **API Documentation** | ✅ PASS | No new APIs; modifying client-side UI logic only |
| **Testing Requirements** | ✅ PASS | Will add E2E tests for navigation flows, unit tests for wallet detection logic |
| **Code Quality** | ✅ PASS | Following existing Blazor component patterns, async/await for API calls |
| **Blueprint Creation Standards** | N/A | Not applicable to UI bug fixes |
| **Domain-Driven Design** | ✅ PASS | Uses existing domain terms (Wallet, Dashboard, Navigation) |
| **Observability by Default** | ✅ PASS | Existing logging infrastructure will capture any errors |

**Verdict**: All applicable gates PASS. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/033-fix-wallet-dashboard-bugs/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── WalletDetectionService.http  # HTTP contract examples for testing
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Web.Client/
│   └── Pages/
│       ├── Home.razor                    # FIX: Dashboard wizard detection logic
│       └── MyWallet.razor                # FIX: Navigation URL to use relative path
│
├── Sorcha.UI.Core/
│   ├── Services/
│   │   ├── IDashboardService.cs          # REVIEW: May need HasAnyWallet method
│   │   ├── DashboardService.cs           # EXTEND: Implement wallet detection
│   │   └── Wallet/
│   │       ├── IWalletApiService.cs      # REVIEW: Check for wallet type methods
│   │       └── WalletApiService.cs       # EXTEND: May need GetWalletTypes method
│   │
│   └── Models/
│       ├── Dashboard/
│       │   └── DashboardStatsViewModel.cs  # EXTEND: May add HasDefaultWallet property
│       └── Wallet/
│           ├── WalletDto.cs              # REVIEW: Check for wallet type indicators
│           └── WalletType.cs             # NEW: Enum for Primary/Derived/Default

tests/Sorcha.UI.E2E.Tests/
├── Tests/
│   ├── WalletDashboardTests.cs           # NEW: E2E test for dashboard wizard behavior
│   └── WalletNavigationTests.cs          # NEW: E2E test for wallet detail navigation
│
└── Fixtures/
    └── AuthenticatedDockerTestBase.cs    # EXISTING: Base class for authenticated tests

tests/Sorcha.UI.Core.Tests/
└── Services/
    └── DashboardServiceTests.cs          # NEW: Unit tests for wallet detection logic
```

**Structure Decision**: This is a UI bug fix that modifies existing Blazor WASM components in the `Sorcha.UI.Web.Client` project. The core service logic lives in `Sorcha.UI.Core`, following the existing separation between presentation (Web.Client) and business logic (Core). No new projects needed; all changes are within the existing UI structure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. This section is not applicable.

## Phase 0: Research

**Objective**: Resolve technical unknowns about wallet detection and navigation in Blazor WASM.

### Research Tasks

1. **Wallet Type Detection**
   - **Unknown**: How does the Wallet Service currently distinguish between primary wallets (with seed phrase) and derived wallets (via derivation path)?
   - **Investigation**: Review `IWalletApiService`, `WalletDto` model, and Wallet Service API to determine if wallet type is already exposed
   - **Outcome**: Document the current API contract and determine if we need to add wallet type metadata

2. **Default Wallet Preference**
   - **Unknown**: Where is the default wallet preference stored? Is it in user settings, local storage, or the Wallet Service?
   - **Investigation**: Search codebase for "default wallet" patterns, check `IWalletApiService` methods, review browser storage usage
   - **Outcome**: Identify storage location and retrieval method for default wallet preference

3. **Blazor Navigation Base Href**
   - **Unknown**: Best practices for navigation in Blazor WASM apps with non-root base href (`/app/`)
   - **Investigation**: Review Blazor documentation for `NavigationManager.NavigateTo()` behavior with base href, test relative vs absolute URLs
   - **Outcome**: Document correct navigation patterns to respect base href

4. **Dashboard Loading Lifecycle**
   - **Unknown**: Why does the dashboard wizard check happen on every `OnInitializedAsync` call? Is there a better lifecycle hook?
   - **Investigation**: Review Blazor component lifecycle, check if `OnAfterRenderAsync` or state persistence would be more appropriate
   - **Outcome**: Determine optimal lifecycle hook for one-time wallet detection

5. **Wallet Service Availability**
   - **Unknown**: How should the dashboard handle Wallet Service being temporarily unavailable?
   - **Investigation**: Review existing error handling patterns in other pages (e.g., `MyWallet.razor` line 36), check for retry mechanisms
   - **Outcome**: Define graceful degradation strategy when wallet detection fails

### Best Practices to Investigate

1. **Blazor WASM State Management**
   - How to persist "wizard already shown" state across page refreshes without server-side session
   - Options: local storage, cascading parameters, scoped services

2. **MudBlazor Navigation Patterns**
   - Review MudBlazor button `Href` attribute vs `NavigationManager.NavigateTo()` for consistency
   - Check if MudBlazor components automatically handle base href

3. **E2E Test Patterns for Dashboard**
   - Review existing Playwright tests for authentication and navigation (in `Sorcha.UI.E2E.Tests`)
   - Determine how to simulate "first login" vs "returning user" scenarios

**Output**: `research.md` with all unknowns resolved

## Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete

### Artifacts to Generate

1. **data-model.md**: Document wallet-related entities and their relationships
   - `WalletType` enum: Primary, Derived, Default
   - `WalletDetectionResult` model: HasAnyWallet, HasDefaultWallet, WalletCount
   - `DashboardStatsViewModel` extensions: Additional wallet metadata

2. **contracts/WalletDetectionService.http**: HTTP examples for testing wallet detection
   - GET /api/wallets - List all wallets
   - GET /api/wallets/default - Get default wallet (if applicable)
   - GET /api/dashboard - Dashboard stats including wallet info

3. **quickstart.md**: Developer guide for testing the bug fixes
   - Step 1: Create fresh user account
   - Step 2: Verify wizard appears on first login
   - Step 3: Create wallet and verify wizard doesn't reappear
   - Step 4: Click wallet in "My Activity" and verify correct URL

### Design Decisions

1. **Wallet Detection Logic**
   - Dashboard will call `DashboardService.GetDashboardStatsAsync()` which already returns `TotalWallets`
   - Add additional check: if `TotalWallets > 0` AND `IsLoaded == true`, skip wizard
   - Add safety: only redirect to wizard if stats successfully loaded (prevent false positives on API errors)

2. **Navigation URL Fix**
   - Change `Navigation.NavigateTo($"/wallets/{wallet.Address}")` to `Navigation.NavigateTo($"wallets/{wallet.Address}")`
   - Relative path will automatically respect base href `/app/`

3. **Edge Case Handling**
   - If stats fail to load (`IsLoaded == false`), do NOT redirect to wizard (show error state instead)
   - If user deletes only wallet, next dashboard load should show wizard again
   - If Wallet Service is unavailable, show service unavailable message (don't redirect to wizard)

### Agent Context Update

After completing design artifacts, run:
```bash
.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude
```

This will update `.specify/memory/agent-context-claude.md` with:
- Wallet detection patterns
- Navigation URL conventions for Blazor with base href
- Dashboard wizard display logic

**Output**: `data-model.md`, `/contracts/*`, `quickstart.md`, updated agent context

## Implementation Strategy

### Phase 2A: Fix Dashboard Wizard Logic

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor`

**Changes**:
1. Modify `OnInitializedAsync` to add safety checks:
   ```csharp
   if (_stats.IsLoaded && _stats.TotalWallets == 0)
   {
       Navigation.NavigateTo("wallets/create?first-login=true");
       return;
   }
   ```
   - Key change: Check `_stats.IsLoaded` to prevent false redirects on API failures
   - This ensures wizard only shows when we're certain no wallets exist

2. Add error state display when stats fail to load (optional improvement):
   ```razor
   @if (!_stats.IsLoaded && !_isLoading)
   {
       <MudAlert Severity="Severity.Warning">
           Unable to load dashboard stats. <a href="wallets">View Wallets</a>
       </MudAlert>
   }
   ```

### Phase 2B: Fix Navigation URL

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyWallet.razor`

**Changes**:
1. Update `NavigateToWallet` method (line 132-135):
   ```csharp
   private void NavigateToWallet(WalletDto wallet)
   {
       Navigation.NavigateTo($"wallets/{wallet.Address}"); // Relative path respects base href
   }
   ```
   - Key change: Remove leading `/` to make path relative
   - Blazor's `NavigationManager` will automatically prepend base href `/app/`

### Phase 2C: Add E2E Tests

**New File**: `tests/Sorcha.UI.E2E.Tests/Tests/WalletDashboardTests.cs`

**Test Scenarios**:
1. `FirstLogin_NoWallets_ShowsWizard()`: Verify wizard appears for new user
2. `AfterWalletCreation_DashboardLoads_WizardDoesNotReappear()`: Verify wizard doesn't loop
3. `ExistingWallet_DashboardLoad_SkipsWizard()`: Verify returning user sees dashboard
4. `StatsFailToLoad_DashboardLoads_DoesNotRedirectToWizard()`: Verify graceful degradation

**New File**: `tests/Sorcha.UI.E2E.Tests/Tests/WalletNavigationTests.cs`

**Test Scenarios**:
1. `MyWallet_ClickWallet_NavigatesToCorrectUrl()`: Verify URL includes `/app/` prefix
2. `MyWallet_ClickWallet_PageLoadsSuccessfully()`: Verify wallet detail page displays
3. `WalletDetailUrl_DirectAccess_PageLoads()`: Verify bookmarked URLs work

### Phase 2D: Add Unit Tests

**New File**: `tests/Sorcha.UI.Core.Tests/Services/DashboardServiceTests.cs`

**Test Scenarios**:
1. `GetDashboardStatsAsync_Success_ReturnsStatsWithIsLoadedTrue()`
2. `GetDashboardStatsAsync_ApiFailure_ReturnsIsLoadedFalse()`
3. `GetDashboardStatsAsync_Exception_ReturnsIsLoadedFalse()`

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing first-time user flow | HIGH | Comprehensive E2E tests for wizard flow |
| False positives (wizard showing when it shouldn't) | MEDIUM | Check `IsLoaded` flag before redirecting |
| False negatives (wizard not showing when it should) | MEDIUM | Keep `TotalWallets == 0` check, add error logging |
| Navigation breaking bookmarks | MEDIUM | Test both relative and direct URL access |
| Wallet Service unavailable during dashboard load | MEDIUM | Show error state instead of redirecting |

## Testing Strategy

### Manual Testing Checklist

- [ ] Fresh user login shows wallet creation wizard
- [ ] After creating wallet, dashboard loads without wizard
- [ ] Refresh dashboard doesn't show wizard again
- [ ] Click wallet in "My Activity" navigates to correct URL with `/app/` prefix
- [ ] Wallet detail page loads successfully
- [ ] Bookmark wallet detail URL and verify it works on return visit
- [ ] Simulate Wallet Service down: verify dashboard shows error instead of redirecting

### Automated Testing Coverage

- **Unit Tests**: DashboardService wallet detection logic
- **E2E Tests**: Full wizard flow and navigation scenarios
- **Target Coverage**: >85% for new/modified code

## Deployment Considerations

### Pre-Deployment

1. Run full E2E test suite to verify no regressions
2. Test with existing user accounts (ensure no breaking changes)
3. Verify Docker container builds and runs successfully

### Post-Deployment

1. Monitor dashboard load errors in telemetry
2. Track "wallet creation wizard shown" events to verify no loops
3. Monitor 404 errors for wallet detail URLs

### Rollback Plan

Changes are isolated to UI components with no database schema changes. Rollback is simple:
1. Revert commit
2. Rebuild and redeploy UI container
3. No data migration needed

## Success Metrics

- Dashboard wizard loop bug reports: 0
- Navigation 404 errors for wallet URLs: 0
- First-time user wallet creation completion rate: >90%
- Dashboard load time: <2 seconds (no regression)

## Notes

- This is a targeted bug fix, not a refactoring. Minimize scope to reduce risk.
- Existing wallet creation wizard functionality remains unchanged (only when it appears, not how it works).
- No changes to Wallet Service API or backend logic required.
- Changes are backward compatible with existing user data.
