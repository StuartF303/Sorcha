# Research: Fix Transaction Submission Pipeline

**Date**: 2026-02-09
**Feature**: 028-fix-transaction-pipeline

## R1: Data Model Mismatch Between Blueprint and Validator

**Decision**: Create a new mapping layer in the Blueprint Service to convert `BuiltTransaction` directly to the Validator's `ValidateTransactionRequest` format, bypassing `TransactionModel` entirely for the submission path.

**Rationale**: The Blueprint's `BuiltTransaction` contains raw `TransactionData` (JSON bytes), `TxId` (SHA-256 hex hash), `Signature` (byte[]), and metadata. The Validator's `ValidateTransactionRequest` expects `TransactionId`, `RegisterId`, `BlueprintId`, `ActionId`, `Payload` (JsonElement), `PayloadHash`, `Signatures` (list), and `CreatedAt`. The Register's `TransactionModel` is a persistence format with JSON-LD, encryption, and blockchain chain structure — it's the wrong intermediate format for validation. The `BuiltTransaction` has all the data the Validator needs, just in a different shape.

**Alternatives considered**:
- Route through `TransactionModel` then convert to Validator format: Double conversion, lossy (PayloadCount=0, no PayloadHash in TransactionModel)
- Have the Validator accept `TransactionModel` directly: Would require the Validator to understand Register-specific formats and JSON-LD, violating microservices isolation

## R2: Register Monitoring for Action Transactions

**Decision**: When the Validator receives an action transaction, it should auto-register the register for monitoring via `IRegisterMonitoringRegistry.RegisterForMonitoring()` if not already registered. This mirrors the genesis path where `RegisterForMonitoring` is called after genesis transaction submission.

**Rationale**: The `DocketBuildTriggerService` already iterates all monitored registers and builds dockets when thresholds are met. The only gap is that action transactions never trigger `RegisterForMonitoring`. The genesis flow calls it explicitly in the `SubmitGenesisTransaction` endpoint. The action submission endpoint should do the same.

**Alternatives considered**:
- Have the Blueprint Service call a separate "monitor" endpoint: Extra network call, Blueprint shouldn't know about Validator internals
- Scan all mempool keys for registers with pending transactions: Expensive Redis scan on every tick, existing monitoring registry is more efficient

## R3: Transaction Confirmation Notification

**Decision**: The "transaction:confirmed" event should be published by the Register Service when it receives a docket write-back, not on direct transaction submission. The Register Service's docket write endpoint already persists transactions — it should publish the event there.

**Rationale**: The current `TransactionManager.StoreTransactionAsync()` publishes "transaction:confirmed" immediately. This method should be either removed or repurposed as an internal-only method for the docket write-back path. The docket write endpoint in `Program.cs` (line 997) already inserts transactions; adding event publication there ensures events only fire for sealed transactions.

**Alternatives considered**:
- Have the Validator Service publish the event: Wrong responsibility boundary — Register Service owns transaction persistence events
- Add a separate "confirm" step after docket write: Over-engineered for the current need

## R4: Sequential Action Execution (Wait for Docket Seal)

**Decision**: After submitting a transaction to the Validator, the Blueprint Service should poll for confirmation before returning success to the caller. The action execution endpoint becomes synchronous from the caller's perspective — it waits until the transaction is sealed in a docket.

**Rationale**: The user chose Option C (wait for docket seal before allowing next action). This eliminates the state reconstruction problem entirely. The docket build time threshold is 10 seconds, so the wait is bounded. The Blueprint Service can poll the Register Service for the transaction (by TxId) — when it appears with a DocketNumber, the transaction is confirmed.

**Alternatives considered**:
- Return immediately with "pending" status and let caller poll: More complex, requires pending status tracking, was Option A/B
- Use SignalR notification from Register Service: More reactive but adds complexity for a bounded-wait scenario

## R5: Existing Validator Endpoint Compatibility

**Decision**: Use the existing `POST /api/v1/transactions/validate` endpoint on the Validator Service. It already validates structure, payload hash, and signatures, then adds to the mempool. No new endpoint is needed — only a new client method (`SubmitTransactionAsync`) on `IValidatorServiceClient` to call it.

**Rationale**: The endpoint exists, handles validation correctly, and returns structured responses. The `ValidateTransactionRequest` model matches what the Blueprint Service can produce from `BuiltTransaction`. The only missing piece is the client method.

**Alternatives considered**:
- Create a new endpoint specifically for action transactions: Duplication, the existing endpoint already handles the use case
- Use gRPC instead of HTTP: The Validator already has gRPC for peer-to-peer, but HTTP is consistent with how the Blueprint Service communicates with other services

## R6: Payload Data in Validator Transaction

**Decision**: The `BuiltTransaction.TransactionData` (JSON bytes) should be deserialized to a `JsonElement` and submitted as the Validator's `Payload` field. The `TxId` (SHA-256 of TransactionData) serves as the `PayloadHash`.

**Rationale**: The Validator's `ValidateTransactionRequest` requires `Payload` (JsonElement) and `PayloadHash` (string). The Blueprint Service already has the raw JSON in `TransactionData` and its SHA-256 hash as `TxId`. The Validator validates that `SHA256(Payload) == PayloadHash`, which will pass since that's exactly how TxId was computed.

**Alternatives considered**:
- Compute a separate PayloadHash: Redundant since TxId is already SHA-256 of the payload
- Send TransactionData as base64 in a string field: Less clean, Validator expects JsonElement

## R7: Peer Gossip for Action Transactions (P3)

**Decision**: Defer full peer gossip implementation to a follow-up feature. For the P1 fix, the Validator receives action transactions locally and processes them in single-validator mode. The existing `DocketDistributor.BroadcastConfirmedDocketAsync()` already handles confirmed docket propagation.

**Rationale**: The current development environment uses single-validator auto-approve. Peer gossip for pending transactions requires adding `GossipTransactionAsync` to `IPeerServiceClient` and implementing reception in the Peer Service. This is significant scope that doesn't block the core pipeline fix.

**Alternatives considered**:
- Implement gossip as part of this feature: Too much scope, Peer Service is only 70% complete
- Gossip only the transaction hash, not the full data: Still requires Peer Service changes
