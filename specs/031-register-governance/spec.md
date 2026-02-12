# Feature Specification: Register Governance — Genesis Blueprint & Decentralized Identity

**Feature Branch**: `031-register-governance`
**Created**: 2026-02-11
**Status**: Draft
**Input**: User description: "Genesis Blueprint - Register Governance"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Register Creation with Ownership Assertion (Priority: P1)

An administrator creates a new register and is established as the Owner via a genesis Control transaction. The genesis blueprint is automatically attached to the register, recording the initial admin roster (a single Owner DID) as the first transaction. Any peer replicating this register can verify who owns it by reading the genesis transaction.

**Why this priority**: Without genesis ownership assertion, no other governance operations are possible. This is the foundation of the entire feature — every register must have an owner.

**Independent Test**: Can be tested by creating a register and verifying the genesis transaction contains a valid Control payload with the creator's DID as Owner. Any peer replaying the Control chain reconstructs a single-entry roster.

**Acceptance Scenarios**:

1. **Given** a user with a wallet, **When** they create a new register, **Then** the genesis transaction is a Control transaction containing their `did:sorcha:w:{walletAddress}` as Owner, the admin roster has exactly one entry, and the genesis blueprint is bound to the register.
2. **Given** a genesis transaction has been recorded, **When** any peer replays all Control transactions on the register, **Then** they derive a roster with exactly one Owner and zero Admins.
3. **Given** a register has been created, **When** the genesis blueprint instance enters its governance loop, **Then** it is ready to accept Propose Change actions from the Owner.

---

### User Story 2 — Add Admin via Quorum (Priority: P1)

An existing admin (or Owner) proposes adding a new admin to the register. The proposal enters a quorum collection loop where other admins approve or reject. Once a majority (>50% of current admins) approve, the new admin counter-signs to accept, and a Control transaction records the updated roster. The Owner can bypass quorum and add admins unilaterally.

**Why this priority**: Adding admins is the first governance operation after genesis and is required to establish multi-party governance.

**Independent Test**: Can be tested by creating a register (1 Owner), having the Owner add an Admin (no quorum needed — only 1 admin), then using both to add a third admin (quorum of 2 required, both must agree since 1/2 = 50% is not >50%).

**Acceptance Scenarios**:

1. **Given** a register with 1 Owner, **When** the Owner proposes adding a new admin, **Then** quorum is skipped (Owner has ultimate authority), the new admin is prompted to accept, and upon acceptance a Control transaction records the updated roster.
2. **Given** a register with 3 admins (1 Owner + 2 Admins), **When** any admin proposes adding a fourth, **Then** at least 2 of the 3 current admins must approve (>50% of 3), the new admin must accept, and a Control transaction is recorded.
3. **Given** a quorum collection is in progress, **When** enough admins reject to make approval impossible (rejections >= m - quorum + 1), **Then** the proposal is marked as blocked and the workflow loops back to accept new proposals.
4. **Given** a new admin is prompted to accept, **When** they decline, **Then** the workflow returns to the proposal state without modifying the roster.

---

### User Story 3 — Remove Admin via Quorum (Priority: P2)

An existing admin proposes removing another admin from the register. The target admin is excluded from the quorum pool — the remaining admins vote. Once a majority of the remaining admins approve, the target is removed and a Control transaction records the updated roster. The Owner can bypass quorum.

**Why this priority**: Removal is essential for revoking compromised or departed admin access, but depends on the add flow being established first.

**Independent Test**: Can be tested by creating a register with 3 admins, proposing removal of one, collecting approval from the other (quorum of remaining 2: both must agree), and verifying the Control transaction reflects the reduced roster.

**Acceptance Scenarios**:

1. **Given** a register with 3 admins and admin A proposes removing admin B, **When** the quorum pool is calculated, **Then** admin B is excluded — the pool is 2 (Owner + admin A), and >50% of 2 means both must approve.
2. **Given** the Owner proposes removing an admin, **When** the proposal is submitted, **Then** quorum is bypassed (Owner override) and the removal is recorded directly after the target is excluded from the roster.
3. **Given** a register with 1 Owner and 1 Admin, **When** the Owner removes the Admin, **Then** the roster returns to a single Owner and the register remains valid.
4. **Given** a removal would leave only the Owner, **When** the Control transaction is recorded, **Then** the register continues to function with a single-admin roster.

---

### User Story 4 — Transfer Ownership (Priority: P2)

The current Owner proposes transferring ownership to an existing Admin. This is a two-party operation: the Owner signs the transfer proposal, and the target Admin counter-signs to accept ownership. Upon acceptance, the old Owner is demoted to Admin (always additive — the roster never shrinks from a transfer). A Control transaction records the change.

**Why this priority**: Ownership transfer enables succession planning and organisational changes but depends on the basic admin add/remove flow.

**Independent Test**: Can be tested by creating a register with 1 Owner + 1 Admin, having the Owner transfer to the Admin, and verifying the old Owner becomes an Admin and the new Owner has full authority.

**Acceptance Scenarios**:

1. **Given** a register with Owner A and Admin B, **When** Owner A proposes transferring ownership to Admin B, **Then** quorum is skipped (transfer is Owner + target only), Admin B is prompted to accept.
2. **Given** Admin B accepts the transfer, **When** the Control transaction is recorded, **Then** Admin B is the new Owner, Owner A is demoted to Admin, and the roster has the same number of members.
3. **Given** a transfer is proposed to a DID that is not currently an admin, **When** the proposal is validated, **Then** it is rejected — only existing admins can receive ownership.
4. **Given** Admin B declines the transfer, **When** the workflow processes the decline, **Then** no change occurs and the workflow returns to accept new proposals.

---

### User Story 5 — DID Resolution (Priority: P2)

The system supports two DID formats: `did:sorcha:w:{walletAddress}` for local wallet-based identity, and `did:sorcha:r:{registerId}:t:{txId}` for decentralized register-based identity. Wallet DIDs resolve via the local Wallet Service. Register DIDs resolve by fetching the referenced transaction from any peer holding a replica of the target register. Admin rosters can contain a mix of both DID types.

**Why this priority**: DID resolution is needed for any cross-instance governance and for peers to verify admin rights without central authority, but the governance workflow can initially operate with wallet DIDs alone.

**Independent Test**: Can be tested by resolving a `did:sorcha:w:*` against the local Wallet Service, publishing a credential as a transaction, and resolving the resulting `did:sorcha:r:*:t:*` from a different peer.

**Acceptance Scenarios**:

1. **Given** a DID in format `did:sorcha:w:{address}`, **When** the resolver processes it, **Then** it fetches the public key from the local Wallet Service.
2. **Given** a DID in format `did:sorcha:r:{registerId}:t:{txId}`, **When** the resolver processes it, **Then** it fetches the referenced transaction from a local replica or remote peer and extracts the public key from the payload.
3. **Given** an admin roster contains both DID formats, **When** a peer replays the Control chain, **Then** both DID types are accepted and the roster is reconstructed correctly.
4. **Given** a register DID references a transaction on a register that is not replicated locally, **When** resolution is attempted, **Then** the system attempts P2P resolution and returns a clear error if the register is unreachable.

---

### User Story 6 — Validator Rights Enforcement (Priority: P3)

When any transaction is submitted to a register, the Validator Service reconstructs the current admin roster by replaying all Control transactions. For governance operations (Control transactions), it verifies the submitter has the appropriate role (Owner for transfers, any admin for proposals). For regular blueprint actions, it verifies the submitter is authorized per the register's governance chain. No dependency on the centralized Tenant Service is required for rights checking.

**Why this priority**: Rights enforcement is the security guarantee that makes decentralized governance meaningful, but it builds on all previous stories.

**Independent Test**: Can be tested by submitting a Control transaction from a non-admin wallet and verifying it is rejected, then submitting from a valid admin and verifying it is accepted.

**Acceptance Scenarios**:

1. **Given** a register with known admin roster, **When** a non-admin submits a governance proposal, **Then** the Validator rejects the transaction with a clear authorization error.
2. **Given** a register with known admin roster, **When** an Admin submits a governance proposal, **Then** the Validator accepts the transaction after verifying the DID is in the roster.
3. **Given** a register where admin B was removed in a recent Control transaction, **When** admin B attempts to submit a new proposal, **Then** the Validator rejects it because admin B is no longer in the current roster.
4. **Given** the Validator is operating on a replicated register, **When** it needs to verify admin rights, **Then** it uses only the register's own Control chain — no call to the Tenant Service is made.

---

### Edge Cases

- What happens when the Owner's wallet key is compromised? The Owner can transfer ownership to another admin; if the compromise is discovered by other admins, they cannot override the Owner without the transfer mechanism — this is an accepted trade-off of Owner supremacy.
- What happens when the last remaining admin (Owner) loses access? The register becomes unmodifiable from a governance perspective. Recovery mechanisms are out of scope for this feature (future: social recovery via ZKP or multi-party recovery blueprints).
- What happens during concurrent governance proposals? Only one governance proposal can be active at a time per register — the blueprint instance is a single looping workflow. A second proposal must wait for the first to complete or be rejected.
- What happens when a register DID references a register that no longer exists? Resolution fails gracefully, and the admin entry is treated as unresolvable. Governance operations requiring that admin's signature cannot proceed until the DID is replaced.
- What happens when quorum is impossible (e.g., 2 admins and one is unreachable)? The proposal expires after 7 days and the workflow returns to proposal state. The Owner can bypass quorum or the reachable admin can propose removing the unreachable one (exclusion from quorum pool means 1/1 = 100% > 50%).
- What happens with the `m=2` strict majority edge case? With exactly 2 admins, >50% requires both (2/2). This is intentional — it prevents a single admin from acting unilaterally. The Owner's bypass provides an escape if deadlock occurs.

## Requirements *(mandatory)*

### Functional Requirements

#### DID Scheme

- **FR-001**: System MUST support DIDs in the format `did:sorcha:w:{walletAddress}` for wallet-based identity resolution.
- **FR-002**: System MUST support DIDs in the format `did:sorcha:r:{registerId}:t:{transactionId}` for register-based identity resolution.
- **FR-003**: System MUST resolve wallet DIDs (`did:sorcha:w:*`) via the local Wallet Service, returning the associated public key.
- **FR-004**: System MUST resolve register DIDs (`did:sorcha:r:*:t:*`) by fetching the referenced transaction from any available peer holding a replica.
- **FR-005**: System MUST accept admin rosters containing a mix of both DID formats.
- **FR-006**: System MUST validate DID format before accepting it in any governance operation (well-formed prefix, non-empty locator).

#### Transaction Type

- **FR-007**: System MUST rename `TransactionType.Genesis` (value 0) to `TransactionType.Control` (value 0) and remove `TransactionType.System` (value 3) entirely. The enum becomes: Control=0, Action=1, Docket=2.
- **FR-008**: The genesis transaction MUST be typed as `TransactionType.Control` (value 0) — existing persisted genesis transactions (integer 0) are automatically compatible with no data migration. Any existing System=3 transactions MUST be migrated to Control=0.
- **FR-009**: All governance operations (add/remove admin, transfer ownership) MUST be recorded as `TransactionType.Control` transactions.
- **FR-010**: Control transactions MUST be included in normal dockets alongside Action transactions — no separate governance chain.

#### Genesis & Ownership

- **FR-011**: System MUST create a genesis Control transaction when a new register is created, containing the creator's DID as Owner.
- **FR-012**: The genesis Control transaction payload MUST contain the initial admin roster (exactly one Owner entry).
- **FR-013**: The genesis blueprint (register governance workflow) MUST be automatically bound to every new register.
- **FR-014**: The genesis blueprint MUST be a well-known, system-seeded blueprint (e.g., `register-governance-v1`) published to the System Register.

#### Governance Workflow

- **FR-015**: The governance blueprint MUST implement a looping workflow: propose → collect quorum → accept → record → loop.
- **FR-016**: Any current admin (including the Owner) MUST be able to propose an Add, Remove, or Transfer operation.
- **FR-017**: Add and Remove operations MUST require approval from >50% of the voting admin pool (Owner + Admin roles only; Auditor and Designer are excluded from quorum).
- **FR-018**: For Remove operations, the target admin MUST be excluded from the quorum pool (m).
- **FR-019**: The Owner MUST be able to bypass quorum for Add and Remove operations (unilateral authority).
- **FR-020**: Transfer operations MUST require only the current Owner's signature and the target Admin's acceptance — no quorum.
- **FR-021**: Transfer MUST only be proposable by the current Owner and MUST target an existing Admin.
- **FR-022**: Upon successful ownership transfer, the old Owner MUST be demoted to Admin (always additive).
- **FR-023**: New admins (Add operation) and new owners (Transfer operation) MUST counter-sign to accept their role.
- **FR-024**: If the target declines acceptance, the workflow MUST return to the proposal state without modifying the roster.
- **FR-024a**: Governance proposals MUST expire after 7 days if quorum is not reached. Upon expiration, the workflow MUST return to the proposal state without modifying the roster. Expired proposals are recorded as failed Control transactions for audit purposes.

#### Control Transaction Payload

- **FR-025**: Each Control transaction payload MUST contain the full updated admin roster (not just the delta).
- **FR-026**: Each roster entry MUST include: DID, role (Owner/Admin), public key, signature, and grant timestamp.
- **FR-027**: Each Control transaction MUST include the operation metadata: operation type, proposer DID, approval signatures collected, and timestamp.
- **FR-028**: The admin roster in any Control transaction MUST have exactly one Owner entry.
- **FR-028a**: The governance blueprint MUST support Add and Remove operations for all four roles: Owner, Admin, Auditor, and Designer. Non-voting roles (Auditor, Designer) are tracked in the roster for access control but do not participate in quorum calculations.
- **FR-028b**: The admin roster MUST NOT exceed 25 total members across all roles. Add operations MUST be rejected if the roster is at capacity.

#### Roster Reconstruction

- **FR-029**: Any peer MUST be able to reconstruct the current admin roster by replaying all Control transactions in docket order.
- **FR-030**: The roster reconstruction algorithm MUST be deterministic — given the same Control chain, every peer MUST derive the same roster.
- **FR-031**: System MUST provide a roster reconstruction service that filters and replays Control transactions from the register.

#### Validator Rights Enforcement

- **FR-032**: The Validator MUST verify that the submitter of a Control transaction is a current admin (or Owner for transfers) by reconstructing the roster.
- **FR-033**: The Validator MUST NOT depend on the Tenant Service for rights verification — only the register's own Control chain.
- **FR-034**: The Validator MUST reject Control transactions from DIDs not present in the current admin roster.
- **FR-035**: The Validator MUST verify quorum requirements are met before accepting a governance Control transaction.

#### Future Requirements (ZKP — Deferred)

- **FR-036**: The system MUST be designed to accommodate zero-knowledge proof payloads in register DIDs (`did:sorcha:r:*:t:*`) in a future phase.
- **FR-037**: The DID resolution interface MUST be extensible to support additional proof types beyond plain public keys.

### Key Entities

- **DID (Decentralized Identifier)**: A self-sovereign identifier in format `did:sorcha:{type}:{locator}`. Two types: wallet-based (`w:`) for local resolution and register-based (`r:`) for decentralized resolution. Contains or references a public key for signature verification.
- **Admin Roster**: The current list of administrators for a register, derived by replaying all Control transactions. Contains exactly one Owner, zero or more Admins, and zero or more non-voting members (Auditor, Designer). Each entry has a DID, role, public key, signature, and grant timestamp. Only Owner and Admin roles participate in quorum votes.
- **Control Transaction**: A transaction of type `Control` that records a governance state change on the register. Contains the full updated admin roster and operation metadata. Stored in normal dockets.
- **Genesis Blueprint**: A well-known, system-seeded blueprint (`register-governance-v1`) that defines the looping governance workflow for every register. Automatically bound at register creation.
- **Governance Proposal**: An in-progress admin change request (Add/Remove/Transfer) that is working through the blueprint's quorum and acceptance flow.
- **Quorum**: The approval threshold for governance operations: strictly more than 50% of the applicable admin pool must approve. The pool includes only voting roles (Owner + Admin), excludes non-voting roles (Auditor, Designer), is adjusted for removals (target excluded), and the Owner can bypass.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every newly created register has a genesis Control transaction establishing ownership, verifiable by any peer within 5 seconds of replication.
- **SC-002**: Admin roster changes (add/remove/transfer) complete within a single governance loop cycle and are recorded as Control transactions in normal dockets.
- **SC-003**: Any peer holding a register replica can independently reconstruct the current admin roster from the Control chain in under 1 second for registers with up to 1,000 Control transactions.
- **SC-004**: Governance proposals from non-admin DIDs are rejected 100% of the time — zero unauthorized governance changes accepted.
- **SC-005**: The Validator performs rights verification using only the register's Control chain — zero calls to the Tenant Service for governance authorization.
- **SC-006**: Wallet DIDs (`did:sorcha:w:*`) resolve successfully against the local Wallet Service, and register DIDs (`did:sorcha:r:*:t:*`) resolve against any reachable peer holding the target register.
- **SC-007**: Quorum calculations correctly exclude the target admin for removal operations and correctly require >50% of the adjusted pool in all scenarios tested (m=1 through m=10).
- **SC-008**: Ownership transfer always results in the old Owner becoming an Admin — the roster size never decreases from a transfer operation.
- **SC-009**: The genesis governance blueprint supports indefinite looping without instance state corruption or memory growth for registers with up to 10,000 governance operations.

## Clarifications

### Session 2026-02-11

- Q: Should Auditor and Designer roles from the existing RegisterControlRecord be included in governance? → A: Include as non-voting governance roles — tracked in roster but excluded from quorum calculations. Only Owner and Admin participate in quorum votes.
- Q: Should Genesis=0 enum value be retained for backward compatibility? → A: No — rename Genesis=0 to Control=0. Remove System=3 entirely. Existing genesis transactions (persisted as integer 0) are automatically compatible. Any existing System=3 transactions must be migrated to Control=0.
- Q: Should the governance roster have a maximum member cap? → A: Yes, 25 total members across all roles. Sufficient for real-world governance while bounding quorum collection time and payload size.
- Q: Should governance proposals have an expiration timeout? → A: Yes, 7-day timeout. Proposals that don't reach quorum within 7 days expire automatically and the workflow returns to the proposal state.

## Assumptions

- **A-001**: The existing blueprint engine's loop/cycle support (route-based routing with cycle warnings) is sufficient for the governance workflow — no engine changes needed for looping.
- **A-002**: Wallet DIDs are the primary identity mechanism for the initial implementation. Register DIDs are supported but ZKP payloads are deferred.
- **A-003**: Only one governance proposal can be active at a time per register (single-instance blueprint loop). Concurrent proposals are queued.
- **A-004**: The genesis blueprint is identical for all registers — it is a well-known system blueprint, not customizable per register.
- **A-005**: Renaming `TransactionType.Genesis` (0) to `Control` (0) is a non-breaking change for persisted data since the integer value is preserved. Removing `System` (3) requires migrating any existing System transactions to Control (0).
- **A-006**: Cross-register DID resolution (where Register A references a DID on Register B) may introduce latency and availability dependencies. This is an accepted trade-off documented in edge cases.
- **A-007**: Register-based DIDs initially contain plain public keys in the transaction payload. ZKP support is a documented future requirement.

## Dependencies

- **Blueprint Engine**: Must support the looping governance workflow (existing cycle support assumed sufficient).
- **Validator Service**: Must be extended to reconstruct admin rosters and enforce governance rights.
- **Register Service**: Must support `TransactionType.Control` and the genesis blueprint binding.
- **Wallet Service**: Must support DID resolution for `did:sorcha:w:*` format.
- **Peer Service**: Must support register DID resolution for `did:sorcha:r:*:t:*` via P2P.
- **System Register**: Must host the governance blueprint (`register-governance-v1`).
