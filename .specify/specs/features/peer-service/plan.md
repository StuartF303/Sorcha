# Implementation Plan: Peer Service

**Feature Branch**: `peer-service`
**Created**: 2025-12-03
**Status**: Design Complete - Awaiting Approval (0% Implementation)
**Implementation Estimate**: 20 weeks (10 sprints)
**Resource Requirement**: 2-3 developers

## Summary

The Peer Service is the distributed networking component of the Sorcha platform, enabling peer-to-peer communication and transaction distribution across a decentralized network. It implements gossip protocol for efficient transaction propagation and supports multiple communication protocols with automatic fallback.

## Design Decisions

### Decision 1: Gossip Protocol for Distribution

**Approach**: Implement gossip protocol with configurable fanout factor (sqrt(total_peers)).

**Rationale**:
- O(log N) message complexity for efficient scaling
- Redundancy for reliability without flooding
- No single point of failure
- Proven pattern in distributed systems

**Alternatives Considered**:
- Flooding - Too bandwidth intensive
- Structured overlay (DHT) - More complex, slower propagation

### Decision 2: Protocol Negotiation

**Approach**: Hierarchical protocol fallback: gRPC Stream -> gRPC -> REST.

**Rationale**:
- gRPC streaming optimal for real-time, persistent connections
- gRPC on-demand for intermittent communication
- REST as universal fallback for restricted networks

**Alternatives Considered**:
- Single protocol - Limits deployment flexibility
- WebSocket only - Less feature-rich than gRPC

### Decision 3: STUN for NAT Traversal

**Approach**: Use STUN servers for external address discovery with HTTP fallback.

**Rationale**:
- Industry standard for NAT traversal
- Multiple free STUN servers available
- HTTP fallback ensures discovery in restricted environments

### Decision 4: Bloom Filter for Deduplication

**Approach**: Bloom filter (10M bits, 3 hash functions) for O(1) duplicate detection.

**Rationale**:
- Space efficient (~1.2MB)
- Constant time lookups
- Acceptable false positive rate (<0.1%)
- Reset interval prevents saturation

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Sorcha.Peer.Service                      │
│                 (BackgroundService)                      │
├─────────────────────────────────────────────────────────┤
│  Core/                                                   │
│  ├── PeerService.cs            (Main orchestrator)      │
│  ├── PeerNode.cs               (Peer representation)    │
│  └── PeerServiceConfiguration.cs                        │
├─────────────────────────────────────────────────────────┤
│  Discovery/                                              │
│  ├── IPeerDiscoveryService.cs                           │
│  ├── PeerDiscoveryService.cs   (Bootstrap, refresh)     │
│  ├── BootstrapNodeProvider.cs                           │
│  └── PeerListManager.cs        (CRUD, persistence)      │
├─────────────────────────────────────────────────────────┤
│  Communication/                                          │
│  ├── ICommunicationManager.cs                           │
│  ├── CommunicationManager.cs   (Protocol negotiation)   │
│  ├── GrpcStreamClient.cs                                │
│  ├── GrpcClient.cs                                      │
│  └── RestClient.cs                                      │
├─────────────────────────────────────────────────────────┤
│  Distribution/                                           │
│  ├── ITransactionDistributor.cs                         │
│  ├── TransactionDistributor.cs                          │
│  ├── GossipProtocol.cs                                  │
│  └── TransactionQueue.cs       (Offline queue)          │
├─────────────────────────────────────────────────────────┤
│  Network/                                                │
│  ├── IAddressDiscoveryService.cs                        │
│  ├── AddressDiscoveryService.cs                         │
│  ├── NatTraversalService.cs    (STUN)                   │
│  └── ExternalAddressLookup.cs  (HTTP fallback)          │
├─────────────────────────────────────────────────────────┤
│  Monitoring/                                             │
│  ├── PeerMetricsCollector.cs                            │
│  └── PeerEventLogger.cs                                 │
├─────────────────────────────────────────────────────────┤
│  Protos/                                                 │
│  ├── peer_discovery.proto                               │
│  ├── peer_communication.proto                           │
│  └── transaction_distribution.proto                     │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Design Documents | 100% | Comprehensive design complete |
| Configuration | 100% | appsettings schema complete |
| Proto definitions | 100% | gRPC protos defined |
| PeerNode model | 100% | Entity model complete |
| **Implementation** | **0%** | **Awaiting approval** |

### Implementation Roadmap (10 Sprints)

| Sprint | Focus | Deliverables |
|--------|-------|--------------|
| 1-2 | Foundation & Address Discovery | Project setup, STUN client |
| 3-4 | Peer Discovery | Bootstrap, recursive discovery |
| 5-6 | Communication Layer | gRPC streaming, protocol fallback |
| 7-8 | Transaction Distribution | Gossip protocol, offline mode |
| 9 | Admin UI & Monitoring | Dashboard, metrics |
| 10 | Hardening & Documentation | Security, performance testing |

### Performance Targets

| Metric | Target |
|--------|--------|
| Transaction distribution | <500ms to 90% of network |
| Peer discovery | <30s for 100 peers |
| Bandwidth per transaction | <10 KB notification |
| Memory usage | <500 MB for 1000 peers |

### gRPC Service Definitions

| Service | Method | Description | Status |
|---------|--------|-------------|--------|
| PeerDiscovery | GetPeerList | Request peer list | Proto defined |
| PeerDiscovery | RegisterPeer | Register with bootstrap | Proto defined |
| PeerDiscovery | Ping | Health check | Proto defined |
| TransactionDistribution | NotifyTransaction | Gossip notification | Proto defined |
| TransactionDistribution | GetTransaction | Request full transaction | Proto defined |
| TransactionDistribution | StreamTransaction | Stream large transaction | Proto defined |

### REST API Fallback

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/v1/peers/register` | Register peer | Pending |
| GET | `/api/v1/peers/list` | Get peer list | Pending |
| GET | `/api/v1/peers/ping` | Health check | Pending |
| POST | `/api/v1/transactions/notify` | Notify transaction | Pending |
| GET | `/api/v1/transactions/{hash}` | Get transaction | Pending |

## Dependencies

### Internal Dependencies

- `Sorcha.ServiceDefaults` - .NET Aspire configuration
- `Sorcha.Register.Service` - Transaction storage
- `Sorcha.Cryptography` - Transaction hashing

### External Dependencies

- `Grpc.Net.Client` - gRPC client
- `Grpc.AspNetCore` - gRPC server
- `STUN.NET` or similar - STUN client
- `Microsoft.Data.Sqlite` - Queue persistence

### Service Dependencies

- Register Service - Transaction persistence
- Validator Service - Consensus coordination

## Migration/Integration Notes

### SQLite Peer List Schema

```sql
CREATE TABLE peers (
  peer_id TEXT PRIMARY KEY,
  address TEXT NOT NULL,
  port INTEGER NOT NULL,
  supported_protocols TEXT,  -- JSON array
  first_seen INTEGER NOT NULL,
  last_seen INTEGER NOT NULL,
  failure_count INTEGER DEFAULT 0,
  capabilities TEXT,  -- JSON object
  is_bootstrap BOOLEAN DEFAULT 0
);

CREATE INDEX idx_last_seen ON peers(last_seen);
CREATE INDEX idx_is_bootstrap ON peers(is_bootstrap);
```

### Transaction Queue Schema

```sql
CREATE TABLE transaction_queue (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  tx_hash TEXT NOT NULL,
  tx_data BLOB NOT NULL,
  queued_at INTEGER NOT NULL,
  retry_count INTEGER DEFAULT 0,
  last_attempt INTEGER
);

CREATE INDEX idx_queued_at ON transaction_queue(queued_at);
```

### Breaking Changes

- None for MVD phase (new service)

## Open Questions

1. Should we implement TURN for restrictive NAT scenarios?
2. How to handle peer reputation/trust scoring?
3. Should we support IPv6-only networks?
4. How to coordinate with consensus during network partitions?
