# Validator Service - Requirements Document

**Created:** 2025-11-19
**Status:** Requirements Gathering
**Purpose:** Define requirements for Sorcha.Validator.Service rebuild

---

## Executive Summary

The Validator Service validates transactions from the memory pool against Blueprint rules before they are promoted to in-memory storage for docket building. It acts as a gatekeeper ensuring only valid transactions are processed into the register.

### Key Requirements

1. **Transaction Source:** Transactions arrive as **messages from Peer Service** and are queued in Redis memory pool
2. **Validation Failures:** Invalid transactions are sent back to **original sender as exceptions** via Peer Service
3. **Single Blueprint:** Transactions **cannot** reference multiple blueprints
4. **Chain-Based Instances:** Workflow instances tracked via **transaction chain** (previousId), not separate instance IDs
5. **Instance Isolation:** Each chain branch from Blueprint = separate instance, action transactions are instance data
6. **Memory Management:** Verified transactions stored in **configurable in-memory queue** with size limits
7. **Docket Building:** Dockets built from verified in-memory transactions only
8. **Chain Validation:** Validator must validate transaction chain continuity via previousId references

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Validator Service                         │
│                                                              │
│  ┌──────────────┐      ┌─────────────┐      ┌────────────┐ │
│  │   Poller     │─────>│  Validator  │─────>│  Promoter  │ │
│  │  (Redis)     │      │   Engine    │      │ (In-Memory)│ │
│  └──────────────┘      └─────────────┘      └────────────┘ │
│         │                     │  │                  │        │
│         v                     │  │ INVALID          v        │
│   ┌──────────┐         ┌─────v──v───────┐   ┌──────────┐   │
│   │  Memory  │<────────│   Exception    │   │ Verified │   │
│   │   Pool   │         │   Response     │   │   Queue  │   │
│   │ (Redis)  │         │ (Peer Service) │   │(In-Memory)│  │
│   └──────────┘         └────────────────┘   └──────────┘   │
│         ^                     ^                     │        │
│         │              ┌──────────┐          ┌──────v─────┐ │
│    ┌────┴─────┐        │Blueprint │          │  Docket    │ │
│    │  Peer    │        │ Service  │          │  Builder   │ │
│    │ Service  │        │  (HTTP)  │          └────────────┘ │
│    │(Messages)│        └──────────┘                         │
│    └──────────┘                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### 1. Transaction Lifecycle

```
Peer Service (Messages)
         │
         v
Unverified Transaction (Redis MemoryPool)
         │
         v
    Validation Process
    - Fetch Blueprint JSON
    - Validate against DataSchemas
    - Check JSON Logic conditions
    - Verify Disclosure rules
    - Check Participant authorization
         │
    ┌────┴────┐
    │         │
    v         v
VALID      INVALID
    │         │
    v         v
In-Memory   Exception Response
Verified    │
Queue       v
    │       Send to Peer Service
    v       │
Docket      v
Builder     Return to Original Sender
    │       (Transaction Rejection)
    v
Register Block
```

**Transaction Source:**
- Transactions arrive as **messages from Peer Service**
- Peer Service receives transactions from network participants
- Messages are queued in Redis memory pool for validation

**Validation Failure Handling:**
- Failed transactions are sent back to **original sender as exceptions**
- Exception sent via **Peer Service** (reverse channel)
- Transaction is **not** added to verified queue
- Detailed validation errors included in exception response

### 2. Storage Layers

| Layer | Storage | Purpose | Persistence |
|-------|---------|---------|-------------|
| **Unverified Pool** | Redis | Pending transactions awaiting validation | Persistent, TTL-based |
| **Verified Queue** | In-Memory | Validated transactions ready for docket | Volatile, size-limited |
| **Docket** | Register | Sealed block of transactions | Permanent |

---

## Core Components

### 1. Transaction Pool Poller

**Responsibility:** Read unverified transactions from Redis memory pool

**Configuration:**
```json
{
  "TransactionPool": {
    "RedisConnectionString": "localhost:6379",
    "PoolKey": "sorcha:transactions:unverified",
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
- `Sorcha.Blueprint.Models.Blueprint` (JSON deserialization)

**Validation Steps:**

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public Transaction ValidatedTransaction { get; set; }
}

public async Task<ValidationResult> ValidateAsync(Transaction transaction)
{
    // 1. Fetch Blueprint JSON
    var blueprint = await FetchBlueprintAsync(transaction.BlueprintId);

    // 2. Identify target action
    var action = blueprint.Actions.FirstOrDefault(a => a.Id == transaction.ActionId);

    // 3. Chain validation (previousId)
    var chainValid = await ValidateChainAsync(transaction);
    if (!chainValid.IsValid)
    {
        return chainValid;
    }

    // 4. Previous data validation
    var previousTx = await FetchTransactionAsync(transaction.PreviousId);
    if (transaction.PreviousData != null &&
        !ValidatePreviousData(transaction.PreviousData, previousTx.Data))
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError { Type = ValidationErrorType.PreviousDataMismatch }
            }
        };
    }

    // 5. Schema validation (DataSchemas)
    var schemaResult = await _schemaValidator.ValidateAsync(
        transaction.Data,
        action.DataSchemas
    );

    // 6. JSON Logic condition evaluation (if applicable)
    if (action.Condition != null)
    {
        var conditionResult = _jsonLogicEvaluator.Evaluate(
            action.Condition,
            transaction.Data
        );
    }

    // 7. Disclosure rules validation
    var disclosureResult = _disclosureProcessor.ValidateDisclosures(
        transaction.Data,
        action.Disclosures,
        transaction.Sender
    );

    // 8. Participant authorization
    var participantValid = blueprint.Participants.Any(
        p => p.Id == transaction.Sender
    );

    return new ValidationResult
    {
        IsValid = schemaResult.IsValid && participantValid,
        Errors = CollectErrors(...)
    };
}

private async Task<ValidationResult> ValidateChainAsync(Transaction tx)
{
    // Verify previousId references valid transaction
    var previousTx = await FetchTransactionAsync(tx.PreviousId);
    if (previousTx == null)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError { Type = ValidationErrorType.InvalidPreviousId }
            }
        };
    }

    // Verify chain continuity
    // Action 0 should reference Blueprint publication transaction
    // Action N should reference previous action in same instance
    if (tx.ActionId == 0 && previousTx.BlueprintId != tx.BlueprintId)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError { Type = ValidationErrorType.BrokenChain }
            }
        };
    }

    return new ValidationResult { IsValid = true };
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

**Behavior:**
- Fetch Blueprint from Blueprint.Service on cache miss
- Store in Redis with TTL
- Invalidate on Blueprint updates (webhook/event)
- LRU eviction when cache full

### 4. Verified Transaction Queue (In-Memory)

**Responsibility:** Hold validated transactions ready for docket building

**Configuration:**
```json
{
  "VerifiedQueue": {
    "MaxSizeBytes": 104857600,        // 100 MB
    "MaxTransactionCount": 10000,
    "EvictionPolicy": "FIFO",         // FIFO, LRU, Priority
    "PersistenceBackup": "Redis",     // Optional backup to Redis
    "BackupIntervalSeconds": 60
  }
}
```

**Features:**
- **Size Limits:**
  - Maximum bytes (configurable)
  - Maximum transaction count (configurable)
- **Eviction Policies:**
  - FIFO: First-in, first-out
  - LRU: Least recently used
  - Priority: Based on transaction priority/fee
- **Optional Backup:**
  - Periodically backup to Redis
  - Restore on service restart

### 5. Docket Builder

**Responsibility:** Build dockets from verified transactions

**Configuration:**
```json
{
  "DocketBuilder": {
    "MaxDocketSize": 1000,            // Max transactions per docket
    "BuildIntervalSeconds": 30,       // Build docket every N seconds
    "MinTransactionsForBuild": 10,    // Minimum txs before building
    "SealingAlgorithm": "MerkleRoot"
  }
}
```

**Behavior:**
- Triggered by:
  - Timer (every N seconds)
  - Transaction count threshold
  - Manual trigger (API endpoint)
- Seal docket with Merkle root
- Submit to Register.Service
- Clear in-memory queue after successful submission

---

## Transaction Structure

```json
{
  "id": "txid3",
  "blueprintId": "blueprint-uuid",
  "actionId": 1,
  "previousId": "txid2",
  "sender": "participant-id",
  "data": {
    // Action-specific data (validated against DataSchemas)
  },
  "previousData": {
    // Data from previous action (if applicable)
  },
  "timestamp": "2025-11-19T10:00:00Z",
  "signature": "...",  // Cryptographic signature
  "nonce": 12345
}
```

**Transaction Chain Model:**

The transaction chain itself provides instance tracking through `previousId` - no separate instance ID needed.

**Example: Two workflow instances from same Blueprint:**

```
Genesis Block
  txid0
    ↓
Blueprint v1 Published
  txid1 (previousId = txid0)
    ├────────────────┬────────────────┐
    │                │                │
Instance 1       Instance 2      Instance 3
Action 1         Action 1        Action 1
  txid2            txid4           txid6
  (prev=txid1)     (prev=txid1)    (prev=txid1)
    ↓
Instance 1
Action 2
  txid3
  (prev=txid2)
```

**Blueprint Execution Model:**
- **Single Blueprint Per Transaction:** Transactions **cannot** reference multiple blueprints
- **Chain-Based Instance Tracking:** Workflow instance is determined by the transaction chain via `previousId`
- **Multiple Concurrent Instances:** Multiple executions of same Blueprint = multiple chains branching from the published Blueprint transaction
- **Instance Isolation:** Each chain represents a separate workflow instance with its own state

**Instance Data:**
- Instance tracking is **implicit in the transaction chain**
- `previousId` links transactions in the same workflow instance
- Action transactions are **instance data** of a particular blueprint flow execution
- Example workflow chains:
  - **Instance 1:** txid1 → txid2 → txid3 (employee Alice's expense approval)
  - **Instance 2:** txid1 → txid4 (employee Bob's expense approval)

**Data Isolation:**
- Instance data is scoped by the transaction chain (following `previousId`)
- `previousData` references the transaction in the same chain
- Cross-instance data access is not supported
- Each branch from the Blueprint transaction = new instance

---

## Integration Points

### 1. Peer Service (Message Source)
**Purpose:** Receive transactions from network participants
**Integration:**
- **Inbound:** Peer Service sends transaction messages to Redis memory pool
- **Outbound:** Validator sends exception responses back to Peer Service for failed validations
**Message Format:**
```json
{
  "messageId": "msg-uuid",
  "transactionId": "tx-uuid",
  "sender": "participant-id",
  "transaction": { ... },
  "routingInfo": {
    "sourceNode": "peer-node-123",
    "returnPath": "peer-service/responses/{messageId}"
  }
}
```
**Exception Response:**
```json
{
  "messageId": "msg-uuid",
  "transactionId": "tx-uuid",
  "status": "rejected",
  "validationErrors": [ ... ],
  "timestamp": "2025-11-19T10:00:00Z"
}
```

### 2. Blueprint Service
**Endpoint:** `GET /api/blueprints/{id}`
**Purpose:** Fetch Blueprint JSON for validation
**Caching:** Yes (Redis, 5min TTL)

### 3. Register Service
**Endpoint:** `POST /api/registers/{id}/dockets`
**Purpose:** Submit sealed docket
**Payload:** Docket with verified transactions

### 4. Transaction Pool (Redis)
**Keys:**
- `sorcha:transactions:unverified` - Queue of pending transactions from Peer Service
- `sorcha:transactions:invalid` - Dead letter queue
- `sorcha:transactions:processing` - Currently validating
- `sorcha:transactions:responses` - Exception responses to send back via Peer Service

### 5. Event Bus (Optional)
**Events:**
- `TransactionValidated` - Transaction passed validation
- `TransactionRejected` - Transaction failed validation (sent to sender)
- `DocketBuilt` - New docket created
- `DocketSealed` - Docket submitted to register

---

## Configuration Requirements

### 1. Memory Management

```json
{
  "MemoryLimits": {
    "VerifiedQueueMaxBytes": 104857600,     // 100 MB
    "BlueprintCacheMaxBytes": 52428800,     // 50 MB
    "WorkingMemoryMaxBytes": 52428800,      // 50 MB
    "TotalMaxBytes": 209715200,             // 200 MB
    "GarbageCollectionMode": "Server",
    "LargeObjectHeapCompactionMode": "Always"
  }
}
```

### 2. Performance Tuning

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

### 3. Resilience

```json
{
  "Resilience": {
    "MaxRetries": 3,
    "RetryDelayMs": 1000,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 60,
    "EnableDeadLetterQueue": true,
    "DeadLetterQueueKey": "sorcha:transactions:dlq"
  }
}
```

### 4. Monitoring

```json
{
  "Monitoring": {
    "EnableMetrics": true,
    "MetricsPort": 9090,
    "HealthCheckEndpoint": "/health",
    "MetricsToTrack": [
      "transactions_validated_total",
      "transactions_rejected_total",
      "validation_duration_seconds",
      "verified_queue_size_bytes",
      "dockets_built_total"
    ]
  }
}
```

---

## Validation Rules

### 1. Schema Validation (JSON Schema Draft 2020-12)
- Transaction data must conform to `Action.DataSchemas`
- Required fields must be present
- Data types must match
- String patterns/formats validated
- Numeric ranges enforced

### 2. JSON Logic Conditions
- Evaluate `Action.Condition` against transaction data
- Condition must evaluate to valid next action ID or boolean
- Variables in condition must reference fields in transaction data
- Invalid condition syntax rejects transaction

### 3. Disclosure Rules
- Transaction sender must be authorized to submit this action
- `Action.Sender` must match transaction sender
- Disclosure recipients must be valid participants
- Data pointers must reference valid fields

### 4. Participant Authorization
- Transaction sender must exist in `Blueprint.Participants`
- Wallet signature must match participant's wallet address
- Stealth address validation if enabled

### 5. Workflow Integrity & Chain Validation
- Transaction must reference valid Blueprint
- Action ID must exist in Blueprint
- **previousId must reference valid transaction:**
  - For Action 0: `previousId` should reference the Blueprint publication transaction
  - For Action N: `previousId` should reference previous action in the same instance
  - Chain continuity validated (no broken chains)
- **Previous action data validation:**
  - `previousData` must match data from transaction referenced by `previousId`
  - Previous action must be from same Blueprint instance (same chain)
- **Instance chain validation:**
  - Validate transaction is on correct chain branch
  - Detect invalid chain merges
  - Ensure action sequence follows Blueprint workflow

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
    InvalidChainBranch,
    ChainMergeDetected,

    // System Errors
    ValidationTimeout,
    ServiceUnavailable
}
```

### Error Response

**Validation failures are sent back to the original sender as exceptions via Peer Service:**

```json
{
  "messageId": "original-msg-uuid",
  "transactionId": "tx-uuid",
  "status": "rejected",
  "isValid": false,
  "errors": [
    {
      "type": "SchemaMismatch",
      "field": "data.amount",
      "message": "Expected number, got string",
      "schemaPath": "#/properties/amount/type"
    }
  ],
  "timestamp": "2025-11-19T10:00:00Z",
  "returnPath": "via-peer-service"
}
```

**Exception Response Flow:**
1. Validation fails for transaction
2. Validator creates exception response with detailed errors
3. Exception sent to Peer Service response queue
4. Peer Service routes exception back to original sender
5. Transaction is **not** added to verified queue or docket

---

## API Endpoints (Validator.Service)

### Health & Monitoring

```http
GET /health
GET /metrics
GET /ready
```

### Transaction Pool Management

```http
GET /api/pool/stats
GET /api/pool/unverified/count
GET /api/pool/verified/count
```

### Manual Operations

```http
POST /api/validate/{transactionId}  # Force validation of specific transaction
POST /api/docket/build              # Manually trigger docket build
DELETE /api/pool/clear              # Clear in-memory verified queue (admin)
```

### Configuration

```http
GET /api/config                     # Get current configuration
PUT /api/config/memory-limits       # Update memory limits
PUT /api/config/validation-rules    # Update validation rules
```

---

## Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Validation throughput | 1000 tx/sec | Single instance |
| Validation latency (P50) | < 50ms | Including Blueprint fetch |
| Validation latency (P99) | < 200ms | Including Blueprint fetch |
| Memory usage | < 200 MB | Verified queue + cache |
| CPU usage | < 50% | 4-core system |
| Blueprint cache hit rate | > 95% | After warmup |

---

## Dependencies

### NuGet Packages
- `Aspire.StackExchange.Redis` (13.0.0+)
- `Sorcha.ServiceDefaults`
- `Sorcha.Blueprint.Engine`
- `Sorcha.Blueprint.Models`
- `Sorcha.Register.Models`
- `Microsoft.Extensions.Caching.Memory`
- `Microsoft.Extensions.Caching.StackExchangeRedis`

### External Services
- Redis (transaction pool, Blueprint cache)
- Blueprint.Service (HTTP client)
- Register.Service (HTTP client for docket submission)

---

## Security Considerations

1. **Transaction Signature Verification**
   - Verify cryptographic signature matches sender wallet
   - Prevent transaction replay attacks (nonce validation)

2. **Blueprint Integrity**
   - Verify Blueprint hasn't been tampered with
   - Consider Blueprint version tracking

3. **Rate Limiting**
   - Limit validation requests per participant
   - Prevent DoS via malformed transactions

4. **Data Privacy**
   - Never log full transaction data (may contain PII)
   - Respect disclosure rules in logging

---

## Testing Requirements

### Unit Tests
- Schema validation edge cases
- JSON Logic condition evaluation
- Disclosure rule validation
- Memory limit enforcement
- Cache eviction policies

### Integration Tests
- Full validation workflow (Redis → Validate → In-Memory)
- Blueprint.Service integration
- Register.Service docket submission
- Redis connection failure handling

### Performance Tests
- 1000 tx/sec throughput validation
- Memory usage under load
- Blueprint cache performance
- Concurrent validation stress test

---

## Future Enhancements

1. **Priority Queues**
   - Fee-based transaction prioritization
   - Express validation lane

2. **Advanced Caching**
   - Predictive Blueprint pre-fetching
   - Multi-level cache (L1: Memory, L2: Redis)

3. **Analytics**
   - Validation success/failure metrics
   - Transaction pattern analysis
   - Blueprint usage statistics

4. **Horizontal Scaling**
   - Multiple validator instances
   - Distributed locking for docket building
   - Shared Redis for coordination

---

## Implementation Phases

### Phase 1: Core Validation (MVP)
- Basic transaction validation
- Schema validation
- Redis transaction pool integration
- In-memory verified queue

### Phase 2: Docket Building
- Docket builder implementation
- Register.Service integration
- Merkle tree sealing

### Phase 3: Advanced Features
- Blueprint caching
- Performance optimization
- Monitoring & metrics

### Phase 4: Production Hardening
- Error handling & resilience
- Security hardening
- Load testing & optimization

---

## Open Questions

1. **Blueprint Updates:**
   - How to handle Blueprint version changes during active workflow instances?
   - Should in-flight transactions validate against old Blueprint versions?
   - Blueprint versioning strategy via transaction chain?

2. **Docket Finality:**
   - What happens if Register.Service rejects a docket?
   - Retry logic? Rollback verified transactions?
   - How to handle partial docket acceptance?

3. **Chain Validation:**
   - How far back should chain validation traverse (performance vs security)?
   - Should validator cache previous transactions for chain validation?
   - How to handle orphaned chains (previousId references missing transaction)?

4. **Exception Response Delivery:**
   - Does Peer Service guarantee exception delivery to original sender?
   - What if the original sender is offline?
   - Retry policy for exception responses?

5. **Blueprint Publication:**
   - How is a Blueprint published to the chain (becomes txid1)?
   - Who can publish Blueprints?
   - Blueprint update/versioning process?

---

**Next Steps:**
1. Review and approve requirements
2. Design detailed implementation plan
3. Create validation engine interface contracts
4. Implement MVP (Phase 1)
