# Feature Specification: Register-to-Peer Advertisement Resync

**Feature Branch**: `030-peer-advertisement-resync`
**Created**: 2026-02-10
**Status**: Draft
**Input**: On startup, the Register Service informs the Peer Service about all local public registers (advertise=true). The Peer Service persists advertisements to Redis so they survive restarts. Additionally, a periodic background resync ensures advertisements stay consistent. This fixes the confirmed bug where 5 public registers exist in the Register Service but the Peer Service Available Registers tab shows 0 after a Docker restart.

## Problem Statement

When the system restarts (Docker restart, deployment, crash recovery), public registers that were previously advertised to the peer network become invisible. The Peer Service stores register advertisements only in memory. After a restart, this in-memory state is lost and there is no mechanism to rebuild it. Administrators see their registers in the Registers page but the Admin > Peer Network > Available Registers tab shows zero entries.

**Confirmed reproduction**: 5 registers with `advertise: true` exist in the Register Service database. The Peer Service `/api/registers/available` endpoint returns an empty array. Neither service logs any advertisement activity after restart.

The root causes are:
1. Register advertisements are stored in a volatile in-memory dictionary in the Peer Service
2. The Register Service only advertises once at register creation time (fire-and-forget)
3. No startup resync mechanism exists between the two services
4. No periodic reconciliation ensures advertisement state stays consistent

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Advertisements Survive Service Restart (Priority: P1)

As a platform administrator, after restarting Docker or deploying a new version, I expect all my public registers to remain visible in the Peer Network > Available Registers tab without any manual intervention.

**Why this priority**: This is the core bug fix. Without it, every restart breaks the peer network's register visibility, making the feature unusable in production.

**Independent Test**: Restart the Peer Service container. Verify that previously advertised registers reappear in the Available Registers tab within seconds of the service becoming healthy.

**Acceptance Scenarios**:

1. **Given** 5 public registers exist in the Register Service and were previously advertised, **When** the Peer Service restarts, **Then** all 5 registers appear in the Available Registers list within 30 seconds of the service becoming healthy.
2. **Given** a register was created with `advertise: false`, **When** the Peer Service restarts, **Then** that register does NOT appear in the Available Registers list.
3. **Given** a register's advertise flag was changed from true to false before the restart, **When** the Peer Service restarts, **Then** that register is NOT advertised.

---

### User Story 2 - Register Service Startup Re-Advertisement (Priority: P1)

As a platform administrator, when the Register Service starts (or restarts), it should proactively inform the Peer Service about all its public registers so the peer network is always aware of locally-hosted registers.

**Why this priority**: Equal to P1 because if only the Peer Service persists but the Register Service doesn't re-push on startup, newly created registers during a Peer Service outage would be permanently invisible.

**Independent Test**: Stop the Peer Service, create a register with `advertise: true` in the Register Service, then start the Peer Service. Within 60 seconds of the Register Service detecting the Peer Service is available, the new register should appear in Available Registers.

**Acceptance Scenarios**:

1. **Given** the Register Service starts with 3 public registers in its database, **When** the Peer Service is reachable, **Then** all 3 are advertised to the Peer Service within 60 seconds of startup.
2. **Given** the Peer Service is unreachable at Register Service startup, **When** the Peer Service later becomes available, **Then** the Register Service retries and successfully advertises all public registers.
3. **Given** a register was deleted from the Register Service while the Peer Service was down, **When** both services are running and resync occurs, **Then** the deleted register is no longer advertised.

---

### User Story 3 - Periodic Reconciliation (Priority: P2)

As a platform administrator, I expect the system to self-heal if advertisements drift out of sync due to transient failures, without requiring a manual restart.

**Why this priority**: Adds resilience beyond startup. Covers edge cases like network partitions or failed fire-and-forget calls that would otherwise require manual intervention.

**Independent Test**: Manually delete a register's advertisement from the persistence store. Within the reconciliation interval, verify the advertisement is restored.

**Acceptance Scenarios**:

1. **Given** a public register exists but its advertisement was lost (e.g., due to a transient failure), **When** the periodic reconciliation runs, **Then** the register is re-advertised within the reconciliation interval.
2. **Given** a register's advertise flag was changed to false, **When** the periodic reconciliation runs, **Then** the advertisement is removed.
3. **Given** all registers are correctly advertised, **When** the reconciliation runs, **Then** no unnecessary updates are made (idempotent operation).

---

### User Story 4 - Remote Peer Visibility (Priority: P3)

As an operator of a second Sorcha node connected to the peer network, I expect to see another node's public registers in my Available Registers tab after both nodes complete their startup sequence, enabling me to subscribe and replicate data.

**Why this priority**: This extends the single-node fix to the multi-node peer network scenario. Depends on stories 1-3 being complete.

**Independent Test**: With two nodes connected via the gossip protocol, create a public register on Node A. Verify it appears in Node B's Available Registers within the gossip exchange interval.

**Acceptance Scenarios**:

1. **Given** Node A has 2 public registers and Node B is connected via gossip, **When** Node B queries available registers, **Then** Node A's registers appear with peer count >= 1.
2. **Given** Node A restarts, **When** the gossip exchange completes after Node A's startup resync, **Then** Node B still sees Node A's registers without interruption.

---

### Edge Cases

- What happens when the Register Service starts before the Peer Service is ready? The Register Service should retry with backoff until the Peer Service is reachable.
- What happens when the persistence store (Redis) is temporarily unavailable? The Peer Service should fall back to in-memory storage and attempt to persist when Redis recovers.
- What happens when a register is deleted while both services are running? The delete operation should remove the advertisement immediately (existing behavior) and the persistence store entry.
- What happens when hundreds of registers need to be re-advertised on startup? The bulk re-advertisement should complete within a reasonable time and not overwhelm the Peer Service.
- What happens during a rolling deployment where services restart at different times? Each service should independently converge to the correct state regardless of startup order.

## Clarifications

### Session 2026-02-10

- Q: Should Redis persist only local advertisements or also remote peer advertisements? → A: Single unified pool backed by Redis with a 5-minute TTL. On startup, both local and remote advertisements are loaded from Redis but must be reverified against ground truth (local via Register Service DB, remote via gossip exchange).
- Q: Which service drives the periodic reconciliation process? → A: The Register Service drives reconciliation (push model) — it periodically queries its own database and pushes the full state to the Peer Service via the bulk endpoint.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Peer Service MUST persist all register advertisements (both local and remote) to a single unified Redis-backed store with a 5-minute TTL, so they survive service restarts and naturally expire if not refreshed.
- **FR-002**: On startup, the Peer Service MUST load previously persisted advertisements from Redis and make them immediately available via the `/api/registers/available` endpoint. All loaded advertisements MUST be reverified — local advertisements against the Register Service ground truth, remote advertisements against the next gossip exchange.
- **FR-003**: On startup, the Register Service MUST query its database for all registers with `advertise: true` and push them to the Peer Service.
- **FR-004**: The Register Service startup re-advertisement MUST retry with exponential backoff if the Peer Service is unreachable.
- **FR-005**: The Peer Service MUST provide a bulk advertisement endpoint that accepts multiple register advertisements in a single call, to support efficient startup resync.
- **FR-006**: The Register Service MUST run a periodic background process that reconciles the Peer Service's advertisement state with its own ground truth at a configurable interval (default: 5 minutes), using the bulk endpoint in full-sync mode.
- **FR-007**: When a register's `advertise` flag changes (true to false or vice versa), the change MUST be reflected in both the in-memory state and the persistent store.
- **FR-008**: When a register is deleted, its advertisement MUST be removed from both in-memory state and the persistent store.
- **FR-009**: The reconciliation process MUST be idempotent — running it multiple times with no state change should produce no side effects.
- **FR-010**: The Peer Service MUST continue to function (serve requests, run gossip) even if the persistence store is temporarily unavailable, falling back to in-memory state.
- **FR-011**: The Peer Service bulk advertisement endpoint MUST support a "full sync" mode where the provided list is treated as the complete set of local public registers, removing any local advertisements not in the list. Remote peer advertisements are unaffected by full-sync operations.

### Key Entities

- **RegisterAdvertisement**: A record of a register being advertised to the peer network. Contains register ID, source (local or remote peer ID), sync state, public flag, latest version, latest docket version, and timestamp. Stored in a single unified Redis pool with a 5-minute TTL and mirrored in the Peer Service's in-memory cache.
- **AdvertisementSyncState**: Tracks the last successful reconciliation timestamp and status between the Register Service and Peer Service. Used to detect drift and trigger re-synchronization.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a full system restart (all services stopped and restarted), all public registers are visible in the Available Registers tab within 60 seconds of all services becoming healthy.
- **SC-002**: After the Peer Service alone restarts, previously advertised registers are available via the API within 5 seconds (loaded from persistent store).
- **SC-003**: If the Peer Service is temporarily unavailable during register creation, the new register appears in Available Registers within the reconciliation interval (5 minutes) after the Peer Service recovers.
- **SC-004**: The startup re-advertisement of 100 registers completes within 10 seconds without service degradation.
- **SC-005**: The periodic reconciliation process adds no more than 100ms of latency to normal Peer Service operations.
- **SC-006**: The system converges to the correct advertisement state regardless of service startup order within 2 minutes.

## Assumptions

- Redis is available to both the Register Service and Peer Service as it is already configured in the Docker environment.
- The existing fire-and-forget advertisement on register creation will be retained as the primary path; the resync mechanisms serve as the safety net.
- The gossip protocol's existing 5-minute exchange interval is acceptable for propagating advertisements to remote peers after local resync completes.
- The existing `/api/registers/{id}/advertise` endpoint on the Peer Service will be extended (not replaced) to support the new bulk and full-sync capabilities.
- The 5-minute TTL on Redis advertisement entries aligns with the reconciliation interval and gossip exchange interval, ensuring entries that are not actively refreshed naturally expire.
- The Register Service drives all reconciliation (push model); the Peer Service does not initiate calls to the Register Service.
