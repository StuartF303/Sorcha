# Research: 022-resolve-runtime-stubs

**Date**: 2026-02-07
**Branch**: `022-resolve-runtime-stubs`

## Research Findings

### R1: JWT Claim Extraction Pattern

**Decision**: The existing `GetCurrentUser`/`GetCurrentTenant` helpers already extract from `ClaimsPrincipal` — the TODO markers are misleading. The actual implementation uses `context.User.FindFirstValue(ClaimTypes.NameIdentifier)` and `context.User.FindFirstValue("tenant")`. These are correct patterns for JWT claim extraction.

**What's actually needed**:
- Replace `"anonymous"` fallback with 401 Unauthorized when no identity present
- Add `"tenant_id"` as the standard claim name (matching Tenant Service token format)
- Add authorization checks (wallet ownership verification) — this is the real gap

**Rationale**: The code reads JWT claims correctly; the issue is that it falls through to defaults instead of rejecting unauthorized access.

**Alternatives considered**: Custom middleware for wallet authorization vs inline checks — inline is simpler and matches the existing Minimal API pattern throughout the codebase.

---

### R2: Wallet Repository — GetAccessById

**Decision**: Add `GetAccessByIdAsync(Guid accessId)` to `IWalletRepository` interface and implement in both `InMemoryWalletRepository` and `EfCoreWalletRepository`.

**Rationale**: The `DelegationService.UpdateAccessAsync` method needs to retrieve an access record by ID before updating it. The repository interface is at `src/Common/Sorcha.Wallet.Core/Repositories/Interfaces/IWalletRepository.cs`. Both implementations need the new method.

**Alternatives considered**: Query by wallet address + grantee combination instead of by ID — rejected because the endpoint receives the access ID directly, and querying by composite key is fragile.

---

### R3: Validator-Peer Integration Architecture

**Decision**: Use existing gRPC contracts in `validator.proto` (RequestVote, ExchangeSignature) via `IPeerServiceClient` for inter-validator communication. The Peer Service acts as a message relay.

**Rationale**: The proto contracts already define `VoteRequest/VoteResponse`, `SignatureExchangeRequest/SignatureExchangeResponse`, and `ReceiveConfirmedDocketRequest/Response`. The `ValidatorGrpcService` already implements the receiving side. What's missing is the **calling** side — the `SignatureCollector` currently simulates responses instead of calling peer validators via gRPC.

**Architecture**:
1. `SignatureCollector` → creates gRPC channel to peer validator endpoint → calls `RequestVote`
2. `RotatingLeaderElectionService` → uses `IPeerServiceClient.PublishProposedDocketAsync` for heartbeat-like status updates
3. `ValidatorRegistry` → creates on-chain registration transactions via `IRegisterServiceClient`

**Alternatives considered**: Direct validator-to-validator gRPC channels vs routing through Peer Service hub — direct is simpler since validator endpoints are already known from `ValidatorInfo.GrpcEndpoint`.

---

### R4: Peer Node Status — Data Sources

**Decision**: Inject `ISystemRegisterRepository` (or a lightweight cache) into Peer Service's `HeartbeatService` and `HubNodeConnectionService` to provide real system register version, blueprint counts, and timestamps.

**Rationale**: `ISystemRegisterRepository` already has `GetLatestVersionAsync()` and `GetBlueprintCountAsync()` (implemented in `MongoSystemRegisterRepository`). The `SystemRegisterCache` on peer nodes also tracks `GetCurrentVersion()` and `GetBlueprintCount()`. For uptime, use `Stopwatch.StartNew()` at service startup.

**Data source mapping**:
| Metric | Source |
|--------|--------|
| `CurrentSystemRegisterVersion` | `SystemRegisterCache.GetCurrentVersion()` or `ISystemRegisterRepository.GetLatestVersionAsync()` |
| `TotalBlueprints` | `SystemRegisterCache.GetBlueprintCount()` or `ISystemRegisterRepository.GetBlueprintCountAsync()` |
| `UptimeSeconds` | `Stopwatch` started at service initialization |
| `SessionId` | `IHubNodeConnectionManager.GetCurrentSessionId()` or generate at startup |
| `LastBlueprintPublishedAt` | `SystemRegisterCache` most recent entry timestamp |

**Alternatives considered**: Calling Register Service API for each status request — rejected, too expensive. Use local cache/repository.

---

### R5: PendingRegistrationStore — Redis Migration

**Decision**: Replace `ConcurrentDictionary` with Redis Hash operations following the `MemPoolManager` pattern.

**Rationale**: The `MemPoolManager` already uses `IConnectionMultiplexer.GetDatabase()` with Hash operations in the same service. The pattern is well-established: `HashSet` for add, `HashExists` for check, `HashDelete` for remove, `HashGetAll` for list.

**Key pattern**: `register:pending:{registerId}` → JSON serialized `PendingRegistration`

**Alternatives considered**:
- MongoDB: Overkill for ephemeral registration data with short TTL
- In-memory with distributed cache abstraction: `IDistributedCache` is too limited (no scan/enumerate)

---

### R6: Key Recovery and Keychain Export/Import

**Decision**: Implement using existing `Sorcha.Cryptography` infrastructure — `ICryptoModule.RecoverKeySetAsync` wraps NBitcoin mnemonic recovery, keychain export uses AES-256-GCM encryption (already available via `ISymmetricCrypto`).

**Rationale**: The crypto module already has `GenerateKeySetAsync` which creates key sets from mnemonics. Recovery is the reverse: take a mnemonic → derive the same key set. Export/import uses the existing `ISymmetricCrypto.EncryptAsync/DecryptAsync` to wrap keychain data with a password-derived key.

**Format**: Keychain export produces a JSON envelope encrypted with AES-256-GCM, key derived from password via PBKDF2 (standard practice).

**Alternatives considered**: Custom binary format — rejected in favor of encrypted JSON for debuggability and version flexibility.

---

### R7: Transaction Version Adapters

**Decision**: Implement version adapters as simple field-mapping transformers. V1→V4: add missing fields with defaults. V2→V4: add missing fields. V3→V4: minimal mapping.

**Rationale**: The `TransactionFactory` already has version detection logic. Each adapter is a small class that reads the version-specific JSON schema and maps to the current V4 model.

**Binary serialization**: Replace `NotImplementedException` with `NotSupportedException("Binary serialization is not supported. Use JSON format.")` — a non-crashing, clear response.

**Alternatives considered**: Full binary with VarInt — deferred per user decision, tracked as future TODO.

---

### R8: Bootstrap Token Generation

**Decision**: Use existing `ITokenService` from Tenant Service to generate JWT tokens during bootstrap.

**Rationale**: `ITokenService` (at `src/Services/Sorcha.Tenant.Service/Services/ITokenService.cs`) and `TokenService` already implement token generation. The bootstrap endpoint just needs to call `tokenService.GenerateTokenAsync(user)` after creating the admin user instead of deferring to a separate login step.

**Alternatives considered**: Return a one-time bootstrap token with limited scope — too complex for initial implementation. Standard JWT is fine since bootstrap is a one-time setup operation.

---

### R9: Consensus Failure Persistence

**Decision**: Store consensus failure records in Redis with a TTL-based retention policy (30 days default), following the `RegisterMonitoringRegistry` pattern.

**Rationale**: Consensus failures are diagnostic data, not critical ledger data. Redis provides simple persistence with automatic expiry. Key pattern: `validator:consensus:failures:{docketId}`.

**Alternatives considered**: MongoDB collection — overkill for diagnostic data. In-memory — loses data on restart, defeating the purpose.

---

### R10: Register Discovery for ValidationEngineService

**Decision**: Use `IRegisterMonitoringRegistry.GetMonitoredRegistersAsync()` which already tracks which registers the validator is responsible for.

**Rationale**: The `RegisterMonitoringRegistry` (Redis-backed) already maintains a set of register IDs that this validator monitors. The `ValidationEngineService` just needs to call this instead of using a hardcoded empty list.

**Alternatives considered**: Peer Service discovery of all registers — too broad; validators should only validate registers they're assigned to.
