# Tasks: Quantum-Safe Cryptography Upgrade

**Input**: Design documents from `/specs/040-quantum-safe-crypto/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/crypto-policy-api.md

**Tests**: Included — CLAUDE.md mandates >85% coverage; plan.md Phase 1 explicitly requires unit tests for all PQC operations.

**Organization**: Tasks grouped by user story (7 stories across 3 priority tiers). Each story is independently testable after the Foundational phase completes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add PQC library dependency and extend core enums for new algorithm types.

- [X] T001 Add BouncyCastle.Cryptography 2.6.2 package reference to src/Common/Sorcha.Cryptography/Sorcha.Cryptography.csproj
- [X] T002 Extend WalletNetworks byte enum with ML_DSA_65 = 0x10, SLH_DSA_128s = 0x11, ML_KEM_768 = 0x12 in src/Common/Sorcha.Cryptography/Enums/WalletNetworks.cs

---

## Phase 2: Foundational (Core PQC Library)

**Purpose**: Build the core PQC primitives in Sorcha.Cryptography that ALL user stories depend on. No service-layer work yet — pure library code and models.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 [P] Create PqcSignatureProvider with ML-DSA-65 key generation, signing, and verification using BouncyCastle MLDsaKeyPairGenerator/MLDsaSigner in src/Common/Sorcha.Cryptography/Core/PqcSignatureProvider.cs
- [X] T004 [P] Create PqcEncapsulationProvider with ML-KEM-768 encapsulate and decapsulate using BouncyCastle MLKemGenerator/MLKemExtractor in src/Common/Sorcha.Cryptography/Core/PqcEncapsulationProvider.cs
- [X] T005 [P] Create HybridSignature model (Classical, ClassicalAlgorithm, Pqc, PqcAlgorithm, WitnessPublicKey) with JSON serialization and validation (at least one signature required) in src/Common/Sorcha.Cryptography/Models/HybridSignature.cs
- [X] T006 [P] Create Bech32m encoding and decoding utility (improved checksum variant for ws2 addresses) in src/Common/Sorcha.Cryptography/Utilities/Bech32m.cs
- [X] T007 [P] Create CryptoPolicy model (Version, AcceptedSignatureAlgorithms, RequiredSignatureAlgorithms, AcceptedEncryptionSchemes, AcceptedHashFunctions, EnforcementMode, EffectiveFrom, DeprecatedAlgorithms) with default policy factory in src/Common/Sorcha.Register.Models/CryptoPolicy.cs
- [X] T008 Extend CryptoModule with ML_DSA_65 and ML_KEM_768 cases in all 6 switch expressions (Sign, Verify, Encrypt, Decrypt, Generate, Recover) dispatching to PqcSignatureProvider and PqcEncapsulationProvider in src/Common/Sorcha.Cryptography/Core/CryptoModule.cs
- [X] T009 Implement IDisposable with CryptographicOperations.ZeroMemory for PQC key material in PqcSignatureProvider and PqcEncapsulationProvider; ensure private keys are zeroized after sign/decapsulate operations and on disposal in src/Common/Sorcha.Cryptography/Core/PqcSignatureProvider.cs and src/Common/Sorcha.Cryptography/Core/PqcEncapsulationProvider.cs
- [X] T010 [P] Unit tests for PqcSignatureProvider: ML-DSA-65 key generation produces valid key pair, signing produces verifiable signature, verification rejects tampered data in tests/Sorcha.Cryptography.Tests/Unit/Pqc/PqcSignatureProviderTests.cs
- [X] T011 [P] Unit tests for PqcEncapsulationProvider: ML-KEM-768 encapsulate produces ciphertext and shared secret, decapsulate recovers same shared secret, mismatched keys fail in tests/Sorcha.Cryptography.Tests/Unit/Pqc/PqcEncapsulationProviderTests.cs
- [X] T012 [P] Unit tests for HybridSignature JSON round-trip serialization, validation (reject empty), and Bech32m encode/decode round-trip with ws2 HRP in tests/Sorcha.Cryptography.Tests/Unit/Pqc/HybridSignatureAndBech32mTests.cs
- [X] T013 [P] Unit tests for CryptoPolicy model: default policy factory, validation (RequiredSignatureAlgorithms subset of Accepted), JSON serialization in tests/Sorcha.Cryptography.Tests/Unit/Pqc/CryptoPolicyModelTests.cs
- [X] T014 Verify all existing cryptography tests pass with zero regressions after CryptoModule extension by running dotnet test tests/Sorcha.Cryptography.Tests

**Checkpoint**: Core PQC library complete — ML-DSA-65, ML-KEM-768, HybridSignature, Bech32m, CryptoPolicy all working with >85% coverage. Key material zeroization verified. User story implementation can now begin.

---

## Phase 3: User Story 1 — Hybrid Quantum-Safe Signing (Priority: P1) MVP

**Goal**: Participants can create wallets with both classical and PQC key pairs, sign transactions producing both signatures concurrently, and validators accept transactions where either signature is valid.

**Independent Test**: Create a wallet with PQC keys, sign a message, verify the signature using both classical and PQC algorithms independently.

### Tests for User Story 1

- [X] T015 [P] [US1] Unit tests for hybrid signing: concurrent classical + PQC signature production, HybridSignature assembly, timing within 500ms in tests/Sorcha.Cryptography.Tests/Unit/Pqc/HybridSigningTests.cs
- [X] T016 [P] [US1] Unit tests for hybrid verification: classical-only accepted, PQC-only accepted, both-valid accepted, both-invalid rejected in tests/Sorcha.Cryptography.Tests/Unit/Pqc/HybridVerificationTests.cs

### Implementation for User Story 1

- [X] T017 [US1] Implement hybrid signing method in CryptoModule that concurrently signs with classical and PQC keys and assembles HybridSignature in src/Common/Sorcha.Cryptography/Core/CryptoModule.cs
- [X] T018 [US1] Implement hybrid verification method in CryptoModule that accepts transaction if either classical or PQC signature is valid in src/Common/Sorcha.Cryptography/Core/CryptoModule.cs
- [X] T019 [US1] Extend KeyManagementService.ParseAlgorithm to recognize PQC algorithm names (ML-DSA-65, SLH-DSA-128s, ML-KEM-768) in src/Common/Sorcha.Wallet.Core/Services/Implementation/KeyManagementService.cs
- [X] T020 [US1] Extend wallet sign endpoint with hybridMode boolean parameter that triggers dual signing in src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs
- [X] T021 [US1] Integration test: create hybrid wallet via API, sign data with hybridMode=true, verify both signatures independently in tests/Sorcha.Wallet.Service.Tests/

**Checkpoint**: Hybrid signing end-to-end working. A wallet can produce concurrent classical + PQC signatures, and either signature validates independently.

---

## Phase 4: User Story 2 — Register Crypto Policy via Control Transactions (Priority: P1)

**Goal**: Register genesis includes a crypto policy specifying accepted algorithms. Policy upgradeable via control transactions. Validators enforce policy on incoming transactions.

**Independent Test**: Create a register with a specific crypto policy, submit matching transactions (accepted) and violating transactions (rejected), then upgrade the policy via control transaction and verify new rules apply.

### Tests for User Story 2

- [X] T022 [P] [US2] Unit tests for CryptoPolicyService: extract active policy from control TX chain, return default if none set, policy version ordering in tests/Sorcha.Register.Service.Tests/CryptoPolicyServiceTests.cs
- [X] T023 [P] [US2] Unit tests for crypto policy validation in ValidationEngine: accept matching TX, reject violating TX, validate against policy-at-submission-time for pre-upgrade TX in tests/Sorcha.Validator.Service.Tests/CryptoPolicyValidationTests.cs

### Implementation for User Story 2

- [X] T024 [US2] Extend RegisterControlRecord with CryptoPolicy property (nullable, for backward compatibility with existing registers) in src/Common/Sorcha.Register.Models/RegisterControlRecord.cs
- [X] T025 [US2] Update RegisterCreationOrchestrator to include default CryptoPolicy (hybrid mode, all algorithms accepted) in genesis control TX payload in src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs
- [X] T026 [US2] Create CryptoPolicyService to extract active crypto policy from a register's control TX chain (latest version wins) in src/Services/Sorcha.Register.Service/Services/CryptoPolicyService.cs
- [X] T027 [US2] Add control.crypto.update action type handling to ControlDocketProcessor for crypto policy upgrade control transactions in src/Services/Sorcha.Validator.Service/Services/ControlDocketProcessor.cs
- [X] T028 [US2] Update ValidationEngine to validate incoming TX signatures against the register's active CryptoPolicy (check AcceptedSignatureAlgorithms, RequiredSignatureAlgorithms, EnforcementMode) in src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs
- [X] T029 [P] [US2] Implement GET /api/registers/{registerId}/crypto-policy endpoint returning active policy in src/Services/Sorcha.Register.Service/Endpoints/
- [X] T030 [P] [US2] Implement POST /api/registers/{registerId}/governance/crypto-policy endpoint for policy update submission as control TX in src/Services/Sorcha.Register.Service/Endpoints/
- [X] T031 [P] [US2] Implement GET /api/registers/{registerId}/crypto-policy/history endpoint returning all policy versions in src/Services/Sorcha.Register.Service/Endpoints/
- [X] T032 [US2] Add YARP routes for crypto-policy endpoints (/api/registers/*/crypto-policy, /api/registers/*/governance/crypto-policy) in src/Services/Sorcha.ApiGateway/appsettings.json

**Checkpoint**: Register crypto policy governance working. New registers get default hybrid policy; policy upgrades via control TX enforce on subsequent transactions; existing registers unaffected.

---

## Phase 5: User Story 7 — Hash-Based Signature Fallback (Priority: P2)

**Goal**: SLH-DSA (SPHINCS+) available as a mathematically independent signature fallback using hash-based cryptography, providing algorithm diversity against potential lattice-based attacks.

**Independent Test**: Generate SLH-DSA keys, sign data, and verify the signature — completely independent of ML-DSA infrastructure.

### Tests for User Story 7

- [ ] T033 [P] [US7] Unit tests for SLH-DSA-128s: key generation (64-byte private, 32-byte public), signing (7,856-byte signature), verification, and SLH-DSA-192s variant (16,224-byte signature) in tests/Sorcha.Cryptography.Tests/Unit/Pqc/SlhDsaSignatureTests.cs

### Implementation for User Story 7

- [ ] T034 [US7] Add SLH-DSA-128s and SLH-DSA-192s key generation, signing, and verification to PqcSignatureProvider using BouncyCastle SlhDsaKeyPairGenerator/SlhDsaSigner in src/Common/Sorcha.Cryptography/Core/PqcSignatureProvider.cs
- [ ] T035 [US7] Extend CryptoModule switch expressions with SLH_DSA_128s cases dispatching to PqcSignatureProvider SLH-DSA methods in src/Common/Sorcha.Cryptography/Core/CryptoModule.cs
- [ ] T036 [US7] Integration test: register with crypto policy requiring hash-based signatures rejects ML-DSA-only and accepts SLH-DSA-signed transactions (depends on US2 policy enforcement) in tests/Sorcha.Validator.Service.Tests/SlhDsaPolicyIntegrationTests.cs

**Checkpoint**: SLH-DSA available as fallback. Algorithm diversity achieved — if lattice-based ML-DSA is ever compromised, SLH-DSA provides an independent hash-based alternative.

---

## Phase 6: User Story 3 — Quantum-Safe Wallet Addresses (Priority: P2)

**Goal**: PQC wallets get compact ws2-prefixed addresses (under 100 characters) by hashing the large PQC public key. Full public key travels as witness data in transactions.

**Independent Test**: Generate a PQC wallet, verify address is compact and human-usable, recover the full public key from transaction witness data, and verify it matches the address hash.

### Tests for User Story 3

- [ ] T037 [P] [US3] Unit tests for ws2 address: generation produces <100 char address with ws2 prefix, round-trip encode/decode, address-key binding (hash of public key matches address), classical ws1 addresses unaffected in tests/Sorcha.Cryptography.Tests/Unit/Pqc/PqcWalletAddressTests.cs

### Implementation for User Story 3

- [ ] T038 [US3] Extend WalletUtilities.PublicKeyToWallet to handle PQC keys: SHA-256(network_byte + public_key) → Bech32m encode with ws2 HRP in src/Common/Sorcha.Cryptography/Utilities/WalletUtilities.cs
- [ ] T039 [US3] Extend WalletUtilities.WalletToPublicKey to recognize ws2 prefix and decode Bech32m (returns hash, not key — flag that full key must come from witness data) in src/Common/Sorcha.Cryptography/Utilities/WalletUtilities.cs
- [ ] T040 [US3] Extend DerivationPath with PQC coin type (m/44'/1'/account'/change/index) and factory method for PQC paths in src/Common/Sorcha.Wallet.Core/Domain/ValueObjects/DerivationPath.cs
- [ ] T041 [US3] Extend wallet creation endpoint with optional pqcAlgorithm and enableHybrid parameters; response includes both walletAddress (ws1) and pqcWalletAddress (ws2) in src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs
- [ ] T042 [US3] Implement witness public key inclusion in transaction signing — when signing with a PQC wallet, include full PQC public key as WitnessPublicKey field in HybridSignature in src/Common/Sorcha.Cryptography/Core/CryptoModule.cs
- [ ] T043 [US3] Integration test: create PQC wallet, verify ws2 address format, sign transaction, extract witness key, hash witness key, confirm hash matches address in tests/Sorcha.Wallet.Service.Tests/

**Checkpoint**: PQC wallet addresses working. Compact ws2 addresses under 100 chars, full key in witness data, address-key binding verifiable.

---

## Phase 7: User Story 4 — Quantum-Safe Payload Encryption (Priority: P2)

**Goal**: PQC key encapsulation (ML-KEM-768) establishes shared secrets for field-level payload encryption, integrating with the existing XChaCha20-Poly1305 symmetric layer.

**Independent Test**: Encrypt a payload with ML-KEM-768 encapsulation, transmit the ciphertext, and decrypt with the corresponding private key — verify plaintext matches.

### Tests for User Story 4

- [ ] T044 [P] [US4] Unit tests for PQC encryption flow: ML-KEM-768 encapsulate → shared secret → XChaCha20-Poly1305 encrypt → decrypt → match original plaintext in tests/Sorcha.Cryptography.Tests/Unit/Pqc/PqcEncryptionFlowTests.cs

### Implementation for User Story 4

- [ ] T045 [US4] Implement PQC key encapsulation + symmetric encryption composed flow (ML-KEM-768 establishes shared secret fed into existing SymmetricCrypto.Encrypt) in src/Common/Sorcha.Cryptography/Core/PqcEncapsulationProvider.cs
- [ ] T046 [P] [US4] Implement POST /api/v1/wallets/{address}/encapsulate endpoint (accepts recipient PQC public key, returns ciphertext + shared secret) in src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs
- [ ] T047 [P] [US4] Implement POST /api/v1/wallets/{address}/decapsulate endpoint (accepts ciphertext + derivation path, returns shared secret) in src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs
- [ ] T048 [US4] Integration test: encapsulate with recipient public key → transmit ciphertext → decapsulate with private key → shared secrets match → symmetric decrypt succeeds in tests/Sorcha.Wallet.Service.Tests/

**Checkpoint**: PQC payload encryption working. ML-KEM-768 encapsulation feeds into existing XChaCha20-Poly1305 for quantum-safe field-level encryption.

---

## Phase 8: User Story 5 — BLS Threshold Signatures for Distributed Validation (Priority: P3)

**Goal**: t-of-n validators produce BLS signing shares that combine into a single compact aggregate signature for docket signing, removing single point of failure.

**Independent Test**: Generate n signing shares, combine t of them into an aggregate signature, verify it validates against the shared public key — while t-1 shares fail to produce a valid aggregate.

### Tests for User Story 5

- [ ] T049 [P] [US5] Unit tests for BLS threshold: t-of-n key share generation, partial signing, aggregation produces valid signature, t-1 shares fail verification, aggregate signature size is constant (~33 bytes) in tests/Sorcha.Cryptography.Tests/Unit/Pqc/BLSThresholdTests.cs

### Implementation for User Story 5

- [ ] T050 [US5] Add BLS12-381 library dependency (Herumi BLS C# bindings NuGet package) to src/Common/Sorcha.Cryptography/Sorcha.Cryptography.csproj
- [ ] T051 [P] [US5] Create BLSThresholdProvider with key share generation (DKG), partial signing, and share aggregation into single signature in src/Common/Sorcha.Cryptography/Core/BLSThresholdProvider.cs
- [ ] T052 [P] [US5] Create BLSAggregateSignature model (Signature, SignerBitfield, Threshold, TotalSigners) and BLSSigningShare model (ValidatorId, ShareIndex, PartialSignature, DocketHash) in src/Common/Sorcha.Cryptography/Models/BLSModels.cs
- [ ] T053 [US5] Extend docket sealing in GenesisManager/ControlDocketProcessor to support BLS aggregate signatures as docket proposer signature in src/Services/Sorcha.Validator.Service/Services/
- [ ] T054 [P] [US5] Implement POST /api/v1/validators/threshold/setup endpoint (initialize BLS threshold for register validator set, distribute encrypted key shares) in src/Services/Sorcha.Validator.Service/Endpoints/
- [ ] T055 [P] [US5] Implement POST /api/v1/validators/threshold/sign endpoint (submit partial BLS signature for docket, track threshold progress) in src/Services/Sorcha.Validator.Service/Endpoints/
- [ ] T056 [US5] Extend peer service gRPC for secure BLS key share distribution — add ShareDistribution RPC method to distribute encrypted key shares to validator nodes after threshold setup in src/Services/Sorcha.Peer.Service/GrpcServices/
- [ ] T057 [US5] Add YARP routes for validator threshold endpoints (/api/v1/validators/threshold/*) in src/Services/Sorcha.ApiGateway/appsettings.json

**Checkpoint**: BLS threshold docket signing working. Docket sealed by t-of-n validators with a single constant-size aggregate signature. Key shares securely distributed via peer gRPC.

---

## Phase 9: User Story 6 — Zero-Knowledge Register Verification (Priority: P3)

**Goal**: Auditors can verify transaction inclusion and numeric constraints in dockets without seeing payload data, using ZK proofs.

**Independent Test**: Create a Merkle proof for a transaction, generate a ZK inclusion proof, and verify the proof without access to the original transaction data.

### Tests for User Story 6

- [ ] T058 [P] [US6] Unit tests for ZK proofs: inclusion proof generation from Merkle tree, verification without original data, range proof generation and verification, proof size within expected bounds, verification completes in under 1 second (SC-008) in tests/Sorcha.Cryptography.Tests/Unit/Pqc/ZKProofTests.cs

### Implementation for User Story 6

- [ ] T059 [US6] Add Bulletproof library dependency to src/Common/Sorcha.Cryptography/Sorcha.Cryptography.csproj
- [ ] T060 [P] [US6] Create ZKInclusionProofProvider for generating and verifying Merkle inclusion proofs with ZK privacy (proves inclusion without revealing transaction content) in src/Common/Sorcha.Cryptography/Core/ZKInclusionProofProvider.cs
- [ ] T061 [P] [US6] Create RangeProofProvider for Bulletproof range proof generation and verification (proves numeric constraint without revealing value) in src/Common/Sorcha.Cryptography/Core/RangeProofProvider.cs
- [ ] T062 [P] [US6] Create ZKInclusionProof model (DocketId, MerkleRoot, ProofData, VerificationKey) and RangeProof model (Commitment, ProofData, BitLength) in src/Common/Sorcha.Cryptography/Models/ZKModels.cs
- [ ] T063 [P] [US6] Implement POST /api/registers/{registerId}/proofs/inclusion endpoint for ZK inclusion proof generation in src/Services/Sorcha.Register.Service/Endpoints/
- [ ] T064 [P] [US6] Implement POST /api/registers/{registerId}/proofs/verify-inclusion endpoint for ZK inclusion proof verification in src/Services/Sorcha.Register.Service/Endpoints/
- [ ] T065 [US6] Integration test: create register with transactions, generate ZK inclusion proof for specific TX, verify proof without access to original payload data in tests/Sorcha.Register.Service.Tests/

**Checkpoint**: ZK verification working. Auditors can prove transaction inclusion and numeric constraints without seeing payload data.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, performance validation, infrastructure verification, end-to-end testing, and constitution compliance across all user stories.

- [ ] T066 [P] Update .specify/MASTER-TASKS.md with PQC feature task status and completion tracking
- [ ] T067 [P] Update docs/development-status.md with quantum-safe cryptography capabilities and algorithm support matrix
- [ ] T068 Performance benchmark: measure hybrid signing throughput vs classical-only, verify within 50% overhead (SC-005) and per-operation under 500ms (SC-006)
- [ ] T069 Verify MongoDB document sizes with PQC signatures (~13.4KB avg) stay well within 16MB limit per research.md R9
- [ ] T070 Verify gRPC peer replication handles transactions with PQC signatures within 4MB message limit per research.md R9
- [ ] T071 Run quickstart.md validation scenarios end-to-end (PQC wallet creation, hybrid signing, crypto policy, ws2 addresses)
- [ ] T072 Verify all existing walkthroughs pass on registers with default hybrid crypto policy (SC-009 — zero regression)
- [ ] T073 [P] Add XML documentation comments and Scalar OpenAPI annotations (WithName, WithSummary, WithDescription) to all new endpoints: crypto-policy (T029-T031), encapsulate/decapsulate (T046-T047), threshold (T054-T055), and ZK proofs (T063-T064) per Constitution Principle III
- [ ] T074 [P] Create FluentValidation validators for new endpoint request models: CryptoPolicyUpdateRequest (RequiredSignatureAlgorithms subset of Accepted, at least one accepted), ThresholdSetupRequest (threshold <= totalValidators, both > 0), ThresholdSignRequest (valid base64 signature), InclusionProofRequest (valid 64-char hex TxId) per Constitution Principle II

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──→ Phase 2 (Foundational) ──→ Phase 3 (US1: Hybrid Signing) ──→ Phase 10 (Polish)
                                             ──→ Phase 4 (US2: Crypto Policy) ──→ Phase 10 (Polish)
                                             ──→ Phase 5 (US7: SLH-DSA) ──→ Phase 10 (Polish)
                                             ──→ Phase 6 (US3: Wallet Addresses) ──→ Phase 10 (Polish)
                                             ──→ Phase 7 (US4: Payload Encryption) ──→ Phase 10 (Polish)
                                             ──→ Phase 8 (US5: BLS Threshold) ──→ Phase 10 (Polish)
                                             ──→ Phase 9 (US6: ZK Proofs) ──→ Phase 10 (Polish)
```

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **US2 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories; can run in parallel with US1
- **US7 (P2)**: Can start after Foundational (Phase 2) — extends PqcSignatureProvider; integration test depends on US2 policy enforcement (T036)
- **US3 (P2)**: Can start after Foundational (Phase 2) — depends on Bech32m from Phase 2; can run in parallel with US1/US2
- **US4 (P2)**: Can start after Foundational (Phase 2) — depends on PqcEncapsulationProvider from Phase 2
- **US5 (P3)**: Can start after Foundational (Phase 2) — independent BLS library; no cross-story dependency
- **US6 (P3)**: Can start after Foundational (Phase 2) — independent Bulletproof library; no cross-story dependency

### Within Each User Story

- Tests MUST be written and FAIL before implementation (where test tasks precede implementation)
- Models → Services → Endpoints → Integration
- Core implementation before service integration
- Story complete before moving to next priority (sequential) or stories can proceed in parallel if team capacity allows

### Parallel Opportunities

- **Phase 2**: T003-T007 all [P] — different files, no dependencies
- **Phase 2**: T010-T013 all [P] — test files, no dependencies
- **Phase 3 (US1)**: T015-T016 [P] — test files
- **Phase 4 (US2)**: T022-T023 [P] — test files; T029-T031 [P] — different endpoints
- **Phase 7 (US4)**: T046-T047 [P] — different endpoints
- **Phase 8 (US5)**: T051-T052 [P] — different files; T054-T055 [P] — different endpoints
- **Phase 9 (US6)**: T060-T064 all [P] — different files and endpoints
- **Phase 10**: T066-T067 [P] — different docs; T073-T074 [P] — different concerns
- **Cross-story**: After Phase 2, US1/US2/US3/US4/US5/US6/US7 can all proceed in parallel

---

## Parallel Example: Foundational Phase

```bash
# Launch all PQC providers and models in parallel:
Task: "Create PqcSignatureProvider (ML-DSA-65) in src/Common/Sorcha.Cryptography/Core/PqcSignatureProvider.cs"
Task: "Create PqcEncapsulationProvider (ML-KEM-768) in src/Common/Sorcha.Cryptography/Core/PqcEncapsulationProvider.cs"
Task: "Create HybridSignature model in src/Common/Sorcha.Cryptography/Models/HybridSignature.cs"
Task: "Create Bech32m utility in src/Common/Sorcha.Cryptography/Utilities/Bech32m.cs"
Task: "Create CryptoPolicy model in src/Common/Sorcha.Register.Models/CryptoPolicy.cs"

# Then sequentially: CryptoModule extension (depends on providers), then zeroization
Task: "Extend CryptoModule switch expressions for PQC algorithms"
Task: "Implement key material zeroization in PQC providers"

# Then launch all tests in parallel:
Task: "Unit tests for PqcSignatureProvider"
Task: "Unit tests for PqcEncapsulationProvider"
Task: "Unit tests for HybridSignature and Bech32m"
Task: "Unit tests for CryptoPolicy model"
```

## Parallel Example: After Foundational Phase

```bash
# Launch multiple user stories in parallel (different services, no conflicts):
Agent 1: US1 (Hybrid Signing) — touches CryptoModule, WalletEndpoints
Agent 2: US2 (Crypto Policy) — touches RegisterControlRecord, RegisterCreationOrchestrator, ValidationEngine
Agent 3: US3 (Wallet Addresses) — touches WalletUtilities, DerivationPath
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (2 tasks)
2. Complete Phase 2: Foundational — Core PQC Library (12 tasks)
3. Complete Phase 3: User Story 1 — Hybrid Signing (7 tasks)
4. **STOP AND VALIDATE**: Create a wallet with PQC keys, sign with hybrid mode, verify both signatures
5. Deploy/demo if ready — immediate quantum resistance available

### Incremental Delivery

1. Setup + Foundational → Core PQC library ready
2. US1 (Hybrid Signing) → **MVP** — quantum-resistant signing available
3. US2 (Crypto Policy) → Per-register governance — policy enforcement active
4. US7 (SLH-DSA) → Algorithm diversity — hash-based fallback available
5. US3 (Wallet Addresses) → Compact ws2 addresses — user-facing identity ready
6. US4 (Payload Encryption) → Quantum-safe encryption — "harvest now, decrypt later" mitigated
7. US5 (BLS Threshold) → Distributed validation — single point of failure removed
8. US6 (ZK Proofs) → Privacy-preserving verification — DAD Disclosure enhanced
9. Polish → Performance validated, docs updated, walkthroughs passing, constitution compliance verified

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 (Hybrid Signing) + US3 (Wallet Addresses) — both touch wallet layer
   - Developer B: US2 (Crypto Policy) + US7 (SLH-DSA) — both touch register/validator layer
   - Developer C: US4 (Payload Encryption) — touches encryption layer
3. After P1/P2 complete:
   - Developer A: US5 (BLS Threshold)
   - Developer B: US6 (ZK Proofs)
4. Team completes Polish phase together

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable after Foundational phase
- All PQC code MUST stay encapsulated in Sorcha.Cryptography (FR-014)
- Key material MUST be zeroized after use (FR-016) — implemented in T009
- All new public API endpoints MUST include XML doc comments and Scalar OpenAPI annotations (Constitution III) — tracked in T073
- All new endpoints accepting request bodies MUST include FluentValidation validators (Constitution II) — tracked in T074
- License header required on all new files: `// SPDX-License-Identifier: MIT` + `// Copyright (c) 2026 Sorcha Contributors`
- Commit after each task or logical group; reference task IDs in commits
