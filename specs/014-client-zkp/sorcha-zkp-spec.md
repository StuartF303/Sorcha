# Feature Specification: Browser Cryptographic Capabilities

**Feature ID:** SORCHA-CRYPTO-001  
**Created:** 2026-01-22  
**Status:** Draft  
**Owner:** Stuart Fraser

---

## Executive Summary

Enable Sorcha Wallet Service to perform cryptographic operations (key derivation, signing, zero-knowledge proofs) directly in the browser while maintaining enterprise-grade security. Private keys derived from a central custodial wallet are protected using WebAuthn PRF hardware binding, ensuring keys never exist in plaintext outside secure enclaves.

---

## Problem Statement

### Current State
Sorcha's Wallet Service currently handles cryptographic operations server-side, requiring users to trust the server with signing operations. This creates:
- A single point of compromise for private keys
- Inability to provide true non-repudiation for user signatures
- Incompatibility with eIDAS 2.0 requirements for Qualified Electronic Signatures
- No mechanism for privacy-preserving credential proofs

### Desired State
Users can sign Blueprints and prove credential attributes directly in their browser, with private keys protected by hardware authenticators. The server never sees raw private key material after initial derivation.

### Impact
- **Security:** Eliminates server-side key exposure risk
- **Compliance:** Enables path to eIDAS 2.0 QSCD compliance
- **Privacy:** Users can prove attributes (age, nationality) without revealing underlying data
- **User Trust:** Cryptographic non-repudiation for all signed operations

---

## User Stories

### US-001: Blueprint Signing with Hardware Protection
**As a** Sorcha user  
**I want to** sign Blueprints using a key protected by my hardware authenticator  
**So that** only I can authorise actions on my behalf, with cryptographic proof

**Acceptance Criteria:**
- [ ] User triggers signing from Blazor UI
- [ ] System prompts for WebAuthn gesture (touch/biometric)
- [ ] Signature is generated without private key leaving browser crypto subsystem
- [ ] Signed Blueprint is submitted to Wallet Service with verifiable signature

**Independent Test:** Can be fully tested by signing a test Blueprint and verifying the signature server-side using only the user's public key.

---

### US-002: Credential Attribute Proof
**As a** Sorcha user with stored credentials  
**I want to** prove I meet certain criteria (e.g., age ≥ 18, UK resident) without revealing my actual data  
**So that** I can satisfy verification requirements while preserving my privacy

**Acceptance Criteria:**
- [ ] User selects credential and predicate to prove
- [ ] ZKP circuit generates proof in browser (WASM)
- [ ] Proof is verifiable by any party with the verification key
- [ ] Original credential data never leaves the browser

**Independent Test:** Can be fully tested by generating a proof for "age ≥ 18" and having an independent verifier confirm validity without access to the user's birthdate.

---

### US-003: Key Recovery Setup
**As a** Sorcha user  
**I want to** set up recovery options for my derived keys  
**So that** I can regain access if I lose my authenticator

**Acceptance Criteria:**
- [ ] User can register multiple authenticators
- [ ] User can opt into server-side encrypted escrow (with MFA recovery)
- [ ] Recovery process requires strong authentication
- [ ] Audit trail records all recovery attempts

**Independent Test:** Can be tested by simulating authenticator loss and completing recovery flow with secondary authenticator or escrow retrieval.

---

### US-004: Delegated Signing Authority
**As an** organisation administrator  
**I want to** delegate signing authority to team members  
**So that** they can sign on behalf of the organisation within defined bounds

**Acceptance Criteria:**
- [ ] Admin can create delegation credential with scope constraints
- [ ] Delegate can prove authority via ZKP without revealing full delegation chain
- [ ] Delegations can be revoked
- [ ] All delegated signatures are traceable to source authority

**Independent Test:** Can be tested by having a delegate sign a Blueprint and verifying both the signature validity and the delegation proof.

---

## User Scenarios & Testing

### Scenario 1: First-Time Setup
**Preconditions:** User has Sorcha account, owns FIDO2 authenticator  
**Steps:**
1. User navigates to Wallet Settings
2. User clicks "Enable Hardware-Protected Signing"
3. System initiates WebAuthn registration ceremony
4. User touches authenticator
5. System derives child key from custodial wallet
6. System encrypts child key with PRF-derived KEK
7. Encrypted key blob stored in browser IndexedDB

**Expected Outcome:** User can now sign Blueprints with hardware protection

**Edge Cases:**
- Authenticator doesn't support PRF extension → Fallback to password-based encryption
- User cancels WebAuthn ceremony → Setup aborted, no partial state
- Browser doesn't support WebAuthn → Clear error message with browser requirements

---

### Scenario 2: Signing a Blueprint
**Preconditions:** User has completed setup, has pending Blueprint requiring signature  
**Steps:**
1. User views Blueprint details
2. User clicks "Sign Blueprint"
3. WASM island loads in Blazor page
4. System retrieves encrypted key from IndexedDB
5. WebAuthn ceremony triggers PRF output
6. Key decrypted in Web Crypto subsystem (non-extractable)
7. Blueprint hash signed
8. Signature submitted to Wallet Service

**Expected Outcome:** Blueprint marked as signed, signature verifiable

**Edge Cases:**
- User's authenticator unavailable → Prompt to use backup or recovery
- Network failure after signing → Local queue with retry
- Concurrent signing requests → Serialised to prevent nonce reuse

---

### Scenario 3: Proving Credential Predicate
**Preconditions:** User has verified credential (e.g., identity document)  
**Steps:**
1. Verifier requests proof of "age ≥ 18"
2. User reviews proof request
3. User approves proof generation
4. Browser loads ZKP circuit (WASM)
5. Proof generated (~10-30 seconds with progress indicator)
6. Proof submitted to verifier

**Expected Outcome:** Verifier confirms predicate without learning user's birthdate

**Edge Cases:**
- Credential expired → Proof generation blocked with clear message
- Circuit not available for requested predicate → Error with supported predicates list
- Proof generation timeout → Graceful failure with retry option

---

## Functional Requirements

### Key Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001 | System MUST derive child keys using HD derivation (BIP-32 compatible) from custodial master key | Must Have |
| FR-002 | System MUST encrypt child keys using AES-256-GCM before browser storage | Must Have |
| FR-003 | System MUST support WebAuthn PRF extension for KEK derivation | Must Have |
| FR-004 | System MUST provide PBKDF2 fallback when PRF unavailable | Must Have |
| FR-005 | System MUST mark decrypted keys as non-extractable in Web Crypto API | Must Have |
| FR-006 | System MUST NOT store unencrypted key material in any persistent storage | Must Have |
| FR-007 | System SHOULD support multiple authenticator registration per user | Should Have |

### Signing Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-010 | System MUST support ECDSA P-256 signatures | Must Have |
| FR-011 | System MUST support EdDSA Ed25519 signatures | Should Have |
| FR-012 | System MUST require user gesture for each signing operation | Must Have |
| FR-013 | System MUST include timestamp and nonce in signed payloads | Must Have |
| FR-014 | System MUST log all signing operations to audit trail | Must Have |

### Zero-Knowledge Proofs

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020 | System MUST support Groth16 proof generation in browser | Must Have |
| FR-021 | System SHOULD support PLONK for universal setup scenarios | Should Have |
| FR-022 | System MUST provide progress indication during proof generation | Must Have |
| FR-023 | System MUST NOT transmit witness data outside browser | Must Have |
| FR-024 | System MUST support credential predicate proofs (equality, range, set membership) | Must Have |
| FR-025 | System SHOULD support composite proofs (multiple predicates) | Should Have |

### Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030 | System MUST integrate with existing Blazor Server pages via WASM island | Must Have |
| FR-031 | System MUST expose signing capability via JS interop | Must Have |
| FR-032 | System MUST NOT require page reload for cryptographic operations | Must Have |
| FR-033 | System MUST maintain session state across signing operations | Must Have |

---

## Data Entities

### EncryptedKeyBlob
Represents a user's encrypted private key stored in browser.
- **id:** Unique identifier (credential ID from WebAuthn)
- **encryptedData:** AES-256-GCM ciphertext
- **iv:** Initialisation vector
- **algorithm:** Key algorithm (ECDSA-P256, EdDSA-Ed25519)
- **derivationPath:** HD derivation path from master
- **createdAt:** Timestamp of key creation
- **publicKey:** Corresponding public key (for verification)

### ProofRequest
Represents a request for a ZKP from a verifier.
- **id:** Unique request identifier
- **verifierId:** Requesting party identifier
- **predicates:** List of predicates to prove
- **challenge:** Verifier-provided challenge (prevents replay)
- **expiresAt:** Request expiration timestamp

### DelegationCredential
Represents delegated signing authority.
- **id:** Credential identifier
- **delegatorId:** Original authority holder
- **delegateId:** Recipient of delegation
- **scope:** Constraints on delegation (Blueprint types, time bounds)
- **proofCircuit:** ZKP circuit for proving delegation

---

## Success Criteria

| ID | Criterion | Measurement |
|----|-----------|-------------|
| SC-001 | Users can complete key setup in under 60 seconds | Time from initiation to completion |
| SC-002 | Signing operations complete in under 3 seconds | Time from user gesture to signature submission |
| SC-003 | ZKP generation completes in under 30 seconds for standard predicates | Browser proof generation time |
| SC-004 | System handles authenticator unavailability gracefully | 100% of failures show actionable recovery path |
| SC-005 | Zero private key exposure in browser memory dumps | Security audit verification |
| SC-006 | 95% of users successfully complete first signing operation | First-attempt success rate |
| SC-007 | System passes FIDO Alliance certification tests | Certification status |

---

## Assumptions

1. Users have access to FIDO2-compatible authenticators (USB keys, platform authenticators)
2. Target browsers support Web Crypto API and WebAuthn (Chrome 116+, Edge, Firefox, Safari 16.4+)
3. Custodial wallet infrastructure exists and can derive child keys on demand
4. Blazor Server infrastructure supports WASM islands (InteractiveAuto mode)
5. Users accept ~10-30 second wait times for ZKP generation

---

## Dependencies

| Dependency | Type | Status |
|------------|------|--------|
| Sorcha Wallet Service (server-side) | Internal | Existing |
| Blazor Server infrastructure | Internal | Existing |
| WebAuthn PRF browser support | External | Available (Chrome 116+) |
| snarkjs WASM build | External | Available |
| fido2-net-lib | External | Available (v4.0.0) |

---

## Out of Scope

- Mobile native app implementations (future feature)
- Custom authenticator firmware
- Post-quantum cryptographic algorithms
- Multi-party computation for threshold signatures
- Blockchain/DLT integration (handled by separate Peer Service)

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| WebAuthn PRF not supported in user's browser | Medium | High | Implement PBKDF2 fallback with clear UX |
| ZKP proof generation too slow for users | Medium | Medium | Progress indicators, background generation, circuit optimisation |
| Authenticator loss leads to key loss | Low | High | Multiple authenticator support, optional server escrow |
| XSS attack accesses signing capability | Low | Critical | CSP headers, non-extractable keys, user gesture requirement |

---

## Review & Acceptance Checklist

### Content Quality
- [ ] No implementation details (languages, frameworks, APIs) in requirements
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous
- [ ] Success criteria are measurable
- [ ] Success criteria are technology-agnostic
- [ ] All acceptance scenarios defined
- [ ] Edge cases identified
- [ ] Scope clearly bounded
- [ ] Dependencies and assumptions identified

### Feature Readiness
- [ ] All functional requirements have priority
- [ ] User stories have acceptance criteria
- [ ] Risks documented with mitigations
- [ ] Out of scope items listed

---

## Appendix: Glossary

| Term | Definition |
|------|------------|
| **KEK** | Key Encryption Key - used to encrypt/decrypt other keys |
| **PRF** | Pseudo-Random Function - WebAuthn extension for deriving secrets |
| **QSCD** | Qualified Signature Creation Device - eIDAS hardware requirement |
| **ZKP** | Zero-Knowledge Proof - cryptographic proof revealing nothing beyond statement validity |
| **HD Derivation** | Hierarchical Deterministic key derivation (BIP-32) |
| **Non-extractable** | Web Crypto key property preventing export of key material |
| **Witness** | Private inputs to a ZKP circuit |
