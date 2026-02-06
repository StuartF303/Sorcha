# Implementation Plan: Payload Encryption for DAD Security Model

**Branch**: `019-payload-encryption` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/019-payload-encryption/spec.md`

## Summary

Replace all stub/no-op cryptographic operations in `PayloadManager` with real encryption, decryption, hashing, and verification using the existing `Sorcha.Cryptography` library. This implements the foundation of the DAD (Disclosure, Alteration, Destruction) security model by ensuring all payload data on the distributed ledger is encrypted with authenticated encryption (XChaCha20-Poly1305 default / AES-GCM), per-recipient symmetric key wrapping via asymmetric encryption (ED25519/RSA-4096), SHA-256 content hashing for integrity verification, and backward compatibility with legacy unencrypted payloads.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: `Sorcha.Cryptography` (ISymmetricCrypto, ICryptoModule, IHashProvider) — all implementations already exist
**Storage**: In-memory (PayloadManager holds `List<Payload>` — no database changes needed)
**Testing**: xUnit 3.2, FluentAssertions, Moq — tests in `Sorcha.TransactionHandler.Tests`
**Target Platform**: .NET 10 server (Linux/Windows), also used in WASM-compatible Engine
**Project Type**: Library modification (existing `Sorcha.TransactionHandler` project)
**Performance Goals**: Encrypt/decrypt 1MB payload in <100ms on standard hardware
**Constraints**: Payload sizes up to 10MB; no streaming encryption; NIST P-256 not supported for key wrapping
**Scale/Scope**: 1 main file modified (`PayloadManager.cs`), 1 interface updated (`IPayloadManager.cs`), 10 call sites updated, ~30 new unit tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| **I. Microservices-First** | PASS | No new services. Changes are in Common library used by all services. Dependencies flow downward (TransactionHandler → Cryptography). |
| **II. Security First** | PASS | This feature *implements* the encryption required by the constitution. AES-256-GCM and XChaCha20-Poly1305 are both authenticated encryption. Key material is zeroized after use. |
| **III. API Documentation** | PASS | Internal library API — XML documentation on all public methods. No REST/gRPC endpoints added. |
| **IV. Testing Requirements** | PASS | Target >85% coverage on new code. ~30 new unit tests covering all acceptance scenarios. |
| **V. Code Quality** | PASS | Nullable reference types enabled. Async/await for crypto operations. DI for crypto dependencies. |
| **VI. Blueprint Standards** | N/A | No blueprint changes. |
| **VII. Domain-Driven Design** | PASS | Uses existing ubiquitous language: Payload, Disclosure, Recipient. |
| **VIII. Observability** | PASS | Structured logging for encryption/decryption failures. No new endpoints requiring health checks. |

**Post-Design Re-check**: All gates still pass. No new projects created. No architectural violations.

## Project Structure

### Documentation (this feature)

```text
specs/019-payload-encryption/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: Crypto research findings
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Build & verify commands
├── contracts/
│   └── payload-manager-api.md  # Internal API contract
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (from /speckit.tasks)
```

### Source Code (repository root)

```text
src/Common/Sorcha.TransactionHandler/
├── Payload/
│   └── PayloadManager.cs           # PRIMARY: Replace all stubs with real crypto
├── Interfaces/
│   └── IPayloadManager.cs          # Add RecipientKeyInfo/DecryptionKeyInfo types, update signatures
├── Models/
│   └── PayloadInfo.cs              # Add EncryptionType to GetInfo() (use actual type, not hardcoded)
├── Core/
│   └── TransactionBuilder.cs       # Pass crypto deps to new PayloadManager(...)
├── Serialization/
│   ├── JsonTransactionSerializer.cs    # Update new PayloadManager() call sites
│   └── BinaryTransactionSerializer.cs  # Update new PayloadManager() call sites
└── Versioning/
    └── TransactionFactory.cs        # Update 4x new PayloadManager() call sites

src/Services/Sorcha.Blueprint.Service/
└── Services/Implementation/
    └── TransactionBuilderService.cs # Update 3x new PayloadManager() call sites

tests/Sorcha.TransactionHandler.Tests/
└── Unit/
    └── PayloadManagerTests.cs       # NEW: ~30 encryption/decryption/verification tests
```

**Structure Decision**: No new projects or directories. All changes are modifications to existing files in `Sorcha.TransactionHandler` (Common library) and `Sorcha.Blueprint.Service` (call site updates). One new test file is added to the existing test project.

## Complexity Tracking

No constitution violations. No complexity justification needed.
