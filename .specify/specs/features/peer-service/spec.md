# Feature Specification: Peer Service

**Feature Branch**: `peer-service`
**Created**: 2025-12-03
**Status**: Design Complete - Awaiting Approval (0% Implementation)
**Input**: Derived from `docs/peer-service-design.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Peer Discovery (Priority: P0)

As a Sorcha node operator, I need my node to automatically discover and connect to other peers in the network so that I can participate in the distributed ledger.

**Why this priority**: Core functionality - without peer discovery, nodes cannot communicate.

**Independent Test**: Can be tested by starting a node, connecting to bootstrap, and verifying peer list population.

**Acceptance Scenarios**:

1. **Given** a configured bootstrap node URL, **When** the peer service starts, **Then** it connects to the bootstrap node within 10 seconds.
2. **Given** a successful bootstrap connection, **When** peer list is requested, **Then** at least 50 peers are discovered within 2 minutes.
3. **Given** peers in the list, **When** the refresh interval elapses (15 min), **Then** the peer list is updated and unhealthy peers are removed.
4. **Given** all bootstrap nodes are unavailable, **When** the service starts, **Then** it enters offline mode and retries after 5 minutes.

---

### User Story 2 - Transaction Distribution (Priority: P0)

As a blockchain participant, I need transactions to be distributed across the peer network so that all nodes have consistent data.

**Why this priority**: Essential for blockchain operation - transactions must propagate.

**Independent Test**: Can be tested by submitting a transaction and verifying it reaches 90% of peers.

**Acceptance Scenarios**:

1. **Given** a new transaction, **When** I submit it for distribution, **Then** it is sent to sqrt(total_peers) peers via gossip protocol.
2. **Given** a transaction notification, **When** a peer doesn't have the transaction, **Then** it requests the full transaction.
3. **Given** duplicate transaction notification, **When** checked against bloom filter, **Then** it is recognized as known and not re-requested.
4. **Given** a transaction >1MB, **When** distributed, **Then** streaming is used instead of single message.

---

### User Story 3 - Protocol Negotiation (Priority: P1)

As a node operator, I need the peer service to negotiate the best communication protocol so that it works across different network environments.

**Why this priority**: Enables flexibility in deployments with different firewall configurations.

**Independent Test**: Can be tested by simulating protocol failures and verifying fallback.

**Acceptance Scenarios**:

1. **Given** a peer supporting gRPC streaming, **When** connecting, **Then** gRPC streaming is used.
2. **Given** gRPC streaming fails, **When** fallback is triggered, **Then** gRPC on-demand is attempted.
3. **Given** all gRPC fails, **When** final fallback, **Then** REST API is used.
4. **Given** a peer is unreachable via all protocols, **Then** it is marked as unreachable.

---

### User Story 4 - Network Address Discovery (Priority: P1)

As a node operator, I need my node to automatically discover its external address so that other peers can connect to it.

**Why this priority**: Required for bi-directional peer communication.

**Independent Test**: Can be tested by querying STUN server and verifying address is discovered.

**Acceptance Scenarios**:

1. **Given** the node is behind NAT, **When** address discovery runs, **Then** external address is discovered via STUN.
2. **Given** STUN servers are unavailable, **When** fallback runs, **Then** HTTP lookup services are used.
3. **Given** an explicit ExternalAddress is configured, **When** discovery runs, **Then** the configured address is used.
4. **Given** the external address changes, **When** detected, **Then** the peer re-registers with bootstrap.

---

### User Story 5 - Offline Mode (Priority: P1)

As a Participant node operator, I need transactions to be queued when the network is unavailable so that no transactions are lost.

**Why this priority**: Ensures reliability in intermittent connectivity scenarios.

**Independent Test**: Can be tested by disconnecting network, queuing transactions, and verifying flush on reconnect.

**Acceptance Scenarios**:

1. **Given** the service is offline, **When** a transaction is submitted, **Then** it is queued to disk.
2. **Given** queued transactions exist, **When** the service comes online, **Then** all queued transactions are flushed.
3. **Given** a transaction fails to flush, **When** retry limit is reached, **Then** it is logged and moved to dead-letter.
4. **Given** the service restarts, **When** starting, **Then** persisted queue is loaded and processed.

---

### User Story 6 - Health Monitoring (Priority: P2)

As a network administrator, I need to monitor the health of the peer network so that I can identify and resolve issues.

**Why this priority**: Operational requirement for maintaining network health.

**Independent Test**: Can be tested by viewing admin dashboard and verifying metrics are displayed.

**Acceptance Scenarios**:

1. **Given** the admin dashboard is accessed, **When** loading peer status, **Then** active peers, total peers, and network status are displayed.
2. **Given** a peer stops responding, **When** health check runs, **Then** the peer's failure count is incremented.
3. **Given** a peer exceeds failure threshold (3), **When** next health check, **Then** the peer is removed from the list.
4. **Given** active peers drop below MinHealthyPeers (5), **Then** bootstrap re-discovery is triggered.

---

### Edge Cases

- What happens when the network partitions?
- How does the system handle malicious peers flooding notifications?
- What happens when bloom filter false positive rate exceeds threshold?
- How does the system handle concurrent transaction distributions for the same transaction?

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST connect to bootstrap nodes on startup
- **FR-002**: System MUST discover peers recursively from bootstrap
- **FR-003**: System MUST refresh peer list every 15 minutes (configurable)
- **FR-004**: System MUST support gRPC streaming, gRPC on-demand, and REST protocols
- **FR-005**: System MUST negotiate protocol with automatic fallback
- **FR-006**: System MUST distribute transactions via gossip protocol
- **FR-007**: System MUST detect duplicate transactions via bloom filter
- **FR-008**: System MUST stream large transactions (>1MB)
- **FR-009**: System MUST discover external address via STUN or HTTP lookup
- **FR-010**: System MUST queue transactions when offline
- **FR-011**: System MUST persist transaction queue to disk
- **FR-012**: System MUST flush queue when coming online
- **FR-013**: System MUST monitor peer health with configurable thresholds
- **FR-014**: System MUST provide admin dashboard with peer metrics

### Key Entities

- **PeerNode**: Peer representation with ID, address, capabilities, and health status
- **TransactionNotification**: Gossip message with transaction hash and metadata
- **TransactionQueue**: Offline transaction queue with persistence
- **PeerCapabilities**: Protocol and feature support information

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Bootstrap connection succeeds within 10 seconds
- **SC-002**: Peer list builds to 50+ peers within 2 minutes
- **SC-003**: Transaction distributes to 90% of network within 1 minute
- **SC-004**: Transaction distribution latency under 500ms (P95)
- **SC-005**: Bandwidth usage under 10KB per transaction notification
- **SC-006**: Memory usage under 500MB for 1000 peers
- **SC-007**: Service uptime exceeds 99.9%
- **SC-008**: Unit test coverage exceeds 80%
