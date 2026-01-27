# Implementation Plan: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Branch**: `015-fix-register-crypto` | **Date**: 2026-01-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/015-fix-register-crypto/spec.md`

## Summary

Make the two-phase register creation flow (initiate/finalize) produce real cryptographic signatures end-to-end. The approach has four pillars: (1) change initiate to return SHA-256 hashes instead of canonical JSON, storing hashes for verification in finalize; (2) add `IsPreHashed` support to the Wallet Service sign endpoint; (3) de-stub `WalletServiceClient` into a real HTTP client with JWT service-to-service authentication; (4) make the Validator's genesis endpoint and GenesisManager use real wallet signing instead of placeholders. Additionally, remove the simple CRUD register creation endpoint, fix API Gateway routing, compute real payload hashes, and make register+genesis atomic.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: .NET Aspire 13, Sorcha.Cryptography (libsodium ED25519, NIST P-256, RSA-4096), Sorcha.ServiceClients, YARP 2.2.0
**Storage**: MongoDB (registers, transactions), in-memory ConcurrentDictionary (pending registrations)
**Testing**: xUnit + FluentAssertions + Moq (1,100+ tests across 30 projects)
**Target Platform**: Linux containers (Docker), .NET Aspire orchestration
**Project Type**: Distributed microservices (existing architecture)
**Performance Goals**: Register creation is a low-frequency admin operation; no specific throughput targets
**Constraints**: Atomic register+genesis (no orphan registers), 5-minute TTL on pending registrations
**Scale/Scope**: Modifications across 8 source projects, ~15 files changed

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Changes respect service boundaries. Validator calls Wallet via HTTP (not direct dependency). Register Service orchestrates without coupling to Validator internals. |
| II. Security First | PASS | Replacing placeholders with real crypto. Adding JWT service-to-service auth for Validator->Wallet calls. No secrets in source. |
| III. API Documentation | PASS | Modified endpoints will retain Scalar/OpenAPI documentation. New `IsPreHashed` parameter documented. |
| IV. Testing Requirements | PASS | Unit tests for pre-hashed signing, signature verification, WalletServiceClient HTTP calls. Integration tests for end-to-end flow. |
| V. Code Quality | PASS | async/await throughout, DI patterns, nullable reference types enabled. No new warnings. |
| VI. Blueprint Creation Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Using correct domain terms: Register, Attestation, Genesis, Participant. |
| VIII. Observability by Default | PASS | Structured logging at key decision points (signing, verification, genesis submission). Existing health checks preserved. |

**Gate result: PASS** -- No violations.

## Project Structure

### Documentation (this feature)

```text
specs/015-fix-register-crypto/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── register-creation-api.md
│   └── wallet-sign-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Register.Models/
│   │   └── RegisterCreationModels.cs          # Add hash storage to PendingRegistration, update DTOs
│   ├── Sorcha.ServiceClients/
│   │   ├── Wallet/
│   │   │   ├── IWalletServiceClient.cs        # Update SignTransactionAsync signature
│   │   │   └── WalletServiceClient.cs         # De-stub: real HTTP calls with JWT auth
│   │   ├── Auth/
│   │   │   ├── IServiceAuthClient.cs          # NEW: service-to-service token acquisition
│   │   │   └── ServiceAuthClient.cs           # NEW: calls Tenant Service /api/service-auth/token
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs  # Register HttpClient for WalletServiceClient + ServiceAuthClient
│   └── Sorcha.Wallet.Core/
│       ├── Services/
│       │   ├── Interfaces/
│       │   │   └── ITransactionService.cs     # Add isPreHashed parameter
│       │   └── Implementation/
│       │       └── TransactionService.cs      # Conditional hash skip when isPreHashed=true
│       └── Models/                            # (if needed for updated DTOs)
├── Services/
│   ├── Sorcha.Register.Service/
│   │   ├── Services/
│   │   │   └── RegisterCreationOrchestrator.cs # Return hex hash as dataToSign, verify from stored hash, atomic create
│   │   └── Program.cs                         # Remove simple POST /api/registers/ creation endpoint
│   ├── Sorcha.Wallet.Service/
│   │   ├── Endpoints/
│   │   │   └── WalletEndpoints.cs             # Pass isPreHashed to WalletManager
│   │   └── Models/
│   │       └── SignTransactionRequest.cs      # Add IsPreHashed property
│   ├── Sorcha.Validator.Service/
│   │   ├── Endpoints/
│   │   │   └── ValidationEndpoints.cs         # Use real wallet signing, real public keys
│   │   ├── Services/
│   │   │   └── GenesisManager.cs              # Use real wallet signing for docket signatures
│   │   └── Program.cs                         # Register ServiceAuthClient, configure JWT token acquisition
│   └── Sorcha.ApiGateway/
│       └── appsettings.json                   # Fix validator genesis route transformation
└── walkthroughs/
    └── RegisterCreationFlow/
        └── test-register-creation-with-real-signing.ps1  # Update for hex hash + isPreHashed

tests/
├── Sorcha.Wallet.Core.Tests/                  # Pre-hashed signing tests
├── Sorcha.ServiceClients.Tests/               # WalletServiceClient HTTP tests (if project exists)
└── Sorcha.Register.Service.Tests/             # Updated orchestrator tests
```

**Structure Decision**: Existing microservices architecture. This feature modifies files across 8 existing projects. One new pair of files is added (`IServiceAuthClient`/`ServiceAuthClient`) in the shared `Sorcha.ServiceClients` project to provide reusable service-to-service JWT token acquisition. No new projects are created.

## Complexity Tracking

> No constitution violations. No complexity justifications needed.

## Design Decisions

### D1: Hash-Based DataToSign (FR-001, FR-002, FR-004)

**Current:** `InitiateAsync` returns canonical JSON as `DataToSign`. `FinalizeAsync` re-serializes attestation data and re-hashes to verify.

**Change:** `InitiateAsync` computes SHA-256 hash of canonical JSON, returns hex-encoded hash as `DataToSign`, and stores the hash bytes in `PendingRegistration`. `FinalizeAsync` retrieves the stored hash bytes and verifies signatures against them directly -- no re-serialization.

**Rationale:** Eliminates JSON canonicalization fragility. The hash is deterministic and stored server-side, so verification is always against the exact bytes that were signed.

**Impact:**
- `RegisterCreationOrchestrator.InitiateAsync`: Change `DataToSign` from `canonicalJson` to `Convert.ToHexString(hashBytes)`
- `PendingRegistration`: Add `Dictionary<string, byte[]> AttestationHashes` keyed by `"{role}:{subject}"`
- `RegisterCreationOrchestrator.FinalizeAsync` / `VerifyAttestationsAsync`: Look up stored hash instead of re-serializing+re-hashing
- Client-side: Convert hex to bytes, Base64-encode for wallet sign endpoint

### D2: Pre-Hashed Signing in Wallet Service (FR-003, FR-010)

**Current:** `TransactionService.SignTransactionAsync` always SHA-256 hashes input before signing.

**Change:** Add `bool isPreHashed` parameter. When `true`, skip the `_hashProvider.ComputeHash` call and pass bytes directly to `_cryptoModule.SignAsync`. Default `false` preserves existing behavior.

**Impact:**
- `ITransactionService.SignTransactionAsync`: Add `bool isPreHashed = false` parameter
- `TransactionService.SignTransactionAsync`: Conditional `var dataToSign = isPreHashed ? transactionData : _hashProvider.ComputeHash(transactionData, HashType.SHA256)`
- `SignTransactionRequest` DTO: Add `public bool IsPreHashed { get; set; } = false`
- `WalletEndpoints.SignTransaction`: Pass `request.IsPreHashed` through to `WalletManager`
- `WalletManager.SignTransactionAsync`: Pass `isPreHashed` through to `TransactionService`

### D3: De-Stub WalletServiceClient with JWT Auth (FR-005, FR-006, FR-014)

**Current:** `WalletServiceClient` has no `HttpClient`, all methods return placeholder strings.

**Change:** Inject `HttpClient` (via `AddHttpClient<WalletServiceClient>`), add `IServiceAuthClient` dependency for JWT token acquisition. Implement `SignTransactionAsync` as `POST /api/v1/wallets/{address}/sign` with Bearer token header.

**New `ServiceAuthClient`:** Calls `POST /api/service-auth/token` on the Tenant Service with `client_credentials` grant. Caches token until near-expiry. Configured via `ServiceAuth:ClientId` and `ServiceAuth:ClientSecret` in each service's configuration.

**Impact:**
- New files: `IServiceAuthClient.cs`, `ServiceAuthClient.cs` in `Sorcha.ServiceClients/Auth/`
- `WalletServiceClient`: Rewrite to inject `HttpClient` + `IServiceAuthClient`, implement real HTTP calls
- `ServiceCollectionExtensions.AddServiceClients`: Register `HttpClient` for `WalletServiceClient`, register `ServiceAuthClient`
- Validator Service `Program.cs`: Configure `ServiceAuth` settings (client_id, client_secret for the validator service principal)
- Register Service `Program.cs`: Configure `ServiceAuth` settings (for the register service principal)

### D4: Real System Wallet Signing in Validator (FR-005, FR-006, FR-007)

**Current:** `ValidationEndpoints.SubmitGenesisTransaction` calls `walletClient.SignTransactionAsync` (stub, returns placeholder). Uses `UTF8.GetBytes(systemWalletAddress)` as "public key". `GenesisManager.CreateGenesisDocketAsync` calls `walletClient.SignDataAsync` (stub, returns placeholder).

**Change:** With `WalletServiceClient` de-stubbed (D3), these calls now produce real signatures. Update the code to:
- Parse the real signature and public key from the wallet's JSON response
- Store real `byte[]` public key and signature in the transaction/docket
- Compute real `PayloadHash` = SHA-256 of the serialized control record payload

**Impact:**
- `ValidationEndpoints.SubmitGenesisTransaction`: Use real public key bytes from wallet response, compute `PayloadHash`
- `GenesisManager.CreateGenesisDocketAsync`: Use real public key/signature bytes from wallet response
- `IWalletServiceClient.SignTransactionAsync`: Return type change from `string` to a structured response with both signature and public key

### D5: Atomic Register + Genesis (FR-013)

**Current:** `FinalizeAsync` calls `RegisterManager.CreateRegisterAsync` first, then `ValidatorServiceClient.SubmitGenesisTransactionAsync`. If genesis fails, the register is orphaned in the database.

**Change:** Reorder to: (1) submit genesis to validator first, (2) only if genesis succeeds, persist the register. If genesis fails, the caller gets an error and the register is not created.

**Alternative considered:** Persist register first, then delete on genesis failure. Rejected because MongoDB operations are not transactional across collections, and the delete could also fail, leaving an orphan.

**Impact:**
- `RegisterCreationOrchestrator.FinalizeAsync`: Move `RegisterManager.CreateRegisterAsync` after successful `SubmitGenesisTransactionAsync`. Pass register data (name, tenantId, etc.) through the genesis submission path.

### D6: Remove Simple CRUD Register Creation (FR-008)

**Current:** `POST /api/registers/` creates a register without crypto, attestations, or genesis.

**Change:** Remove the `POST` handler from the `registersGroup` in `Program.cs`. Keep GET, PUT, DELETE, and stats endpoints.

**Impact:**
- `Program.cs` lines ~431-461: Remove the `CreateRegister` POST handler
- Remove `CreateRegisterRequest` DTO if unused elsewhere
- Any code that calls this endpoint (tests, scripts) must be updated

### D7: Fix API Gateway Validator Route (FR-009)

**Current:** `/api/validator/{**catch-all}` transforms to `/api/v1/{**catch-all}`. The genesis endpoint is at `/api/validator/genesis` on the validator. So gateway request to `/api/validator/genesis` transforms to `/api/v1/genesis`, but the endpoint is at `/api/validator/genesis`.

**Change:** Add a specific route for genesis before the catch-all: `/api/validator/genesis` -> `/api/validator/genesis` (passthrough, no path transformation).

**Impact:**
- `appsettings.json`: Add `validator-genesis-route` with higher priority than the catch-all, mapping `/api/validator/genesis` to `/api/validator/genesis` on the validator cluster.

### D8: WalletServiceClient Return Type for Sign Operations

**Current:** `IWalletServiceClient.SignTransactionAsync` returns `Task<string>` (a single Base64 signature string). The Validator needs both signature AND public key.

**Change:** Return a structured `WalletSignResult` record containing `Signature` (byte[]), `PublicKey` (byte[]), `SignedBy` (string), and `Algorithm` (string). Update the interface and all callers.

**Impact:**
- `IWalletServiceClient`: New `WalletSignResult` return type
- `WalletServiceClient`: Parse JSON response from wallet sign endpoint into `WalletSignResult`
- `ValidationEndpoints.SubmitGenesisTransaction`: Use `result.PublicKey` and `result.Signature` instead of placeholders
- `GenesisManager.CreateGenesisDocketAsync`: Same

## Post-Design Constitution Re-Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | New `ServiceAuthClient` is shared infrastructure in `Sorcha.ServiceClients`, not a new service. Inter-service calls use HTTP with JWT auth. |
| II. Security First | PASS | JWT service-to-service auth added. Real cryptographic signatures replace placeholders. Service credentials stored in configuration (not source). |
| III. API Documentation | PASS | `IsPreHashed` parameter documented via OpenAPI. |
| IV. Testing Requirements | PASS | Unit tests for: pre-hashed signing (3 algorithms), WalletServiceClient HTTP calls (mocked HttpClient), orchestrator hash-based verification, atomic create flow. |
| V. Code Quality | PASS | All changes use async/await, DI, nullable types. |
| VI. Blueprint Creation Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Consistent terminology. |
| VIII. Observability by Default | PASS | Structured logging at sign, verify, genesis submission points. |

**Post-design gate result: PASS** -- No violations.
