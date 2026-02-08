# Research: Register Creation Pipeline Fix

**Branch**: `026-fix-register-creation-pipeline` | **Date**: 2026-02-08

## R1: Transaction Model Mapping (Payload Loss Bug)

**Decision**: Fix the `DocketBuildTriggerService.WriteDocketAndTransactionsAsync()` mapping to include full payload data using the existing `DocketSerializer.ToRegisterModel()` pattern as reference.

**Rationale**: A correct mapping pattern already exists in `DocketSerializer.cs` (lines 53-85) but is not used by `DocketBuildTriggerService`. The current mapping loses 80% of transaction data (payloads, signatures, metadata, chain linkage). The fix is to implement complete field mapping inline, converting the Validator's `Transaction` model to Register's `TransactionModel` with all fields populated.

**Key Field Mapping**:

| Validator Transaction | Register TransactionModel | Transform |
|---|---|---|
| `TransactionId` | `TxId` | Direct |
| `RegisterId` | `RegisterId` | Direct |
| `Payload` (JsonElement) | `Payloads[0].Data` | `Payload.GetRawText()` → Base64 |
| `PayloadHash` | `Payloads[0].Hash` | Direct |
| N/A | `PayloadCount` | Set to 1 |
| `Signatures[0].PublicKey` | `SenderWallet` | Base64 encode |
| `Signatures[0].SignatureValue` | `Signature` | Base64 encode |
| `BlueprintId` | `MetaData.BlueprintId` | Direct |
| `ActionId` | `MetaData.ActionId` | `uint.TryParse()` |
| `PreviousTransactionId` | `PrevTxId` | Direct or empty |
| `CreatedAt` | `TimeStamp` | `.UtcDateTime` |
| `Metadata` keys | `MetaData.TrackingData` | Dict → SortedList |

**Alternatives Considered**:
- Extract to shared mapper utility class: Rejected as over-engineering for this fix; only two call sites exist.
- Call `DocketSerializer.ToRegisterModel()` directly: Not possible; it's an instance method that creates the full DocketModel, not just transactions.

## R2: Genesis Docket Retry Logic

**Decision**: Add a retry counter (`ConcurrentDictionary<string, int>`) in `DocketBuildTriggerService`. Only set `_genesisWritten` on successful write. After 3 failures, unregister from monitoring and log warning.

**Rationale**: The current bug sets `_genesisWritten[registerId] = true` even when the write throws an exception (the catch block swallows the exception but execution falls through to the flag-setting line). Moving the flag inside a success-only path and adding a counter prevents both permanent failure and infinite retries.

**Alternatives Considered**:
- Exponential backoff: Rejected; the 10-second timer interval already provides natural spacing.
- Unlimited retries: Rejected per clarification; caps at 3 attempts.

## R3: GenesisManager Error Propagation

**Decision**: Remove the catch-all in `NeedsGenesisDocketAsync` (lines 151-154) and let exceptions propagate to the caller (`DocketBuilder.BuildDocketAsync`), which already has its own catch block that returns null and logs the error.

**Rationale**: The current code catches all exceptions and returns `false`, which means "no genesis needed" — silently skipping genesis docket creation. The caller already handles exceptions gracefully, so propagation is safe.

**Alternatives Considered**:
- Return a tri-state (true/false/error): Rejected as unnecessary complexity; exception propagation is idiomatic.

## R4: Peer Service Advertisement Integration

**Decision**: Add `Advertise` field to `InitiateRegisterCreationRequest` and `PendingRegistration`. Thread it through the orchestrator. Add `AdvertiseRegisterAsync` method to `IPeerServiceClient`. Call fire-and-forget after register creation.

**Rationale**: The `RegisterAdvertisementService` lives in the Peer Service (not a shared library), so the Register Service must call it via HTTP/gRPC. Adding a method to `IPeerServiceClient` follows the existing service client pattern. A new Peer Service endpoint (`POST /api/registers/{id}/advertise`) is needed.

**Key Changes**:
- `InitiateRegisterCreationRequest`: Add `Advertise` property (bool, default false)
- `PendingRegistration`: Add `Advertise` property to carry through two-phase flow
- `RegisterCreationOrchestrator`: Use `pending.Advertise` instead of hardcoded `false`
- `IPeerServiceClient`: Add `AdvertiseRegisterAsync(registerId, isPublic)` method
- Peer Service: Add `POST /api/registers/{id}/advertise` endpoint
- Register Service PUT endpoint: Add fire-and-forget notification on advertise flag change

**Alternatives Considered**:
- Event-driven via domain events: Rejected per clarification; fire-and-forget is simpler and already consistent with existing patterns.
- Move `RegisterAdvertisementService` to shared library: Rejected; it depends on Peer Service internals (`PeerListManager`, `ConcurrentDictionary`).

## R5: Validator Monitoring Endpoint

**Decision**: Add `GET /api/admin/validators/monitoring` endpoint to `AdminEndpoints.cs`. Return register IDs from `IRegisterMonitoringRegistry.GetAll()` with count.

**Rationale**: The `GetAll()` method already exists and returns `IEnumerable<string>` from the Redis-backed set. The endpoint is a simple read-only query with no business logic.

**Alternatives Considered**:
- Add to MetricsEndpoints: Rejected; monitoring is an admin concern, not a metrics concern.

## R6: Register Service Test Fixes

**Decision**: Fix all 26 compilation errors across 4 files.

**Detailed Fix Plan**:

| File | Errors | Fix |
|---|---|---|
| `SignalRHubTests.cs` | 2 | Change `Task` → `ValueTask` for `InitializeAsync` and `DisposeAsync` (xUnit v3 `IAsyncLifetime`) |
| `RegisterCreationOrchestratorTests.cs` | ~13 | Add `Mock<TransactionManager>` and `Mock<IPendingRegistrationStore>` to constructor; Replace all `Creator = new CreatorInfo{...}` with `Owners = new List<OwnerInfo>{new(){...}}` |
| `MongoSystemRegisterRepositoryTests.cs` | ~9 | Fix namespace to `Sorcha.Register.Service.Core.SystemRegisterEntry`; update constructor to match new signature; add `publishedBy` param to `PublishBlueprintAsync` |
| `QueryApiTests.cs` | 2 | Replace `?.` null-propagating operator in expression tree lambdas with explicit null checks |

## R7: Genesis Blueprint Constant

**Decision**: Define `public const string GenesisBlueprintId = "genesis"` in a shared constants location (e.g., `Sorcha.Register.Models.Constants` or `Sorcha.Validator.Service.Models.TransactionConstants`). Replace the magic string in `ValidationEndpoints.cs:317`.

**Rationale**: The magic string `"genesis"` appears in `ValidationEndpoints.cs` and could be referenced by downstream code. A named constant prevents typos and documents the convention.

**Alternatives Considered**:
- Use an enum: Rejected; BlueprintId is a string field everywhere, and "genesis" is a special-case convention, not a true blueprint.
