# Feature Specification: Fix Wallet Dashboard and Navigation Bugs

**Feature Branch**: `033-fix-wallet-dashboard-bugs`
**Created**: 2026-02-13
**Status**: Draft
**Input**: User description: "that from a userpoint of view the first time login create wallet works but bug that once created dhasboard keeps firing the create wizard could check for a default wallet aswell as a primary wallet (one for which a seed phrase is known) as comparded to a derived wallet (created with derivation path ) and clicking into a wallet in the 'my activity - my wallet' has a wrong url think its missing /app/"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dashboard Loads Without Recurring Wizard (Priority: P1)

After creating their first wallet, users should see the dashboard with their wallet information instead of being repeatedly prompted to create a wallet.

**Why this priority**: This is a critical bug that prevents users from accessing the dashboard after wallet creation. It blocks the primary workflow and creates a poor user experience.

**Independent Test**: Can be fully tested by creating a wallet and verifying that the dashboard shows the wallet information without re-triggering the creation wizard. Delivers immediate value by allowing users to access their dashboard.

**Acceptance Scenarios**:

1. **Given** a new user logs in for the first time, **When** they complete the wallet creation wizard, **Then** they should see the dashboard with their new wallet information
2. **Given** a user has already created a wallet, **When** they navigate to the dashboard, **Then** the create wallet wizard should not appear
3. **Given** a user has a primary wallet (with seed phrase), **When** the dashboard loads, **Then** the system should detect this wallet and skip the creation wizard
4. **Given** a user has a derived wallet (created via derivation path), **When** the dashboard loads, **Then** the system should detect this wallet and skip the creation wizard
5. **Given** a user has set a default wallet, **When** the dashboard loads, **Then** the system should use the default wallet preference and skip the creation wizard

---

### User Story 2 - Wallet Navigation Works Correctly (Priority: P2)

Users clicking on a wallet in "My Activity - My Wallet" should navigate to the correct wallet detail page.

**Why this priority**: Important for usability and navigation consistency, but doesn't block core wallet functionality. Users can still access their wallets through alternative navigation paths.

**Independent Test**: Can be tested independently by clicking wallet links in the "My Activity" section and verifying the URL and page load correctly. Delivers value by enabling direct navigation to wallet details.

**Acceptance Scenarios**:

1. **Given** a user is viewing "My Activity - My Wallet" section, **When** they click on a wallet item, **Then** they should navigate to the correct wallet detail page with proper URL including `/app/` prefix
2. **Given** a user navigates to a wallet detail page via "My Activity", **When** the page loads, **Then** the wallet information should display correctly
3. **Given** a user bookmarks a wallet detail URL, **When** they return to the bookmarked URL later, **Then** the page should load correctly

---

### Edge Cases

- What happens when a user has multiple wallets (both primary and derived)? Which one should be considered the "default"?
- How does the system handle a user who deletes their only wallet? Should the wizard reappear?
- What happens if a user has a wallet in local storage but it's corrupted or invalid?
- How does the system handle wallet detection if the wallet service is temporarily unavailable?
- What happens when a user navigates directly to a wallet URL that doesn't exist?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST detect existing wallets before showing the wallet creation wizard on dashboard load
- **FR-002**: System MUST distinguish between primary wallets (with known seed phrase) and derived wallets (created via derivation path) when checking for wallet existence
- **FR-003**: System MUST check for a default wallet preference before showing the wallet creation wizard
- **FR-004**: System MUST NOT show the wallet creation wizard if any valid wallet exists (primary, derived, or default)
- **FR-005**: System MUST generate correct URLs for wallet detail pages that include the `/app/` prefix
- **FR-006**: Wallet navigation links in "My Activity - My Wallet" MUST route to the correct wallet detail page
- **FR-007**: System MUST handle cases where no wallets exist by showing the creation wizard
- **FR-008**: System MUST handle invalid or corrupted wallet data gracefully without blocking dashboard access
- **FR-009**: System MUST persist wallet detection state to prevent wizard from reappearing after successful wallet creation
- **FR-010**: System MUST support navigation to wallet detail pages via direct URL access (including bookmarked URLs)

### Key Entities

- **Primary Wallet**: A wallet for which the user has the seed phrase, representing full ownership and backup capability
- **Derived Wallet**: A wallet created using a derivation path from an existing wallet, used for organizational or privacy purposes
- **Default Wallet**: The user's preferred wallet for primary operations, can be either primary or derived
- **Wallet Detection State**: Information about which wallets exist for a user, including type (primary/derived) and default preference

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users who create a wallet successfully see the dashboard without the wizard reappearing (100% success rate)
- **SC-002**: Dashboard loads with correct wallet information within 2 seconds of wallet creation completion
- **SC-003**: Wallet navigation links route to the correct URL with proper `/app/` prefix (100% accuracy)
- **SC-004**: Users can navigate to wallet detail pages from "My Activity" section and see correct information within 1 second
- **SC-005**: Support tickets related to "recurring wallet wizard" or "can't access dashboard after wallet creation" are reduced to zero
- **SC-006**: All wallet navigation URLs are bookmarkable and work when accessed directly

## Assumptions

- Wallet data is stored in a persistent location (local storage, database, or service) that can be queried on dashboard load
- The system has a way to determine if a wallet is primary (has seed phrase) vs derived (derivation path)
- Default wallet preference is stored in user settings or preferences
- The dashboard component has access to wallet detection logic before rendering the creation wizard
- The routing system supports proper URL construction with the `/app/` prefix
- Users may have zero, one, or multiple wallets at any given time

## Dependencies

- Wallet service or storage layer for querying existing wallets
- User preferences service for default wallet setting
- Routing configuration for wallet detail page navigation
- Dashboard component lifecycle hooks for wallet detection logic

## Out of Scope

- Wallet backup or recovery features
- Multi-wallet management UI improvements
- Wallet performance optimizations
- Wallet security enhancements beyond current implementation
- Changes to wallet creation wizard functionality (only when it appears, not how it works)
