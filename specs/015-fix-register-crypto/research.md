# Research: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Branch**: `015-fix-register-crypto` | **Date**: 2026-01-27

## R1: Double-Hashing Bug Diagnosis

**Decision:** There is no double-hashing bug. The sign and verify paths are symmetric.

**Rationale:** Detailed code trace through the complete data flow:
- **Sign path:** Client sends canonical JSON bytes -> `TransactionService.SignTransactionAsync` computes `SHA256(canonicalJsonBytes)` -> `CryptoModule.SignAsync` signs the hash. For ED25519, `PublicKeyAuth.SignDetached(hash, privateKey)` signs whatever bytes are passed (Ed25519 internally applies SHA-512 as part of EdDSA, but this is symmetric between sign and verify).
- **Verify path:** `RegisterCreationOrchestrator.VerifyAttestationsAsync` re-serializes attestation data to canonical JSON -> computes `SHA256(canonicalJsonBytes)` -> `CryptoModule.VerifyAsync(signature, hash, algorithm, publicKey)`. For ED25519, `PublicKeyAuth.VerifyDetached(signature, hash, publicKey)`.
- Both paths hash exactly once with SHA-256, then pass the hash to the crypto module which does NOT re-hash.

**Alternatives considered:** The walkthrough README (2026-01-05) claimed a double-hashing bug. Upon code audit, this was incorrect. The real blockers are: (a) JSON canonicalization fragility during re-serialization, (b) stubbed wallet service client returning placeholder signatures, (c) stubbed system wallet signing in the Validator.

## R2: Service-to-Service JWT Authentication

**Decision:** Create a reusable `ServiceAuthClient` in `Sorcha.ServiceClients` that implements the OAuth2 `client_credentials` flow against the Tenant Service's `/api/service-auth/token` endpoint.

**Rationale:** The Tenant Service already provides a full OAuth2-compliant token endpoint. All JWT infrastructure (signing key, validation, claims) is shared across services via `JwtAuthenticationExtensions`. The only missing piece is the client-side token acquisition. A shared client avoids duplicating this logic in every service.

**Key findings:**
- Tenant Service endpoint: `POST /api/service-auth/token` with `grant_type=client_credentials`, `client_id`, `client_secret`
- Token claims: `sub`, `client_id`, `service_name`, `token_type=service`, plus `scope` claims
- Token lifetime: configurable via `JwtSettings.ServiceTokenLifetimeHours` (default 8 hours)
- Service principals registered via `POST /api/service-principals/` with scopes like `wallet:sign`
- No existing service auth client in the codebase -- all current service clients are stubs or unauthenticated

**Design:**
- `ServiceAuthClient` caches the token in-memory, refreshes when within 5 minutes of expiry
- Configured via `ServiceAuth:ClientId`, `ServiceAuth:ClientSecret`, `ServiceAuth:TenantServiceUrl`
- Thread-safe token refresh using `SemaphoreSlim`

## R3: WalletServiceClient De-Stub Strategy

**Decision:** Rewrite `WalletServiceClient` to inject `HttpClient` via `AddHttpClient<T>()` and make real HTTP calls to the Wallet Service REST API. Use `IServiceAuthClient` for Bearer token authentication.

**Rationale:** Two wallet client patterns exist in the codebase:
1. `IWalletServiceClient` (shared, in `Sorcha.ServiceClients`) -- currently a stub, used by Register Service and Validator Service
2. `IWalletIntegrationService` (Validator-specific, uses gRPC) -- a real client with key caching and local signing optimization

The shared `IWalletServiceClient` should use HTTP because: (a) the Wallet Service has a mature REST API with sign endpoints; (b) HTTP is simpler to add auth headers to than gRPC interceptors; (c) the gRPC proto for signing (`SignData`) is less feature-rich than the REST endpoint (no derivation path support in proto). The `IWalletIntegrationService` can continue to exist for the Validator's high-frequency docket/vote signing (where gRPC + local key caching is a performance win), while `IWalletServiceClient` handles genesis-level signing.

**Key REST endpoint:** `POST /api/v1/wallets/{address}/sign`
- Request: `{ "transactionData": "<base64>", "derivationPath": "<path>", "isPreHashed": true/false }`
- Response: `{ "signature": "<base64>", "signedBy": "<address>", "signedAt": "<datetime>", "publicKey": "<base64>" }`
- Auth: `CanManageWallets` policy (JWT Bearer)

**Return type change:** `SignTransactionAsync` currently returns `Task<string>`. Must change to `Task<WalletSignResult>` where `WalletSignResult` contains `Signature` (byte[]), `PublicKey` (byte[]), `SignedBy` (string), `Algorithm` (string).

## R4: Atomic Register + Genesis Strategy

**Decision:** Submit genesis transaction to Validator BEFORE persisting the register to MongoDB. Only persist after genesis succeeds.

**Rationale:**
- MongoDB does not support multi-document ACID transactions across different collections in a way that would allow rollback of a register if genesis submission fails
- The simpler alternative (persist first, delete on failure) risks orphaned registers if the delete also fails
- Submitting genesis first means: if genesis fails, the register simply doesn't exist -- clean state
- If genesis succeeds but register persist fails (unlikely but possible), the genesis transaction exists in the mempool but has no register -- this is recoverable by retrying the persist, or the mempool entry will expire

**Impact on FinalizeAsync flow:**
1. Verify attestations (unchanged)
2. Build control record (unchanged)
3. Build genesis transaction model (unchanged)
4. Submit genesis to Validator (moved earlier)
5. If genesis succeeds: persist register to MongoDB
6. If genesis fails: return error, no register persisted

## R5: Pre-Hashed Signing Design

**Decision:** Add `bool isPreHashed = false` parameter to `TransactionService.SignTransactionAsync` and corresponding `IsPreHashed` property to `SignTransactionRequest` DTO.

**Rationale:** The `TransactionService` currently always applies SHA-256 before signing. For register attestation signing, the orchestrator computes the hash and sends it as `dataToSign`. The wallet must sign these bytes directly without re-hashing. A boolean flag is the simplest approach -- it preserves all existing behavior when `false` and enables the new hash-signing flow when `true`.

**Alternatives considered:**
- Separate endpoint (`/api/v1/wallets/{address}/sign-hash`): More explicit but adds endpoint proliferation. Rejected.
- Always sign raw data, move hashing to caller: Would break all existing callers. Rejected.
- Detect hash by length (32 bytes = SHA-256): Fragile heuristic. Rejected.

**Propagation path:** `SignTransactionRequest.IsPreHashed` -> `WalletEndpoints` -> `WalletManager.SignTransactionAsync` -> `TransactionService.SignTransactionAsync` -> conditional hash skip.

## R6: API Gateway Routing Fix

**Decision:** Add a specific high-priority route for `/api/validator/genesis` that passes through without path transformation.

**Rationale:** The current catch-all route `/api/validator/{**catch-all}` transforms the path to `/api/v1/{**catch-all}`. The genesis endpoint on the Validator Service is mapped at `/api/validator/genesis` (not `/api/v1/genesis`). YARP evaluates routes by order, so a specific route defined with a lower `Order` value takes priority over the catch-all.

**Fix:** Add route `validator-genesis-route` with `Match.Path = "/api/validator/genesis"` and no path transformation, targeting `validator-cluster`. Place it before the catch-all route in the route list.
