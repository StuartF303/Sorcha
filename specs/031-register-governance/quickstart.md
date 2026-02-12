# Quickstart: Register Governance

**Branch**: `031-register-governance` | **Date**: 2026-02-11

## Overview

This feature adds decentralized governance to Sorcha registers via a genesis blueprint that manages admin rosters using multi-sig quorum voting.

## Key Changes

### 1. TransactionType Enum (Breaking Name Change)

```csharp
// BEFORE
public enum TransactionType { Genesis = 0, Action = 1, Docket = 2, System = 3 }

// AFTER
public enum TransactionType { Control = 0, Action = 1, Docket = 2 }
```

All `TransactionType.Genesis` → `TransactionType.Control`. All `TransactionType.System` → `TransactionType.Control`.

### 2. DID Identifiers

```
did:sorcha:w:{walletAddress}              → Local wallet-based identity
did:sorcha:r:{registerId}:t:{txId}        → Decentralized register-based identity
```

### 3. Governance Workflow

```
Register Created → Genesis Control TX (Owner asserted)
                        ↓
                   ┌─ Propose Change ←─────────────────┐
                   │   (any admin)                      │
                   ├─ Collect Quorum ──── blocked ──────┤
                   │   (>50% voting admins)             │
                   ├─ Accept Role ─────── declined ─────┤
                   │   (target signs)                   │
                   └─ Record Control TX ────────────────┘
                        ↓
                   Updated Roster (on register)
```

### 4. Roster Roles

| Role     | Voting | Authority                    |
|----------|--------|------------------------------|
| Owner    | Yes    | Ultimate — bypasses quorum   |
| Admin    | Yes    | Participates in quorum votes |
| Auditor  | No     | Read-only access             |
| Designer | No     | Blueprint modification       |

## Files Changed (Estimated)

### Modified

| File | Change |
|------|--------|
| `src/Common/Sorcha.Register.Models/Enums/TransactionType.cs` | Rename Genesis→Control, remove System |
| `src/Common/Sorcha.Register.Models/RegisterControlRecord.cs` | Add voting helpers, increase cap to 25 |
| `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs` | Use Control type for genesis |
| `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` | Add rights enforcement stage |
| `src/Services/Sorcha.Validator.Service/Services/GenesisManager.cs` | Handle Control type |
| `src/Services/Sorcha.Validator.Service/Services/BlueprintVersionResolver.cs` | Update Genesis→Control check |
| `src/Services/Sorcha.Validator.Service/Services/ValidatorRegistry.cs` | Update System→Control |
| `src/Common/Sorcha.Validator.Core/Validators/ChainValidatorCore.cs` | Update Genesis→Control |
| `src/Services/Sorcha.Register.Service/Services/SystemRegisterService.cs` | Seed governance blueprint |

### New

| File | Purpose |
|------|---------|
| `src/Common/Sorcha.Register.Models/SorchaDidIdentifier.cs` | DID value object (parse, validate, format) |
| `src/Common/Sorcha.Register.Models/GovernanceModels.cs` | GovernanceOperation, ControlPayload, enums |
| `src/Core/Sorcha.Register.Core/Services/GovernanceRosterService.cs` | Roster reconstruction from Control chain |
| `src/Core/Sorcha.Register.Core/Services/DIDResolver.cs` | DID resolution (wallet + register) |
| `src/Services/Sorcha.Validator.Service/Services/RightsEnforcementService.cs` | Governance rights validation |
| `src/Services/Sorcha.Register.Service/Endpoints/GovernanceEndpoints.cs` | Roster and history API endpoints |
| `examples/templates/register-governance-v1.json` | Governance blueprint JSON |

### Tests

| Project | Scope |
|---------|-------|
| `tests/Sorcha.Register.Models.Tests/` | DID parsing, roster validation, quorum calculation |
| `tests/Sorcha.Register.Core.Tests/` | Roster reconstruction, DID resolution |
| `tests/Sorcha.Validator.Service.Tests/` | Rights enforcement, Control TX validation |
| `tests/Sorcha.Register.Service.Tests/` | Governance endpoints, genesis update |

## Running Tests

```bash
# All governance-related tests
dotnet test --filter "FullyQualifiedName~Governance"

# Specific projects
dotnet test tests/Sorcha.Register.Models.Tests
dotnet test tests/Sorcha.Register.Core.Tests
dotnet test tests/Sorcha.Validator.Service.Tests
dotnet test tests/Sorcha.Register.Service.Tests
```

## Verification

1. Create a register → verify genesis TX has `TransactionType.Control` (value 0)
2. Read roster endpoint → verify single Owner entry
3. Submit governance proposal (Add Admin) → verify quorum flow
4. Submit from non-admin wallet → verify rejection
5. Transfer ownership → verify old owner becomes admin
