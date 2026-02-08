# Quickstart: Sorcha UI Modernization

**Branch**: `025-ui-modernization` | **Date**: 2026-02-07

## How to Verify Each Feature

### Prerequisites

```bash
# Start all services
docker-compose up -d

# Verify services are running
curl http://localhost/api/health

# Default test credentials
# Email: admin@sorcha.local
# Password: Dev_Pass_2025!
# Profile: docker
```

### US1: Identifier Truncation Component

1. Navigate to any page with long identifiers (e.g., `/registers`, `/wallets`)
2. Verify identifiers show first 6 chars + "..." + last 6 chars
3. Hover over a truncated ID → tooltip shows full value
4. Click a truncated ID → clipboard copy + snackbar confirmation
5. Check an ID shorter than 12 chars → displayed in full

### US2: Navigation Restructure

1. Log in as admin
2. Check sidebar: ADMIN section should show individual links:
   - System Health → `/admin/health`
   - Peer Network → `/admin/peers`
   - Organizations → `/admin/organizations`
   - Validator → `/admin/validator`
   - Service Principals → `/admin/principals`
3. Click each link → navigates directly to that page (no tab selection needed)
4. Old `/admin` route redirects appropriately

### US3: Organization Management

1. Navigate to Organizations page (`/admin/organizations`)
2. Click "Create Organization"
3. Fill in: Name, Description, Subdomain → Submit
4. Verify organization appears in list with status "Active"
5. Click Edit on the organization → change description → Save
6. Click Deactivate → confirm dialog → status changes to "Suspended"
7. Organization IDs use truncated display

### US4: Validator Admin Panel

1. Navigate to Validator page (`/admin/validator`)
2. Verify mempool stats display (pending count, oldest entry age)
3. If registers exist with pending transactions, verify per-register breakdown
4. Page auto-refreshes on interval

### US5: Service Principal Management

1. Navigate to Service Principals page (`/admin/principals`)
2. Verify list of service credentials with name, status, expiration
3. Credentials nearing expiration show warning indicator

### US6: Dashboard Live Stats

1. Navigate to Dashboard (`/`)
2. Verify stat cards show non-zero values (if data exists)
3. Cards should show: Active Blueprints, Wallets, Recent Transactions, Connected Peers
4. If gateway is down, cards show "Data unavailable" indicator
5. Refresh page → values update

### US7: Workflow Instance Management

1. Navigate to My Workflows (`/my-workflows`)
2. If workflow instances exist, verify list shows blueprint name, status, step
3. Click a workflow → detail view with participants, action history
4. Navigate to My Actions (`/my-actions`)
5. If pending actions exist, verify list with action name, blueprint, urgency
6. Click an action → form generated from data schema → submit

### US8: Blueprint Cloud Persistence

1. Navigate to Designer (`/designer`)
2. Create a simple blueprint (add 2 participants, 1 action)
3. Save the blueprint
4. Clear browser LocalStorage (DevTools → Application → Clear)
5. Refresh page → blueprint should still be available (from API)
6. Navigate to Blueprints (`/blueprints`) → blueprint appears in list
7. Click Publish → review validation → confirm
8. View version history

### US9: Wallet Management

1. Navigate to My Wallet (`/my-wallet`)
2. Click "Create Wallet" → enter name → submit
3. CRITICAL: Mnemonic shown once — verify display, then dismiss
4. Wallet appears in list with truncated address
5. Click wallet → detail with derived addresses
6. Initiate signing → verify signature result

### US10: Transaction History

1. Navigate to My Transactions (`/my-transactions`)
2. If transactions exist, verify paginated list with date, register, type, status
3. Click a transaction → detail view with payload summary
4. Use filters: register, date range, type → verify filtered results
5. Transaction IDs use truncated display

### US11: Template Library

1. Navigate to Templates (`/templates`)
2. Verify templates loaded from backend API (not hardcoded)
3. Filter by category → only matching templates shown
4. Click "Use Template" → new blueprint created in Designer

### US12: Explorer Enhancements

1. Navigate to a register detail page (`/registers/{id}`)
2. Click "Docket Chain" tab → view dockets with version, hash, tx count
3. Click a docket → detail with transaction IDs, previous hash
4. Navigate to Explorer query page
5. Open OData query builder → add filter row → select field, operator, value
6. Click Execute → results in table with pagination
7. Verify raw OData query string shown

---

## Playwright E2E Test Execution

```bash
# Run all UI E2E tests
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker"

# Run specific feature tests
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker & Category=Admin"
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker & Category=Dashboard"
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker & Category=Workflows"

# Run with verbose output
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker" -v detailed
```

## Common Issues

- **Services offline**: Run `docker-compose up -d` and wait 30 seconds for startup
- **Auth failure**: Ensure Tenant Service is running and `admin@sorcha.local` credentials are bootstrapped
- **Empty lists**: Create test data via CLI or direct API calls before testing list pages
- **Truncation not visible**: Ensure identifiers are longer than 12 characters
