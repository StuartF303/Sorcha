# Feature Specification: Register Creation with Genesis Record and Administrative Control

**Feature Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Draft
**Input**: User description: "lets add some requirements around register creation. We know that we will need to create the new storage in the database to store the dockets but will need to create the genesis record, this will contain a register control record that lists who has administratve control over the register. initially its whom ever creates it (other can be added at this point). I think we might design this using a blueprint workflow. so lets think about how we might do that"

## Clarifications

### Session 2025-12-13

- Q: How frequently should peer nodes synchronize the system register with central nodes? → A: 5-minute periodic synchronization with push notification support for immediate propagation when blueprints are published
- Q: What should be the exponential backoff parameters for peer connection retries? → A: Initial: 1s, Multiplier: 2x, Max: 1min
- Q: Should peer nodes maintain connections to multiple central nodes simultaneously, or only connect to one at a time? → A: Connect to first reachable only, switch to another central node on failure
- Q: How should peer nodes detect that their connection to a central node has failed? → A: 30s heartbeat timeout
- Q: What is the purpose and scope of the active peers list? → A: Maintained by each peer node locally for tracking its own connection status and network awareness

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create New Register with Self as Administrator (Priority: P1)

A user needs to create a new register to track a specific type of transaction or data lineage. Upon creation, they become the initial administrator with full control over the register, and the system creates a genesis record that establishes the register's authority chain.

**Why this priority**: This is the foundational capability - without the ability to create a register with initial administrative control, no other register operations are possible. It establishes the trust anchor for all subsequent transactions.

**Independent Test**: Can be fully tested by creating a register through the workflow, verifying the genesis record exists in storage, confirming the creator is listed as administrator in the control record, and validating the register is operational for recording dockets.

**Acceptance Scenarios**:

1. **Given** a user with valid credentials and register creation permissions, **When** they initiate the register creation workflow, **Then** the system creates a new register with a unique identifier, generates a genesis record containing a control record listing the creator as the initial administrator, stores the genesis record in the database, and confirms successful creation to the user.

2. **Given** a newly created register with a genesis record, **When** querying the register's control record, **Then** the system returns the creator's identity as the sole administrator with full permissions (add administrators, modify control, record transactions).

3. **Given** a register creation request with a creator identity, **When** the genesis record is created, **Then** the genesis record contains: register metadata (name, purpose, creation timestamp), a control record with the creator as administrator, a cryptographic signature establishing authenticity, and is immutably stored as the first record in the register.

---

### User Story 2 - Add Additional Administrators to Register (Priority: P2)

An existing register administrator needs to delegate administrative control by adding additional administrators to the register's control record, allowing shared governance and operational continuity.

**Why this priority**: While not essential for initial register creation, this enables governance scaling and prevents single points of failure. It's critical for production use but can be deferred after basic creation works.

**Independent Test**: Can be tested independently by creating a register (using P1 functionality), then using the administrative interface to add another user as an administrator, verifying the control record updates, and confirming the new administrator can perform administrative actions.

**Acceptance Scenarios**:

1. **Given** a user who is an administrator of a register, **When** they add another user as an administrator through the workflow, **Then** the system creates a new control record transaction, updates the register's control record to include the new administrator, records the change with timestamp and initiator identity, and notifies the new administrator.

2. **Given** a register with multiple administrators, **When** querying the control record, **Then** the system returns all current administrators with their respective permissions and the timestamp when each was added.

3. **Given** an attempt to add an administrator by a non-administrator, **When** the system validates permissions, **Then** the request is rejected with an authorization error, and no changes are made to the control record.

---

### User Story 3 - Orchestrate Register Creation via Blueprint Workflow (Priority: P1)

The register creation process should be defined as a reusable blueprint workflow that orchestrates the steps: validate creator permissions, allocate database storage, generate genesis record, create control record, and confirm completion. This ensures consistency and auditability.

**Why this priority**: Using a blueprint workflow for register creation provides several critical benefits: standardized process enforcement, audit trail of all creation steps, ability to customize creation workflows for different register types, and integration with the existing Sorcha workflow engine. This is P1 because it establishes the architectural pattern for register lifecycle management.

**Independent Test**: Can be tested by executing a register creation blueprint, monitoring workflow execution through each action, verifying all required steps complete successfully, confirming the genesis record reflects the workflow execution history, and validating that failed workflows rollback cleanly without orphaned resources.

**Acceptance Scenarios**:

1. **Given** a register creation blueprint is defined with actions for storage allocation, genesis record creation, and control record initialization, **When** a user triggers the workflow with register parameters (name, purpose, type), **Then** the workflow executes each action sequentially, creates database storage for dockets, generates the genesis record with control record, records workflow completion in the genesis record metadata, and returns the new register identifier.

2. **Given** a register creation workflow in progress, **When** any step fails (e.g., database error, permission denied), **Then** the workflow rolls back all completed steps, cleans up any allocated resources, records the failure reason, and notifies the initiator with actionable error information.

3. **Given** multiple concurrent register creation workflows, **When** they execute simultaneously, **Then** each workflow completes independently without conflicts, unique register identifiers are assigned, and all genesis records are properly isolated in their respective storage.

---

### User Story 4 - System Register for Blueprint Publication and Replication (Priority: P1)

The system needs a special system register to store published blueprints (including the register creation blueprint itself) that can be replicated across peer nodes. This system register must be seeded into a central node during system initialization and serve as the authoritative source for blueprint distribution.

**Why this priority**: This is foundational infrastructure - the register creation blueprint must be published somewhere for nodes to discover and execute it. Without a system register, there's no mechanism to distribute blueprints across the network. This is P1 because it's required for the entire blueprint-based register creation to function in a distributed environment.

**Independent Test**: Can be tested by initializing a central node, verifying the system register is created and seeded with the register creation blueprint, connecting a peer node, triggering replication, and confirming the peer node receives and can execute the blueprint from its local replica of the system register.

**Acceptance Scenarios**:

1. **Given** a central node initializing for the first time, **When** the system bootstrap process runs, **Then** the system creates a special system register with a well-known identifier, generates its genesis record with the system as the administrator, seeds the register creation blueprint into the system register, and marks the node as ready to accept peer connections.

2. **Given** a peer node connecting to a central node with a system register, **When** the peer initiates replication, **Then** the system register and all its published blueprints (including register creation blueprint) are replicated to the peer node, the peer can query its local copy of the system register, and the peer can execute blueprints from the replicated system register.

3. **Given** a new blueprint is published to the system register on any node, **When** replication occurs, **Then** the blueprint propagates to all connected peer nodes, the publication transaction is recorded with timestamp and publisher identity, and all nodes can execute the newly published blueprint.

4. **Given** the system register already exists on a node, **When** the system restarts, **Then** the bootstrap process detects the existing system register, skips creation, validates its integrity, and resumes normal operations without creating duplicate system registers.

5. **Given** a peer service starting up on a non-central node, **When** the service initializes, **Then** it attempts to connect to the configured list of central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev) in order, establishes connection to the first reachable central node, replicates the system register, and adds itself to the network's active peers list.

6. **Given** a peer service starting up and detecting it is running on the sorcha.dev domain (e.g., n0.sorcha.dev), **When** the service initializes, **Then** it recognizes itself as a central node, skips attempting outbound connections to other central nodes, adds itself to the active peers list, begins listening for incoming peer connections, and remains online to accept connections from other peers.

7. **Given** a peer node attempting to connect to central nodes, **When** all configured central nodes are unreachable, **Then** the peer service logs connection failures, continues retrying connections with exponential backoff, operates in isolated mode using its last known system register replica, and notifies administrators that it cannot reach central infrastructure.

---

### Edge Cases

- **What happens when a creator tries to create a register without proper permissions?**
  The workflow should validate permissions in the first action and fail fast with a clear authorization error before allocating any resources.

- **How does the system handle database storage allocation failures during register creation?**
  The workflow should detect storage allocation failures, log the error, and notify the user that register creation failed without creating a genesis record or control record entry.

- **What happens if the genesis record creation succeeds but the control record initialization fails?**
  The workflow should treat this as a critical failure, attempt to remove the orphaned genesis record, and rollback the transaction. If rollback fails, the system should log the inconsistency for manual resolution.

- **Can a register exist without any administrators after the initial creator is removed?**
  No - the system must prevent removal of the last administrator. At least one administrator must always exist to maintain register governance.

- **What happens when querying a register that has been corrupted or has an invalid genesis record?**
  The system should detect genesis record integrity violations, mark the register as compromised, prevent further transactions, and alert all administrators.

- **How does the system handle concurrent attempts to add the same user as an administrator?**
  The control record update should be idempotent - adding an existing administrator is a no-op that succeeds without creating duplicate entries.

- **What happens if the system register becomes corrupted or is deleted?**
  The system should detect missing or invalid system register on startup, log a critical error, and prevent register creation operations until an administrator manually restores or re-seeds the system register from a backup or trusted peer.

- **How does the system handle network partitions during system register replication?**
  Peer nodes should continue operating with their last known good replica of the system register. When connectivity is restored, the system should reconcile differences, detect conflicts (if blueprints were published to different partitions), and use timestamp-based conflict resolution to converge to a consistent state.

- **Can a user accidentally delete or modify the system register?**
  No - the system register should be protected with special system-level permissions that prevent user modification or deletion. Only system administrators through privileged operations can modify the system register.

- **What happens if two central nodes are initialized independently and create different system registers?**
  This represents a split-brain scenario. The system should detect multiple system registers with the same well-known identifier but different genesis records, alert administrators, and require manual intervention to choose the authoritative system register and re-seed the others.

- **How does the system handle replication failures when publishing blueprints to the system register?**
  Blueprint publication should succeed locally even if replication to peers fails. The system should queue failed replications for retry, log replication failures, and notify administrators if peers remain out of sync beyond a configured threshold (e.g., 1 hour).

- **What happens when a peer service starts and cannot reach any central nodes?**
  The peer service should continue retrying connections with exponential backoff, operate using its last known system register replica, log the isolation state, and notify administrators. It should not crash or become unavailable.

- **How does a central node know it's running on the sorcha.dev domain?**
  The peer service should check its configured hostname or domain during startup. If it matches the sorcha.dev domain (or is explicitly configured as a central node), it should behave as a central node rather than attempting outbound connections.

- **What happens if a central node tries to connect to other central nodes by mistake?**
  If a central node is misconfigured and attempts to connect to other central nodes, this could create connection loops or conflicts. The system should detect this scenario (e.g., mutual connection attempts), log a configuration error, and prevent the connection to avoid circular dependencies.

- **Can peer nodes connect directly to each other for system register replication, or only to central nodes?**
  In the initial implementation, peer nodes replicate system register only from central nodes, not from other peers. Peer-to-peer replication (without central nodes) is out of scope but could be added later for resilience.

- **What happens when a peer's active connection to a central node fails?**
  The peer should immediately attempt to connect to the next central node in the configured list (n1 if connected to n0, n2 if connected to n1, back to n0 if connected to n2). During the reconnection attempt, the peer operates in isolated mode using its last known system register replica.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create a unique, immutable genesis record when a new register is created, containing register metadata (name, purpose, creation timestamp, creator identity) and the initial control record.

- **FR-002**: System MUST allocate isolated database storage for each register to store dockets (transaction records), ensuring data segregation between registers.

- **FR-003**: System MUST initialize the control record in the genesis record with the register creator as the first administrator, granting full administrative permissions.

- **FR-004**: System MUST define register creation as a blueprint workflow with actions for: permission validation, storage allocation, genesis record creation, control record initialization, and completion confirmation.

- **FR-005**: System MUST cryptographically sign the genesis record to establish authenticity and prevent tampering, using the creator's wallet key.

- **FR-006**: System MUST allow existing administrators to add additional administrators to the register's control record through an administrative action.

- **FR-007**: System MUST record all changes to the control record (adding/removing administrators) as transactions in the register with timestamp, initiator identity, and action type.

- **FR-008**: System MUST validate that only existing administrators can modify the control record (add/remove administrators).

- **FR-009**: System MUST prevent removal of the last administrator from a register to ensure continuous governance.

- **FR-010**: System MUST provide a query interface to retrieve the current control record, showing all active administrators and their permissions.

- **FR-011**: System MUST rollback all changes if the register creation workflow fails at any step, including cleaning up allocated database storage.

- **FR-012**: System MUST assign a globally unique identifier to each register upon creation.

- **FR-013**: System MUST store the genesis record as the first immutable entry in the register's docket storage.

- **FR-014**: System MUST record the blueprint workflow execution history in the genesis record metadata for audit purposes.

- **FR-015**: System MUST validate register creation requests against user permissions before allocating any resources.

- **FR-016**: System MUST create a special system register with a well-known identifier during central node initialization to store published blueprints.

- **FR-017**: System MUST seed the register creation blueprint into the system register during bootstrap initialization.

- **FR-018**: System MUST replicate the system register and all published blueprints to peer nodes when they connect to the network.

- **FR-019**: System MUST allow authorized publishers to publish new blueprints to the system register, recording each publication as a transaction with timestamp and publisher identity.

- **FR-020**: System MUST protect the system register with system-level permissions that prevent user modification or deletion, allowing only privileged system administrators to modify it.

- **FR-021**: System MUST detect an existing system register on startup and skip creation to prevent duplicate system registers on the same node.

- **FR-022**: System MUST validate system register integrity on startup, checking for valid genesis record, control record, and cryptographic signatures.

- **FR-023**: System MUST queue failed blueprint replication attempts for retry and log replication failures for administrator review.

- **FR-024**: System MUST use timestamp-based conflict resolution to reconcile system register differences when network partitions heal.

- **FR-025**: System MUST allow peer nodes to query their local replica of the system register to retrieve published blueprints for execution.

- **FR-026**: Peer Service MUST attempt to connect to a configured list of central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev) in order during non-central node startup.

- **FR-027**: Peer Service MUST detect if it is running on the sorcha.dev domain during startup and, if so, skip attempting outbound connections to other central nodes.

- **FR-028**: Central nodes (nodes on sorcha.dev domain) MUST add themselves to the active peers list and begin listening for incoming peer connections without attempting to connect as clients.

- **FR-029**: Central nodes MUST remain online to accept incoming peer connections and MUST NOT shut down or go offline when other peers are attempting to connect.

- **FR-030**: Peer nodes MUST continue retrying connections to central nodes with exponential backoff when all central nodes are unreachable, operating in isolated mode using their last known system register replica.

- **FR-031**: Peer Service MUST detect and prevent connection loops when a central node mistakenly attempts to connect to another central node, logging a configuration error instead of establishing the connection.

- **FR-032**: Peer nodes MUST perform periodic synchronization of the system register with central nodes every 5 minutes to ensure eventual consistency.

- **FR-033**: Central nodes MUST send push notifications to all connected peer nodes immediately when a new blueprint is published to the system register, enabling faster propagation than periodic sync alone.

- **FR-034**: Peer nodes MUST accept and process push notifications from central nodes for immediate system register updates, falling back to periodic synchronization if push delivery fails.

- **FR-035**: Peer nodes MUST maintain a connection to only one central node at a time (the first reachable from the configured list), switching to the next central node in the list if the active connection fails.

- **FR-036**: Peer nodes MUST send heartbeat messages to their connected central node and detect connection failure if no heartbeat response is received within 30 seconds, triggering failover to the next central node.

- **FR-037**: Each peer node MUST maintain its own local active peers list to track its connection status, currently connected central node, and network awareness, without requiring global coordination or shared state with other peers.

### Key Entities

- **Register**: A named, isolated ledger for recording a specific type of transaction or data lineage. Contains a unique identifier, name, purpose, creation timestamp, and storage location for dockets. Associated with a control record that governs administrative access.

- **Genesis Record**: The immutable first record in a register that establishes the register's authority chain. Contains register metadata (name, purpose, creation timestamp, creator identity), the initial control record, cryptographic signature, and workflow execution history. Serves as the trust anchor for all subsequent transactions.

- **Control Record**: Governs administrative access to a register. Lists all current administrators with their identities, permissions (add administrators, remove administrators, modify control, record transactions), and timestamps of when they were granted access. Initially contains only the register creator.

- **Docket**: A transaction record stored in a register. Contains transaction data, timestamp, participant identities, and cryptographic proofs. Not part of the genesis record specification but represents the data that the register will store after creation.

- **Register Creation Workflow**: A blueprint workflow that orchestrates the register creation process. Contains actions for: permission validation, database storage allocation, genesis record generation, control record initialization, and completion confirmation. Ensures consistent and auditable register creation.

- **System Register**: A special register with a well-known identifier that stores published blueprints for distribution across the peer network. Created during central node initialization, seeded with the register creation blueprint, and replicated to all peer nodes. Protected with system-level permissions to prevent unauthorized modification. Serves as the authoritative source for blueprint distribution and discovery.

- **Administrator**: A user identity with permissions to manage a register's control record (add/remove administrators) and operational settings. At least one administrator must exist at all times.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can successfully create a new register and receive confirmation within 5 seconds under normal load (excluding network latency).

- **SC-002**: 100% of successfully created registers have a valid genesis record stored in the database with a properly initialized control record listing the creator as administrator.

- **SC-003**: Register creation workflows fail gracefully with clear error messages, and 100% of failed workflows rollback completely without orphaned database storage or incomplete records.

- **SC-004**: The system can support at least 100 concurrent register creation workflows without failures or resource conflicts.

- **SC-005**: All genesis records are cryptographically signed and verifiable, with 0% of registers having unsigned or invalid genesis records.

- **SC-006**: Administrators can query a register's control record and receive the complete list of current administrators in under 1 second.

- **SC-007**: 95% of users can successfully create their first register without requiring support or documentation beyond inline help.

- **SC-008**: All control record modifications (adding administrators) are recorded as auditable transactions with complete timestamp and initiator information.

- **SC-009**: The system register is successfully created and seeded with the register creation blueprint on 100% of central node initializations.

- **SC-010**: Peer nodes can replicate the complete system register including all published blueprints within 30 seconds of connecting to a central node under normal network conditions.

- **SC-011**: Blueprint publications to the system register propagate to at least 95% of connected peer nodes within 5 minutes.

- **SC-012**: System register integrity validation on startup completes in under 2 seconds and correctly detects 100% of corruption scenarios in testing.

- **SC-013**: Peer services running on central nodes (sorcha.dev domain) correctly identify themselves as central nodes and skip outbound connection attempts on 100% of startups.

- **SC-014**: Non-central peer nodes successfully connect to at least one central node from the configured list within 30 seconds during startup under normal network conditions.

- **SC-015**: Central nodes remain online and accept incoming peer connections, with 100% uptime during normal operations (excluding planned maintenance).

- **SC-016**: When a blueprint is published to the system register, at least 80% of connected peer nodes receive the push notification and update within 30 seconds, with remaining peers receiving the update during the next 5-minute periodic synchronization.

## Assumptions

- Register creation permission is managed by the existing Tenant Service and organization role-based access control (RBAC) system.
- Database storage for dockets will use the existing storage abstraction layer (MongoDB, PostgreSQL, or in-memory implementations).
- Cryptographic signing of genesis records uses the existing Sorcha.Cryptography library with the creator's wallet key.
- Blueprint workflows for register creation will use the existing Sorcha.Blueprint.Engine for execution.
- The Register Service will be responsible for orchestrating register creation and maintaining the control record.
- Register identifiers will be globally unique UUIDs (GUIDs) generated by the system.
- The genesis record and control record will be stored as special metadata in the register's storage, separate from regular dockets.
- Administrative permissions are binary (full admin rights) in the initial implementation; granular permissions can be added later.
- Control record changes create new versioned records in the register rather than modifying the genesis record, preserving immutability.
- The system register will have a well-known identifier (e.g., UUID "00000000-0000-0000-0000-000000000000" or similar reserved value) that all nodes recognize.
- Central nodes are designated through configuration (not auto-elected) in the initial implementation; leader election can be added later.
- Central nodes are hosted on the sorcha.dev domain with hostnames: n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev.
- Peer services detect if they are central nodes by checking their configured hostname against the sorcha.dev domain or through explicit configuration flags.
- Non-central peer nodes are configured with the list of central node addresses (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev) to attempt connections during startup.
- Peer nodes connect to central nodes in the order they appear in the configuration list, using the first reachable central node.
- Peer nodes maintain a single active connection to one central node at a time to minimize resource usage; if the active connection fails, the peer switches to the next central node in the list.
- Peer nodes use heartbeat messages to monitor connection health, with a 30-second timeout for heartbeat responses; missed heartbeats trigger failover to the next central node.
- Blueprint replication uses the existing Peer Service's P2P networking capabilities for peer-to-peer data transfer.
- The system register is replicated in its entirety to peer nodes; selective replication or partial sync is not supported in the initial implementation.
- Network partition resolution favors availability over consistency (AP in CAP theorem); eventual consistency is acceptable for blueprint distribution.
- Failed replication retries use exponential backoff with a maximum retry interval of 1 hour.
- Peer connection retries to central nodes use exponential backoff with initial delay of 1 second, multiplier of 2x, and maximum delay of 1 minute (sequence: 1s, 2s, 4s, 8s, 16s, 32s, 60s, 60s, ...).
- Peer nodes can operate in isolated mode with their last known system register replica when central nodes are unreachable, continuing to serve existing blueprints but unable to receive new blueprint publications.
- Peer nodes use a hybrid pull+push synchronization model: periodic 5-minute pulls for baseline consistency, plus immediate push notifications from central nodes for faster propagation.
- Push notifications are best-effort delivery; if a peer misses a push notification, it will receive the update during the next 5-minute periodic synchronization.
- Each peer node maintains its own local active peers list for tracking connection status and network state; this list is not shared or synchronized with other peers and does not require global coordination.

## Dependencies

- **Tenant Service**: Provides user identity and permission validation for register creators and administrators.
- **Wallet Service**: Provides cryptographic signing capabilities for genesis record authentication.
- **Blueprint Engine**: Executes the register creation workflow and manages action orchestration.
- **Storage Abstraction Layer**: Provides database allocation and storage interfaces for register dockets and metadata.
- **Register Service**: Implements register creation logic, control record management, and system register operations.
- **Peer Service**: Provides P2P networking capabilities for replicating the system register and published blueprints across peer nodes.

## Out of Scope

- Granular administrator permissions (read-only admin, delegate-only admin, etc.) - all administrators have full permissions in this version.
- Register deletion or archival workflows - only creation is covered.
- Cross-register control record sharing or federated administration.
- Automatic administrator succession or delegation policies.
- Register creation quotas or rate limiting per user/organization.
- Register templates or pre-configured register types beyond the standard creation workflow.
- Migration of existing registers to the new genesis record format (this is for new registers only).
- User interface for register creation - this specification covers the backend workflow and API contracts.
- Automatic leader election or consensus protocols for central node designation - central nodes are configured manually in this version.
- Selective or partial replication of the system register - the entire system register is replicated to all peers.
- Blueprint versioning or deprecation management in the system register - blueprints are published and replicated as-is.
- Conflict-free replicated data types (CRDTs) for system register replication - uses timestamp-based conflict resolution.
- Bandwidth optimization or delta synchronization for large system registers - full register replication only.
- Guaranteed delivery of push notifications - push notifications are best-effort; periodic synchronization ensures eventual consistency.
