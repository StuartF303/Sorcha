# Data Model: Unified Transaction Submission

**Date**: 2026-02-18
**Feature**: 036-unified-transaction-submission

## Entities

### 1. SystemWalletSigningService (New)

Central service for system-level transaction signing with security controls.

**State:**
- `WalletAddress: string?` — cached system wallet address (lazy-initialized)
- `ValidatorId: string` — unique identifier for wallet creation/retrieval
- `AllowedDerivationPaths: IReadOnlySet<string>` — operation whitelist
- `RateLimitCounters: ConcurrentDictionary<string, SlidingWindow>` — per-register rate tracking

**Configuration:**
- `ValidatorId` — from service configuration
- `MaxSignsPerRegisterPerMinute: int` — rate limit threshold (default: 10)
- `AllowedDerivationPaths: string[]` — permitted paths (default: `["sorcha:register-control", "sorcha:docket-signing"]`)

**Operations:**
- `SignAsync(registerId, txId, payloadHash, derivationPath, transactionType, ct)` → `SystemSignResult`
- Validates derivation path against whitelist
- Checks rate limit for register
- Acquires wallet (create if needed)
- Signs using `{TxId}:{PayloadHash}` format
- Logs audit entry
- Returns signature, public key, algorithm

**Lifecycle:**
- Singleton per process
- Wallet address cached after first acquisition
- Rate limit state in-memory (resets on process restart)

### 2. SystemSignResult (New)

Return type from the signing service.

**Fields:**
- `Signature: byte[]` — cryptographic signature
- `PublicKey: byte[]` — public key of signing wallet
- `Algorithm: string` — signing algorithm used (e.g. "ED25519")
- `WalletAddress: string` — address of the wallet that signed

### 3. SystemSignAuditEntry (New — structured log)

Audit record emitted for every signing operation (not persisted as entity — structured log).

**Fields:**
- `Timestamp: DateTimeOffset`
- `CallerService: string` — identity of calling service
- `RegisterId: string`
- `TransactionId: string`
- `TransactionType: string` — from metadata (Genesis, Control, etc.)
- `DerivationPath: string`
- `WalletAddress: string`
- `Outcome: string` — Success, RateLimited, WhitelistRejected, WalletUnavailable, Error

### 4. TransactionSubmission (Renamed from ActionTransactionSubmission)

Unified request model for all transaction types — replaces both `ActionTransactionSubmission` and `GenesisTransactionSubmission`.

**Fields:**
- `TransactionId: string` — required, 64-char hex
- `RegisterId: string` — required
- `BlueprintId: string` — required (e.g. "genesis", blueprint GUID, "register-governance-v1")
- `ActionId: string` — required (e.g. "register-creation", "blueprint-publish", action step)
- `Payload: JsonElement` — required, transaction payload
- `PayloadHash: string` — required, hex-encoded SHA-256 of payload
- `Signatures: List<SignatureInfo>` — required, at least one
- `CreatedAt: DateTimeOffset` — required
- `ExpiresAt: DateTimeOffset?` — optional (null for control transactions)
- `PreviousTransactionId: string?` — optional chain linkage
- `Priority: TransactionPriority` — default Normal, High for control/genesis
- `Metadata: Dictionary<string, string>?` — optional, contains Type, RegisterName, etc.

**Validation Rules:**
- TransactionId must be 64 hexadecimal characters
- Signatures list must not be empty
- PayloadHash must match SHA-256 of serialized Payload
- CreatedAt must not be in the future (with clock skew tolerance)

## Entity Relationships

```
SystemWalletSigningService
    │
    │ uses
    ▼
IWalletServiceClient ──── signs ────► WalletSignResult
    │                                      │
    │                                      │ maps to
    │                                      ▼
    │                              SignatureInfo (in TransactionSubmission)
    │
    ▼
TransactionSubmission ──── submitted to ────► Validator Generic Endpoint
    │                                              │
    │ contains                                     │ validates & pools
    ▼                                              ▼
Metadata (Type, RegisterName, etc.)         Transaction (internal model)
```

## State Transitions

### System Wallet Lifecycle
```
Uninitialized ──[first sign request]──► Creating ──[success]──► Cached
                                           │
                                           │ [failure]
                                           ▼
                                        Failed ──[next request retries]──► Creating

Cached ──[wallet unavailable]──► Recreating ──[success]──► Cached
                                      │
                                      │ [failure after retries]
                                      ▼
                                   Error (propagated to caller)
```

### Rate Limit State
```
Under Limit ──[sign request]──► Under Limit (counter incremented)
Under Limit ──[sign request when counter >= max]──► Rate Limited (request rejected)
Rate Limited ──[window expires]──► Under Limit (counter reset)
```

## Migration Notes

### Models to Remove (Phase: Legacy Cleanup)
- `GenesisTransactionSubmission` — replaced by `TransactionSubmission`
- `GenesisSignature` — replaced by `SignatureInfo`
- `GenesisTransactionRequest` — replaced by `ValidateTransactionRequest`
- `SubmitGenesisTransactionAsync()` — replaced by `SubmitTransactionAsync()`

### Models Unchanged
- `ValidateTransactionRequest` — already sufficient for all types
- `SignatureRequest` — already used by generic endpoint
- `TransactionSubmissionResult` — already returned by generic client method
