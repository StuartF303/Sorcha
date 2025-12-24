# Research Document: Peer Service Hub Node Connection

**Feature Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Complete
**Purpose**: Technical research and decision documentation for peer service hub node connection capabilities

---

## Overview

This document captures the research, analysis, and technical decisions for implementing peer service enhancements to support hub node discovery, system register replication, heartbeat monitoring, and connection management. The implementation enables peer nodes to connect to hub nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev), synchronize the system register containing published blueprints, and maintain connection health.

**Technology Stack**:
- .NET 10 / C# 13
- gRPC (Grpc.Net 2.71.0)
- MongoDB (MongoDB.Driver 3.5.2)
- Redis (StackExchange.Redis 2.10.1)
- Polly (8.6.5)

**Performance Goals**:
- 30s connection timeout
- 5-minute periodic sync
- 30s push notification delivery to 80% of peers
- 30s heartbeat timeout

---

## Research Task 1: Hub Node Detection Mechanism

### Question
How should the peer service detect if it is running on the sorcha.dev domain to determine whether to act as a hub node or a peer node?

### Options Considered

#### Option 1: DNS Hostname Check
- **Approach**: Use `Environment.MachineName` or `Dns.GetHostName()` to check if hostname matches n0/n1/n2.sorcha.dev pattern
- **Pros**:
  - Simple implementation
  - No external dependencies
  - Works in Docker/Kubernetes with proper hostname configuration
- **Cons**:
  - Relies on correct hostname configuration
  - May not work reliably in all container environments
  - Hostname can be spoofed

#### Option 2: DNS Reverse Lookup
- **Approach**: Get external IP address (via STUN or HTTP lookup) and perform reverse DNS lookup to verify sorcha.dev domain
- **Pros**:
  - More reliable verification of actual domain
  - Harder to spoof
- **Cons**:
  - Requires network connectivity for detection
  - Additional latency on startup
  - Reverse DNS not always configured correctly

#### Option 3: Explicit Configuration Flag
- **Approach**: Add `IsCentralNode` boolean flag to `appsettings.json`
- **Pros**:
  - Explicit and predictable
  - Works in all environments
  - Easy to test locally
  - No dependency on infrastructure
- **Cons**:
  - Manual configuration required
  - Risk of misconfiguration
  - No automatic detection

#### Option 4: Hybrid: Configuration with Hostname Validation
- **Approach**: Use explicit configuration flag but validate against hostname for safety
- **Pros**:
  - Best of both worlds: explicit control with validation
  - Catches configuration errors
  - Works reliably
- **Cons**:
  - Slightly more complex
  - Two places to configure

### Decision: **Option 4 - Hybrid Configuration with Hostname Validation**

**Rationale**:
- Provides explicit control needed for production deployments
- Validates configuration against hostname to catch errors
- Allows local development/testing with simple config flag
- Aligns with existing `PeerServiceConfiguration` pattern in the codebase
- Provides clear error messages when misconfigured

**Implementation Notes**:
```csharp
public class CentralNodeConfiguration
{
    /// <summary>
    /// Explicitly set this node as a hub node
    /// </summary>
    public bool IsCentralNode { get; set; } = false;

    /// <summary>
    /// Expected hostname pattern for hub nodes (e.g., "*.sorcha.dev")
    /// </summary>
    public string? ExpectedHostnamePattern { get; set; } = "*.sorcha.dev";

    /// <summary>
    /// Whether to validate hostname matches expected pattern
    /// </summary>
    public bool ValidateHostname { get; set; } = true;
}

// Validation logic
public bool IsCentralNodeWithValidation()
{
    if (!IsCentralNode) return false;

    if (!ValidateHostname) return true;

    var hostname = Dns.GetHostName();
    var pattern = ExpectedHostnamePattern ?? "*.sorcha.dev";

    // Check if hostname matches pattern
    if (!MatchesPattern(hostname, pattern))
    {
        _logger.LogError("IsCentralNode is true but hostname '{Hostname}' does not match pattern '{Pattern}'",
            hostname, pattern);
        throw new InvalidOperationException($"Central node configuration invalid: hostname mismatch");
    }

    return true;
}
```

**Configuration Example**:
```json
{
  "PeerService": {
    "CentralNode": {
      "IsCentralNode": true,
      "ExpectedHostnamePattern": "*.sorcha.dev",
      "ValidateHostname": true
    }
  }
}
```

---

## Research Task 2: System Register Storage Strategy

### Question
How should the system register be stored in MongoDB? What collection design provides optimal query performance and replication efficiency?

### Options Considered

#### Option 1: Single Document with Array of Blueprints
- **Approach**: Store all blueprints in a single document with `blueprints` array field
- **Schema**:
  ```json
  {
    "_id": "system-register",
    "registerId": "00000000-0000-0000-0000-000000000000",
    "version": 123,
    "lastUpdated": "2025-12-13T12:00:00Z",
    "blueprints": [
      {
        "blueprintId": "register-creation-v1",
        "document": { ... },
        "publishedAt": "2025-12-13T10:00:00Z",
        "publishedBy": "system"
      }
    ]
  }
  ```
- **Pros**:
  - Simple to replicate (single document)
  - Atomic updates
  - Easy versioning
- **Cons**:
  - MongoDB document size limit (16MB)
  - Cannot index individual blueprints efficiently
  - Full document must be transferred during replication
  - Performance degrades as array grows

#### Option 2: Collection of Blueprint Documents
- **Approach**: Separate document for each blueprint in `system_register_blueprints` collection
- **Schema**:
  ```json
  {
    "_id": "register-creation-v1",
    "registerId": "00000000-0000-0000-0000-000000000000",
    "blueprintId": "register-creation-v1",
    "document": { ... },
    "publishedAt": "2025-12-13T10:00:00Z",
    "publishedBy": "system",
    "version": 1,
    "isActive": true
  }
  ```
- **Pros**:
  - No document size limits (scales to thousands of blueprints)
  - Can index by blueprintId, publishedAt, publishedBy
  - Incremental replication (only new/changed blueprints)
  - Better query performance
- **Cons**:
  - Multiple documents to replicate
  - Requires versioning logic for consistency
  - Slightly more complex

#### Option 3: Hybrid with Metadata Document
- **Approach**: Collection of blueprints + single metadata document for versioning
- **Pros**:
  - Combines benefits of both approaches
  - Efficient incremental sync using version number
- **Cons**:
  - More complex implementation

### Decision: **Option 2 - Collection of Blueprint Documents**

**Rationale**:
- Aligns with existing MongoDB repository pattern in Register Service
- Scales beyond 16MB limit as system grows
- Enables efficient incremental synchronization (only transfer new blueprints since last sync)
- Supports efficient querying by blueprint ID
- Matches the docket storage pattern already used in Register Service
- Better performance characteristics for real-world usage

**Implementation Notes**:
```csharp
public class SystemRegisterEntry
{
    [BsonId]
    public string BlueprintId { get; set; } = string.Empty;

    public Guid RegisterId { get; set; } // System register ID (well-known constant)

    public BsonDocument Document { get; set; } = new(); // Blueprint JSON

    public DateTime PublishedAt { get; set; }

    public string PublishedBy { get; set; } = string.Empty;

    public long Version { get; set; } // Incrementing version for sync

    public bool IsActive { get; set; } = true;

    [BsonIgnoreIfNull]
    public string? PublicationTransactionId { get; set; } // Link to register transaction
}

// Collection indexes
public void CreateIndexes(IMongoCollection<SystemRegisterEntry> collection)
{
    collection.Indexes.CreateOne(
        new CreateIndexModel<SystemRegisterEntry>(
            Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.Version)));

    collection.Indexes.CreateOne(
        new CreateIndexModel<SystemRegisterEntry>(
            Builders<SystemRegisterEntry>.IndexKeys.Descending(x => x.PublishedAt)));

    collection.Indexes.CreateOne(
        new CreateIndexModel<SystemRegisterEntry>(
            Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.IsActive)));
}
```

**Collection Naming**: `sorcha_system_register_blueprints`

**Query Patterns**:
- Get all blueprints: `db.sorcha_system_register_blueprints.find({ isActive: true })`
- Get blueprints since version: `db.sorcha_system_register_blueprints.find({ version: { $gt: 100 } })`
- Get specific blueprint: `db.sorcha_system_register_blueprints.findOne({ blueprintId: "register-creation-v1" })`

---

## Research Task 3: gRPC Streaming for System Register Sync

### Question
Should the system register synchronization use bidirectional streaming or server streaming? What pattern provides the best balance of efficiency and simplicity?

### Options Considered

#### Option 1: Server Streaming (Pull Model)
- **Approach**: Client initiates sync request, server streams blueprint entries
- **Proto**:
  ```protobuf
  service SystemRegisterSync {
    rpc SyncRegister(SyncRequest) returns (stream BlueprintEntry);
  }

  message SyncRequest {
    int64 last_known_version = 1;
    string peer_id = 2;
  }
  ```
- **Pros**:
  - Simple client implementation
  - Clear request/response semantics
  - Easy to implement incremental sync
  - Client controls when to sync
- **Cons**:
  - Requires periodic polling (5-minute intervals)
  - No immediate push on blueprint publication
  - Higher latency for updates

#### Option 2: Bidirectional Streaming (Hybrid Push/Pull)
- **Approach**: Long-lived stream where server pushes updates and client can request sync
- **Proto**:
  ```protobuf
  service SystemRegisterSync {
    rpc StreamRegister(stream SyncMessage) returns (stream BlueprintEntry);
  }

  message SyncMessage {
    oneof message {
      SyncRequest request = 1;
      Heartbeat heartbeat = 2;
    }
  }
  ```
- **Pros**:
  - Immediate push notifications for new blueprints
  - Can multiplex heartbeat and sync on same stream
  - Lower latency for updates
- **Cons**:
  - More complex implementation
  - Harder to handle reconnections
  - Stream lifecycle management complexity

#### Option 3: Separate Services for Pull and Push
- **Approach**: Server streaming for periodic sync + unary RPC for push notifications
- **Proto**:
  ```protobuf
  service SystemRegisterSync {
    rpc SyncRegister(SyncRequest) returns (stream BlueprintEntry);
    rpc NotifyNewBlueprint(BlueprintNotification) returns (Acknowledgement);
  }
  ```
- **Pros**:
  - Clear separation of concerns
  - Pull sync can be stateless
  - Push notification is simple unary call
  - Easy to test each independently
- **Cons**:
  - Two separate mechanisms to maintain
  - Client needs to implement both

#### Option 4: Server Streaming with Connection Reuse
- **Approach**: Server streaming for sync + maintain connection for push (no bidirectional)
- **Proto**: Same as Option 1, but keep connection alive for server-initiated pushes
- **Pros**:
  - Simpler than bidirectional
  - Supports both pull and push
- **Cons**:
  - gRPC server streaming doesn't naturally support server-initiated messages
  - Requires connection state management

### Decision: **Option 3 - Separate Services for Pull and Push**

**Rationale**:
- Aligns with the spec's hybrid synchronization model (5-minute periodic + immediate push)
- Clear separation makes testing and debugging easier
- Pull sync (server streaming) handles bulk synchronization efficiently
- Push notification (unary RPC) handles immediate updates simply
- Each mechanism can be optimized independently
- Easier to implement retry logic for failed pushes
- Matches existing Peer Service patterns (PeerDiscovery uses unary RPCs)

**Implementation Notes**:

**Proto Definition** (`Protos/SystemRegisterSync.proto`):
```protobuf
syntax = "proto3";

package sorcha.peer.systemregister;

option csharp_namespace = "Sorcha.Peer.Service.Protos";

service SystemRegisterSync {
  // Periodic pull synchronization (server streaming)
  rpc SyncRegister(SyncRequest) returns (stream BlueprintEntry);

  // Push notification for immediate updates (unary)
  rpc NotifyBlueprintPublished(BlueprintNotification) returns (Acknowledgement);

  // Get current sync checkpoint
  rpc GetSyncCheckpoint(CheckpointRequest) returns (SyncCheckpoint);
}

message SyncRequest {
  string peer_id = 1;
  int64 last_known_version = 2; // For incremental sync
  bool full_sync = 3; // Force full sync
}

message BlueprintEntry {
  string blueprint_id = 1;
  bytes blueprint_document = 2; // JSON serialized
  int64 version = 3;
  int64 published_at = 4; // Unix timestamp
  string published_by = 5;
}

message BlueprintNotification {
  string blueprint_id = 1;
  int64 version = 2;
  repeated string target_peer_ids = 3; // Empty = broadcast to all
}

message Acknowledgement {
  bool success = 1;
  string message = 2;
}

message CheckpointRequest {
  string peer_id = 1;
}

message SyncCheckpoint {
  int64 current_version = 1;
  int64 last_sync_time = 2;
  int32 total_blueprints = 3;
}
```

**Sync Flow**:
1. **Periodic Sync (every 5 minutes)**: Peer calls `SyncRegister(last_known_version)`, server streams new blueprints
2. **Push Notification**: Central node calls `NotifyBlueprintPublished(blueprint_id)` on all connected peers
3. **Fallback**: If push fails, peer will get update on next periodic sync

**Error Handling**:
- Failed periodic sync: Retry with exponential backoff
- Failed push notification: Log and rely on periodic sync to eventually propagate
- Stream disconnection: Reconnect and resume from last checkpoint

---

## Research Task 4: Heartbeat Protocol Design

### Question
Should heartbeat monitoring use a dedicated gRPC service, be embedded in the replication stream, or use HTTP health checks?

### Options Considered

#### Option 1: Dedicated gRPC Heartbeat Service
- **Approach**: Separate `HeartbeatService` with unary RPC calls
- **Proto**:
  ```protobuf
  service Heartbeat {
    rpc SendHeartbeat(HeartbeatMessage) returns (HeartbeatAcknowledgement);
  }
  ```
- **Pros**:
  - Simple and explicit
  - Easy to test independently
  - Clear semantics
  - Can include custom health metrics
- **Cons**:
  - Additional network calls
  - Separate from data sync connection

#### Option 2: Embedded in Replication Stream
- **Approach**: Send heartbeat messages within bidirectional sync stream
- **Pros**:
  - Reuses existing connection
  - No additional network overhead
  - Automatic correlation with sync health
- **Cons**:
  - Couples heartbeat to sync implementation
  - More complex stream message handling
  - Not applicable with server-streaming approach (Option 3 from Task 3)

#### Option 3: HTTP Health Check Endpoint
- **Approach**: Standard HTTP endpoint that peer polls
- **Pros**:
  - Standard pattern
  - Works with load balancers
  - Widely supported
- **Cons**:
  - HTTP overhead vs gRPC
  - Less expressive than custom protocol
  - Doesn't validate gRPC connection health

#### Option 4: gRPC Health Check Protocol
- **Approach**: Use standard gRPC health checking protocol
- **Proto**: Built-in `grpc.health.v1.Health` service
- **Pros**:
  - Industry standard
  - Supported by gRPC tooling
  - Works with Kubernetes probes
- **Cons**:
  - Generic (not specific to peer connection)
  - Doesn't carry custom metrics
  - Service-level health, not connection-level

### Decision: **Option 1 - Dedicated gRPC Heartbeat Service**

**Rationale**:
- Provides explicit peer-to-peer connection health monitoring
- Allows custom heartbeat payload (version, last sync time, metrics)
- Independent of sync implementation choices
- Aligns with 30-second heartbeat timeout requirement
- Can be easily tested in isolation
- Matches existing Peer Service pattern (separate services for different concerns)
- Simple to implement with Polly timeout policies

**Implementation Notes**:

**Proto Definition** (`Protos/Heartbeat.proto`):
```protobuf
syntax = "proto3";

package sorcha.peer.heartbeat;

option csharp_namespace = "Sorcha.Peer.Service.Protos";

service Heartbeat {
  // Send heartbeat to connected hub node
  rpc SendHeartbeat(HeartbeatMessage) returns (HeartbeatAcknowledgement);

  // Get heartbeat status from connected peer
  rpc GetHeartbeatStatus(StatusRequest) returns (HeartbeatStatus);
}

message HeartbeatMessage {
  string peer_id = 1;
  int64 timestamp = 2; // Unix timestamp
  int64 sequence_number = 3; // Incrementing sequence
  int64 last_sync_version = 4; // Last known system register version

  // Optional metrics
  int32 active_connections = 5;
  double cpu_usage_percent = 6;
  double memory_usage_mb = 7;
}

message HeartbeatAcknowledgement {
  bool success = 1;
  int64 timestamp = 2;
  string central_node_id = 3;
  int64 current_system_register_version = 4; // Allows peer to detect if behind
}

message StatusRequest {
  string peer_id = 1;
}

message HeartbeatStatus {
  int64 last_heartbeat_time = 1;
  int64 missed_heartbeats = 2;
  bool is_healthy = 3;
  string status_message = 4;
}
```

**Heartbeat Monitor Service**:
```csharp
public class HeartbeatMonitorService : BackgroundService
{
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(30);
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly Heartbeat.HeartbeatClient _client;
    private long _sequenceNumber = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_heartbeatInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var request = new HeartbeatMessage
                {
                    PeerId = _nodeId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                    LastSyncVersion = _lastKnownVersion
                };

                using var cts = new CancellationTokenSource(_heartbeatTimeout);
                var response = await _client.SendHeartbeatAsync(request, cancellationToken: cts.Token);

                _logger.LogDebug("Heartbeat acknowledged by {CentralNode}", response.CentralNodeId);

                // Check if peer is behind on sync
                if (response.CurrentSystemRegisterVersion > _lastKnownVersion)
                {
                    _logger.LogInformation("System register version mismatch - triggering sync");
                    await TriggerIncrementalSync();
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                _logger.LogWarning("Heartbeat timeout - connection to hub node may be lost");
                await HandleHeartbeatTimeout();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat failed");
            }
        }
    }

    private async Task HandleHeartbeatTimeout()
    {
        // Trigger failover to next hub node
        await _centralNodeManager.FailoverToNextNode();
    }
}
```

**Timeout Handling**:
- 30-second timeout per heartbeat RPC call
- If timeout occurs, trigger failover to next hub node
- After 2 consecutive timeouts (60 seconds total), consider connection failed

---

## Research Task 5: Exponential Backoff Implementation

### Question
Should connection retry logic use Polly resilience policies or a custom exponential backoff implementation?

### Options Considered

#### Option 1: Polly Resilience Policies
- **Approach**: Use Polly's `WaitAndRetryAsync` with exponential backoff strategy
- **Implementation**:
  ```csharp
  var retryPolicy = Policy
      .Handle<RpcException>()
      .WaitAndRetryAsync(
          retryCount: 10,
          sleepDurationProvider: attempt =>
              TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt - 1), 60)),
          onRetry: (exception, timespan, retryCount, context) =>
          {
              _logger.LogWarning("Retry {RetryCount} after {Delay}s", retryCount, timespan.TotalSeconds);
          });
  ```
- **Pros**:
  - Industry-standard library (already used in Sorcha.Storage.Redis)
  - Well-tested and maintained
  - Rich policy composition (combine with circuit breaker)
  - Built-in jitter support
  - Excellent observability hooks
- **Cons**:
  - Additional dependency (already present)
  - Learning curve for team
  - May be overkill for simple retry logic

#### Option 2: Custom Exponential Backoff
- **Approach**: Simple custom implementation
- **Implementation**:
  ```csharp
  public async Task<bool> ConnectWithBackoff(CancellationToken ct)
  {
      var delay = TimeSpan.FromSeconds(1);
      var maxDelay = TimeSpan.FromMinutes(1);
      var multiplier = 2.0;

      for (int attempt = 1; attempt <= 10; attempt++)
      {
          try
          {
              await ConnectToCentralNode(ct);
              return true;
          }
          catch (Exception ex)
          {
              _logger.LogWarning("Connection attempt {Attempt} failed: {Error}", attempt, ex.Message);

              if (attempt < 10)
              {
                  await Task.Delay(delay, ct);
                  delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * multiplier, maxDelay.TotalMilliseconds));
              }
          }
      }

      return false;
  }
  ```
- **Pros**:
  - No external dependency
  - Simple to understand
  - Full control over logic
- **Cons**:
  - Need to implement and test ourselves
  - Missing features like jitter
  - Less observable
  - Reinventing the wheel

#### Option 3: Polly with ResiliencePipeline (Polly v8)
- **Approach**: Use modern Polly v8 `ResiliencePipeline` builder
- **Implementation**:
  ```csharp
  var pipeline = new ResiliencePipelineBuilder()
      .AddRetry(new RetryStrategyOptions
      {
          MaxRetryAttempts = 10,
          BackoffType = DelayBackoffType.Exponential,
          Delay = TimeSpan.FromSeconds(1),
          MaxDelay = TimeSpan.FromMinutes(1),
          UseJitter = true,
          OnRetry = args =>
          {
              _logger.LogWarning("Retry {Attempt} after {Delay}",
                  args.AttemptNumber, args.RetryDelay);
              return ValueTask.CompletedTask;
          }
      })
      .AddTimeout(TimeSpan.FromSeconds(30))
      .Build();

  await pipeline.ExecuteAsync(async ct => await ConnectToCentralNode(ct), cancellationToken);
  ```
- **Pros**:
  - Modern Polly v8 API (already in use - see RedisCacheStore.cs)
  - Built-in jitter to prevent thundering herd
  - Composable with timeout and circuit breaker
  - Better performance than Polly v7
  - Excellent telemetry integration
- **Cons**:
  - None significant

### Decision: **Option 3 - Polly v8 ResiliencePipeline**

**Rationale**:
- Polly 8.6.5 is already a dependency (Sorcha.Storage.Redis uses it)
- Provides built-in jitter to prevent all peers from retrying simultaneously (thundering herd protection)
- ResiliencePipeline pattern is already established in RedisCacheStore implementation
- Composable with timeout policies for connection attempts
- Better observability with built-in telemetry
- Aligns with specification requirements: 1s initial delay, 2x multiplier, 60s max
- Production-proven and well-tested

**Implementation Notes**:

```csharp
public class CentralNodeConnectionManager
{
    private readonly ResiliencePipeline _connectionPipeline;

    public CentralNodeConnectionManager(ILogger<CentralNodeConnectionManager> logger)
    {
        _connectionPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1), // Initial delay
                MaxDelay = TimeSpan.FromMinutes(1), // Maximum 60s between retries
                UseJitter = true, // Add randomness to prevent thundering herd
                ShouldHandle = new PredicateBuilder().Handle<RpcException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Connection retry {Attempt}/{MaxAttempts} to hub node after {Delay}s: {Exception}",
                        args.AttemptNumber + 1,
                        args.RetryCount,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30), // Connection timeout per attempt
                OnTimeout = args =>
                {
                    logger.LogWarning("Connection attempt timed out after 30s");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<bool> ConnectToCentralNodeAsync(string centralNodeAddress, CancellationToken ct)
    {
        try
        {
            await _connectionPipeline.ExecuteAsync(async token =>
            {
                await EstablishGrpcConnection(centralNodeAddress, token);
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to hub node {Address} after all retries", centralNodeAddress);
            return false;
        }
    }
}
```

**Retry Sequence** (with jitter, approximate):
- Attempt 1: 0s (immediate)
- Attempt 2: ~1s
- Attempt 3: ~2s
- Attempt 4: ~4s
- Attempt 5: ~8s
- Attempt 6: ~16s
- Attempt 7: ~32s
- Attempt 8-10: ~60s (capped at max)

**Total retry time**: ~2 minutes before giving up and trying next hub node

---

## Research Task 6: Push Notification Delivery

### Question
What gRPC pattern should be used for hub nodes to push blueprint publication notifications to connected peer nodes?

### Options Considered

#### Option 1: gRPC Server-Initiated Unary Call (Reverse Connection)
- **Approach**: Central node makes unary RPC call to peer node's gRPC service
- **Requires**: Peer nodes must expose gRPC server endpoint
- **Pros**:
  - Simple unary RPC semantics
  - No long-lived connections needed
  - Standard request/response pattern
- **Cons**:
  - Requires peer nodes to be reachable (firewall, NAT issues)
  - Peers must expose public endpoints
  - Not feasible for peers behind NAT/firewall

#### Option 2: Long-Lived Bidirectional Stream
- **Approach**: Maintain persistent bidirectional stream, server pushes notifications on stream
- **Pros**:
  - Works through NAT/firewall (peer initiates connection)
  - Immediate delivery
  - Multiplexed with other messages
- **Cons**:
  - Complex stream lifecycle management
  - State management for long-lived connections
  - Difficult error recovery

#### Option 3: Unary RPC from Central to Peer Callback Service
- **Approach**: Peer registers callback gRPC service address, central calls it
- **Pros**:
  - Decoupled from sync connection
  - Standard unary pattern
- **Cons**:
  - Same NAT/firewall issues as Option 1
  - Requires peer callback service registration

#### Option 4: Server Streaming with Client Pull
- **Approach**: Peer maintains server streaming connection, pulls notifications from queue
- **Proto**:
  ```protobuf
  service NotificationService {
    rpc SubscribeNotifications(SubscriptionRequest) returns (stream Notification);
  }
  ```
- **Pros**:
  - Works through NAT (peer initiates)
  - Server controls notification delivery
  - Relatively simple stream management
- **Cons**:
  - Requires maintaining streaming connection
  - Not truly "push" (relies on active stream)

#### Option 5: Hybrid: Track Connected Peers + Unary Notification Service
- **Approach**:
  - Peers expose simple notification endpoint
  - Central node tracks connected peers (those currently in sync or heartbeat)
  - Central node attempts to notify via unary RPC
  - Fallback to periodic sync if notification fails
- **Proto**:
  ```protobuf
  // Peer implements this service
  service PeerNotificationReceiver {
    rpc NotifyBlueprintPublished(BlueprintNotification) returns (Acknowledgement);
  }
  ```
- **Pros**:
  - Simple implementation
  - Best-effort delivery
  - Graceful fallback to periodic sync
  - Works for peers that CAN accept connections
  - Doesn't block if peer unreachable
- **Cons**:
  - Won't work for NAT'd peers (but they get periodic sync)
  - Requires peer to expose service

### Decision: **Option 4 - Server Streaming with Client Pull**

**Rationale**:
- Works reliably through NAT/firewall (peer initiates outbound connection)
- Enables true "push" delivery from hub node perspective
- Peer maintains subscription stream to hub node for notifications
- Central node can immediately push to all subscribed peers
- Aligns with specification requirement for 30s push notification delivery
- Graceful fallback: if stream fails, peer relies on 5-minute periodic sync
- Simpler than bidirectional streaming but more powerful than polling

**Implementation Notes**:

**Proto Definition** (`Protos/SystemRegisterSync.proto` - addition):
```protobuf
service SystemRegisterSync {
  // ... existing SyncRegister RPC ...

  // Subscribe to push notifications for new blueprints
  rpc SubscribeNotifications(NotificationSubscription) returns (stream BlueprintNotification);
}

message NotificationSubscription {
  string peer_id = 1;
  int64 subscribe_time = 2;
}

message BlueprintNotification {
  string blueprint_id = 1;
  int64 version = 2;
  int64 published_at = 3;
  string published_by = 4;
  bytes blueprint_summary = 5; // Optional: small metadata, peer fetches full doc via sync
  NotificationType type = 6;
}

enum NotificationType {
  BLUEPRINT_PUBLISHED = 0;
  BLUEPRINT_UPDATED = 1;
  BLUEPRINT_DEPRECATED = 2;
}
```

**Hub Node Push Handler**:
```csharp
public class PushNotificationHandler
{
    private readonly ConcurrentDictionary<string, IServerStreamWriter<BlueprintNotification>> _subscribers = new();

    public void RegisterSubscriber(string peerId, IServerStreamWriter<BlueprintNotification> stream)
    {
        _subscribers.TryAdd(peerId, stream);
        _logger.LogInformation("Peer {PeerId} subscribed to notifications", peerId);
    }

    public void UnregisterSubscriber(string peerId)
    {
        _subscribers.TryRemove(peerId, out _);
        _logger.LogInformation("Peer {PeerId} unsubscribed from notifications", peerId);
    }

    public async Task NotifyBlueprintPublished(string blueprintId, long version)
    {
        var notification = new BlueprintNotification
        {
            BlueprintId = blueprintId,
            Version = version,
            PublishedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = NotificationType.BlueprintPublished
        };

        var deliveryTasks = _subscribers.Select(async kvp =>
        {
            try
            {
                await kvp.Value.WriteAsync(notification);
                _logger.LogDebug("Notification sent to peer {PeerId}", kvp.Key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify peer {PeerId} - will rely on periodic sync", kvp.Key);
                UnregisterSubscriber(kvp.Key);
            }
        });

        await Task.WhenAll(deliveryTasks);
    }
}
```

**Peer Notification Subscriber**:
```csharp
public class NotificationSubscriberService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var subscription = new NotificationSubscription
                {
                    PeerId = _nodeId,
                    SubscribeTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                using var call = _client.SubscribeNotifications(subscription, cancellationToken: stoppingToken);

                _logger.LogInformation("Subscribed to blueprint notifications");

                await foreach (var notification in call.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    _logger.LogInformation("Received notification for blueprint {BlueprintId} version {Version}",
                        notification.BlueprintId, notification.Version);

                    // Trigger immediate incremental sync to fetch the new blueprint
                    await _syncService.TriggerIncrementalSync(notification.Version);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogInformation("Notification subscription cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification subscription failed - will reconnect");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait before reconnecting
            }
        }
    }
}
```

**Delivery Guarantee**: Best-effort with fallback
- Central node attempts to push to all subscribed peers
- If push fails, peer still receives update via 5-minute periodic sync
- Success criteria: 80% of connected peers notified within 30s (from spec SC-016)
- Remaining 20% receive update within next 5-minute sync window

---

## Research Task 7: Redis Integration for Active Peers List

### Question
Should the active peers list be stored locally in-memory, in Redis for distributed access, or a hybrid approach?

### Options Considered

#### Option 1: Local In-Memory Only
- **Approach**: Each peer maintains its own active peers list in memory (existing `PeerListManager`)
- **Implementation**: `ConcurrentDictionary<string, PeerNode>`
- **Pros**:
  - Simple and fast
  - No external dependency
  - Already implemented in Peer Service
  - Aligns with spec requirement (FR-037: "local active peers list")
- **Cons**:
  - Not visible to other services
  - Lost on restart
  - Cannot query peer status across cluster

#### Option 2: Redis Distributed List
- **Approach**: Store active peers in Redis sorted set or hash
- **Implementation**:
  ```csharp
  // Redis key: "sorcha:peers:active"
  await _redis.SortedSetAddAsync("sorcha:peers:active", peerId, timestamp);
  ```
- **Pros**:
  - Shared across all service instances
  - Survives restarts
  - Can implement cluster-wide peer discovery
  - Enables global network view
- **Cons**:
  - Additional network latency for every update
  - Dependency on Redis availability
  - More complex failure scenarios
  - Spec explicitly states "local" list (FR-037)

#### Option 3: Hybrid with Redis Cache
- **Approach**: Primary in-memory storage, periodic sync to Redis for observability
- **Pros**:
  - Fast local access
  - Redis provides backup and observability
  - Best of both worlds
- **Cons**:
  - More complex
  - Synchronization overhead
  - Eventual consistency issues

#### Option 4: Local In-Memory with Redis for Hub Node Coordination
- **Approach**:
  - Each peer maintains local in-memory list
  - Central nodes optionally publish their connected peers to Redis for monitoring
  - Non-central peers use local-only
- **Pros**:
  - Aligns with FR-037 (local list)
  - Central nodes can provide cluster visibility
  - Minimal overhead for peer nodes
  - Optional Redis dependency
- **Cons**:
  - Inconsistent storage model between central/peer nodes
  - Added complexity

### Decision: **Option 1 - Local In-Memory Only**

**Rationale**:
- Explicitly required by FR-037: "Each peer node MUST maintain its own local active peers list"
- Aligns with specification's intent: track local connection status, not global coordination
- Simplest implementation with best performance
- Existing `PeerListManager` already provides this functionality
- No additional dependencies required
- Failure isolation: peer state is independent
- Matches the architecture pattern where peers only connect to one hub node at a time

**Implementation Notes**:

**Reuse Existing PeerListManager**:
```csharp
// Existing class in Sorcha.Peer.Service/Discovery/PeerListManager.cs
public class PeerListManager
{
    private readonly ConcurrentDictionary<string, PeerNode> _peers = new();

    public void AddPeer(PeerNode peer) { ... }
    public void RemovePeer(string peerId) { ... }
    public List<PeerNode> GetAllPeers() { ... }
    public List<PeerNode> GetHealthyPeers() { ... }
    public PeerNode? GetPeer(string peerId) { ... }
}
```

**Extend for Hub Node Tracking**:
```csharp
public class ActivePeerInfo
{
    public string PeerId { get; set; } = string.Empty;
    public string? ConnectedCentralNodeId { get; set; } // null if this IS hub node
    public DateTime ConnectionEstablished { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public long LastSyncVersion { get; set; }
    public PeerConnectionStatus Status { get; set; }
}

public enum PeerConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    HeartbeatTimeout,
    Isolated // Operating without hub node connection
}

// Extension to PeerListManager
public class PeerListManager
{
    private ActivePeerInfo? _localPeerInfo;

    public void UpdateLocalPeerStatus(string? connectedCentralNode, PeerConnectionStatus status)
    {
        _localPeerInfo = new ActivePeerInfo
        {
            PeerId = _nodeId,
            ConnectedCentralNodeId = connectedCentralNode,
            LastHeartbeat = DateTime.UtcNow,
            Status = status
        };
    }

    public ActivePeerInfo? GetLocalPeerStatus() => _localPeerInfo;
}
```

**No Redis Required**: The active peers list is purely local state used for:
- Tracking which hub node this peer is connected to
- Monitoring local connection health
- Logging and diagnostics

**Hub Node Monitoring** (Optional Future Enhancement):
- If needed, hub nodes could periodically publish connected peer count to Redis for observability
- This would be a metrics/monitoring feature, not core functionality
- Not required for MVP

---

## Summary of Decisions

| Research Area | Decision | Primary Rationale |
|---------------|----------|-------------------|
| **1. Hub Node Detection** | Hybrid: Explicit config flag with hostname validation | Explicit control with safety validation |
| **2. System Register Storage** | MongoDB collection of blueprint documents | Scalability, incremental sync, aligns with existing patterns |
| **3. gRPC Streaming** | Separate pull (server streaming) and push (unary) services | Clear separation, matches hybrid sync model |
| **4. Heartbeat Protocol** | Dedicated gRPC service with unary RPCs | Simple, explicit, independent testing |
| **5. Exponential Backoff** | Polly v8 ResiliencePipeline | Already in use, jitter support, production-proven |
| **6. Push Notifications** | Server streaming with client subscription | Works through NAT, true push delivery, graceful fallback |
| **7. Active Peers List** | Local in-memory only | Spec requirement, simplest, best performance |

---

## Implementation Priorities

Based on research findings, suggested implementation order:

1. **Phase 1 - Foundation**:
   - Central node configuration and detection logic
   - MongoDB system register collection schema
   - Extend existing PeerListManager for local tracking

2. **Phase 2 - Connection Management**:
   - Central node connection manager with Polly resilience
   - gRPC heartbeat service
   - Failover logic (switch to next hub node)

3. **Phase 3 - Synchronization**:
   - System register sync service (server streaming pull)
   - Periodic sync service (5-minute timer)
   - Incremental sync based on version

4. **Phase 4 - Push Notifications**:
   - Notification subscription service
   - Push notification handler on hub nodes
   - Integration with blueprint publication

5. **Phase 5 - Testing & Observability**:
   - Integration tests
   - Performance tests
   - Logging and metrics

---

## Key Technical Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **NAT/Firewall blocking peer connections** | High - Prevents push notifications | Graceful fallback to periodic sync (5 min) |
| **MongoDB 16MB document limit** | Medium - Limits system register growth | Use collection pattern, not single document |
| **Thundering herd on hub node restart** | High - Overwhelms hub node | Polly jitter in retry backoff |
| **Stream connection failures** | Medium - Loss of push notifications | Automatic reconnection + periodic sync fallback |
| **Split-brain system registers** | High - Data inconsistency | Manual reconciliation procedure (documented) |
| **Heartbeat false positives** | Low - Unnecessary failovers | Grace period (2 missed heartbeats = 60s) |

---

## Dependencies & Prerequisites

**NuGet Packages** (already in project or compatible):
- Grpc.AspNetCore (2.71.0) - ✅ Already installed
- MongoDB.Driver (3.5.2) - ✅ Already installed
- Polly (8.6.5) - ✅ Already in Sorcha.Storage.Redis
- StackExchange.Redis (2.10.1) - ✅ Already installed (optional for future)

**Infrastructure**:
- MongoDB instance for system register storage
- DNS configuration for n0/n1/n2.sorcha.dev hostnames
- Network connectivity between peer nodes and hub nodes (gRPC port 5000)

**Existing Services to Integrate**:
- Register Service: Add `SystemRegisterService` for managing system register
- Peer Service: Extend with connection, heartbeat, and replication services

---

## Next Steps

1. ✅ Complete this research document
2. ⏭️ Proceed to Phase 1: Design & Contracts
   - Create `data-model.md` with entity definitions
   - Generate gRPC `.proto` contracts
   - Write `quickstart.md` for local development
3. ⏭️ Run `/speckit.tasks` to generate implementation tasks
4. ⏭️ Begin implementation following priority order

---

**Research Complete**: 2025-12-13
**Reviewers**: Architecture Team
**Status**: Ready for Phase 1 (Design & Contracts)
