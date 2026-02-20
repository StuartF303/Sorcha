# Research: Published Participant Records on Register

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## R1: TransactionSubmission.BlueprintId Handling

**Decision**: Make `BlueprintId` and `ActionId` optional on `TransactionSubmission` (nullable strings instead of required).

**Rationale**: The current `TransactionSubmission` record has `required string BlueprintId` and `required string ActionId`, designed for blueprint workflow actions. Participant transactions have no blueprint context. Making these nullable is the cleanest approach and avoids sentinel values like empty strings.

**Alternatives considered**:
- Use empty string `""` for BlueprintId — hacky, leaks implementation detail, confuses logging
- Create a separate `ParticipantSubmission` record — unnecessary duplication, validator already handles multiple types
- Add a `SubmissionType` discriminator — over-engineering for this case

**Impact**: `TransactionSubmission` record change. Validator code that reads `BlueprintId` must handle null. Blueprint conformance validation already skips when `IsGenesisOrControlTransaction()` — extend this check to include Participant type.

## R2: Fork Detection for Participant Transactions

**Decision**: Participant transactions follow two fork detection rules:
1. **First publication** chains from latest Control TX → fork detection is SKIPPED (Control TXs allow multiple children, established in prior fix)
2. **Version updates** chain from previous version of same participant → fork detection is ENFORCED (linear per-participant chain)

**Rationale**: This leverages the existing fork detection logic with no new rules needed. The validator already:
- Allows multiple children of Control TXs (`isControlTx` check)
- Blocks forks of non-Control TXs

A Participant TX (type=3) is not a Control TX (type=0), so when an update chains from a previous Participant TX, the fork detection will naturally block concurrent updates.

**For first publications**, PrevTxId points to a Control TX → `isControlTx = true` → fork detection skipped → multiple participants can fork from the same Control TX.

**Alternatives considered**:
- Add explicit `isParticipantTx` check — unnecessary, existing logic handles it
- All Participant TXs skip fork detection — too permissive, would allow version chain corruption

## R3: MongoDB Indexing Strategy

**Decision**: Add a compound index on `MetaData.TransactionType` + participant record fields for fast participant queries.

**Rationale**: Current indexes cover TxId (unique), SenderWallet, TimeStamp, DocketNumber, BlueprintId+InstanceId, and PrevTxId. For participant queries we need:
1. Filter by `MetaData.TransactionType == Participant` (narrow to participant TXs)
2. Search within payload for wallet addresses (participant address index)
3. Group by participant ID and find latest version

**Approach**: Two new indexes:
- `MetaData.TransactionType` — enables efficient filtering by transaction type
- Dedicated participant address collection/index (see R4 below)

**Alternatives considered**:
- Query all transactions and filter in memory — doesn't scale
- Separate MongoDB collection for participants — breaks transaction chain model
- Materialized view — MongoDB doesn't natively support; build application-level index instead

## R4: Participant Address Index

**Decision**: Build an application-level index in Register Service that maintains a mapping of `walletAddress → (participantId, registerId, latestTxId, version, status)`. Updated when Participant transactions are written.

**Rationale**: Participant records are stored as transaction payloads (JSON blobs). MongoDB can't efficiently index nested array elements within a JSON payload stored as a generic field. An application-level index (in-memory + Redis cache) provides O(1) lookups.

**Update flow**:
1. When a Participant TX is confirmed and stored, extract the ParticipantRecord payload
2. For each address in the record, upsert into the address index
3. Cache in Redis with TTL (1 hour, same as transaction cache)
4. On cache miss, rebuild from transaction history (scan Participant TXs on that register)

**Alternatives considered**:
- MongoDB text search on payload — slow for structured lookups
- Separate participants collection in per-register DB — adds complexity but could work for Phase 2
- Store addresses as top-level TransactionModel fields — breaks generic transaction model

## R5: Participant Record JSON Schema

**Decision**: Define a JSON Schema for participant record validation, loaded by the Validator Service during schema validation step.

**Schema structure**:
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://schema.sorcha.io/participant-record/v1",
  "type": "object",
  "required": ["participantId", "organizationName", "participantName", "status", "version", "addresses"],
  "properties": {
    "participantId": { "type": "string", "format": "uuid" },
    "organizationName": { "type": "string", "minLength": 1, "maxLength": 200 },
    "participantName": { "type": "string", "minLength": 1, "maxLength": 200 },
    "status": { "type": "string", "enum": ["active", "deprecated", "revoked"] },
    "version": { "type": "integer", "minimum": 1 },
    "addresses": {
      "type": "array",
      "minItems": 1,
      "maxItems": 10,
      "items": {
        "type": "object",
        "required": ["walletAddress", "publicKey", "algorithm"],
        "properties": {
          "walletAddress": { "type": "string", "minLength": 1, "maxLength": 256 },
          "publicKey": { "type": "string", "minLength": 1 },
          "algorithm": { "type": "string", "enum": ["ED25519", "P-256", "RSA-4096"] },
          "primary": { "type": "boolean", "default": false }
        }
      }
    },
    "metadata": { "type": "object" }
  },
  "additionalProperties": false
}
```

**Rationale**: Follows existing pattern where blueprint payloads are validated against JSON Schema. Max 10 addresses matches the existing LinkedWalletAddress limit in Tenant Service. Algorithm values match `Sorcha.Cryptography` supported algorithms.

## R6: Deterministic TxId Formula

**Decision**: `SHA256("participant-publish-{registerId}-{participantId}-v{version}")` for deterministic, idempotent TxIds.

**Rationale**: Follows the blueprint publish pattern (`SHA256("blueprint-publish-{registerId}-{blueprintId}")`). Including the version number makes each version's TxId unique while remaining deterministic. The participantId (UUID) is the stable identity anchor.

**Alternatives considered**:
- Use random TxId — loses idempotency
- Omit version from TxId — all versions would collide
- Use participantName — not stable across renames

## R7: Validator Schema Validation Path for Participant TXs

**Decision**: The Validator's `ValidateSchemaAsync` method currently skips genesis/control transactions. Extend it to validate Participant transactions against the built-in participant record schema (R5) instead of requiring a blueprint-defined schema.

**Rationale**: The current schema validation flow:
1. Check if genesis/control → skip
2. Get blueprint → get action → get action's schemas → validate

For Participant TXs, there's no blueprint to look up. Instead:
1. Check transaction type
2. If Participant → validate against built-in participant record schema
3. If Action → existing blueprint schema flow

**Implementation**: Add a `ValidateParticipantSchemaAsync` method that loads the built-in schema and validates the payload. Called from `ValidateSchemaAsync` when `TransactionType == Participant`.

## R8: Governance Validation Skip

**Decision**: The `ValidateGovernanceRightsAsync` check must be skipped for Participant transactions.

**Rationale**: The current governance validation checks if the signer is in the register's governance roster. This is appropriate for Control transactions (register admin operations) but Participant transactions are intentionally lighter — authorization is handled by the Tenant Service before submission. The validator should check `TransactionType != Participant` before governance validation.

**Implementation**: Add type check at start of governance validation, or in the main validation flow's governance gate.
