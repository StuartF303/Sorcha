# Transaction Submission Flow Analysis

**Created:** 2026-02-18
**Status:** Research / P0 Investigation
**Related:** MASTER-TASKS.md, VALIDATOR-SERVICE-REQUIREMENTS.md

---

## Current Understanding

### Transaction Lifecycle

All transactions in Sorcha must follow this validated pipeline:

```
Caller (Register Service / Blueprint Service / UI)
    │
    ▼
Validator Service Endpoint
    ├── /api/validator/genesis     (Control/Genesis transactions)
    └── /api/v1/transactions/validate  (Action transactions)
    │
    ▼
Unverified Pool (Redis-backed ITransactionPoolPoller)
    │  Key: {prefix}{registerId}:queue / {prefix}{registerId}:data:{txId}
    │  TTL: configurable expiry
    │
    ▼
ValidationEngineService (BackgroundService)
    │  Polls monitored registers periodically
    │  Creates scoped IValidationEngine per batch
    │
    ▼
Validation Pipeline (6 stages):
    1. Structure validation (TransactionId, RegisterId, Payload)
    2. Payload hash verification (SHA-256)
    3. Schema validation (if enabled)
    4. Signature verification (if enabled)
    4b. Blueprint conformance (if enabled)
    4c. Governance rights for Control TXs (if enabled)
    5. Chain validation (PrevTxId continuity, fork detection)
    6. Timing validation (expiry, clock skew)
    │
    ▼
Verified Transaction Queue (IVerifiedTransactionQueue)
    │  Priority-ordered (High=10, Normal=0, Low=-10)
    │
    ▼
DocketBuildTriggerService (BackgroundService)
    │  Hybrid triggers: time threshold OR size threshold
    │  Monitors all registers in IRegisterMonitoringRegistry
    │
    ▼
DocketBuilder.BuildDocketAsync()
    │  1. Dequeue from verified queue
    │  2. Check NeedsGenesisDocket (height == 0)
    │  3. Get previous docket hash
    │  4. Compute Merkle root from transaction hashes
    │  5. Compute docket hash (SHA-256)
    │  6. Sign docket with system wallet
    │
    ▼
Consensus (if multi-validator)
    │  ConsensusEngine.AchieveConsensusAsync()
    │
    ▼
WriteDocketAndTransactionsAsync()
    │  POST /api/registers/{registerId}/dockets
    │  Includes transaction models in docket payload
    │
    ▼
Register Service persists to MongoDB
    │  Docket + Transactions sealed in ledger
    │  Register height updated
```

---

## Transaction Types

### 1. Genesis (Register Creation)

- **Submitted by:** RegisterCreationOrchestrator → IValidatorServiceClient.SubmitGenesisTransactionAsync()
- **Endpoint:** POST /api/validator/genesis
- **BlueprintId:** `GenesisConstants.BlueprintId` = "genesis"
- **ActionId:** `GenesisConstants.ActionId` = "register-creation"
- **Signed by:** System wallet (validator signs with wallet service)
- **Metadata:** Type=Genesis, RegisterName, TenantId, SystemWalletAddress
- **Docket:** Genesis docket (number 0) created by GenesisManager
- **Status:** Working correctly

### 2. Blueprint Publish (Control)

- **Submitted by:** Register Service publish endpoint → IValidatorServiceClient.SubmitGenesisTransactionAsync()
- **Endpoint:** POST /api/validator/genesis (reuses genesis endpoint with overrides)
- **BlueprintId:** Actual blueprint ID (e.g., "7f3f88d6-1fcc-4231-b3a4-17874540802e")
- **ActionId:** "blueprint-publish"
- **Signed by:** System wallet (validator signs with wallet service)
- **Metadata:** Type=Control, transactionType=BlueprintPublish, publishedBy=...
- **Docket:** Normal docket (number N+1) created by DocketBuilder
- **Status:** NEWLY IMPLEMENTED — needs testing

### 3. Action (Blueprint Execution)

- **Submitted by:** Blueprint Service → IValidatorServiceClient.SubmitTransactionAsync()
- **Endpoint:** POST /api/v1/transactions/validate
- **BlueprintId:** Blueprint being executed
- **ActionId:** Current action step
- **Signed by:** User's wallet (externally signed before submission)
- **Signatures:** Required, validated against wallet service
- **Docket:** Normal docket
- **Status:** Working (validated in 3-round ping-pong walkthrough)

### 4. Governance (Control)

- **Submitted by:** Register Service governance endpoints
- **BlueprintId:** "register-governance-v1"
- **Status:** Not fully wired to validator — needs investigation

---

## Known Issues

### Issue 1: Genesis Endpoint Reuse for Blueprint Publish

The genesis endpoint (`/api/validator/genesis`) was designed for register creation only. We extended it with optional `BlueprintId`, `ActionId`, and `Metadata` overrides to support blueprint publish Control transactions.

**Concern:** The genesis endpoint creates a `Transaction` with `GenesisConstants.BlueprintId` by default. When the overrides are provided, it uses the actual blueprint ID. But:
- The validation pipeline may have special handling for "genesis" BlueprintId
- The DocketBuilder has genesis-specific logic (`NeedsGenesisDocketAsync`)
- Schema/blueprint conformance checks may fail for custom BlueprintId values

**Recommendation:** Test thoroughly. If the validation pipeline rejects blueprint-publish transactions, consider:
1. A dedicated `/api/validator/control` endpoint for non-genesis Control transactions
2. Skipping blueprint conformance for Control transactions (they're system-level)

### Issue 2: Validator Stuck in Initialization Loop

**Symptom:** Validator logs show repeated `RegisterServiceClient initialized` / `WalletServiceClient initialized` messages every ~10 seconds with no actual transaction processing.

**Possible causes:**
- No registers in the `IRegisterMonitoringRegistry` (nothing to monitor)
- Service client initialization failing and retrying
- Background services not starting properly

**Investigation needed:**
- Check if `monitoringRegistry.RegisterForMonitoring()` was called after genesis
- Check if `ValidationEngineService` and `DocketBuildTriggerService` are running
- Verify Redis connectivity for the transaction pool

### Issue 3: Stale Direct-Write Transaction

**Symptom:** A blueprint publish transaction was previously stored directly to MongoDB via `transactionManager.StoreTransactionAsync()` without going through the validator. This transaction:
- Exists in the ledger without a docket
- Has no system wallet signature
- Was never validated

**Impact:** The register now has an orphan transaction that doesn't belong to any docket. The chain integrity may be broken because the next docket's transactions may reference it.

**Resolution options:**
1. Delete the orphan transaction from MongoDB manually
2. Leave it — the docket builder only processes transactions from the verified queue
3. The WriteDocket endpoint handles duplicate transactions gracefully (DuplicateKey catch)

### Issue 4: WriteDocket Endpoint Authorization

**Concern:** The `/api/registers/{registerId}/dockets` POST endpoint requires `CanWriteDockets` authorization policy. The validator service must authenticate with the register service to write dockets.

**Check:** Verify the validator's service-to-service JWT includes the required claims for `CanWriteDockets`.

### Issue 5: Register Service Direct Transaction Endpoints

**Concern:** Some register service endpoints may write transactions directly without going through the validator. These need to be audited:
- Blueprint publish endpoint (FIXED — now submits to validator)
- Any diagnostic/admin transaction submission endpoints
- The WriteDocket endpoint's transaction insertion (this is OK — validator calls it)

---

## Key Files

| Component | File | Purpose |
|-----------|------|---------|
| Genesis endpoint | `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs` | Accepts Control/genesis transactions |
| Action endpoint | Same file | Accepts action transactions for validation |
| Validation engine | `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` | 6-stage validation pipeline |
| Validation poller | `src/Services/Sorcha.Validator.Service/Services/ValidationEngineService.cs` | Background polling of unverified pool |
| Docket builder | `src/Services/Sorcha.Validator.Service/Services/DocketBuilder.cs` | Builds dockets from verified transactions |
| Docket trigger | `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs` | Monitors thresholds, triggers builds |
| Genesis manager | `src/Services/Sorcha.Validator.Service/Services/GenesisManager.cs` | Creates genesis dockets (height=0) |
| Transaction pool | `src/Services/Sorcha.Validator.Service/Services/TransactionPoolPoller.cs` | Redis-backed unverified pool |
| Verified queue | `src/Services/Sorcha.Validator.Service/Services/VerifiedTransactionQueue.cs` | In-memory verified queue |
| Monitoring registry | `src/Services/Sorcha.Validator.Service/Services/RegisterMonitoringRegistry.cs` | Tracks active registers |
| Consensus engine | `src/Services/Sorcha.Validator.Service/Services/ConsensusEngine.cs` | Multi-validator consensus |
| WriteDocket endpoint | `src/Services/Sorcha.Register.Service/Program.cs` (~line 1031) | Receives dockets from validator |
| Publish endpoint | `src/Services/Sorcha.Register.Service/Program.cs` (~line 1108) | Blueprint publish → validator |
| Orchestrator | `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs` | Genesis flow |
| Validator client | `src/Common/Sorcha.ServiceClients/Validator/ValidatorServiceClient.cs` | HTTP client to validator |
| Transaction manager | `src/Core/Sorcha.Register.Core/Managers/TransactionManager.cs` | Direct MongoDB operations |

---

## Action Items

1. **Test blueprint publish through validator** — Rebuild Docker, publish a blueprint, verify:
   - Transaction appears in validator logs
   - Transaction passes validation
   - Docket is created
   - Transaction appears in register with docket number

2. **Investigate validator initialization loop** — Determine why validator only logs initialization messages

3. **Clean up orphan transaction** — Remove the directly-stored transaction from MongoDB

4. **Audit all transaction write paths** — Ensure NO path writes to the register without going through the validator

5. **Governance transaction submission** — Wire governance operations through the validator pipeline

6. **Add Control transaction endpoint** — Consider a dedicated `/api/validator/control` endpoint separate from genesis
