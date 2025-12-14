# Quickstart Guide: Peer Service Central Node Connection

**Feature**: Central Node Connection & System Register Synchronization
**Branch**: `001-register-genesis`
**Created**: 2025-12-13
**Status**: Developer Guide

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Configuration Examples](#configuration-examples)
4. [Local Development Setup](#local-development-setup)
5. [Testing Scenarios](#testing-scenarios)
6. [Verification Steps](#verification-steps)
7. [Common Issues and Troubleshooting](#common-issues-and-troubleshooting)
8. [API Endpoints](#api-endpoints)
9. [gRPC Testing](#grpc-testing)

---

## Overview

### What This Feature Does

The Peer Service Central Node Connection feature enables peer nodes to:

1. **Connect to Central Nodes**: Establish connections to central nodes (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev) with automatic failover
2. **Synchronize System Register**: Replicate published blueprints from the system register using hybrid pull + push synchronization
3. **Monitor Connection Health**: Maintain connection health through 30-second heartbeat monitoring
4. **Handle Failover**: Automatically failover to backup central nodes on connection failures
5. **Receive Push Notifications**: Get real-time notifications when new blueprints are published

### Key Concepts

**Central Nodes**: Three highly-available nodes (n0, n1, n2) that maintain the authoritative system register
- `n0.sorcha.dev` (priority 0, primary)
- `n1.sorcha.dev` (priority 1, secondary)
- `n2.sorcha.dev` (priority 2, tertiary)

**System Register**: A well-known register (ID: `00000000-0000-0000-0000-000000000000`) containing published blueprint definitions

**Synchronization Modes**:
- **Full Sync**: Initial synchronization of all blueprints (on first connection)
- **Incremental Sync**: Periodic sync every 5 minutes (only new blueprints since last version)
- **Push Notifications**: Real-time notifications via server streaming (80% delivery within 30s)

**Connection Lifecycle**:
```
Disconnected → Connecting → Connected → [Heartbeat Monitoring]
                    ↓                            ↓
              Failed (Retry)              Heartbeat Timeout
                    ↓                            ↓
           Failover to Next Node          Failover to Next Node
```

---

## Prerequisites

### Required Tools

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - For running MongoDB, PostgreSQL, Redis
- **grpcurl** (optional) - For testing gRPC endpoints: `brew install grpcurl` or [Download](https://github.com/fullstorydev/grpcurl)
- **MongoDB Compass** (optional) - For inspecting system register: [Download](https://www.mongodb.com/products/compass)

### Dependencies

The following NuGet packages are already included:

```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.71.0" />
<PackageReference Include="MongoDB.Driver" Version="3.5.2" />
<PackageReference Include="Polly" Version="8.6.5" />
<PackageReference Include="StackExchange.Redis" Version="2.10.1" />
```

### Infrastructure Requirements

- **MongoDB**: For system register storage (`sorcha_system_register_blueprints` collection)
- **Redis**: For distributed caching and SignalR backplane
- **Network Access**: gRPC port 5000 (HTTP/2) and HTTP port 8080 (health checks)

---

## Configuration Examples

### Running as a Central Node

**File**: `appsettings.Production.json` (on n0.sorcha.dev server)

```json
{
  "PeerService": {
    "NodeId": "n0.sorcha.dev",
    "CentralNode": {
      "IsCentralNode": true,
      "ExpectedHostnamePattern": "*.sorcha.dev",
      "ValidateHostname": true
    },
    "MongoDb": {
      "ConnectionString": "mongodb://mongo:27017",
      "DatabaseName": "sorcha_register",
      "SystemRegisterCollectionName": "sorcha_system_register_blueprints"
    },
    "Heartbeat": {
      "IntervalSeconds": 30,
      "TimeoutSeconds": 30,
      "MaxMissedHeartbeats": 2
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Sorcha.Peer.Service": "Debug"
    }
  }
}
```

**Environment Variables** (Docker):
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
PeerService__NodeId=n0.sorcha.dev
PeerService__CentralNode__IsCentralNode=true
PeerService__MongoDb__ConnectionString=mongodb://mongo:27017
```

### Running as a Peer Node

**File**: `appsettings.Development.json` (on peer node)

```json
{
  "PeerService": {
    "NodeId": "peer-node-local-001",
    "CentralNode": {
      "IsCentralNode": false,
      "CentralNodes": [
        {
          "NodeId": "n0.sorcha.dev",
          "Hostname": "n0.sorcha.dev",
          "Port": 5000,
          "Priority": 0
        },
        {
          "NodeId": "n1.sorcha.dev",
          "Hostname": "n1.sorcha.dev",
          "Port": 5000,
          "Priority": 1
        },
        {
          "NodeId": "n2.sorcha.dev",
          "Hostname": "n2.sorcha.dev",
          "Port": 5000,
          "Priority": 2
        }
      ]
    },
    "Synchronization": {
      "PeriodicSyncIntervalMinutes": 5,
      "ConnectionTimeoutSeconds": 30,
      "EnablePushNotifications": true
    },
    "ConnectionRetry": {
      "MaxRetryAttempts": 10,
      "InitialDelaySeconds": 1,
      "MaxDelaySeconds": 60,
      "Multiplier": 2.0,
      "UseJitter": true
    },
    "MongoDb": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "sorcha_peer_local",
      "SystemRegisterCollectionName": "sorcha_system_register_blueprints"
    }
  }
}
```

**Environment Variables** (Local Development):
```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_HTTP_PORTS=5000
PeerService__NodeId=peer-node-local-001
PeerService__CentralNode__IsCentralNode=false
```

### All Configuration Options

```json
{
  "PeerService": {
    // Node identification
    "NodeId": "peer-node-001",                    // Unique identifier for this node

    // Central node configuration
    "CentralNode": {
      "IsCentralNode": false,                     // Is this node a central node?
      "ExpectedHostnamePattern": "*.sorcha.dev",  // Hostname validation pattern
      "ValidateHostname": true,                   // Validate hostname on startup

      // Central node endpoints (for peer nodes only)
      "CentralNodes": [
        {
          "NodeId": "n0.sorcha.dev",              // Central node ID
          "Hostname": "n0.sorcha.dev",            // DNS hostname
          "Port": 5000,                           // gRPC port
          "Priority": 0                           // Connection priority (0 = highest)
        }
      ]
    },

    // Heartbeat configuration
    "Heartbeat": {
      "IntervalSeconds": 30,                      // Send heartbeat every 30s
      "TimeoutSeconds": 30,                       // Heartbeat timeout
      "MaxMissedHeartbeats": 2,                   // Failover after 2 missed (60s total)
      "IncludeMetrics": true                      // Include CPU/memory metrics
    },

    // Synchronization configuration
    "Synchronization": {
      "PeriodicSyncIntervalMinutes": 5,           // Full sync every 5 minutes
      "ConnectionTimeoutSeconds": 30,             // Connection timeout per attempt
      "EnablePushNotifications": true,            // Enable real-time push notifications
      "MaxBlueprintsPerSync": 0,                  // 0 = unlimited
      "ForceFullSyncOnStartup": false             // Force full sync on service startup
    },

    // Connection retry configuration (Polly)
    "ConnectionRetry": {
      "MaxRetryAttempts": 10,                     // Max retries before trying next node
      "InitialDelaySeconds": 1,                   // Initial retry delay
      "MaxDelaySeconds": 60,                      // Max delay between retries (cap)
      "Multiplier": 2.0,                          // Exponential backoff multiplier
      "UseJitter": true                           // Add jitter to prevent thundering herd
    },

    // MongoDB configuration
    "MongoDb": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "sorcha_peer",
      "SystemRegisterCollectionName": "sorcha_system_register_blueprints",
      "EnableRetries": true,
      "MaxRetries": 3,
      "RetryDelayMs": 500
    },

    // Checkpoint persistence
    "Checkpoint": {
      "PersistenceMode": "File",                  // File, Memory, MongoDB
      "FilePath": "./data/sync_checkpoint.json",  // Local file path
      "AutoSaveIntervalSeconds": 30               // Auto-save checkpoint interval
    },

    // Logging
    "Logging": {
      "LogConnectionEvents": true,
      "LogHeartbeats": false,                     // Can be verbose
      "LogSyncOperations": true,
      "LogNotifications": true
    }
  }
}
```

---

## Local Development Setup

### Option 1: Docker Compose (Recommended for Testing)

This setup creates 3 central nodes + 2 peer nodes for comprehensive testing.

**File**: `docker-compose.central-nodes.yml`

```yaml
version: '3.8'

services:
  # Shared MongoDB for all nodes
  mongo:
    image: mongo:7
    container_name: sorcha-mongo
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db
    environment:
      MONGO_INITDB_DATABASE: sorcha_register

  # Shared Redis for caching
  redis:
    image: redis:7-alpine
    container_name: sorcha-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  # Central Node 0 (Primary)
  central-node-0:
    build:
      context: .
      dockerfile: src/Services/Sorcha.Peer.Service/Dockerfile
    container_name: central-n0.sorcha.dev
    hostname: n0.sorcha.dev
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: 8080
      PeerService__NodeId: n0.sorcha.dev
      PeerService__CentralNode__IsCentralNode: "true"
      PeerService__MongoDb__ConnectionString: mongodb://mongo:27017
      PeerService__MongoDb__DatabaseName: sorcha_register
    depends_on:
      - mongo
      - redis

  # Central Node 1 (Secondary)
  central-node-1:
    build:
      context: .
      dockerfile: src/Services/Sorcha.Peer.Service/Dockerfile
    container_name: central-n1.sorcha.dev
    hostname: n1.sorcha.dev
    ports:
      - "5001:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: 8080
      PeerService__NodeId: n1.sorcha.dev
      PeerService__CentralNode__IsCentralNode: "true"
      PeerService__MongoDb__ConnectionString: mongodb://mongo:27017
      PeerService__MongoDb__DatabaseName: sorcha_register
    depends_on:
      - mongo
      - redis

  # Central Node 2 (Tertiary)
  central-node-2:
    build:
      context: .
      dockerfile: src/Services/Sorcha.Peer.Service/Dockerfile
    container_name: central-n2.sorcha.dev
    hostname: n2.sorcha.dev
    ports:
      - "5002:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: 8080
      PeerService__NodeId: n2.sorcha.dev
      PeerService__CentralNode__IsCentralNode: "true"
      PeerService__MongoDb__ConnectionString: mongodb://mongo:27017
      PeerService__MongoDb__DatabaseName: sorcha_register
    depends_on:
      - mongo
      - redis

  # Peer Node 1
  peer-node-1:
    build:
      context: .
      dockerfile: src/Services/Sorcha.Peer.Service/Dockerfile
    container_name: peer-node-1
    hostname: peer-node-1.local
    ports:
      - "5100:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: 8080
      PeerService__NodeId: peer-node-1
      PeerService__CentralNode__IsCentralNode: "false"
      PeerService__CentralNode__CentralNodes__0__NodeId: n0.sorcha.dev
      PeerService__CentralNode__CentralNodes__0__Hostname: central-n0.sorcha.dev
      PeerService__CentralNode__CentralNodes__0__Port: 8080
      PeerService__CentralNode__CentralNodes__0__Priority: 0
      PeerService__CentralNode__CentralNodes__1__NodeId: n1.sorcha.dev
      PeerService__CentralNode__CentralNodes__1__Hostname: central-n1.sorcha.dev
      PeerService__CentralNode__CentralNodes__1__Port: 8080
      PeerService__CentralNode__CentralNodes__1__Priority: 1
      PeerService__CentralNode__CentralNodes__2__NodeId: n2.sorcha.dev
      PeerService__CentralNode__CentralNodes__2__Hostname: central-n2.sorcha.dev
      PeerService__CentralNode__CentralNodes__2__Port: 8080
      PeerService__CentralNode__CentralNodes__2__Priority: 2
      PeerService__MongoDb__ConnectionString: mongodb://mongo:27017
      PeerService__MongoDb__DatabaseName: sorcha_peer_1
    depends_on:
      - mongo
      - redis
      - central-node-0
      - central-node-1
      - central-node-2

  # Peer Node 2
  peer-node-2:
    build:
      context: .
      dockerfile: src/Services/Sorcha.Peer.Service/Dockerfile
    container_name: peer-node-2
    hostname: peer-node-2.local
    ports:
      - "5101:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: 8080
      PeerService__NodeId: peer-node-2
      PeerService__CentralNode__IsCentralNode: "false"
      PeerService__CentralNode__CentralNodes__0__NodeId: n0.sorcha.dev
      PeerService__CentralNode__CentralNodes__0__Hostname: central-n0.sorcha.dev
      PeerService__CentralNode__CentralNodes__0__Port: 8080
      PeerService__CentralNode__CentralNodes__0__Priority: 0
      PeerService__CentralNode__CentralNodes__1__NodeId: n1.sorcha.dev
      PeerService__CentralNode__CentralNodes__1__Hostname: central-n1.sorcha.dev
      PeerService__CentralNode__CentralNodes__1__Port: 8080
      PeerService__CentralNode__CentralNodes__1__Priority: 1
      PeerService__CentralNode__CentralNodes__2__NodeId: n2.sorcha.dev
      PeerService__CentralNode__CentralNodes__2__Hostname: central-n2.sorcha.dev
      PeerService__CentralNode__CentralNodes__2__Port: 8080
      PeerService__CentralNode__CentralNodes__2__Priority: 2
      PeerService__MongoDb__ConnectionString: mongodb://mongo:27017
      PeerService__MongoDb__DatabaseName: sorcha_peer_2
    depends_on:
      - mongo
      - redis
      - central-node-0
      - central-node-1
      - central-node-2

volumes:
  mongo-data:
  redis-data:
```

**Start the environment**:

```bash
# Build and start all services
docker-compose -f docker-compose.central-nodes.yml up -d

# View logs
docker-compose -f docker-compose.central-nodes.yml logs -f

# View specific service logs
docker-compose -f docker-compose.central-nodes.yml logs -f peer-node-1

# Stop all services
docker-compose -f docker-compose.central-nodes.yml down

# Stop and remove volumes (clean state)
docker-compose -f docker-compose.central-nodes.yml down -v
```

**Service Endpoints**:
- Central Node 0: http://localhost:5000
- Central Node 1: http://localhost:5001
- Central Node 2: http://localhost:5002
- Peer Node 1: http://localhost:5100
- Peer Node 2: http://localhost:5101
- MongoDB: localhost:27017
- Redis: localhost:6379

### Option 2: .NET Aspire (Integrated Development)

**.NET Aspire** already orchestrates the Peer Service in the AppHost.

**File**: `src/Apps/Sorcha.AppHost/AppHost.cs` (excerpt)

```csharp
// Add MongoDB for Register Service transaction storage
var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress(); // Adds Mongo Express UI for development

var registerDb = mongodb.AddDatabase("register-db", "sorcha_register");

// Add Redis for distributed caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander();

// Add Peer Service with Redis reference
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis)
    .WithReference(registerDb); // Add MongoDB reference
```

**Run with Aspire**:

```bash
# Navigate to AppHost directory
cd src/Apps/Sorcha.AppHost

# Run the application
dotnet run

# Aspire Dashboard opens automatically at: http://localhost:15888
# Peer Service gRPC endpoint: https://localhost:7XXX (check dashboard for assigned port)
```

**Aspire Dashboard Features**:
- View all services and their status
- See logs in real-time
- Monitor resource usage
- Access MongoDB via Mongo Express
- Access Redis via Redis Commander

### Option 3: Local Development (Single Peer Node)

**Prerequisites**:
```bash
# Start MongoDB
docker run -d -p 27017:27017 --name sorcha-mongo mongo:7

# Start Redis
docker run -d -p 6379:6379 --name sorcha-redis redis:7-alpine
```

**Run Peer Service**:
```bash
# Navigate to Peer Service directory
cd src/Services/Sorcha.Peer.Service

# Run the service
dotnet run

# Service starts on: http://localhost:5000 (HTTP/1.1 + HTTP/2)
```

**Configure Central Nodes** in `appsettings.Development.json`:

For local testing, you can point to localhost instances:

```json
{
  "PeerService": {
    "NodeId": "peer-local",
    "CentralNode": {
      "IsCentralNode": false,
      "CentralNodes": [
        {
          "NodeId": "n0.local",
          "Hostname": "localhost",
          "Port": 5000,
          "Priority": 0
        }
      ]
    }
  }
}
```

---

## Testing Scenarios

### Scenario 1: Test System Register Replication

**Goal**: Verify peer node can synchronize blueprints from central node

**Steps**:

1. **Publish a blueprint to system register** (on central node):

```bash
# Using MongoDB directly
mongosh mongodb://localhost:27017/sorcha_register

# Insert a test blueprint
db.sorcha_system_register_blueprints.insertOne({
  "_id": "test-blueprint-v1",
  "registerId": "00000000-0000-0000-0000-000000000000",
  "document": {
    "@context": "https://sorcha.dev/blueprints/v1",
    "id": "test-blueprint-v1",
    "name": "Test Blueprint",
    "version": "1.0.0",
    "actions": []
  },
  "publishedAt": new Date(),
  "publishedBy": "system",
  "version": NumberLong(1),
  "isActive": true
});
```

2. **Trigger incremental sync on peer node**:

```bash
# View peer node logs to see sync operation
docker logs -f peer-node-1

# Or check sync checkpoint via REST API
curl http://localhost:5100/api/sync/checkpoint
```

3. **Verify blueprint replicated to peer**:

```bash
# Connect to peer's MongoDB database
mongosh mongodb://localhost:27017/sorcha_peer_1

# Query for the blueprint
db.sorcha_system_register_blueprints.findOne({ "_id": "test-blueprint-v1" })
```

**Expected Result**:
- Peer node logs show incremental sync completed
- Blueprint exists in peer's local MongoDB
- Checkpoint version updated to 1

### Scenario 2: Test Heartbeat Monitoring and Failover

**Goal**: Verify peer node fails over to secondary central node when primary becomes unavailable

**Steps**:

1. **Start all nodes** (Docker Compose setup):

```bash
docker-compose -f docker-compose.central-nodes.yml up -d
```

2. **Verify peer connected to primary** (n0):

```bash
# Check peer node logs
docker logs peer-node-1 | grep "Connected to central node"
# Expected: "Connected to central node: n0.sorcha.dev"

# Or use REST API
curl http://localhost:5100/api/connection/status | jq
```

3. **Stop primary central node**:

```bash
docker stop central-n0.sorcha.dev
```

4. **Monitor failover in peer logs**:

```bash
docker logs -f peer-node-1

# Expected sequence:
# [Heartbeat] Heartbeat timeout - no response from n0.sorcha.dev
# [Heartbeat] Missed heartbeat 1/2
# [Heartbeat] Missed heartbeat 2/2 - triggering failover
# [Connection] Disconnecting from n0.sorcha.dev (reason: HeartbeatTimeout)
# [Connection] Attempting connection to n1.sorcha.dev (priority 1)
# [Connection] Connected to central node: n1.sorcha.dev
```

5. **Verify connection to secondary**:

```bash
curl http://localhost:5100/api/connection/status | jq
# Expected: "connectedCentralNodeId": "n1.sorcha.dev"
```

6. **Restore primary and verify it stays on n1** (no unnecessary failback):

```bash
docker start central-n0.sorcha.dev

# Peer should remain connected to n1 (sticky connection)
docker logs peer-node-1 | tail -20
```

**Expected Result**:
- Peer detects heartbeat timeout after 60 seconds (2 missed heartbeats)
- Peer automatically fails over to n1.sorcha.dev
- Peer continues operating normally on n1
- Peer does NOT fail back to n0 automatically (manual failover only)

### Scenario 3: Test Push Notifications

**Goal**: Verify peer receives real-time push notification when blueprint is published

**Steps**:

1. **Subscribe peer to push notifications**:

Peer automatically subscribes on connection, but verify:

```bash
# Check peer logs for subscription confirmation
docker logs peer-node-1 | grep "Subscribed to push notifications"
```

2. **Publish a new blueprint** (on central node):

```bash
mongosh mongodb://localhost:27017/sorcha_register

db.sorcha_system_register_blueprints.insertOne({
  "_id": "push-test-v1",
  "registerId": "00000000-0000-0000-0000-000000000000",
  "document": {
    "@context": "https://sorcha.dev/blueprints/v1",
    "id": "push-test-v1",
    "name": "Push Notification Test",
    "version": "1.0.0"
  },
  "publishedAt": new Date(),
  "publishedBy": "system",
  "version": NumberLong(2),
  "isActive": true
});
```

3. **Verify peer receives push notification within 30 seconds**:

```bash
# Monitor peer logs
docker logs -f peer-node-1

# Expected log entries:
# [Notification] Received push notification: blueprint=push-test-v1, version=2
# [Sync] Triggered incremental sync due to push notification
# [Sync] Incremental sync completed: 1 blueprint(s) synchronized
```

4. **Verify blueprint available on peer**:

```bash
mongosh mongodb://localhost:27017/sorcha_peer_1

db.sorcha_system_register_blueprints.findOne({ "_id": "push-test-v1" })
```

**Expected Result**:
- Peer receives notification within 30 seconds
- Peer triggers incremental sync automatically
- Blueprint synchronized to peer's local database
- Total latency < 60 seconds (push notification + sync)

### Scenario 4: Test Connection Retry with Exponential Backoff

**Goal**: Verify connection retry logic with exponential backoff sequence

**Steps**:

1. **Stop all central nodes**:

```bash
docker stop central-n0.sorcha.dev central-n1.sorcha.dev central-n2.sorcha.dev
```

2. **Start peer node**:

```bash
docker start peer-node-1
```

3. **Monitor retry attempts** in logs:

```bash
docker logs -f peer-node-1

# Expected retry sequence (with jitter):
# [Connection] Attempt 1/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 1 after ~1s
# [Connection] Attempt 2/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 2 after ~2s
# [Connection] Attempt 3/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 3 after ~4s
# [Connection] Attempt 4/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 4 after ~8s
# [Connection] Attempt 5/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 5 after ~16s
# [Connection] Attempt 6/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 6 after ~32s
# [Connection] Attempt 7/10 to n0.sorcha.dev - Failed (Connection refused)
# [Connection] Retry 7 after ~60s (capped at max)
# ... continues until max attempts (10) ...
# [Connection] All retry attempts exhausted for n0.sorcha.dev
# [Connection] Attempting connection to n1.sorcha.dev (priority 1)
```

4. **Start n1 during retry to n1**:

```bash
# Wait for peer to start trying n1
docker start central-n1.sorcha.dev
```

5. **Verify successful connection**:

```bash
docker logs peer-node-1 | tail -10
# Expected: "Connected to central node: n1.sorcha.dev"
```

**Expected Result**:
- Retry delays follow exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s, 60s, 60s, 60s
- Jitter prevents all peers from retrying simultaneously
- After 10 failed attempts to n0, peer tries n1
- Connection succeeds when n1 becomes available

### Scenario 5: Test All Central Nodes Unreachable (Isolated Mode)

**Goal**: Verify peer operates in "isolated mode" when all central nodes are unreachable

**Steps**:

1. **Ensure peer has some blueprints synced**:

```bash
# Check peer's local database
mongosh mongodb://localhost:27017/sorcha_peer_1
db.sorcha_system_register_blueprints.countDocuments()
```

2. **Stop all central nodes**:

```bash
docker stop central-n0.sorcha.dev central-n1.sorcha.dev central-n2.sorcha.dev
```

3. **Monitor peer status**:

```bash
docker logs -f peer-node-1

# Expected:
# [Connection] All central nodes unreachable
# [Status] Entering isolated mode - operating with last known system register replica
# [Status] Connection status: Isolated
```

4. **Verify peer continues operating**:

```bash
# Peer health check should still respond
curl http://localhost:5100/health
# Expected: HTTP 200 OK

# Connection status shows isolated
curl http://localhost:5100/api/connection/status | jq
# Expected: "status": "Isolated"

# Peer can still serve local blueprints
curl http://localhost:5100/api/blueprints | jq
```

5. **Restore n2 (lowest priority) and verify reconnection**:

```bash
docker start central-n2.sorcha.dev

# Monitor peer logs
docker logs -f peer-node-1

# Expected:
# [Connection] Central node n2.sorcha.dev reachable - attempting connection
# [Connection] Connected to central node: n2.sorcha.dev
# [Status] Exiting isolated mode
# [Sync] Triggering full sync to catch up...
```

**Expected Result**:
- Peer enters isolated mode gracefully
- Peer continues to operate with last known system register replica
- Peer periodically checks for central node availability
- Peer reconnects automatically when central node becomes available
- Peer triggers full sync to catch up after reconnection

---

## Verification Steps

### 1. Verify Central Node Detection

**Check if node correctly identifies as central or peer**:

```bash
# View startup logs
docker logs central-n0.sorcha.dev | grep "Central Node"

# Expected (Central Node):
# [Startup] Central node detection: IsCentralNode=true
# [Startup] Hostname validation: n0.sorcha.dev matches pattern *.sorcha.dev
# [Startup] Running as CENTRAL NODE: n0.sorcha.dev

# Expected (Peer Node):
# [Startup] Central node detection: IsCentralNode=false
# [Startup] Running as PEER NODE: peer-node-1
```

**REST API Check**:

```bash
# Central node
curl http://localhost:5000/api/node/info | jq

# Expected response:
{
  "nodeId": "n0.sorcha.dev",
  "nodeType": "Central",
  "isCentralNode": true,
  "hostname": "n0.sorcha.dev"
}

# Peer node
curl http://localhost:5100/api/node/info | jq

# Expected response:
{
  "nodeId": "peer-node-1",
  "nodeType": "Peer",
  "isCentralNode": false,
  "connectedCentralNode": "n0.sorcha.dev"
}
```

### 2. Verify System Register Sync

**Check sync checkpoint**:

```bash
# Get sync checkpoint from peer
curl http://localhost:5100/api/sync/checkpoint | jq

# Expected response:
{
  "peerId": "peer-node-1",
  "currentVersion": 5,
  "lastSyncTime": 1702474800000,
  "totalBlueprints": 5,
  "centralNodeId": "n0.sorcha.dev",
  "nextSyncDue": "2025-12-13T12:35:00Z",
  "status": "UpToDate"
}
```

**Compare blueprint counts**:

```bash
# Central node blueprint count
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.sorcha_system_register_blueprints.countDocuments({ isActive: true })"

# Peer node blueprint count
mongosh mongodb://localhost:27017/sorcha_peer_1 --quiet --eval \
  "db.sorcha_system_register_blueprints.countDocuments({ isActive: true })"

# Should match after successful sync
```

**Check sync history**:

```bash
# Get recent sync operations
curl http://localhost:5100/api/sync/history | jq

# Expected response (array of sync operations):
[
  {
    "syncId": "sync-12345",
    "syncType": "Incremental",
    "startTime": "2025-12-13T12:30:00Z",
    "endTime": "2025-12-13T12:30:15Z",
    "blueprintsSynced": 3,
    "status": "Completed"
  },
  {
    "syncId": "sync-12344",
    "syncType": "Full",
    "startTime": "2025-12-13T12:00:00Z",
    "endTime": "2025-12-13T12:00:45Z",
    "blueprintsSynced": 25,
    "status": "Completed"
  }
]
```

### 3. Verify Heartbeat Monitoring

**Check heartbeat status**:

```bash
# Get heartbeat status from peer
curl http://localhost:5100/api/heartbeat/status | jq

# Expected response:
{
  "peerId": "peer-node-1",
  "connectedCentralNode": "n0.sorcha.dev",
  "lastHeartbeatSent": "2025-12-13T12:30:45Z",
  "lastHeartbeatAcknowledged": "2025-12-13T12:30:45Z",
  "missedHeartbeats": 0,
  "isHealthy": true,
  "averageLatencyMs": 15.3,
  "totalHeartbeatsSent": 120,
  "heartbeatSequence": 120
}
```

**Monitor heartbeat logs** (verbose):

Enable heartbeat logging in `appsettings.json`:

```json
{
  "PeerService": {
    "Logging": {
      "LogHeartbeats": true
    }
  }
}
```

```bash
docker logs -f peer-node-1 | grep Heartbeat

# Expected output (every 30 seconds):
# [12:30:00] [Heartbeat] Sending heartbeat #120 to n0.sorcha.dev
# [12:30:00] [Heartbeat] Acknowledged by n0.sorcha.dev (RTT: 15ms, Version: 5)
# [12:30:30] [Heartbeat] Sending heartbeat #121 to n0.sorcha.dev
# [12:30:30] [Heartbeat] Acknowledged by n0.sorcha.dev (RTT: 14ms, Version: 5)
```

**Test heartbeat timeout**:

```bash
# Block network traffic to central node (Docker)
docker network disconnect bridge central-n0.sorcha.dev

# Monitor peer logs for timeout
docker logs -f peer-node-1

# Expected (after 60 seconds):
# [Heartbeat] Heartbeat timeout #1/2
# [Heartbeat] Heartbeat timeout #2/2 - triggering failover
# [Connection] Failing over to n1.sorcha.dev

# Restore network
docker network connect bridge central-n0.sorcha.dev
```

### 4. Check Connection Status

**REST API - Connection Status**:

```bash
curl http://localhost:5100/api/connection/status | jq

# Expected response:
{
  "peerId": "peer-node-1",
  "status": "Connected",
  "connectedCentralNodeId": "n0.sorcha.dev",
  "connectionEstablished": "2025-12-13T12:00:00Z",
  "sessionId": "session-abc123",
  "lastHeartbeat": "2025-12-13T12:30:45Z",
  "lastSyncVersion": 5,
  "centralNodeConfig": {
    "nodeId": "n0.sorcha.dev",
    "hostname": "n0.sorcha.dev",
    "port": 5000,
    "priority": 0
  }
}
```

**gRPC - Get Central Node Status**:

```bash
grpcurl -plaintext -d '{"peer_id": "peer-node-1"}' \
  localhost:5000 sorcha.peer.v1.CentralNodeConnection/GetCentralNodeStatus

# Expected response:
{
  "nodeId": "n0.sorcha.dev",
  "health": "NODE_HEALTH_HEALTHY",
  "currentSystemRegisterVersion": "5",
  "totalBlueprints": 5,
  "activePeerCount": 2,
  "uptimeSeconds": "3600",
  "lastBlueprintPublishedAt": "1702474800000"
}
```

**Check all central node configurations**:

```bash
curl http://localhost:5100/api/connection/central-nodes | jq

# Expected response:
[
  {
    "nodeId": "n0.sorcha.dev",
    "hostname": "n0.sorcha.dev",
    "port": 5000,
    "priority": 0,
    "connectionStatus": "Connected",
    "isActive": true,
    "lastSuccessfulConnection": "2025-12-13T12:00:00Z"
  },
  {
    "nodeId": "n1.sorcha.dev",
    "hostname": "n1.sorcha.dev",
    "port": 5000,
    "priority": 1,
    "connectionStatus": "Disconnected",
    "isActive": false,
    "lastConnectionAttempt": null
  },
  {
    "nodeId": "n2.sorcha.dev",
    "hostname": "n2.sorcha.dev",
    "port": 5000,
    "priority": 2,
    "connectionStatus": "Disconnected",
    "isActive": false,
    "lastConnectionAttempt": null
  }
]
```

---

## Common Issues and Troubleshooting

### Issue 1: Peer Cannot Connect to Central Node

**Symptoms**:
```
[Error] Failed to connect to n0.sorcha.dev: Connection refused
[Error] All retry attempts exhausted
```

**Possible Causes**:
1. Central node not running
2. Network connectivity issue
3. Incorrect hostname/port configuration
4. Firewall blocking gRPC port (5000)

**Solutions**:

```bash
# Verify central node is running
docker ps | grep central-n0

# Check central node logs
docker logs central-n0.sorcha.dev

# Test network connectivity
nc -zv n0.sorcha.dev 5000
# or
telnet n0.sorcha.dev 5000

# Verify DNS resolution
nslookup n0.sorcha.dev
# or
dig n0.sorcha.dev

# Check Docker network
docker network inspect bridge

# Verify configuration
cat appsettings.Development.json | jq '.PeerService.CentralNode.CentralNodes'
```

### Issue 2: Heartbeat Timeout (False Positives)

**Symptoms**:
```
[Warning] Heartbeat timeout - no response from n0.sorcha.dev
[Warning] Missed heartbeat 1/2
```

**Possible Causes**:
1. Network latency > 30 seconds
2. Central node under heavy load
3. Clock skew between nodes
4. Firewall dropping gRPC packets

**Solutions**:

```bash
# Measure network latency
ping n0.sorcha.dev

# Check central node CPU/memory
docker stats central-n0.sorcha.dev

# Verify clock sync
date -u # On both nodes, should match

# Increase heartbeat timeout (temporary workaround)
# Edit appsettings.json:
{
  "PeerService": {
    "Heartbeat": {
      "TimeoutSeconds": 60  // Increase from 30s
    }
  }
}
```

### Issue 3: System Register Not Syncing

**Symptoms**:
```
[Info] Incremental sync completed: 0 blueprint(s) synchronized
```
Peer checkpoint shows `status: "Behind"` but no blueprints syncing.

**Possible Causes**:
1. MongoDB connection issue
2. Collection name mismatch
3. Version number not incrementing
4. Sync checkpoint corrupted

**Solutions**:

```bash
# Verify MongoDB connectivity
mongosh mongodb://localhost:27017/sorcha_register --eval "db.runCommand({ ping: 1 })"

# Check collection exists
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.getCollectionNames()"

# Verify blueprints exist
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.sorcha_system_register_blueprints.find().pretty()"

# Check version numbers
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.sorcha_system_register_blueprints.find({}, {blueprintId: 1, version: 1}).sort({version: 1})"

# Reset sync checkpoint (force full sync)
curl -X POST http://localhost:5100/api/sync/reset-checkpoint

# Or delete checkpoint file
rm ./data/sync_checkpoint.json
docker restart peer-node-1
```

### Issue 4: Push Notifications Not Received

**Symptoms**:
```
[Info] Blueprint published to system register
```
But peer doesn't receive push notification.

**Possible Causes**:
1. Peer not subscribed to notifications
2. Notification stream disconnected
3. Central node not publishing notifications
4. Firewall blocking streaming RPC

**Solutions**:

```bash
# Verify subscription status
curl http://localhost:5100/api/notifications/subscription | jq

# Expected:
{
  "isSubscribed": true,
  "subscribedAt": "2025-12-13T12:00:00Z",
  "centralNodeId": "n0.sorcha.dev"
}

# Check notification logs (enable verbose logging)
docker logs -f peer-node-1 | grep Notification

# Manually trigger subscription
curl -X POST http://localhost:5100/api/notifications/subscribe

# Check if EnablePushNotifications is true
cat appsettings.Development.json | jq '.PeerService.Synchronization.EnablePushNotifications'

# Restart peer to re-establish stream
docker restart peer-node-1
```

### Issue 5: MongoDB Version Conflicts

**Symptoms**:
```
[Error] Duplicate key error: version already exists
```

**Possible Causes**:
1. Multiple central nodes writing to same MongoDB
2. Version auto-increment not configured
3. Race condition in version assignment

**Solutions**:

```bash
# Check for duplicate versions
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.sorcha_system_register_blueprints.aggregate([
     { \$group: { _id: '\$version', count: { \$sum: 1 } } },
     { \$match: { count: { \$gt: 1 } } }
   ])"

# Ensure unique index on version
mongosh mongodb://localhost:27017/sorcha_register --quiet --eval \
  "db.sorcha_system_register_blueprints.createIndex({ version: 1 }, { unique: true })"

# Use MongoDB transactions for version assignment (in code)
# Or use atomic findAndModify for version counter
```

### Issue 6: "Isolated Mode" When Central Nodes Are Reachable

**Symptoms**:
```
[Status] Connection status: Isolated
```
But central nodes are running and reachable.

**Possible Causes**:
1. Retry logic exhausted all central nodes
2. Authentication/authorization failure (future enhancement)
3. Central nodes rejecting connection
4. Configuration mismatch

**Solutions**:

```bash
# Check central node logs for connection attempts
docker logs central-n0.sorcha.dev | grep "Connection request from peer"

# Verify peer ID is valid
cat appsettings.Development.json | jq '.PeerService.NodeId'

# Test gRPC connectivity manually
grpcurl -plaintext -d '{"peer_id": "peer-node-1", "peer_info": {"node_type": "Peer"}}' \
  localhost:5000 sorcha.peer.v1.CentralNodeConnection/ConnectToCentralNode

# Check if central node health is degraded
curl http://localhost:5000/api/node/health | jq

# Manually trigger reconnection
curl -X POST http://localhost:5100/api/connection/reconnect
```

---

## API Endpoints

The Peer Service exposes REST endpoints for monitoring and management.

### Connection Status

```bash
# Get current connection status
GET /api/connection/status

curl http://localhost:5100/api/connection/status | jq
```

**Response**:
```json
{
  "peerId": "peer-node-1",
  "status": "Connected",
  "connectedCentralNodeId": "n0.sorcha.dev",
  "connectionEstablished": "2025-12-13T12:00:00Z",
  "sessionId": "session-abc123",
  "lastHeartbeat": "2025-12-13T12:30:45Z",
  "lastSyncVersion": 5
}
```

### Central Nodes Configuration

```bash
# List all configured central nodes
GET /api/connection/central-nodes

curl http://localhost:5100/api/connection/central-nodes | jq
```

**Response**:
```json
[
  {
    "nodeId": "n0.sorcha.dev",
    "hostname": "n0.sorcha.dev",
    "port": 5000,
    "priority": 0,
    "connectionStatus": "Connected",
    "isActive": true,
    "lastSuccessfulConnection": "2025-12-13T12:00:00Z",
    "consecutiveFailures": 0
  }
]
```

### Sync Checkpoint

```bash
# Get current sync checkpoint
GET /api/sync/checkpoint

curl http://localhost:5100/api/sync/checkpoint | jq
```

**Response**:
```json
{
  "peerId": "peer-node-1",
  "currentVersion": 5,
  "lastSyncTime": 1702474800000,
  "totalBlueprints": 5,
  "centralNodeId": "n0.sorcha.dev",
  "nextSyncDue": "2025-12-13T12:35:00Z",
  "status": "UpToDate"
}
```

### Trigger Manual Sync

```bash
# Trigger incremental sync
POST /api/sync/trigger

curl -X POST http://localhost:5100/api/sync/trigger
```

**Response**:
```json
{
  "success": true,
  "message": "Incremental sync triggered",
  "syncId": "sync-12346"
}
```

### Heartbeat Status

```bash
# Get heartbeat status
GET /api/heartbeat/status

curl http://localhost:5100/api/heartbeat/status | jq
```

**Response**:
```json
{
  "peerId": "peer-node-1",
  "connectedCentralNode": "n0.sorcha.dev",
  "lastHeartbeatSent": "2025-12-13T12:30:45Z",
  "lastHeartbeatAcknowledged": "2025-12-13T12:30:45Z",
  "missedHeartbeats": 0,
  "isHealthy": true,
  "averageLatencyMs": 15.3
}
```

### Node Information

```bash
# Get node information
GET /api/node/info

curl http://localhost:5100/api/node/info | jq
```

**Response**:
```json
{
  "nodeId": "peer-node-1",
  "nodeType": "Peer",
  "isCentralNode": false,
  "hostname": "peer-node-1.local",
  "version": "1.0.0",
  "uptime": "3600"
}
```

### Blueprints (Local Replica)

```bash
# List blueprints in local replica
GET /api/blueprints

curl http://localhost:5100/api/blueprints | jq
```

**Response**:
```json
[
  {
    "blueprintId": "test-blueprint-v1",
    "version": 1,
    "publishedAt": "2025-12-13T10:00:00Z",
    "publishedBy": "system",
    "isActive": true
  }
]
```

---

## gRPC Testing

### Using grpcurl

**grpcurl** is a command-line tool for interacting with gRPC services.

#### Install grpcurl

```bash
# macOS
brew install grpcurl

# Linux
go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest

# Windows (via Scoop)
scoop install grpcurl
```

#### List Available Services

```bash
# List all gRPC services
grpcurl -plaintext localhost:5000 list

# Expected output:
# grpc.health.v1.Health
# grpc.reflection.v1alpha.ServerReflection
# sorcha.peer.v1.CentralNodeConnection
# sorcha.peer.v1.Heartbeat
# sorcha.peer.v1.SystemRegisterSync
```

#### List Service Methods

```bash
# List methods for CentralNodeConnection service
grpcurl -plaintext localhost:5000 list sorcha.peer.v1.CentralNodeConnection

# Expected output:
# sorcha.peer.v1.CentralNodeConnection.ConnectToCentralNode
# sorcha.peer.v1.CentralNodeConnection.DisconnectFromCentralNode
# sorcha.peer.v1.CentralNodeConnection.GetCentralNodeStatus
```

#### Describe Service

```bash
# Get detailed service description
grpcurl -plaintext localhost:5000 describe sorcha.peer.v1.CentralNodeConnection

# Get message structure
grpcurl -plaintext localhost:5000 describe sorcha.peer.v1.ConnectRequest
```

#### Test ConnectToCentralNode

```bash
# Connect to central node
grpcurl -plaintext -d '{
  "peer_id": "grpcurl-test-peer",
  "peer_info": {
    "address": "localhost:5100",
    "port": 5100,
    "node_type": "Peer",
    "supported_protocols": ["v1"],
    "capabilities": {
      "supports_push_notifications": true,
      "supports_incremental_sync": true,
      "max_blueprint_size": 16777216,
      "is_nat_restricted": false
    }
  },
  "last_known_version": 0,
  "connection_time": 1702474800000
}' localhost:5000 sorcha.peer.v1.CentralNodeConnection/ConnectToCentralNode
```

**Expected Response**:
```json
{
  "success": true,
  "message": "Connection established",
  "sessionId": "session-xyz789",
  "centralNodeId": "n0.sorcha.dev",
  "currentSystemRegisterVersion": "5",
  "connectedAt": "1702474800123",
  "heartbeatIntervalSeconds": 30,
  "config": {
    "heartbeatTimeoutSeconds": 30,
    "periodicSyncIntervalMinutes": 5,
    "pushNotificationsEnabled": true,
    "maxConcurrentSyncs": 3
  }
}
```

#### Test SendHeartbeat

```bash
# Send heartbeat
grpcurl -plaintext -d '{
  "peer_id": "grpcurl-test-peer",
  "timestamp": 1702474800000,
  "sequence_number": 1,
  "last_sync_version": 0,
  "session_id": "session-xyz789",
  "node_type": "Peer",
  "metrics": {
    "active_connections": 3,
    "cpu_usage_percent": 25.5,
    "memory_usage_mb": 512.0,
    "blueprint_count": 0
  }
}' localhost:5000 sorcha.peer.v1.Heartbeat/SendHeartbeat
```

**Expected Response**:
```json
{
  "success": true,
  "timestamp": "1702474800234",
  "centralNodeId": "n0.sorcha.dev",
  "currentSystemRegisterVersion": "5",
  "message": "OK",
  "recommendedAction": "RECOMMENDED_ACTION_SYNC",
  "serverLatencyMs": "12"
}
```

#### Test IncrementalSync (Server Streaming)

```bash
# Incremental sync
grpcurl -plaintext -d '{
  "peer_id": "grpcurl-test-peer",
  "last_known_version": 0,
  "full_sync": false,
  "session_id": "session-xyz789",
  "request_time": 1702474800000
}' localhost:5000 sorcha.peer.v1.SystemRegisterSync/IncrementalSync

# Response streams blueprint entries:
# {
#   "blueprintId": "test-blueprint-v1",
#   "registerId": "00000000-0000-0000-0000-000000000000",
#   "blueprintDocument": "eyJpZCI6InRlc3QifQ==",  // base64 encoded JSON
#   "publishedAt": "1702474800000",
#   "publishedBy": "system",
#   "version": "1",
#   "isActive": true
# }
# ... more entries ...
```

#### Test GetCentralNodeStatus

```bash
# Get central node status
grpcurl -plaintext -d '{
  "peer_id": "grpcurl-test-peer",
  "include_peer_list": true
}' localhost:5000 sorcha.peer.v1.CentralNodeConnection/GetCentralNodeStatus
```

**Expected Response**:
```json
{
  "nodeId": "n0.sorcha.dev",
  "health": "NODE_HEALTH_HEALTHY",
  "currentSystemRegisterVersion": "5",
  "totalBlueprints": 5,
  "activePeerCount": 3,
  "lastBlueprintPublishedAt": "1702474800000",
  "uptimeSeconds": "3600",
  "connectedPeers": [
    {
      "peerId": "peer-node-1",
      "sessionId": "session-abc123",
      "connectedAt": "1702471200000",
      "lastHeartbeatAt": "1702474750000",
      "lastSyncVersion": "5",
      "status": "PEER_CONNECTION_STATUS_CONNECTED"
    }
  ]
}
```

### Using BloomRPC (GUI Alternative)

**BloomRPC** is a GUI tool for testing gRPC services (alternative to grpcurl).

1. **Download**: [BloomRPC](https://github.com/bloomrpc/bloomrpc)
2. **Import Proto Files**: Load `*.proto` files from `specs/001-register-genesis/contracts/`
3. **Set Server Address**: `localhost:5000`
4. **Select Method**: Choose RPC method from dropdown
5. **Edit Request**: Fill in request fields in JSON editor
6. **Invoke**: Click "Play" button to send request

**Advantages**:
- Visual interface
- Auto-completion for message fields
- Response history
- Stream visualization

---

## Next Steps

After completing this quickstart guide, you should be able to:

1. Run central nodes and peer nodes locally
2. Test system register synchronization
3. Monitor heartbeat and failover behavior
4. Verify push notifications
5. Troubleshoot common issues

### Further Reading

- **Research Document**: [research.md](./research.md) - Technical decisions and rationale
- **Data Model**: [data-model.md](./data-model.md) - Entity definitions and schemas
- **gRPC Contracts**: [contracts/](./contracts/) - Proto definitions for all services
- **Implementation Guide**: Run `/speckit.tasks` to generate implementation tasks

### Production Deployment

For production deployment, refer to:
- **Security**: Implement TLS for gRPC endpoints
- **Authentication**: Add OAuth2/JWT authentication for central node connections
- **Monitoring**: Set up Prometheus metrics and Grafana dashboards
- **High Availability**: Deploy 3 central nodes with geographic distribution
- **DNS**: Configure proper DNS records for n0/n1/n2.sorcha.dev

---

**Happy Testing!**

For questions or issues, create a GitHub issue with:
- Environment details (Docker, .NET Aspire, local)
- Logs from affected services
- Configuration files (redact sensitive data)
- Steps to reproduce the issue
