# Data Model: Peer Service Central Node Connection

**Feature Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Complete
**Purpose**: Comprehensive data model documentation for peer service central node connection, system register replication, heartbeat monitoring, and connection management

---

## Overview

This document defines all entities, relationships, validation rules, state machines, storage schemas, and C# class definitions for the peer service central node connection feature. The implementation enables peer nodes to connect to central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev), synchronize the system register containing published blueprints, and maintain connection health through heartbeat monitoring.

**Technology Stack**:
- .NET 10 / C# 13
- MongoDB 3.5.2 (system register storage)
- gRPC 2.71.0 (peer communication)
- Polly 8.6.5 (resilience policies)

---

## 1. Entities

### 1.1 CentralNodeInfo

Represents configuration and runtime state for a central node endpoint.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `NodeId` | `string` | Yes | Unique identifier for the central node | Pattern: `^n[0-2]\.sorcha\.dev$` |
| `Hostname` | `string` | Yes | DNS hostname of central node | Must match `n0.sorcha.dev`, `n1.sorcha.dev`, or `n2.sorcha.dev` |
| `Port` | `int` | Yes | gRPC port for peer connections | Range: 1-65535, Default: 5000 |
| `Priority` | `int` | Yes | Connection priority (0 = highest) | Range: 0-2, 0=n0, 1=n1, 2=n2 |
| `ConnectionStatus` | `CentralNodeConnectionStatus` | Yes | Current connection state | Enum: Disconnected, Connecting, Connected, Failed, HeartbeatTimeout |
| `LastConnectionAttempt` | `DateTime?` | No | Timestamp of last connection attempt | UTC timezone |
| `LastSuccessfulConnection` | `DateTime?` | No | Timestamp of last successful connection | UTC timezone |
| `LastHeartbeatSent` | `DateTime?` | No | Timestamp of last heartbeat sent | UTC timezone |
| `LastHeartbeatAcknowledged` | `DateTime?` | No | Timestamp of last heartbeat acknowledged | UTC timezone |
| `ConsecutiveFailures` | `int` | Yes | Number of consecutive connection failures | Default: 0, Reset on success |
| `IsActive` | `bool` | Yes | Whether this is the actively connected central node | Only one can be true at a time |
| `GrpcChannelAddress` | `string` | Yes | Computed gRPC channel address | Format: `https://{Hostname}:{Port}` |

**Business Rules**:
- Central node hostnames MUST match `n0.sorcha.dev`, `n1.sorcha.dev`, or `n2.sorcha.dev` pattern (FR-026)
- Only one central node can have `IsActive = true` at any time (FR-035)
- Priority determines connection order: 0 (n0) → 1 (n1) → 2 (n2) → 0 (wrap around)
- `ConsecutiveFailures` resets to 0 on successful connection
- Connection timeout: 30 seconds (FR-036)

---

### 1.2 SystemRegisterEntry

Represents a single blueprint document stored in the system register MongoDB collection.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `BlueprintId` | `string` | Yes | Unique identifier for blueprint (MongoDB `_id`) | MaxLength: 255, Pattern: `^[a-zA-Z0-9\-_]+$` |
| `RegisterId` | `Guid` | Yes | System register identifier (well-known constant) | Must be `00000000-0000-0000-0000-000000000000` |
| `Document` | `BsonDocument` | Yes | Blueprint JSON document | Valid JSON, MaxSize: 16MB |
| `PublishedAt` | `DateTime` | Yes | Timestamp when blueprint was published | UTC timezone, Indexed |
| `PublishedBy` | `string` | Yes | Identity of publisher (user/system) | MaxLength: 255 |
| `Version` | `long` | Yes | Incrementing version number for sync | Auto-increment, Indexed, Unique |
| `IsActive` | `bool` | Yes | Whether blueprint is active/available | Default: true, Indexed |
| `PublicationTransactionId` | `string?` | No | Link to register transaction that published this | Optional link to audit trail |
| `Checksum` | `string?` | No | SHA-256 checksum of Document for integrity | Computed on save |
| `Metadata` | `Dictionary<string, string>?` | No | Optional metadata key-value pairs | MaxSize: 4KB |

**Business Rules**:
- `BlueprintId` is the MongoDB `_id` field (unique primary key)
- `Version` must be monotonically increasing (auto-increment by MongoDB collection)
- `RegisterId` must always be the well-known system register ID (FR-016)
- Blueprints are immutable once published (no updates, only new versions)
- `IsActive = false` indicates deprecated/withdrawn blueprints
- `Document` must contain valid blueprint JSON-LD schema

**Indexes**:
```csharp
// Ascending index on Version for incremental sync queries
collection.Indexes.CreateOne(
    new CreateIndexModel<SystemRegisterEntry>(
        Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.Version)));

// Descending index on PublishedAt for recent blueprints query
collection.Indexes.CreateOne(
    new CreateIndexModel<SystemRegisterEntry>(
        Builders<SystemRegisterEntry>.IndexKeys.Descending(x => x.PublishedAt)));

// Index on IsActive for active blueprints query
collection.Indexes.CreateOne(
    new CreateIndexModel<SystemRegisterEntry>(
        Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.IsActive)));
```

---

### 1.3 HeartbeatMessage

Represents a heartbeat message sent from peer to central node or vice versa.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `PeerId` | `string` | Yes | Unique identifier of peer sending heartbeat | MaxLength: 64 |
| `Timestamp` | `long` | Yes | Unix timestamp when heartbeat was sent | Milliseconds since epoch (UTC) |
| `SequenceNumber` | `long` | Yes | Monotonically increasing sequence number | Auto-increment per peer |
| `LastSyncVersion` | `long` | Yes | Last known system register version | Used to detect peer lag |
| `ActiveConnections` | `int` | No | Number of active peer connections | Optional metric |
| `CpuUsagePercent` | `double` | No | CPU usage percentage | Optional metric, Range: 0-100 |
| `MemoryUsageMb` | `double` | No | Memory usage in megabytes | Optional metric |
| `NodeType` | `string` | Yes | Type of node (Central, Peer) | Enum: "Central", "Peer" |

**Business Rules**:
- Heartbeat must be sent every 30 seconds (FR-036)
- Heartbeat timeout: 30 seconds (no response = connection failure)
- `SequenceNumber` must increment per heartbeat from same peer
- `Timestamp` must be within ±60 seconds of receiver's current time (clock skew tolerance)
- Metrics (`ActiveConnections`, `CpuUsagePercent`, `MemoryUsageMb`) are optional

---

### 1.4 HeartbeatAcknowledgement

Represents acknowledgement response to a heartbeat message.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `Success` | `bool` | Yes | Whether heartbeat was successfully processed | - |
| `Timestamp` | `long` | Yes | Unix timestamp when acknowledgement sent | Milliseconds since epoch (UTC) |
| `CentralNodeId` | `string` | Yes | Identifier of responding central node | MaxLength: 64 |
| `CurrentSystemRegisterVersion` | `long` | Yes | Current system register version on central node | Used by peer to detect lag |
| `Message` | `string?` | No | Optional status message | MaxLength: 500 |

**Business Rules**:
- Must be sent within 30 seconds of receiving heartbeat (FR-036)
- `CurrentSystemRegisterVersion` allows peer to detect if it's behind and trigger incremental sync
- If `Success = false`, connection should be considered unhealthy

---

### 1.5 ActivePeerInfo

Represents local peer connection status information (stored in memory, not MongoDB).

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `PeerId` | `string` | Yes | Unique identifier for this peer | MaxLength: 64 |
| `ConnectedCentralNodeId` | `string?` | No | ID of connected central node (null if disconnected) | MaxLength: 64 |
| `ConnectionEstablished` | `DateTime` | Yes | When connection was established | UTC timezone |
| `LastHeartbeat` | `DateTime` | Yes | Last heartbeat sent or received | UTC timezone |
| `LastSyncVersion` | `long` | Yes | Last synchronized system register version | Default: 0 |
| `Status` | `PeerConnectionStatus` | Yes | Current connection status | Enum |
| `HeartbeatSequence` | `long` | Yes | Current heartbeat sequence number | Auto-increment |
| `MissedHeartbeats` | `int` | Yes | Consecutive missed heartbeats | Reset on successful heartbeat |

**Business Rules**:
- Each peer node maintains its own local `ActivePeerInfo` (FR-037)
- `ConnectedCentralNodeId` is null when `Status = Disconnected` or `Status = Isolated`
- `MissedHeartbeats >= 2` (60 seconds total) triggers failover to next central node
- This is local in-memory state, not shared across peers or stored in database

---

### 1.6 SyncCheckpoint

Represents a synchronization checkpoint for tracking incremental sync progress.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `PeerId` | `string` | Yes | Peer that owns this checkpoint | MaxLength: 64 |
| `CurrentVersion` | `long` | Yes | Last synchronized system register version | Monotonically increasing |
| `LastSyncTime` | `long` | Yes | Unix timestamp of last successful sync | Milliseconds since epoch (UTC) |
| `TotalBlueprints` | `int` | Yes | Total number of blueprints in local replica | Count of active blueprints |
| `CentralNodeId` | `string` | Yes | Central node this checkpoint is for | MaxLength: 64 |
| `NextSyncDue` | `DateTime` | Yes | When next periodic sync is due | UTC, Default: Now + 5 minutes |

**Business Rules**:
- Periodic sync interval: 5 minutes (FR-032)
- `CurrentVersion` used for incremental sync: `SELECT * FROM system_register WHERE version > CurrentVersion`
- Checkpoint persisted locally (file or in-memory) to survive restarts
- Checkpoint reset when connecting to different central node

---

### 1.7 BlueprintNotification

Represents a push notification for new blueprint publication.

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `BlueprintId` | `string` | Yes | Identifier of newly published blueprint | MaxLength: 255 |
| `Version` | `long` | Yes | System register version of this blueprint | Unique |
| `PublishedAt` | `long` | Yes | Unix timestamp of publication | Milliseconds since epoch (UTC) |
| `PublishedBy` | `string` | Yes | Identity of publisher | MaxLength: 255 |
| `BlueprintSummary` | `byte[]?` | No | Optional small metadata preview | MaxSize: 4KB |
| `Type` | `NotificationType` | Yes | Type of notification | Enum: BlueprintPublished, BlueprintUpdated, BlueprintDeprecated |

**Business Rules**:
- Push notifications sent immediately on blueprint publication (FR-033)
- Best-effort delivery: 80% of peers within 30 seconds (SC-016)
- Remaining peers get update via 5-minute periodic sync (FR-032)
- `BlueprintSummary` is optional metadata; peer fetches full document via sync if needed

---

## 2. Enumerations

### 2.1 CentralNodeConnectionStatus

```csharp
/// <summary>
/// Connection status for a central node
/// </summary>
public enum CentralNodeConnectionStatus
{
    /// <summary>
    /// Not connected to this central node
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Connection attempt in progress
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Successfully connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connection attempt failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Connected but heartbeat timeout occurred
    /// </summary>
    HeartbeatTimeout = 4
}
```

### 2.2 PeerConnectionStatus

```csharp
/// <summary>
/// Overall peer connection status
/// </summary>
public enum PeerConnectionStatus
{
    /// <summary>
    /// Peer is disconnected from all central nodes
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Peer is attempting to connect to a central node
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Peer is connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Heartbeat timeout detected, attempting failover
    /// </summary>
    HeartbeatTimeout = 3,

    /// <summary>
    /// Operating without central node connection (using last known replica)
    /// </summary>
    Isolated = 4
}
```

### 2.3 NotificationType

```csharp
/// <summary>
/// Type of blueprint notification
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// New blueprint published
    /// </summary>
    BlueprintPublished = 0,

    /// <summary>
    /// Existing blueprint updated (new version)
    /// </summary>
    BlueprintUpdated = 1,

    /// <summary>
    /// Blueprint deprecated/withdrawn
    /// </summary>
    BlueprintDeprecated = 2
}
```

---

## 3. Relationships

### 3.1 Entity Relationship Diagram

```
┌─────────────────────────┐
│  CentralNodeInfo        │ 1
│  (Configuration)        ├─────────┐
└─────────────────────────┘         │
                                    │ Active Connection
                                    │ (0..1 active at a time)
┌─────────────────────────┐         │
│  ActivePeerInfo         │ 1       │
│  (Local State)          │◄────────┘
└──────────┬──────────────┘
           │
           │ Tracks
           │ (1..1)
           │
           ▼
┌─────────────────────────┐
│  SyncCheckpoint         │
│  (Sync Progress)        │
└─────────────────────────┘


┌─────────────────────────┐
│  SystemRegisterEntry    │ *
│  (MongoDB Collection)   ├─────────┐
└─────────────────────────┘         │
                                    │ Contains
                                    │ (0..*)
                                    │
                                    ▼
                          ┌─────────────────────────┐
                          │  BlueprintNotification  │
                          │  (Push Event)           │
                          └─────────────────────────┘


┌─────────────────────────┐         ┌─────────────────────────┐
│  HeartbeatMessage       │ 1     * │ HeartbeatAcknowledgement│
│  (Request)              ├────────►│ (Response)              │
└─────────────────────────┘         └─────────────────────────┘
```

### 3.2 Cardinality

| Entity A | Relationship | Entity B | Cardinality |
|----------|--------------|----------|-------------|
| `ActivePeerInfo` | actively connected to | `CentralNodeInfo` | 0..1 (one active at a time) |
| `ActivePeerInfo` | tracks sync via | `SyncCheckpoint` | 1..1 (one checkpoint per peer) |
| `SystemRegisterEntry` | triggers | `BlueprintNotification` | 1..* (one notification per connected peer) |
| `HeartbeatMessage` | receives | `HeartbeatAcknowledgement` | 1..1 (one ack per heartbeat) |
| `CentralNodeInfo` | configured in | `PeerServiceConfiguration` | 1..3 (n0, n1, n2) |

---

## 4. Validation Rules

### 4.1 Central Node Hostname Validation

**Rule**: Central node hostnames MUST match `n0.sorcha.dev`, `n1.sorcha.dev`, or `n2.sorcha.dev` pattern.

**Validation Logic**:
```csharp
public static class CentralNodeValidator
{
    private static readonly string[] ValidHostnames = { "n0.sorcha.dev", "n1.sorcha.dev", "n2.sorcha.dev" };
    private static readonly Regex HostnamePattern = new Regex(@"^n[0-2]\.sorcha\.dev$", RegexOptions.Compiled);

    public static bool IsValidCentralNodeHostname(string hostname)
    {
        return HostnamePattern.IsMatch(hostname);
    }

    public static void ValidateHostname(string hostname)
    {
        if (!IsValidCentralNodeHostname(hostname))
        {
            throw new ArgumentException(
                $"Invalid central node hostname: '{hostname}'. Must match pattern: n0.sorcha.dev, n1.sorcha.dev, or n2.sorcha.dev",
                nameof(hostname));
        }
    }
}
```

**Applied to**: `CentralNodeInfo.Hostname`

---

### 4.2 Heartbeat Timeout Validation

**Rule**: Heartbeat timeout MUST be 30 seconds (FR-036).

**Validation Logic**:
```csharp
public static class HeartbeatValidator
{
    public const int HeartbeatIntervalSeconds = 30;
    public const int HeartbeatTimeoutSeconds = 30;
    public const int MaxMissedHeartbeats = 2; // 60 seconds total

    public static bool IsHeartbeatTimedOut(DateTime lastHeartbeat)
    {
        var elapsed = DateTime.UtcNow - lastHeartbeat;
        return elapsed.TotalSeconds > HeartbeatTimeoutSeconds;
    }

    public static bool ShouldFailover(int missedHeartbeats)
    {
        return missedHeartbeats >= MaxMissedHeartbeats;
    }
}
```

**Applied to**: `HeartbeatMessage`, `HeartbeatAcknowledgement`, `ActivePeerInfo.MissedHeartbeats`

---

### 4.3 Periodic Sync Interval Validation

**Rule**: Periodic sync interval MUST be 5 minutes (FR-032).

**Validation Logic**:
```csharp
public static class SyncValidator
{
    public const int PeriodicSyncIntervalMinutes = 5;
    public const int PeriodicSyncIntervalSeconds = 300;

    public static DateTime CalculateNextSyncTime(DateTime lastSyncTime)
    {
        return lastSyncTime.AddMinutes(PeriodicSyncIntervalMinutes);
    }

    public static bool IsSyncDue(DateTime nextSyncDue)
    {
        return DateTime.UtcNow >= nextSyncDue;
    }
}
```

**Applied to**: `SyncCheckpoint.NextSyncDue`

---

### 4.4 Connection Retry Backoff Validation

**Rule**: Connection retry backoff MUST follow 1s, 2s, 4s, 8s, 16s, 32s, 60s sequence (exponential with 60s cap).

**Validation Logic**:
```csharp
public static class RetryBackoffValidator
{
    public const int InitialDelaySeconds = 1;
    public const int MaxDelaySeconds = 60;
    public const double Multiplier = 2.0;
    public const int MaxRetryAttempts = 10;

    public static TimeSpan CalculateBackoff(int attemptNumber)
    {
        if (attemptNumber <= 0)
            return TimeSpan.Zero;

        var delaySeconds = Math.Min(
            InitialDelaySeconds * Math.Pow(Multiplier, attemptNumber - 1),
            MaxDelaySeconds);

        return TimeSpan.FromSeconds(delaySeconds);
    }

    // Expected sequence: 1s, 2s, 4s, 8s, 16s, 32s, 60s, 60s, 60s, 60s
    public static readonly TimeSpan[] ExpectedBackoffSequence = new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16),
        TimeSpan.FromSeconds(32),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60)
    };
}
```

**Applied to**: `CentralNodeConnectionManager` retry logic (implemented via Polly ResiliencePipeline)

---

### 4.5 System Register ID Validation

**Rule**: System register ID MUST be the well-known constant `00000000-0000-0000-0000-000000000000`.

**Validation Logic**:
```csharp
public static class SystemRegisterValidator
{
    public static readonly Guid SystemRegisterId = Guid.Empty; // 00000000-0000-0000-0000-000000000000

    public static bool IsSystemRegister(Guid registerId)
    {
        return registerId == SystemRegisterId;
    }

    public static void ValidateSystemRegisterId(Guid registerId)
    {
        if (registerId != SystemRegisterId)
        {
            throw new ArgumentException(
                $"Invalid system register ID: {registerId}. Must be {SystemRegisterId}",
                nameof(registerId));
        }
    }
}
```

**Applied to**: `SystemRegisterEntry.RegisterId`

---

## 5. State Machines

### 5.1 Central Node Connection Lifecycle

```
┌───────────────┐
│  Disconnected │ ◄─────────────────────────────────┐
└───────┬───────┘                                   │
        │                                           │
        │ Connect() called                          │
        │                                           │
        ▼                                           │
┌───────────────┐                                   │
│  Connecting   │                                   │
└───────┬───────┘                                   │
        │                                           │
        ├─► Connection Success ──────► ┌──────────┐│
        │                              │ Connected││
        │                              └─────┬────┘│
        │                                    │     │
        │                                    │     │
        │                         ┌──────────▼─────┴────────┐
        │                         │ Heartbeat Timeout       │
        │                         │ (2 missed heartbeats)   │
        │                         └──────────┬──────────────┘
        │                                    │
        │                                    ▼
        │                         ┌─────────────────┐
        └─► Connection Failed ───►│ HeartbeatTimeout│
                                  └─────────┬───────┘
                                            │
                                            │ Failover to Next Central Node
                                            │
                                            ▼
                                  ┌───────────────┐
                                  │ Connecting    │
                                  │ (Next Node)   │
                                  └───────────────┘
```

**State Transitions**:

| From State | Event | To State | Action |
|------------|-------|----------|--------|
| `Disconnected` | `Connect()` | `Connecting` | Initialize gRPC channel, start connection attempt |
| `Connecting` | `ConnectionSuccess` | `Connected` | Start heartbeat timer, update `IsActive=true` |
| `Connecting` | `ConnectionFailed` | `Disconnected` | Increment `ConsecutiveFailures`, retry with backoff |
| `Connected` | `HeartbeatTimeout` | `HeartbeatTimeout` | Stop heartbeat timer, increment `MissedHeartbeats` |
| `HeartbeatTimeout` | `MissedHeartbeats >= 2` | `Disconnected` | Trigger failover to next central node |
| `HeartbeatTimeout` | `HeartbeatAcknowledged` | `Connected` | Reset `MissedHeartbeats`, resume normal operation |

---

### 5.2 Peer Connection Status Lifecycle

```
┌───────────────┐
│ Disconnected  │ ◄─────────────────────────────────┐
└───────┬───────┘                                   │
        │                                           │
        │ Startup / Connect to Central Nodes       │
        │                                           │
        ▼                                           │
┌───────────────┐                                   │
│  Connecting   │                                   │
└───────┬───────┘                                   │
        │                                           │
        ├─► Connection to any central node ────► ┌─┴────────┐
        │                                         │ Connected│
        │                                         └─────┬────┘
        │                                               │
        │                                               │
        │                                               │
        │                                 ┌─────────────▼──────────────┐
        │                                 │ Heartbeat Timeout          │
        │                                 │ (Attempting failover)      │
        │                                 └─────────────┬──────────────┘
        │                                               │
        │                                               │ All central nodes unreachable
        │                                               │
        └─► All central nodes unreachable ────► ┌──────▼─────┐
                                                 │  Isolated  │
                                                 └──────┬─────┘
                                                        │
                                                        │ Central node becomes reachable
                                                        │
                                                        ▼
                                                 ┌────────────┐
                                                 │ Connecting │
                                                 └────────────┘
```

**State Transitions**:

| From State | Event | To State | Action |
|------------|-------|----------|--------|
| `Disconnected` | `ServiceStartup` | `Connecting` | Try to connect to first central node (n0) |
| `Connecting` | `ConnectionSuccess` | `Connected` | Update `ConnectedCentralNodeId`, start sync/heartbeat |
| `Connecting` | `AllNodesUnreachable` | `Isolated` | Operate with last known system register replica |
| `Connected` | `HeartbeatTimeout` | `HeartbeatTimeout` | Attempt failover to next central node |
| `HeartbeatTimeout` | `FailoverSuccess` | `Connected` | Connected to next central node |
| `HeartbeatTimeout` | `FailoverFailed` | `Isolated` | All central nodes unreachable |
| `Isolated` | `CentralNodeReachable` | `Connecting` | Attempt connection to reachable central node |

---

### 5.3 System Register Sync State Machine

```
┌─────────────┐
│    Idle     │ ◄─────────────────────────────────┐
└──────┬──────┘                                   │
       │                                          │
       │ NextSyncDue OR Push Notification         │
       │                                          │
       ▼                                          │
┌─────────────┐                                   │
│   Syncing   │                                   │
└──────┬──────┘                                   │
       │                                          │
       ├─► Sync Success ──────────────────► ┌────┴──────┐
       │                                    │   Idle    │
       │                                    └───────────┘
       │
       └─► Sync Failed ────► ┌──────────────┐
                             │ Retry (Backoff)│
                             └────────┬───────┘
                                      │
                                      │ Max retries exceeded
                                      │
                                      ▼
                             ┌──────────────┐
                             │ Sync Error   │
                             └──────┬───────┘
                                    │
                                    │ Wait for next periodic sync
                                    │
                                    ▼
                             ┌──────────────┐
                             │    Idle      │
                             └──────────────┘
```

**State Transitions**:

| From State | Event | To State | Action |
|------------|-------|----------|--------|
| `Idle` | `NextSyncDue` | `Syncing` | Start periodic sync (5-minute timer) |
| `Idle` | `PushNotification` | `Syncing` | Start incremental sync for new blueprint |
| `Syncing` | `SyncSuccess` | `Idle` | Update `SyncCheckpoint`, reset retry count |
| `Syncing` | `SyncFailed` | `Retry` | Apply exponential backoff, retry |
| `Retry` | `MaxRetriesExceeded` | `SyncError` | Log error, wait for next periodic sync |
| `SyncError` | `NextSyncDue` | `Syncing` | Attempt periodic sync again |

---

## 6. Storage Schema

### 6.1 MongoDB System Register Collection

**Collection Name**: `sorcha_system_register_blueprints`

**Database**: Shared with Register Service MongoDB instance

**Schema Definition**:
```javascript
{
  "_id": "register-creation-v1",                // string (BlueprintId)
  "registerId": "00000000-0000-0000-0000-000000000000", // UUID (well-known constant)
  "document": {                                  // BsonDocument (Blueprint JSON)
    "@context": "https://sorcha.dev/blueprints/v1",
    "id": "register-creation-v1",
    "name": "Register Creation Workflow",
    "version": "1.0.0",
    "actions": [...]
  },
  "publishedAt": ISODate("2025-12-13T12:00:00.000Z"), // DateTime (indexed)
  "publishedBy": "system",                       // string
  "version": 1,                                  // long (auto-increment, indexed)
  "isActive": true,                              // bool (indexed)
  "publicationTransactionId": "tx-12345",        // string (optional)
  "checksum": "sha256:abcdef...",                // string (optional)
  "metadata": {                                  // object (optional)
    "category": "workflow",
    "tags": ["register", "creation"]
  }
}
```

**Indexes**:
```javascript
db.sorcha_system_register_blueprints.createIndex({ "version": 1 });        // Ascending for incremental sync
db.sorcha_system_register_blueprints.createIndex({ "publishedAt": -1 });   // Descending for recent queries
db.sorcha_system_register_blueprints.createIndex({ "isActive": 1 });       // Filter active blueprints
db.sorcha_system_register_blueprints.createIndex({ "blueprintId": 1 }, { unique: true }); // Unique constraint
```

**Query Patterns**:

1. **Get all active blueprints**:
```javascript
db.sorcha_system_register_blueprints.find({ isActive: true });
```

2. **Incremental sync (get blueprints since version N)**:
```javascript
db.sorcha_system_register_blueprints.find({ version: { $gt: 100 } }).sort({ version: 1 });
```

3. **Get specific blueprint**:
```javascript
db.sorcha_system_register_blueprints.findOne({ _id: "register-creation-v1" });
```

4. **Get recent blueprints**:
```javascript
db.sorcha_system_register_blueprints.find({ isActive: true }).sort({ publishedAt: -1 }).limit(10);
```

---

### 6.2 Local Sync Checkpoint Storage

**Storage**: Local file persistence (JSON) or in-memory

**File Path**: `./data/sync_checkpoint.json`

**Schema**:
```json
{
  "peerId": "peer-abc123",
  "currentVersion": 142,
  "lastSyncTime": 1702474800000,
  "totalBlueprints": 25,
  "centralNodeId": "n0.sorcha.dev",
  "nextSyncDue": "2025-12-13T12:05:00.000Z"
}
```

**Persistence Strategy**:
- Write checkpoint after each successful sync
- Load checkpoint on service startup
- Reset checkpoint when connecting to different central node
- Default to version 0 if checkpoint file missing

---

### 6.3 Active Peer Info Storage

**Storage**: In-memory only (not persisted)

**Structure**: `ConcurrentDictionary<string, ActivePeerInfo>`

**Key**: `PeerId` (string)

**Value**: `ActivePeerInfo` object

**Lifetime**: Reset on service restart

**Thread Safety**: Uses `ConcurrentDictionary` for thread-safe access

---

## 7. C# Class Definitions

### 7.1 CentralNodeInfo.cs

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration and runtime state for a central node endpoint
/// </summary>
public class CentralNodeInfo
{
    private static readonly Regex HostnamePattern = new(@"^n[0-2]\.sorcha\.dev$", RegexOptions.Compiled);

    /// <summary>
    /// Unique identifier for the central node (matches hostname)
    /// </summary>
    [Required]
    [MaxLength(64)]
    [RegularExpression(@"^n[0-2]\.sorcha\.dev$", ErrorMessage = "Central node hostname must match pattern: n0.sorcha.dev, n1.sorcha.dev, or n2.sorcha.dev")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// DNS hostname of central node (must be n0/n1/n2.sorcha.dev)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// gRPC port for peer connections
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Connection priority (0 = highest, try first)
    /// </summary>
    [Required]
    [Range(0, 2)]
    public int Priority { get; set; }

    /// <summary>
    /// Current connection state
    /// </summary>
    [Required]
    public CentralNodeConnectionStatus ConnectionStatus { get; set; } = CentralNodeConnectionStatus.Disconnected;

    /// <summary>
    /// Timestamp of last connection attempt
    /// </summary>
    public DateTime? LastConnectionAttempt { get; set; }

    /// <summary>
    /// Timestamp of last successful connection
    /// </summary>
    public DateTime? LastSuccessfulConnection { get; set; }

    /// <summary>
    /// Timestamp of last heartbeat sent
    /// </summary>
    public DateTime? LastHeartbeatSent { get; set; }

    /// <summary>
    /// Timestamp of last heartbeat acknowledged by central node
    /// </summary>
    public DateTime? LastHeartbeatAcknowledged { get; set; }

    /// <summary>
    /// Number of consecutive connection failures
    /// </summary>
    public int ConsecutiveFailures { get; set; } = 0;

    /// <summary>
    /// Whether this is the actively connected central node (only one can be true)
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Computed gRPC channel address
    /// </summary>
    public string GrpcChannelAddress => $"https://{Hostname}:{Port}";

    /// <summary>
    /// Validates central node hostname pattern
    /// </summary>
    public static bool IsValidHostname(string hostname)
    {
        return HostnamePattern.IsMatch(hostname);
    }

    /// <summary>
    /// Resets connection state (called on successful connection)
    /// </summary>
    public void ResetConnectionState()
    {
        ConsecutiveFailures = 0;
        ConnectionStatus = CentralNodeConnectionStatus.Connected;
        LastSuccessfulConnection = DateTime.UtcNow;
    }

    /// <summary>
    /// Records connection failure
    /// </summary>
    public void RecordFailure()
    {
        ConsecutiveFailures++;
        ConnectionStatus = CentralNodeConnectionStatus.Failed;
        LastConnectionAttempt = DateTime.UtcNow;
    }
}
```

---

### 7.2 SystemRegisterEntry.cs

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Represents a blueprint document in the system register MongoDB collection
/// </summary>
public class SystemRegisterEntry
{
    /// <summary>
    /// Unique blueprint identifier (MongoDB _id)
    /// </summary>
    [BsonId]
    [Required]
    [MaxLength(255)]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Blueprint ID must contain only alphanumeric characters, hyphens, and underscores")]
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// System register identifier (well-known constant: 00000000-0000-0000-0000-000000000000)
    /// </summary>
    [BsonElement("registerId")]
    [Required]
    public Guid RegisterId { get; set; } = Guid.Empty;

    /// <summary>
    /// Blueprint JSON document
    /// </summary>
    [BsonElement("document")]
    [Required]
    public BsonDocument Document { get; set; } = new();

    /// <summary>
    /// Timestamp when blueprint was published (UTC)
    /// </summary>
    [BsonElement("publishedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Required]
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identity of publisher (user ID or "system")
    /// </summary>
    [BsonElement("publishedBy")]
    [Required]
    [MaxLength(255)]
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Incrementing version number for sync (auto-increment)
    /// </summary>
    [BsonElement("version")]
    [Required]
    public long Version { get; set; }

    /// <summary>
    /// Whether blueprint is active/available
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Link to register transaction that published this blueprint (optional)
    /// </summary>
    [BsonElement("publicationTransactionId")]
    [BsonIgnoreIfNull]
    public string? PublicationTransactionId { get; set; }

    /// <summary>
    /// SHA-256 checksum of Document for integrity verification (optional)
    /// </summary>
    [BsonElement("checksum")]
    [BsonIgnoreIfNull]
    public string? Checksum { get; set; }

    /// <summary>
    /// Optional metadata key-value pairs
    /// </summary>
    [BsonElement("metadata")]
    [BsonIgnoreIfNull]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Validates that RegisterId is the well-known system register ID
    /// </summary>
    public void ValidateSystemRegister()
    {
        if (RegisterId != Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Invalid system register ID: {RegisterId}. Must be {Guid.Empty}");
        }
    }
}
```

---

### 7.3 HeartbeatMessage.cs

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Heartbeat message sent from peer to central node
/// </summary>
public class HeartbeatMessage
{
    /// <summary>
    /// Unique identifier of peer sending heartbeat
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp (milliseconds) when heartbeat was sent
    /// </summary>
    [Required]
    public long Timestamp { get; set; }

    /// <summary>
    /// Monotonically increasing sequence number
    /// </summary>
    [Required]
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Last known system register version
    /// </summary>
    [Required]
    public long LastSyncVersion { get; set; }

    /// <summary>
    /// Number of active peer connections (optional metric)
    /// </summary>
    public int? ActiveConnections { get; set; }

    /// <summary>
    /// CPU usage percentage (optional metric)
    /// </summary>
    [Range(0, 100)]
    public double? CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage in megabytes (optional metric)
    /// </summary>
    public double? MemoryUsageMb { get; set; }

    /// <summary>
    /// Type of node sending heartbeat
    /// </summary>
    [Required]
    public string NodeType { get; set; } = "Peer";

    /// <summary>
    /// Creates a heartbeat message with current timestamp
    /// </summary>
    public static HeartbeatMessage Create(string peerId, long sequenceNumber, long lastSyncVersion)
    {
        return new HeartbeatMessage
        {
            PeerId = peerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SequenceNumber = sequenceNumber,
            LastSyncVersion = lastSyncVersion,
            NodeType = "Peer"
        };
    }
}
```

---

### 7.4 ActivePeerInfo.cs

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Local peer connection status information (in-memory only)
/// </summary>
public class ActivePeerInfo
{
    /// <summary>
    /// Unique identifier for this peer
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// ID of connected central node (null if disconnected)
    /// </summary>
    [MaxLength(64)]
    public string? ConnectedCentralNodeId { get; set; }

    /// <summary>
    /// When connection was established (UTC)
    /// </summary>
    public DateTime ConnectionEstablished { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last heartbeat sent or received (UTC)
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last synchronized system register version
    /// </summary>
    public long LastSyncVersion { get; set; } = 0;

    /// <summary>
    /// Current connection status
    /// </summary>
    [Required]
    public PeerConnectionStatus Status { get; set; } = PeerConnectionStatus.Disconnected;

    /// <summary>
    /// Current heartbeat sequence number
    /// </summary>
    public long HeartbeatSequence { get; set; } = 0;

    /// <summary>
    /// Consecutive missed heartbeats (reset on success)
    /// </summary>
    public int MissedHeartbeats { get; set; } = 0;

    /// <summary>
    /// Updates heartbeat state
    /// </summary>
    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        HeartbeatSequence++;
        MissedHeartbeats = 0;
    }

    /// <summary>
    /// Records missed heartbeat
    /// </summary>
    public void RecordMissedHeartbeat()
    {
        MissedHeartbeats++;
        if (MissedHeartbeats >= 2)
        {
            Status = PeerConnectionStatus.HeartbeatTimeout;
        }
    }

    /// <summary>
    /// Checks if heartbeat is timed out
    /// </summary>
    public bool IsHeartbeatTimedOut()
    {
        var elapsed = DateTime.UtcNow - LastHeartbeat;
        return elapsed.TotalSeconds > 30;
    }
}
```

---

### 7.5 SyncCheckpoint.cs

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Synchronization checkpoint for tracking incremental sync progress
/// </summary>
public class SyncCheckpoint
{
    /// <summary>
    /// Peer that owns this checkpoint
    /// </summary>
    [Required]
    [MaxLength(64)]
    [JsonPropertyName("peerId")]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Last synchronized system register version
    /// </summary>
    [Required]
    [JsonPropertyName("currentVersion")]
    public long CurrentVersion { get; set; } = 0;

    /// <summary>
    /// Unix timestamp (milliseconds) of last successful sync
    /// </summary>
    [Required]
    [JsonPropertyName("lastSyncTime")]
    public long LastSyncTime { get; set; }

    /// <summary>
    /// Total number of blueprints in local replica
    /// </summary>
    [Required]
    [JsonPropertyName("totalBlueprints")]
    public int TotalBlueprints { get; set; } = 0;

    /// <summary>
    /// Central node this checkpoint is for
    /// </summary>
    [Required]
    [MaxLength(64)]
    [JsonPropertyName("centralNodeId")]
    public string CentralNodeId { get; set; } = string.Empty;

    /// <summary>
    /// When next periodic sync is due (UTC)
    /// </summary>
    [Required]
    [JsonPropertyName("nextSyncDue")]
    public DateTime NextSyncDue { get; set; } = DateTime.UtcNow.AddMinutes(5);

    /// <summary>
    /// Updates checkpoint after successful sync
    /// </summary>
    public void UpdateAfterSync(long newVersion, int blueprintCount)
    {
        CurrentVersion = newVersion;
        TotalBlueprints = blueprintCount;
        LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        NextSyncDue = DateTime.UtcNow.AddMinutes(5); // 5-minute periodic sync
    }

    /// <summary>
    /// Checks if sync is due
    /// </summary>
    public bool IsSyncDue()
    {
        return DateTime.UtcNow >= NextSyncDue;
    }
}
```

---

### 7.6 Enumerations

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Connection status for a central node
/// </summary>
public enum CentralNodeConnectionStatus
{
    /// <summary>
    /// Not connected to this central node
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Connection attempt in progress
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Successfully connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connection attempt failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Connected but heartbeat timeout occurred
    /// </summary>
    HeartbeatTimeout = 4
}

/// <summary>
/// Overall peer connection status
/// </summary>
public enum PeerConnectionStatus
{
    /// <summary>
    /// Peer is disconnected from all central nodes
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Peer is attempting to connect to a central node
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Peer is connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Heartbeat timeout detected, attempting failover
    /// </summary>
    HeartbeatTimeout = 3,

    /// <summary>
    /// Operating without central node connection (using last known replica)
    /// </summary>
    Isolated = 4
}

/// <summary>
/// Type of blueprint notification
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// New blueprint published
    /// </summary>
    BlueprintPublished = 0,

    /// <summary>
    /// Existing blueprint updated (new version)
    /// </summary>
    BlueprintUpdated = 1,

    /// <summary>
    /// Blueprint deprecated/withdrawn
    /// </summary>
    BlueprintDeprecated = 2
}
```

---

## 8. Validation Constants

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validation constants for peer service central node connection
/// </summary>
public static class PeerServiceConstants
{
    /// <summary>
    /// Well-known system register identifier
    /// </summary>
    public static readonly Guid SystemRegisterId = Guid.Empty; // 00000000-0000-0000-0000-000000000000

    /// <summary>
    /// MongoDB collection name for system register blueprints
    /// </summary>
    public const string SystemRegisterCollectionName = "sorcha_system_register_blueprints";

    /// <summary>
    /// Valid central node hostnames
    /// </summary>
    public static readonly string[] CentralNodeHostnames =
    {
        "n0.sorcha.dev",
        "n1.sorcha.dev",
        "n2.sorcha.dev"
    };

    /// <summary>
    /// Heartbeat interval in seconds (FR-036)
    /// </summary>
    public const int HeartbeatIntervalSeconds = 30;

    /// <summary>
    /// Heartbeat timeout in seconds (FR-036)
    /// </summary>
    public const int HeartbeatTimeoutSeconds = 30;

    /// <summary>
    /// Maximum missed heartbeats before failover (2 = 60 seconds total)
    /// </summary>
    public const int MaxMissedHeartbeats = 2;

    /// <summary>
    /// Periodic sync interval in minutes (FR-032)
    /// </summary>
    public const int PeriodicSyncIntervalMinutes = 5;

    /// <summary>
    /// Connection retry initial delay in seconds
    /// </summary>
    public const int RetryInitialDelaySeconds = 1;

    /// <summary>
    /// Connection retry multiplier (exponential backoff)
    /// </summary>
    public const double RetryMultiplier = 2.0;

    /// <summary>
    /// Connection retry max delay in seconds (cap at 60s)
    /// </summary>
    public const int RetryMaxDelaySeconds = 60;

    /// <summary>
    /// Maximum retry attempts before giving up and trying next central node
    /// </summary>
    public const int MaxRetryAttempts = 10;

    /// <summary>
    /// Connection timeout per attempt in seconds
    /// </summary>
    public const int ConnectionTimeoutSeconds = 30;

    /// <summary>
    /// Push notification delivery target (80% of peers within 30s, SC-016)
    /// </summary>
    public const double PushNotificationTargetPercent = 0.80;

    /// <summary>
    /// Push notification delivery timeout in seconds (SC-016)
    /// </summary>
    public const int PushNotificationTimeoutSeconds = 30;
}
```

---

## 9. Summary

This data model provides comprehensive definitions for all entities, relationships, validation rules, state machines, and storage schemas required for the peer service central node connection feature. Key highlights:

### Entities
- **7 core entities** defined with complete field specifications, types, and validation rules
- **3 enumerations** for status management
- **MongoDB storage schema** for system register with indexes for efficient querying
- **Local file/in-memory storage** for sync checkpoints and active peer info

### Validation Rules
- Central node hostnames MUST match `n0/n1/n2.sorcha.dev` pattern
- Heartbeat timeout: 30 seconds (2 missed = failover)
- Periodic sync: 5 minutes
- Connection retry backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s sequence
- System register ID: well-known constant `00000000-0000-0000-0000-000000000000`

### State Machines
- **Central node connection lifecycle**: 5 states with transitions
- **Peer connection status lifecycle**: 5 states with failover logic
- **System register sync lifecycle**: Periodic + push notification hybrid

### C# Implementation
- All entities defined as C# classes with proper annotations
- Data annotations for validation (`[Required]`, `[Range]`, `[MaxLength]`)
- MongoDB BSON attributes for schema mapping
- Thread-safe in-memory storage using `ConcurrentDictionary`
- JSON serialization support for checkpoint persistence

This data model is ready for implementation and aligns with all functional requirements (FR-001 through FR-037) and success criteria (SC-001 through SC-016) defined in the feature specification.

---

**Data Model Complete**: 2025-12-13
**Next Phase**: gRPC Contract Definitions (`contracts.md`)
**Status**: Ready for Implementation
