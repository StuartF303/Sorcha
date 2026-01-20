# Quickstart: Registers and Transactions UI

**Date**: 2026-01-20
**Feature**: 012-registers-transactions-ui

## Overview

This guide provides step-by-step instructions for testing the Registers and Transactions UI feature.

## Prerequisites

1. Docker Desktop running
2. Sorcha services started via `docker-compose up -d`
3. Admin user credentials: `admin@sorcha.local` / `Dev_Pass_2025!`

## Test Scenarios

### Scenario 1: View Registers List (P1)

**Goal**: Verify the registers list page displays correctly.

**Steps**:
1. Navigate to `https://localhost/app/auth/login`
2. Login with admin credentials
3. Click "Registers" in the sidebar navigation
4. Verify the registers list page loads

**Expected Results**:
- [ ] Page loads within 2 seconds
- [ ] Registers list displays (may be empty initially)
- [ ] Each register shows: name, status badge, last update, height
- [ ] "Create Register" button visible (admin only)
- [ ] Status badges use correct colors (green=Online, gray=Offline)

### Scenario 2: View Empty Register (P2)

**Goal**: Verify empty state handling for registers with no transactions.

**Steps**:
1. Create a new register (see Scenario 5)
2. Click on the newly created register
3. View the transactions area

**Expected Results**:
- [ ] Empty state message displays
- [ ] Message indicates no transactions exist yet
- [ ] Back navigation is available

### Scenario 3: View Transactions (P2)

**Goal**: Verify transaction list displays and scrolls correctly.

**Prerequisites**: A register with transactions exists (use CLI to submit test transactions)

**Steps**:
1. Navigate to Registers page
2. Click on a register with transactions
3. Verify transaction list loads
4. Scroll down to load more transactions

**Expected Results**:
- [ ] Transactions load within 2 seconds
- [ ] Sorted newest first
- [ ] Each transaction shows: truncated ID, timestamp, sender, type
- [ ] Infinite scroll loads more transactions
- [ ] Back button returns to registers list

### Scenario 4: View Transaction Details (P4)

**Goal**: Verify transaction detail panel displays complete information.

**Steps**:
1. Navigate to a register with transactions
2. Click on any transaction in the list
3. View the detail panel

**Expected Results**:
- [ ] Detail panel appears in lower section
- [ ] Shows full transaction ID (64 chars)
- [ ] Shows full sender address
- [ ] Shows recipient addresses
- [ ] Shows timestamp
- [ ] Shows block number
- [ ] Shows signature
- [ ] Shows payload count
- [ ] Close button dismisses panel
- [ ] Clicking different transaction updates panel

### Scenario 5: Create Register (P5, Admin Only)

**Goal**: Verify register creation wizard works correctly.

**Prerequisites**: Logged in as administrator

**Steps**:
1. Navigate to Registers page
2. Click "Create Register" button
3. Enter register name: "Test Register"
4. Complete wizard steps
5. Verify register appears in list

**Expected Results**:
- [ ] Wizard opens with step 1
- [ ] Name validation works (1-38 chars)
- [ ] Progress through steps
- [ ] Success confirmation displays
- [ ] New register appears in list
- [ ] Register status shows Online

### Scenario 6: Real-Time Updates (P3)

**Goal**: Verify new transactions appear in real-time.

**Prerequisites**: Register with SignalR connection

**Steps**:
1. Open a register's transaction view
2. In another terminal, submit a transaction via CLI:
   ```bash
   dotnet run --project src/Apps/Sorcha.Cli -- tx submit --register <id>
   ```
3. Observe the transaction list

**Expected Results**:
- [ ] New transaction appears at top within 1 second
- [ ] Visual highlight indicates new transaction
- [ ] No page refresh required
- [ ] If scrolled down, notification appears for new transactions

### Scenario 7: Connection Loss Handling (Edge Case)

**Goal**: Verify graceful handling of connection loss.

**Steps**:
1. Open a register's transaction view
2. Stop the Register Service container:
   ```bash
   docker stop sorcha-register-service
   ```
3. Observe connection status indicator
4. Restart the service:
   ```bash
   docker start sorcha-register-service
   ```

**Expected Results**:
- [ ] Connection status shows disconnected
- [ ] Manual refresh button appears
- [ ] Auto-reconnection attempts
- [ ] Connection restores when service returns

### Scenario 8: Non-Admin Access Control (P5)

**Goal**: Verify non-admin users cannot create registers.

**Steps**:
1. Create a non-admin test user (or use existing)
2. Login as non-admin user
3. Navigate to Registers page

**Expected Results**:
- [ ] "Create Register" button NOT visible
- [ ] Can view registers and transactions
- [ ] Cannot access creation wizard via URL

## Performance Benchmarks

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Registers list load | < 2s | Browser DevTools Network tab |
| Transactions load | < 2s | Browser DevTools Network tab |
| Real-time update | < 1s | Stopwatch from CLI submit to UI display |
| Transaction detail | < 500ms | Browser DevTools Performance tab |
| 100K transactions scroll | Smooth | Visual inspection, no lag |

## CLI Commands for Testing

```bash
# List registers
dotnet run --project src/Apps/Sorcha.Cli -- register list

# Create test register
dotnet run --project src/Apps/Sorcha.Cli -- register create "Test Register"

# Submit test transaction
dotnet run --project src/Apps/Sorcha.Cli -- tx submit --register <id> --data "test"

# Get transaction details
dotnet run --project src/Apps/Sorcha.Cli -- tx get --register <id> --tx <txId>
```

## Troubleshooting

### Registers Not Loading
1. Check API Gateway is running: `docker ps | grep api-gateway`
2. Check Register Service: `docker ps | grep register-service`
3. Check browser console for errors
4. Verify JWT token is valid

### Real-Time Updates Not Working
1. Check SignalR connection in browser DevTools (Network → WS)
2. Verify `/hubs/register` endpoint accessible
3. Check for CORS errors in console
4. Ensure subscribed to correct register ID

### Create Register Fails
1. Verify admin role in JWT claims
2. Check register name validation (1-38 chars)
3. Check for existing register with same name
4. Review API response error message
