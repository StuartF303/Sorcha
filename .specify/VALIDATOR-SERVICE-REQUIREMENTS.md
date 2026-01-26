# Validator Service - Requirements Document

**Created:** 2025-11-19
**Updated:** 2026-01-26
**Status:** Requirements Refined
**Purpose:** Define requirements for Sorcha.Validator.Service as a decentralized consensus participant

---

## Executive Summary

The Validator Service is a dual-purpose component in Sorcha's decentralized ledger network. It validates transactions against Blueprint rules and participates in multi-validator consensus to build and sign dockets before committing them to the register.

### Key Requirements

1. **Dual-Role Architecture:** Validators act as both **initiators** (build dockets, collect signatures) and **confirmers** (validate and co-sign dockets from other validators)
2. **Leader-Based Docket Building:** A single **elected leader** initiates docket builds to manage sequencing; other validators act as confirmers
3. **Decentralized Consensus:** Dockets require signatures from multiple validators before commitment, with thresholds defined in the genesis blueprint
4. **Consensus Failure Handling:** If threshold not met by timeout, docket is **abandoned** and transactions return to unverified pool for retry
5. **Genesis Blueprint Governance:** Register configuration (thresholds, timeouts, validator rules) is defined in a control blueprint embedded in the genesis block
6. **Control Blueprint Versioning:** The control blueprint can be updated via control dockets to add/remove organizations, change register properties, etc.
7. **Multi-Blueprint Registers:** Registers CAN contain multiple blueprints; each blueprint's `previousId` chains to genesis or prior version
8. **Chain-Based Instances:** Workflow instances tracked via transaction chain (`previousId`), not separate instance IDs
9. **Blueprint Versioning:** New blueprint versions reference prior version's transaction ID as `previousId`
10. **Configurable Signatures:** Minimum threshold to commit, maximum cap to prevent bloat - both configurable per register
11. **gRPC Communication:** Validator-to-validator communication via Peer Service using gRPC

---

## Architecture Overview

### Dual-Role Validator

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              VALIDATOR SERVICE                               │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                      INITIATOR ROLE                                     │ │
│  │  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌───────────────────┐   │ │
│  │  │  Poller  │──▶│ Validate │──▶│  Docket  │──▶│ Sign + Broadcast  │   │ │
│  │  │ (Redis)  │   │  Engine  │   │ Builder  │   │  to Confirmers    │   │ │
│  │  └──────────┘   └──────────┘   └──────────┘   └───────────────────┘   │ │
│  │                                                         │              │ │
│  │                      ┌──────────────────────────────────┘              │ │
│  │                      ▼                                                 │ │
│  │  ┌─────────────────────────────────┐   ┌────────────────────────────┐ │ │
│  │  │ Signature Collector             │──▶│ Commit to Register +       │ │ │
│  │  │ (wait for threshold/timeout)    │   │ Distribute via Peer Svc    │ │ │
│  │  └─────────────────────────────────┘   └────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                      CONFIRMER ROLE                                     │ │
│  │  ┌──────────────────┐   ┌──────────────┐   ┌────────────────────────┐ │ │
│  │  │ Receive Docket   │──▶│ Validate All │──▶│ Sign + Return to       │ │ │
│  │  │ from Peer Svc    │   │ Transactions │   │ Initiator              │ │ │
│  │  └──────────────────┘   └──────────────┘   └────────────────────────┘ │ │
│  │                                │                                       │ │
│  │                                ▼ (if invalid)                          │ │
│  │                    ┌────────────────────────┐                          │ │
│  │                    │ Reject + Log for       │                          │ │
│  │                    │ Bad Actor Detection    │                          │ │
│  │                    └────────────────────────┘                          │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                      SHARED COMPONENTS                                  │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                 │ │
│  │  │  Validation  │  │  Blueprint   │  │   Genesis    │                 │ │
│  │  │    Engine    │  │    Cache     │  │ Config Cache │                 │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                 │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Network Topology

```
                    ┌─────────────────┐
                    │  Peer Network   │
                    │     (gRPC)      │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  Validator 1  │◀──▶│  Validator 2  │◀──▶│  Validator 3  │
│   (LEADER)    │    │  (Confirmer)  │    │  (Confirmer)  │
│  Builds Dockets    │  Signs Dockets │    │  Signs Dockets │
└───────┬───────┘    └───────────────┘    └───────────────┘
        │
        ▼
┌───────────────┐
│   Register    │
│   Service     │
└───────────────┘
```

### Leader Election

Only one validator (the **leader**) initiates docket builds at any time. This ensures:
- **Sequencing:** Docket sequence numbers are assigned without conflicts
- **Ordering:** Transaction ordering is deterministic
- **Efficiency:** No competing docket builds

**Election Mechanisms (configurable per register):**

| Mechanism | Description | Use Case |
|-----------|-------------|----------|
| **Rotating** | Round-robin based on validator order | Simple, predictable |
| **Raft-style** | Heartbeat-based leader election | Fault-tolerant |
| **Stake-weighted** | Higher stake = higher election probability | Incentive-aligned |

**Leader Responsibilities:**
- Poll transaction pool and build dockets
- Assign docket sequence numbers
- Broadcast dockets for confirmation
- Collect signatures and commit

**Leader Failure:**
- Confirmers detect leader timeout (no heartbeat/docket)
- New leader elected per configured mechanism
- Pending docket abandoned, transactions return to pool

---

## Data Flow

### 1. Initiator Flow (Building Dockets)

```
Transactions arrive via Peer Service
         │
         ▼
Redis Memory Pool (unverified)
         │
         ▼
┌─────────────────────────────────────┐
│        VALIDATION PROCESS           │
│  1. Fetch Blueprint JSON (cached)   │
│  2. Validate chain (previousId)     │
│  3. Validate schemas (DataSchemas)  │
│  4. Evaluate conditions (JsonLogic) │
│  5. Check disclosures               │
│  6. Verify participant auth         │
└─────────────────────────────────────┘
         │
    ┌────┴────┐
    │         │
  VALID    INVALID
    │         │
    ▼         ▼
In-Memory   Exception Response
Verified    → Peer Service
Queue       → Original Sender
    │
    ▼
┌─────────────────────────────────────┐
│         DOCKET BUILDING             │
│  1. Read genesis config             │
│     - signatureThreshold            │
│     - docketTimeout                 │
│     - maxSignatures                 │
│  2. Build docket from verified txs  │
│  3. Compute Merkle root             │
│  4. Sign docket (initiator sig)     │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│      CONSENSUS COLLECTION           │
│  1. Broadcast to confirming         │
│     validators via Peer Service     │
│  2. Collect signatures              │
│  3. Wait until:                     │
│     - min threshold reached, OR     │
│     - timeout expires               │
│  4. Cap at maxSignatures            │
└─────────────────────────────────────┘
         │
         ▼ (threshold met)
┌─────────────────────────────────────┐
│           COMMITMENT                │
│  1. Submit to Register Service      │
│  2. Distribute via Peer Service     │
└─────────────────────────────────────┘
```

### 2. Confirmer Flow (Validating Dockets)

```
Receive Docket from Peer Service
         │
         ▼
┌─────────────────────────────────────┐
│       DOCKET VALIDATION             │
│  1. Verify initiator signature      │
│  2. Validate ALL transactions       │
│     (same validation as initiator)  │
│  3. Verify Merkle root              │
│  4. Check docket structure          │
└─────────────────────────────────────┘
         │
    ┌────┴────┐
    │         │
  VALID    INVALID
    │         │
    ▼         ▼
Sign &     Reject
Return     (log for bad actor detection)
to         (do not sign)
Initiator
```

### 3. Storage Layers

| Layer | Storage | Purpose | Persistence |
|-------|---------|---------|-------------|
| **Unverified Pool** | Redis | Pending transactions from Peer Service | Persistent, TTL-based |
| **Verified Queue** | In-Memory | Validated transactions ready for docket | Volatile, size-limited |
| **Pending Dockets** | In-Memory | Dockets awaiting signature threshold | Volatile, timeout-based |
| **Committed Docket** | Register | Sealed, signed docket | Permanent |

---

## Transaction Chain Model

### Multi-Blueprint Register with Versioning

```
Genesis Block (txid0)
│
├── Contains: Register Control Blueprint
│   ├── signatureThreshold: { min: 3, max: 10 }
│   ├── docketTimeout: "PT30S"
│   ├── validatorRegistration: "public" | "consent"
│   └── maxSignaturesPerDocket: 10
│
├── Blueprint A v1 (previousId = txid0)
│   │
│   ├── Instance A1 Action 1 (previousId = Blueprint A v1 txid)
│   │   └── Instance A1 Action 2 (previousId = A1 Action 1 txid)
│   │
│   └── Instance A2 Action 1 (previousId = Blueprint A v1 txid)
│
├── Blueprint A v2 (previousId = Blueprint A v1 txid)  ← VERSION UPDATE
│   │
│   └── Instance A3 Action 1 (previousId = Blueprint A v2 txid)
│
└── Blueprint B v1 (previousId = txid0)  ← DIFFERENT BLUEPRINT
    │
    └── Instance B1 Action 1 (previousId = Blueprint B v1 txid)
```

### Transaction Chain Rules

| Transaction Type | previousId Value |
|-----------------|------------------|
| Genesis Block | null (root) |
| New Blueprint (v1) | Genesis block txid |
| Blueprint Update (v2+) | Prior blueprint version txid |
| First Action (Action 0) | Blueprint publication txid |
| Subsequent Actions | Prior action in same instance |

### Blueprint Versioning

- **New Blueprint:** `previousId` = genesis block transaction ID
- **Updated Blueprint:** `previousId` = prior version's transaction ID
- **Instance Actions:** Always reference the blueprint version they were instantiated from
- **Version Lookup:** Follow `previousId` chain to find all versions of a blueprint

---

## Core Components

### 1. Transaction Pool Poller

**Responsibility:** Read unverified transactions from Redis memory pool

**Configuration:**
```json
{
  "TransactionPool": {
    "RedisConnectionString": "localhost:6379",
    "PoolKey": "sorcha:transactions:unverified:{registerId}",
    "PollIntervalMs": 1000,
    "BatchSize": 100,
    "MaxRetries": 3
  }
}
```

**Behavior:**
- Poll Redis for unverified transactions
- Batch processing (configurable batch size)
- Retry logic for transient failures
- Dead letter queue for permanently failed transactions

### 2. Validation Engine

**Responsibility:** Validate transaction data against Blueprint rules

**Dependencies:**
- `Sorcha.Blueprint.Engine.Interfaces.ISchemaValidator`
- `Sorcha.Blueprint.Engine.Interfaces.IJsonLogicEvaluator`
- `Sorcha.Blueprint.Engine.Interfaces.IDisclosureProcessor`
- `Sorcha.Validator.Core` (enclave-safe validation library)

**Validation Steps:**

```csharp
public async Task<ValidationResult> ValidateTransactionAsync(Transaction transaction)
{
    // 1. Fetch Blueprint (from cache or Blueprint Service)
    var blueprint = await _blueprintCache.GetAsync(transaction.BlueprintId);

    // 2. Identify target action
    var action = blueprint.Actions.FirstOrDefault(a => a.Id == transaction.ActionId);
    if (action == null)
        return ValidationResult.Failed(ValidationErrorType.ActionNotFound);

    // 3. Chain validation (previousId)
    var chainResult = await ValidateChainAsync(transaction, blueprint);
    if (!chainResult.IsValid)
        return chainResult;

    // 4. Previous data validation
    if (transaction.PreviousData != null)
    {
        var previousTx = await _transactionCache.GetAsync(transaction.PreviousId);
        if (!ValidatePreviousData(transaction.PreviousData, previousTx.Data))
            return ValidationResult.Failed(ValidationErrorType.PreviousDataMismatch);
    }

    // 5. Schema validation
    var schemaResult = await _schemaValidator.ValidateAsync(
        transaction.Data, action.DataSchemas);
    if (!schemaResult.IsValid)
        return schemaResult;

    // 6. JSON Logic condition evaluation
    if (action.Condition != null)
    {
        var conditionResult = _jsonLogicEvaluator.Evaluate(
            action.Condition, transaction.Data);
        if (!conditionResult.IsValid)
            return conditionResult;
    }

    // 7. Disclosure rules validation
    var disclosureResult = _disclosureProcessor.ValidateDisclosures(
        transaction.Data, action.Disclosures, transaction.Sender);
    if (!disclosureResult.IsValid)
        return disclosureResult;

    // 8. Participant authorization
    if (!blueprint.Participants.Any(p => p.Id == transaction.Sender))
        return ValidationResult.Failed(ValidationErrorType.UnauthorizedSender);

    // 9. Signature verification
    var signatureValid = await _cryptoService.VerifySignatureAsync(
        transaction.Signature, transaction.Sender);
    if (!signatureValid)
        return ValidationResult.Failed(ValidationErrorType.SignatureVerificationFailed);

    return ValidationResult.Success(transaction);
}

private async Task<ValidationResult> ValidateChainAsync(
    Transaction tx, Blueprint blueprint)
{
    // Genesis/Blueprint publication: previousId should be genesis or prior version
    if (tx.Type == TransactionType.BlueprintPublication)
    {
        // New blueprint: previousId = genesis
        // Updated blueprint: previousId = prior version
        var previousTx = await _transactionCache.GetAsync(tx.PreviousId);
        if (previousTx == null)
            return ValidationResult.Failed(ValidationErrorType.InvalidPreviousId);

        // If updating, verify it's the same blueprint being versioned
        if (previousTx.Type == TransactionType.BlueprintPublication &&
            previousTx.BlueprintId != tx.BlueprintId)
            return ValidationResult.Failed(ValidationErrorType.InvalidBlueprintVersion);

        return ValidationResult.Success();
    }

    // Action transaction: previousId must reference valid prior tx
    var previous = await _transactionCache.GetAsync(tx.PreviousId);
    if (previous == null)
        return ValidationResult.Failed(ValidationErrorType.InvalidPreviousId);

    // First action (Action 0): previousId = blueprint publication tx
    if (tx.ActionId == 0)
    {
        if (previous.Type != TransactionType.BlueprintPublication ||
            previous.BlueprintId != tx.BlueprintId)
            return ValidationResult.Failed(ValidationErrorType.BrokenChain);
    }
    // Subsequent actions: previousId = prior action in same instance
    else
    {
        if (previous.BlueprintId != tx.BlueprintId)
            return ValidationResult.Failed(ValidationErrorType.BrokenChain);
    }

    return ValidationResult.Success();
}
```

### 3. Blueprint Cache

**Responsibility:** Cache Blueprint JSON to avoid repeated HTTP calls

**Configuration:**
```json
{
  "BlueprintCache": {
    "Provider": "Redis",
    "ConnectionString": "localhost:6379",
    "CacheKeyPrefix": "sorcha:blueprint:",
    "DefaultTTLSeconds": 300,
    "MaxCacheSize": 1000
  }
}
```

### 4. Genesis Config Cache

**Responsibility:** Cache register governance configuration from genesis blueprint

**Configuration:**
```json
{
  "GenesisConfigCache": {
    "Provider": "Redis",
    "CacheKeyPrefix": "sorcha:genesis:",
    "DefaultTTLSeconds": 600,
    "RefreshOnUpdate": true
  }
}
```

**Cached Properties:**
- `signatureThreshold` (min/max)
- `docketTimeout`
- `maxSignaturesPerDocket`
- `validatorRegistration` (public/consent)
- Registered validators list

### 5. Verified Transaction Queue (In-Memory)

**Responsibility:** Hold validated transactions ready for docket building

**Configuration:**
```json
{
  "VerifiedQueue": {
    "MaxSizeBytes": 104857600,
    "MaxTransactionCount": 10000,
    "EvictionPolicy": "FIFO",
    "PersistenceBackup": "Redis",
    "BackupIntervalSeconds": 60
  }
}
```

### 6. Docket Builder

**Responsibility:** Build dockets from verified transactions and initiate consensus

**Configuration:**
```json
{
  "DocketBuilder": {
    "MaxDocketSize": 1000,
    "BuildIntervalSeconds": 30,
    "MinTransactionsForBuild": 10,
    "SealingAlgorithm": "MerkleRoot"
  }
}
```

**Behavior:**
1. Triggered by timer, transaction count threshold, or manual trigger
2. Read genesis config for signature requirements
3. Build docket with Merkle root
4. Sign with validator's key (first signature)
5. Broadcast to confirming validators via Peer Service
6. Await signature collection

### 7. Signature Collector

**Responsibility:** Collect signatures from confirming validators

**Behavior:**
```csharp
public async Task<SignatureCollectionResult> CollectSignaturesAsync(
    Docket docket, GenesisConfig config, CancellationToken ct)
{
    var signatures = new List<ValidatorSignature> { docket.InitiatorSignature };
    var timeout = config.DocketTimeout;
    var minThreshold = config.SignatureThreshold.Min;
    var maxSignatures = config.MaxSignaturesPerDocket;

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(timeout);

    try
    {
        await foreach (var signature in _peerService.ReceiveSignaturesAsync(
            docket.Id, cts.Token))
        {
            // Verify signature is from registered validator
            if (!await _validatorRegistry.IsRegisteredAsync(signature.ValidatorId))
                continue;

            signatures.Add(signature);

            // Check if we've reached max
            if (signatures.Count >= maxSignatures)
                break;
        }
    }
    catch (OperationCanceledException)
    {
        // Timeout - check if we have enough
    }

    return new SignatureCollectionResult
    {
        Signatures = signatures,
        ThresholdMet = signatures.Count >= minThreshold,
        TimedOut = cts.IsCancellationRequested
    };
}
```

### Consensus Failure Handling

**When signature threshold is NOT met by timeout:**

```
┌─────────────────────────────────────────────────────────────┐
│                   CONSENSUS FAILURE FLOW                     │
│                                                              │
│  Docket Build Complete                                       │
│         │                                                    │
│         ▼                                                    │
│  Broadcast to Confirmers ──────────────────┐                │
│         │                                   │                │
│         ▼                                   ▼                │
│  Wait for Signatures              Timeout Expires            │
│         │                              │                     │
│    ┌────┴────┐                         │                     │
│    │         │                         │                     │
│ Threshold  Threshold                   │                     │
│   MET      NOT MET ◀───────────────────┘                    │
│    │         │                                               │
│    ▼         ▼                                               │
│ COMMIT    ABANDON                                            │
│    │         │                                               │
│    │         ├── Log failure reason                          │
│    │         ├── Return transactions to unverified pool      │
│    │         ├── Clear pending docket                        │
│    │         └── Transactions eligible for next docket       │
│    │                                                         │
│    ▼                                                         │
│ Submit to Register + Distribute                              │
└─────────────────────────────────────────────────────────────┘
```

**Implementation:**
```csharp
public async Task HandleConsensusResultAsync(
    Docket docket, SignatureCollectionResult result)
{
    if (result.ThresholdMet)
    {
        // SUCCESS: Commit docket
        docket.Signatures = result.Signatures;
        await _registerService.SubmitDocketAsync(docket);
        await _peerService.DistributeDocketAsync(docket);
        _metrics.DocketsCommitted.Inc();
    }
    else
    {
        // FAILURE: Abandon and retry
        _logger.LogWarning(
            "Consensus failed for docket {DocketId}: {Signatures}/{Required} signatures",
            docket.Id, result.Signatures.Count, config.SignatureThreshold.Min);

        // Return transactions to unverified pool
        foreach (var tx in docket.Transactions)
        {
            await _transactionPool.ReturnToUnverifiedAsync(tx);
        }

        // Clear pending docket
        await _pendingDocketStore.RemoveAsync(docket.Id);

        _metrics.DocketsAbandoned.Inc();
    }
}
```

**Retry Behavior:**
- Abandoned transactions return to unverified pool
- Transactions are eligible for the next docket build cycle
- No special retry counter (transactions may be included in future dockets)
- Persistent failures may indicate network issues or bad actors

### 8. Docket Confirmer

**Responsibility:** Validate and sign dockets received from other validators

**Behavior:**
```csharp
public async Task<ConfirmationResult> ConfirmDocketAsync(Docket docket)
{
    // 1. Verify initiator signature
    if (!await _cryptoService.VerifySignatureAsync(
        docket.InitiatorSignature, docket.InitiatorId))
    {
        return ConfirmationResult.Rejected(RejectionReason.InvalidInitiatorSignature);
    }

    // 2. Validate all transactions
    foreach (var tx in docket.Transactions)
    {
        var result = await _validationEngine.ValidateTransactionAsync(tx);
        if (!result.IsValid)
        {
            _badActorDetector.LogRejection(docket.InitiatorId, result);
            return ConfirmationResult.Rejected(RejectionReason.InvalidTransaction);
        }
    }

    // 3. Verify Merkle root
    var computedRoot = MerkleTree.ComputeRoot(docket.Transactions);
    if (computedRoot != docket.MerkleRoot)
    {
        return ConfirmationResult.Rejected(RejectionReason.InvalidMerkleRoot);
    }

    // 4. Sign and return
    var signature = await _cryptoService.SignAsync(docket.Id, _validatorKey);
    return ConfirmationResult.Confirmed(signature);
}
```

### 9. Exception Response Handler

**Responsibility:** Send validation failures back to original sender via Peer Service

**Behavior:**
- Create detailed error response
- Route via Peer Service to original sender
- Log for analytics

---

## Docket Structure

```json
{
  "id": "docket-uuid",
  "registerId": "register-uuid",
  "previousDocketId": "prev-docket-uuid",
  "sequenceNumber": 42,
  "transactions": [
    {
      "id": "tx-uuid-1",
      "blueprintId": "blueprint-uuid",
      "actionId": 1,
      "previousId": "tx-uuid-0",
      "sender": "participant-id",
      "data": { },
      "signature": "...",
      "timestamp": "2026-01-26T10:00:00Z"
    }
  ],
  "merkleRoot": "0x...",
  "createdAt": "2026-01-26T10:00:00Z",
  "initiatorId": "validator-1-address",
  "signatures": [
    {
      "validatorId": "validator-1-address",
      "signature": "0x...",
      "timestamp": "2026-01-26T10:00:01Z",
      "isInitiator": true
    },
    {
      "validatorId": "validator-2-address",
      "signature": "0x...",
      "timestamp": "2026-01-26T10:00:02Z",
      "isInitiator": false
    },
    {
      "validatorId": "validator-3-address",
      "signature": "0x...",
      "timestamp": "2026-01-26T10:00:03Z",
      "isInitiator": false
    }
  ],
  "consensusMetadata": {
    "thresholdRequired": 3,
    "signaturesCollected": 3,
    "timeoutMs": 30000,
    "completedAt": "2026-01-26T10:00:03Z"
  }
}
```

---

## Transaction Structure

```json
{
  "id": "txid3",
  "type": "Action",
  "blueprintId": "blueprint-uuid",
  "actionId": 1,
  "previousId": "txid2",
  "sender": "participant-id",
  "data": {
  },
  "previousData": {
  },
  "timestamp": "2026-01-26T10:00:00Z",
  "signature": "...",
  "nonce": 12345
}
```

**Transaction Types:**
- `Genesis` - Register initialization with control blueprint
- `BlueprintPublication` - New blueprint or version update
- `Action` - Workflow action execution

---

## Integration Points

### 1. Peer Service (Bidirectional)

**Inbound:**
- Receive unverified transactions (queue to Redis)
- Receive dockets for confirmation (confirmer role)
- Receive signatures from confirmers (initiator role)

**Outbound:**
- Send exception responses for failed validations
- Broadcast dockets for confirmation
- Return signatures to initiators
- Distribute committed dockets

**Message Types:**
```csharp
public enum PeerMessageType
{
    // Inbound
    TransactionSubmission,
    DocketForConfirmation,
    SignatureResponse,

    // Outbound
    ValidationException,
    DocketBroadcast,
    SignatureProvided,
    DocketCommitted
}
```

### 2. Blueprint Service
**Endpoint:** `GET /api/blueprints/{id}`
**Purpose:** Fetch Blueprint JSON for validation
**Caching:** Yes (Redis, 5min TTL)

### 3. Register Service
**Endpoint:** `POST /api/registers/{id}/dockets`
**Purpose:** Submit committed docket with signatures
**Payload:** Docket with all collected signatures

### 4. Redis Keys

| Key Pattern | Purpose |
|-------------|---------|
| `sorcha:tx:unverified:{registerId}` | Pending transactions from Peer Service |
| `sorcha:tx:processing:{registerId}` | Currently validating |
| `sorcha:tx:invalid:{registerId}` | Dead letter queue |
| `sorcha:blueprint:{id}` | Cached blueprints |
| `sorcha:genesis:{registerId}` | Cached genesis config |
| `sorcha:docket:pending:{docketId}` | Dockets awaiting signatures |
| `sorcha:validators:{registerId}` | Registered validators for register |

---

## Validator Registration

### Public Registers
- Validators can volunteer to confirm dockets
- Registration via Peer Service announcement
- May require stake or reputation threshold (future)

### Private Registers (Consent-Based)
- Register owners must approve validators
- Approval recorded in control docket
- Revocation supported via control actions

**Registration Model:**
```json
{
  "validatorId": "validator-address",
  "registerId": "register-uuid",
  "registrationType": "public" | "consent",
  "registeredAt": "2026-01-26T10:00:00Z",
  "status": "active" | "suspended" | "revoked",
  "metadata": {
    "endpoint": "https://validator.example.com",
    "publicKey": "0x..."
  }
}
```

---

## Configuration Requirements

### 1. Consensus Settings (from Genesis Blueprint)

These are read from the genesis block's control blueprint, not local config:

```json
{
  "signatureThreshold": {
    "min": 3,
    "max": 10
  },
  "docketTimeout": "PT30S",
  "maxSignaturesPerDocket": 10,
  "validatorRegistration": "public"
}
```

### 2. Local Validator Configuration

```json
{
  "Validator": {
    "ValidatorId": "validator-1-address",
    "PrivateKeyPath": "/secrets/validator.key",
    "Roles": ["initiator", "confirmer"],
    "RegisterIds": ["register-1", "register-2"]
  }
}
```

### 3. Memory Management

```json
{
  "MemoryLimits": {
    "VerifiedQueueMaxBytes": 104857600,
    "BlueprintCacheMaxBytes": 52428800,
    "PendingDocketsMaxBytes": 26214400,
    "TotalMaxBytes": 209715200
  }
}
```

### 4. Performance Tuning

```json
{
  "Performance": {
    "MaxConcurrentValidations": 10,
    "ValidationTimeoutSeconds": 30,
    "PollerThreadCount": 2,
    "EnableParallelValidation": true,
    "ValidationBatchSize": 100
  }
}
```

---

## Validation Rules

### 1. Schema Validation (JSON Schema Draft 2020-12)
- Transaction data must conform to `Action.DataSchemas`
- Required fields must be present
- Data types must match

### 2. JSON Logic Conditions
- Evaluate `Action.Condition` against transaction data
- Condition must evaluate to valid next action ID or boolean

### 3. Disclosure Rules
- Transaction sender must be authorized to submit this action
- Disclosure recipients must be valid participants

### 4. Participant Authorization
- Transaction sender must exist in `Blueprint.Participants`
- Wallet signature must match participant's wallet address

### 5. Chain Validation
- `previousId` must reference valid transaction
- Chain continuity validated per transaction type rules
- Blueprint versioning chain validated

### 6. Docket Validation (Confirmer)
- All transactions individually valid
- Merkle root matches computed value
- Initiator signature valid
- Docket structure valid

---

## Error Handling

### Validation Error Types

```csharp
public enum ValidationErrorType
{
    // Schema Errors
    SchemaMismatch,
    MissingRequiredField,
    InvalidDataType,

    // Logic Errors
    ConditionEvaluationFailed,
    InvalidNextAction,

    // Authorization Errors
    UnauthorizedSender,
    InvalidParticipant,
    SignatureVerificationFailed,

    // Workflow Errors
    BlueprintNotFound,
    ActionNotFound,
    InvalidActionSequence,

    // Chain Validation Errors
    InvalidPreviousId,
    BrokenChain,
    PreviousDataMismatch,
    InvalidBlueprintVersion,

    // Consensus Errors
    InvalidInitiatorSignature,
    InvalidMerkleRoot,
    ThresholdNotMet,
    DocketTimeout,

    // System Errors
    ValidationTimeout,
    ServiceUnavailable
}
```

---

## API Endpoints

### Health & Monitoring
```http
GET /health
GET /metrics
GET /ready
```

### Transaction Pool
```http
GET  /api/pool/stats
GET  /api/pool/unverified/count
GET  /api/pool/verified/count
POST /api/validate/{transactionId}
```

### Docket Management
```http
GET  /api/dockets/pending
POST /api/dockets/build
GET  /api/dockets/{id}/signatures
```

### Validator Registration
```http
GET  /api/validators
POST /api/validators/register
GET  /api/validators/{id}/status
```

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Validation throughput | 1000 tx/sec |
| Validation latency (P50) | < 50ms |
| Validation latency (P99) | < 200ms |
| Signature collection (P50) | < 5s |
| Memory usage | < 200 MB |
| Blueprint cache hit rate | > 95% |

---

## Security Considerations

1. **Transaction Signature Verification** - Verify cryptographic signature matches sender wallet
2. **Validator Signature Verification** - Verify all docket signatures from registered validators
3. **Replay Protection** - Nonce validation, transaction ID uniqueness
4. **Rate Limiting** - Limit validation requests per participant
5. **Bad Actor Detection** - Track rejection patterns (future: throttle/remove)
6. **Data Privacy** - Never log full transaction data (may contain PII)

---

## Future Enhancements

1. **Bad Actor Management** - Throttling, reputation scores, removal from network
2. **Priority Queues** - Fee-based transaction prioritization
3. **Horizontal Scaling** - Multiple validator instances with distributed coordination
4. **Validator Incentives** - Reward mechanisms for honest validation

---

## Related Documents

- [GENESIS-BLUEPRINT-SPEC.md](GENESIS-BLUEPRINT-SPEC.md) - Genesis block and control blueprint specification
- [MASTER-TASKS.md](MASTER-TASKS.md) - Sprint 9 implementation tasks
- [constitution.md](constitution.md) - Project standards and principles

---

**Last Updated:** 2026-01-26
**Document Owner:** Sorcha Architecture Team
