# Implementation Plan: Register Creation with Genesis Record and Peer Service Updates

**Branch**: `001-register-genesis` | **Date**: 2025-12-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-register-genesis/spec.md`
**Focus**: Peer service updates for central node connection, system register replication, and heartbeat monitoring

## Summary

This implementation plan focuses on enhancing the Peer Service to support central node discovery, system register replication, and connection management. The peer service will connect to central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev), detect if running as a central node, replicate the system register containing published blueprints, and maintain connection health through heartbeat monitoring. The system implements a hybrid pull+push synchronization model with 5-minute periodic syncs and immediate push notifications.

**Key Changes**:
1. Central node detection and connection logic
2. System register replication service
3. Heartbeat monitoring with 30-second timeout
4. Hybrid pull+push synchronization
5. Exponential backoff for connection retries
6. Local active peers list management

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: gRPC (Grpc.Net 2.71.0), .NET Aspire 13.0.0, Redis (Aspire 13.0.0)
**Storage**: MongoDB (system register storage), Redis (distributed caching), local in-memory peer list
**Testing**: xUnit, Testcontainers for integration tests, NBomber for performance tests
**Target Platform**: Linux containers (Docker), Azure Container Apps (production)
**Project Type**: Microservice - existing Sorcha.Peer.Service project
**Performance Goals**:
- 30s connection timeout to central nodes
- 5-minute periodic sync with central nodes
- 30s push notification delivery to 80% of connected peers
- 30s heartbeat timeout for connection failure detection
- <2s system register integrity validation

**Constraints**:
- Single active connection per peer node (no connection multiplexing)
- Exponential backoff: 1s initial, 2x multiplier, 1min max
- Central nodes must remain online (100% uptime during normal operations)
- Eventual consistency for system register replication (AP in CAP theorem)

**Scale/Scope**:
- Support 100+ peer nodes connecting to 3 central nodes
- System register size: initially <1MB, growing with blueprint publications
- Network partition tolerance with isolated mode operation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Microservices-First Architecture
- **Compliant**: Peer Service is independently deployable microservice
- **Compliant**: Uses .NET Aspire for orchestration
- **Compliant**: Minimal coupling - communicates via gRPC and REST

### ✅ Security First
- **Compliant**: Zero trust model - all peer connections authenticated
- **Compliant**: System register protected with system-level permissions (FR-020)
- **Compliant**: No secrets in source control
- **Action Required**: Implement TLS for central node connections
- **Action Required**: Validate system register integrity on startup (FR-022)

### ✅ API Documentation
- **Compliant**: Uses .NET 10 built-in OpenAPI (NOT Swagger)
- **Compliant**: Uses Scalar.AspNetCore for interactive docs
- **Action Required**: Add XML documentation for new public APIs
- **Action Required**: Document new gRPC services in OpenAPI

### ✅ Testing Requirements
- **Action Required**: Achieve >85% coverage for new code
- **Action Required**: Integration tests for central node connection logic
- **Action Required**: Integration tests for system register replication
- **Action Required**: Performance tests for 5-minute sync and push notifications

### ✅ Code Quality
- **Compliant**: C# coding conventions, async/await patterns
- **Compliant**: Dependency injection used throughout
- **Compliant**: .NET 10 target framework

### ✅ Observability by Default
- **Compliant**: OpenTelemetry integration via ServiceDefaults
- **Action Required**: Add structured logging for central node connections
- **Action Required**: Add metrics for heartbeat monitoring
- **Action Required**: Add traces for system register replication

### Gate Status: ✅ **PASSED** (with action items for Phase 1)

No constitutional violations. All requirements align with project standards.

---

## Constitution Check Re-evaluation (Post-Design)

*Re-evaluated after Phase 1 design and research decisions*

### ✅ Microservices-First Architecture
- **✅ Compliant**: Peer Service remains independently deployable
- **✅ Compliant**: New Replication directory maintains separation of concerns
- **✅ Compliant**: gRPC contracts enable loose coupling
- **Decision**: Hybrid configuration approach (explicit flag + hostname validation) maintains deployment flexibility

### ✅ Security First
- **✅ Compliant**: TLS for all gRPC connections (built-in with .NET 10 gRPC)
- **✅ Compliant**: System register integrity validation using cryptographic hashes
- **✅ Compliant**: Central node authentication via peer certificates
- **Decision**: MongoDB system register collection protected with role-based access control
- **Decision**: Polly circuit breaker prevents cascading failures

### ✅ API Documentation
- **✅ Compliant**: All proto files have comprehensive comments
- **✅ Compliant**: REST endpoints documented in contracts/README.md
- **Decision**: XML documentation will be added to all C# implementations
- **Decision**: Scalar UI will expose new REST monitoring endpoints

### ✅ Testing Requirements
- **✅ Compliant**: Test strategy documented in quickstart.md
- **✅ Compliant**: Integration tests planned for Testcontainers (MongoDB, 3 central nodes)
- **Decision**: NBomber performance tests for 5-minute sync and push notification delivery
- **Target**: >85% coverage for new Replication/ directory code

### ✅ Code Quality
- **✅ Compliant**: All design follows C# coding conventions
- **✅ Compliant**: async/await patterns in all gRPC services
- **✅ Compliant**: Dependency injection for all new services
- **Decision**: Polly ResiliencePipeline for exponential backoff (consistent with existing RedisCacheStore)

### ✅ Observability by Default
- **✅ Compliant**: Structured logging planned for all connection events
- **Decision**: Metrics: connection_status, heartbeat_latency, sync_duration, push_notification_delivery_rate
- **Decision**: Traces: full sync, incremental sync, push notification, heartbeat failover
- **Decision**: Logs: central node detection, connection failures, sync errors, heartbeat timeouts

### ✅ Blueprint Creation Standards
- **N/A**: This feature does not create blueprints (it replicates them)

### ✅ Domain-Driven Design
- **✅ Compliant**: Ubiquitous language maintained (Peer, Central Node, System Register, Blueprint)
- **Decision**: New aggregates: CentralNodeInfo, SyncCheckpoint (protect connection state invariants)

### Final Gate Status: ✅ **PASSED**

All design decisions comply with the constitution. Implementation is approved to proceed.

## Project Structure

### Documentation (this feature)

```text
specs/001-register-genesis/
├── spec.md                  # Feature specification
├── plan.md                  # This file (/speckit.plan command output)
├── research.md              # Phase 0 output - technology research
├── data-model.md            # Phase 1 output - data models
├── quickstart.md            # Phase 1 output - developer guide
├── contracts/               # Phase 1 output - API contracts
│   ├── peer-connection.proto # gRPC service for central node connections
│   ├── register-replication.proto # gRPC service for system register sync
│   └── heartbeat.proto      # gRPC service for connection health
├── checklists/
│   └── requirements.md      # Quality validation checklist
└── tasks.md                 # Phase 2 output (/speckit.tasks - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Existing Structure** (will be enhanced):

```text
src/Services/Sorcha.Peer.Service/
├── Communication/           # Existing: CircuitBreaker, CommunicationProtocolManager
├── Core/                    # Existing: PeerNode, PeerServiceConfiguration, PeerStatus
│   └── [NEW] CentralNodeConfiguration.cs
│   └── [NEW] SystemRegisterConfiguration.cs
├── Discovery/               # Existing: PeerDiscoveryService, PeerListManager
│   └── [NEW] CentralNodeDiscoveryService.cs
├── Distribution/            # Existing: GossipProtocolEngine, TransactionDistributionService
├── Monitoring/              # Existing: HealthMonitorService, ConnectionQualityTracker
│   └── [NEW] HeartbeatMonitorService.cs
├── Network/                 # Existing: NetworkAddressService, StunClient
├── Replication/             # [NEW DIRECTORY]
│   ├── SystemRegisterReplicationService.cs
│   ├── SystemRegisterCache.cs
│   ├── PushNotificationHandler.cs
│   └── PeriodicSyncService.cs
├── Protos/                  # Existing gRPC definitions
│   └── [NEW] CentralNodeConnection.proto
│   └── [NEW] SystemRegisterSync.proto
│   └── [NEW] Heartbeat.proto
├── PeerService.cs           # Existing: Background service - WILL BE MODIFIED
├── Program.cs               # Existing: Service registration - WILL BE MODIFIED
└── appsettings.json         # WILL BE MODIFIED with central node config

tests/Sorcha.Peer.Service.Tests/
├── Unit/
│   ├── CentralNodeDiscoveryServiceTests.cs
│   ├── HeartbeatMonitorServiceTests.cs
│   ├── SystemRegisterReplicationServiceTests.cs
│   └── PeriodicSyncServiceTests.cs
├── Integration/
│   ├── CentralNodeConnectionTests.cs
│   ├── SystemRegisterReplicationTests.cs
│   └── HeartbeatFailoverTests.cs
└── Performance/
    ├── PeriodicSyncPerformanceTests.cs
    └── PushNotificationPerformanceTests.cs

src/Services/Sorcha.Register.Service/
├── Repositories/            # Existing: MongoDB repository
│   └── [MODIFIED] IRegisterRepository.cs  # Add system register queries
│   └── [MODIFIED] MongoRegisterRepository.cs # Implement system register
└── Services/                # Existing: Register service logic
    └── [NEW] SystemRegisterService.cs # System register management
```

**Structure Decision**:
- Enhancing existing Sorcha.Peer.Service microservice with new Replication directory
- Adding gRPC contracts in Protos directory following existing patterns
- Register Service gets new SystemRegisterService for central node operations
- Maintaining separation of concerns: Discovery, Communication, Replication, Monitoring

## Complexity Tracking

> **This section is empty - no constitutional violations to justify**

All design decisions comply with the constitution. The implementation follows established patterns in the existing Peer Service.

---

## Phase 0: Outline & Research

**Objective**: Resolve all unknowns from Technical Context and document research decisions.

### Research Tasks

1. **Central Node Detection Mechanism**
   - Research: How to detect if peer service is running on sorcha.dev domain
   - Options: DNS lookup, hostname check, explicit configuration flag
   - Decision criteria: Reliability, performance, ease of deployment

2. **System Register Storage Strategy**
   - Research: MongoDB collection design for system register
   - Options: Single document vs. collection of blueprint documents
   - Decision criteria: Query performance, replication efficiency, size limits

3. **gRPC Streaming for System Register Sync**
   - Research: Bidirectional streaming vs. server streaming for replication
   - Options: Full sync on connect, incremental sync, streaming sync
   - Decision criteria: Network efficiency, resume capability, error handling

4. **Heartbeat Protocol Design**
   - Research: Existing heartbeat patterns in gRPC
   - Options: Dedicated heartbeat service, embedded in replication stream, HTTP health checks
   - Decision criteria: Latency, reliability, resource usage

5. **Exponential Backoff Implementation**
   - Research: .NET resilience libraries (Polly, Microsoft.Extensions.Resilience)
   - Options: Custom implementation vs. Polly policies
   - Decision criteria: Maintainability, configurability, testing

6. **Push Notification Delivery**
   - Research: gRPC server-push patterns for blueprint publications
   - Options: Long-lived streams, webhook callbacks, SignalR
   - Decision criteria: Scalability, reliability, NAT traversal

7. **Redis Integration for Active Peers List**
   - Research: Redis data structures for peer list (local vs. distributed)
   - Options: Local in-memory only, Redis sorted set, Redis hash
   - Decision criteria: Performance, consistency requirements, failover

**Output File**: `research.md`

---

## Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete

### Design Artifacts to Create

#### 1. Data Model (`data-model.md`)

Extract entities from feature spec and research decisions:

**Entities**:
- **CentralNodeInfo**: Hostname, IP address, port, last contact timestamp, connection status
- **SystemRegisterEntry**: Blueprint ID, blueprint document, publication timestamp, publisher identity
- **HeartbeatMessage**: Timestamp, peer ID, sequence number, central node ID
- **ActivePeerInfo**: Peer ID, connected central node, connection timestamp, last heartbeat
- **SyncCheckpoint**: Last sync timestamp, replica version, conflict resolution state

**Relationships**:
- Peer → CentralNodeInfo (many-to-one: single active connection)
- SystemRegister → SystemRegisterEntry (one-to-many: blueprint collection)
- Peer → ActivePeerInfo (one-to-one: local state)

**Validation Rules** (from requirements):
- Central node hostnames: MUST match n0/n1/n2.sorcha.dev pattern
- Heartbeat timeout: MUST be 30 seconds (FR-036)
- Periodic sync interval: MUST be 5 minutes (FR-032)
- Connection retry backoff: MUST follow 1s, 2s, 4s, 8s, 16s, 32s, 60s sequence (Assumptions)

**State Transitions**:
```
Peer Connection States:
Disconnected → Connecting → Connected → Heartbeat Monitoring
            ↓              ↓          ↓
            ← Retry (exponential backoff) ←
```

#### 2. API Contracts (`/contracts/`)

**Generate gRPC contracts from functional requirements**:

**File**: `contracts/CentralNodeConnection.proto`
- Service: CentralNodeConnectionService
- RPC: ConnectToCentralNode(PeerInfo) → ConnectionResponse
- RPC: DisconnectFromCentralNode(PeerInfo) → DisconnectionResponse
- RPC: GetCentralNodeStatus(Empty) → CentralNodeStatus

**File**: `contracts/SystemRegisterSync.proto`
- Service: SystemRegisterSyncService
- RPC: FullSync(SyncRequest) → stream SystemRegisterEntry (server streaming)
- RPC: IncrementalSync(SyncCheckpoint) → stream SystemRegisterEntry
- RPC: ReceivePushNotification(stream SystemRegisterEntry) → PushAcknowledgement (bidirectional)

**File**: `contracts/Heartbeat.proto`
- Service: HeartbeatService
- RPC: SendHeartbeat(HeartbeatMessage) → HeartbeatAcknowledgement
- RPC: MonitorHeartbeat(stream HeartbeatMessage) → stream HeartbeatAcknowledgement (bidirectional)

**OpenAPI Endpoints** (REST fallback):
- `GET /api/central-nodes` - List configured central nodes
- `GET /api/system-register` - Query local system register replica
- `GET /api/system-register/blueprints/{id}` - Get specific blueprint
- `GET /api/connection/status` - Get current central node connection status

#### 3. Developer Quickstart (`quickstart.md`)

**Content**:
- How to run peer service in central node mode
- How to run peer service in peer node mode
- How to test system register replication locally
- How to verify heartbeat monitoring
- Configuration examples for appsettings.json

#### 4. Agent Context Update

**Run script**: `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`

**New technology to add**:
- gRPC bidirectional streaming
- Exponential backoff with Polly (if chosen)
- System register MongoDB collection design
- Redis integration for distributed peer list (if chosen)

---

## Phase 1 Implementation Checklist

- [x] Research all unknowns and create `research.md` ✅
- [x] Document data model in `data-model.md` ✅
- [x] Generate gRPC contracts in `/contracts/` ✅
- [x] Create OpenAPI specifications for REST endpoints ✅ (included in contracts README)
- [x] Write `quickstart.md` with configuration examples ✅
- [x] Run agent context update script ✅
- [x] Re-evaluate Constitution Check with design decisions ✅ (see below)

---

## Phase 2: Task Generation

**NOT PART OF THIS COMMAND** - Will be handled by `/speckit.tasks`

---

## Next Steps

1. **Immediate**: Execute Phase 0 research tasks
2. **After Research**: Execute Phase 1 design and contract generation
3. **After Design**: Run `/speckit.tasks` to generate actionable implementation tasks
4. **After Tasks**: Begin implementation following task priority order

---

## Notes

### Key Design Considerations

1. **Central Node Detection**: Must reliably detect sorcha.dev domain at startup
2. **Connection Failover**: Implement exponential backoff correctly to avoid overwhelming central nodes
3. **System Register Integrity**: Validate on startup to detect corruption (FR-022)
4. **Push vs. Pull Balance**: 5-minute periodic sync ensures baseline consistency even if push notifications fail
5. **Isolated Mode**: Peer nodes must continue operating with stale replica when central nodes unreachable

### Risk Areas

1. **Network Partitions**: Peers may operate with stale blueprints during prolonged outages
2. **Split-Brain**: Multiple system registers could be created if central nodes initialized independently
3. **Heartbeat False Positives**: Network hiccups could trigger unnecessary failovers
4. **Connection Storms**: If all peers retry simultaneously after central node restart

### Mitigation Strategies

1. Manual system register reconciliation procedure for split-brain (out of scope for auto-resolution)
2. Jitter in connection retry backoff to prevent thundering herd
3. Grace period before failover (2 missed heartbeats = 60s total before switching)
4. Rate limiting on central node connection acceptance

---

## Summary of Generated Artifacts

### Phase 0: Research ✅
- **research.md** (800+ lines) - Complete technical research with 7 research tasks resolved
  - Central node detection: Hybrid configuration with hostname validation
  - System register storage: MongoDB collection per blueprint
  - gRPC streaming: Server streaming for sync + push notifications
  - Heartbeat protocol: Dedicated gRPC service with 30s timeout
  - Exponential backoff: Polly v8 ResiliencePipeline with jitter
  - Push notifications: Server streaming subscription pattern
  - Redis integration: Local in-memory only (per FR-037)

### Phase 1: Design & Contracts ✅
- **data-model.md** (900+ lines) - Complete data model documentation
  - 7 entities with full C# implementations
  - 3 enumerations for connection states
  - MongoDB schema for system register
  - State machines for connection lifecycle
  - Validation rules from requirements

- **contracts/** directory with 4 files (1,400+ lines total)
  - **CentralNodeConnection.proto** - Connection management gRPC service
  - **SystemRegisterSync.proto** - Replication gRPC service with hybrid pull+push
  - **Heartbeat.proto** - Health monitoring gRPC service
  - **README.md** - Comprehensive implementation guide with examples

- **quickstart.md** (800+ lines) - Developer guide
  - Configuration examples for central and peer nodes
  - Docker Compose setup for 3 central + 2 peer nodes
  - 5 testing scenarios with step-by-step instructions
  - Troubleshooting guide
  - API endpoint reference with curl examples
  - gRPC testing with grpcurl examples

### Agent Context Updates ✅
- **CLAUDE.md** updated with new technology stack entries
  - C# 13 / .NET 10
  - gRPC (Grpc.Net 2.71.0)
  - MongoDB (system register storage)
  - Redis (distributed caching)
  - Polly for resilience

### Constitution Check ✅
- **Initial evaluation**: PASSED with action items
- **Post-design re-evaluation**: PASSED - all design decisions compliant

---

**Generated**: 2025-12-13
**Tool**: `/speckit.plan`
**Status**: ✅ **Phase 0 and Phase 1 COMPLETE**

## Next Steps

1. ✅ **Phase 0 Complete**: All research tasks resolved
2. ✅ **Phase 1 Complete**: Design and contracts generated
3. **Ready for Phase 2**: Execute `/speckit.tasks` to generate implementation tasks
4. **Ready for Implementation**: All design artifacts ready for development

The implementation plan is **complete and approved**. All constitutional gates passed. The feature is ready for task breakdown and implementation.
