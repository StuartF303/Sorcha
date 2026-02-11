# Quickstart: Register-to-Peer Advertisement Resync

**Branch**: `030-peer-advertisement-resync` | **Date**: 2026-02-10

## Prerequisites

- .NET 10 SDK
- Docker Desktop (running)
- Redis available (via `docker-compose up -d` or Aspire)

## Implementation Order

### Phase 1: Redis Advertisement Store (Peer Service)

1. Add `Aspire.StackExchange.Redis` to `Sorcha.Peer.Service.csproj`
2. Add `builder.AddRedisClient("redis")` to `Program.cs`
3. Create `RedisAdvertisementStore` class in `Replication/`:
   - Constructor takes `IConnectionMultiplexer`, `ILogger`
   - Methods: `SetLocalAsync`, `SetRemoteAsync`, `GetAllLocalAsync`, `GetAllRemoteAsync`, `RemoveLocalAsync`, `RemoveLocalExceptAsync` (for full-sync)
   - Key pattern: `peer:advert:local:{id}` and `peer:advert:remote:{peerId}:{id}`
   - TTL: 300 seconds on all writes
4. Refactor `RegisterAdvertisementService`:
   - Add `RedisAdvertisementStore` dependency
   - On `AdvertiseRegister()`: write to both memory + Redis
   - On `RemoveAdvertisement()`: remove from both memory + Redis
   - On startup: load from Redis → populate `_localAdvertisements`
   - On `ProcessRemoteAdvertisementsAsync()`: also write to Redis
5. Update existing tests (add Redis mock/dependency)
6. Write `RedisAdvertisementStoreTests`

### Phase 2: Bulk Advertisement Endpoint (Peer Service)

1. Add `POST /api/registers/bulk-advertise` endpoint to `Program.cs`
2. Accept `BulkAdvertiseRequest` body
3. For each advertisement: call `AdvertiseRegister()` or `RemoveAdvertisement()`
4. If `fullSync`: call `RemoveLocalExceptAsync()` for cleanup
5. Return `BulkAdvertiseResponse` with counts
6. Add OpenAPI documentation (.WithName, .WithSummary, .WithTags)

### Phase 3: PeerServiceClient Extension

1. Add `BulkAdvertiseAsync` to `IPeerServiceClient`
2. Implement in `PeerServiceClient`: `POST /api/registers/bulk-advertise`
3. Add `FullSync` parameter support

### Phase 4: Register Service Background Reconciliation

1. Create `AdvertisementResyncService : BackgroundService` in Register Service
2. `ExecuteAsync` loop:
   - Query `IRegisterRepository` for all registers with `Advertise == true`
   - Build `BulkAdvertiseRequest` with `FullSync = true`
   - Call `_peerClient.BulkAdvertiseAsync(request)`
   - Log results (structured logging)
   - `await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken)`
3. Retry logic: exponential backoff on `HttpRequestException` (Peer Service unavailable)
4. Register in DI: `builder.Services.AddHostedService<AdvertisementResyncService>()`
5. Write `AdvertisementResyncServiceTests`

### Phase 5: Refactor GetNetworkAdvertisedRegisters

1. Update `GetNetworkAdvertisedRegisters()` to aggregate from unified Redis store
2. Include both local and remote advertisements
3. Maintain existing `AvailableRegisterInfo` response shape

### Phase 6: Integration Testing & Verification

1. Run full Peer Service test suite (baseline: 504 pass)
2. Run Register Service tests
3. Manual Docker verification: restart services, verify Available Registers tab

## Verification Commands

```bash
# Build affected projects
dotnet build src/Services/Sorcha.Peer.Service
dotnet build src/Services/Sorcha.Register.Service
dotnet build src/Common/Sorcha.ServiceClients

# Run tests
dotnet test tests/Sorcha.Peer.Service.Tests
dotnet test tests/Sorcha.Register.Service.Tests

# Docker verification
docker-compose restart sorcha-peer-service
# Wait 5 seconds, then check:
curl http://localhost/api/peer/registers/available
# Should return previously advertised registers

docker-compose restart
# Wait 60 seconds, then check:
curl http://localhost/api/peer/registers/available
# Should return all public registers
```
