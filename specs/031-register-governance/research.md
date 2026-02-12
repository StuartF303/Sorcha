# Research: Register Governance

**Branch**: `031-register-governance` | **Date**: 2026-02-11

## Decision 1: TransactionType Enum Refactoring

**Decision**: Rename `Genesis = 0` to `Control = 0`, remove `System = 3` entirely.

**Rationale**: Existing persisted genesis transactions (integer value 0) become Control transactions with zero data migration. System transactions (value 3) are rare and can be migrated to Control (0). The enum simplifies to 3 values: Control=0, Action=1, Docket=2.

**Alternatives Considered**:
- Keep Genesis=0 and rename System=3 to Control=3 — rejected because it requires data migration for existing genesis TXs
- Add Control=4 as new value — rejected because it fragments governance across two types

**Impact**: 6 source files + 2 test files reference `TransactionType.Genesis` or `TransactionType.System`:
- `RegisterCreationOrchestrator.cs` (line 559) — Genesis
- `BlueprintVersionResolver.cs` (line 335) — Genesis
- `ValidatorRegistry.cs` (lines 302, 480) — System
- `ChainValidatorCore.cs` (lines 29, 140, 279, 304) — Genesis
- `BlueprintVersionResolverTests.cs` (line 509) — System
- `ChainValidatorCoreTests.cs` (lines 32, 126, 253, 313, 529, 585) — Genesis

## Decision 2: DID Format and Resolution Strategy

**Decision**: Two DID formats — `did:sorcha:w:{walletAddress}` (local) and `did:sorcha:r:{registerId}:t:{txId}` (decentralized).

**Rationale**: Wallet DIDs leverage existing `IWalletServiceClient.GetWalletAsync()` for resolution. Register DIDs leverage existing `IRegisterServiceClient.GetTransactionAsync()` and `RegisterReplicationService` + `RegisterCache` for peer resolution. Both resolution paths already have infrastructure.

**Alternatives Considered**:
- W3C DID standard (`did:web:`, `did:key:`) — rejected because Sorcha needs its own resolution method tied to registers
- Single DID format only — rejected because wallet DIDs can't be resolved by peers without Wallet Service access

**Existing DID Reference**: `TransactionModel.GenerateDidUri()` already produces `did:sorcha:register:{registerId}/tx/{txId}` — the new `did:sorcha:r:{registerId}:t:{txId}` is a variation with abbreviated prefix.

## Decision 3: Governance Blueprint Structure

**Decision**: System-seeded `register-governance-v1` blueprint with 5 actions in a looping workflow, published to the System Register via `SystemRegisterService`.

**Rationale**: Follows the existing `register-creation-v1` pattern already seeded on startup. Uses route-based routing (supported, cycles produce warnings not errors). Instance state tracks current action IDs.

**Alternatives Considered**:
- Hardcoded governance logic in Validator — rejected because it bypasses the blueprint execution pipeline and isn't verifiable by peers
- Per-register custom governance blueprint — rejected because governance must be standardised for peer verification

**Key Patterns**:
- Routes with `NextActionIds` enable looping (e.g., ping-pong template)
- `RejectionConfig.TargetActionId` enables rejection-back-to-proposal flow
- `Instance.CurrentActionIds` tracks where the governance workflow currently is
- `AccumulatedState` from `StateReconstructionService` provides prior action data for routing decisions

## Decision 4: Admin Roster Model Evolution

**Decision**: Evolve `RegisterControlRecord` to support governance roster with 4 roles (Owner, Admin, Auditor, Designer) where only Owner+Admin are voting roles. Max 25 members.

**Rationale**: Existing `RegisterControlRecord` already has `List<RegisterAttestation>` with `RegisterRole` enum containing all 4 roles. The model needs minimal evolution — primarily increasing the cap from 10 to 25 and adding governance operation metadata.

**Alternatives Considered**:
- New separate `GovernanceRoster` model — rejected because it duplicates the existing attestation structure
- Flat DID list without roles — rejected because role-based access control is already established

## Decision 5: Roster Reconstruction Approach

**Decision**: Filter Control transactions by `TransactionType.Control` from the register, replay in docket order, use latest Control TX payload as current roster.

**Rationale**: Each Control TX contains the full roster (FR-025), so the "current" roster is simply the payload of the most recent Control transaction. No delta replay needed. Deterministic for all peers.

**Alternatives Considered**:
- Delta-based replay (each TX has add/remove operations) — rejected because it requires replaying all operations and is error-prone
- Cached roster in register metadata — rejected because it's not verifiable from the transaction chain alone

## Decision 6: Validator Rights Enforcement Integration

**Decision**: Add a new validation stage in `ValidationEngine` between structure validation and schema validation. For Control transactions, verify submitter DID is in current roster with appropriate role.

**Rationale**: The 6-stage validation pipeline already exists. Inserting rights check as stage 2.5 (after structure, before schema) ensures governance is enforced before expensive validation. Uses existing `IRegisterServiceClient` to fetch Control chain.

**Alternatives Considered**:
- Separate governance validator service — rejected because it adds service coupling and latency
- Post-validation check in docket sealing — rejected because invalid transactions would enter the mempool

## Decision 7: Proposal Timeout Implementation

**Decision**: 7-day timeout using blueprint `Branch.Deadline` mechanism on the quorum collection action.

**Rationale**: The existing `Branch` model has a `Deadline` property (DateTimeOffset?) and `BranchState.TimedOut`. The routing engine already handles deadline-based timeout. This requires no new infrastructure.

**Alternatives Considered**:
- Redis-based TTL on proposal state — rejected because it's not verifiable by peers
- No timeout with Owner override only — rejected because it doesn't handle inactive Owner scenarios
