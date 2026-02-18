# Research: Unified Transaction Submission

**Date**: 2026-02-18
**Feature**: 036-unified-transaction-submission

## R1: Genesis/Control-Specific Validation Pipeline Paths

### Decision
The validation pipeline has 3 explicit skip points for genesis/control transactions, all keyed off `IsGenesisOrControlTransaction()` which checks `BlueprintId="genesis"` OR `Metadata.Type="Genesis"/"Control"`. These skips must be preserved (schema validation and blueprint conformance are not applicable to genesis/control) but the **signature verification skip must be removed** once callers provide real signatures.

### Findings

| Pipeline Stage | Current Behaviour | Target Behaviour |
|---------------|-------------------|------------------|
| Schema validation | SKIP for genesis/control | SKIP (unchanged — no schema for genesis/control) |
| Signature verification | SKIP (attestation sigs use different contract) | VERIFY normally — callers now use `{TxId}:{PayloadHash}` format |
| Blueprint conformance | SKIP for genesis/control | SKIP (unchanged — no blueprint to conform to) |
| Governance rights | Allows first Control TX if no roster exists | Unchanged |

### Key Finding: Signing Data Format
The genesis endpoint already constructs signing data as `$"{request.TransactionId}:{controlRecordHashHex}"` (ValidationEndpoints.cs:213), which matches the standard `{TxId}:{PayloadHash}` contract used by action transactions. This means **no signing format change is needed** — we just need to remove the signature verification skip and ensure callers sign using this format.

### Alternatives Considered
- Keep signature skip for genesis/control → Rejected: violates uniform validation principle, creates a security gap where unsigned transactions pass through
- Create a separate verification path for attestation signatures → Rejected: over-complex, the `{TxId}:{PayloadHash}` format already works

---

## R2: System Wallet Infrastructure

### Decision
Create `ISystemWalletSigningService` in `Sorcha.ServiceClients` (not in Validator.Service) so both Register Service and Validator Service can use it. Extract the wallet acquisition/signing/retry logic currently in the genesis endpoint into this shared service.

### Current State
- `ISystemWalletProvider` — Validator-internal (`Validator.Service.Services`), singleton, stores wallet address in memory
- `SystemWalletInitializer` — IHostedService in Validator Service that creates/retrieves wallet on startup
- Genesis endpoint (ValidationEndpoints.cs:220-296) — Contains full wallet lifecycle: get → create if missing → sign → retry/recreate on failure
- DocketBuilder (DocketBuilder.cs:138-148) — Separate wallet acquisition + signing logic for docket signing

### Design
The new `ISystemWalletSigningService` in `Sorcha.ServiceClients` will:
1. Wrap `IWalletServiceClient` for the signing calls
2. Manage wallet address caching (thread-safe, lazy init)
3. Implement retry/recreate on wallet unavailability
4. Enforce operation whitelist (allowed derivation paths)
5. Rate-limit per register per time window
6. Emit structured audit logs for every operation

### Signing Data Format Contract
All system-signed transactions use: `SHA-256(UTF-8("{TxId}:{PayloadHash}"))` → byte[] → sign with `isPreHashed: true`.

### Alternatives Considered
- Keep signing logic inline per caller → Rejected: duplication, inconsistent retry behaviour, no audit trail
- Put in Validator.Service only → Rejected: Register Service also needs system signing
- Put in Sorcha.Cryptography → Rejected: depends on IWalletServiceClient (HTTP), not a pure crypto concern

---

## R3: All Transaction Write Paths (Audit)

### Decision
All direct writes to the register that bypass the validator must be identified and migrated. The scope of this feature is larger than originally estimated.

### Findings

| # | Path | Source | Current Target | Status |
|---|------|--------|---------------|--------|
| 1 | Register creation (genesis) | RegisterCreationOrchestrator | POST `/api/validator/genesis` | Migrating to generic endpoint |
| 2 | Blueprint publish (control) | Register Service Program.cs:1185 | POST `/api/validator/genesis` | Migrating to generic endpoint |
| 3 | Action execution | ActionExecutionService | POST `/api/v1/transactions/validate` | Already correct |
| 4 | Action rejection | ActionExecutionService | POST `/api/v1/transactions/validate` | Already correct |
| 5 | Credential issuance | ActionExecutionService | POST `/api/v1/transactions/validate` | Already correct |
| 6 | **Action execution endpoint** | **Blueprint Service Program.cs:880** | **POST `/api/registers/{id}/transactions` (DIRECT)** | **CRITICAL: Bypasses validator** |
| 7 | **Action rejection endpoint** | **Blueprint Service Program.cs:1031** | **POST `/api/registers/{id}/transactions` (DIRECT)** | **CRITICAL: Bypasses validator** |
| 8 | Validator registration | ValidatorRegistry:305 | POST `/api/registers/{id}/transactions` (DIRECT) | Bypasses validator — acceptable for validator self-registration? |
| 9 | Validator approval | ValidatorRegistry:483 | POST `/api/registers/{id}/transactions` (DIRECT) | Bypasses validator — acceptable for validator self-registration? |
| 10 | Diagnostic endpoint | Register Service Program.cs:750 | Direct TransactionManager.StoreTransactionAsync | Marked "internal/diagnostic only" |

### Critical Discovery
Blueprint Service Program.cs has **two endpoints** (action execution ~line 880, action rejection ~line 1031) that call `IRegisterServiceClient.SubmitTransactionAsync()` which writes directly to the Register Service, completely bypassing the validator. These are separate from the ActionExecutionService methods that correctly use the validator.

These appear to be **legacy endpoints** that predate the validator integration in ActionExecutionService. They should be audited to determine if they are still reachable or if they can be removed/redirected.

### Validator Self-Registration (Paths 8-9)
ValidatorRegistry writes Control transactions directly to the register for validator registration and approval. This is a special case — the validator is recording its own participation, creating a chicken-and-egg problem. These paths should be documented as exceptions or migrated to use the signing service + generic endpoint.

### Alternatives Considered
- Ignore Blueprint Service direct writes → Rejected: violates core principle, creates unsigned transactions in ledger
- Only fix genesis/control paths → Rejected: would leave the bigger problem (action direct writes) unresolved

---

## R4: Service Client Registration Patterns

### Decision
The `ISystemWalletSigningService` must use an explicit opt-in registration (`AddSystemWalletSigning()`) separate from the existing `AddServiceClients()` extension method.

### Current Pattern
`AddServiceClients()` in `ServiceCollectionExtensions.cs` registers all service clients (Wallet, Validator, Register, Peer, Participant, Blueprint) as scoped dependencies. Every service that calls `AddServiceClients()` gets all clients.

### Design
New extension method: `AddSystemWalletSigning(IServiceCollection, IConfiguration)` in a new file within `Sorcha.ServiceClients/SystemWallet/`. This:
- Registers `ISystemWalletSigningService` as a **singleton** (manages wallet address state, rate limit counters)
- Requires `IWalletServiceClient` to already be registered (dependency)
- Reads configuration for: ValidatorId, allowed derivation paths, rate limit settings
- Is called **only** from Register Service and Validator Service startup

### Alternatives Considered
- Include in `AddServiceClients()` → Rejected: violates least-privilege, every service would get signing capability
- Register as scoped → Rejected: wallet address caching and rate limiting need process-level state

---

## R5: Validator Pipeline Type-Agnostic Check

### Decision
The `IsGenesisOrControlTransaction()` method in ValidationEngine.cs is correctly placed and can remain. The only change needed is removing the signature verification skip (line 490-498). Schema validation skip and blueprint conformance skip should remain.

### Rationale
- Schema validation: Genesis/control transactions define their own payload structure — there's no user-defined schema to validate against
- Blueprint conformance: Genesis creates the register, it doesn't execute a blueprint action
- Signature verification: Once callers sign properly with `{TxId}:{PayloadHash}`, the standard verification path works for all types
- Governance rights (stage 4c): Already handles genesis/control correctly via RightsEnforcementService

### Testing Impact
The signature verification removal means genesis transactions will now be cryptographically verified. Tests that submit genesis transactions must provide valid signatures (currently they don't need to, since verification is skipped).
