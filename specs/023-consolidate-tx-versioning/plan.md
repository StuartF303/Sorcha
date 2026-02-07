# Implementation Plan: Consolidate Transaction Versioning

**Branch**: `023-consolidate-tx-versioning` | **Date**: 2026-02-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/023-consolidate-tx-versioning/spec.md`

## Summary

Consolidate the transaction versioning system from four versions (V1-V4) into a single V1 that carries all current V4 capabilities. This is a pure renumbering — the wire format, field set, and serialization logic remain structurally identical. The version marker in serialized data changes from `4` to `1`. All version-specific adapters, factory methods, and dead code paths for V2/V3/V4 are removed. No production data exists, so no migration is needed.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Sorcha.TransactionHandler, Sorcha.Cryptography, Sorcha.Register.Models
**Storage**: N/A (no storage schema changes — wire format only)
**Testing**: xUnit 3.2.2, FluentAssertions 8.8.0, Moq 4.20.72
**Target Platform**: .NET 10 (cross-platform)
**Project Type**: Multi-project solution (microservices)
**Performance Goals**: N/A (no behavioral change — version byte swap only)
**Constraints**: Binary wire format must remain byte-identical except for the 4-byte version field
**Scale/Scope**: 6 source files + 11 test files across 4 projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new services or cross-service coupling. Changes are within existing project boundaries. |
| II. Security First | PASS | No security model changes. Cryptographic signing/verification logic is untouched. |
| III. API Documentation | N/A | No API surface changes. |
| IV. Testing Requirements | PASS | All existing tests will be updated; no coverage reduction. Target >85% maintained. |
| V. Code Quality | PASS | Removing dead code improves quality. Nullable reference types unaffected. |
| VI. Blueprint Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Maintains ubiquitous language (Transaction, Version). |
| VIII. Observability | N/A | No telemetry changes. |

**Gate result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/023-consolidate-tx-versioning/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: entity changes (minimal — enum only)
├── quickstart.md        # Phase 1: implementation guide
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (affected files)

```text
src/Common/Sorcha.TransactionHandler/
├── Enums/
│   └── TransactionVersion.cs          # Remove V2/V3/V4 from enum
├── Core/
│   ├── Transaction.cs                 # Default V4→V1
│   └── TransactionBuilder.cs          # Default V4→V1
├── Interfaces/
│   ├── ITransactionBuilder.cs         # Default V4→V1
│   └── ITransactionFactory.cs         # No change (no default param)
├── Versioning/
│   ├── TransactionFactory.cs          # Remove CreateV2/V3/V4, simplify switch
│   └── VersionDetector.cs             # Accept only version 1
└── Serialization/
    ├── BinaryTransactionSerializer.cs  # No structural change
    └── JsonTransactionSerializer.cs    # No structural change

src/Common/Sorcha.Register.Models/
└── TransactionModel.cs                # Verify default=1 (no change needed)

src/Services/Sorcha.Blueprint.Service/
└── Services/Implementation/
    └── TransactionBuilderService.cs   # V4→V1 (3 call sites)

tests/Sorcha.TransactionHandler.Tests/
├── BackwardCompatibility/
│   ├── VersionDetectionTests.cs       # Rewrite for V1-only
│   └── TransactionFactoryTests.cs     # Rewrite for V1-only
├── Unit/
│   ├── VersioningTests.cs             # Remove V2/V3/V4 theories
│   ├── TransactionBuilderTests.cs     # V4→V1 references
│   ├── SerializerTests.cs             # V4→V1 assertions
│   └── TransactionTests.cs            # V4→V1 assertions
├── Integration/
│   ├── EndToEndTransactionTests.cs    # V4→V1
│   ├── MultiRecipientTests.cs         # V4→V1
│   └── SigningVerificationTests.cs    # V4→V1
└── TestHelpers.cs                     # V4→V1 if referenced

tests/Sorcha.TransactionHandler.Benchmarks/
└── TransactionBenchmarks.cs           # V4→V1

tests/Sorcha.Blueprint.Service.Tests/
└── Services/TransactionBuilderServiceTests.cs  # V4→V1
```

**Structure Decision**: No new files or directories. This is purely modifying existing files within the established project structure.

## Post-Design Constitution Re-Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new projects, no cross-service coupling introduced. |
| II. Security First | PASS | Crypto signing/verification unchanged. Wire format structurally identical. |
| III. API Documentation | N/A | No public API changes. |
| IV. Testing Requirements | PASS | All 11 test files updated. No tests removed, only rewritten for V1-only. Coverage maintained. |
| V. Code Quality | PASS | Net reduction in code complexity (removing 3 factory methods + 3 enum values + adapter comments). |
| VI. Blueprint Standards | N/A | No blueprint format changes. |
| VII. Domain-Driven Design | PASS | "Transaction" and "Version" terms preserved. |
| VIII. Observability | N/A | No telemetry changes. |

**Post-design gate result**: PASS — no violations. Design is consistent with pre-research assessment.

## Complexity Tracking

No constitution violations — table not required.
