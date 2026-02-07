# Quickstart: Peer Network Management & Observability

**Feature**: 024-peer-network-management

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL, Redis)
- Sorcha solution builds: `dotnet restore && dotnet build`

## Development Workflow

### 1. Start Infrastructure

```bash
docker-compose up -d postgres redis
```

### 2. Run Peer Service

```bash
dotnet run --project src/Services/Sorcha.Peer.Service
# Listens on port 8080 (REST) and 5000 (gRPC)
```

### 3. Test New Endpoints

```bash
# List peers with enhanced data (quality, registers, ban status)
curl http://localhost:8080/api/peers | jq

# View quality scores
curl http://localhost:8080/api/peers/quality | jq

# View available registers in the network
curl http://localhost:8080/api/registers/available | jq

# View current subscriptions
curl http://localhost:8080/api/registers/subscriptions | jq

# Subscribe to a register (requires JWT)
curl -X POST http://localhost:8080/api/registers/reg-123/subscribe \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"mode": "forward-only"}'

# Ban a peer (requires JWT)
curl -X POST http://localhost:8080/api/peers/node-abc/ban \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Serving corrupt data"}'

# Reset failure count (requires JWT)
curl -X POST http://localhost:8080/api/peers/node-abc/reset \
  -H "Authorization: Bearer $TOKEN"
```

### 4. Test CLI Commands

```bash
# Build CLI
dotnet build src/Apps/Sorcha.Cli

# List subscriptions
sorcha peer subscriptions

# Subscribe to a register
sorcha peer subscribe --register-id reg-123 --mode full-replica

# View quality scores
sorcha peer quality

# Ban a peer
sorcha peer ban --peer-id node-abc --reason "Bad data"

# Reset a peer
sorcha peer reset --peer-id node-abc
```

### 5. Test UI

```bash
# Run with Aspire (full stack)
dotnet run --project src/Apps/Sorcha.AppHost

# Navigate to http://localhost/app/admin → Peer Service tab
```

### 6. Run Tests

```bash
# Peer service tests
dotnet test tests/Sorcha.Peer.Service.Tests

# CLI tests (if applicable)
dotnet test tests/Sorcha.Cli.Tests
```

## Key Files to Modify

| File | Change |
|------|--------|
| `src/Services/Sorcha.Peer.Service/Core/PeerNode.cs` | Add IsBanned, BannedAt, BanReason |
| `src/Services/Sorcha.Peer.Service/data/PeerDbContext.cs` | Add ban columns to entity config |
| `src/Services/Sorcha.Peer.Service/Discovery/PeerListManager.cs` | Add BanPeerAsync, UnbanPeerAsync, ResetFailureCountAsync |
| `src/Services/Sorcha.Peer.Service/Replication/RegisterAdvertisementService.cs` | Add GetNetworkAdvertisedRegisters() |
| `src/Services/Sorcha.Peer.Service/Program.cs` | Add ~10 new REST endpoints |
| `src/Apps/Sorcha.Cli/Commands/PeerCommands.cs` | Add 6 new subcommands |
| `src/Apps/Sorcha.Cli/Services/IPeerServiceClient.cs` | Add Refit methods |
| `src/Apps/Sorcha.Cli/Models/Peer.cs` | Add DTOs |
| `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/PeerServiceAdmin.razor` | Enhance with 4 panels |
| `src/Services/Sorcha.ApiGateway/appsettings.json` | Add registers route |
