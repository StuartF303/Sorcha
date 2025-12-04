# Tasks: Peer Service

**Feature Branch**: `peer-service`
**Created**: 2025-12-03
**Status**: Design Complete - Awaiting Approval (0% Implementation)

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 8 |
| In Progress | 4 |
| Pending | 18 |
| **Total** | **30** |

---

## Phase 1: Foundation (Sprint 1-2)

### PEER-001: Project Setup
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Create Sorcha.Peer.Service project structure.

**Acceptance Criteria**:
- [x] Project created with .NET 10
- [x] Service defaults reference
- [x] Aspire integration
- [x] Test project created

---

### PEER-002: Define gRPC Proto Files
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-001

**Description**: Define gRPC protocol buffer definitions.

**Acceptance Criteria**:
- [x] peer_discovery.proto
- [x] peer_communication.proto
- [x] transaction_distribution.proto
- [x] Proto compilation configured

---

### PEER-003: PeerNode Model
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-001

**Description**: Create PeerNode entity model.

**Acceptance Criteria**:
- [x] PeerNode.cs with all properties
- [x] PeerCapabilities class
- [x] PeerStatus enum
- [x] Serialization support

---

### PEER-004: Configuration System
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-001

**Description**: Implement configuration model.

**Acceptance Criteria**:
- [x] PeerServiceConfiguration class
- [x] appsettings.json schema
- [x] Options pattern binding
- [x] Configuration validation

---

### PEER-005: Address Discovery Service
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-001

**Description**: Implement network address discovery.

**Acceptance Criteria**:
- [x] IAddressDiscoveryService interface
- [ ] Internal IP detection
- [ ] STUN client integration
- [ ] HTTP lookup fallback
- [ ] Address validation

---

### PEER-006: Unit Tests - Foundation
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-003, PEER-004

**Description**: Unit tests for foundation components.

**Acceptance Criteria**:
- [x] PeerNode tests
- [x] Configuration tests
- [ ] Address discovery tests

---

## Phase 2: Peer Discovery (Sprint 3-4)

### PEER-007: Bootstrap Node Provider
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-004

**Description**: Manage bootstrap node connections.

**Acceptance Criteria**:
- [x] BootstrapNodeProvider class
- [x] Configuration-driven nodes
- [x] Connection management

---

### PEER-008: Peer Discovery Service
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-007

**Description**: Implement peer discovery logic.

**Acceptance Criteria**:
- [x] IPeerDiscoveryService interface
- [ ] Bootstrap connection
- [ ] Recursive discovery
- [ ] Periodic refresh
- [ ] Health monitoring

---

### PEER-009: Peer List Manager
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-003

**Description**: Manage peer list CRUD and persistence.

**Acceptance Criteria**:
- [x] PeerListManager class
- [ ] SQLite persistence
- [ ] Query methods
- [ ] Deduplication

---

### PEER-010: Unit Tests - Discovery
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-008

**Description**: Unit tests for peer discovery.

**Acceptance Criteria**:
- [ ] Bootstrap connection tests
- [ ] Discovery algorithm tests
- [ ] Refresh logic tests
- [ ] Health monitoring tests

---

### PEER-011: Integration Tests - Discovery
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-008

**Description**: Integration tests with multiple peers.

**Acceptance Criteria**:
- [ ] Docker Compose setup
- [ ] Multi-peer scenario tests
- [ ] Failure recovery tests

---

## Phase 3: Communication Layer (Sprint 5-6)

### PEER-012: Communication Manager
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-002

**Description**: Protocol negotiation and connection management.

**Acceptance Criteria**:
- [ ] ICommunicationManager interface
- [ ] Protocol negotiation logic
- [ ] Connection pooling
- [ ] Circuit breaker pattern

---

### PEER-013: gRPC Stream Client
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-002

**Description**: Implement gRPC streaming client.

**Acceptance Criteria**:
- [ ] GrpcStreamClient class
- [ ] Bidirectional streaming
- [ ] Connection keep-alive
- [ ] Error handling

---

### PEER-014: gRPC Client
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: PEER-002

**Description**: Implement gRPC on-demand client.

**Acceptance Criteria**:
- [ ] GrpcClient class
- [ ] Request/response pattern
- [ ] Retry logic

---

### PEER-015: REST Client
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: PEER-012

**Description**: Implement REST fallback client.

**Acceptance Criteria**:
- [ ] RestClient class
- [ ] HTTP client configuration
- [ ] Error mapping

---

### PEER-016: Unit Tests - Communication
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-012

**Description**: Unit tests for communication layer.

**Acceptance Criteria**:
- [ ] Protocol negotiation tests
- [ ] Connection pool tests
- [ ] Fallback tests

---

## Phase 4: Transaction Distribution (Sprint 7-8)

### PEER-017: Transaction Distributor
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-012

**Description**: Main transaction distribution service.

**Acceptance Criteria**:
- [ ] ITransactionDistributor interface
- [ ] Distribution orchestration
- [ ] Event integration

---

### PEER-018: Gossip Protocol
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 10 hours
- **Assignee**: TBD
- **Dependencies**: PEER-017

**Description**: Implement gossip protocol.

**Acceptance Criteria**:
- [ ] GossipProtocol class
- [ ] Fanout selection
- [ ] Round tracking
- [ ] Notification handling

---

### PEER-019: Bloom Filter
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Implement bloom filter for deduplication.

**Acceptance Criteria**:
- [x] BloomFilter class
- [x] Configurable size
- [x] Reset mechanism
- [x] Unit tests

---

### PEER-020: Transaction Queue
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-017

**Description**: Offline transaction queue.

**Acceptance Criteria**:
- [ ] TransactionQueue class
- [ ] SQLite persistence
- [ ] Retry logic
- [ ] Flush mechanism

---

### PEER-021: Unit Tests - Distribution
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-018

**Description**: Unit tests for distribution.

**Acceptance Criteria**:
- [ ] Gossip algorithm tests
- [ ] Deduplication tests
- [ ] Queue tests

---

### PEER-022: Integration Tests - Distribution
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 10 hours
- **Assignee**: TBD
- **Dependencies**: PEER-018

**Description**: Integration tests for gossip.

**Acceptance Criteria**:
- [ ] Multi-peer propagation tests
- [ ] Network partition tests
- [ ] Performance benchmarks

---

## Phase 5: Admin UI (Sprint 9)

### PEER-023: Peer Status Dashboard
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-008

**Description**: Create admin dashboard.

**Acceptance Criteria**:
- [ ] Blazor components
- [ ] Real-time peer status
- [ ] Network health metrics
- [ ] Event log viewer

---

### PEER-024: Metrics Collector
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: PEER-001

**Description**: Collect operational metrics.

**Acceptance Criteria**:
- [x] PeerMetricsCollector class
- [x] OpenTelemetry integration
- [x] Key metrics defined

---

## Phase 6: Hardening (Sprint 10)

### PEER-025: Security - mTLS
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-012

**Description**: Implement mutual TLS.

**Acceptance Criteria**:
- [ ] Certificate configuration
- [ ] Peer authentication
- [ ] Revocation support

---

### PEER-026: Rate Limiting
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: PEER-012

**Description**: Implement rate limiting.

**Acceptance Criteria**:
- [ ] Per-peer limits
- [ ] Bandwidth throttling
- [ ] Automatic blocking

---

### PEER-027: Performance Optimization
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: PEER-022

**Description**: Optimize performance.

**Acceptance Criteria**:
- [ ] Latency benchmarks
- [ ] Memory optimization
- [ ] Connection pooling tuning

---

### PEER-028: Load Testing
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: PEER-027

**Description**: Load testing with NBomber.

**Acceptance Criteria**:
- [ ] Transaction throughput tests
- [ ] Peer scalability tests
- [ ] Resource usage profiles

---

### PEER-029: Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Complete service documentation.

**Acceptance Criteria**:
- [x] Design document (docs/peer-service-design.md)
- [x] Configuration guide
- [x] Implementation roadmap

---

### PEER-030: Service Registration
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: TBD
- **Dependencies**: PEER-008

**Description**: Register with .NET Aspire.

**Acceptance Criteria**:
- [ ] AppHost registration
- [ ] Service discovery
- [ ] Health checks

---

## Notes

- Design document complete at `docs/peer-service-design.md`
- gRPC proto files defined but not fully implemented
- Bloom filter implementation complete with unit tests
- Major work remaining: communication layer, gossip protocol, and admin UI
- Target completion: Sprint 10 (post-MVD)
