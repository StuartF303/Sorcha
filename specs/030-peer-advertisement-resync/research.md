# Research: Register-to-Peer Advertisement Resync

**Branch**: `030-peer-advertisement-resync` | **Date**: 2026-02-10

## R1: Redis Integration Pattern for Peer Service

**Decision**: Use `IConnectionMultiplexer` directly (not `IDistributedCache`) for the Redis advertisement store.

**Rationale**: The advertisement store requires TTL per key, batch operations (get all keys by pattern), and atomic set+expire. `IConnectionMultiplexer.GetDatabase()` provides `StringSet` with `TimeSpan` expiry, `KeyDeleteAsync` for batch removal, and `SCAN` via `server.Keys()` for pattern matching. `IDistributedCache` lacks pattern-based operations needed for "get all advertisements" on startup.

**Alternatives considered**:
- `IDistributedCache`: Simpler API but no pattern-based key scanning; would require maintaining a separate index key listing all advertisement keys.
- `ICacheStore` (Sorcha.Storage.Redis): Adds resilience pipeline but introduces a dependency on Sorcha.Storage.Redis; the Peer Service doesn't currently reference this project and the added abstraction isn't justified for a single store.

**Existing pattern**: `PendingRegistrationStore` in Register Service uses the exact same approach — `IConnectionMultiplexer` → `GetDatabase()` → `StringSet`/`StringGet` with TTL via `KeyExpire`.

## R2: Peer Service Redis Configuration

**Decision**: Add `Aspire.StackExchange.Redis` NuGet package and `builder.AddRedisClient("redis")` to Peer Service Program.cs.

**Rationale**: The AppHost already wires Redis to the Peer Service via `.WithReference(redis)` (AppHost.cs line 53-54). The Peer Service just needs the client package and DI registration to resolve `IConnectionMultiplexer`.

**Alternatives considered**: None — this is the standard Aspire pattern used by Register Service, Validator Service, and Tenant Service.

## R3: Unified vs Separate Storage Pools

**Decision**: Single unified Redis hash/key namespace for both local and remote advertisements, distinguished by a `source` field in the serialized value.

**Rationale**: The spec requires a single pool with 5-minute TTL. Local advertisements are refreshed by the Register Service reconciliation push (every 5 min). Remote advertisements are refreshed by gossip heartbeat exchanges (every 5 min). Entries not refreshed naturally expire via TTL.

**Key structure**:
- Key pattern: `peer:advert:{registerId}:{source}` where source is `local` or a peer ID
- Value: JSON-serialized advertisement data (register ID, sync state, version, docket version, isPublic, timestamp)
- TTL: 5 minutes (300 seconds), refreshed on each write

**Alternatives considered**:
- Redis Hash per register (HSET/HGETALL): More complex, harder to set per-field TTL. Redis TTL is per-key only.
- Separate key spaces for local vs remote: Would require two SCAN operations on startup; unified namespace is simpler.

## R4: Background Service Pattern for Register Service

**Decision**: Implement `AdvertisementResyncService` as a `BackgroundService` (derives from `Microsoft.Extensions.Hosting.BackgroundService`) in the Register Service.

**Rationale**: The Register Service currently has zero background services. `BackgroundService` is the standard .NET pattern for periodic work — override `ExecuteAsync` with a `while (!stoppingToken.IsCancellationRequested)` loop and `Task.Delay` between iterations. First iteration runs immediately on startup (the startup push), subsequent iterations run every 5 minutes (reconciliation).

**Alternatives considered**:
- `IHostedService` with manual timer: More boilerplate, no benefit over `BackgroundService`.
- Separate startup and reconciliation services: Unnecessary complexity — startup push IS the first reconciliation cycle.

## R5: Bulk Advertisement Endpoint Design

**Decision**: Add `POST /api/registers/bulk-advertise` endpoint to the Peer Service that accepts a list of advertisements with an optional `fullSync` flag.

**Rationale**: The existing `POST /api/registers/{registerId}/advertise` handles single registers. A bulk endpoint avoids N+1 HTTP calls during startup resync. The `fullSync` flag (FR-011) enables the Register Service to say "this is the complete set of local registers — remove any local ads not in this list."

**Alternatives considered**:
- Reuse existing single-register endpoint in a loop: Inefficient for 100+ registers (100 HTTP roundtrips vs 1).
- gRPC streaming: The Peer Service has gRPC capabilities, but the advertisement path is HTTP-based. Mixing protocols adds complexity for minimal benefit.

## R6: GetNetworkAdvertisedRegisters Impact

**Decision**: `GetNetworkAdvertisedRegisters()` must be refactored to read from Redis (the unified pool) instead of scanning `PeerNode.AdvertisedRegisters` on each peer.

**Rationale**: Currently, this method iterates all peers and their in-memory `AdvertisedRegisters` lists. With the unified Redis store, all advertisements (local + remote) live in Redis with an in-memory cache. The method should query the cache/store rather than peer objects.

**Impact**: The `PeerNode.AdvertisedRegisters` property may become redundant for the Available Registers endpoint. However, it's still used by:
- `PeerHeartbeatGrpcService` — to send/receive heartbeat data
- `PeerExchangeService` — for gossip exchanges
- `DetectVersionLag()` — for sync decisions

The gossip/heartbeat path should continue writing to both `PeerNode.AdvertisedRegisters` AND the Redis store, keeping them synchronized.

## R7: In-Memory Cache Strategy

**Decision**: Maintain the existing `ConcurrentDictionary<string, LocalRegisterAdvertisement>` as a read-through cache of Redis state. On startup, populate from Redis. On write, write-through to both Redis and memory.

**Rationale**: The `GetLocalAdvertisements()` method is called on every heartbeat (frequent, performance-sensitive). Reading from Redis on every heartbeat would add unnecessary latency. The in-memory cache provides O(1) lookups while Redis provides durability.

**Consistency model**: Write-through (Redis + memory on write), read-from-memory. On startup, seed from Redis. TTL expiry in Redis means the memory cache may temporarily hold stale entries — acceptable because the 5-minute reconciliation cycle refreshes everything.

## R8: Existing Test Baseline

**Decision**: Extend existing `RegisterAdvertisementServiceTests` (18 tests) and add new test classes for `RedisAdvertisementStore` and `AdvertisementResyncService`.

**Rationale**: The existing 18 tests use real `RegisterAdvertisementService` and `PeerListManager` instances with only loggers mocked. The refactored service will take an additional `IRedisAdvertisementStore` dependency (or `IConnectionMultiplexer`). Existing tests need the new dependency added (mock or in-memory fake). New tests validate Redis serialization, TTL behavior, bulk operations, and the resync background service.

**Current baselines**: Peer Service 504 pass, Register Service (check during implementation).
