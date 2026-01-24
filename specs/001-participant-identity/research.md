# Research: Participant Identity Registry

**Feature**: 001-participant-identity
**Date**: 2026-01-24
**Purpose**: Resolve technical decisions and document best practices for implementation

## Research Areas

### 1. Wallet Signature Verification Pattern

**Question**: How should we verify wallet ownership during the linking process?

**Decision**: Use challenge-response pattern with Wallet Service signature verification

**Rationale**:
- Wallet Service already exposes `SignTransactionAsync` for signing arbitrary data
- Challenge message should include: participant ID, wallet address, timestamp, nonce
- Verification uses public key returned from wallet to validate signature
- Time-limited challenges (5 minutes) prevent replay attacks

**Implementation Pattern**:
```csharp
// 1. Generate challenge
var challenge = new WalletLinkChallenge
{
    Id = Guid.NewGuid(),
    ParticipantId = participantId,
    WalletAddress = address,
    Challenge = GenerateChallengeMessage(participantId, address),
    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
    Status = ChallengeStatus.Pending
};

// 2. User signs challenge via Wallet Service
// 3. Verify signature
var isValid = await _walletServiceClient.VerifySignatureAsync(
    address, challenge.Challenge, providedSignature);
```

**Alternatives Considered**:
- Direct wallet access: Rejected - violates service boundaries
- External signing service: Rejected - unnecessary complexity

---

### 2. Multi-Tenant Data Isolation

**Question**: How should participant data be isolated across organizations?

**Decision**: Use per-organization PostgreSQL schema (existing TenantDbContext pattern)

**Rationale**:
- Tenant Service already implements `org_{organizationId}` schema pattern
- ParticipantIdentity table in org-specific schema
- LinkedWalletAddress in public schema (platform-wide uniqueness constraint)
- WalletLinkChallenge in public schema (cross-org lookup for transfer verification)

**Schema Layout**:
```sql
-- Public schema (shared)
public.linked_wallet_addresses    -- Global uniqueness enforcement
public.wallet_link_challenges     -- Temporary verification records

-- Per-org schema
org_{guid}.participant_identities -- Org-scoped participant records
org_{guid}.participant_audit_log  -- Org-scoped audit trail
```

**Alternatives Considered**:
- Single schema with org_id column: Rejected - less isolation, complex queries
- Separate database per org: Rejected - operational overhead

---

### 3. Wallet Address Uniqueness Enforcement

**Question**: How to enforce platform-wide wallet address uniqueness while supporting org-scoped participants?

**Decision**: LinkedWalletAddress table in public schema with unique constraint on address

**Rationale**:
- Wallet addresses are globally unique (cryptographic property)
- A single address should only authenticate one participant at a time
- Transfer requires explicit unlink from source org before link to target org
- Soft-delete preserves history while releasing address for new linking

**Database Design**:
```sql
CREATE TABLE public.linked_wallet_addresses (
    id UUID PRIMARY KEY,
    participant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    wallet_address VARCHAR(256) NOT NULL,
    public_key BYTEA NOT NULL,
    linked_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    status VARCHAR(20) NOT NULL, -- Active, Revoked
    CONSTRAINT uq_active_wallet_address
        UNIQUE (wallet_address) WHERE status = 'Active'
);
```

**Alternatives Considered**:
- Allow same address in multiple orgs: Rejected - clarification specified transfer model
- Wallet-per-org restriction: Rejected - overly restrictive for legitimate use cases

---

### 4. Search Performance Strategy

**Question**: How to achieve <2s search across 10,000 participants?

**Decision**: PostgreSQL full-text search with GIN indexes + materialized view for cross-org admin search

**Rationale**:
- Display name and email are primary search targets
- GIN index on tsvector column for full-text search
- B-tree indexes on organization_id, status for filtering
- Cross-org admin search uses materialized view refreshed periodically

**Index Strategy**:
```sql
-- Full-text search
CREATE INDEX idx_participant_search
ON participant_identities USING GIN (to_tsvector('english', display_name || ' ' || email));

-- Filter indexes
CREATE INDEX idx_participant_org_status
ON participant_identities (organization_id, status);

-- Wallet address lookup (public schema)
CREATE INDEX idx_wallet_address
ON linked_wallet_addresses (wallet_address) WHERE status = 'Active';
```

**Alternatives Considered**:
- External search service (Elasticsearch): Rejected - overkill for current scale
- In-memory caching: Rejected - complexity, stale data concerns

---

### 5. Audit Logging Pattern

**Question**: How should participant identity changes be logged?

**Decision**: Dedicated audit table per org schema + structured logging

**Rationale**:
- Follows existing TenantDbContext audit pattern
- Captures: action, actor, timestamp, old/new values, IP address
- Queryable for compliance and forensics
- Also emits structured logs for OpenTelemetry aggregation

**Audit Entry Structure**:
```csharp
public class ParticipantAuditEntry
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public string Action { get; set; } // Created, Updated, WalletLinked, WalletRevoked, Deactivated
    public string ActorId { get; set; }
    public string ActorType { get; set; } // User, Admin, System
    public DateTimeOffset Timestamp { get; set; }
    public JsonDocument? OldValues { get; set; }
    public JsonDocument? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
```

**Alternatives Considered**:
- Event sourcing: Rejected - complexity not justified for this domain
- Append-only audit stream: Considered but table approach simpler for queries

---

### 6. Service Client Integration

**Question**: How should other services look up participant information?

**Decision**: Add IParticipantServiceClient to Sorcha.ServiceClients

**Rationale**:
- Follows established pattern (IWalletServiceClient, IBlueprintServiceClient)
- Blueprint Service needs participant validation during workflow execution
- Register Service needs participant lookup for signature verification
- Cached responses where appropriate (participant identity is relatively stable)

**Client Interface**:
```csharp
public interface IParticipantServiceClient
{
    Task<ParticipantResponse?> GetByIdAsync(Guid participantId, CancellationToken ct);
    Task<ParticipantResponse?> GetByWalletAddressAsync(string address, CancellationToken ct);
    Task<IReadOnlyList<ParticipantResponse>> SearchAsync(ParticipantSearchCriteria criteria, CancellationToken ct);
    Task<bool> ValidateSigningCapabilityAsync(Guid participantId, CancellationToken ct);
}
```

**Alternatives Considered**:
- gRPC client: Rejected - REST is consistent with other service clients
- Direct database access: Rejected - violates service boundaries

---

### 7. UI Component Architecture

**Question**: How should the UI components be structured?

**Decision**: MudBlazor components in Sorcha.UI.Core with service layer abstraction

**Rationale**:
- Follows existing pattern (OrganizationForm, UserList, etc.)
- MudBlazor provides Material Design consistency
- ParticipantApiService wraps HTTP calls
- Components reusable in both Admin and main UI contexts

**Component Hierarchy**:
```
ParticipantList.razor        - MudDataGrid with search/filter
  └── ParticipantSearch.razor - Search input with debounce
  └── ParticipantForm.razor   - Create/edit dialog
      └── WalletLinkDialog.razor - Signature verification flow
```

**Alternatives Considered**:
- Separate component libraries: Rejected - unnecessary separation
- Custom components: Rejected - MudBlazor provides everything needed

---

## Best Practices Applied

### From Sorcha Constitution

1. **Microservices-First**: Extend Tenant Service rather than create new service
2. **Security First**: Challenge-response for wallet verification, org-scoped access
3. **API Documentation**: Scalar with comprehensive XML docs
4. **Testing**: Unit tests for service/repository, integration tests for endpoints
5. **Code Quality**: Async throughout, nullable enabled, no warnings
6. **Observability**: Structured logging, audit trail, health checks

### From Existing Codebase

1. **Repository Pattern**: Follow IdentityRepository structure
2. **Endpoint Pattern**: Follow WalletEndpoints with TypedResults
3. **Service Extension**: Follow WalletServiceExtensions pattern
4. **Multi-Tenant**: Follow TenantDbContext schema pattern
5. **DTOs**: Records for immutability, clear request/response separation

---

## Unresolved Items

None - all technical decisions resolved.

## Next Steps

1. Generate data-model.md with entity definitions
2. Generate API contracts (OpenAPI)
3. Generate quickstart.md for developer onboarding
