# Implementation Plan: Quantum-Safe Cryptography Upgrade

**Branch**: `040-quantum-safe-crypto` | **Date**: 2026-02-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/040-quantum-safe-crypto/spec.md`

## Summary

Upgrade Sorcha's cryptography layer to support NIST-standardized post-quantum algorithms (ML-DSA-65, SLH-DSA-128, ML-KEM-768) running concurrently alongside existing classical algorithms. All PQC code is encapsulated in `Sorcha.Cryptography` using BouncyCastle.NET. Register-level crypto policies govern which algorithms are accepted per-register, configured via control transactions and upgradeable over time. PQC wallet addresses use hash-based encoding (`ws2` prefix) to keep addresses compact despite larger public keys.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: BouncyCastle.Cryptography 2.6.2 (PQC), Sodium.Core 1.4.0 (ED25519), NBitcoin 9.0.4 (HD wallets)
**Storage**: MongoDB (register transactions), PostgreSQL (wallet metadata), Redis (caching)
**Testing**: xUnit + FluentAssertions + Moq (1,100+ existing tests)
**Target Platform**: Linux containers (Ubuntu Chiseled), Docker Compose orchestration
**Project Type**: Distributed microservices (7 services, 39 source projects)
**Performance Goals**: Hybrid signing < 500ms per transaction; throughput within 50% of classical-only
**Constraints**: All PQC in Sorcha.Cryptography only; backward compatible with existing registers; Bech32m addresses < 100 chars
**Scale/Scope**: Extends 3 core projects (Cryptography, Wallet.Core, Register.Models), touches 4 services (Wallet, Register, Validator, Peer)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | PQC encapsulated in shared library; no new service needed |
| II. Security First | PASS | NIST-standardized algorithms; key zeroization maintained; no secrets in code |
| III. API Documentation | PASS | New endpoints documented with Scalar/OpenAPI |
| IV. Testing Requirements | PASS | >85% coverage target for new PQC code; xUnit tests |
| V. Code Quality | PASS | Async/await, DI, nullable enabled, .NET 10 / C# 13 |
| VI. Blueprint Creation Standards | N/A | No blueprint format changes |
| VII. Domain-Driven Design | PASS | Uses Sorcha domain language (Register, Participant, Disclosure) |
| VIII. Observability by Default | PASS | Structured logging for crypto operations; OTel traces |

**Gate Result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/040-quantum-safe-crypto/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Entity definitions and relationships
├── quickstart.md        # Developer quickstart guide
├── contracts/           # API contracts
│   └── crypto-policy-api.md
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/Common/Sorcha.Cryptography/
├── Enums/
│   └── WalletNetworks.cs          # Extended with ML_DSA_65, SLH_DSA_128, ML_KEM_768
├── Core/
│   ├── CryptoModule.cs            # Extended switch expressions for PQC algorithms
│   ├── PqcSignatureProvider.cs    # NEW — ML-DSA-65 and SLH-DSA signing/verification
│   ├── PqcEncapsulationProvider.cs # NEW — ML-KEM-768 encapsulate/decapsulate
│   └── SymmetricCrypto.cs         # Unchanged (XChaCha20-Poly1305 remains quantum-safe)
├── Models/
│   ├── HybridSignature.cs         # NEW — Composite classical + PQC signature
│   └── CryptoPolicy.cs            # NEW — Register crypto policy model
├── Utilities/
│   ├── Bech32m.cs                 # NEW — Bech32m encoding for ws2 addresses
│   └── WalletUtilities.cs         # Extended for ws2 PQC address generation
└── Interfaces/
    └── ICryptoModule.cs           # Unchanged interface (network byte dispatch)

src/Common/Sorcha.Wallet.Core/
├── Services/Implementation/
│   └── KeyManagementService.cs    # Extended ParseAlgorithm + PQC derivation paths
└── Domain/ValueObjects/
    └── DerivationPath.cs          # Extended with PQC coin type (1')

src/Common/Sorcha.Register.Models/
├── TransactionModel.cs            # Signature field now supports HybridSignature JSON
├── CryptoPolicy.cs                # NEW — Shared crypto policy model
└── RegisterControlRecord.cs       # Extended with CryptoPolicy section

src/Services/Sorcha.Wallet.Service/
└── Endpoints/                     # Extended sign endpoint for hybrid mode

src/Services/Sorcha.Register.Service/
├── Services/
│   ├── RegisterCreationOrchestrator.cs  # Include default CryptoPolicy in genesis
│   └── GovernanceRosterService.cs       # Extract CryptoPolicy from control TX chain
└── Endpoints/                           # NEW crypto-policy query endpoints

src/Services/Sorcha.Validator.Service/
├── Services/
│   ├── ValidationEngine.cs              # Validate TX against register CryptoPolicy
│   └── ControlDocketProcessor.cs        # Handle control.crypto.update action type
└── BLS/                                 # NEW — BLS threshold signing (Phase 3)

tests/
├── Sorcha.Cryptography.Tests/
│   └── Unit/Pqc/                  # NEW — PQC algorithm tests
├── Sorcha.Register.Service.Tests/
│   └── CryptoPolicyTests.cs       # NEW — Policy enforcement tests
└── Sorcha.Validator.Service.Tests/
    └── CryptoPolicyValidationTests.cs  # NEW — Validation against policy
```

**Structure Decision**: Extends existing project structure. No new projects created — PQC code goes into `Sorcha.Cryptography`, policy models into `Sorcha.Register.Models`, and service extensions into existing service projects. This follows Constitution Principle I (microservices-first) with minimal coupling.

## Complexity Tracking

No constitution violations to justify.

## Implementation Phases

### Phase 1: Core PQC Algorithms in Sorcha.Cryptography (P1)

**Goal**: ML-DSA-65, SLH-DSA-128, ML-KEM-768 working in the cryptography library with full test coverage.

1. Add `BouncyCastle.Cryptography` 2.6.2 to `Sorcha.Cryptography.csproj`
2. Extend `WalletNetworks` enum with `ML_DSA_65 = 0x10`, `SLH_DSA_128 = 0x11`, `ML_KEM_768 = 0x12`
3. Create `PqcSignatureProvider` — ML-DSA-65 and SLH-DSA key generation, signing, verification via BouncyCastle
4. Create `PqcEncapsulationProvider` — ML-KEM-768 encapsulate/decapsulate via BouncyCastle
5. Extend `CryptoModule` — add cases to all 6 switch expressions for new network types
6. Create `HybridSignature` model — JSON-serializable composite signature
7. Add unit tests (>85% coverage) for all PQC operations
8. Verify existing tests still pass (zero regression)

**Exit Criteria**: `dotnet test tests/Sorcha.Cryptography.Tests` passes with new PQC tests and zero regressions.

### Phase 2: PQC Wallet Addresses & Key Management (P2)

**Goal**: `ws2` PQC addresses, hybrid wallet creation, HD derivation for PQC keys.

1. Create `Bech32m` utility (improved checksum variant for new address versions)
2. Extend `WalletUtilities` — `PublicKeyToWallet` for PQC keys: SHA-256 hash → Bech32m with `ws2` prefix
3. Extend `WalletUtilities` — `WalletToPublicKey` to recognize and decode `ws2` addresses (returns hash, not key)
4. Extend `KeyManagementService.ParseAlgorithm` for PQC algorithm names
5. Extend `DerivationPath` with PQC coin type (`m/44'/1'/...`) and factory method
6. Extend wallet creation endpoint for optional `pqcAlgorithm` and `enableHybrid` parameters
7. Extend wallet sign endpoint for `hybridMode` parameter
8. Add unit tests for address generation, key derivation, and hybrid signing
9. Add integration tests for wallet service PQC endpoints

**Exit Criteria**: Can create a PQC wallet via API, get `ws2` address, sign in hybrid mode, verify both signatures.

### Phase 3: Register Crypto Policy (P1)

**Goal**: Per-register crypto policy in genesis control transactions, upgradeable via governance.

1. Create `CryptoPolicy` model in `Sorcha.Register.Models`
2. Extend `RegisterControlRecord` with `CryptoPolicy` property
3. Update `RegisterCreationOrchestrator` — include default CryptoPolicy in genesis TX payload
4. Create `CryptoPolicyService` in Register Service — extract policy from control TX chain
5. Add `control.crypto.update` action type to `ControlDocketProcessor`
6. Update `ValidationEngine` — validate incoming TX signatures against register's active CryptoPolicy
7. Add governance endpoint for crypto policy updates (`POST /governance/crypto-policy`)
8. Add query endpoints (`GET /crypto-policy`, `GET /crypto-policy/history`)
9. Add YARP routes for new endpoints in API Gateway
10. Test: policy enforcement (accept/reject), policy upgrade, backward compatibility

**Exit Criteria**: Register with strict PQC policy rejects classical-only signatures; policy upgrade via control TX works; existing registers unaffected.

### Phase 4: Infrastructure & Integration Testing (P2)

**Goal**: Verify PQC works end-to-end across all services with acceptable performance.

1. Update Docker images (if any new native dependencies from BouncyCastle)
2. Run MedicalEquipmentRefurb walkthrough with PQC-enabled wallets
3. Create PQC-specific walkthrough scenario (PQC-only register)
4. Performance benchmarks: hybrid signing throughput vs classical-only
5. Measure MongoDB document sizes with PQC signatures
6. Verify gRPC peer replication with larger payloads
7. Update Aspire dashboard configuration if needed

**Exit Criteria**: All walkthroughs pass; hybrid throughput within 50% of classical; no infrastructure limits hit.

### Phase 5: BLS Threshold Signatures (P3)

**Goal**: Distributed docket signing with BLS threshold signatures.

1. Add BLS12-381 library dependency to `Sorcha.Cryptography`
2. Create `BLSThresholdProvider` — key share generation, partial signing, aggregation
3. Create `BLSAggregateSignature` model
4. Extend `GenesisManager` — support BLS aggregate docket signatures
5. Extend `ControlDocketProcessor` — verify BLS aggregate signatures
6. Create validator threshold setup/signing endpoints
7. Extend peer service for share distribution
8. Test: t-of-n threshold correctness, aggregation, verification

**Exit Criteria**: Docket signed by t-of-n validators, single aggregate signature verifiable.

### Phase 6: Zero-Knowledge Proofs (P3)

**Goal**: Privacy-preserving transaction inclusion and range proofs.

1. Add Bulletproof library to `Sorcha.Cryptography`
2. Create `ZKInclusionProofProvider` — Merkle proof + ZK verification
3. Create `RangeProofProvider` — Bulletproof range proof generation/verification
4. Add proof generation/verification endpoints to Register Service
5. Test: inclusion proof correctness, range proof correctness, proof size

**Exit Criteria**: Can prove transaction inclusion without revealing payload; can prove numeric range without revealing value.

## Dependencies Between Phases

```
Phase 1 (Core PQC) ──→ Phase 2 (Wallet Addresses)
                   ──→ Phase 3 (Crypto Policy) ──→ Phase 4 (Integration)
Phase 1 ──→ Phase 5 (BLS Threshold)
Phase 1 ──→ Phase 6 (ZK Proofs)
```

Phases 2 and 3 can run in parallel after Phase 1. Phases 5 and 6 are independent of each other and of Phase 4.

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| BouncyCastle PQC API changes | Low | Medium | Pin to 2.6.2; wrap in Sorcha abstractions |
| PQC performance worse than benchmarks | Medium | Medium | Benchmark early in Phase 1; fallback to SLH-DSA if ML-DSA too slow |
| Bech32m address collision | Very Low | High | SHA-256 gives 256-bit collision resistance; 2^128 birthday bound |
| HybridSignature JSON breaks old parsers | Low | High | Version field on TransactionModel; migration guide for consumers |
| BLS native library compatibility with chiseled images | Medium | Medium | Test in Docker early; fallback to managed implementation |
| Register policy migration conflicts | Low | Medium | Policies are append-only; historical validation uses policy-at-time |
