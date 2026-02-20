# Deferred Tasks

**These tasks are not required for MVD and will be addressed post-launch.**

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)

---

## Peer Service Transaction Processing

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| PEER-1 | Transaction processing loop | P3 | 12h | 📋 Deferred | Sprint 4 originally planned |
| PEER-2 | Transaction distribution | P3 | 10h | 📋 Deferred | P2P gossip protocol |
| PEER-3 | Streaming communication | P3 | 8h | 📋 Deferred | gRPC streaming |

---

## Tenant Service Full Implementation

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| TENANT-1 | Multi-tenant data isolation | P3 | 16h | 📋 Deferred | Use simple provider for MVD |
| TENANT-2 | Azure AD integration | P3 | 12h | 📋 Deferred | Full identity federation |
| TENANT-3 | Billing and metering | P3 | 20h | 📋 Deferred | Enterprise feature |

---

## Advanced Features

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| ADV-1 | Smart contract support | P3 | 40h | 📋 Deferred | Future roadmap |
| ADV-2 | Advanced consensus | P3 | 32h | 📋 Deferred | Beyond simple Register |
| ADV-3 | External SDK development | P3 | 24h | 📋 Deferred | Developer ecosystem |
| ADV-4 | Blueprint marketplace | P3 | 30h | 📋 Deferred | Community feature |

---

## Authentication & Session Hardening

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| AUTH-H1 | Refresh token rotation | P2 | 8h | 📋 Deferred | Issue new refresh token on each refresh — limits replay window |
| AUTH-H2 | Cross-tab token synchronization | P2 | 6h | 📋 Deferred | localStorage event listener to sync token state across browser tabs |
| AUTH-H3 | Session expiry warning UI | P3 | 4h | 📋 Deferred | Toast/dialog warning user before session expires, "Extend Session" button |
| AUTH-H4 | Sliding window refresh token extension | P3 | 6h | 📋 Deferred | Extend refresh token TTL on activity — avoids hard 24h logout for active users |

---

## Register Governance — Future Enhancements

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| GOV-1 | ZKP-based admin credentials via register DIDs | P4 | 40h | 📋 Deferred | IDIDResolver interface designed for extensibility; requires ZKP library integration |
| GOV-2 | Social recovery for lost Owner wallet access | P4 | 24h | 📋 Deferred | Multi-party recovery blueprints or ZKP-based recovery; currently register becomes unmodifiable |
| GOV-3 | Concurrent governance proposals | P3 | 16h | 📋 Deferred | Current: single proposal at a time (implicit queueing via blueprint loop); future: multi-instance or queue-based |
| GOV-4 | Enhanced DID resolution with retry & fallback | P3 | 12h | 📋 Deferred | Retry with exponential backoff, consensus-based fallback for unreachable registers |
| GOV-5 | Deadlock detection for m=2 edge case | P3 | 8h | 📋 Deferred | Automatic detection + alerting when quorum impossible; Owner bypass is current escape hatch |
| GOV-6 | Roster reconstruction caching in Validator | P3 | 6h | 📋 Deferred | Cache roster after first reconstruction per register; performance optimization for rights checks |
| GOV-7 | Governance audit trail streaming via SignalR | P3 | 12h | 📋 Deferred | Real-time audit event streaming; immutable audit trail as separate transactions |
| GOV-8 | Roster member limit increase (>25) | P4 | 4h | 📋 Deferred | Current cap: 25 members; increase based on real-world needs + performance testing |
| GOV-9 | Control TX payload versioning strategy | P3 | 8h | 📋 Deferred | ControlTransactionPayload.Version field exists but migration strategy for future versions not documented |
| GOV-10 | Multi-tenant governance policies | P4 | 16h | 📋 Deferred | Cross-tenant constraints (e.g., block admins from competing tenants); currently per-register only |

---

## Published Participant Records — Phase 2+ (Out of Scope from 001-participant-records)

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| PART-1 | External identity provider (OIDC) integration for participant authentication | P3 | 24h | 📋 Deferred | Enable orgs to link participants to Microsoft, Google, GitHub, Apple IdPs |
| PART-2 | API key management for machine participants | P3 | 16h | 📋 Deferred | Service clients and AI agents authenticating via API keys |
| PART-3 | Blueprint participant resolution by address | P2 | 20h | 📋 Deferred | Phase 2 integration — blueprint participants resolve to wallet addresses instead of text names |
| PART-4 | Field-level encryption using published public keys | P2 | 24h | 📋 Deferred | Phase 2 — encrypt action payload fields for specific participant addresses |
| PART-5 | DID document generation and resolution endpoints | P3 | 16h | 📋 Deferred | Generate W3C DID documents from published participant records |
| PART-6 | Peer-to-peer participant record replication and synchronization | P3 | 20h | 📋 Deferred | Replicate participant indexes across peer nodes |
| PART-7 | UI components for participant management | P2 | 16h | 📋 Deferred | Blazor WASM pages for publishing, updating, revoking, and browsing participants |
| PART-8 | Organization-level wallet signing for participant publication | P2 | 12h | 📋 Deferred | Currently uses individual user wallet; upgrade to org-level signing wallet |
| PART-9 | Migrate participant authorization to register governance/control system | P3 | 16h | 📋 Deferred | Move from Tenant Service enforcement to register Control TX governance roster |

---

## Summary

**Total Deferred Tasks:** 33
**Total Deferred Effort:** 538 hours (~14 weeks)

These tasks represent features that enhance the platform but are not critical for the Minimum Viable Deliverable (MVD). They can be prioritized for post-MVD development based on user feedback and business requirements.

---

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)
