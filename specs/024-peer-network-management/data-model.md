# Data Model: Peer Network Management & Observability

**Feature**: 024-peer-network-management
**Date**: 2026-02-07

## Entity Changes

### PeerNode (Modified)

Existing entity with 3 new fields for ban support.

| Field | Type | New? | Description |
|-------|------|------|-------------|
| PeerId | string | - | Unique peer identifier (primary key) |
| Address | string | - | IP address or hostname |
| Port | int | - | gRPC port |
| SupportedProtocols | List\<string\> | - | Protocol list (CSV in DB) |
| FirstSeen | DateTimeOffset | - | When peer was first discovered |
| LastSeen | DateTimeOffset | - | Last successful contact |
| FailureCount | int | - | Consecutive failures (auto-incremented) |
| IsSeedNode | bool | - | Whether this is a bootstrap peer |
| AdvertisedRegisters | List\<PeerRegisterInfo\> | - | Registers this peer advertises (in-memory only) |
| AverageLatencyMs | int | - | Running average latency |
| **IsBanned** | **bool** | **YES** | Whether peer is banned from communication |
| **BannedAt** | **DateTimeOffset?** | **YES** | When the ban was applied (null if not banned) |
| **BanReason** | **string?** | **YES** | Operator-provided reason for the ban |

**Persistence**: `PeerDbContext` entity updated. EF Core migration adds 3 columns to `peer.Peers` table.

**Ban Behavior**:
- Banned peers are excluded from `GetHealthyPeers()` results
- Banned peers are included in `GetAllPeers()` with `IsBanned = true`
- Ban status checked in heartbeat, gossip, and sync flows — banned peers skipped
- Seed nodes CAN be banned (operator override; warning logged)

### RegisterSubscription (Read-Only — Already Exists)

No changes needed. Already has all required fields per the spec.

| Field | Type | Description |
|-------|------|-------------|
| RegisterId | string | Register being replicated |
| Mode | ReplicationMode | ForwardOnly or FullReplica |
| SyncState | RegisterSyncState | Subscribing/Syncing/FullyReplicated/Active/Error |
| LastSyncedDocketVersion | long | Last docket version synced |
| LastSyncedTransactionVersion | long | Last transaction version synced |
| TotalDocketsInChain | long | Total dockets known in chain |
| SyncProgressPercent | double | Computed: (LastSyncedDocketVersion / TotalDocketsInChain) * 100 |
| CanParticipateInValidation | bool | Computed: SyncState == FullyReplicated |
| IsReceiving | bool | Currently receiving transactions |
| LastSyncAt | DateTimeOffset? | Last successful sync time |
| ConsecutiveFailures | int | Failure count for this subscription |
| ErrorMessage | string? | Last error description |

### ConnectionQuality (Read-Only — Already Exists)

No changes needed. Already computed by `ConnectionQualityTracker`.

| Field | Type | Description |
|-------|------|-------------|
| PeerId | string | Peer this quality applies to |
| AverageLatencyMs | long | Mean latency in ms |
| MinLatencyMs | long | Best observed latency |
| MaxLatencyMs | long | Worst observed latency |
| SuccessRate | double | 0.0 - 1.0 success ratio |
| TotalRequests | int | Total connection attempts |
| SuccessfulRequests | int | Successful attempts |
| FailedRequests | int | Failed attempts |
| QualityScore | double | 0-100 computed score |
| QualityRating | string | Excellent/Good/Fair/Poor/Very Poor |
| LastUpdated | DateTimeOffset | Last quality recalculation |

## New Response DTOs

### AvailableRegisterInfo (New)

Aggregated from peer advertisements across the network.

| Field | Type | Description |
|-------|------|-------------|
| RegisterId | string | Register identifier |
| PeerCount | int | Number of peers holding this register |
| LatestVersion | long | Highest transaction version seen |
| LatestDocketVersion | long | Highest docket version seen |
| IsPublic | bool | Whether register is publicly advertised |
| FullReplicaPeerCount | int | Peers with FullyReplicated state |

### BanRequest (New)

Request body for banning a peer.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Reason | string | No | Operator-provided ban reason (max 500 chars) |

### SubscribeRequest (New)

Request body for subscribing to a register.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Mode | string | Yes | "forward-only" or "full-replica" |

## State Transitions

### Peer Ban Lifecycle

```
Active ──[BanPeerAsync()]──► Banned
  ▲                            │
  └───[UnbanPeerAsync()]───────┘
```

- `Active → Banned`: Sets IsBanned=true, BannedAt=now, BanReason=reason. Peer excluded from all communication.
- `Banned → Active`: Sets IsBanned=false, BannedAt=null, BanReason=null. FailureCount preserved (not reset).

### Register Subscription Lifecycle (Existing — No Changes)

```
[Subscribe] → Subscribing → Syncing → FullyReplicated (full-replica)
[Subscribe] → Subscribing → Active (forward-only)
Any State → Error (on failure)
[Unsubscribe] → Removed (cache retained)
[Purge] → Cache deleted
```
