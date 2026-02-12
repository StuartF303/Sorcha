# Implementation Plan: Verifiable Credentials & eIDAS-Aligned Attestation System

**Branch**: `031-verifiable-credentials` | **Date**: 2026-02-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/031-verifiable-credentials/spec.md`

## Summary

Add a composable credential system to Sorcha blueprints where actions can require verifiable credentials as entry gates, blueprint flows can issue new credentials (SD-JWT VC format), and credential flows compose across blueprints. The system uses the participant's wallet for credential storage, supports selective disclosure natively, records all credential events on the immutable ledger, and enables issuer-maintained credential registers.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Sorcha.Blueprint.Models, Sorcha.Blueprint.Engine, Sorcha.Blueprint.Fluent, Sorcha.Cryptography, Sorcha.Wallet.Service, Sorcha.Register.Service, HeroSD-JWT (NuGet) or custom SD-JWT implementation
**Storage**: PostgreSQL (wallet credential store via EF Core), MongoDB (register transactions), Redis (revocation status cache)
**Testing**: xUnit 3.2.2, FluentAssertions 8.8.0, Moq 4.20.72
**Target Platform**: .NET 10 / Linux containers / .NET Aspire orchestration
**Project Type**: Microservices (existing architecture — modifications across multiple services)
**Performance Goals**: Credential verification < 2s (SC-002), revocation propagation < 1 min (SC-003)
**Constraints**: Must work within existing ActionProcessor pipeline; no new microservices; SD-JWT VC format per eIDAS 2.0 ARF
**Scale/Scope**: Modifications to 6 existing projects, 1 possible new NuGet dependency, ~15 new source files, ~20 new test files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new services. Changes spread across existing services (Blueprint, Wallet, Register). Dependencies flow downward. |
| II. Security First | PASS | Cryptographic verification on all credentials. Signature validation, revocation checks. No secrets in source. Input validation on all credential data via FluentValidation. |
| III. API Documentation | PASS | New wallet credential endpoints documented with Scalar/OpenAPI. XML docs on all public APIs. |
| IV. Testing Requirements | PASS | Target >85% coverage on new code. Unit tests for engine, crypto, models. Integration tests for wallet endpoints. |
| V. Code Quality | PASS | async/await for all I/O (signing, verification, ledger queries). DI throughout. .NET 10 / C# 13. Nullable enabled. |
| VI. Blueprint Creation Standards | PASS | Credential requirements/issuance defined in JSON format. Fluent API for developer scenarios only. |
| VII. Domain-Driven Design | PASS | Uses consistent terms: Blueprint, Action, Participant, Disclosure. New terms: Credential, CredentialRequirement, CredentialPresentation. |
| VIII. Observability by Default | PASS | Structured logging for credential operations. Health checks on wallet credential store. Telemetry for verification latency. |

**Gate Result: PASS — No violations.**

## Project Structure

### Documentation (this feature)

```text
specs/031-verifiable-credentials/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: library research, architecture decisions
├── data-model.md        # Phase 1: entity definitions and relationships
├── quickstart.md        # Phase 1: end-to-end usage example
├── contracts/           # Phase 1: API contracts
│   └── credential-endpoints.md
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2: implementation tasks (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.Blueprint.Models/
│   │   └── Credentials/                    # NEW: credential model classes
│   │       ├── CredentialRequirement.cs
│   │       ├── CredentialIssuanceConfig.cs
│   │       ├── ClaimConstraint.cs
│   │       ├── ClaimMapping.cs
│   │       ├── CredentialPresentation.cs
│   │       ├── CredentialRevocation.cs
│   │       ├── RevocationCheckPolicy.cs
│   │       └── CredentialValidationError.cs
│   ├── Sorcha.Cryptography/
│   │   └── SdJwt/                          # NEW: SD-JWT VC implementation
│   │       ├── ISdJwtService.cs
│   │       ├── SdJwtService.cs
│   │       ├── SdJwtToken.cs
│   │       ├── SdJwtPresentation.cs
│   │       └── SdJwtVerificationResult.cs
│   └── Sorcha.TransactionHandler/           # MODIFY: credential metadata in transactions
├── Core/
│   ├── Sorcha.Blueprint.Engine/
│   │   ├── Credentials/                    # NEW: credential verification pipeline
│   │   │   ├── ICredentialVerifier.cs
│   │   │   ├── CredentialVerifier.cs
│   │   │   ├── ICredentialIssuer.cs
│   │   │   ├── CredentialIssuer.cs
│   │   │   └── CredentialValidationResult.cs
│   │   ├── Implementation/
│   │   │   └── ActionProcessor.cs          # MODIFY: insert Step 0 credential check
│   │   └── Models/
│   │       ├── ExecutionContext.cs          # MODIFY: add CredentialPresentations
│   │       └── ActionExecutionResult.cs    # MODIFY: add credential results
│   └── Sorcha.Blueprint.Fluent/
│       ├── CredentialRequirementBuilder.cs  # NEW
│       ├── CredentialIssuanceBuilder.cs     # NEW
│       └── ActionBuilder.cs                 # MODIFY: add RequiresCredential()
├── Services/
│   ├── Sorcha.Blueprint.Service/
│   │   ├── Services/Implementation/
│   │   │   └── ActionExecutionService.cs   # MODIFY: pass credentials to engine
│   │   └── Endpoints/
│   │       └── CredentialEndpoints.cs      # NEW: revocation endpoint
│   └── Sorcha.Wallet.Service/
│       ├── Credentials/                    # NEW: credential storage & matching
│       │   ├── ICredentialStore.cs
│       │   ├── CredentialStore.cs
│       │   ├── CredentialMatcher.cs
│       │   └── CredentialEntity.cs         # EF Core entity
│       ├── Endpoints/
│       │   └── CredentialEndpoints.cs      # NEW: wallet credential endpoints
│       └── Migrations/
│           └── AddCredentialStore.cs       # NEW: EF Core migration

tests/
├── Sorcha.Blueprint.Models.Tests/
│   └── Credentials/                        # NEW: model validation tests
├── Sorcha.Blueprint.Engine.Tests/
│   └── Credentials/                        # NEW: verification pipeline tests
├── Sorcha.Blueprint.Fluent.Tests/
│   └── Credentials/                        # NEW: builder tests
├── Sorcha.Cryptography.Tests/
│   └── SdJwt/                              # NEW: SD-JWT format tests
├── Sorcha.Wallet.Service.Tests/
│   └── Credentials/                        # NEW: storage & matching tests
└── Sorcha.Blueprint.Service.Tests/
    └── Credentials/                        # NEW: issuance & revocation tests
```

**Structure Decision**: No new projects. All changes fit within existing projects as new subdirectories/namespaces. The credential system spans Blueprint Models (data), Blueprint Engine (verification), Blueprint Fluent (builder), Cryptography (SD-JWT), Wallet Service (storage), and Blueprint Service (issuance/revocation). This follows the existing layered architecture.

## Complexity Tracking

No constitution violations to justify.
