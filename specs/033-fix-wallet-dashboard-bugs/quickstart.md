# Quickstart: Testing Wallet Dashboard Bug Fixes

**Branch**: `033-fix-wallet-dashboard-bugs`
**Date**: 2026-02-13

## Overview

This guide helps developers test the two bug fixes:
1. **Dashboard wizard loop** - Wallet creation wizard no longer reappears after wallet is created
2. **Navigation routing** - Clicking wallet in "My Activity" navigates to correct URL with `/app/` prefix

## Prerequisites

- Docker Desktop running
- .NET 10 SDK installed
- Web browser (Chrome, Firefox, Edge, or Safari)
- Optional: REST Client extension for VS Code (for HTTP tests)

## Quick Test (5 minutes)

### Step 1: Start Services

```bash
# From repository root
docker-compose up -d

# Wait for services to start (~30 seconds)
docker-compose ps
```

**Expected**: All services showing "Up" status

### Step 2: Test Dashboard Wizard (Fresh User)

1. Open browser to `http://localhost/app/`
2. Click "Login" (or you'll be redirected automatically)
3. Create a new test user account:
   - Email: `test-wizard@example.com`
   - Password: `Test123!@#`
4. After login, verify you're redirected to wallet creation wizard
   - URL should be: `http://localhost/app/wallets/create?first-login=true`
   - Page should show "Welcome to Sorcha!" message
5. Create a wallet:
   - Name: "Test Wallet"
   - Algorithm: "ED25519 (Recommended)"
   - Word Count: "12 words (Standard)"
   - Click "Create Wallet"
6. Save mnemonic phrase (copy to notepad)
7. Confirm mnemonic by selecting words in order
8. Click "I have safely stored my recovery phrase"
9. **CRITICAL**: Verify you're back on dashboard WITHOUT wizard showing again
   - URL should be: `http://localhost/app/dashboard` or `http://localhost/app/`
   - Should see "Welcome back, test-wizard@example.com!"
   - Should see wallet count: "1 Wallets"
10. Refresh page (F5 or Ctrl+R)
11. **VERIFY BUG FIX**: Wizard should NOT reappear (dashboard should load normally)

**✅ PASS**: Dashboard loads without wizard after wallet creation
**❌ FAIL**: Wizard reappears → Bug not fixed

### Step 3: Test Wallet Navigation

1. From dashboard, click "Manage Wallets" button (or navigate to `/app/my-wallet`)
2. Verify "My Wallet" page shows your wallet card
3. Click on the wallet card
4. **VERIFY BUG FIX**: Check browser URL bar
   - Expected: `http://localhost/app/wallets/sor1...` (includes `/app/` prefix)
   - Broken: `http://localhost/wallets/sor1...` (missing `/app/`, results in 404)
5. Page should load successfully showing wallet details
6. Bookmark this page (Ctrl+D or Cmd+D)
7. Navigate away (click "Dashboard" in sidebar)
8. Use bookmarked URL to return to wallet detail page
9. **VERIFY**: Bookmarked URL works correctly

**✅ PASS**: Navigation URLs include `/app/` prefix and work correctly
**❌ FAIL**: URL is `/wallets/...` without `/app/` → Bug not fixed

## Detailed Test Scenarios

### Scenario A: Fresh User Flow

**Purpose**: Verify wizard shows for new users with no wallets

**Steps**:
1. Create new user account (or use incognito/private browser window)
2. Login
3. Verify automatic redirect to wallet creation wizard
4. Complete wallet creation
5. Verify dashboard loads afterward

**Expected**: Wizard shows once, then never again (unless user deletes all wallets)

### Scenario B: Existing User Flow

**Purpose**: Verify wizard doesn't show for users with wallets

**Steps**:
1. Login as user who already has a wallet
2. Navigate to `/app/dashboard`
3. Verify dashboard loads immediately (no redirect)
4. Check that wallet count shows correctly

**Expected**: No wizard, immediate dashboard display

### Scenario C: API Failure Handling

**Purpose**: Verify graceful degradation when Wallet Service is unavailable

**Steps**:
1. Stop Wallet Service:
   ```bash
   docker-compose stop wallet-service
   ```
2. Login as any user
3. Navigate to `/app/dashboard`
4. **VERIFY**: Dashboard should load with error message, NOT redirect to wizard
5. Check browser console for error logs (should see API failure logged)
6. Restart Wallet Service:
   ```bash
   docker-compose start wallet-service
   ```
7. Refresh dashboard
8. Dashboard should now load stats correctly

**Expected**: Error state displayed, no redirect to wizard during service outage

### Scenario D: Wallet Deletion Flow

**Purpose**: Verify wizard reappears when user deletes their only wallet

**Steps**:
1. Login as user with one wallet
2. Navigate to `/app/wallets`
3. Delete the wallet (if delete functionality is available)
4. Navigate to `/app/dashboard`
5. **VERIFY**: Should redirect to wizard (expected behavior - user has no wallets)

**Expected**: Wizard shows again after deleting last wallet

### Scenario E: Multiple Wallets

**Purpose**: Verify navigation works with multiple wallets

**Steps**:
1. Login as user
2. Create 2-3 wallets
3. Navigate to `/app/my-wallet`
4. Click on each wallet card
5. **VERIFY**: Each navigation results in correct URL with `/app/` prefix

**Expected**: All wallet navigation URLs work correctly

## HTTP API Tests

Use the `.http` file for manual API testing:

```bash
# Open in VS Code with REST Client extension
code specs/033-fix-wallet-dashboard-bugs/contracts/WalletDetectionService.http
```

**Key Tests**:
1. `GET /api/dashboard` - Check `isLoaded` and `totalWallets` fields
2. `GET /api/v1/wallets` - Verify wallet list matches dashboard count
3. Simulate failures by stopping services

## Automated Tests

### Run E2E Tests

```bash
# From repository root
cd tests/Sorcha.UI.E2E.Tests
dotnet test --filter "FullyQualifiedName~WalletDashboard"
dotnet test --filter "FullyQualifiedName~WalletNavigation"
```

**Expected**: All tests pass

### Run Unit Tests

```bash
cd tests/Sorcha.UI.Core.Tests
dotnet test --filter "FullyQualifiedName~DashboardService"
```

**Expected**: All tests pass

## Debugging Tips

### Dashboard Wizard Keeps Appearing

**Possible Causes**:
1. `IsLoaded` check not implemented
   - **Check**: `Home.razor` line ~185 should have `if (_stats.IsLoaded && _stats.TotalWallets == 0)`
2. Wallet Service is failing silently
   - **Check**: `docker-compose logs wallet-service`
3. Dashboard stats API is broken
   - **Check**: Browser DevTools Network tab, look for `/api/dashboard` call
   - Should return `{ "isLoaded": true, "totalWallets": 1 }`

### Navigation URLs Missing `/app/` Prefix

**Possible Causes**:
1. Absolute path used instead of relative
   - **Check**: `MyWallet.razor` line ~134 should use `Navigation.NavigateTo($"wallets/{wallet.Address}")` (no leading `/`)
2. Base href misconfigured
   - **Check**: `wwwroot/app/index.html` should have `<base href="/app/" />`

### Wallet Creation Doesn't Increment Count

**Possible Causes**:
1. Dashboard stats not refreshing after wallet creation
   - **Check**: Wallet creation should redirect to dashboard, which re-fetches stats
2. Cache issue
   - **Workaround**: Hard refresh (Ctrl+Shift+R)

## Test Data Cleanup

### Reset Test User

```bash
# Remove test user from database (if needed)
docker-compose exec tenant-db psql -U postgres -d tenant -c "DELETE FROM users WHERE email = 'test-wizard@example.com';"
```

### Clear Browser State

1. Open DevTools (F12)
2. Application tab → Storage → Clear site data
3. Or use Incognito/Private browsing mode for fresh state

## Success Criteria

After testing, verify:

- [ ] Fresh user sees wizard on first login
- [ ] Wizard doesn't reappear after creating wallet
- [ ] Dashboard loads correctly after wizard completion
- [ ] Refreshing dashboard doesn't show wizard again
- [ ] Navigation URLs include `/app/` prefix
- [ ] Wallet detail page loads successfully
- [ ] Bookmarked wallet URLs work
- [ ] API failure shows error instead of redirecting
- [ ] All E2E tests pass
- [ ] All unit tests pass

## Next Steps

Once manual testing is complete:
1. Run full test suite: `dotnet test`
2. Check for any regressions in other areas
3. Review code changes for quality
4. Prepare for PR submission

## Troubleshooting

### Services Won't Start

```bash
# Check Docker logs
docker-compose logs

# Rebuild if needed
docker-compose down
docker-compose build
docker-compose up -d
```

### Authentication Issues

```bash
# Check Tenant Service logs
docker-compose logs tenant-service

# Verify JWT configuration
docker-compose logs api-gateway | grep -i "jwt"
```

### Database Connection Errors

```bash
# Check database containers
docker-compose ps | grep db

# Restart databases
docker-compose restart tenant-db wallet-db
```

## Support

For issues or questions:
- Check `docs/troubleshooting.md`
- Review `CLAUDE.md` for development setup
- Open GitHub issue with test results and logs
