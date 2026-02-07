# API Contracts: Peer Network Management & Observability

**Feature**: 024-peer-network-management
**Date**: 2026-02-07

## New Endpoints

All new endpoints are added to `Program.cs` in Sorcha.Peer.Service.

### Peer Management

#### GET /api/peers/quality
Get connection quality metrics for all tracked peers.

**Auth**: None (read-only monitoring)
**Tags**: Monitoring

**Response 200**:
```json
[
  {
    "peerId": "node_abc123",
    "averageLatencyMs": 45,
    "minLatencyMs": 12,
    "maxLatencyMs": 230,
    "successRate": 0.97,
    "totalRequests": 1500,
    "successfulRequests": 1455,
    "failedRequests": 45,
    "qualityScore": 87.5,
    "qualityRating": "Good",
    "lastUpdated": "2026-02-07T10:30:00Z"
  }
]
```

---

#### POST /api/peers/{peerId}/ban
Ban a peer, preventing all communication.

**Auth**: Required (JWT Bearer)
**Tags**: Management

**Request Body** (optional):
```json
{
  "reason": "Consistently serving corrupt data"
}
```

**Response 200**:
```json
{
  "peerId": "node_abc123",
  "isBanned": true,
  "bannedAt": "2026-02-07T10:30:00Z",
  "banReason": "Consistently serving corrupt data"
}
```

**Response 404**: Peer not found
**Response 409**: Peer is already banned

---

#### DELETE /api/peers/{peerId}/ban
Unban a peer, restoring normal communication.

**Auth**: Required (JWT Bearer)
**Tags**: Management

**Response 200**:
```json
{
  "peerId": "node_abc123",
  "isBanned": false
}
```

**Response 404**: Peer not found
**Response 409**: Peer is not currently banned

---

#### POST /api/peers/{peerId}/reset
Reset a peer's failure count to zero.

**Auth**: Required (JWT Bearer)
**Tags**: Management

**Response 200**:
```json
{
  "peerId": "node_abc123",
  "failureCount": 0,
  "previousFailureCount": 12
}
```

**Response 404**: Peer not found

---

### Register Subscription Management

#### GET /api/registers/available
List registers advertised across the peer network (public only).

**Auth**: None (read-only discovery)
**Tags**: Registers

**Response 200**:
```json
[
  {
    "registerId": "reg-abc-123",
    "peerCount": 5,
    "latestVersion": 1042,
    "latestDocketVersion": 87,
    "isPublic": true,
    "fullReplicaPeerCount": 3
  }
]
```

---

#### POST /api/registers/{registerId}/subscribe
Subscribe to a register for replication.

**Auth**: Required (JWT Bearer)
**Tags**: Registers

**Request Body**:
```json
{
  "mode": "full-replica"
}
```

Valid modes: `"forward-only"`, `"full-replica"`

**Response 201**:
```json
{
  "registerId": "reg-abc-123",
  "mode": "FullReplica",
  "syncState": "Subscribing",
  "lastSyncedDocketVersion": 0,
  "lastSyncedTransactionVersion": 0,
  "syncProgressPercent": 0.0
}
```

**Response 400**: Invalid mode value
**Response 409**: Already subscribed to this register
**Response 404**: Register not found in network advertisements

---

#### DELETE /api/registers/{registerId}/subscribe
Unsubscribe from a register. Cached data is retained unless `?purge=true`.

**Auth**: Required (JWT Bearer)
**Tags**: Registers

**Query Parameters**:
- `purge` (bool, optional, default: false): Also delete cached data

**Response 200**:
```json
{
  "registerId": "reg-abc-123",
  "unsubscribed": true,
  "cacheRetained": true
}
```

**Response 404**: No subscription found for this register

---

#### DELETE /api/registers/{registerId}/cache
Purge cached data for a register (standalone purge action).

**Auth**: Required (JWT Bearer)
**Tags**: Registers

**Response 200**:
```json
{
  "registerId": "reg-abc-123",
  "purged": true,
  "transactionsRemoved": 1042,
  "docketsRemoved": 87
}
```

**Response 404**: No cached data for this register

---

## Modified Endpoints

### GET /api/peers (Enhanced)

Add `advertisedRegisters`, `isBanned`, `qualityScore` to each peer in the response.

**Response 200** (enhanced fields marked with *):
```json
[
  {
    "peerId": "node_abc123",
    "address": "192.168.1.10",
    "port": 5000,
    "supportedProtocols": ["grpc"],
    "firstSeen": "2026-02-01T08:00:00Z",
    "lastSeen": "2026-02-07T10:29:55Z",
    "failureCount": 0,
    "isSeedNode": false,
    "averageLatencyMs": 45,
    "isBanned": false,
    "qualityScore": 87.5,
    "qualityRating": "Good",
    "advertisedRegisterCount": 3,
    "advertisedRegisters": [
      {
        "registerId": "reg-abc-123",
        "syncState": "FullyReplicated",
        "latestVersion": 1042,
        "isPublic": true
      }
    ]
  }
]
```

### GET /api/peers/{peerId} (Enhanced)

Same enhancements as list endpoint, plus full quality details for the single peer.

---

## YARP Gateway Addition

Add to `appsettings.json` ReverseProxy.Routes:

```json
"registers-base-route": {
  "ClusterId": "peer-cluster",
  "Match": { "Path": "/api/registers/{**catch-all}" },
  "Transforms": [{ "PathPattern": "/api/registers/{**catch-all}" }]
}
```
