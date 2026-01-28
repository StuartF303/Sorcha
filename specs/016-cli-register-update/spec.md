# Feature Specification: CLI Register Commands Update

**Feature Branch**: `016-cli-register-update`
**Created**: 2026-01-28
**Status**: Draft
**Input**: Update Sorcha.CLI to use shared common model libraries (Sorcha.Register.Models, Sorcha.Blueprint.Models) instead of duplicate local DTOs, and implement proper two-phase register creation (initiate/finalize with cryptographic attestations). Also add missing CLI commands for dockets, query API, register update, and register stats to match the full Register Service backend.

## Clarifications

### Session 2026-01-28

- Q: When creating a register with multiple owners, how should the CLI handle signing attestations for wallets the current user does not control? → A: Single-owner only for now. CLI signs with the current user's wallet. Multi-owner register creation is out of scope for this CLI update.
- Q: Should all new commands support machine-readable JSON output in addition to human-readable table format? → A: Yes. All new commands support both table (default) and JSON output via the existing `--output` global flag.
- Q: Should the CLI expose OData querying in addition to the dedicated query commands? → A: Yes. Add a `sorcha query odata` command that passes raw OData query strings (filter, orderby, top, skip) to the backend.
- Q: Should the old `--org-id` flag on `register create` be preserved as a deprecated alias for `--tenant-id`? → A: No. Replace entirely with `--tenant-id` and `--owner-wallet`. No backward compatibility aliases.

## User Scenarios & Testing

### User Story 1 - Create a Register with Cryptographic Attestations (Priority: P1)

An administrator creates a new distributed ledger register using the CLI. The system guides them through a two-phase process: first initiating the register (which returns attestation data to sign), then finalizing the register by providing the signed attestations. The CLI orchestrates signing via the user's wallet automatically.

**Why this priority**: Register creation is the foundational operation for the platform. The current CLI uses a simplified creation flow that does not match the backend's two-phase cryptographic attestation process. Without this, registers created via the CLI lack the security guarantees the platform is designed to provide.

**Independent Test**: Can be fully tested by running `sorcha register create` with valid parameters and verifying a register is created with a genesis transaction and docket.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a wallet, **When** they run `sorcha register create --name "My Register" --tenant-id <id> --owner-wallet <walletId>`, **Then** the CLI initiates register creation, signs the attestation using the specified wallet, finalizes the register, and displays the register ID, genesis transaction ID, and genesis docket ID.
2. **Given** an initiation response that expires in 5 minutes, **When** the user takes too long to sign, **Then** the CLI displays a clear expiration error and instructs the user to retry.
4. **Given** an invalid wallet ID or signing failure, **When** the attestation signing fails, **Then** the CLI displays a meaningful error indicating which attestation failed and why.

---

### User Story 2 - Use Shared Models Instead of Duplicate DTOs (Priority: P1)

The CLI references the same shared model libraries (Sorcha.Register.Models, Sorcha.Blueprint.Models) used by the backend services and the UI, instead of maintaining its own duplicate DTO definitions. This ensures the CLI stays in sync with backend changes and reduces maintenance burden.

**Why this priority**: The current duplicate models are already out of sync with the backend - missing fields like Height, Status, Advertise, IsFullReplica, and Votes. Every backend change risks silent CLI breakage. This is a structural prerequisite for all other stories.

**Independent Test**: Can be tested by building the CLI project with shared model references, verifying it compiles, and confirming the `register list` and `register get` commands display all fields from the shared models.

**Acceptance Scenarios**:

1. **Given** the CLI project references Sorcha.Register.Models, **When** a user runs `sorcha register get --id <id>`, **Then** the output includes all register fields (Height, Status, Advertise, IsFullReplica, Votes, TenantId, CreatedAt, UpdatedAt) rather than the current subset.
2. **Given** the shared models are updated in a future release, **When** the CLI is rebuilt, **Then** it automatically picks up the new fields without requiring CLI-specific model changes.
3. **Given** the CLI previously had local Register, Transaction, CreateRegisterRequest, SubmitTransactionRequest, and SubmitTransactionResponse models, **When** the migration is complete, **Then** those local model files are removed and all commands use shared types.

---

### User Story 3 - Browse Dockets in a Register (Priority: P2)

An administrator inspects the sealed blocks (dockets) of a register to audit the ledger structure. They can list all dockets, view a specific docket's details (hash, previous hash, state, timestamps), and see which transactions are contained within a docket.

**Why this priority**: Dockets are the core unit of the immutable ledger. Inspecting them is essential for auditing and debugging register integrity, but depends on having registers created first.

**Independent Test**: Can be tested by running `sorcha docket list --register-id <id>` against a register that has transactions, and verifying docket details are displayed.

**Acceptance Scenarios**:

1. **Given** a register with committed dockets, **When** the user runs `sorcha docket list --register-id <id>`, **Then** the CLI displays a table of dockets with ID, hash, state, transaction count, and timestamp.
2. **Given** a specific docket ID, **When** the user runs `sorcha docket get --register-id <id> --docket-id <docketId>`, **Then** the CLI displays full docket details including hash, previous hash, state, metadata, and votes.
3. **Given** a docket with transactions, **When** the user runs `sorcha docket transactions --register-id <id> --docket-id <docketId>`, **Then** the CLI lists all transactions contained in that docket.

---

### User Story 4 - Query Transactions Across Registers (Priority: P2)

An administrator or auditor queries transactions across the platform by wallet address, sender address, or blueprint ID. They can also view aggregate transaction statistics. This enables cross-register auditing without needing to know which specific register a transaction belongs to.

**Why this priority**: Cross-register querying is critical for audit and compliance workflows, but is secondary to the core register creation and docket inspection capabilities.

**Independent Test**: Can be tested by running `sorcha query wallet --address <addr>` and verifying matching transactions are returned with pagination support.

**Acceptance Scenarios**:

1. **Given** transactions exist from a specific wallet, **When** the user runs `sorcha query wallet --address <address>`, **Then** the CLI displays matching transactions with pagination.
2. **Given** transactions exist from a specific sender, **When** the user runs `sorcha query sender --address <address>`, **Then** the CLI displays matching transactions.
3. **Given** transactions linked to a blueprint, **When** the user runs `sorcha query blueprint --id <blueprintId>`, **Then** the CLI displays matching transactions.
4. **Given** the platform has transaction data, **When** the user runs `sorcha query stats`, **Then** the CLI displays aggregate statistics (total transactions, register count, etc.).
5. **Given** a query returns many results, **When** the user specifies `--page` and `--page-size`, **Then** results are paginated accordingly.
6. **Given** the user needs a complex query, **When** they run `sorcha query odata --resource Transactions --filter "RegisterId eq 'abc'" --orderby "TimeStamp desc" --top 10`, **Then** the CLI sends the OData query to the backend and displays matching results.

---

### User Story 5 - Update Register Metadata (Priority: P3)

An administrator updates a register's metadata (name, status, advertise flag) after creation. This allows registers to be renamed, taken offline, or made discoverable on the peer network.

**Why this priority**: Updating register metadata is useful but less frequently needed than creation, querying, and auditing.

**Independent Test**: Can be tested by running `sorcha register update --id <id> --name "New Name"` and verifying the register's name changes.

**Acceptance Scenarios**:

1. **Given** an existing register, **When** the user runs `sorcha register update --id <id> --name "New Name"`, **Then** the register name is updated and the new name is displayed.
2. **Given** an existing register, **When** the user runs `sorcha register update --id <id> --status Offline`, **Then** the register status changes to Offline.
3. **Given** an existing register, **When** the user runs `sorcha register update --id <id> --advertise true`, **Then** the register becomes discoverable on the peer network.

---

### User Story 6 - View Register Statistics (Priority: P3)

An administrator checks overall register statistics to understand platform usage (total register count).

**Why this priority**: Statistics provide operational visibility but are not essential for core register management workflows.

**Independent Test**: Can be tested by running `sorcha register stats` and verifying a count is returned.

**Acceptance Scenarios**:

1. **Given** registers exist on the platform, **When** the user runs `sorcha register stats`, **Then** the CLI displays the total number of registers.
2. **Given** no registers exist, **When** the user runs `sorcha register stats`, **Then** the CLI displays a count of zero.

---

### Edge Cases

- What happens when the user's wallet service is unreachable during register creation signing?
- What happens when a register creation initiation expires before finalization (5-minute window)?
- ~~What happens when one of multiple owners fails to sign during multi-owner register creation?~~ (Out of scope: single-owner only)
- What happens when querying with a wallet address that has no transactions?
- What happens when attempting to update a register that does not exist?
- What happens when pagination parameters exceed available results?
- What happens when the backend returns fields the CLI does not expect (forward compatibility of shared models)?

## Requirements

### Functional Requirements

- **FR-001**: CLI project MUST reference Sorcha.Register.Models and Sorcha.Blueprint.Models shared libraries instead of maintaining duplicate local model definitions.
- **FR-002**: Local CLI model files for Register, Transaction, CreateRegisterRequest, SubmitTransactionRequest, and SubmitTransactionResponse MUST be removed after migration to shared models.
- **FR-018**: The `register create` command MUST replace the current `--org-id` flag with `--tenant-id` and add `--owner-wallet`. No backward compatibility aliases for removed flags.
- **FR-003**: CLI register model serialization MUST remain compatible with the Register Service API responses (handle any property name differences between shared models and API JSON).
- **FR-004**: The `register create` command MUST implement the two-phase creation flow: initiate (Phase 1) returns attestation data, CLI signs attestations via wallet service, finalize (Phase 2) submits signed attestations.
- **FR-005**: The `register create` command MUST support single-owner register creation. Multi-owner register creation is out of scope for this update.
- **FR-006**: The `register create` command MUST display the register ID, genesis transaction ID, and genesis docket ID upon successful creation.
- **FR-007**: The `register create` command MUST handle attestation expiration (5-minute window) with a clear error message.
- **FR-008**: The CLI MUST provide a `docket` command group with `list`, `get`, and `transactions` subcommands for docket inspection.
- **FR-009**: The CLI MUST provide a `query` command group with `wallet`, `sender`, `blueprint`, `stats`, and `odata` subcommands for cross-register querying.
- **FR-017**: The `query odata` subcommand MUST accept `--resource` (Registers, Transactions, or Dockets), `--filter`, `--orderby`, `--top`, `--skip`, `--select`, and `--count` options corresponding to OData v4 query parameters.
- **FR-010**: Query commands MUST support pagination via `--page` and `--page-size` options.
- **FR-011**: The CLI MUST provide a `register update` subcommand to update register name, status, and advertise flag.
- **FR-012**: The CLI MUST provide a `register stats` subcommand to display total register count.
- **FR-013**: The CLI Refit service client interface MUST be updated to include endpoints for dockets, queries, OData, register update, register stats, and two-phase register creation (initiate/finalize).
- **FR-014**: All new commands MUST follow existing CLI patterns for authentication, error handling, and output formatting.
- **FR-016**: All new commands MUST support both human-readable table output (default) and machine-readable JSON output via the existing `--output` global flag.
- **FR-015**: The `register list` and `register get` commands MUST display all fields available from the shared Register model (Height, Status, TenantId, Advertise, Votes, CreatedAt, UpdatedAt).

### Key Entities

- **Register**: A distributed ledger instance with identity, ownership, height, status, and configuration (advertise, replication). Identified by a 32-character hex ID.
- **Docket**: A sealed block of transactions within a register. Contains a hash chain (hash, previous hash), transaction IDs, state, metadata, and votes.
- **Transaction**: An individual ledger entry with sender wallet, payloads, signature, and chain linkage (previous transaction ID).
- **Attestation**: A cryptographically signed statement by a register owner/admin approving register creation. Contains role, subject, register identity, and timestamp. Signed using SHA-256 hash of canonical JSON.
- **Control Record**: An immutable ownership record for a register, constructed from verified attestations during finalization.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All register management operations available through the backend API (create, read, update, delete, list, stats) are accessible through CLI commands.
- **SC-002**: All docket inspection operations (list, get, view transactions) are accessible through CLI commands.
- **SC-003**: All cross-register query operations (by wallet, sender, blueprint, and stats) are accessible through CLI commands.
- **SC-004**: Register creation via CLI produces cryptographically valid attestations and genesis blocks identical to those created via the API directly.
- **SC-005**: The CLI project contains zero duplicate model definitions for entities that exist in shared common libraries.
- **SC-006**: Existing CLI commands (register list, register get, register delete, tx list, tx get, tx submit, tx status) continue to function correctly after migration to shared models.
- **SC-007**: All new commands provide clear, actionable error messages for authentication failures, authorization failures, not-found responses, validation errors, and network errors.

## Assumptions

- The wallet service API is available for signing attestation data during register creation. The CLI will call the wallet service to perform signing rather than requiring users to sign externally.
- The CLI's existing authentication flow (JWT Bearer tokens via `sorcha auth login`) provides sufficient authorization for all new endpoints.
- The existing Refit-based HTTP client pattern will be extended (not replaced) for new endpoints.
- The API Gateway already routes `/api/registers`, `/api/query`, and docket endpoints to the Register Service (confirmed from YARP configuration).
- CLI-specific DTOs for non-register entities (Wallet, Organization, User, Profile, Bootstrap, Peer, auth models) remain as-is since those shared model libraries serve different purposes.
- Pagination in the query API uses `page`/`pageSize` parameters matching the backend convention, not the CLI's current `skip`/`take` pattern. Existing transaction list pagination will be aligned.
