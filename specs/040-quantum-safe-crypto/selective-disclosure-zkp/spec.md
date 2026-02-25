# Feature Specification: BBS+ Selective Disclosure Signatures

**Feature Branch**: `040-quantum-safe-crypto`
**Created**: 2026-02-25
**Status**: Draft (Roadmap — Phase 6+)
**Parent Feature**: [Quantum-Safe Cryptography Upgrade](../spec.md)
**Input**: Add BBS+ selective disclosure signatures to enable zero-knowledge proofs over individual payload fields, aligned with Sorcha's existing JSON Pointer disclosure model.

## Overview

BBS+ signatures enable a signer to create a single signature over multiple messages (payload fields), and a holder to later derive a zero-knowledge proof that reveals only selected fields while proving the remaining fields were part of the original signed set. This maps directly to Sorcha's JSON Pointer-based disclosure model, where actions define which fields are disclosed to each participant.

### Relationship to Existing Features

- **Disclosure Model**: Sorcha blueprints already specify per-participant field disclosure via JSON Pointers. BBS+ adds cryptographic enforcement — instead of trusting the system to redact fields, recipients receive mathematical proof that undisclosed fields exist and were signed.
- **PQC Integration**: BBS+ operates on pairing-friendly curves (BLS12-381). While not itself quantum-safe, it can be combined with PQC signatures in a hybrid scheme — the BBS+ proof handles selective disclosure while an ML-DSA-65 signature covers the full payload for quantum resistance.
- **Register Crypto Policy**: BBS+ support is governed by the register's crypto policy, allowing registers to opt-in to selective disclosure capabilities.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Selective Field Disclosure in Transaction Payloads (Priority: P3)

A data contributor submits a transaction with multiple payload fields (e.g., device serial number, calibration date, technician name, test results). The blueprint specifies that the auditor participant should see calibration date and test results, but not the technician name or internal serial number. Using BBS+ signatures, the system creates a disclosure proof that reveals only the permitted fields while cryptographically proving the hidden fields were part of the original signed transaction.

**Why this priority**: Builds on the PQC foundation (P1-P2) and adds privacy-preserving verification. Not needed for MVP but critical for regulated industries (healthcare, finance) where data minimisation is legally required.

**Independent Test**: Can be tested by signing a multi-field payload, generating a selective disclosure proof for a subset of fields, and verifying the proof reveals only the selected fields.

**Acceptance Scenarios**:

1. **Given** a transaction with 5 payload fields signed using BBS+, **When** a disclosure proof is generated for 2 specific fields, **Then** the proof is valid and reveals only those 2 fields.
2. **Given** a valid BBS+ disclosure proof, **When** a verifier checks the proof, **Then** verification confirms the disclosed fields are authentic without revealing undisclosed fields.
3. **Given** a blueprint with JSON Pointer disclosure rules, **When** a BBS+ proof is generated for a participant, **Then** the disclosed fields match exactly the JSON Pointers specified for that participant.
4. **Given** a tampered disclosure proof (modified field value), **When** verification is attempted, **Then** verification fails.

---

### User Story 2 - Predicate Proofs on Numeric Fields (Priority: P3)

A register participant needs to prove that a numeric field (e.g., equipment age, certification score, temperature reading) satisfies a condition (e.g., "age < 5 years", "score >= 80") without revealing the actual value. Using BBS+ with predicate extensions, the system generates a proof that the condition is met without disclosing the underlying number.

**Acceptance Scenarios**:

1. **Given** a signed payload with a numeric field "calibrationScore: 92", **When** a predicate proof is generated for "calibrationScore >= 80", **Then** the proof is valid without revealing 92.
2. **Given** a signed payload with "equipmentAge: 3", **When** a predicate proof is generated for "equipmentAge < 5", **Then** the proof is valid without revealing 3.
3. **Given** a signed payload with "calibrationScore: 72", **When** a predicate proof is generated for "calibrationScore >= 80", **Then** proof generation fails (condition not met).

---

### User Story 3 - Hybrid BBS+ with PQC Signatures (Priority: P3)

A security-conscious register mandates both selective disclosure capability and quantum resistance. Transactions are signed with both BBS+ (for selective disclosure) and ML-DSA-65 (for quantum resistance). The BBS+ signature enables privacy-preserving field disclosure, while the ML-DSA-65 signature provides a quantum-safe integrity guarantee over the full payload.

**Acceptance Scenarios**:

1. **Given** a register requiring hybrid BBS+ and ML-DSA-65, **When** a transaction is signed, **Then** both a BBS+ signature and ML-DSA-65 signature are produced.
2. **Given** a hybrid-signed transaction, **When** a selective disclosure proof is generated, **Then** the proof includes the BBS+ disclosure proof and the ML-DSA-65 signature over the full payload hash.
3. **Given** a register crypto policy that does not include BBS+, **When** a BBS+ signed transaction is submitted, **Then** it is accepted based on its classical/PQC signature alone (BBS+ is optional).

---

## Functional Requirements *(mandatory)*

### Selective Disclosure Signing

| ID | Requirement |
|----|-------------|
| SD-FR-01 | The system shall support BBS+ signature creation over an ordered set of messages (payload fields) using BLS12-381 pairing curves |
| SD-FR-02 | The system shall support selective disclosure proof generation where the prover chooses which messages (fields) to reveal |
| SD-FR-03 | The system shall support selective disclosure proof verification that confirms revealed fields are authentic and part of the original signed set |
| SD-FR-04 | The system shall map JSON Pointer disclosure rules from blueprints to BBS+ message indices for automatic proof generation |
| SD-FR-05 | The system shall support predicate proofs for numeric fields (greater than, less than, equality, range) |

### Integration with Sorcha Architecture

| ID | Requirement |
|----|-------------|
| SD-FR-06 | BBS+ implementation shall be encapsulated within `Sorcha.Cryptography` with no BBS+ dependencies in service projects |
| SD-FR-07 | Register crypto policy shall include an optional `selectiveDisclosure` field to enable/disable BBS+ on a per-register basis |
| SD-FR-08 | BBS+ shall be combinable with any PQC signature algorithm in hybrid mode — the BBS+ proof covers selective disclosure while PQC covers quantum-safe integrity |
| SD-FR-09 | Transaction payloads signed with BBS+ shall store the BBS+ signature alongside classical/PQC signatures in the existing `HybridSignature` structure |
| SD-FR-10 | Disclosure proofs shall be verifiable without access to the original full payload — only the revealed fields and the proof are needed |

### Privacy and Security

| ID | Requirement |
|----|-------------|
| SD-FR-11 | Disclosure proofs shall be unlinkable — two proofs derived from the same signature shall not be correlatable to each other or to the original signature |
| SD-FR-12 | The system shall support proof freshness via a nonce to prevent replay of disclosure proofs |
| SD-FR-13 | BBS+ key material shall follow the same zeroisation and secure storage patterns as other cryptographic keys in Sorcha |

## Edge Cases & Failure Modes *(mandatory)*

| Scenario | Expected Behaviour |
|----------|-------------------|
| Disclosure proof requested for all fields | Proof is generated but functionally equivalent to revealing the BBS+ signature — system warns that no privacy benefit |
| Disclosure proof requested for zero fields | Proof generated confirming payload was signed without revealing any content |
| Payload field contains nested JSON objects | JSON Pointer paths map to flattened BBS+ message indices; nested objects are serialised to canonical JSON for signing |
| Blueprint disclosure rules change after signing | Disclosure proofs use the rules at proof generation time, not signing time — the BBS+ signature covers all fields regardless |
| BBS+ key compromise | Revocation follows existing wallet key revocation flow; previously generated proofs remain valid (they are derived from the signature, not the key) |
| Register does not support BBS+ | BBS+ signatures are ignored during validation; classical/PQC signatures are used |
| Very large payloads (100+ fields) | BBS+ performance scales linearly with message count; proof generation benchmarked to stay under 2 seconds for 100 fields |

## Success Criteria *(mandatory)*

| Metric | Target |
|--------|--------|
| Selective disclosure proof generation for 10 fields | < 500ms |
| Selective disclosure proof verification | < 200ms |
| BBS+ signature size (fixed regardless of field count) | < 200 bytes |
| Disclosure proof size per revealed field | < 100 bytes overhead |
| Predicate proof generation | < 1 second |
| Integration with JSON Pointer disclosure model | Automatic mapping without manual configuration |
| Hybrid BBS+ + PQC signing latency | < 1 second combined |

## Assumptions *(mandatory)*

1. BBS+ will use the BLS12-381 curve, which is the standard curve for BBS+ implementations and is also used for BLS threshold signatures in Phase 5.
2. The W3C BBS+ specification (draft) will be followed for interoperability, with Sorcha-specific extensions for predicate proofs.
3. BBS+ is classified as P3 priority — implementation begins after core PQC (P1) and wallet/policy (P2) features are complete.
4. The BouncyCastle.NET library may not include BBS+ — a separate library (e.g., Mattrglobal/bbs-signatures or custom implementation) may be needed.
5. Disclosure proof unlinkability is a design goal but may have performance trade-offs at very high field counts.

## Dependencies

- **Phase 1 (Core PQC)**: BBS+ keys and signatures are managed through the same `Sorcha.Cryptography` infrastructure.
- **Phase 3 (Register Crypto Policy)**: BBS+ support is governed by the register's crypto policy `selectiveDisclosure` field.
- **Phase 5 (BLS Threshold)**: BLS12-381 curve dependency is shared with BLS threshold signatures.
- **Blueprint Engine**: JSON Pointer disclosure rules must be accessible for automatic BBS+ message index mapping.

## Out of Scope

- BBS+ key rotation or re-signing of existing transactions with new BBS+ keys.
- Multi-party BBS+ signing (collaborative signing by multiple participants).
- Integration with external verifiable credential frameworks (W3C VC-DATA-MODEL).
- BBS+ over non-JSON payloads (binary, XML).
