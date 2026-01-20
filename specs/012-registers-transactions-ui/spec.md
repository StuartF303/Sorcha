# Feature Specification: Registers and Transactions UI

**Feature Branch**: `012-registers-transactions-ui`
**Created**: 2026-01-20
**Status**: Draft
**Input**: User description: "Registers and Transactions UI for Sorcha.UI - includes: 1) Registers list page showing registers the organization has subscribed to or public registers, with basic info (name, description, last update, size), 2) Register creation following the same genesis process the CLI uses, 3) Transaction list within a register with real-time scrolling updates as transactions arrive, default sorting newest first with basic status info, 4) Transaction detail view in lower section when a transaction is selected, 5) Navigation to go back to register selection. Role-based access for organization participants."

## Clarifications

### Session 2026-01-20

- Q: Where should the Registers feature be placed in the UI navigation hierarchy? → A: New sidebar menu item "Registers" alongside existing items (Designer, Wallets, Admin)
- Q: Should users be able to change the transaction sort order? → A: Fixed sort order (newest first only) - no user-selectable sorting options

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Available Registers (Priority: P1)

As an organization participant, I want to see a list of all registers available to my organization so that I can browse and select registers to view their transactions.

**Why this priority**: This is the entry point for the entire feature. Without the ability to see available registers, users cannot access any other functionality. This provides immediate value by giving visibility into the distributed ledger ecosystem.

**Independent Test**: Can be fully tested by logging in as an organization participant and verifying the registers list displays with correct filtering (organization subscribed + public registers) and basic information.

**Acceptance Scenarios**:

1. **Given** I am logged in as an organization participant, **When** I navigate to the Registers page, **Then** I see a list of registers my organization has subscribed to and all public registers
2. **Given** I am viewing the registers list, **When** registers are displayed, **Then** I see the name, status (Online/Offline), last update time, and transaction count (height) for each register
3. **Given** I am viewing the registers list, **When** a register is marked as offline, **Then** I see a visual indicator distinguishing it from online registers
4. **Given** I am not logged in, **When** I navigate to the Registers page, **Then** I am redirected to the login page

---

### User Story 2 - View Transactions in a Register (Priority: P2)

As an organization participant, I want to view transactions within a selected register so that I can monitor activity and find specific transactions.

**Why this priority**: Once users can see registers, viewing their contents (transactions) is the core value proposition. This enables users to monitor ledger activity and track data flow.

**Independent Test**: Can be tested by selecting a register and verifying the transaction list displays with correct sorting, pagination, and basic transaction information.

**Acceptance Scenarios**:

1. **Given** I am viewing the registers list, **When** I select a register, **Then** I see a list of transactions within that register sorted by newest first
2. **Given** I am viewing transactions in a register, **When** transactions are displayed, **Then** I see the transaction ID (truncated), timestamp, sender address (truncated), and transaction type for each
3. **Given** I am viewing transactions in a register, **When** I scroll to the bottom of the list, **Then** older transactions are loaded automatically (infinite scroll)
4. **Given** I am viewing transactions in a register, **When** I want to return to the registers list, **Then** I can navigate back using a clear "back" control

---

### User Story 3 - Real-Time Transaction Updates (Priority: P3)

As an organization participant, I want to see new transactions appear in real-time as they are added to the register so that I can monitor live activity without refreshing.

**Why this priority**: Real-time updates enhance the user experience significantly but build upon the foundational transaction viewing capability. This enables live monitoring use cases.

**Independent Test**: Can be tested by opening a register view and submitting a new transaction (via CLI or API), then verifying the new transaction appears in the list without page refresh.

**Acceptance Scenarios**:

1. **Given** I am viewing transactions in a register, **When** a new transaction is added to the register, **Then** the transaction appears at the top of the list automatically
2. **Given** I am viewing transactions in a register, **When** a new transaction appears, **Then** I see a visual indication (highlight/animation) that a new transaction has arrived
3. **Given** I am viewing transactions in a register, **When** multiple transactions arrive rapidly, **Then** they are queued and displayed smoothly without UI disruption
4. **Given** I have scrolled down in the transaction list, **When** a new transaction arrives, **Then** I see a notification/indicator that new transactions are available at the top

---

### User Story 4 - View Transaction Details (Priority: P4)

As an organization participant, I want to view detailed information about a specific transaction so that I can inspect its contents, metadata, and verify its authenticity.

**Why this priority**: After users can browse transactions, they need to inspect individual transactions for auditing and verification purposes. This completes the read-only viewing experience.

**Independent Test**: Can be tested by selecting a transaction and verifying the detail panel displays comprehensive transaction information.

**Acceptance Scenarios**:

1. **Given** I am viewing transactions in a register, **When** I select a transaction, **Then** a detail panel appears in the lower section of the screen showing full transaction details
2. **Given** I am viewing transaction details, **When** the details are displayed, **Then** I see: full transaction ID, full sender address, recipient addresses, timestamp, block number, signature, and payload count
3. **Given** I am viewing transaction details, **When** I select a different transaction, **Then** the detail panel updates to show the newly selected transaction
4. **Given** I am viewing transaction details, **When** I want to dismiss the details, **Then** I can close the detail panel and return to the transaction list view

---

### User Story 5 - Create a New Register (Priority: P5)

As an organization administrator, I want to create a new register so that my organization can store and manage its own distributed ledger data.

**Why this priority**: Register creation is an administrative task that extends the platform's capability. It requires the viewing features to be in place first so administrators can verify their newly created registers.

**Independent Test**: Can be tested by navigating through the register creation wizard, completing all steps, and verifying the new register appears in the registers list.

**Acceptance Scenarios**:

1. **Given** I am logged in as an organization administrator, **When** I am on the Registers page, **Then** I see a "Create Register" button
2. **Given** I click "Create Register", **When** the creation wizard opens, **Then** I see a multi-step form following the genesis process (register details, confirmation, finalization)
3. **Given** I am in the register creation wizard, **When** I enter the register name and configuration, **Then** the system validates my inputs before proceeding
4. **Given** I have completed the register creation steps, **When** the register is successfully created, **Then** I am redirected to the registers list and see my new register
5. **Given** I am a regular participant (not administrator), **When** I am on the Registers page, **Then** I do not see the "Create Register" button

---

### Edge Cases

- What happens when a user views a register that goes offline while they are viewing it?
  - Display a notification indicating the register status has changed; transactions remain visible but marked as potentially stale
- What happens when the real-time connection is lost?
  - Display a connection status indicator; automatically attempt reconnection; show manual refresh button
- What happens when a register has no transactions (empty register)?
  - Display an empty state with helpful messaging indicating no transactions exist yet
- How does the system handle extremely long transaction lists (millions of transactions)?
  - Use virtual scrolling/pagination to load transactions on demand; never load entire list
- What happens if register creation fails mid-process?
  - Display clear error message; allow retry from failed step; do not leave orphaned partial registers
- What happens when viewing a transaction that references payloads the user cannot decrypt?
  - Display payload count but indicate contents are encrypted; show "Encrypted payload" placeholder

## Requirements *(mandatory)*

### Functional Requirements

**Registers List:**
- **FR-001**: System MUST display all registers the user's organization has subscribed to
- **FR-002**: System MUST display all public registers (where Advertise = true)
- **FR-003**: System MUST show register name, status, last update time (UpdatedAt), and height for each register
- **FR-004**: System MUST visually distinguish between Online, Offline, Checking, and Recovery status registers
- **FR-005**: System MUST provide navigation from registers list to transaction view when a register is selected

**Transaction List:**
- **FR-006**: System MUST display transactions sorted by timestamp (newest first) - fixed sort order, no user-selectable options
- **FR-007**: System MUST show transaction ID (truncated to 8 characters with ellipsis), timestamp, sender address (truncated), and transaction type
- **FR-008**: System MUST support infinite scroll or pagination for large transaction lists
- **FR-009**: System MUST provide clear navigation to return to the registers list
- **FR-010**: System MUST update transaction list in real-time when new transactions are added to the register

**Transaction Details:**
- **FR-011**: System MUST display full transaction details in a lower panel when a transaction is selected
- **FR-012**: System MUST show: transaction ID, register ID, sender wallet, recipient wallets, timestamp, block number, version, signature, payload count, and metadata
- **FR-013**: System MUST allow closing the detail panel to return to list-only view

**Register Creation:**
- **FR-014**: System MUST restrict register creation to users with organization administrator role
- **FR-015**: System MUST implement a multi-step creation wizard following the genesis process (initiate → finalize)
- **FR-016**: System MUST validate register name (1-38 characters, required)
- **FR-017**: System MUST display success confirmation upon register creation and update the registers list

**Access Control:**
- **FR-018**: System MUST require authentication to access any registers or transactions
- **FR-019**: System MUST enforce organization-based access control (users only see registers their organization can access)
- **FR-020**: System MUST hide administrative features (create register) from non-administrator users

**Navigation:**
- **FR-021**: System MUST provide a "Registers" menu item in the main sidebar navigation alongside existing items (Designer, Wallets, Admin)

### Key Entities

- **Register**: Distributed ledger container with name, status (Online/Offline/Checking/Recovery), height (block count), tenant association, and visibility settings (Advertise)
- **Transaction**: Signed data record within a register containing sender, recipients, timestamp, payloads, metadata, and cryptographic signature
- **Organization Subscription**: Association between an organization and the registers it has access to (beyond public registers)
- **User Role**: Participant vs Administrator - determines access to administrative functions like register creation

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view the registers list within 2 seconds of navigation
- **SC-002**: Users can view transactions within a register within 2 seconds of selection
- **SC-003**: New transactions appear in real-time within 1 second of being committed to the register
- **SC-004**: Transaction detail panel displays within 500ms of selection
- **SC-005**: Users can create a new register in under 3 minutes following the guided wizard
- **SC-006**: 95% of users successfully navigate between registers and transactions on first attempt
- **SC-007**: Real-time connection maintains uptime of 99% during active sessions
- **SC-008**: System handles registers with 100,000+ transactions without performance degradation (smooth scrolling, responsive UI)

## Assumptions

- Users are already authenticated via the existing authentication system
- Organization membership and role information is available from the Tenant Service
- Register Service APIs (list, get, create) are available and functional
- Real-time updates will be delivered via the existing SignalR infrastructure
- The CLI's genesis process (initiate/finalize) is the canonical register creation flow to follow
- Transaction payload contents are not decrypted in the UI (only metadata displayed)

## Dependencies

- Tenant Service: User authentication and organization membership
- Register Service: Register and transaction CRUD operations
- SignalR Hub: Real-time transaction notifications (existing Blueprint Service pattern can be reused)
- API Gateway: Routing to backend services

## Out of Scope

- Transaction submission/creation from the UI (read-only for transactions)
- Payload decryption or content viewing
- Register deletion or modification (beyond creation)
- Advanced search/filtering of transactions (future enhancement)
- Export functionality for transactions
- Register subscription management (managing which registers an organization subscribes to)
