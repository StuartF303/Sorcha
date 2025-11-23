# Sorcha Peer Service

**Version**: 1.0.0
**Status**: Core Complete (65% Complete - Production Infrastructure Pending)
**Framework**: .NET 10.0
**Architecture**: Microservice (gRPC + REST Hybrid)

---

## Overview

The **Peer Service** is the distributed networking layer of the Sorcha platform, enabling peer-to-peer communication, transaction propagation, and network topology management. Built on gRPC for high-performance communication and REST for compatibility, it creates a resilient mesh network where nodes can discover each other, exchange transactions, and maintain consensus coordination.

This service acts as the P2P network foundation for:
- **Peer discovery** via bootstrap nodes and gossip protocols
- **Transaction distribution** using epidemic gossip algorithms
- **Network resilience** with NAT traversal, offline queueing, and circuit breakers
- **Health monitoring** of peer connections and network quality
- **Protocol negotiation** supporting gRPC streaming, gRPC unary, and REST fallback
- **Offline mode** with transaction queueing and automatic retry

### Key Features

- **gRPC-First Design**: High-performance bidirectional streaming for transaction distribution (HTTP/2)
- **Peer Discovery**: Automatic peer discovery via bootstrap nodes with periodic refresh
- **NAT Traversal**: STUN protocol support for discovering external addresses behind NAT/firewalls
- **Gossip Protocol**: Epidemic gossip for efficient transaction propagation with configurable fanout
- **Health Monitoring**: Continuous peer health checks with connection quality tracking
- **Offline Queue**: Transaction queueing when network is unavailable with disk persistence
- **Multi-Protocol Support**: gRPC streaming (primary), gRPC unary, and REST (fallback)
- **Circuit Breaker**: Automatic failure detection and recovery for unreliable peers
- **Connection Testing**: Protocol negotiation and bandwidth testing for optimal communication
- **Dual-Port Architecture**: gRPC on port 5000 (HTTP/2), REST/SignalR on port 5001
- **Bootstrap Coordination**: Register with bootstrap nodes for network participation
- **Transaction Deduplication**: Prevent duplicate transaction propagation across the network

---

## Architecture

### Components

```
Peer Service
├── gRPC Layer (Port 5000, HTTP/2)
│   ├── PeerDiscoveryService (peer list, registration, ping)
│   ├── TransactionDistributionService (gossip, streaming)
│   └── gRPC Reflection (development)
├── REST Layer (Port 5001, HTTP/1.1)
│   ├── Service Info Endpoint
│   └── Health Checks
├── Business Logic Layer
│   ├── PeerListManager (peer registry)
│   ├── PeerDiscoveryService (bootstrap connection)
│   ├── NetworkAddressService (NAT traversal, STUN)
│   ├── HealthMonitorService (peer health checks)
│   ├── GossipProtocolEngine (epidemic gossip)
│   ├── TransactionDistributionService (propagation)
│   ├── TransactionQueueManager (offline queueing)
│   ├── CommunicationProtocolManager (protocol selection)
│   ├── ConnectionQualityTracker (latency, bandwidth)
│   └── ConnectionTestingService (protocol negotiation)
├── Network Layer
│   ├── StunClient (NAT traversal, external IP discovery)
│   └── HttpLookupService (fallback IP detection)
└── Background Services
    └── PeerService (continuous peer management)
```

### Data Flow

**Peer Discovery Flow:**
```
Node Startup → NetworkAddressService → [STUN Query, HTTP Lookup]
      ↓
Discover External Address (e.g., 203.0.113.5:5000)
      ↓
PeerDiscoveryService → [Connect to Bootstrap Nodes]
      ↓
Request Peer List (gRPC GetPeerList)
      ↓
PeerListManager → [Add Discovered Peers]
      ↓
Register with Bootstrap (gRPC RegisterPeer)
      ↓
HealthMonitorService → [Continuous Health Checks]
```

**Transaction Distribution Flow (Gossip):**
```
Transaction Created (from Blueprint/Wallet Service)
      ↓
TransactionQueueManager → [Queue for Distribution]
      ↓
GossipProtocolEngine → [Select Fanout Peers (e.g., 3 peers)]
      ↓
TransactionDistributionService → [Send to Peers via gRPC Stream]
      ↓
Peer 1 → [Validate, Forward to 3 more peers (Round 2)]
Peer 2 → [Validate, Forward to 3 more peers (Round 2)]
Peer 3 → [Validate, Forward to 3 more peers (Round 2)]
      ↓
Exponential propagation across network (3 rounds = ~40 nodes)
      ↓
Register Service → [Store Confirmed Transactions]
```

### Gossip Protocol

The Peer Service implements **epidemic gossip** for efficient transaction propagation:

- **Fanout Factor**: Each node forwards to N peers (default: 3)
- **Gossip Rounds**: Number of forwarding hops (default: 3)
- **Deduplication**: Transaction cache prevents re-propagation
- **Probabilistic Coverage**: High probability of reaching all nodes

**Example Propagation:**
```
Round 1: Node A → Nodes B, C, D (3 nodes)
Round 2: B→E,F,G, C→H,I,J, D→K,L,M (9 nodes)
Round 3: E→N,O,P, F→Q,R,S, ... (27 nodes)

Total: 1 + 3 + 9 + 27 = 40 nodes reached in 3 rounds
```

### NAT Traversal (STUN)

The Peer Service uses **STUN (Session Traversal Utilities for NAT)** to discover external addresses:

```
Local Node (192.168.1.100:5000)
      ↓
StunClient → [Query stun.l.google.com:19302]
      ↓
STUN Response: External Address = 203.0.113.5:12345
      ↓
Register external address with bootstrap nodes
```

**Supported STUN Servers:**
- `stun.l.google.com:19302` (default)
- `stun1.l.google.com:19302`
- `stun2.l.google.com:19302`

---

## Quick Start

### Prerequisites

- **.NET 10 SDK** or later
- **Git**
- *Optional*: **Access to public STUN servers** (for NAT traversal)

### 1. Clone and Navigate

```bash
git clone https://github.com/yourusername/Sorcha.git
cd Sorcha/src/Services/Sorcha.Peer.Service
```

### 2. Set Up Configuration

The service uses `appsettings.json` for configuration. For local development, defaults are pre-configured.

### 3. Run the Service

```bash
dotnet run
```

Service will start at:
- **gRPC**: `http://localhost:5000` (HTTP/2, primary protocol)
- **REST**: `https://localhost:5001` (HTTP/1.1, fallback)
- **Scalar API Docs**: `https://localhost:5001/scalar`
- **Health Checks**: `https://localhost:5001/health`

### 4. Test gRPC Endpoints (Optional)

Using `grpcurl` (install from https://github.com/fullstorydev/grpcurl):

```bash
# List available gRPC services
grpcurl -plaintext localhost:5000 list

# Ping a peer
grpcurl -plaintext -d '{"peer_id": "test-node"}' localhost:5000 PeerDiscovery/Ping

# Get peer list
grpcurl -plaintext -d '{"requesting_peer_id": "my-node", "max_peers": 10}' \
  localhost:5000 PeerDiscovery/GetPeerList
```

---

## Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Grpc": "Information"
    }
  },
  "AllowedHosts": "*",
  "PeerService": {
    "Enabled": true,
    "NodeId": "node-12345",
    "ListenPort": 5001,
    "NetworkAddress": {
      "ExternalAddress": null,
      "StunServers": [
        "stun.l.google.com:19302",
        "stun1.l.google.com:19302"
      ],
      "HttpLookupServices": [
        "https://api.ipify.org",
        "https://icanhazip.com"
      ],
      "PreferredProtocol": "IPv4"
    },
    "PeerDiscovery": {
      "BootstrapNodes": [
        "bootstrap1.sorcha.io:5000",
        "bootstrap2.sorcha.io:5000"
      ],
      "RefreshIntervalMinutes": 15,
      "MaxPeersInList": 1000,
      "MinHealthyPeers": 5,
      "PeerTimeoutSeconds": 30,
      "MaxConcurrentDiscoveries": 10
    },
    "Communication": {
      "PreferredProtocol": "GrpcStream",
      "ConnectionTimeout": 30,
      "MaxRetries": 3,
      "RetryDelaySeconds": 5,
      "CircuitBreakerThreshold": 5,
      "CircuitBreakerResetMinutes": 5
    },
    "TransactionDistribution": {
      "FanoutFactor": 3,
      "GossipRounds": 3,
      "TransactionCacheTTL": 3600,
      "MaxTransactionSize": 10485760,
      "StreamingThreshold": 1048576,
      "EnableCompression": true
    },
    "OfflineMode": {
      "MaxQueueSize": 10000,
      "MaxRetries": 5,
      "QueuePersistence": true,
      "PersistencePath": "./data/tx_queue.db"
    }
  },
  "OpenTelemetry": {
    "ServiceName": "Sorcha.Peer.Service",
    "ZipkinEndpoint": "http://localhost:9411"
  }
}
```

### Environment Variables

For production deployment:

```bash
# Node configuration
PEERSERVICE__NODEID="node-prod-001"
PEERSERVICE__LISTENPORT=5001

# External address (if known, skips STUN/HTTP lookup)
PEERSERVICE__NETWORKADDRESS__EXTERNALADDRESS="203.0.113.5:5000"

# Bootstrap nodes
PEERSERVICE__PEERDISCOVERY__BOOTSTRAPNODES__0="bootstrap1.sorcha.io:5000"
PEERSERVICE__PEERDISCOVERY__BOOTSTRAPNODES__1="bootstrap2.sorcha.io:5000"

# Gossip configuration
PEERSERVICE__TRANSACTIONDISTRIBUTION__FANOUTFACTOR=5
PEERSERVICE__TRANSACTIONDISTRIBUTION__GOSSIPROUNDS=4

# Observability
OPENTELEMETRY__ZIPKINENDPOINT="https://zipkin.yourcompany.com"
```

---

## gRPC API

### PeerDiscovery Service

**Proto Definition**: `Protos/peer_discovery.proto`

| Method | Description | Request | Response |
|--------|-------------|---------|----------|
| `GetPeerList` | Retrieve list of known peers | `PeerListRequest` | `PeerListResponse` |
| `RegisterPeer` | Register this node with another peer | `RegisterPeerRequest` | `RegisterPeerResponse` |
| `Ping` | Health check for peer availability | `PingRequest` | `PingResponse` |
| `GetNodeInfo` | Get detailed node capabilities | `NodeInfoRequest` | `NodeInfoResponse` |

### TransactionDistribution Service

**Proto Definition**: `Protos/transaction_distribution.proto`

| Method | Description | Type | Request | Response |
|--------|-------------|------|---------|----------|
| `DistributeTransaction` | Send single transaction | Unary | `TransactionMessage` | `DistributionResponse` |
| `StreamTransactions` | Stream multiple transactions | Bidirectional Stream | `TransactionMessage` | `DistributionResponse` |
| `AnnounceTransaction` | Announce transaction availability | Unary | `TransactionAnnouncement` | `AcknowledgeResponse` |

### PeerCommunication Service

**Proto Definition**: `Protos/peer_communication.proto`

| Method | Description | Type |
|--------|-------------|------|
| `TestConnection` | Test connection quality | Unary |
| `NegotiateProtocol` | Negotiate communication protocol | Unary |
| `SendHeartbeat` | Keep-alive heartbeat | Unary |

---

## REST API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get service information |
| GET | `/health` | Health check endpoint |
| GET | `/scalar` | Interactive API documentation (dev only) |

---

## Development

### Project Structure

```
Sorcha.Peer.Service/
├── Program.cs                      # Service entry point, gRPC/REST configuration
├── Core/
│   ├── PeerServiceConfiguration.cs # Configuration models
│   ├── PeerNode.cs                 # Peer node model
│   ├── PeerCapabilities.cs         # Peer capability flags
│   └── TransactionNotification.cs  # Transaction message model
├── Discovery/
│   ├── PeerDiscoveryService.cs     # Bootstrap connection, peer discovery
│   ├── PeerDiscoveryServiceImpl.cs # gRPC service implementation
│   └── PeerListManager.cs          # Peer registry management
├── Distribution/
│   ├── GossipProtocolEngine.cs     # Epidemic gossip logic
│   ├── TransactionDistributionService.cs  # Transaction propagation
│   └── TransactionQueueManager.cs  # Offline queue management
├── Communication/
│   ├── CommunicationProtocolManager.cs  # Protocol selection
│   └── ConnectionTestingService.cs      # Bandwidth/latency testing
├── Monitoring/
│   ├── HealthMonitorService.cs     # Peer health checks
│   └── ConnectionQualityTracker.cs # Latency/bandwidth tracking
├── Network/
│   ├── NetworkAddressService.cs    # External address discovery
│   └── StunClient.cs               # STUN protocol client
├── Protos/
│   ├── peer_discovery.proto        # gRPC service definitions
│   ├── transaction_distribution.proto
│   └── peer_communication.proto
└── PeerService.cs                  # Background service (hosted service)
```

### Running Tests

```bash
# Run all Peer Service tests
dotnet test tests/Sorcha.Peer.Service.Tests
dotnet test tests/Sorcha.Peer.Service.Integration.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (auto-rerun on changes)
dotnet watch test --project tests/Sorcha.Peer.Service.Tests
```

### Code Coverage

**Current Coverage**: ~70%
**Tests**: 14 test classes
- **Unit Tests**: 8 test classes (Core, Network, Monitoring, Discovery)
- **Integration Tests**: 4 test classes (Communication, Discovery, Health, Throughput)
**Lines of Code**: ~5,200 LOC

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

Open `coverage/index.html` in your browser.

---

## Integration with Other Services

### Register Service Integration

The Peer Service integrates with the Register Service for:
- **Transaction Confirmation**: Forward gossip transactions to Register for storage
- **Register Discovery**: Discover which registers are advertised to the network
- **State Synchronization**: Sync register state with peers

**Communication**: gRPC + Events
**Events Published**: `TransactionReceived`, `PeerConnected`, `PeerDisconnected`

### Blueprint Service Integration

The Peer Service integrates with the Blueprint Service for:
- **Action Propagation**: Distribute blueprint action transactions across the network
- **Blueprint Discovery**: Share published blueprint definitions

**Communication**: Event-driven messaging
**Events Subscribed**: `ActionSubmitted`, `BlueprintPublished`

### Validator Service Integration

The Peer Service integrates with the Validator Service for:
- **Consensus Coordination**: Facilitate consensus voting across peer network
- **Docket Propagation**: Distribute sealed dockets to peers

**Communication**: gRPC streaming
**Events Subscribed**: `DocketSealed`, `ConsensusRoundStarted`

### Example gRPC Client (C#)

```csharp
using Grpc.Net.Client;
using Sorcha.Peer.Service.Protos;

var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new PeerDiscovery.PeerDiscoveryClient(channel);

// Get peer list
var request = new PeerListRequest
{
    RequestingPeerId = "my-node-id",
    MaxPeers = 50
};

var response = await client.GetPeerListAsync(request);

Console.WriteLine($"Received {response.Peers.Count} peers:");
foreach (var peer in response.Peers)
{
    Console.WriteLine($"  - {peer.PeerId} at {peer.Address}:{peer.Port}");
}
```

### Example gRPC Client (TypeScript/Node.js)

```typescript
import * as grpc from '@grpc/grpc-js';
import * as protoLoader from '@grpc/proto-loader';

const packageDefinition = protoLoader.loadSync('peer_discovery.proto');
const proto = grpc.loadPackageDefinition(packageDefinition);

const client = new proto.PeerDiscovery(
  'localhost:5000',
  grpc.credentials.createInsecure()
);

client.GetPeerList({ requesting_peer_id: 'my-node', max_peers: 50 }, (error, response) => {
  if (error) {
    console.error('Error:', error);
  } else {
    console.log(`Received ${response.peers.length} peers`);
  }
});
```

---

## NAT Traversal and Network Configuration

### STUN Configuration

The Peer Service uses STUN to discover external addresses behind NAT:

```json
{
  "PeerService": {
    "NetworkAddress": {
      "StunServers": [
        "stun.l.google.com:19302",
        "stun1.l.google.com:19302",
        "stun2.l.google.com:19302"
      ]
    }
  }
}
```

### Manual External Address

For environments where STUN is unavailable or unreliable:

```json
{
  "PeerService": {
    "NetworkAddress": {
      "ExternalAddress": "203.0.113.5:5000"
    }
  }
}
```

### Firewall Configuration

**Required Ports:**
- **Inbound**: Port 5000 (gRPC, HTTP/2)
- **Inbound**: Port 5001 (REST, HTTP/1.1)
- **Outbound**: Port 19302 (STUN)
- **Outbound**: Ports 5000-5001 (peer connections)

**Firewall Rules (Linux iptables):**
```bash
# Allow gRPC
iptables -A INPUT -p tcp --dport 5000 -j ACCEPT

# Allow REST
iptables -A INPUT -p tcp --dport 5001 -j ACCEPT

# Allow STUN
iptables -A OUTPUT -p udp --dport 19302 -j ACCEPT
```

---

## Offline Mode and Transaction Queueing

### Queue Configuration

```json
{
  "PeerService": {
    "OfflineMode": {
      "MaxQueueSize": 10000,
      "MaxRetries": 5,
      "QueuePersistence": true,
      "PersistencePath": "./data/tx_queue.db"
    }
  }
}
```

### Queue Behavior

- **Offline Detection**: Peer Service detects network unavailability
- **Queue Transactions**: Store transactions in local SQLite database
- **Automatic Retry**: Periodically attempt to reconnect and flush queue
- **Max Retries**: After 5 failed retries, transaction is marked as failed
- **Queue Size Limit**: Maximum 10,000 transactions queued (configurable)

### Queue Management

```bash
# Check queue status
curl https://localhost:5001/api/queue/status

# Manually flush queue
curl -X POST https://localhost:5001/api/queue/flush

# Clear failed transactions
curl -X DELETE https://localhost:5001/api/queue/failed
```

---

## Security Considerations

### Authentication (Production)

- **Current**: Development mode (no authentication required)
- **Production**: Mutual TLS (mTLS) for peer-to-peer authentication
- **Node Identity**: Public key-based node identification

### Authorization

- **Peer Verification**: Validate peer signatures before accepting transactions
- **Bootstrap Trust**: Establish trust chain from bootstrap nodes

### Data Protection

- **TLS 1.3**: All gRPC and REST communications encrypted
- **Transaction Signatures**: Validate cryptographic signatures
- **No Sensitive Logging**: Never log transaction payloads or private keys

### Secrets Management

- **Node Private Keys**: Store in Azure Key Vault or secure storage
- **TLS Certificates**: Rotate certificates every 90 days
- **Bootstrap Trust**: Verify bootstrap node identities

---

## Deployment

### .NET Aspire (Development)

The Peer Service is registered in the Aspire AppHost:

```csharp
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis);
```

Start the entire platform:

```bash
dotnet run --project src/Apps/Sorcha.AppHost
```

Access Aspire Dashboard: `http://localhost:15888`

### Docker

```bash
# Build Docker image
docker build -t sorcha-peer-service:latest -f src/Services/Sorcha.Peer.Service/Dockerfile .

# Run container
docker run -d \
  -p 5000:5000 \
  -p 5001:5001 \
  -e PeerService__NodeId="docker-node-001" \
  -e PeerService__PeerDiscovery__BootstrapNodes__0="bootstrap.sorcha.io:5000" \
  --name peer-service \
  sorcha-peer-service:latest
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sorcha-peer-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: sorcha-peer-service
  template:
    metadata:
      labels:
        app: sorcha-peer-service
    spec:
      containers:
      - name: peer-service
        image: sorcha-peer-service:latest
        ports:
        - containerPort: 5000
          name: grpc
          protocol: TCP
        - containerPort: 5001
          name: http
          protocol: TCP
        env:
        - name: PeerService__NodeId
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: PeerService__PeerDiscovery__BootstrapNodes__0
          value: "bootstrap.sorcha.svc.cluster.local:5000"
---
apiVersion: v1
kind: Service
metadata:
  name: sorcha-peer-service
spec:
  type: LoadBalancer
  ports:
  - port: 5000
    targetPort: 5000
    name: grpc
  - port: 5001
    targetPort: 5001
    name: http
  selector:
    app: sorcha-peer-service
```

---

## Observability

### Logging (Serilog + Seq)

Structured logging with Serilog:

```csharp
Log.Information("Discovered {Count} peers from {BootstrapNode}", count, bootstrapNode);
Log.Warning("Peer {PeerId} failed health check: {Reason}", peerId, reason);
```

**Log Sinks**:
- Console (development)
- Seq (production) - `http://localhost:5341`

### Tracing (OpenTelemetry + Zipkin)

Distributed tracing with OpenTelemetry:

```bash
# View traces in Zipkin
open http://localhost:9411
```

**Traced Operations**:
- gRPC method calls
- Peer discovery operations
- Transaction gossip propagation
- STUN queries
- Health checks

### Metrics (Prometheus)

Metrics exposed at `/metrics`:
- Peer connection count (active, discovered, healthy)
- Transaction propagation rate
- Gossip round latency
- STUN query success rate
- Connection quality metrics (latency, bandwidth)
- Queue size (offline mode)

---

## Troubleshooting

### Common Issues

**Issue**: Cannot discover external address (NAT traversal fails)
**Solution**: Verify STUN server connectivity. Try manual external address configuration.

```bash
# Test STUN connectivity
nslookup stun.l.google.com
ping stun.l.google.com

# Manual configuration
"ExternalAddress": "your-public-ip:5000"
```

**Issue**: Bootstrap nodes unreachable
**Solution**: Verify bootstrap node addresses and network connectivity.

```bash
# Test gRPC connectivity
grpcurl -plaintext bootstrap1.sorcha.io:5000 list
```

**Issue**: Peers not discovering this node
**Solution**: Check firewall rules, ensure ports 5000-5001 are open inbound.

**Issue**: High transaction propagation latency
**Solution**: Increase gossip fanout factor and verify network bandwidth.

```json
{
  "TransactionDistribution": {
    "FanoutFactor": 5,
    "GossipRounds": 4
  }
}
```

**Issue**: Offline queue filling up
**Solution**: Increase queue size or reduce `MaxRetries`. Check network connectivity.

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Sorcha.Peer.Service": "Trace",
      "Grpc": "Debug"
    }
  }
}
```

---

## Contributing

### Development Workflow

1. **Create a feature branch**: `git checkout -b feature/your-feature`
2. **Make changes**: Follow C# coding conventions
3. **Write tests**: Maintain >70% coverage
4. **Run tests**: `dotnet test`
5. **Format code**: `dotnet format`
6. **Commit**: `git commit -m "feat: your feature description"`
7. **Push**: `git push origin feature/your-feature`
8. **Create PR**: Reference issue number

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Include unit tests for all business logic
- Use dependency injection for testability

---

## Resources

- **Architecture**: [docs/architecture.md](../../docs/architecture.md)
- **Development Status**: [docs/development-status.md](../../docs/development-status.md)
- **gRPC Documentation**: https://grpc.io/docs/languages/csharp/
- **STUN RFC 5389**: https://tools.ietf.org/html/rfc5389
- **Gossip Protocols**: https://en.wikipedia.org/wiki/Gossip_protocol

---

## Technology Stack

**Runtime:**
- .NET 10.0 (10.0.100)
- C# 13
- ASP.NET Core 10

**Frameworks:**
- gRPC for .NET (Grpc.AspNetCore)
- Protocol Buffers (Google.Protobuf)
- .NET Aspire 13.0+ for orchestration

**Networking:**
- HTTP/2 (gRPC primary protocol)
- HTTP/1.1 (REST fallback)
- STUN protocol for NAT traversal

**Observability:**
- OpenTelemetry for distributed tracing
- Serilog for structured logging
- Prometheus metrics

**Testing:**
- xUnit for test framework
- FluentAssertions for assertions
- Moq for mocking

---

## License

Apache License 2.0 - See [LICENSE](../../LICENSE) for details.

---

**Last Updated**: 2025-11-23
**Maintained By**: Sorcha Platform Team
**Status**: ⚙️ Core Complete (65% - Production Infrastructure Pending)
