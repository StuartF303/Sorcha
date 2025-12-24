# Tasks: Peer Service Hub Node Connection and System Register Replication

**Input**: Design documents from `/specs/001-register-genesis/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Feature Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Ready for Implementation

**Primary Focus**: User Story 4 - System Register for Blueprint Publication and Replication (Priority: P1)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US4 = User Story 4)
- Include exact file paths in descriptions

## Path Conventions

Repository structure:
- `src/Services/Sorcha.Peer.Service/` - Peer service microservice
- `src/Services/Sorcha.Register.Service/` - Register service (system register management)
- `tests/Sorcha.Peer.Service.Tests/` - Peer service tests

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project structure setup and gRPC proto compilation

- [X] T001 [P] Copy gRPC proto files from specs/001-register-genesis/contracts/ to src/Services/Sorcha.Peer.Service/Protos/
- [X] T002 [P] Add Protobuf compilation entries to src/Services/Sorcha.Peer.Service/Sorcha.Peer.Service.csproj for CentralNodeConnection.proto, SystemRegisterSync.proto, Heartbeat.proto (renamed PeerInfo to CentralNodePeerInfo and PeerCapabilities to CentralNodePeerCapabilities to avoid conflicts)
- [X] T003 [P] Build project to verify proto compilation generates C# client/server stubs in obj/Debug/net10.0/Protos/
- [X] T004 [P] Create tests/Sorcha.Peer.Service.Tests/Unit/ directory for unit tests
- [X] T005 [P] Create tests/Sorcha.Peer.Service.Tests/Integration/ directory for integration tests
- [X] T006 [P] Create tests/Sorcha.Peer.Service.Tests/Performance/ directory for performance tests

**Checkpoint**: Proto files compiled successfully, test directories created

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story implementation can begin

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Configuration and Constants

- [X] T007 [P] Create src/Services/Sorcha.Peer.Service/Core/CentralNodeConfiguration.cs with IsCentralNode, ExpectedHostnamePattern, ValidateHostname properties
- [X] T008 [P] Create src/Services/Sorcha.Peer.Service/Core/SystemRegisterConfiguration.cs with PeriodicSyncIntervalMinutes, HeartbeatIntervalSeconds, HeartbeatTimeoutSeconds, MaxRetryAttempts
- [X] T009 [P] Create src/Services/Sorcha.Peer.Service/Core/PeerServiceConstants.cs with SystemRegisterId (Guid.Empty), CentralNodeHostnames array, HeartbeatIntervalSeconds (30), PeriodicSyncIntervalMinutes (5), RetryBackoffParameters

### Core Entities and Enumerations

- [X] T010 [P] Create src/Services/Sorcha.Peer.Service/Core/CentralNodeConnectionStatus.cs enum with Disconnected, Connecting, Connected, Failed, HeartbeatTimeout values
- [X] T011 [P] Create src/Services/Sorcha.Peer.Service/Core/PeerConnectionStatus.cs enum with Disconnected, Connecting, Connected, HeartbeatTimeout, Isolated values
- [X] T012 [P] Create src/Services/Sorcha.Peer.Service/Core/NotificationType.cs enum with BlueprintPublished, BlueprintUpdated, BlueprintDeprecated values
- [X] T013 [P] Create src/Services/Sorcha.Peer.Service/Core/CentralNodeInfo.cs with NodeId, Hostname, Port, Priority, ConnectionStatus, LastConnectionAttempt, LastSuccessfulConnection, LastHeartbeatSent, LastHeartbeatAcknowledged, ConsecutiveFailures, IsActive properties and ResetConnectionState(), RecordFailure() methods
- [X] T014 [P] Create src/Services/Sorcha.Peer.Service/Core/SystemRegisterEntry.cs with BlueprintId, RegisterId, Document (BsonDocument), PublishedAt, PublishedBy, Version, IsActive, PublicationTransactionId, Checksum, Metadata properties and ValidateSystemRegister() method
- [X] T015 [P] Create src/Services/Sorcha.Peer.Service/Core/HeartbeatMessage.cs with PeerId, Timestamp, SequenceNumber, LastSyncVersion, ActiveConnections, CpuUsagePercent, MemoryUsageMb, NodeType properties and Create() factory method
- [X] T016 [P] Create src/Services/Sorcha.Peer.Service/Core/ActivePeerInfo.cs with PeerId, ConnectedCentralNodeId, ConnectionEstablished, LastHeartbeat, LastSyncVersion, Status, HeartbeatSequence, MissedHeartbeats properties and RecordHeartbeat(), RecordMissedHeartbeat(), IsHeartbeatTimedOut() methods
- [X] T017 [P] Create src/Services/Sorcha.Peer.Service/Core/SyncCheckpoint.cs with PeerId, CurrentVersion, LastSyncTime, TotalBlueprints, CentralNodeId, NextSyncDue properties and UpdateAfterSync(), IsSyncDue() methods
- [X] T018 [P] Create src/Services/Sorcha.Peer.Service/Core/BlueprintNotification.cs with BlueprintId, Version, PublishedAt, PublishedBy, BlueprintSummary, Type properties

### Validation Utilities

- [X] T019 [P] Create src/Services/Sorcha.Peer.Service/Core/CentralNodeValidator.cs with IsValidCentralNodeHostname(), ValidateHostname() methods using regex pattern ^n[0-2]\.sorcha\.dev$
- [X] T020 [P] Create src/Services/Sorcha.Peer.Service/Core/HeartbeatValidator.cs with IsHeartbeatTimedOut(), ShouldFailover() methods with 30s timeout, 2 missed heartbeats threshold
- [X] T021 [P] Create src/Services/Sorcha.Peer.Service/Core/SyncValidator.cs with CalculateNextSyncTime(), IsSyncDue() methods with 5-minute interval
- [X] T022 [P] Create src/Services/Sorcha.Peer.Service/Core/RetryBackoffValidator.cs with CalculateBackoff() method implementing exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s sequence
- [X] T023 [P] Create src/Services/Sorcha.Peer.Service/Core/SystemRegisterValidator.cs with IsSystemRegister(), ValidateSystemRegisterId() methods checking for Guid.Empty (00000000-0000-0000-0000-000000000000)

### Polly Resilience Pipeline

- [X] T024 Create src/Services/Sorcha.Peer.Service/Resilience/ConnectionResiliencePipeline.cs with ResiliencePipeline configured for exponential backoff (1s initial, 2x multiplier, 60s max), 10 max retry attempts, 30s timeout per attempt, jitter enabled to prevent thundering herd

### MongoDB System Register Repository

- [X] T025 [P] Create src/Services/Sorcha.Register.Service/Repositories/ISystemRegisterRepository.cs interface with GetAllBlueprintsAsync(), GetBlueprintByIdAsync(), GetBlueprintsSinceVersionAsync(), PublishBlueprintAsync(), GetLatestVersionAsync() methods
- [X] T026 Create src/Services/Sorcha.Register.Service/Repositories/MongoSystemRegisterRepository.cs implementing ISystemRegisterRepository using MongoDB.Driver with collection "sorcha_system_register_blueprints", indexes on Version (ascending), PublishedAt (descending), IsActive, auto-increment version number
- [X] T027 Create tests/Sorcha.Register.Service.Tests/Unit/MongoSystemRegisterRepositoryTests.cs with tests for CRUD operations, version auto-increment, index creation, query patterns (all, by ID, since version)

### Extend Existing PeerListManager

- [X] T028 Update existing src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs to add UpdateLocalPeerStatus() method accepting ConnectedCentralNodeId and PeerConnectionStatus, add GetLocalPeerStatus() method returning ActivePeerInfo
- [X] T029 Create tests/Sorcha.Peer.Service.Tests/Unit/PeerListManagerTests.cs with tests for UpdateLocalPeerStatus(), GetLocalPeerStatus(), status transitions

**Checkpoint**: Foundation ready - all core entities, enums, validators, resilience pipeline, MongoDB repository, and peer list manager extensions complete

---

## Phase 3: User Story 4 - System Register for Blueprint Publication and Replication (Priority: P1) 🎯 MVP

**Goal**: Enable hub nodes to initialize with system register, peer nodes to connect and replicate system register, and maintain connection health through heartbeat monitoring with failover support.

**Independent Test**: Initialize a hub node (verify system register created), start a peer node (verify connection to hub node established, system register replicated), publish a blueprint (verify push notification delivered), kill hub node connection (verify peer fails over to next hub node), kill all hub nodes (verify peer enters isolated mode).

### Tests for User Story 4 (>85% coverage target per constitution) ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

#### Unit Tests (Written First - TDD)

- [ ] T030 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/CentralNodeDiscoveryServiceTests.cs testing DetectIfCentralNode() with hostname "n0.sorcha.dev" returns true, hostname "peer-1.local" returns false, IsCentralNode=true with hostname validation enabled throws if hostname mismatch
- [ ] T031 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/CentralNodeConnectionManagerTests.cs testing ConnectToCentralNodeAsync() retry sequence (1s, 2s, 4s...), failover to next node on all retries exhausted, exponential backoff with jitter
- [ ] T032 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/HeartbeatMonitorServiceTests.cs testing SendHeartbeat() every 30s, RecordMissedHeartbeat() increments counter, ShouldFailover() triggers at 2 missed (60s), heartbeat acknowledgement resets missed count
- [ ] T033 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/SystemRegisterReplicationServiceTests.cs testing FullSync() streams all blueprints, IncrementalSync() streams only version > lastKnownVersion, checkpoint update after successful sync
- [ ] T034 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/PeriodicSyncServiceTests.cs testing sync triggered every 5 minutes, IsSyncDue() calculation, NextSyncDue timestamp update
- [ ] T035 [P] [US4] Create tests/Sorcha.Peer.Service.Tests/Unit/PushNotificationHandlerTests.cs testing RegisterSubscriber() adds to subscriber list, NotifyBlueprintPublished() streams to all subscribers, UnregisterSubscriber() on stream failure, best-effort delivery (logs failure, continues to other peers)

#### Integration Tests (System-Level Validation)

- [ ] T036 [US4] Create tests/Sorcha.Peer.Service.Tests/Integration/CentralNodeInitializationTests.cs testing hub node startup creates system register with well-known ID 00000000-0000-0000-0000-000000000000, seeds register-creation-v1 blueprint, marks node as ready to accept peer connections, validates system register integrity on restart (skips re-creation)
- [ ] T037 [US4] Create tests/Sorcha.Peer.Service.Tests/Integration/PeerNodeConnectionTests.cs testing peer connects to n0 in order, full sync on first connection, session ID assignment, ConnectionResponse includes current system register version
- [ ] T038 [US4] Create tests/Sorcha.Peer.Service.Tests/Integration/HeartbeatFailoverTests.cs testing heartbeat timeout after 2 missed (60s), failover from n0→n1→n2→n0 (wrap around), isolated mode when all hub nodes unreachable, reconnection when hub node becomes reachable
- [ ] T039 [US4] Create tests/Sorcha.Peer.Service.Tests/Integration/SystemRegisterReplicationTests.cs testing full sync completes within 60s for 1000 blueprints, incremental sync completes within 30s, checkpoint persistence across restarts, version-based incremental query
- [ ] T040 [US4] Create tests/Sorcha.Peer.Service.Tests/Integration/PushNotificationDeliveryTests.cs testing blueprint publication triggers push notification, 80% of connected peers receive notification within 30s, remaining peers receive via periodic sync, notification subscription reconnection on stream failure

#### Performance Tests (Success Criteria Validation)

- [ ] T041 [US4] Create tests/Sorcha.Peer.Service.Tests/Performance/PeriodicSyncPerformanceTests.cs using NBomber to validate SC-010 (30s replication time), SC-011 (95% of peers within 5 minutes), measure sync duration, throughput (blueprints/second)
- [ ] T042 [US4] Create tests/Sorcha.Peer.Service.Tests/Performance/PushNotificationPerformanceTests.cs using NBomber to validate SC-016 (80% delivery within 30s), measure notification latency percentiles (p50, p95, p99), test with 100+ connected peers

### Implementation for User Story 4

#### Scenario 1: Hub Node Startup and System Register Initialization

- [X] T043 [US4] Create src/Services/Sorcha.Peer.Service/Discovery/CentralNodeDiscoveryService.cs with DetectIfCentralNode() method checking IsCentralNode flag from appsettings, validating hostname against ExpectedHostnamePattern if ValidateHostname=true
- [X] T044 [US4] Create src/Services/Sorcha.Register.Service/Services/SystemRegisterService.cs with InitializeSystemRegisterAsync() creating system register with ID 00000000-0000-0000-0000-000000000000, SeedRegisterCreationBlueprintAsync() publishing register-creation-v1 blueprint, ValidateSystemRegisterIntegrityAsync() checking genesis record on startup
- [ ] T045 [US4] Update src/Services/Sorcha.Peer.Service/PeerService.cs ExecuteAsync() to detect if hub node on startup, if true skip outbound connection attempts and call AcceptIncomingPeerConnectionsAsync(), if false call ConnectToCentralNodesAsync()
- [X] T046 [US4] Update src/Services/Sorcha.Register.Service/Program.cs to call SystemRegisterService.InitializeSystemRegisterAsync() on startup with idempotency (detect existing system register, skip creation, validate integrity)

#### Scenario 2: Peer Node Startup and Connection to Hub Nodes

- [X] T047 [US4] Create src/Services/Sorcha.Peer.Service/Connection/CentralNodeConnectionManager.cs with ConnectToCentralNodeAsync() using Polly ResiliencePipeline for exponential backoff, connection priority order (n0 priority 0, n1 priority 1, n2 priority 2), 30s connection timeout per attempt
- [ ] T048 [US4] Create src/Services/Sorcha.Peer.Service/Network/CentralNodeConnectionService.cs implementing CentralNodeConnection gRPC service with ConnectToCentralNode() RPC handler validating peer identity, assigning session ID, returning ConnectionResponse with current system register version, connection metadata
- [ ] T049 [US4] Update src/Services/Sorcha.Peer.Service/PeerService.cs ExecuteAsync() to load CentralNodeConfiguration from appsettings.json (list of n0.sorcha.dev:5000, n1.sorcha.dev:5000, n2.sorcha.dev:5000), call CentralNodeConnectionManager.ConnectToCentralNodeAsync() in priority order until first success
- [X] T050 [US4] Create src/Services/Sorcha.Peer.Service/appsettings.json configuration section "CentralNode" with IsCentralNode=false, CentralNodes array [{ NodeId: "n0.sorcha.dev", Hostname: "n0.sorcha.dev", Port: 5000, Priority: 0 }], ExpectedHostnamePattern: "*.sorcha.dev", ValidateHostname: true
- [X] T051 [US4] Update src/Services/Sorcha.Peer.Service/Program.cs to register CentralNodeConnectionManager, CentralNodeDiscoveryService, CentralNodeConfiguration from appsettings in dependency injection container

#### Scenario 3: System Register Replication

- [ ] T052 [US4] Create src/Services/Sorcha.Peer.Service/Replication/SystemRegisterReplicationService.cs implementing SystemRegisterSync gRPC service with FullSync() RPC handler streaming all active blueprints from MongoDB ordered by version, IncrementalSync() RPC handler streaming blueprints WHERE version > request.LastKnownVersion
- [ ] T053 [US4] Create src/Services/Sorcha.Peer.Service/Replication/SystemRegisterCache.cs with in-memory cache (ConcurrentDictionary) for local system register replica, AddOrUpdateBlueprintAsync() method, GetBlueprintByIdAsync() method, GetAllBlueprintsAsync() method
- [ ] T054 [US4] Create src/Services/Sorcha.Peer.Service/Replication/PeriodicSyncService.cs as BackgroundService with PeriodicTimer (5 minutes), ExecuteAsync() calling SystemRegisterReplicationClient.IncrementalSyncAsync() on each tick, updating SyncCheckpoint after successful sync
- [ ] T055 [US4] Create src/Services/Sorcha.Peer.Service/Replication/SystemRegisterReplicationClient.cs with FullSyncAsync() calling SystemRegisterSync.FullSync RPC and processing stream, IncrementalSyncAsync() calling SystemRegisterSync.IncrementalSync RPC, updating SystemRegisterCache with received blueprints
- [ ] T056 [US4] Implement checkpoint persistence in src/Services/Sorcha.Peer.Service/Replication/SyncCheckpointStore.cs with LoadCheckpointAsync() reading from ./data/sync_checkpoint.json, SaveCheckpointAsync() writing to ./data/sync_checkpoint.json, using JSON serialization
- [ ] T057 [US4] Update src/Services/Sorcha.Peer.Service/Program.cs to register SystemRegisterReplicationService (server), SystemRegisterReplicationClient, SystemRegisterCache, PeriodicSyncService, SyncCheckpointStore in dependency injection

#### Scenario 4: Push Notifications for Blueprint Publications

- [X] T058 [US4] Create src/Services/Sorcha.Peer.Service/Replication/PushNotificationHandler.cs with ConcurrentDictionary<string, IServerStreamWriter<BlueprintNotification>> for subscriber tracking, RegisterSubscriber() method, UnregisterSubscriber() method, NotifyBlueprintPublishedAsync() iterating subscribers and calling WriteAsync() with best-effort delivery (log failure, continue)
- [X] T059 [US4] Implement src/Services/Sorcha.Peer.Service/Services/SystemRegisterSyncService.cs SubscribeToPushNotifications() RPC handler maintaining long-lived server stream, calling PushNotificationHandler.RegisterSubscriber() on subscription, streaming BlueprintNotification messages when blueprints published
- [ ] T060 [US4] Create src/Services/Sorcha.Peer.Service/Replication/NotificationSubscriberService.cs as BackgroundService calling SystemRegisterSync.SubscribeToPushNotifications RPC, maintaining subscription stream, handling notifications by triggering SystemRegisterReplicationClient.IncrementalSyncAsync(), auto-reconnecting on stream failure with 5s delay (NOTE: This requires peer-side client implementation)
- [ ] T061 [US4] Update src/Services/Sorcha.Register.Service/Services/SystemRegisterService.cs PublishBlueprintAsync() to call PushNotificationHandler.NotifyBlueprintPublishedAsync() after successful MongoDB insert, passing BlueprintNotification with blueprint ID, version, published timestamp (NOTE: Requires cross-service integration between Register Service and Peer Service)
- [X] T062 [US4] Update src/Services/Sorcha.Peer.Service/Program.cs to register PushNotificationHandler (singleton for subscriber tracking)

#### Scenario 5: Isolated Mode When Hub Nodes Unreachable

- [X] T063 [US4] Update src/Services/Sorcha.Peer.Service/Connection/CentralNodeConnectionManager.cs to add HandleIsolatedModeAsync() method that sets PeerConnectionStatus=Isolated when all hub nodes return failure after retry exhaustion, log "Operating in isolated mode with last known system register replica", continue retrying connections in background with exponential backoff
- [ ] T064 [US4] Create src/Services/Sorcha.Peer.Service/Monitoring/IsolatedModeMonitor.cs with CheckIsolatedStatusAsync() detecting when all hub nodes unreachable, LogIsolatedWarningAsync() logging "Cannot reach central infrastructure - administrators notified", continue serving existing blueprints from SystemRegisterCache (NOTE: Basic isolated mode handling implemented via CentralNodeConnectionManager.HandleIsolatedModeAsync())
- [X] T065 [US4] Update src/Services/Sorcha.Peer.Service/Replication/PeriodicSyncService.cs ExecuteAsync() to catch RpcException when hub node unreachable, call CentralNodeConnectionManager.HandleIsolatedModeAsync(), skip sync iteration (wait for next 5-minute tick), allow service to continue operating
- [X] T066 [US4] Update src/Services/Sorcha.Peer.Service/Replication/PeriodicSyncService.cs to handle isolated mode gracefully - SystemRegisterCache.GetAllBlueprintsAsync() continues serving cached blueprints, background connection retry continues attempting to reconnect (implemented via error handling in PeriodicSyncService)

#### Scenario 6: Hub Node Detection

- [X] T067 [US4] Implement src/Services/Sorcha.Peer.Service/Discovery/CentralNodeDiscoveryService.cs IsCentralNodeWithValidation() checking IsCentralNode flag from config, if true and ValidateHostname=true then call Dns.GetHostName() and validate against ExpectedHostnamePattern using regex, throw InvalidOperationException with clear message if mismatch
- [X] T068 [US4] Update src/Services/Sorcha.Peer.Service/appsettings.json to add CentralNode section examples: for n0.sorcha.dev set IsCentralNode=true, ExpectedHostnamePattern="n0.sorcha.dev", ValidateHostname=true; for peer nodes set IsCentralNode=false
- [ ] T069 [US4] Update src/Services/Sorcha.Peer.Service/PeerService.cs ExecuteAsync() to call CentralNodeDiscoveryService.IsCentralNodeWithValidation() on startup, log result "Detected as hub node: {IsCentralNode}", branch logic to AcceptIncomingPeerConnectionsAsync() or ConnectToCentralNodesAsync()
- [ ] T070 [US4] Create src/Services/Sorcha.Peer.Service/Network/PeerConnectionListener.cs with AcceptIncomingPeerConnectionsAsync() method for hub nodes, registering gRPC services (CentralNodeConnectionService, SystemRegisterReplicationService, HeartbeatService), logging "Central node ready to accept peer connections"

#### Scenario 7: Connection Failure Handling

- [X] T071 [US4] Create src/Services/Sorcha.Peer.Service/Monitoring/HeartbeatMonitorService.cs as BackgroundService with PeriodicTimer (30s interval), ExecuteAsync() sending HeartbeatMessage via Heartbeat.SendHeartbeatAsync RPC with 30s timeout, incrementing sequence number, tracking LastSyncVersion
- [X] T072 [US4] Implement src/Services/Sorcha.Peer.Service/Monitoring/HeartbeatMonitorService.cs HandleHeartbeatTimeoutAsync() catching RpcException with StatusCode.DeadlineExceeded, calling ActivePeerInfo.RecordMissedHeartbeat(), if MissedHeartbeats >= 2 then trigger CentralNodeConnectionManager.FailoverToNextNodeAsync()
- [X] T073 [US4] Create src/Services/Sorcha.Peer.Service/Services/HeartbeatService.cs implementing Heartbeat gRPC service with SendHeartbeat() RPC handler recording peer heartbeat timestamp, checking peer sync version lag (LastSyncVersion vs CurrentSystemRegisterVersion), returning HeartbeatAcknowledgement with RecommendedAction=SYNC if peer behind
- [X] T074 [US4] Implement src/Services/Sorcha.Peer.Service/Connection/CentralNodeConnectionManager.cs FailoverToNextNodeAsync() disconnecting from current hub node (calling DisconnectFromCentralNode RPC), incrementing priority (n0→n1→n2→n0 wrap-around), calling ConnectToCentralNodeAsync() with next hub node, resetting SyncCheckpoint if connection to different hub node
- [X] T075 [US4] Update src/Services/Sorcha.Peer.Service/Monitoring/HeartbeatMonitorService.cs SendHeartbeatAsync() to use gRPC HeartbeatService.SendHeartbeat RPC and add HandleRecommendedActionAsync() method - if SYNC then trigger SystemRegisterReplicationClient.IncrementalSyncAsync(), if FAILOVER then call CentralNodeConnectionManager.FailoverToNextNodeAsync(), if RECONNECT then call CentralNodeConnectionManager.ReconnectAsync()
- [X] T076 [US4] Update src/Services/Sorcha.Peer.Service/Program.cs to register HeartbeatMonitorService as BackgroundService, HeartbeatService (gRPC server), configure gRPC client for Heartbeat service with 30s timeout

### Observability and Logging (Cross-Cutting)

- [X] T077 [P] [US4] Add structured logging to src/Services/Sorcha.Peer.Service/Network/CentralNodeConnectionManager.cs for connection attempts, failures, successes, failover events with correlation ID (session ID)
- [X] T078 [P] [US4] Add structured logging to src/Services/Sorcha.Peer.Service/Monitoring/HeartbeatMonitorService.cs for heartbeat sent, acknowledged, timeout, failover trigger with peer ID, sequence number, latency
- [X] T079 [P] [US4] Add structured logging to src/Services/Sorcha.Peer.Service/Replication/SystemRegisterReplicationService.cs for sync start, progress (blueprints processed), completion, failure with peer ID, sync duration, blueprints count
- [X] T080 [P] [US4] Add OpenTelemetry metrics to src/Services/Sorcha.Peer.Service/ for connection_status (gauge), heartbeat_latency (histogram), sync_duration (histogram), push_notification_delivery_rate (counter), failover_count (counter)
- [X] T081 [P] [US4] Add OpenTelemetry traces to src/Services/Sorcha.Peer.Service/ for full sync operation, incremental sync operation, push notification delivery, heartbeat request-response, connection lifecycle (connect, disconnect, failover)

**Checkpoint**: At this point, User Story 4 should be fully functional and testable independently. All 7 acceptance scenarios implemented and validated.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple components across User Story 4

- [X] T082 [P] Update docs/API-DOCUMENTATION.md with new gRPC services (CentralNodeConnection, SystemRegisterSync, Heartbeat) including RPC descriptions, message formats, examples using grpcurl
- [X] T083 [P] Update docs/architecture.md with system register replication architecture diagram showing hub nodes, peer nodes, MongoDB, replication flows (full sync, incremental sync, push notifications)
- [ ] T084 [P] Update docs/development-status.md with Peer Service completion status updated to reflect hub node connection, system register replication, heartbeat monitoring features complete
- [ ] T085 [P] Update src/Services/Sorcha.Peer.Service/README.md with configuration guide for hub nodes vs peer nodes, appsettings.json examples, running locally, testing heartbeat and sync
- [ ] T086 [P] Create specs/001-register-genesis/quickstart.md with step-by-step guide: start MongoDB, configure hub node (n0), configure peer node, verify connection, verify sync, publish blueprint, verify push notification
- [ ] T087 Code cleanup and refactoring: extract common gRPC error handling, consolidate retry logic, apply consistent naming conventions across CentralNodeConnectionManager, SystemRegisterReplicationService, HeartbeatMonitorService
- [ ] T088 Performance optimization: benchmark MongoDB query performance for GetBlueprintsSinceVersionAsync(), add caching for frequently accessed blueprints, optimize gRPC streaming buffer size for large blueprint transfers
- [ ] T089 [P] Security hardening: implement TLS for all gRPC connections (https:// instead of http://), add peer authentication using metadata bearer tokens, implement rate limiting (1 connection/s, 1 heartbeat/30s, 1 sync/min per peer)
- [ ] T090 [P] Additional unit tests for edge cases: clock skew handling (timestamp validation ±60s), out-of-order heartbeat sequence numbers, concurrent sync requests, MongoDB connection failure during sync
- [ ] T091 Run quickstart.md validation: follow step-by-step guide to start 3 hub nodes (n0, n1, n2), start 2 peer nodes, verify connections, verify full sync, verify periodic sync (wait 5 min), publish blueprint, verify push notifications, kill n0 (verify peer fails over to n1), kill n1 (verify peer fails over to n2), kill n2 (verify peer enters isolated mode)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion (proto compilation) - BLOCKS all user story work
- **User Story 4 (Phase 3)**: Depends on Foundational phase completion - all tasks map to acceptance scenarios 1-7
- **Polish (Phase 4)**: Depends on User Story 4 core implementation completion

### User Story 4 Internal Dependencies

**Critical Path** (must be sequential):
1. Tests (T030-T042) written FIRST (TDD approach)
2. Central node detection (T043-T046) enables branching logic
3. Connection management (T047-T051) enables peer-to-central connection
4. System register repository (T025-T027) enables storage layer
5. Replication services (T052-T057) enable sync functionality
6. Push notifications (T058-T062) enable real-time updates
7. Heartbeat monitoring (T071-T076) enables connection health tracking
8. Isolated mode (T063-T066) enables graceful degradation

**Parallel Opportunities** (can work simultaneously):
- Configuration classes (T007-T009)
- Core entities (T010-T018)
- Validators (T019-T023)
- Unit tests (T030-T035) for different services
- Integration tests (T036-T040) after core implementation
- Observability (T077-T081)

### Within User Story 4 Scenarios

**Scenario 1** (Hub Node Startup): T043-T046
- T043 (DetectIfCentralNode) before T045 (PeerService branching logic)
- T044 (SystemRegisterService) before T046 (Program.cs initialization)

**Scenario 2** (Peer Connection): T047-T051
- T047 (CentralNodeConnectionManager) before T049 (PeerService connection call)
- T050 (appsettings.json) before T049 (loading config)

**Scenario 3** (Replication): T052-T057
- T025-T026 (MongoDB repository) before T052 (replication service using repository)
- T053 (SystemRegisterCache) before T055 (replication client updating cache)
- T056 (checkpoint persistence) before T054 (periodic sync using checkpoint)

**Scenario 4** (Push Notifications): T058-T062
- T058 (PushNotificationHandler) before T059 (replication service using handler)
- T059 (SubscribeToPushNotifications RPC) before T060 (client subscribing)

**Scenario 5** (Isolated Mode): T063-T066
- T063 (connection manager isolated state) before T064 (isolated mode monitor)

**Scenario 6** (Hub Node Detection): T067-T070
- T067 (IsCentralNodeWithValidation) before T069 (PeerService calling validation)

**Scenario 7** (Heartbeat Failover): T071-T076
- T071 (HeartbeatMonitorService) before T072 (timeout handling)
- T073 (HeartbeatService server) independent
- T074 (FailoverToNextNodeAsync) before T072 (timeout handler calling failover)

### Parallel Execution Example: Foundational Phase

```bash
# All core entities can be created in parallel:
Task T010: "Create CentralNodeConnectionStatus.cs enum"
Task T011: "Create PeerConnectionStatus.cs enum"
Task T012: "Create NotificationType.cs enum"
Task T013: "Create CentralNodeInfo.cs"
Task T014: "Create SystemRegisterEntry.cs"
Task T015: "Create HeartbeatMessage.cs"
Task T016: "Create ActivePeerInfo.cs"
Task T017: "Create SyncCheckpoint.cs"
Task T018: "Create BlueprintNotification.cs"

# All validators can be created in parallel:
Task T019: "Create CentralNodeValidator.cs"
Task T020: "Create HeartbeatValidator.cs"
Task T021: "Create SyncValidator.cs"
Task T022: "Create RetryBackoffValidator.cs"
Task T023: "Create SystemRegisterValidator.cs"
```

### Parallel Execution Example: User Story 4 Tests

```bash
# All unit tests can be written in parallel (after entities exist):
Task T030: "CentralNodeDiscoveryServiceTests.cs"
Task T031: "CentralNodeConnectionManagerTests.cs"
Task T032: "HeartbeatMonitorServiceTests.cs"
Task T033: "SystemRegisterReplicationServiceTests.cs"
Task T034: "PeriodicSyncServiceTests.cs"
Task T035: "PushNotificationHandlerTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 4 Complete)

1. ✅ Complete Phase 1: Setup (proto compilation, test directories)
2. ✅ Complete Phase 2: Foundational (CRITICAL - all core entities, validators, MongoDB repository)
3. ✅ Complete Phase 3: User Story 4 (all 7 acceptance scenarios)
   - Write tests FIRST (T030-T042) - ensure they FAIL
   - Implement Scenario 1 (hub node startup) - T043-T046
   - Implement Scenario 2 (peer connection) - T047-T051
   - Implement Scenario 3 (replication) - T052-T057
   - Implement Scenario 4 (push notifications) - T058-T062
   - Implement Scenario 5 (isolated mode) - T063-T066
   - Implement Scenario 6 (hub node detection) - T067-T070
   - Implement Scenario 7 (heartbeat failover) - T071-T076
   - Add observability - T077-T081
4. **STOP and VALIDATE**: Test all 7 scenarios independently using quickstart.md
5. Complete Phase 4: Polish (documentation, optimization, security)
6. Deploy/demo if ready

### Validation Checkpoints

After **Phase 2** (Foundational):
- Build succeeds without errors
- All core entities instantiate correctly
- All validators pass unit tests
- MongoDB repository CRUD operations work with Testcontainers

After **Scenario 1** (Hub Node Startup):
- Central node starts with IsCentralNode=true
- System register created with ID 00000000-0000-0000-0000-000000000000
- register-creation-v1 blueprint seeded
- Central node listens for incoming connections

After **Scenario 2** (Peer Connection):
- Peer connects to n0.sorcha.dev successfully
- ConnectionResponse includes session ID and system register version
- Full sync initiated automatically

After **Scenario 3** (Replication):
- Full sync completes within 60s for 100 test blueprints
- Incremental sync fetches only new blueprints since checkpoint
- Periodic sync runs every 5 minutes

After **Scenario 4** (Push Notifications):
- Blueprint publication triggers push notification
- Peer receives notification within 30s
- Notification triggers incremental sync to fetch full blueprint

After **Scenario 5** (Isolated Mode):
- Kill all hub nodes
- Peer enters isolated mode
- Peer continues serving cached blueprints
- Peer retries connections in background

After **Scenario 6** (Hub Node Detection):
- n0.sorcha.dev detects as hub node
- peer-1.local detects as peer node
- Hostname validation catches misconfiguration

After **Scenario 7** (Heartbeat Failover):
- Heartbeat sent every 30s
- 2 missed heartbeats (60s) triggers failover n0→n1
- Kill n1, failover to n2
- Kill n2, enter isolated mode

### Acceptance Criteria (from spec.md)

**User Story 4 - Success Criteria Mapping**:

| Scenario | Success Criteria | Validation Tasks |
|----------|------------------|------------------|
| 1. Hub Node Startup | SC-009 (100% successful initialization) | T036, T046 |
| 2. Peer Connection | SC-014 (30s connection time) | T037, T051 |
| 3. System Register Replication | SC-010 (30s replication), SC-012 (2s integrity check) | T039, T041 |
| 4. Push Notifications | SC-016 (80% delivery in 30s) | T040, T042 |
| 5. Isolated Mode | SC-015 (100% uptime hub nodes) | T038, T066 |
| 6. Hub Node Detection | SC-013 (100% correct detection) | T036, T069 |
| 7. Heartbeat Failover | SC-014 (30s connection), FR-036 (30s timeout) | T038, T075 |

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[US4] label**: Maps task to User Story 4 acceptance scenarios for traceability
- **TDD approach**: Tests (T030-T042) MUST be written FIRST and FAIL before implementation
- **Constitution compliance**: >85% coverage target, tests verify all functional requirements (FR-001 through FR-037)
- **Performance goals**: Full sync <60s (SC-010), push delivery 80% in 30s (SC-016), heartbeat 30s (FR-036)
- **Commit strategy**: Commit after each scenario completion (checkpoints in Phase 3)
- **Integration test dependencies**: Requires Testcontainers for MongoDB, 3 hub node instances, 2 peer node instances
- **Avoid**: Vague tasks, same file conflicts, circular dependencies between scenarios

---

## Dependency Graph Visualization

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational) ← BLOCKS Phase 3
    ↓
Phase 3 (User Story 4)
    ├─ Scenario 1 (Hub Node) → T043-T046
    ├─ Scenario 2 (Peer Connection) → T047-T051 (depends on Scenario 1)
    ├─ Scenario 3 (Replication) → T052-T057 (depends on Scenario 2, MongoDB repo T025-T027)
    ├─ Scenario 4 (Push Notifications) → T058-T062 (depends on Scenario 3)
    ├─ Scenario 5 (Isolated Mode) → T063-T066 (depends on Scenario 2)
    ├─ Scenario 6 (Central Detection) → T067-T070 (depends on Scenario 1)
    └─ Scenario 7 (Heartbeat Failover) → T071-T076 (depends on Scenario 2)
    ↓
Phase 4 (Polish) → T082-T091 (depends on Scenarios 1-7 complete)
```

---

**Generated**: 2025-12-13
**Tool**: Manual generation based on `/speckit.plan` methodology
**Status**: ✅ Ready for Implementation
**Total Tasks**: 91
**Estimated Effort**: 4-6 weeks (1 developer) or 2-3 weeks (parallel team)
**Test Coverage Target**: >85% (per constitution requirement)
**Priority**: P1 (MVP feature)
