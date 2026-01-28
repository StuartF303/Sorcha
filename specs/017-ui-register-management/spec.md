# Feature Specification: UI Register Management

**Feature Branch**: `017-ui-register-management`
**Created**: 2026-01-28
**Status**: Draft
**Input**: User description: "Sorcha.UI Register Management - Create UI pages for register creation (two-phase flow), register listing, transaction viewing, and transaction detail drill-down. Mirror the CLI functionality in a Blazor WASM interface."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Register List (Priority: P1)

As an authenticated user, I want to view a list of all registers I have access to, so that I can quickly see the status and health of my distributed ledgers.

**Why this priority**: The register list is the entry point to all register management functionality. Users need to see what registers exist before they can interact with them.

**Independent Test**: Can be fully tested by logging in and navigating to the Registers page. Delivers immediate value by showing all accessible registers with their status.

**Acceptance Scenarios**:

1. **Given** an authenticated user with access to 5 registers, **When** they navigate to the Registers page, **Then** they see all 5 registers displayed with name, status, transaction count, and last updated time
2. **Given** an authenticated user with no registers, **When** they navigate to the Registers page, **Then** they see an empty state message with guidance on how to create their first register
3. **Given** a user viewing the register list, **When** they click on a register card, **Then** they are navigated to the register detail page
4. **Given** registers with different statuses (Online, Offline, Checking, Recovery), **When** the list is displayed, **Then** each register shows an appropriate status badge with distinct visual styling

---

### User Story 2 - View Register Details and Transactions (Priority: P1)

As an authenticated user, I want to view detailed information about a specific register including its transaction history, so that I can monitor activity and audit the ledger.

**Why this priority**: Transaction visibility is core to the distributed ledger value proposition. Users need to see what's happening in their registers.

**Independent Test**: Can be tested by navigating to a register detail page and verifying all information displays correctly with transaction list.

**Acceptance Scenarios**:

1. **Given** a register with 50 transactions, **When** the user views the register detail page, **Then** they see register metadata (ID, name, tenant, status, height) and the first page of transactions
2. **Given** a register with more than 20 transactions, **When** the user scrolls or clicks "Load More", **Then** additional transactions are loaded and displayed
3. **Given** real-time updates are enabled, **When** a new transaction is confirmed on the register, **Then** the user sees a notification banner offering to load the new transaction
4. **Given** the user is viewing the transaction list, **When** they click on a transaction row, **Then** the transaction detail panel opens showing full transaction information

---

### User Story 3 - View Transaction Details (Priority: P2)

As an authenticated user, I want to drill into a specific transaction to see all its details, so that I can verify transaction data, participants, and signatures.

**Why this priority**: Detailed transaction inspection is essential for auditing and verification, but requires the list view (P1) to be functional first.

**Independent Test**: Can be tested by selecting a transaction from the list and verifying all detail fields are displayed correctly with copy functionality.

**Acceptance Scenarios**:

1. **Given** a selected transaction, **When** the detail panel opens, **Then** the user sees: Transaction ID, timestamp, status (Confirmed/Pending), block number (if confirmed), sender wallet address, payload type, and signature
2. **Given** a transaction with a long wallet address, **When** displayed in the detail panel, **Then** the user can copy the full address to clipboard with a single click
3. **Given** the detail panel is open, **When** the user clicks the close button or clicks outside the panel, **Then** the panel closes and no transaction is selected
4. **Given** a pending transaction, **When** viewed in detail, **Then** it displays "Pending" status without a block number

---

### User Story 4 - Create New Register (Priority: P2)

As an authorized user (with register creation permission), I want to create a new register through a guided wizard, so that I can establish a new distributed ledger with proper cryptographic attestation.

**Why this priority**: Register creation is important but less frequent than viewing. The two-phase cryptographic flow requires careful UX design.

**Independent Test**: Can be tested by completing the creation wizard and verifying the new register appears in the list.

**Acceptance Scenarios**:

1. **Given** an authorized user on the Registers page, **When** they click "Create Register", **Then** a multi-step wizard opens guiding them through the creation process
2. **Given** the user is in step 1 (Basic Info), **When** they enter a valid register name (1-38 characters) and description, **Then** they can proceed to the next step
3. **Given** the user is in step 2 (Select Wallet), **When** they select a wallet for signing the attestation, **Then** they can proceed to the review step
4. **Given** the user is in step 3 (Review & Create), **When** they confirm the details and click "Create", **Then** the system executes the two-phase creation flow (initiate, sign, finalize) and shows progress
5. **Given** the creation process completes successfully, **When** the wizard closes, **Then** the new register appears in the register list
6. **Given** a creation error occurs (e.g., network failure, signature failure), **When** the error happens, **Then** the user sees a clear error message with option to retry

---

### User Story 5 - Filter and Search Registers (Priority: P3)

As a user with many registers, I want to filter and search the register list, so that I can quickly find specific registers.

**Why this priority**: Filtering becomes important at scale but basic list functionality (P1) must work first.

**Independent Test**: Can be tested by entering search terms and verifying the list filters correctly.

**Acceptance Scenarios**:

1. **Given** a user with 20 registers, **When** they type in the search box, **Then** the list filters to show only registers whose name contains the search term
2. **Given** filter options for status, **When** the user selects "Online" filter, **Then** only online registers are displayed
3. **Given** active filters, **When** the user clears the filters, **Then** all registers are displayed again

---

### User Story 6 - Query Transactions Across Registers (Priority: P3)

As an authenticated user, I want to search for transactions by wallet address or other criteria, so that I can trace activity across multiple registers.

**Why this priority**: Cross-register queries are advanced functionality that builds on basic transaction viewing.

**Independent Test**: Can be tested by entering a wallet address and verifying matching transactions are returned.

**Acceptance Scenarios**:

1. **Given** a user on the transaction query page, **When** they enter a wallet address and submit, **Then** they see all transactions involving that wallet across accessible registers
2. **Given** query results with multiple pages, **When** the user scrolls or pages, **Then** additional results load
3. **Given** no matching transactions, **When** a query is executed, **Then** the user sees a "No results found" message

---

### Edge Cases

- What happens when a user's session expires during register creation? System should preserve wizard state and prompt for re-authentication, then resume
- How does the system handle network disconnection during real-time updates? The UI should show a connection status indicator and automatically attempt reconnection
- What happens when a register is deleted while a user is viewing it? The UI should show a "Register no longer available" message and navigate back to the list
- How are very long register names displayed? Names should truncate with ellipsis and show full name on hover/tooltip
- What happens if the user has no wallets when attempting to create a register? The wizard should detect this and guide the user to create a wallet first

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a paginated list of registers accessible to the authenticated user
- **FR-002**: System MUST show register status (Online, Offline, Checking, Recovery) with distinct visual indicators
- **FR-003**: System MUST display register metadata including name, ID, tenant, height, transaction count, and timestamps
- **FR-004**: System MUST support navigation from register list to register detail view
- **FR-005**: System MUST display paginated transaction history within a register detail view
- **FR-006**: System MUST support transaction selection to view detailed information in a side panel
- **FR-007**: System MUST display transaction details including ID, timestamp, status, block number, sender, payload type, and signature
- **FR-008**: System MUST provide copy-to-clipboard functionality for transaction IDs and wallet addresses
- **FR-009**: System MUST support real-time transaction notifications via existing SignalR connection
- **FR-010**: System MUST provide a multi-step wizard for register creation with validation at each step
- **FR-011**: System MUST execute the two-phase register creation flow (initiate, sign, finalize) with progress indication
- **FR-012**: System MUST handle creation errors gracefully with user-friendly messages and retry options
- **FR-013**: System MUST restrict register creation to users with appropriate permissions
- **FR-014**: System MUST support text search filtering of the register list by name
- **FR-015**: System MUST support status filtering of the register list
- **FR-016**: System MUST provide a transaction query interface for searching by wallet address
- **FR-017**: System MUST show loading states during data fetches and empty states when no data exists
- **FR-018**: System MUST be responsive and work on desktop and tablet screen sizes; mobile phones MUST support read-only access (viewing registers and transactions) but MAY exclude creation wizards

### Key Entities

- **Register**: Represents a distributed ledger with ID, name, tenant, status, height, transaction count, and timestamps
- **Transaction**: Represents a ledger entry with ID, timestamp, status (Confirmed/Pending), block number, sender wallet, payload, and signature
- **Wallet**: User's signing wallet with address and algorithm, used for attestation during register creation

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view their complete register list within 3 seconds of navigation
- **SC-002**: Users can navigate from register list to transaction detail within 2 clicks
- **SC-003**: Register creation wizard can be completed in under 2 minutes by a first-time user
- **SC-004**: 95% of page loads complete within 2 seconds on standard broadband connection
- **SC-005**: Real-time transaction notifications appear within 5 seconds of confirmation
- **SC-006**: Users can find a specific register among 50+ registers within 10 seconds using search
- **SC-007**: All interactive elements are accessible via keyboard navigation
- **SC-008**: Transaction detail copy-to-clipboard works in a single click with visual confirmation

## Clarifications

### Session 2026-01-28

- Q: What level of mobile phone support is required? → A: Mobile phones get read-only access (view registers/transactions, no creation)
- Q: Should this feature enhance existing components or allow restructuring? → A: Enhance existing components and preserve UX patterns; refactoring acceptable when requirements demand it

## Assumptions

- Users are already authenticated via the existing authentication system
- The Register Service API endpoints for listing, getting, and creating registers are available
- The Transaction Service API endpoints for listing and getting transactions are available
- SignalR hub connection for real-time updates is already established (RegisterHubConnection exists)
- The wallet service is available for signing attestations during register creation
- Users have at least one wallet created before attempting register creation
- MudBlazor component library is the standard UI framework for this application
- Existing register list and detail pages exist and will be enhanced; refactoring is acceptable when requirements demand it, but current UX patterns should be preserved where possible
