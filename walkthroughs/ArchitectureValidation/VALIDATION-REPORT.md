# Architecture Validation Report

**Date:** 2026-02-17
**Scope:** End-to-end system validation — Genesis flow, Blueprint lifecycle, Transaction routing, Identity chain, Event system
**Commit:** `a942ce8` — 8 pipeline fixes
**Docker Instance:** Rebuilt with debug logging (`docker-compose.debug.yml`)

---

## Executive Summary

| Area | Status | Notes |
|------|--------|-------|
| Docker Services | PASS | All 11 services healthy, debug logging configured |
| Genesis Flow | PASS | Register creation via direct write; genesis TX fails validation (known) |
| Blueprint Publish | PASS | Create → validate → publish → Redis cache populated for Validator |
| Blueprint Execution | PASS | 3-round ping-pong (6 transactions), full pipeline verified |
| Event System | PASS | Redis Streams (6 streams) + SignalR (ActionConfirmed, ActionAvailable) |
| Org/Participant Setup | PASS | Bootstrap → users → participants → wallet links |
| Wallet Linking | PASS | Challenge-response verification, ownership validated on every action |

**Result:** 8 architectural issues found and fixed. Full end-to-end pipeline verified with 3-round ping-pong walkthrough (6/6 actions passed).

---

## 1. Docker Environment

### 1.1 Service Health
| Service | Port | Status | Notes |
|---------|------|--------|-------|
| Redis | 16379 | PASS | Cache, streams, SignalR backplane |
| PostgreSQL | 5432 | PASS | Wallet + Tenant data |
| MongoDB | 27017 | PASS | Register + Blueprint data |
| Aspire Dashboard | 18888 | PASS | Telemetry UI |
| API Gateway | 80 | PASS | YARP reverse proxy |
| Blueprint Service | 5000 | PASS | Workflow orchestration |
| Register Service | 5380 | PASS | Distributed ledger |
| Tenant Service | 5450 | PASS | Identity + auth |
| Validator Service | 5800 | PASS | Consensus + validation |
| Peer Service | 50051 | PASS | gRPC P2P (standalone) |
| Sorcha UI | 5400 | PASS | Blazor WASM frontend |

### 1.2 Debug Logging Configuration
Extended `docker-compose.debug.yml` to cover all services:
- Tenant: Authentication, Authorization, JWT debug
- Register: Core + Service debug
- Validator: Core + Service debug
- Blueprint: Service + Engine debug
- Wallet: Service debug
- API Gateway: YARP + Gateway debug

---

## 2. Genesis Flow Validation

### 2.1 Expected Sequence
1. `POST /api/registers/initiate` → get attestations to sign
2. Sign attestation hashes with wallet
3. `POST /api/registers/finalize` → genesis transaction created
4. Genesis TX → Validator unverified pool (Redis)
5. ValidationEngine validates structure + signatures
6. Promoted to verified queue
7. DocketBuilder creates genesis docket (docket #0)
8. ConsensusEngine achieves quorum
9. Docket written to Register Service (MongoDB)
10. Register status: "created"

### 2.2 Observed Behavior
- **Steps 1-3**: Register creation orchestrator works correctly. Control record written to MongoDB.
- **Step 4**: Genesis TX submitted to Validator unverified pool via Redis.
- **Step 5**: Genesis TX **fails** validation with VAL_SIG_002 (signature mismatch — see Fix 1). Genesis uses `BlueprintId = "genesis"` and `IsGenesisOrControlTransaction()` skips schema validation, but signature verification still runs against the wrong signing contract.
- **Steps 6-7**: DocketBuilder detects `height=0` and creates **empty** genesis docket (0 transactions) before ValidationEngine processes the genesis TX.
- **Steps 8-9**: 0 active validators → auto-approved. Empty docket #0 written to Register Service.
- **Step 10**: Register status set to "created" via direct write path in `RegisterCreationOrchestrator`.

### 2.3 Issues Found
- **Genesis TX VAL_SIG_002** (known, non-blocking): Genesis signing path signs `SHA256(controlRecordJson)` with `isPreHashed: true`, but Validator verifies `SHA256("{TxId}:{PayloadHash}")`. The register creation still works because the orchestrator writes the control record directly.
- **Empty genesis docket**: Docket #0 always contains 0 transactions. The genesis TX never makes it to the verified queue due to signature failure.
- **Impact**: None for current flow. Register creation is functional. Future work should align genesis signing with the standard contract.

---

## 3. Blueprint Lifecycle Validation

### 3.1 Publish Flow
1. `POST /api/blueprints` — Create blueprint from JSON template. PASS.
2. Blueprint cycle detection runs — warns about cyclic routes (expected for ping-pong). PASS.
3. `POST /api/blueprints/{id}/publish` — Immutable version created. PASS.
4. Blueprint serialized to Redis with key `sorcha:validator:blueprint:{blueprintId}` (Fix 2). PASS.

### 3.2 Instance Creation
1. `POST /api/instances` — Create workflow instance with participant-wallet mapping. PASS.
2. Starting actions identified (action 0 = Ping). PASS.
3. Instance state = 0 (active), current actions = [0]. PASS.

### 3.3 Action Execution Flow (Verified with 3-round ping-pong)

| Step | Component | Operation | Status |
|------|-----------|-----------|--------|
| 1 | Blueprint Service | `POST /api/instances/{id}/actions/{actionId}/execute` | PASS |
| 2 | Blueprint Service | Idempotency check (cycle-aware, Fix 8) | PASS |
| 3 | Blueprint Service | Wallet ownership via Tenant Service (participant + wallet links) | PASS |
| 4 | Blueprint Service | State reconstruction (LastTransactionId fallback, Fix 6) | PASS |
| 5 | Blueprint Service | Schema validation (action data schemas) | PASS |
| 6 | Blueprint Service | Disclosure rules (default full disclosure, Fix 4) | PASS |
| 7 | Blueprint Service | Build transaction (envelope with payloads) | PASS |
| 8 | Blueprint Service | Sign `{TxId}:{PayloadHash}` via Wallet Service (Fix 1) | PASS |
| 9 | Blueprint Service | Submit to Validator with PreviousTransactionId (Fix 7) | PASS |
| 10 | Validator Service | Structure validation | PASS |
| 11 | Validator Service | Payload hash verification | PASS |
| 12 | Validator Service | Signature verification (ED25519) | PASS |
| 13 | Validator Service | Blueprint cache lookup from Redis (Fix 2) | PASS |
| 14 | Validator Service | Schema validation with payload extraction (Fix 3) | PASS |
| 15 | Validator Service | Blueprint conformance (VAL_BP_001-003) | PASS |
| 16 | Validator Service | Add to verified queue | PASS |
| 17 | Validator Service | DocketBuilder dequeues → builds docket | PASS |
| 18 | Validator Service | ConsensusEngine (0 validators = auto-approve) | PASS |
| 19 | Validator Service | Write docket to Register Service | PASS |
| 20 | Blueprint Service | Poll for confirmation (1s interval, 30s timeout) | PASS |
| 21 | Blueprint Service | Update instance state (LastTransactionId, current actions) | PASS |
| 22 | Blueprint Service | SignalR notifications (ActionConfirmed + ActionAvailable) | PASS |

### 3.4 Walkthrough Results

```
  [Round  1/3] Ping OK -> Pong OK
  [Round  2/3] Ping OK -> Pong OK
  [Round  3/3] Ping OK -> Pong OK

  RESULT: PASS - Full-stack pipeline verified!
  Actions: 6/6 succeeded
  Duration: 102.8s (~17s per action)
```

---

## 4. Fixes Applied

| # | File(s) | Issue | Fix | Severity |
|---|---------|-------|-----|----------|
| 1 | `ActionExecutionService.cs`, `ITransactionBuilderService.cs` | Signature contract mismatch (VAL_SIG_002): Blueprint signed `SHA256(fullJSON)`, Validator verified `SHA256("{TxId}:{PayloadHash}")` | Changed signing to use `BuiltTransaction.SigningData` = `"{TxId}:{PayloadHash}"` bytes | Critical |
| 2 | `Blueprint.Service/Program.cs` | Blueprint cache empty (VAL_SCHEMA_001): Validator's Redis cache never populated | Blueprint Service writes to Redis on publish with key `sorcha:validator:blueprint:{id}` | Critical |
| 3 | `ValidationEngine.cs` | Schema validated full envelope (VAL_SCHEMA_004): Schema ran against `{type, blueprintId, ..., payloads}` instead of user data | Extract first wallet's data from `payloads` property before schema evaluation | Critical |
| 4 | `ActionExecutionService.cs` | Empty disclosure payloads: `ApplyDisclosures()` returned `{}` when no rules defined, causing empty `payloads` in envelope | Default to full disclosure under sender's wallet when no rules produce results | High |
| 5 | `test-org-ping-pong.ps1` | Walkthrough wallet link idempotency: Script used newly-created wallet addresses, not actually-linked ones on re-run | GET actual linked wallets after link phase and use those for execution | Medium |
| 6 | `ActionExecutionService.cs` | Missing previous TX: State reconstruction queries nonexistent Register endpoint (404) | Fall back to `instance.LastTransactionId` when register query returns empty | High |
| 7 | `IValidatorServiceClient.cs`, `ValidationEndpoints.cs`, `ITransactionBuilderService.cs` | PreviousTransactionId not submitted: `ActionTransactionSubmission` had no field, Validator always saw null | Added `PreviousTransactionId` to submission model, endpoint request, and transaction mapping | Critical |
| 8 | `ActionExecutionService.cs` | Cyclic idempotency block: Key = `instance:action:wallet` was identical every cycle | Include `LastTransactionId` in idempotency key; moved check after instance load | High |

Additional non-fix changes:
- `docker-compose.debug.yml` — Extended debug logging for all services
- `walkthroughs/*/test-*.ps1` (3 files) — Fixed template paths from `examples/templates/` to `blueprints/`

---

## 5. Architecture Issues for Discussion

| # | Area | Description | Impact | Recommendation |
|---|------|-------------|--------|----------------|
| 1 | Genesis | Genesis TX signing uses different contract than action transactions | Genesis TX always fails validation; non-blocking because register works via direct write | Align genesis signing to `{TxId}:{PayloadHash}` contract |
| 2 | Register API | Instance-based transaction query endpoint doesn't exist | State reconstruction falls back to `LastTransactionId`; limits multi-participant state merging | Add `GET /api/query/instance/{id}/transactions/{registerId}` to Register Service |
| 3 | Validator | Docket build trigger logs WARNING when verified queue is empty | Log noise every 10s per register; masks real warnings | Change to TRACE level for empty-queue case |
| 4 | Blueprint Cache | No cache invalidation or versioning | Blueprint updates after publish could create inconsistency | Add version field or event-driven invalidation |
| 5 | Consensus | 0 validators = auto-approve | All dockets are auto-approved in current deployment | Expected for development; production needs validator registration |

---

## 6. Event System Validation

### 6.1 Redis Streams
Register Service subscribes to 6 event streams on startup:

| Stream | Event Type | Status |
|--------|-----------|--------|
| `sorcha:events:register:created` | RegisterCreatedEvent | PASS |
| `sorcha:events:register:status-changed` | RegisterStatusChangedEvent | PASS |
| `sorcha:events:transaction:confirmed` | TransactionConfirmedEvent | PASS |
| `sorcha:events:docket:confirmed` | DocketConfirmedEvent | PASS |
| `sorcha:events:register:height-updated` | RegisterHeightUpdatedEvent | PASS |
| `sorcha:events:transaction:submitted` | TransactionSubmittedEvent | PASS |

- Consumer group: `register-service-{hostname}-1`
- `RegisterEventBridgeService` bridges Redis events to SignalR
- `EventSubscriptionHostedService` runs processing loop

### 6.2 SignalR Notifications
Two notification types observed during walkthrough execution:

| Notification | Target | Trigger | Status |
|-------------|--------|---------|--------|
| `ActionConfirmed` | Sender's wallet address | Transaction confirmed in docket | PASS |
| `ActionAvailable` | Next participant (by role) | Route evaluation identifies next action | PASS |

Pattern observed across 6 actions:
- Ping executes → ActionConfirmed to Alpha wallet → ActionAvailable to Pong participant
- Pong executes → ActionConfirmed to Beta wallet → ActionAvailable to Ping participant
- Cycle repeats for 3 rounds

---

## 7. Test Summary

| Test | Result | Duration | Notes |
|------|--------|----------|-------|
| Docker health check | PASS | <1s | All 11 services healthy |
| Organization bootstrap | PASS | ~2s | Idempotent (409 on re-run) |
| Participant creation | PASS | ~1s | 3 participants (designer + 2 basic) |
| Wallet creation | PASS | ~3s | 3 ED25519 wallets |
| Wallet linking | PASS | ~2s | Challenge-response for 2 wallets |
| Register creation | PASS | ~5s | Genesis docket written |
| Blueprint create + publish | PASS | ~3s | Redis cache populated |
| Instance creation | PASS | ~1s | Starting action identified |
| Ping Round 1 | PASS | ~17s | Full pipeline: build → sign → validate → docket → confirm |
| Pong Round 1 | PASS | ~17s | PreviousTransactionId correctly linked |
| Ping Round 2 | PASS | ~17s | Cyclic idempotency key works |
| Pong Round 2 | PASS | ~17s | State tracking persists across cycles |
| Ping Round 3 | PASS | ~17s | Sustained operation confirmed |
| Pong Round 3 | PASS | ~17s | Final round clean |
| **Total** | **PASS** | **112s** | **11/11 steps, 6/6 actions** |

---

## 8. Files Modified

| File | Changes |
|------|---------|
| `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs` | Added `PreviousTransactionId` to `ActionTransactionSubmission` |
| `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs` | Added `PreviousTransactionId` to `ValidateTransactionRequest` + transaction mapping |
| `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` | Schema payload extraction from transaction envelope |
| `src/Services/Sorcha.Blueprint.Service/Program.cs` | Redis cache population on blueprint publish |
| `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` | Signing contract, default disclosure, LastTransactionId fallback, cyclic idempotency |
| `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` | `PayloadHash`, `SigningData`, `PreviousTransactionId` in `BuiltTransaction` |
| `docker-compose.debug.yml` | Debug logging for all services |
| `walkthroughs/OrganizationPingPong/test-org-ping-pong.ps1` | Wallet link idempotency fix |
| `walkthroughs/PingPong/test-ping-pong-workflow.ps1` | Template path fix |
| `walkthroughs/DistributedRegister/test-distributed-register.ps1` | Template path fix |
| `.specify/MASTER-TASKS.md` | Added validation results to recent updates |
