# Quickstart: Fix Transaction Submission Pipeline

## What This Changes

Action transactions currently bypass the Validator Service entirely. This fix routes them through validation, mempool staging, docket building, and consensus before they are persisted in the Register database.

### Before (Broken)
```
Blueprint Service → Register Service (immediate DB write, no validation)
```

### After (Fixed)
```
Blueprint Service → Validator Service (validate + mempool)
                  → Docket build (timer/size trigger)
                  → Consensus (single-validator auto-approve)
                  → Register Service (persist with docket number)
```

## Files to Modify

### 1. Service Client Layer (Sorcha.ServiceClients)

| File | Change |
|------|--------|
| `Validator/IValidatorServiceClient.cs` | Add `SubmitTransactionAsync` method + request/response models |
| `Validator/ValidatorServiceClient.cs` | Implement HTTP POST to `/api/v1/transactions/validate` |

### 2. Blueprint Service

| File | Change |
|------|--------|
| `Services/Implementation/ActionExecutionService.cs` | Replace `_registerClient.SubmitTransactionAsync()` with `_validatorClient.SubmitTransactionAsync()`. Add confirmation polling loop. |
| `Services/Interfaces/ITransactionBuilderService.cs` | Add `ToValidateTransactionRequest()` method on `BuiltTransaction` |
| DI registration | Inject `IValidatorServiceClient` into `ActionExecutionService` |

### 3. Validator Service

| File | Change |
|------|--------|
| `Endpoints/ValidationEndpoints.cs` | Add `RegisterForMonitoring` call after successful mempool addition in `ValidateTransaction` endpoint |

### 4. Register Service

| File | Change |
|------|--------|
| `Program.cs` | Remove or restrict the direct transaction submission endpoint. Ensure docket write-back publishes "transaction:confirmed" events. |

### 5. Tests

| File | Change |
|------|--------|
| `Sorcha.ServiceClients.Tests/` | Tests for `SubmitTransactionAsync` client method |
| `Sorcha.Blueprint.Service.Tests/` | Update `ActionExecutionService` tests for new Validator submission path |
| `Sorcha.Validator.Service.Tests/` | Test that `ValidateTransaction` endpoint registers monitoring |
| `Sorcha.Register.Core.Tests/` | Update `TransactionManager` tests if `StoreTransactionAsync` is modified |

## Verification

Run the Ping-Pong walkthrough after changes:
```powershell
pwsh walkthroughs/PingPong/test-ping-pong-workflow.ps1
```

Expected: All 10 action transactions appear in Register database with docket numbers assigned.

## Key Decision: Sequential Execution

After submitting to the Validator, the Blueprint Service polls the Register Service until the transaction appears with a DocketNumber. This means:
- Each action waits for docket sealing before returning success
- The wait is bounded by the docket build time threshold (default: 10 seconds)
- State reconstruction always operates on confirmed data
- No pending transaction tracking needed in the Blueprint Service
