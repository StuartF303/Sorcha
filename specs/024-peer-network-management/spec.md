# Feature Specification: Peer Network Management & Observability

**Feature Branch**: `024-peer-network-management`
**Created**: 2026-02-07
**Status**: Draft
**Input**: User description: "Peer Network Management & Observability - Add comprehensive peer network inspection, register subscription management, and peer reputation tracking across CLI, REST API, and Blazor UI."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Peer Network State (Priority: P1)

An operator opens the Administration page in the Sorcha UI and navigates to the Peer Service tab. They see a live dashboard showing all connected peers, their health status, latency, advertised registers, and sync progress. The dashboard auto-refreshes and allows drilling into individual peer details. The same information is available via `sorcha peer list` and `sorcha peer stats` in the CLI, and via REST endpoints for programmatic access.

**Why this priority**: Operators need visibility into the peer network to understand what nodes are connected, what data they hold, and whether the network is healthy. This is the foundational requirement — without network visibility, none of the other features (subscribe, reputation) can be used effectively.

**Independent Test**: Can be fully tested by starting a peer service, connecting to seed nodes, and verifying that the UI dashboard, CLI commands, and REST endpoints all return accurate peer network state including connected peers, their advertised registers, and sync statistics.

**Acceptance Scenarios**:

1. **Given** a running peer service with 3 connected peers, **When** the operator opens the Peer Service admin tab, **Then** they see a peer list showing all 3 peers with their ID, address, latency, health status, and advertised registers.
2. **Given** a peer that goes offline, **When** the dashboard auto-refreshes, **Then** the peer's status updates to show it is unreachable and its last-seen timestamp is displayed.
3. **Given** a running peer service, **When** the operator runs `sorcha peer list`, **Then** the CLI displays a table of all known peers with ID, address, status, latency, and number of advertised registers.
4. **Given** a running peer service, **When** the operator calls `GET /api/peers`, **Then** the response includes each peer's advertised registers and sync state alongside existing peer metadata.

---

### User Story 2 - Monitor Register Subscriptions & Replication Progress (Priority: P1)

An operator wants to see which registers this node is subscribed to, what replication mode each subscription uses (forward-only or full-replica), and how far along the sync is. In the UI, a "Register Subscriptions" panel shows each subscription with a progress bar for full-replica syncs and a status indicator (Subscribing, Syncing, FullyReplicated, Active, Error). The CLI provides `sorcha peer subscriptions` to list the same information. REST endpoints expose the data programmatically.

**Why this priority**: Register replication is the core function of the P2P network. Operators must monitor sync progress to know when a node is ready for validation or when replication has stalled.

**Independent Test**: Can be tested by subscribing to a register, monitoring the sync state transitions, and verifying that progress is accurately reflected in UI, CLI, and REST responses.

**Acceptance Scenarios**:

1. **Given** a node subscribed to 2 registers (one forward-only, one full-replica), **When** the operator views the subscriptions panel, **Then** each subscription shows its register ID, mode, sync state, and progress percentage.
2. **Given** a full-replica subscription that is 60% through docket chain pull, **When** the operator views the subscription, **Then** a progress bar shows approximately 60% and the current/total docket counts are displayed.
3. **Given** a subscription in Error state, **When** the operator views the subscription, **Then** the error message and consecutive failure count are displayed, and the status is visually distinguished (red indicator).
4. **Given** a running peer service, **When** the operator runs `sorcha peer subscriptions`, **Then** a table shows all subscriptions with register ID, mode, state, progress, and last sync time.

---

### User Story 3 - Subscribe to Registers from Other Peers (Priority: P2)

An operator wants to subscribe this node to a register that other peers advertise. In the UI, they see a list of registers advertised across the network (from peer advertisements). They select a register and choose a replication mode (forward-only or full-replica). The system creates a subscription and begins syncing. The CLI provides `sorcha peer subscribe --register-id <id> --mode <forward-only|full-replica>` for the same operation.

**Why this priority**: This enables operators to actively manage what data their node holds, which is essential for setting up new nodes and configuring replication topology. It depends on Story 1 (network visibility) to know what registers are available.

**Independent Test**: Can be tested by advertising a register on one peer, then using the UI or CLI on another peer to subscribe and verifying that sync begins.

**Acceptance Scenarios**:

1. **Given** peers in the network advertising 3 public registers, **When** the operator views the "Available Registers" panel, **Then** they see all 3 registers with their IDs, the number of peers holding each, and the latest version available.
2. **Given** an available register, **When** the operator clicks "Subscribe" and selects "Full Replica" mode, **Then** a new subscription is created and appears in the subscriptions panel with state "Subscribing", transitioning to "Syncing".
3. **Given** an available register, **When** the operator runs `sorcha peer subscribe --register-id reg-123 --mode forward-only`, **Then** the CLI confirms the subscription was created and displays its initial state.
4. **Given** the operator is already subscribed to a register, **When** they attempt to subscribe again, **Then** the system informs them of the existing subscription and does not create a duplicate.

---

### User Story 4 - Unsubscribe from a Register (Priority: P2)

An operator wants to stop replicating a register. They select a subscription in the UI and click "Unsubscribe", which stops sync and removes the subscription. The CLI provides `sorcha peer unsubscribe --register-id <id>`.

**Why this priority**: Paired with subscribe — operators need to manage their subscriptions in both directions.

**Independent Test**: Can be tested by subscribing, then unsubscribing and verifying sync stops and the subscription is removed from the list.

**Acceptance Scenarios**:

1. **Given** an active subscription to a register, **When** the operator clicks "Unsubscribe" and confirms, **Then** the subscription is removed, sync stops, and cached data is retained.
2. **Given** an unsubscribed register with retained cached data, **When** the operator clicks "Purge Cache", **Then** the cached transactions and dockets for that register are deleted.
3. **Given** an active subscription, **When** the operator runs `sorcha peer unsubscribe --register-id reg-123`, **Then** the CLI confirms removal and notes that cached data is retained (use `--purge` flag to also delete cached data).
4. **Given** no subscription to a register, **When** the operator attempts to unsubscribe, **Then** the system returns a clear "not found" message.

---

### User Story 5 - View Peer Reputation & Network Quality (Priority: P3)

An operator wants to understand the quality and reliability of peers in the network. The UI shows a peer quality breakdown (excellent, good, fair, poor) and per-peer reputation scores based on connection quality, latency, and failure history. This helps operators identify problematic peers and understand network health trends.

**Why this priority**: Reputation visibility is important for network health but is read-only and informational — it builds on the foundation of Stories 1-2 and doesn't block core functionality.

**Independent Test**: Can be tested by observing peer quality scores after a period of operation and verifying scores reflect actual connection quality metrics.

**Acceptance Scenarios**:

1. **Given** a network with peers of varying quality, **When** the operator views the peer reputation panel, **Then** each peer shows a quality score (0-100), quality category (Excellent/Good/Fair/Poor), and contributing metrics (latency, failure count, success rate).
2. **Given** a peer with high failure count, **When** the operator views its reputation, **Then** the score reflects the poor reliability with a "Poor" quality badge.
3. **Given** a running peer service, **When** the operator runs `sorcha peer quality`, **Then** a table shows all peers ranked by quality score with breakdown metrics.

---

### User Story 6 - Manage Peer Reputation (Priority: P3)

An operator wants to manually adjust peer trust. They can ban a peer (block all communication), or reset a peer's failure count to give it another chance. The UI provides action buttons on each peer row, and the CLI provides `sorcha peer ban --peer-id <id>` and `sorcha peer reset --peer-id <id>`.

**Why this priority**: Manual reputation management is an advanced operational capability. It requires all read-only features to be in place first and is primarily needed for troubleshooting edge cases.

**Independent Test**: Can be tested by banning a peer and verifying it is excluded from gossip and sync, then unbanning and verifying it resumes participation.

**Acceptance Scenarios**:

1. **Given** a connected peer, **When** the operator bans it via the UI, **Then** the peer is excluded from gossip, sync, and heartbeat, and its status shows "Banned".
2. **Given** a banned peer, **When** the operator unbans it, **Then** the peer is restored to normal participation.
3. **Given** a peer with 15 consecutive failures, **When** the operator resets its failure count, **Then** the failure count returns to 0 and the peer is eligible for normal communication.
4. **Given** a running service, **When** the operator runs `sorcha peer ban --peer-id peer-abc`, **Then** the CLI confirms the peer was banned.

---

### Edge Cases

- What happens when the peer service is offline or unreachable from the UI? The dashboard shows a clear "Service Unavailable" state with the last known data and a retry option.
- What happens when a subscribe request targets a register that no peer currently advertises? The system returns a "register not found in network" error rather than creating a subscription to nothing.
- What happens when a peer is banned but is the only source for a subscribed register? The system warns the operator that banning will interrupt replication and requires confirmation.
- What happens when the peer list is very large (hundreds of peers)? The UI uses pagination and the CLI limits default output to 50 peers with a `--limit` option.
- What happens when a subscription's source peers all go offline? The subscription transitions to Error state with an appropriate message and retries when peers come back.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose peer network state (peer list with health, latency, advertised registers) via REST API, CLI, and Blazor UI.
- **FR-002**: System MUST expose register subscription state (mode, sync state, progress, errors) via REST API, CLI, and Blazor UI.
- **FR-003**: System MUST allow operators to subscribe to a register with a chosen replication mode (forward-only or full-replica) via REST API, CLI, and Blazor UI.
- **FR-004**: System MUST allow operators to unsubscribe from a register via REST API, CLI, and Blazor UI. Unsubscribing stops sync but retains cached data; a separate purge action deletes cached data.
- **FR-005**: System MUST show public registers advertised across the peer network (aggregated from peer advertisements where `IsPublic = true`) so operators can discover what is available to subscribe to. Private register sharing via invitation protocol is out of scope (future feature).
- **FR-006**: System MUST display per-peer quality scores and quality categories based on connection metrics (latency, failure count, success rate).
- **FR-007**: System MUST allow operators to ban/unban a peer, preventing all communication with the banned peer. Bans MUST be persisted to the database and survive service restarts.
- **FR-008**: System MUST allow operators to reset a peer's failure count.
- **FR-009**: System MUST display register cache statistics (transaction count, docket count, latest versions) for each locally cached register.
- **FR-010**: System MUST provide auto-refreshing dashboard in the UI with configurable refresh interval.
- **FR-011**: System MUST paginate large peer lists and subscription lists in both UI and CLI.
- **FR-012**: System MUST show a clear "Service Unavailable" state when the peer service is unreachable from the UI, with last-known data preserved.
- **FR-013**: System MUST warn operators when banning a peer that is the sole source for an active subscription.
- **FR-014**: System MUST prevent duplicate register subscriptions (one subscription per register per node).
- **FR-015**: All management operations (subscribe, unsubscribe, ban, unban, reset) MUST require authentication.

### Key Entities

- **PeerNetworkSummary**: Aggregated view of the peer network — total peers, healthy count, average latency, quality distribution, active subscriptions count.
- **PeerDetail**: Extended peer information including ID, address, latency, health status, quality score, quality category, advertised registers, failure history, banned status.
- **RegisterSubscriptionView**: Subscription state including register ID, replication mode, sync state, progress percentage, docket/transaction versions, last sync time, error details.
- **AvailableRegister**: A register discovered through peer advertisements — register ID, number of peers holding it, latest known version, whether it is public.
- **PeerReputationScore**: Per-peer quality metrics — quality score (0-100), category (Excellent/Good/Fair/Poor), latency average, success rate, failure count.

## Clarifications

### Session 2026-02-07

- Q: When an operator unsubscribes from a register, what should happen to locally cached data? → A: Keep cached data but stop syncing; offer a separate "purge" action to delete cached data explicitly.
- Q: Should peer bans persist across service restarts? → A: Yes, persisted to database so bans survive restarts.
- Q: Should "Available Registers" show only public or also private registers? → A: Public registers only for now. Private register sharing via an invitation protocol is a future requirement (out of scope for this feature).

## Assumptions

- The existing Peer Service REST endpoints (`/api/peers`, `/api/peers/stats`, `/api/peers/health`, `/api/registers/subscriptions`, `/api/registers/cache`) provide a foundation; new endpoints extend rather than replace them.
- The existing `PeerServiceAdmin.razor` component in the UI will be enhanced rather than replaced, preserving its auto-refresh pattern and MudBlazor component usage.
- The existing CLI `peer` command group (`list`, `get`, `stats`, `health`) will be extended with new subcommands (`subscriptions`, `subscribe`, `unsubscribe`, `quality`, `ban`, `reset`).
- Authentication follows the existing JWT Bearer pattern used across all Sorcha services.
- The API Gateway YARP routes for the peer service already handle `/api/peer/{**catch-all}` routing.
- Peer banning is a local operation — this node stops communicating with the banned peer, but cannot enforce a network-wide ban.
- Quality scores are computed from existing `ConnectionQualityTracker` data; no new scoring algorithm is needed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can view the complete peer network state (peers, registers, subscriptions) within 3 seconds of opening the admin dashboard.
- **SC-002**: Operators can subscribe to a new register in under 30 seconds using either the UI or CLI.
- **SC-003**: Register subscription progress is visible and updates within 10 seconds of actual sync progress changes.
- **SC-004**: All peer management operations (ban, unban, reset, subscribe, unsubscribe) are available through all three interfaces (REST API, CLI, UI) with consistent behavior.
- **SC-005**: The peer dashboard supports displaying 200+ peers without degradation in responsiveness.
- **SC-006**: Operators can identify and act on problematic peers (high failure count, poor quality) within 1 minute of viewing the dashboard.
- **SC-007**: All management endpoints require authentication; unauthenticated requests are rejected.
