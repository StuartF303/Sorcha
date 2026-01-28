# Implementation Plan: CLI Register Commands Update

**Branch**: `016-cli-register-update` | **Date**: 2026-01-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/016-cli-register-update/spec.md`

## Summary

Update the Sorcha CLI to eliminate duplicate model definitions by referencing shared common libraries (`Sorcha.Register.Models`, `Sorcha.Blueprint.Models`), replace the simplified register creation with the backend's two-phase cryptographic attestation flow (initiate → sign → finalize), and add missing command groups for dockets, cross-register queries, OData queries, register updates, and register statistics.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: System.CommandLine 2.0.2, Refit 9.0.2, Spectre.Console 0.54.0, Polly (resilience)
**Storage**: N/A (CLI is stateless; token cache uses DPAPI)
**Testing**: xUnit + FluentAssertions + Moq (existing test project: `tests/Sorcha.Cli.Tests/`)
**Target Platform**: Windows/Linux/macOS (.NET global tool)
**Project Type**: Single console application
**Performance Goals**: N/A (interactive CLI)
**Constraints**: Must maintain existing CLI command patterns; must not break non-register commands
**Scale/Scope**: ~10 new/modified command classes, 1 Refit interface update, 1 model file deletion, 2 project reference additions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | CLI is a client; no service coupling added. Shared models are domain contracts, not service dependencies. |
| II. Security First | PASS | Two-phase attestation signing via wallet service. No secrets in CLI. Pre-hashed signing prevents data exposure. |
| III. API Documentation | N/A | CLI tool, not an API. Backend endpoints already documented. |
| IV. Testing Requirements | PASS | Unit tests required for new commands. Target >85% for new code. |
| V. Code Quality | PASS | C# 13, async/await, DI, nullable enabled. Follows existing CLI patterns. |
| VI. Blueprint Creation Standards | N/A | No blueprint creation in this feature. |
| VII. Domain-Driven Design | PASS | Uses correct terminology: Register, Docket, Transaction, Attestation. |
| VIII. Observability | PASS | CLI has `--verbose` flag for debug output. Error messages are structured. |

**Post-Phase 1 Re-check**: All gates still pass. No new projects introduced. Shared model references follow the same pattern as Sorcha.UI.Core.

## Project Structure

### Documentation (this feature)

```text
specs/016-cli-register-update/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
├── contracts/
│   └── cli-commands.md  # Phase 1 command contracts
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.Cli/
├── Sorcha.Cli.csproj                    # MODIFY: Add project references
├── Program.cs                           # MODIFY: Register new command groups
├── Commands/
│   ├── RegisterCommands.cs              # MODIFY: Rewrite create, add update/stats
│   ├── TransactionCommands.cs           # MODIFY: Update pagination params
│   ├── DocketCommands.cs                # NEW: list, get, transactions
│   └── QueryCommands.cs                 # NEW: wallet, sender, blueprint, stats, odata
├── Models/
│   ├── Register.cs                      # DELETE: Replaced by Sorcha.Register.Models
│   └── Wallet.cs                        # MODIFY: Add IsPreHashed, DerivationPath to SignTransactionRequest
├── Services/
│   └── IRegisterServiceClient.cs        # MODIFY: Add new Refit endpoints

src/Common/
├── Sorcha.Register.Models/              # EXISTING: Referenced by CLI (new dependency)
└── Sorcha.Blueprint.Models/             # EXISTING: Referenced by CLI (new dependency)
```

**Structure Decision**: Existing single-project CLI structure is maintained. No new projects needed. Two project references added to shared Common libraries, matching the pattern used by Sorcha.UI.Core.

## Complexity Tracking

No constitution violations to justify. The changes are additive command additions and a model migration within an existing project.

## Implementation Phases

### Phase A: Shared Model Migration (P1 - Foundation)

**Goal**: Replace duplicate CLI models with shared library references. All existing commands must continue working.

**Steps**:
1. Add `ProjectReference` to `Sorcha.Register.Models` and `Sorcha.Blueprint.Models` in `Sorcha.Cli.csproj`
2. Update `SignTransactionRequest` in `Models/Wallet.cs` to add `IsPreHashed` and `DerivationPath` fields
3. Update `IRegisterServiceClient.cs`: Change return types from `Sorcha.Cli.Models.Register` to `Sorcha.Register.Models.Register`, and `Transaction` to `TransactionModel`
4. Update `RegisterCommands.cs`: Fix display logic for new Register model fields (Height, Status, TenantId, Advertise, etc.)
5. Update `TransactionCommands.cs`: Fix display logic for `TransactionModel` (Payloads array instead of flat Payload string, TxId instead of Id, etc.)
6. Delete `Models/Register.cs` (Register, CreateRegisterRequest, Transaction, SubmitTransactionRequest, SubmitTransactionResponse)
7. Update pagination in `TxListCommand`: Replace `--skip`/`--take` with `--page`/`--page-size`
8. Build and verify: `dotnet build src/Apps/Sorcha.Cli`

**Validation**: All existing `register list`, `register get`, `register delete`, `tx list`, `tx get`, `tx submit`, `tx status` commands compile and produce correct output.

### Phase B: Two-Phase Register Creation (P1 - Core)

**Goal**: Replace simplified `register create` with the two-phase initiate/finalize flow.

**Steps**:
1. Add Refit endpoints to `IRegisterServiceClient.cs`:
   - `POST /api/registers/initiate` → `InitiateRegisterCreationAsync`
   - `POST /api/registers/finalize` → `FinalizeRegisterCreationAsync`
2. Rewrite `RegisterCreateCommand`:
   - Replace `--org-id` with `--tenant-id` and add `--owner-wallet`
   - Add optional `--description` and `--metadata` options
   - Implement flow: get userId from token claims → build `InitiateRegisterCreationRequest` → call initiate → sign attestation hash via wallet service (`IsPreHashed=true`) → build `FinalizeRegisterCreationRequest` → call finalize → display results
3. Add error handling for: wallet unreachable, attestation expired, invalid signature, finalization failure
4. Support `--output json` for creation response

**Validation**: `sorcha register create --name "Test" --tenant-id <id> --owner-wallet <addr>` successfully creates a register with genesis transaction and docket.

### Phase C: Register Update & Stats (P3)

**Goal**: Add register metadata update and statistics commands.

**Steps**:
1. Add Refit endpoints to `IRegisterServiceClient.cs`:
   - `PUT /api/registers/{id}` → `UpdateRegisterAsync`
   - `GET /api/registers/stats/count` → `GetRegisterStatsAsync`
2. Create `RegisterUpdateCommand` in `RegisterCommands.cs` with `--id`, `--name`, `--status`, `--advertise` options
3. Create `RegisterStatsCommand` in `RegisterCommands.cs`
4. Register both in `RegisterCommand` constructor
5. Support `--output json` for both commands

**Validation**: Update changes register metadata; stats returns count.

### Phase D: Docket Commands (P2)

**Goal**: Add docket inspection command group.

**Steps**:
1. Add Refit endpoints to `IRegisterServiceClient.cs`:
   - `GET /api/registers/{regId}/dockets` → `ListDocketsAsync`
   - `GET /api/registers/{regId}/dockets/{docketId}` → `GetDocketAsync`
   - `GET /api/registers/{regId}/dockets/{docketId}/transactions` → `GetDocketTransactionsAsync`
2. Create `Commands/DocketCommands.cs` with:
   - `DocketCommand` (parent)
   - `DocketListCommand` (--register-id)
   - `DocketGetCommand` (--register-id, --docket-id)
   - `DocketTransactionsCommand` (--register-id, --docket-id)
3. Register in `Program.cs`: `rootCommand.Subcommands.Add(new DocketCommand(...))`
4. Support `--output json` for all docket commands

**Validation**: All three docket subcommands return correct data for a register with committed dockets.

### Phase E: Query Commands (P2)

**Goal**: Add cross-register query command group including OData.

**Steps**:
1. Add Refit endpoints to `IRegisterServiceClient.cs`:
   - `GET /api/query/wallets/{address}/transactions` → `QueryByWalletAsync`
   - `GET /api/query/senders/{address}/transactions` → `QueryBySenderAsync`
   - `GET /api/query/blueprints/{id}/transactions` → `QueryByBlueprintAsync`
   - `GET /api/query/stats` → `GetQueryStatsAsync`
   - `GET /odata/{resource}` → `QueryODataAsync` (returns raw `HttpResponseMessage` for flexible parsing)
2. Create `Commands/QueryCommands.cs` with:
   - `QueryCommand` (parent)
   - `QueryWalletCommand` (--address, --page, --page-size)
   - `QuerySenderCommand` (--address, --page, --page-size)
   - `QueryBlueprintCommand` (--id, --page, --page-size)
   - `QueryStatsCommand`
   - `QueryODataCommand` (--resource, --filter, --orderby, --top, --skip, --select, --count)
3. Register in `Program.cs`: `rootCommand.Subcommands.Add(new QueryCommand(...))`
4. Support `--output json` for all query commands
5. OData command: pass raw query parameters, display JSON response (OData responses are already JSON)

**Validation**: Query commands return correct results; OData command passes through filter/orderby correctly.

### Phase F: Testing

**Goal**: Unit tests for all new and modified commands.

**Steps**:
1. Add tests for shared model migration (verify Register, TransactionModel, Docket types resolve correctly)
2. Add tests for two-phase creation flow (mock wallet service signing, mock register service initiate/finalize)
3. Add tests for docket commands (mock list/get/transactions responses)
4. Add tests for query commands (mock query responses with pagination)
5. Add tests for OData command (verify query string construction)
6. Add tests for register update and stats
7. Add tests for error handling (expired attestation, wallet unreachable, 404, 401, 403)
8. Verify >85% coverage for new code

**Validation**: `dotnet test tests/Sorcha.Cli.Tests` passes with >85% coverage on new code.

## Dependencies Between Phases

```
Phase A (Shared Models) ─────► Phase B (Two-Phase Create) ─► Phase F (Tests)
                         ├───► Phase C (Update/Stats) ──────►
                         ├───► Phase D (Dockets) ───────────►
                         └───► Phase E (Queries) ───────────►
```

Phase A is the prerequisite for all others (shared models must be in place first). Phases B-E can be developed in parallel after A. Phase F runs last to test everything.

## Key Files

| File | Action | Phase |
|------|--------|-------|
| `src/Apps/Sorcha.Cli/Sorcha.Cli.csproj` | Add project references | A |
| `src/Apps/Sorcha.Cli/Models/Register.cs` | DELETE | A |
| `src/Apps/Sorcha.Cli/Models/Wallet.cs` | Add IsPreHashed, DerivationPath | A |
| `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs` | Add all new endpoints | A-E |
| `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs` | Rewrite create, add update/stats | A, B, C |
| `src/Apps/Sorcha.Cli/Commands/TransactionCommands.cs` | Update model types, pagination | A |
| `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs` | NEW | D |
| `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs` | NEW | E |
| `src/Apps/Sorcha.Cli/Program.cs` | Register docket + query commands | D, E |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Shared model JSON serialization differs from API response | Commands return wrong data | Compare JsonPropertyName attributes in shared models vs API responses; add integration test |
| Wallet sign endpoint response missing PublicKey field for CLI model | Finalization fails | Verify `SignTransactionResponse` includes `PublicKey`; CLI model needs update |
| API Gateway lacks query/OData routes | CLI can't reach query endpoints | CLI connects directly to register service (not gateway); confirmed in research |
| Two-phase creation timeout (5 min) | User confusion | Clear progress messages + retry guidance in error output |
