# Genesis Blueprint Specification

**Created:** 2026-01-26
**Status:** Draft
**Purpose:** Define the structure and behavior of genesis blocks and register control blueprints

---

## Executive Summary

The genesis block is the foundational transaction of every Sorcha register. It serves two critical purposes:

1. **Cryptographic Anchor:** Establishes the root of the transaction chain with cryptographic initialization
2. **Governance Blueprint:** Contains the Register Control Blueprint that defines how the register operates

All register configuration - signature thresholds, timeouts, validator registration rules - is managed through a blueprint embedded in the genesis block, making register governance transparent, auditable, and on-chain.

**Important:** The control blueprint is **versionable**. Updates to register configuration (adding/removing organizations, changing properties, etc.) are published as new versions of the control blueprint via control dockets, following the same versioning pattern as regular blueprints.

---

## Genesis Block Structure

### Transaction Format

```json
{
  "id": "genesis-txid-uuid",
  "type": "Genesis",
  "previousId": null,
  "timestamp": "2026-01-26T00:00:00Z",
  "creator": "register-owner-address",
  "signature": "0x...",
  "data": {
    "registerId": "register-uuid",
    "registerName": "My Register",
    "registerDescription": "Purpose of this register",
    "controlBlueprint": {
      // Full Register Control Blueprint (see below)
    },
    "initialValidators": [
      {
        "validatorId": "validator-1-address",
        "publicKey": "0x...",
        "endpoint": "https://validator1.example.com",
        "registeredAt": "2026-01-26T00:00:00Z"
      }
    ],
    "cryptoConfig": {
      "hashAlgorithm": "SHA256",
      "signatureAlgorithm": "ED25519",
      "merkleTreeAlgorithm": "SHA256"
    }
  }
}
```

### Genesis Block Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string (UUID) | Yes | Unique transaction identifier |
| `type` | string | Yes | Always "Genesis" |
| `previousId` | null | Yes | Always null (root of chain) |
| `timestamp` | ISO 8601 | Yes | Creation timestamp |
| `creator` | string | Yes | Wallet address of register creator |
| `signature` | string | Yes | Creator's signature over the block |
| `data.registerId` | string (UUID) | Yes | Unique register identifier |
| `data.registerName` | string | Yes | Human-readable register name |
| `data.registerDescription` | string | No | Purpose/description of register |
| `data.controlBlueprint` | object | Yes | Register Control Blueprint |
| `data.initialValidators` | array | Yes | Initial set of validators (min 1) |
| `data.cryptoConfig` | object | Yes | Cryptographic algorithms configuration |

---

## Register Control Blueprint

The control blueprint is a special blueprint that governs the register itself. It defines:
- Consensus parameters (signature thresholds, timeouts)
- Validator management (registration, suspension, removal)
- Register metadata updates
- Access control policies

### Control Blueprint Schema

```json
{
  "$schema": "https://sorcha.io/schemas/control-blueprint/v1",
  "id": "control-blueprint-uuid",
  "title": "Register Control Blueprint",
  "version": "1.0.0",
  "type": "control",

  "configuration": {
    "consensus": {
      "signatureThreshold": {
        "min": 3,
        "max": 10
      },
      "docketTimeout": "PT30S",
      "maxSignaturesPerDocket": 10,
      "maxTransactionsPerDocket": 1000,
      "docketBuildInterval": "PT30S",
      "leaderElection": {
        "mechanism": "rotating",
        "heartbeatInterval": "PT5S",
        "leaderTimeout": "PT15S"
      }
    },

    "validators": {
      "registrationMode": "public",
      "minValidators": 3,
      "maxValidators": 100,
      "requireStake": false,
      "stakeAmount": null
    },

    "blueprints": {
      "allowPublicPublication": false,
      "requireApproval": true,
      "approvalThreshold": 2
    },

    "register": {
      "allowMetadataUpdates": true,
      "allowConfigUpdates": true,
      "configUpdateThreshold": 3
    }
  },

  "participants": [
    {
      "id": "register-owner",
      "role": "owner",
      "description": "Register owner with full control"
    },
    {
      "id": "validator",
      "role": "validator",
      "description": "Validator nodes that confirm transactions"
    },
    {
      "id": "blueprint-publisher",
      "role": "publisher",
      "description": "Authorized to publish blueprints"
    }
  ],

  "actions": [
    // Control actions defined below
  ]
}
```

### Configuration Properties

#### Consensus Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `signatureThreshold.min` | integer | 3 | Minimum signatures required to commit docket |
| `signatureThreshold.max` | integer | 10 | Maximum signatures to collect (prevents bloat) |
| `docketTimeout` | ISO 8601 duration | PT30S | Time to wait for signatures |
| `maxSignaturesPerDocket` | integer | 10 | Hard cap on signatures per docket |
| `maxTransactionsPerDocket` | integer | 1000 | Maximum transactions per docket |
| `docketBuildInterval` | ISO 8601 duration | PT30S | Interval between docket builds |
| `leaderElection.mechanism` | enum | "rotating" | Leader election mechanism (see below) |
| `leaderElection.heartbeatInterval` | ISO 8601 duration | PT5S | Leader heartbeat interval |
| `leaderElection.leaderTimeout` | ISO 8601 duration | PT15S | Time before leader considered failed |

#### Leader Election Mechanisms

| Mechanism | Description | Best For |
|-----------|-------------|----------|
| `rotating` | Round-robin based on validator registration order | Simple setups, trusted validators |
| `raft` | Raft-style heartbeat election with term numbers | Fault tolerance, dynamic networks |
| `stake-weighted` | Higher stake = higher election probability | Incentive-aligned networks (future) |

**Consensus Failure:** If the leader cannot collect enough signatures before `docketTimeout`, the docket is **abandoned** and transactions return to the unverified pool for inclusion in the next docket.

#### Validator Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `registrationMode` | enum | "consent" | "public" (volunteer) or "consent" (approval required) |
| `minValidators` | integer | 3 | Minimum validators required for consensus |
| `maxValidators` | integer | 100 | Maximum validators allowed |
| `requireStake` | boolean | false | Whether validators must stake tokens (future) |
| `stakeAmount` | number | null | Required stake amount if enabled |

#### Blueprint Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `allowPublicPublication` | boolean | false | Allow anyone to publish blueprints |
| `requireApproval` | boolean | true | Require approval for new blueprints |
| `approvalThreshold` | integer | 2 | Number of approvals needed |

#### Register Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `allowMetadataUpdates` | boolean | true | Allow register name/description changes |
| `allowConfigUpdates` | boolean | true | Allow configuration changes via control actions |
| `configUpdateThreshold` | integer | 3 | Signatures required for config changes |

---

## Control Actions

The control blueprint defines actions for managing the register. These are special actions that modify register state rather than workflow data.

### Action: Register Validator

**ID:** `control.validator.register`

Registers a new validator for the register.

```json
{
  "id": "control.validator.register",
  "title": "Register Validator",
  "sender": "validator",
  "dataSchemas": [
    {
      "$id": "validator-registration",
      "type": "object",
      "properties": {
        "validatorId": { "type": "string" },
        "publicKey": { "type": "string" },
        "endpoint": { "type": "string", "format": "uri" },
        "metadata": { "type": "object" }
      },
      "required": ["validatorId", "publicKey", "endpoint"]
    }
  ],
  "condition": {
    "or": [
      { "==": [{ "var": "config.validators.registrationMode" }, "public"] },
      { "in": [{ "var": "sender" }, { "var": "approvedValidators" }] }
    ]
  }
}
```

### Action: Approve Validator (Consent Mode)

**ID:** `control.validator.approve`

Approves a pending validator registration (consent mode only).

```json
{
  "id": "control.validator.approve",
  "title": "Approve Validator",
  "sender": "register-owner",
  "dataSchemas": [
    {
      "$id": "validator-approval",
      "type": "object",
      "properties": {
        "validatorId": { "type": "string" },
        "approvedBy": { "type": "string" },
        "approvalNotes": { "type": "string" }
      },
      "required": ["validatorId", "approvedBy"]
    }
  ]
}
```

### Action: Suspend Validator

**ID:** `control.validator.suspend`

Temporarily suspends a validator from participating in consensus.

```json
{
  "id": "control.validator.suspend",
  "title": "Suspend Validator",
  "sender": "register-owner",
  "dataSchemas": [
    {
      "$id": "validator-suspension",
      "type": "object",
      "properties": {
        "validatorId": { "type": "string" },
        "reason": { "type": "string" },
        "suspendedBy": { "type": "string" },
        "suspendedUntil": { "type": "string", "format": "date-time" }
      },
      "required": ["validatorId", "reason", "suspendedBy"]
    }
  ]
}
```

### Action: Remove Validator

**ID:** `control.validator.remove`

Permanently removes a validator from the register.

```json
{
  "id": "control.validator.remove",
  "title": "Remove Validator",
  "sender": "register-owner",
  "dataSchemas": [
    {
      "$id": "validator-removal",
      "type": "object",
      "properties": {
        "validatorId": { "type": "string" },
        "reason": { "type": "string" },
        "removedBy": { "type": "string" }
      },
      "required": ["validatorId", "reason", "removedBy"]
    }
  ]
}
```

### Action: Update Configuration

**ID:** `control.config.update`

Updates register configuration (requires threshold signatures).

```json
{
  "id": "control.config.update",
  "title": "Update Configuration",
  "sender": "register-owner",
  "dataSchemas": [
    {
      "$id": "config-update",
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "JSON path to config property" },
        "oldValue": { "description": "Current value (for verification)" },
        "newValue": { "description": "New value to set" },
        "reason": { "type": "string" }
      },
      "required": ["path", "newValue", "reason"]
    }
  ],
  "condition": {
    ">=": [
      { "var": "docket.signatureCount" },
      { "var": "config.register.configUpdateThreshold" }
    ]
  }
}
```

### Action: Publish Blueprint

**ID:** `control.blueprint.publish`

Publishes a new blueprint to the register.

```json
{
  "id": "control.blueprint.publish",
  "title": "Publish Blueprint",
  "sender": "blueprint-publisher",
  "dataSchemas": [
    {
      "$id": "blueprint-publication",
      "type": "object",
      "properties": {
        "blueprintId": { "type": "string" },
        "blueprint": { "$ref": "https://sorcha.io/schemas/blueprint/v1" },
        "previousVersionId": { "type": "string", "description": "If updating, txid of prior version" },
        "publishedBy": { "type": "string" }
      },
      "required": ["blueprintId", "blueprint", "publishedBy"]
    }
  ]
}
```

### Action: Update Register Metadata

**ID:** `control.register.updateMetadata`

Updates register name, description, or other metadata.

```json
{
  "id": "control.register.updateMetadata",
  "title": "Update Register Metadata",
  "sender": "register-owner",
  "dataSchemas": [
    {
      "$id": "metadata-update",
      "type": "object",
      "properties": {
        "field": { "type": "string", "enum": ["name", "description", "tags"] },
        "oldValue": { "type": "string" },
        "newValue": { "type": "string" },
        "reason": { "type": "string" }
      },
      "required": ["field", "newValue"]
    }
  ]
}
```

---

## Control Blueprint Versioning

The control blueprint is **not immutable** - it can be updated to evolve register governance over time.

### Version Chain

```
Genesis Block (txid0)
│
├── Control Blueprint v1.0.0 (embedded in genesis)
│   └── Initial configuration
│
├── Control Blueprint v1.1.0 (txid5)
│   │  previousId = txid0
│   └── Added new validator, updated timeout
│
└── Control Blueprint v2.0.0 (txid20)
       previousId = txid5
       Major governance change (e.g., registration mode)
```

### What Can Be Updated

| Category | Updatable Properties |
|----------|---------------------|
| **Consensus** | Signature thresholds, timeouts, docket limits, leader election |
| **Validators** | Registration mode, min/max validators, stake requirements |
| **Blueprints** | Publication rules, approval thresholds |
| **Register** | Metadata update rules, config update thresholds |
| **Participants** | Add/remove organizations, change roles |

### Update Process

1. Register owner creates `control.config.update` transaction
2. Transaction included in control docket
3. Docket requires `configUpdateThreshold` signatures (higher bar than normal)
4. On commit, new control blueprint version is active
5. All validators refresh their Genesis Config Cache

### Version Resolution

Validators always use the **latest committed** control blueprint version for:
- Signature threshold checks
- Timeout values
- Validator registration validation

---

## Control Docket

Control actions are grouped into **control dockets** - special dockets that modify register state. Control dockets follow the same consensus rules as regular dockets but are processed differently by the Register Service.

### Control Docket Structure

```json
{
  "id": "control-docket-uuid",
  "type": "control",
  "registerId": "register-uuid",
  "previousDocketId": "prev-docket-uuid",
  "sequenceNumber": 5,
  "transactions": [
    {
      "id": "tx-uuid",
      "type": "ControlAction",
      "blueprintId": "control-blueprint-uuid",
      "actionId": "control.validator.register",
      "previousId": "genesis-txid",
      "sender": "new-validator-address",
      "data": {
        "validatorId": "new-validator-address",
        "publicKey": "0x...",
        "endpoint": "https://new-validator.example.com"
      },
      "signature": "0x...",
      "timestamp": "2026-01-26T10:00:00Z"
    }
  ],
  "merkleRoot": "0x...",
  "signatures": [...],
  "consensusMetadata": {...}
}
```

### Control Docket Processing

1. **Validation:** Same validation rules as regular dockets
2. **State Update:** Control actions update register configuration state
3. **Propagation:** Changes propagate to all validators via Peer Service
4. **Cache Invalidation:** Genesis config cache invalidated on config updates

---

## Validator Registration Flow

### Public Registration Mode

```
New Validator                    Peer Network                 Register
     │                                │                          │
     │  1. Announce(registration)     │                          │
     │ ──────────────────────────────▶│                          │
     │                                │                          │
     │                                │  2. Broadcast to         │
     │                                │     active validators    │
     │                                │                          │
     │  3. Create registration tx     │                          │
     │ ──────────────────────────────▶│                          │
     │                                │                          │
     │                                │  4. Include in docket    │
     │                                │ ────────────────────────▶│
     │                                │                          │
     │  5. Registration confirmed     │                          │
     │ ◀──────────────────────────────│                          │
     │                                │                          │
     │  6. Begin confirming dockets   │                          │
     │ ◀─────────────────────────────▶│                          │
```

### Consent Registration Mode

```
New Validator        Register Owner        Peer Network         Register
     │                     │                    │                   │
     │  1. Request access  │                    │                   │
     │ ───────────────────▶│                    │                   │
     │                     │                    │                   │
     │                     │  2. Review request │                   │
     │                     │                    │                   │
     │                     │  3. Create approval tx                 │
     │                     │ ──────────────────▶│                   │
     │                     │                    │                   │
     │                     │                    │  4. Process in    │
     │                     │                    │     control docket│
     │                     │                    │ ─────────────────▶│
     │                     │                    │                   │
     │  5. Approval confirmed                   │                   │
     │ ◀───────────────────────────────────────│                   │
     │                     │                    │                   │
     │  6. Create registration tx               │                   │
     │ ────────────────────────────────────────▶│                   │
     │                     │                    │                   │
     │  7. Begin confirming dockets             │                   │
     │ ◀───────────────────────────────────────▶│                   │
```

---

## Validator State Model

```json
{
  "validatorId": "validator-address",
  "registerId": "register-uuid",
  "status": "active",
  "publicKey": "0x...",
  "endpoint": "https://validator.example.com",
  "registeredAt": "2026-01-26T10:00:00Z",
  "registrationTxId": "tx-uuid",
  "approvalTxId": "tx-uuid",
  "suspensions": [
    {
      "suspendedAt": "2026-02-01T00:00:00Z",
      "suspendedUntil": "2026-02-08T00:00:00Z",
      "reason": "Maintenance",
      "txId": "suspension-tx-uuid"
    }
  ],
  "statistics": {
    "docketsConfirmed": 1234,
    "docketsRejected": 2,
    "lastActiveAt": "2026-01-26T12:00:00Z",
    "uptime": 0.9987
  }
}
```

### Validator Status Values

| Status | Description |
|--------|-------------|
| `pending` | Registration submitted, awaiting approval (consent mode) |
| `active` | Fully registered and participating in consensus |
| `suspended` | Temporarily suspended, not participating |
| `removed` | Permanently removed from register |

---

## Blueprint Versioning via Transaction Chain

### Version Chain Structure

```
Genesis Block (txid0)
     │
     │  previousId = null
     │
Blueprint v1.0.0 (txid1)
     │  previousId = txid0  (links to genesis = new blueprint)
     │  data.blueprint.version = "1.0.0"
     │
Blueprint v1.1.0 (txid5)
     │  previousId = txid1  (links to v1.0.0 = version update)
     │  data.blueprint.version = "1.1.0"
     │
Blueprint v2.0.0 (txid12)
     │  previousId = txid5  (links to v1.1.0 = major update)
     │  data.blueprint.version = "2.0.0"
```

### Version Resolution

To find all versions of a blueprint:

```csharp
public async Task<List<BlueprintVersion>> GetBlueprintVersionsAsync(string blueprintId)
{
    var versions = new List<BlueprintVersion>();
    var current = await GetLatestBlueprintTxAsync(blueprintId);

    while (current != null && current.Type == TransactionType.BlueprintPublication)
    {
        versions.Add(new BlueprintVersion
        {
            TransactionId = current.Id,
            Version = current.Data.Blueprint.Version,
            PublishedAt = current.Timestamp
        });

        // Follow previousId chain
        current = await GetTransactionAsync(current.PreviousId);

        // Stop at genesis or different blueprint
        if (current?.Type == TransactionType.Genesis)
            break;
    }

    return versions;
}
```

### Instance Version Binding

When a workflow instance is created (first action executed), it binds to a specific blueprint version:

```json
{
  "id": "action-tx-uuid",
  "type": "Action",
  "blueprintId": "blueprint-uuid",
  "actionId": 0,
  "previousId": "blueprint-v1.0.0-txid",
  "data": { ... }
}
```

The `previousId` pointing to a specific blueprint publication transaction permanently binds that instance to that version. All subsequent actions in the instance chain inherit this version.

---

## Example: Complete Genesis Block

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "Genesis",
  "previousId": null,
  "timestamp": "2026-01-26T00:00:00Z",
  "creator": "0x1234567890abcdef1234567890abcdef12345678",
  "signature": "0xabc123...",
  "data": {
    "registerId": "660e8400-e29b-41d4-a716-446655440001",
    "registerName": "Acme Corp Workflow Register",
    "registerDescription": "Distributed ledger for Acme Corp business workflows",

    "controlBlueprint": {
      "$schema": "https://sorcha.io/schemas/control-blueprint/v1",
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "title": "Acme Register Control Blueprint",
      "version": "1.0.0",
      "type": "control",

      "configuration": {
        "consensus": {
          "signatureThreshold": { "min": 2, "max": 5 },
          "docketTimeout": "PT60S",
          "maxSignaturesPerDocket": 5,
          "maxTransactionsPerDocket": 500,
          "docketBuildInterval": "PT15S"
        },
        "validators": {
          "registrationMode": "consent",
          "minValidators": 2,
          "maxValidators": 10,
          "requireStake": false
        },
        "blueprints": {
          "allowPublicPublication": false,
          "requireApproval": true,
          "approvalThreshold": 1
        },
        "register": {
          "allowMetadataUpdates": true,
          "allowConfigUpdates": true,
          "configUpdateThreshold": 2
        }
      },

      "participants": [
        { "id": "register-owner", "role": "owner" },
        { "id": "validator", "role": "validator" },
        { "id": "blueprint-publisher", "role": "publisher" }
      ],

      "actions": [
        { "id": "control.validator.register", "title": "Register Validator", "sender": "validator" },
        { "id": "control.validator.approve", "title": "Approve Validator", "sender": "register-owner" },
        { "id": "control.validator.suspend", "title": "Suspend Validator", "sender": "register-owner" },
        { "id": "control.validator.remove", "title": "Remove Validator", "sender": "register-owner" },
        { "id": "control.config.update", "title": "Update Configuration", "sender": "register-owner" },
        { "id": "control.blueprint.publish", "title": "Publish Blueprint", "sender": "blueprint-publisher" },
        { "id": "control.register.updateMetadata", "title": "Update Metadata", "sender": "register-owner" }
      ]
    },

    "initialValidators": [
      {
        "validatorId": "0xaaa111222333444555666777888999aaabbbccc",
        "publicKey": "0x04...",
        "endpoint": "https://validator1.acme.com:7004",
        "registeredAt": "2026-01-26T00:00:00Z"
      },
      {
        "validatorId": "0xbbb111222333444555666777888999aaabbbccc",
        "publicKey": "0x04...",
        "endpoint": "https://validator2.acme.com:7004",
        "registeredAt": "2026-01-26T00:00:00Z"
      }
    ],

    "cryptoConfig": {
      "hashAlgorithm": "SHA256",
      "signatureAlgorithm": "ED25519",
      "merkleTreeAlgorithm": "SHA256"
    }
  }
}
```

---

## Implementation Considerations

### Genesis Block Creation

1. **Single Creator:** Only register owner can create genesis block
2. **Immutable:** Genesis block cannot be modified after creation
3. **Validation:** Full validation of control blueprint schema
4. **Initial Validators:** At least `minValidators` must be specified

### Control Blueprint Validation

1. **Schema Compliance:** Must conform to control blueprint schema
2. **Logical Consistency:** `signatureThreshold.min` <= `maxSignaturesPerDocket`
3. **Participant Roles:** Required roles must be defined
4. **Action Completeness:** Core control actions must be present

### Configuration Updates

1. **Threshold Enforcement:** Changes require `configUpdateThreshold` signatures
2. **Validation:** New values must be valid (e.g., min <= max)
3. **Propagation:** All validators must update cached config
4. **Audit Trail:** All changes recorded in transaction chain

---

## Related Documents

- [VALIDATOR-SERVICE-REQUIREMENTS.md](VALIDATOR-SERVICE-REQUIREMENTS.md) - Validator service architecture
- [MASTER-TASKS.md](MASTER-TASKS.md) - Implementation tasks
- [constitution.md](constitution.md) - Project standards

---

**Last Updated:** 2026-01-26
**Document Owner:** Sorcha Architecture Team
