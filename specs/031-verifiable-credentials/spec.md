# Feature Specification: Verifiable Credentials & eIDAS-Aligned Attestation System

**Feature Branch**: `031-verifiable-credentials`
**Created**: 2026-02-12
**Status**: Draft
**Input**: User description: "Add a composable credential system to Sorcha blueprints where actions can require credentials as entry gates, blueprints can issue credentials as outputs, and credential flows compose across blueprints. Uses SD-JWT VC format aligned with eIDAS 2.0 Architecture Reference Framework."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gate a Blueprint Action on a Credential (Priority: P1)

A blueprint designer creates a workflow where a specific action requires participants to present a verifiable credential before they can execute it. For example, a "Submit Work Order" action requires the submitter to hold a valid "Electrical License" credential. When a participant attempts the action, the system checks that they possess a matching, unexpired, unrevoked credential signed by a recognized issuer. If the credential is valid, the action proceeds normally. If not, the participant is informed which credential is missing and cannot proceed.

**Why this priority**: This is the foundational capability — without credential-gated actions, none of the other features matter. It enables the core "prove once, use everywhere" pattern and transforms blueprints from open workflows into trust-aware workflows.

**Independent Test**: Can be fully tested by creating a blueprint with a credential requirement on an action, then attempting execution with and without a valid credential. Delivers immediate value by restricting sensitive workflow steps to credentialed participants.

**Acceptance Scenarios**:

1. **Given** a blueprint with an action requiring a "LicenseCredential" from a specific issuer, **When** a participant presents a valid, unexpired credential matching all requirements, **Then** the action executes successfully and the credential details are recorded alongside the transaction.
2. **Given** a blueprint with a credential-gated action, **When** a participant attempts the action without any credential, **Then** the system rejects the action with a clear message listing the missing credential requirements.
3. **Given** a blueprint with a credential-gated action, **When** a participant presents an expired credential, **Then** the system rejects the action with a message indicating the credential has expired.
4. **Given** a blueprint with a credential-gated action, **When** a participant presents a credential from an unrecognized issuer, **Then** the system rejects the action with a message indicating the issuer is not accepted.
5. **Given** a blueprint with a credential-gated action, **When** a participant presents a credential that has been revoked, **Then** the system rejects the action with a message indicating the credential has been revoked.

---

### User Story 2 - Issue a Credential from a Blueprint Flow (Priority: P2)

An authority (e.g., a licensing body) runs a blueprint flow where an individual proves their identity and a skill certifier attests to their competence. When the authority participant executes the final approval action, the system mints a new verifiable credential (in SD-JWT VC format) signed by the authority's wallet key. The credential contains claims defined by the blueprint's output schema and is delivered to the recipient participant. The issuance event is recorded on the ledger as proof of issuance.

**Why this priority**: This completes the credential lifecycle — without issuance, there are no credentials to gate on. Combined with P1, it enables the full "composable credential flow" pattern where one blueprint's output feeds into another's input.

**Independent Test**: Can be fully tested by running a multi-step approval blueprint to completion and verifying that a valid credential is produced, properly signed, and contains the expected claims. The credential can then be independently verified against the issuer's public key.

**Acceptance Scenarios**:

1. **Given** a blueprint with a credential-issuing final action and an authority participant, **When** the authority executes the final approval action, **Then** a new credential is minted in SD-JWT VC format, signed by the authority's wallet key, and delivered to the recipient.
2. **Given** a credential-issuing action, **When** the credential is minted, **Then** the issuance event (credential identifier, issuer, recipient, claim summary) is recorded on the immutable ledger.
3. **Given** a minted credential, **When** a third party verifies it against the issuer's public key, **Then** the signature validates successfully and the claims are readable.
4. **Given** a credential-issuing action with a defined output schema, **When** the credential is minted, **Then** the credential's claims conform to the schema and include only the fields defined by the blueprint designer.

---

### User Story 3 - Compose Credential Flows Across Blueprints (Priority: P2)

An individual completes a license approval blueprint (Story 2) and receives a license credential. They then start a separate "Work Order" blueprint that requires this license credential (Story 1). The individual presents the credential issued by the first flow to satisfy the entry requirement of the second flow. The system verifies the credential's validity, checks that it was issued by a recognized authority, and allows the individual to proceed — without the second blueprint needing to know anything about the individual's identity beyond what the credential attests.

**Why this priority**: This is the composability story — the reason the credential system exists. It validates the end-to-end pattern and proves that blueprints can form trust chains.

**Independent Test**: Can be fully tested by running two blueprints end-to-end: first to issue a credential, then to consume it. Delivers the "prove once, use many" value proposition.

**Acceptance Scenarios**:

1. **Given** a credential issued by Blueprint A, **When** the holder presents it to satisfy a credential requirement in Blueprint B, **Then** Blueprint B accepts the credential and allows the action to proceed.
2. **Given** a credential issued by Blueprint A that has since been revoked, **When** the holder presents it to Blueprint B, **Then** Blueprint B rejects the credential and the action is denied.
3. **Given** a credential issued by Blueprint A with multiple claims, **When** Blueprint B only requires a subset of those claims, **Then** the holder can present only the required claims via selective disclosure, and Blueprint B accepts the presentation.

---

### User Story 4 - Selective Disclosure of Credential Claims (Priority: P3)

A credential holder has a license credential containing their name, license number, license type, skill level, and expiry date. A downstream blueprint only requires proof that the holder has a valid license of a specific type — it does not need the holder's name or license number. The holder presents the credential using selective disclosure, revealing only the license type and expiry while keeping other claims hidden. The verifying blueprint confirms the revealed claims satisfy the requirement without learning any additional information.

**Why this priority**: Selective disclosure is a privacy-enhancing feature that makes the credential system suitable for real-world use. SD-JWT VC format supports this natively, so it comes at low incremental cost once the base credential format is implemented.

**Independent Test**: Can be fully tested by issuing a credential with multiple claims, then presenting it with only a subset disclosed, and verifying that the verification succeeds while the undisclosed claims remain hidden.

**Acceptance Scenarios**:

1. **Given** a credential with 5 claims and a blueprint action requiring only 2 specific claims, **When** the holder presents the credential with selective disclosure, **Then** only the 2 required claims are revealed to the verifier and the action succeeds.
2. **Given** a credential presentation with selective disclosure, **When** the verifier examines the presented data, **Then** non-disclosed claims are cryptographically hidden and cannot be inferred.
3. **Given** a blueprint action requiring claim "licenseType" with value "electrical", **When** the holder presents a credential disclosing "licenseType" as "plumbing", **Then** the action is rejected because the claim value does not match the requirement.

---

### User Story 5 - Revoke a Previously Issued Credential (Priority: P3)

An authority that previously issued a license credential needs to revoke it (e.g., the license holder violated regulations). The authority initiates a revocation action within the original issuing blueprint (or a dedicated revocation blueprint). The revocation is recorded on the ledger. Any subsequent attempt to use the revoked credential in any blueprint flow is rejected.

**Why this priority**: Revocation completes the credential lifecycle and is essential for real-world trust. Without it, credentials are irrevocable once issued, which is unacceptable for regulated use cases.

**Independent Test**: Can be fully tested by issuing a credential, verifying it works, revoking it, then verifying it is rejected.

**Acceptance Scenarios**:

1. **Given** a previously issued credential, **When** the issuing authority executes a revocation action, **Then** the revocation is recorded on the ledger with a timestamp and reason.
2. **Given** a revoked credential, **When** any participant presents it to any blueprint's credential-gated action, **Then** the action is rejected with a message indicating the credential has been revoked.
3. **Given** a revoked credential, **When** the revocation is queried, **Then** the system returns the revocation timestamp, reason, and the identity of the revoking authority.

---

### Edge Cases

- What happens when a credential requirement references an issuer whose wallet address is not registered in the network? The system should reject the blueprint at publish time with a clear validation error.
- What happens when a credential expires mid-workflow (valid at action start, expired by the time the action completes)? The credential validity is checked at the moment of action execution; mid-action expiry does not retroactively invalidate completed actions.
- What happens when a blueprint requires multiple credentials from different issuers for the same action? All credential requirements must be satisfied — they are combined with AND logic by default.
- What happens when a credential's issuer key is rotated after issuance? Credentials remain valid as long as the signing key was valid at the time of issuance. Key rotation does not invalidate previously-issued credentials.
- What happens when the same credential is presented to multiple actions within the same workflow? The credential is verified independently at each action — there is no "single-use" constraint unless the blueprint designer explicitly configures one.
- What happens when a blueprint designer specifies contradictory credential requirements (e.g., requires a credential type that no issuer can produce)? The system should warn at blueprint publish time but not prevent publishing, as issuers may register later.
- What happens when the revocation registry is unreachable and the credential requirement is configured as fail-closed? The action is blocked and the participant receives a message explaining the temporary unavailability; they may retry when the registry is accessible.
- What happens when the revocation registry is unreachable and the credential requirement is configured as fail-open? The action proceeds but an audit warning is recorded on the ledger, and the credential is flagged as "revocation status unverified" in the transaction record.

## Requirements *(mandatory)*

### Functional Requirements

#### Credential Data Model

- **FR-001**: System MUST support a credential format that includes issuer identity, subject identity, claims (key-value pairs), issuance timestamp, expiry timestamp, and a cryptographic signature.
- **FR-002**: System MUST support selective disclosure, allowing credential holders to reveal only a subset of claims when presenting a credential.
- **FR-003**: System MUST record credential issuance events on the immutable ledger, including the credential identifier, issuer, recipient, and claim summary.
- **FR-004**: Each credential MUST have a unique identifier that can be referenced for verification and revocation.

#### Credential Requirements on Blueprint Actions

- **FR-005**: Blueprint actions MUST support an optional list of credential requirements, each specifying the credential type, accepted issuer(s), required claims, and claim value constraints.
- **FR-006**: When a credential-gated action is executed, the system MUST verify all credential requirements are satisfied before allowing the action to proceed.
- **FR-007**: Credential verification MUST check: (a) signature validity, (b) expiry status, (c) revocation status, and (d) claim matching against requirements.
- **FR-008**: When credential verification fails, the system MUST return specific, actionable error messages indicating which requirement failed and why.
- **FR-009**: Multiple credential requirements on a single action MUST be combined with AND logic — all requirements must be satisfied.
- **FR-009a**: When a participant navigates to a credential-gated action, the system MUST automatically check their stored credentials against the action's requirements and present matching credentials for the participant to confirm or select.
- **FR-009b**: When no matching credentials are found in the participant's wallet, the system MUST display the unmet requirements with clear descriptions of what credential is needed and from which issuer(s).
- **FR-010**: Blueprint designers MUST be able to specify credential requirements using the blueprint JSON format, with corresponding support in the fluent builder API.

#### Credential Issuance

- **FR-011**: Blueprint actions MUST support an optional credential issuance configuration that defines the credential type, claim schema, recipient, and expiry rules.
- **FR-012**: When a credential-issuing action is executed, the system MUST mint a new credential signed by the executing participant's wallet key.
- **FR-013**: Issued credentials MUST conform to the claim schema defined in the blueprint action's issuance configuration.
- **FR-014**: The system MUST deliver issued credentials to the recipient participant via the existing notification/delivery mechanisms.
- **FR-014a**: The system MUST store issued credentials in the recipient's wallet for discovery and presentation in future actions.
- **FR-014b**: The system MUST support exporting credentials as portable, self-contained tokens for use outside the Sorcha platform.
- **FR-014c**: The system MUST support recording issued credentials on a register (e.g., a "Register of Licenses") maintained by the issuing authority, enabling queryable registries of issued credentials.

#### Credential Revocation

- **FR-015**: The system MUST support credential revocation by the original issuing authority.
- **FR-016**: Revocation MUST be recorded on the immutable ledger with a timestamp and optional reason.
- **FR-017**: Credential verification (FR-007) MUST check revocation status as part of every verification.
- **FR-017a**: Each credential requirement MUST include a configurable revocation check policy: fail-closed (block action until revocation status is confirmed) or fail-open (action proceeds with an audit warning if revocation status cannot be determined).
- **FR-017b**: When fail-open is triggered, the system MUST record an audit warning on the ledger indicating the credential's revocation status was unverified at the time of action execution.

#### Composability

- **FR-018**: Credentials issued by one blueprint flow MUST be usable as input credentials in any other blueprint flow, provided the credential type and issuer match the requirement.
- **FR-019**: The system MUST NOT require the verifying blueprint to have any direct relationship with the issuing blueprint — verification is based solely on credential content and issuer signature.

#### Blueprint Validation

- **FR-020**: When a blueprint is published, the system MUST validate that credential requirement definitions are well-formed (valid types, valid claim constraints, valid issuer references).

### Key Entities

- **VerifiableCredential**: A cryptographically signed attestation containing an issuer identity, subject identity, a set of claims, issuance and expiry timestamps, and a unique identifier. Can be presented with selective disclosure.
- **CredentialRequirement**: A specification on a blueprint action defining what credential(s) a participant must present — includes credential type, accepted issuers, required claims, claim value constraints, and a revocation check policy (fail-closed or fail-open).
- **CredentialIssuanceConfig**: A specification on a blueprint action defining what credential to mint upon execution — includes credential type, claim schema (derived from action data), recipient, and expiry rules.
- **CredentialRevocation**: A ledger record indicating that a specific credential has been revoked by its issuer, with timestamp and reason.
- **CredentialPresentation**: A holder's submission of one or more credentials (potentially with selective disclosure) to satisfy an action's credential requirements.
- **CredentialRegister**: An issuer-maintained register (using Sorcha's existing register infrastructure) that records all credentials issued by that authority, enabling public or authorized queryability (e.g., a "Register of Licenses").

## Clarifications

### Session 2026-02-12

- Q: Where are issued credentials stored between issuance and presentation? → A: Hybrid — wallet service stores credentials for holder convenience, credentials are exportable as portable tokens for use outside Sorcha, and issuance can also be recorded on a register (e.g., a "Register of Licenses") maintained by the issuing authority for public/authority queryability.
- Q: How does credential presentation work at action time? → A: Auto-match — system checks the participant's stored credentials against the action's requirements and presents matching credentials for the participant to confirm/select.
- Q: What happens if the revocation registry is unreachable during credential verification? → A: Configurable per credential requirement — blueprint designers choose between fail-closed (block action until revocation status confirmed) or fail-open with warning (action proceeds but credential flagged as "revocation status unverified" with audit warning).

## Assumptions

- **SD-JWT VC format**: Credentials will use the SD-JWT VC format as mandated by the eIDAS 2.0 Architecture Reference Framework. This provides selective disclosure natively without requiring full zero-knowledge proof infrastructure.
- **Wallet-based signing**: Credential issuance uses existing wallet cryptographic keys (ED25519, P-256, or RSA-4096 as already supported by Sorcha.Cryptography). No new key types are required.
- **Ledger as revocation registry**: Revocation status is checked against the existing immutable ledger rather than introducing a separate revocation service. This aligns with the DAD security model.
- **OID4VCI/OID4VP patterns**: Issuance and presentation protocols follow OID4VCI and OID4VP patterns respectively, but adapted for Sorcha's participant/action model rather than implementing full OAuth-based flows.
- **Derived wallets are out of scope**: Per-context unlinkable wallet addresses are deferred to a future feature. Participants use their primary wallet address for credential interactions in this phase.
- **Full ZKP is out of scope**: Advanced zero-knowledge proofs (range proofs, predicate proofs) beyond SD-JWT selective disclosure are deferred to a future feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A blueprint designer can add credential requirements to any action and the system correctly gates execution — 100% of invalid credential presentations are rejected, 100% of valid presentations are accepted.
- **SC-002**: A credential issued by one blueprint flow can be successfully verified and accepted by a different blueprint flow within 2 seconds of presentation.
- **SC-003**: Credential revocation takes effect within 1 minute — a revoked credential is rejected on the next verification attempt after revocation is recorded.
- **SC-004**: Selective disclosure works correctly — a credential with N claims can be presented with any subset of claims, and the verifier learns nothing about undisclosed claims.
- **SC-005**: All credential operations (issuance, verification, revocation) produce auditable ledger records that can be independently verified.
- **SC-006**: Blueprint designers can configure credential requirements and issuance without writing code — using the JSON blueprint format or the visual designer.
- **SC-007**: The credential format is interoperable — credentials issued by Sorcha can be independently verified by any system supporting the same credential format standard.
