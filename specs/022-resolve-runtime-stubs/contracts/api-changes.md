# API Contract Changes: 022-resolve-runtime-stubs

**Date**: 2026-02-07

## Behavioral Changes (No New Endpoints)

This feature modifies the behavior of existing endpoints and internal services. No new REST or gRPC endpoints are introduced.

### Wallet Service — Authorization Enforcement

**Affected Endpoints**:
- `GET /api/wallets/{address}` — Now returns 403 if caller is not owner or delegate
- `GET /api/wallets` — Unchanged (already filtered by owner)
- `POST /api/wallets/delegation/grant` — Now extracts user from JWT (was falling through to "anonymous")
- `DELETE /api/wallets/delegation/{subject}/{walletAddress}` — Same JWT extraction fix
- `PUT /api/wallets/delegation/{accessId}` — Now functional (was throwing NotImplementedException)

**New Error Responses**:
- `401 Unauthorized` — When no valid JWT identity is present (replaces "anonymous" fallback)
- `403 Forbidden` — When caller lacks ownership or delegation rights

### Wallet Service — Address Generation

**Affected Endpoint**:
- `POST /api/wallets/{address}/derive` — Now returns 400 with structured error instead of 500 from NotImplementedException

**Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Server-Side Derivation Not Supported",
  "detail": "Address generation requires the wallet mnemonic. Use RegisterDerivedAddressAsync for client-side derivation.",
  "status": 400
}
```

### Tenant Service — Bootstrap Token

**Affected Endpoint**:
- `POST /api/bootstrap` — Now returns JWT token in response (was deferring to separate login)

**Updated Response**:
```json
{
  "organizationId": "guid",
  "adminUserId": "guid",
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

### Transaction Serializer — Binary Format

**Affected Methods** (internal, not HTTP):
- `SerializeBinary()` — Now throws `NotSupportedException` instead of `NotImplementedException`
- `DeserializeBinary()` — Same change

The distinction matters: `NotSupportedException` indicates a deliberate design choice, `NotImplementedException` indicates incomplete code.

## Internal Service Contract Changes

### IWalletRepository — New Method

```csharp
Task<WalletAccess?> GetAccessByIdAsync(Guid accessId, CancellationToken cancellationToken = default);
```

### SignatureCollector — Real gRPC Calls

Replaces simulated responses with actual gRPC calls to `ValidatorService.RequestVote` on peer validators.

### RotatingLeaderElectionService — Heartbeat Broadcasting

Uses `IPeerServiceClient.PublishProposedDocketAsync` (or a new `BroadcastHeartbeatAsync` method) to announce leader status.

### ValidatorRegistry — On-Chain Registration

Creates transactions via `IRegisterServiceClient` for validator registration/approval/deregistration events.

### ValidationEngineService — Register Discovery

Calls `IRegisterMonitoringRegistry.GetMonitoredRegistersAsync()` to discover active registers.

### ConsensusFailureHandler — Persistence

Stores `ConsensusFailureRecord` in Redis with 30-day TTL.

### PendingRegistrationStore — Redis Backend

Same interface, Redis-backed implementation. Key: `register:pending:{registerId}`.
