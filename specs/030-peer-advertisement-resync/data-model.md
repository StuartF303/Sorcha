# Data Model: Register-to-Peer Advertisement Resync

**Branch**: `030-peer-advertisement-resync` | **Date**: 2026-02-10

## Entities

### LocalRegisterAdvertisement (existing — modified)

Represents a register advertisement from the co-located Register Service.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| RegisterId | string | yes | Primary key. GUID format (32 chars, no hyphens). |
| SyncState | RegisterSyncState | yes | Enum: Subscribing, Syncing, FullyReplicated, Active, Error |
| LatestVersion | long | no | Latest transaction version. Default 0. |
| LatestDocketVersion | long | no | Latest docket version. Default 0. |
| IsPublic | bool | yes | Whether the register is publicly advertised. |
| LastUpdated | DateTimeOffset | yes | Timestamp of last update. Auto-set on write. |

**Changes**: No field changes. Storage moves from in-memory-only to Redis-backed with in-memory cache.

### RemoteRegisterAdvertisement (new)

Represents a register advertisement received from a remote peer via gossip.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| RegisterId | string | yes | Composite key part 1. |
| SourcePeerId | string | yes | Composite key part 2. Peer ID that advertised this register. |
| SyncState | RegisterSyncState | yes | Sync state as reported by the remote peer. |
| LatestVersion | long | no | Latest transaction version on the remote peer. |
| LatestDocketVersion | long | no | Latest docket version on the remote peer. |
| IsPublic | bool | yes | Always true (only public ads are gossiped). |
| CanServeFullReplica | bool | no | Computed: SyncState == FullyReplicated. |
| LastUpdated | DateTimeOffset | yes | Timestamp of last gossip update. |

### AdvertisementSyncState (new)

Tracks the last successful reconciliation between Register Service and Peer Service.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| LastSyncTimestamp | DateTimeOffset | yes | When the last successful reconciliation completed. |
| LastSyncStatus | string | yes | "Success", "Failed", "InProgress". |
| RegisterCount | int | no | Number of registers in last successful sync. |
| ErrorMessage | string | no | Error details if last sync failed. |

### BulkAdvertiseRequest (new — DTO)

Request body for the bulk advertisement endpoint.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Advertisements | List\<AdvertisementItem\> | yes | List of register advertisements. |
| FullSync | bool | no | Default false. If true, removes local ads not in the list. |

### AdvertisementItem (new — DTO)

Single advertisement entry within a bulk request.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| RegisterId | string | yes | Register identifier. |
| IsPublic | bool | yes | Whether to advertise publicly. |
| LatestVersion | long | no | Current transaction version. Default 0. |
| LatestDocketVersion | long | no | Current docket version. Default 0. |

### BulkAdvertiseResponse (new — DTO)

Response from the bulk advertisement endpoint.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Processed | int | yes | Number of advertisements processed. |
| Added | int | yes | Number of new advertisements added. |
| Updated | int | yes | Number of existing advertisements updated. |
| Removed | int | yes | Number of advertisements removed (fullSync mode only). |

## Redis Key Structure

```
peer:advert:local:{registerId}     → JSON(LocalRegisterAdvertisement)   TTL: 300s
peer:advert:remote:{peerId}:{registerId} → JSON(RemoteRegisterAdvertisement) TTL: 300s
peer:sync:state                     → JSON(AdvertisementSyncState)       TTL: none
```

**Key patterns for bulk operations**:
- `peer:advert:local:*` — all local advertisements (SCAN)
- `peer:advert:remote:*` — all remote advertisements (SCAN)
- `peer:advert:remote:{peerId}:*` — all advertisements from a specific peer (SCAN)

## State Transitions

### Advertisement Lifecycle

```
[Not Advertised] ---(Register created with advertise=true)---> [Persisted in Redis + Memory]
[Persisted]      ---(TTL expires, not refreshed)-----------> [Expired/Removed]
[Persisted]      ---(Reconciliation refresh)---------------> [Persisted, TTL reset]
[Persisted]      ---(Advertise flag set to false)----------> [Removed from Redis + Memory]
[Persisted]      ---(Register deleted)---------------------> [Removed from Redis + Memory]
[Expired]        ---(Reconciliation runs)-------------------> [Re-persisted, TTL reset]
```

### Startup Sequence

```
1. Peer Service starts
2. Load all peer:advert:* keys from Redis → populate in-memory cache
3. Serve /api/registers/available immediately (from loaded state)
4. Mark loaded ads as "unverified" (pending reverification)
5. Register Service starts (or is already running)
6. Register Service queries MongoDB for all advertise=true registers
7. Register Service calls POST /api/registers/bulk-advertise?fullSync=true
8. Peer Service updates Redis + memory, removes stale local ads
9. Gossip exchanges update remote ads (TTL refreshed)
10. Unverified remote ads expire via TTL if not refreshed by gossip
```
