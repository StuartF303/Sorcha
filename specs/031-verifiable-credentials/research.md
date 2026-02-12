# Research: Verifiable Credentials & eIDAS-Aligned Attestation System

**Branch**: `031-verifiable-credentials` | **Date**: 2026-02-12

## 1. SD-JWT VC Library Selection

### Decision: Evaluate HeroSD-JWT; fall back to custom implementation

### Rationale
The .NET ecosystem for SD-JWT is immature. RFC 9901 (SD-JWT) was only finalized November 2025, and the SD-JWT VC companion spec is still in draft. No mainstream .NET JWT library (Microsoft.IdentityModel, jose-jwt) supports SD-JWT.

**HeroSD-JWT** is the strongest candidate:
- Targets .NET 8/9/10, MIT license, zero third-party dependencies, AOT compatible
- Supports Ed25519, ECDSA/P-256, RSA — matches Sorcha.Cryptography algorithms exactly
- Key binding (KB-JWT) for proof of possession included
- Decoy digests, nested object disclosure, array element disclosure
- Risk: New library (~2,700 downloads), no visible public source repository

**Fallback**: Build a custom SD-JWT layer on top of Sorcha.Cryptography's existing JWS primitives. The core SD-JWT spec (RFC 9901) adds: disclosure creation (base64url JSON), digest computation (SHA-256), `_sd` claim arrays, and KB-JWT — a bounded implementation effort.

### Alternatives Considered
| Library | Verdict | Reason |
|---------|---------|--------|
| HeroSD-JWT 1.1.7 | **Primary candidate** | .NET 10, zero deps, full RFC 9901 |
| SD-JWT (Lissi) 0.1.0-rc.67 | Rejected | Perpetual prerelease, Newtonsoft dependency |
| WalletFramework.SdJwtLib 2.0.0-rc.315 | Rejected | Heavy dependency chain (full wallet framework) |
| Owf.Sd.Jwt | Rejected | Archived Feb 2025, never released |
| Custom implementation | **Fallback** | Full control, uses existing crypto infrastructure |

## 2. Credential Storage Architecture

### Decision: Hybrid — wallet-managed + portable export + register-backed

### Rationale
Per clarification session: credentials are stored in three layers:
1. **Wallet Service (PostgreSQL)**: Stores credentials for the holder's convenience. Enables auto-match at action time.
2. **Portable Export**: Credentials can be exported as self-contained SD-JWT tokens for use outside Sorcha.
3. **Credential Register**: Issuers can optionally record credentials on a Sorcha register (e.g., "Register of Licenses") for public/authority queryability. Uses existing register infrastructure — no new service needed.

### Alternatives Considered
- Holder-only (no server storage): Rejected — breaks auto-match UX and composability story
- Dedicated credential service: Rejected — adds microservice complexity; wallet service is the natural home for credentials the participant holds; register service is the natural home for issuer registries

## 3. Blueprint Engine Integration Point

### Decision: Insert credential verification as Step 0 in ActionProcessor, before schema validation

### Rationale
The ActionProcessor pipeline runs: Schema Validation → Calculations → Routing → Disclosures. Credential verification is a pre-condition that must pass before data is even examined. Inserting at Step 0:
- Fails fast with clear credential errors before schema validation runs
- Keeps the existing pipeline untouched for non-credentialed actions
- Follows the same pattern as `RequiredActionData` (checked before processing)

The `ExecutionContext` (immutable, init-only) needs a new `CredentialPresentations` property to carry submitted credentials into the engine.

### Alternatives Considered
- Verification in ActionExecutionService (service layer): Rejected — splits validation logic between service and engine; engine should own all action pre-condition checks
- Verification as a schema extension: Rejected — credential verification is cryptographic, not JSON Schema-based

## 4. Credential Data Model Integration

### Decision: New properties on Action model + new model classes in Sorcha.Blueprint.Models

### Rationale
The existing model already has extension points:
- `Participant.VerifiableCredential` (JsonNode) — already exists for participant-level credentials
- `Participant.DidUri` — already exists for DID identifiers
- `Action.Disclosures`, `Action.RejectionConfig` — existing patterns for optional action properties
- `Action.AdditionalProperties` — extensible JSON-LD properties

New additions follow the same patterns:
- `Action.CredentialRequirements` (IEnumerable<CredentialRequirement>)
- `Action.CredentialIssuanceConfig` (CredentialIssuanceConfig?)
- New model classes: `CredentialRequirement`, `CredentialIssuanceConfig`, `CredentialPresentation`, `CredentialRevocation`

### Alternatives Considered
- Storing credential requirements in `AdditionalProperties`: Rejected — too loosely typed; credential requirements need explicit model for validation and tooling
- Separate credential schema files: Rejected — credential requirements are part of the action definition, not external

## 5. DID URI Format for Credentials

### Decision: `did:sorcha:credential:{credentialId}`

### Rationale
Sorcha already uses DID URIs: `did:sorcha:register:{registerId}/tx/{txId}`. Extending to `did:sorcha:credential:{id}` follows the same pattern and enables:
- Unique credential identification across the platform
- Cross-referencing between credentials, transactions, and registers
- Standard DID resolution patterns

## 6. Revocation Check Strategy

### Decision: Configurable per credential requirement (fail-closed / fail-open)

### Rationale
Per clarification session: blueprint designers choose the policy. Revocation status is checked against the register/ledger. If the ledger is unreachable:
- **Fail-closed** (default for high-security): Block action, participant retries later
- **Fail-open**: Action proceeds with audit warning recorded on the ledger

Implementation: Add `RevocationCheckPolicy` enum property to `CredentialRequirement`.

## 7. Signing Algorithm for Credentials

### Decision: Use issuer's existing wallet key algorithm (Ed25519, P-256, or RSA-4096)

### Rationale
Sorcha.Cryptography already supports all three algorithms. The wallet service already has signing endpoints. Credential issuance reuses the same signing infrastructure — no new crypto required. The SD-JWT format is algorithm-agnostic (uses JWS `alg` header).

## 8. Credential Presentation UX

### Decision: Auto-match from wallet-stored credentials

### Rationale
Per clarification session: when a participant approaches a credential-gated action, the system automatically:
1. Queries the participant's stored credentials in the wallet service
2. Matches against the action's credential requirements (type, issuer, claims)
3. Presents matching credentials for the participant to confirm/select
4. If no match found, displays unmet requirements with descriptions

This requires a new credential matching service in the wallet service and a UI component for credential selection.
