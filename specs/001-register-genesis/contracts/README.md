# gRPC Protocol Buffer Contracts

**Feature**: Peer Service Central Node Connection
**Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Ready for Implementation

## Overview

This directory contains gRPC Protocol Buffer (proto3) contract definitions for the peer service central node connection feature. These contracts enable peer nodes to connect to central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev), synchronize the system register containing published blueprints, and maintain connection health.

## Proto Files

### 1. CentralNodeConnection.proto

**Service**: `CentralNodeConnection`
**Package**: `sorcha.peer.v1`
**Namespace**: `Sorcha.Peer.Service.Protos`

**Purpose**: Manages connection lifecycle between peer nodes and central nodes.

**RPCs**:
- `ConnectToCentralNode`: Establish connection to central node
- `DisconnectFromCentralNode`: Gracefully disconnect from central node
- `GetCentralNodeStatus`: Query central node health and status

**Key Messages**:
- `ConnectRequest`: Connection parameters with peer info
- `ConnectionResponse`: Connection confirmation with session ID
- `PeerInfo`: Peer capabilities and endpoint information
- `CentralNodeStatus`: Central node health and metrics

**Features**:
- 30-second connection timeout
- Exponential backoff retry: 1s, 2s, 4s, 8s, 16s, 32s, 60s
- Session-based connection tracking
- Failover support (n0 → n1 → n2 → n0)

---

### 2. SystemRegisterSync.proto

**Service**: `SystemRegisterSync`
**Package**: `sorcha.peer.v1`
**Namespace**: `Sorcha.Peer.Service.Protos`

**Purpose**: Synchronizes system register (published blueprints) between central nodes and peers using hybrid pull + push model.

**RPCs**:
- `FullSync`: Stream all blueprints (initial sync)
- `IncrementalSync`: Stream only new blueprints since last version (periodic sync)
- `SubscribeToPushNotifications`: Subscribe to real-time blueprint publication notifications (server streaming)
- `GetSyncCheckpoint`: Query current sync status

**Key Messages**:
- `SyncRequest`: Sync parameters with version checkpoint
- `SystemRegisterEntry`: Blueprint document with metadata
- `BlueprintNotification`: Real-time publication notification
- `SyncCheckpoint`: Sync progress tracking

**Synchronization Strategies**:
1. **Full Sync**: Initial connection, streams all active blueprints
2. **Incremental Sync**: Periodic (5 minutes), streams only new blueprints since last version
3. **Push Notifications**: Real-time, server streams notifications on blueprint publication

**Performance Goals**:
- Full sync: <60 seconds for 1000 blueprints
- Incremental sync: <30 seconds
- Push delivery: 80% of peers within 30 seconds

---

### 3. Heartbeat.proto

**Service**: `Heartbeat`
**Package**: `sorcha.peer.v1`
**Namespace**: `Sorcha.Peer.Service.Protos`

**Purpose**: Monitors connection health between peer and central node using periodic heartbeat messages.

**RPCs**:
- `SendHeartbeat`: Send unary heartbeat (recommended)
- `MonitorHeartbeat`: Bidirectional streaming heartbeat (advanced)
- `GetHeartbeatStatus`: Query heartbeat health status

**Key Messages**:
- `HeartbeatMessage`: Heartbeat with sequence number and sync version
- `HeartbeatAcknowledgement`: Acknowledgement with current system register version
- `HeartbeatStatus`: Connection health metrics
- `HealthMetrics`: Optional peer health data (CPU, memory, connections)

**Protocol**:
- Heartbeat interval: 30 seconds
- Heartbeat timeout: 30 seconds
- Failover trigger: 2 missed heartbeats (60 seconds total)

**Failover Logic**:
- 0 missed: Healthy
- 1 missed (30s): Warning, continue
- 2 missed (60s): Trigger failover to next central node
- All nodes unreachable: Enter "Isolated" mode

---

## Data Model Mapping

These proto contracts map to the following C# entities defined in [data-model.md](../data-model.md):

| Proto Message | C# Entity | Location |
|---------------|-----------|----------|
| `PeerInfo` | `CentralNodeInfo` | `Sorcha.Peer.Service.Core` |
| `SystemRegisterEntry` | `SystemRegisterEntry` | `Sorcha.Peer.Service.Core` |
| `HeartbeatMessage` | `HeartbeatMessage` | `Sorcha.Peer.Service.Core` |
| `HeartbeatAcknowledgement` | `HeartbeatAcknowledgement` | `Sorcha.Peer.Service.Core` |
| `SyncCheckpoint` | `SyncCheckpoint` | `Sorcha.Peer.Service.Core` |
| `BlueprintNotification` | `BlueprintNotification` | `Sorcha.Peer.Service.Core` |

## gRPC Code Generation

These proto files will be compiled into C# client and server stubs using the gRPC toolchain.

### Add to .csproj

```xml
<ItemGroup>
  <Protobuf Include="Protos\CentralNodeConnection.proto" GrpcServices="Both" />
  <Protobuf Include="Protos\SystemRegisterSync.proto" GrpcServices="Both" />
  <Protobuf Include="Protos\Heartbeat.proto" GrpcServices="Both" />
</ItemGroup>
```

### Generated Files

- `CentralNodeConnection.cs`: Client/server for connection management
- `CentralNodeConnectionGrpc.cs`: gRPC service base classes
- `SystemRegisterSync.cs`: Client/server for sync operations
- `SystemRegisterSyncGrpc.cs`: gRPC service base classes
- `Heartbeat.cs`: Client/server for heartbeat monitoring
- `HeartbeatGrpc.cs`: gRPC service base classes

## Implementation Guidelines

### Server Implementation (Central Nodes)

Central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev) implement all three services:

```csharp
// CentralNodeConnectionService.cs
public class CentralNodeConnectionService : CentralNodeConnection.CentralNodeConnectionBase
{
    public override async Task<ConnectionResponse> ConnectToCentralNode(
        ConnectRequest request, ServerCallContext context)
    {
        // Validate peer identity
        // Register peer session
        // Return connection metadata
    }
}

// SystemRegisterSyncService.cs
public class SystemRegisterSyncService : SystemRegisterSync.SystemRegisterSyncBase
{
    public override async Task FullSync(
        SyncRequest request, IServerStreamWriter<SystemRegisterEntry> responseStream,
        ServerCallContext context)
    {
        // Query MongoDB for all active blueprints
        // Stream each blueprint to peer
    }

    public override async Task SubscribeToPushNotifications(
        SubscriptionRequest request, IServerStreamWriter<BlueprintNotification> responseStream,
        ServerCallContext context)
    {
        // Register peer in subscriber list
        // Stream notifications on blueprint publication
    }
}

// HeartbeatService.cs
public class HeartbeatService : Heartbeat.HeartbeatBase
{
    public override async Task<HeartbeatAcknowledgement> SendHeartbeat(
        HeartbeatMessage request, ServerCallContext context)
    {
        // Record heartbeat timestamp
        // Check peer sync version lag
        // Return acknowledgement with recommended action
    }
}
```

### Client Implementation (Peer Nodes)

Peer nodes consume all three services as clients:

```csharp
// CentralNodeConnectionManager.cs
public class CentralNodeConnectionManager
{
    private readonly CentralNodeConnection.CentralNodeConnectionClient _client;

    public async Task<bool> ConnectAsync(string peerId)
    {
        var request = new ConnectRequest
        {
            PeerId = peerId,
            PeerInfo = new PeerInfo { /* ... */ },
            LastKnownVersion = _lastSyncVersion
        };

        var response = await _client.ConnectToCentralNodeAsync(request);
        return response.Success;
    }
}

// SystemRegisterSyncManager.cs
public class SystemRegisterSyncManager
{
    private readonly SystemRegisterSync.SystemRegisterSyncClient _client;

    public async Task PeriodicSyncAsync()
    {
        var request = new SyncRequest
        {
            PeerId = _peerId,
            LastKnownVersion = _checkpoint.CurrentVersion,
            FullSync = false
        };

        using var call = _client.IncrementalSync(request);
        await foreach (var entry in call.ResponseStream.ReadAllAsync())
        {
            await ProcessBlueprintAsync(entry);
        }
    }

    public async Task SubscribeToNotificationsAsync()
    {
        var subscription = new SubscriptionRequest { PeerId = _peerId };
        using var call = _client.SubscribeToPushNotifications(subscription);

        await foreach (var notification in call.ResponseStream.ReadAllAsync())
        {
            await HandleNotificationAsync(notification);
        }
    }
}

// HeartbeatMonitor.cs
public class HeartbeatMonitor : BackgroundService
{
    private readonly Heartbeat.HeartbeatClient _client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var heartbeat = new HeartbeatMessage
            {
                PeerId = _peerId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SequenceNumber = _sequence++,
                LastSyncVersion = _lastSyncVersion
            };

            var ack = await _client.SendHeartbeatAsync(heartbeat);

            if (ack.RecommendedAction == RecommendedAction.Sync)
            {
                await _syncManager.TriggerIncrementalSyncAsync();
            }
        }
    }
}
```

## Validation Rules

All proto messages include validation constraints in comments. Implement server-side validation:

### String Validation
- `peer_id`: MaxLength 64, pattern `^[a-zA-Z0-9\-_]+$`
- `blueprint_id`: MaxLength 255, pattern `^[a-zA-Z0-9\-_]+$`
- `register_id`: Must be `00000000-0000-0000-0000-000000000000`

### Numeric Validation
- `port`: Range 1-65535
- `cpu_usage_percent`: Range 0-100
- `disk_usage_percent`: Range 0-100
- `timestamp`: Within ±60 seconds of server time (clock skew tolerance)

### Size Validation
- `blueprint_document`: MaxSize 16MB (MongoDB limit)
- `blueprint_summary`: MaxSize 4KB
- `metadata`: Total size <4KB

## Error Handling

### gRPC Status Codes

Use appropriate gRPC status codes for errors:

| Status Code | Use Case |
|-------------|----------|
| `OK` | Successful operation |
| `CANCELLED` | Client cancelled request |
| `INVALID_ARGUMENT` | Validation error (peer_id, version, etc.) |
| `DEADLINE_EXCEEDED` | Connection timeout (30s) or heartbeat timeout |
| `NOT_FOUND` | Peer session not found |
| `ALREADY_EXISTS` | Peer already connected |
| `PERMISSION_DENIED` | Peer not authorized |
| `RESOURCE_EXHAUSTED` | Central node overloaded |
| `FAILED_PRECONDITION` | Peer not connected before heartbeat |
| `UNAVAILABLE` | Central node unavailable, try next node |
| `INTERNAL` | Unexpected server error |

### Retry Policy

Implement retry with Polly ResiliencePipeline:

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 10,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromMinutes(1),
        UseJitter = true
    })
    .AddTimeout(TimeSpan.FromSeconds(30))
    .Build();
```

## Testing

### Unit Tests

Test proto message serialization/deserialization:

```csharp
[Fact]
public void ConnectRequest_Serializes_Correctly()
{
    var request = new ConnectRequest
    {
        PeerId = "test-peer",
        LastKnownVersion = 100
    };

    var bytes = request.ToByteArray();
    var deserialized = ConnectRequest.Parser.ParseFrom(bytes);

    Assert.Equal("test-peer", deserialized.PeerId);
    Assert.Equal(100, deserialized.LastKnownVersion);
}
```

### Integration Tests

Test full RPC workflows with TestServer:

```csharp
[Fact]
public async Task ConnectToCentralNode_Returns_Success()
{
    // Arrange
    var channel = GrpcChannel.ForAddress("http://localhost:5000");
    var client = new CentralNodeConnection.CentralNodeConnectionClient(channel);

    // Act
    var response = await client.ConnectToCentralNodeAsync(new ConnectRequest
    {
        PeerId = "test-peer",
        PeerInfo = new PeerInfo { NodeType = "Peer" }
    });

    // Assert
    Assert.True(response.Success);
    Assert.NotEmpty(response.SessionId);
}
```

### Performance Tests

Test sync performance goals:

```csharp
[Fact]
public async Task FullSync_Completes_Within_60_Seconds_For_1000_Blueprints()
{
    var stopwatch = Stopwatch.StartNew();
    var count = 0;

    using var call = _client.FullSync(new SyncRequest { PeerId = "test" });
    await foreach (var entry in call.ResponseStream.ReadAllAsync())
    {
        count++;
    }

    stopwatch.Stop();
    Assert.Equal(1000, count);
    Assert.True(stopwatch.Elapsed.TotalSeconds < 60);
}
```

## Monitoring and Observability

### Metrics to Track

- Connection success/failure rate
- Heartbeat latency (average, p50, p95, p99)
- Sync duration and throughput
- Push notification delivery rate
- Failover frequency

### Logging

Use structured logging with correlation IDs:

```csharp
_logger.LogInformation(
    "Peer {PeerId} connected to central node {CentralNodeId} with session {SessionId}",
    peerId, centralNodeId, sessionId);

_logger.LogWarning(
    "Heartbeat timeout for peer {PeerId} - missed {MissedCount} consecutive heartbeats",
    peerId, missedCount);
```

## Security Considerations

### TLS/SSL

All gRPC connections MUST use TLS:

```csharp
var channel = GrpcChannel.ForAddress("https://n0.sorcha.dev:5000", new GrpcChannelOptions
{
    Credentials = ChannelCredentials.SecureSsl
});
```

### Authentication

Implement metadata-based authentication:

```csharp
var metadata = new Metadata
{
    { "peer-id", peerId },
    { "authorization", $"Bearer {authToken}" }
};

var response = await _client.ConnectToCentralNodeAsync(request, metadata);
```

### Rate Limiting

Central nodes should implement rate limiting per peer:

- Max 1 connection attempt per second per peer
- Max 1 heartbeat per 30 seconds per peer
- Max 1 sync request per minute per peer (excluding incremental)

## References

- [Data Model](../data-model.md): Entity definitions and validation rules
- [Research](../research.md): Technical decisions and rationale
- [Google Protocol Buffers Style Guide](https://protobuf.dev/programming-guides/style/)
- [gRPC C# Documentation](https://grpc.io/docs/languages/csharp/)
- [.NET 10 gRPC Services](https://learn.microsoft.com/aspnet/core/grpc/)

## Next Steps

1. Copy proto files to `src/Services/Sorcha.Peer.Service/Protos/`
2. Add `<Protobuf>` entries to `Sorcha.Peer.Service.csproj`
3. Build project to generate C# client/server code
4. Implement service base classes (central nodes)
5. Implement client managers (peer nodes)
6. Write unit and integration tests
7. Deploy to staging environment
8. Performance test with 1000+ blueprints

---

**Status**: Ready for Implementation
**Last Updated**: 2025-12-13
**Version**: 1.0
