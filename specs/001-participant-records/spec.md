# Feature Specification: Published Participant Records on Register

**Feature Branch**: `001-participant-records`
**Created**: 2026-02-20
**Status**: Draft
**Input**: User description: "Add a new TransactionType.Participant for publishing participant identity records as transactions on a register. Participants are the principals in blueprints that enact actions. Published records contain organization name, participant name, status, version, an array of addresses with multiple algorithm support, and optional metadata. Publishing is signed by the user and submitted via the normal mempool/validator route."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Publish a Participant Record to a Register (Priority: P1)

An organization administrator wants to make a participant (person, team, or service) discoverable on a register so that blueprint creators can assign actions to them. The admin selects a register, provides the participant's display name, organization name, one or more wallet addresses (each with its algorithm and public key), and optional metadata such as a description or links. The system creates a signed transaction containing this participant record and submits it through the standard validation pipeline. Once validated and written to the register, the participant is discoverable by any node on the network.

**Why this priority**: This is the foundational capability. Without the ability to publish participant records, no other participant discovery or resolution features can function.

**Independent Test**: Can be fully tested by publishing a participant record to a register and confirming it appears in the register's transaction history as a Participant-type transaction with correct content.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a linked wallet on a register, **When** they submit a participant record with a valid name, organization, and at least one address entry, **Then** a Participant-type transaction is created, signed by the user's wallet, submitted to the mempool, validated, and written to the register.
2. **Given** a participant record with multiple address entries (e.g., ED25519 and P-256), **When** the record is published, **Then** all addresses are stored in the transaction payload and each is independently indexed for lookup.
3. **Given** a participant record submission, **When** the validator processes it, **Then** it verifies the transaction signature, validates the record content against the participant record schema, and checks chain integrity — but does NOT require governance roster authorization.
4. **Given** a participant record with missing required fields (no name, no addresses), **When** submitted, **Then** the validator rejects the transaction with a clear validation error.

---

### User Story 2 - Look Up Published Participants on a Register (Priority: P1)

A user building a blueprint or routing an action needs to find published participants on a register. They can query by wallet address (any algorithm) to resolve the participant's published name, organization, and available addresses. They can also list all published participants on a register. The system returns the latest version of each participant record, filtering out revoked records by default.

**Why this priority**: Publishing records without the ability to query them provides no value. Publish and lookup are co-dependent for a minimum viable feature.

**Independent Test**: Can be tested by publishing one or more participant records and querying the register to retrieve them by address, confirming the correct record is returned with all address entries.

**Acceptance Scenarios**:

1. **Given** a register with published participant records, **When** a user queries by wallet address, **Then** the system returns the latest version of the participant record containing that address, regardless of which algorithm's address was used in the query.
2. **Given** a register with multiple published participants, **When** a user lists all participants, **Then** the system returns the latest version of each participant, excluding revoked records by default.
3. **Given** a participant published with version 1 and later updated to version 2, **When** queried, **Then** only the version 2 record is returned.
4. **Given** a wallet address that does not match any published participant, **When** queried, **Then** the system returns an empty result (not an error).

---

### User Story 3 - Update a Published Participant Record (Priority: P2)

A participant's details change — they add a new wallet address (e.g., switching to a different algorithm), update their metadata, or change their published name. An authorized user publishes a new version of the participant record to the same register. The new version supersedes the previous one. The full history remains on the chain for audit purposes.

**Why this priority**: Participants will inevitably need to update their published details. Without versioning, the system becomes rigid and forces workarounds.

**Independent Test**: Can be tested by publishing a participant record, then publishing an updated version, and confirming queries return only the latest version while both transactions exist on the chain.

**Acceptance Scenarios**:

1. **Given** an existing published participant record at version N, **When** an authorized user publishes an update, **Then** a new Participant transaction is written with version N+1 carrying the same participant ID. The participant name and organization name may be changed.
2. **Given** a version 2 record that adds a P-256 address to an ED25519-only version 1, **When** queried, **Then** both addresses are available from the latest record.
3. **Given** an update transaction, **When** validated, **Then** the validator confirms the update's PrevTxId references the previous version of that participant (by participant ID) and rejects it if a fork is detected within the participant's version chain.

---

### User Story 4 - Revoke or Deprecate a Participant Record (Priority: P2)

An organization needs to retire a participant — perhaps a service desk is being decommissioned, or a wallet address has been compromised. An authorized user publishes a new version with a status of "deprecated" (still usable but flagged for transition) or "revoked" (no longer valid for routing actions). Revoked participants are excluded from default queries but remain on the chain for audit.

**Why this priority**: Without lifecycle management, compromised or retired participants remain active indefinitely, creating security and operational risks.

**Independent Test**: Can be tested by publishing a participant, then revoking it, and confirming it no longer appears in default participant listings but its transaction history is preserved.

**Acceptance Scenarios**:

1. **Given** an active published participant, **When** an authorized user publishes a version with status "revoked", **Then** the participant is excluded from default queries and lookups.
2. **Given** a deprecated participant, **When** queried with an explicit flag to include deprecated records, **Then** the participant is returned with its deprecated status visible.
3. **Given** a revoked participant, **When** a blueprint attempts to route an action to its address, **Then** the system warns that the participant has been revoked.
4. **Given** a revoked participant, **When** querying the register's full transaction history, **Then** all versions (active, deprecated, revoked) are visible for audit.

---

### User Story 5 - Resolve Participant Address for Encryption (Priority: P3)

When building or executing a blueprint, the system needs to resolve a participant's public key for field-level encryption. Given a participant's wallet address, the system looks up their published record on the register, retrieves the public key for the matching algorithm (or the primary address if no specific algorithm is requested), and uses it to encrypt data intended only for that participant.

**Why this priority**: Field-level encryption is a key DAD security model capability, but it depends on published records and lookup being in place first. This story represents the downstream integration payoff.

**Independent Test**: Can be tested by publishing a participant record with multiple addresses, then resolving the public key for a specific algorithm and confirming it matches the published value.

**Acceptance Scenarios**:

1. **Given** a published participant with ED25519 and P-256 addresses, **When** the system requests the public key for ED25519, **Then** the correct public key for that algorithm is returned.
2. **Given** a published participant with a primary address marked, **When** the system requests the public key without specifying an algorithm, **Then** the primary address's public key is returned.
3. **Given** a participant whose record has been revoked, **When** the system attempts to resolve their public key, **Then** the resolution fails with a clear indication that the participant is revoked.

---

### Edge Cases

- What happens when two participants on the same register publish records containing the same wallet address? The system rejects the second publication — a wallet address can only be claimed by one participant record per register.
- What happens when a participant record is published with zero addresses? The validator rejects it — at least one address entry is required.
- What happens when a participant record has multiple addresses but none marked as primary? The system treats the first address in the array as the default.
- What happens when an update changes the organization name or participant name? This is permitted — the participant ID (UUID) is the identity anchor, not the names. Names are informational and can be updated across versions.
- What happens when the publishing user's wallet is different from any address in the participant record? This is expected and valid — the publisher is an authorized user acting on behalf of the participant.
- What happens when the same participant is published to multiple registers? Each register holds an independent copy. The participant's DID encodes the register, so each publication is a distinct identity assertion on that register.
- What happens when version numbers are non-sequential (e.g., version 1 then version 5)? The system accepts it — the highest version number on the register is the current record, regardless of gaps.
- What happens when two users simultaneously submit version 2 for the same participant? The first to be validated and written wins; the second is rejected as a fork within the participant's version chain (same PrevTxId). The rejected submitter must re-fetch the current version and resubmit.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support a new transaction type "Participant" (value 3) distinct from Control, Action, and Docket transactions.
- **FR-002**: A Participant transaction payload MUST contain: participant ID (UUID, required — system-generated on first publication, carried unchanged in all subsequent versions), organization name (string, required), participant name (string, required), status (one of: active, deprecated, revoked), version (positive integer), and an addresses array.
- **FR-003**: Each entry in the addresses array MUST contain: wallet address (string, required), public key (base64-encoded, required), algorithm identifier (string, required — e.g., ED25519, P-256, RSA-4096), and a primary flag (boolean, default false).
- **FR-004**: A participant record MUST contain at least one address entry. If no address is marked as primary, the first address in the array is treated as the default.
- **FR-005**: The participant record MUST NOT contain a participant type field (human, machine, or service). The nature of the participant is private to the publishing organization.
- **FR-006**: Publishing a participant record MUST be signed by the wallet of the user performing the publication. The publishing user's wallet address may differ from the addresses listed in the participant record.
- **FR-007**: Participant transactions MUST be submitted through the standard mempool and validation pipeline, following the same route as all other transaction types.
- **FR-008**: The validator MUST apply these rules to Participant transactions: signature verification, participant record schema validation, and chain integrity checks. The validator MUST NOT require governance roster authorization for Participant transactions — authorization is enforced by the Tenant Service at submission time.
- **FR-009**: The register MUST index all wallet addresses contained in a participant record's addresses array for fast lookup by any individual address.
- **FR-010**: A wallet address MUST be unique per register — if an active participant record already claims a given wallet address on a register, a new participant record claiming the same address MUST be rejected unless it is a version update from the same participant (matched by participant ID).
- **FR-011**: Updating a participant record MUST be done by publishing a new Participant transaction with an incremented version number. The latest (highest version) record for a given participant on a register is the current record.
- **FR-012**: Revoking or deprecating a participant MUST be done by publishing a new version with the status set to "deprecated" or "revoked". Revoked participants MUST be excluded from default query results.
- **FR-013**: The register MUST provide a query interface to list published participants, retrieve a participant by wallet address, and filter by status. Queries MUST return only the latest version of each participant by default.
- **FR-014**: The participant record payload MUST support an optional metadata field (JSON object) for extensible properties such as description, links, and capabilities.
- **FR-015**: A participant MAY be published to any register. The default target register is the one containing the blueprint in which the participant is involved.
- **FR-016**: The Tenant Service MUST provide an endpoint for authorized users to initiate participant record publication, building the transaction payload and submitting it to the validation pipeline. The Tenant Service MUST enforce organization-level authorization (org admin or designated role) before accepting the request.
- **FR-017**: All versions of a participant record MUST remain on the register's immutable transaction chain for audit purposes, even after revocation.
- **FR-018**: The first Participant transaction for a new participant MUST chain from the latest Control transaction (genesis or blueprint-publish) on the register. Version updates MUST chain from the previous version of the same participant (matched by participant ID), forming a per-participant sub-chain.
- **FR-019**: The validator MUST allow multiple Participant transactions to reference the same Control transaction as their predecessor (multiple participants forking from the same Control TX is expected). The validator MUST NOT allow forks within a single participant's version chain (two updates claiming the same previous version).

### Key Entities

- **ParticipantRecord**: The content payload of a Participant transaction. Contains participant ID (UUID, immutable identity anchor), organization name, participant name, status, version, addresses array, and optional metadata. Represents a discoverable identity assertion on a register.
- **ParticipantAddress**: An entry in the addresses array. Contains wallet address, public key, algorithm identifier, and primary flag. Represents one cryptographic identity for the participant.
- **Participant Transaction**: A register transaction of type Participant (value 3). Wraps a ParticipantRecord as its payload. Signed by the publishing user. Chained and validated like other transactions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An authorized user can publish a participant record containing up to 5 address entries across different algorithms and have it written to the register within the same time bounds as other transaction types.
- **SC-002**: Any node on the network can resolve a participant's published record by wallet address in a single query, regardless of which algorithm's address is used for lookup.
- **SC-003**: Publishing an updated version of a participant record correctly supersedes the previous version — queries return only the latest version by default.
- **SC-004**: Revoking a participant removes it from default query results while preserving the full version history for audit review.
- **SC-005**: The validation pipeline processes Participant transactions without requiring governance roster authorization, reducing the barrier to entry for participant publication compared to Control transactions.
- **SC-006**: A participant published with multiple addresses has all addresses independently indexed — lookup by any one address resolves the full participant record.
- **SC-007**: Unit test coverage for new participant transaction handling, validation rules, and query logic meets the project minimum of 85%.

## Assumptions

- The publishing user already has a linked wallet in the Tenant Service (existing wallet linking flow).
- The register to which the participant is published already exists (created via the existing register creation flow).
- Version numbers are managed by the publishing client (Tenant Service) — the register does not auto-increment versions.
- The participant record schema is validated by the validator using the same JSON Schema validation infrastructure used for blueprint payloads.
- No governance roster check is needed because organization-level authorization is enforced by the Tenant Service before submission — the register and validator treat the signed transaction as self-authorizing.
- The "primary" flag on addresses is informational for consumers — the register and validator do not enforce any behaviour based on it.
- Metadata content is opaque to the register and validator — it is stored and returned as-is without interpretation.

## Scope Boundaries

### In Scope
- New TransactionType.Participant enum value
- ParticipantRecord content model and validation schema
- Validator rules for Participant transactions
- Register indexing and query endpoints for published participants
- Tenant Service endpoint to build and submit participant publication transactions
- Version management (publish, update, deprecate, revoke)
- Service client methods for participant record queries

### Out of Scope (Future Phases)
- External identity provider (OIDC) integration for participant authentication
- API key management for machine participants
- Blueprint participant resolution by address (Phase 2 integration)
- Field-level encryption using published public keys (Phase 2)
- DID document generation and resolution endpoints
- Peer-to-peer participant record replication and synchronization
- UI components for participant management
- Organization-level wallet signing (currently uses individual user wallet)
- Migration of participant publication authorization to the register governance/control system

## Clarifications

### Session 2026-02-20

- Q: What uniquely identifies a participant across versions on a register? → A: A system-generated participant ID (UUID) assigned at first publication and carried in all subsequent versions. This allows renaming participant name and organization name across versions.
- Q: Who is authorized to update or revoke a published participant record? → A: The Tenant Service enforces organization-level authorization (org admin or designated role); the validator trusts any validly-signed Participant transaction. Future: migrate authorization to the register's governance/control system.
- Q: What does PrevTxId reference for Participant transactions? → A: First publication chains from the latest Control TX (genesis or blueprint-publish); updates chain from the previous version of that same participant (by participant ID), forming a per-participant sub-chain.

## Dependencies

- Existing register and transaction infrastructure (Register Service, Validator Service)
- Existing mempool submission pipeline
- Existing wallet linking in Tenant Service (for signing)
- Existing JSON Schema validation infrastructure in Validator Service
- Existing service client patterns in Sorcha.ServiceClients
