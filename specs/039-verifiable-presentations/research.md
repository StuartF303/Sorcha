# Research: Verifiable Credential Lifecycle & Presentations

**Phase 0 Output** | **Date**: 2026-02-21

All technical unknowns were resolved during the brainstorming/design phase. This document consolidates the research findings and decisions.

## R1: Credential Storage Model

**Decision**: Wallet-centric architecture — credentials stored in wallet, register is audit trail + status list host.

**Rationale**: Aligns with W3C/EUDI direction. Wallet already has 9 credential endpoints. Register serves its natural role as immutable ledger, not real-time query engine. SingleUse enforcement via Bitstring Status List (verifier trusts status list, not wallet).

**Alternatives considered**:
- Register-centric (every presentation = TX): Too slow, not offline-capable, diverges from W3C
- Hybrid service layer (new Credential Service): Adds infrastructure, duplicates wallet functionality

## R2: DID Method Selection

**Decision**: Ship `did:sorcha`, `did:web`, `did:key` with pluggable `IDidResolver` interface.

**Rationale**: `did:sorcha` for native (existing), `did:web` for organizations with web presence (HTTP-based, easy), `did:key` for ephemeral/offline (no network call). Covers enterprise, demo, and ephemeral scenarios. Pluggable interface allows `did:ethr`, `did:ion`, `did:indy` to be added later without architecture changes.

**Alternatives considered**:
- Pluggable interface only (no implementations beyond did:sorcha): Limits interop story
- Full universal resolver service: Over-engineers for current needs, adds external dependency
- External resolver endpoint: Simplest but requires external infrastructure

**Key technical details**:
- `did:key` uses multicodec encoding — ED25519 prefix `0xed01`, P-256 prefix `0x8024`
- `did:web` resolves via `GET https://{domain}/.well-known/did.json` — HTTPS enforced, 5s timeout
- `did:sorcha:w:` queries wallet service, `did:sorcha:r:` queries register service
- DID Document follows W3C DID Core: `id`, `verificationMethod[]`, `authentication[]`, `assertionMethod[]`

## R3: Revocation/Suspension Standard

**Decision**: W3C Bitstring Status List v1.0 (published May 2025 as W3C Recommendation).

**Rationale**: The industry-standard approach. Privacy-preserving (verifier downloads entire list, can't correlate which credential checked). Space-efficient (131,072 entries ≈ 16KB compressed). Supports both revocation and suspension purposes via separate bitstrings. Cacheable (5 min TTL). Public endpoint — no authentication needed.

**Alternatives considered**:
- Per-credential revocation endpoint: Privacy-violating (issuer sees every check), not scalable
- CRL (Certificate Revocation List): Legacy approach, large downloads, not VC-specific
- OCSP-style online checking: Real-time but privacy-violating, availability dependency

**Key technical details**:
- Two bitstrings per list: `statusPurpose: "revocation"` and `statusPurpose: "suspension"`
- Minimum list size: 131,072 entries (W3C recommended for privacy — larger lists are more private)
- Compression: GZip + Base64 encoding
- Storage: Control TX on register (canonical), Blueprint Service HTTP cache (verifier access)
- Status list is itself a Verifiable Credential, signed by the issuer

## R4: Presentation Protocol

**Decision**: OID4VP (OpenID for Verifiable Presentations) with QR code initiation for in-person flows.

**Rationale**: EUDI/EU standard for credential presentation. Same protocol for remote (HTTPS) and proximity (QR-initiated). QR encodes a URL + nonce (small), not the credential (large). `response_mode=direct_post` — wallet POSTs vp_token to verifier's callback. Nonce binding prevents replay.

**Alternatives considered**:
- Custom presentation protocol: Non-standard, limits interop
- DIF Presentation Exchange v2: More complex format, deferred to future
- ISO 18013-5 proximity (NFC/BLE): Requires native SDK, deferred to future

**Key technical details**:
- QR format: `openid4vp://authorize?request_uri=https://...&nonce=xyz`
- Presentation request TTL: 5 minutes default (configurable)
- Selective disclosure via SD-JWT: holder chooses which claims to reveal
- Verification checks: signature (DID resolution), status list, claim constraints, nonce

## R5: Credential Lifecycle States

**Decision**: Five states — Active, Suspended, Revoked, Expired, Consumed.

**Rationale**: Matches W3C/EBSI lifecycle model. Active is default on issuance. Suspended is reversible (temporary disable). Revoked is permanent (terminal). Expired is time-based (refreshable). Consumed is for single-use/limited-use credentials (terminal, equivalent to revoked on status list).

**Key technical details**:
- Suspension: reversible, issuer reinstates by clearing suspension bit
- Revocation: permanent, terminal state — revocation bit set, cannot be unset
- Expiry: `expiresAt` field checked at verification time, not via status list
- Consumed: triggered by successful presentation of SingleUse/LimitedUse credential — revocation bit set
- Authorization: original issuing wallet + register governance roles (Owner/Admin)

## R6: Usage Policy

**Decision**: Three policies — Reusable, SingleUse, LimitedUse.

**Rationale**: Covers the full spectrum from permanent licenses (Reusable) to event tickets (SingleUse) to limited-use vouchers (LimitedUse). Policy enforced by verifier via status list — after successful presentation of SingleUse credential, issuer flips revocation bit. LimitedUse tracks presentation count in wallet + flips bit when limit reached.

**Key technical details**:
- `UsagePolicy` enum on `CredentialIssuanceConfig`
- `MaxPresentations` (int?) on `CredentialIssuanceConfig` — only for LimitedUse
- `PresentationCount` tracked on `CredentialEntity` in wallet
- Concurrent presentation race condition: first successful verification triggers consumption; second receives "credential consumed"

## R7: Wallet UI Approach

**Decision**: Card-based visual display (Apple Wallet style) with issuer-defined display configuration.

**Rationale**: Visual cards are the established UX pattern for digital credentials (Apple Wallet, Google Pay). Issuer-defined display config means credential type dictates appearance (building permit looks different from event ticket). Default generated from type name when no config provided.

**Key technical details**:
- `CredentialDisplayConfig`: backgroundColor, textColor, icon (MudBlazor icon name), cardLayout (Standard/Compact/Ticket), highlightClaims (claim path → display label)
- Card states: green dot (Active), amber dot (Suspended/Expiring Soon), red dot (Revoked), grey dot (Expired), strikethrough (Consumed)
- Detail view: all claims, disclosure indicators, metadata, usage history, actions
- Presentation request inbox: incoming requests as notifications

## R8: External Credential Import

**Decision**: Manual import of pre-formed SD-JWT VCs via existing store endpoint. Full OID4VCI deferred.

**Rationale**: Enables the wallet to hold credentials from multiple sources without implementing the full OID4VCI client flow. The existing `POST /api/v1/wallets/{address}/credentials` endpoint already accepts raw SD-JWT VCs. Imported credentials are treated identically for display, presentation, and status checking.

**Key technical details**:
- Import validates: SD-JWT structure, issuer DID resolvable, credential not expired
- Status list checks use the `credentialStatus` claim embedded in the imported VC
- Display config: uses defaults (no issuer-defined config available for external credentials)
- OID4VCI client implementation deferred to future phase

## R9: Status List Access Control

**Decision**: Public (unauthenticated) endpoint. Privacy via list size, not access control.

**Rationale**: W3C Bitstring Status List is designed for public access. The privacy property comes from the minimum list size (131,072 entries) — a verifier downloading the list reveals nothing about which specific credential they're checking. Public access is essential for external verifiers who may not have Sorcha accounts.

## R10: Credential Deletion Behavior

**Decision**: Local wallet operation only. Status list unchanged. Issuer notification deferred.

**Rationale**: Deletion is the holder's prerogative — like throwing away a physical card. The status list is the issuer's domain. A holder deleting a credential doesn't affect its validity or revocability. If the credential was exported, it remains valid elsewhere. Issuer notification is a future enhancement.
