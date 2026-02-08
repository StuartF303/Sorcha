# Quickstart: Consolidate Transaction Versioning

**Feature**: 023-consolidate-tx-versioning
**Date**: 2026-02-07

## Implementation Order

This feature is a coordinated find-and-replace plus dead code removal. Follow this order to avoid intermediate build errors:

### Step 1: Update the Enum (foundation)

Modify `TransactionVersion.cs` to contain only `V1 = 1`. This will cause compile errors in any code referencing V2/V3/V4, which guides the remaining changes.

### Step 2: Fix Compile Errors in Source Code

Work through compile errors in this order:
1. **TransactionFactory** — remove CreateV2/V3/V4 methods, simplify switch to V1-only
2. **VersionDetector** — update switch statements to accept only version 1
3. **Transaction** — change default parameter from V4 to V1
4. **TransactionBuilder** — change default parameter from V4 to V1
5. **ITransactionBuilder** — change default parameter from V4 to V1
6. **TransactionBuilderService** — change 3 call sites from V4 to V1

### Step 3: Verify Build

Run `dotnet build` on the solution. All compile errors from Step 1 should now be resolved.

### Step 4: Update Tests

Fix test compile errors and update assertions:
1. **BackwardCompatibility tests** — rewrite V2/V3/V4 theories to test rejection
2. **Unit tests** — update V4→V1 references and remove multi-version theory data
3. **Integration tests** — update V4→V1 references
4. **Benchmarks** — update V4→V1 references
5. **Blueprint service tests** — update V4→V1 references

### Step 5: Verify Tests

Run `dotnet test` on the full solution. All tests should pass.

### Step 6: Codebase Sweep

Search the entire codebase for any remaining references to `V2`, `V3`, or `V4` in the context of TransactionVersion. Remove any straggling comments or documentation references.

## Key Validation

After implementation, verify:
- `dotnet build` succeeds with no warnings
- `dotnet test` passes all tests
- `grep -r "TransactionVersion.V[234]"` returns zero results across all source and test files
- A round-trip test (create → serialize → deserialize) preserves all fields with version=1
