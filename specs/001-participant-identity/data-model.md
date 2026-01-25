# Data Model: Participant Identity Registry

**Feature**: 001-participant-identity
**Date**: 2026-01-24

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PUBLIC SCHEMA                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────┐         ┌─────────────────────────┐           │
│  │  LinkedWalletAddress    │         │  WalletLinkChallenge    │           │
│  ├─────────────────────────┤         ├─────────────────────────┤           │
│  │ Id: Guid (PK)           │         │ Id: Guid (PK)           │           │
│  │ ParticipantId: Guid (FK)│         │ ParticipantId: Guid (FK)│           │
│  │ OrganizationId: Guid    │         │ WalletAddress: string   │           │
│  │ WalletAddress: string   │◄────────│ Challenge: string       │           │
│  │ PublicKey: byte[]       │         │ ExpiresAt: DateTimeOffset│          │
│  │ Algorithm: string       │         │ Status: ChallengeStatus │           │
│  │ LinkedAt: DateTimeOffset│         │ CreatedAt: DateTimeOffset│          │
│  │ RevokedAt: DateTimeOffset?│       │ CompletedAt: DateTimeOffset?│       │
│  │ Status: WalletLinkStatus│         └─────────────────────────┘           │
│  └─────────────────────────┘                                               │
│           │                                                                 │
│           │ UNIQUE(WalletAddress) WHERE Status='Active'                    │
│           │                                                                 │
└───────────┼─────────────────────────────────────────────────────────────────┘
            │
            │ FK: ParticipantId
            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ORG_{GUID} SCHEMA                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────┐         ┌─────────────────────────┐           │
│  │  ParticipantIdentity    │         │  ParticipantAuditEntry  │           │
│  ├─────────────────────────┤         ├─────────────────────────┤           │
│  │ Id: Guid (PK)           │────────►│ Id: Guid (PK)           │           │
│  │ UserId: Guid (FK→Tenant)│         │ ParticipantId: Guid (FK)│           │
│  │ OrganizationId: Guid    │         │ Action: string          │           │
│  │ DisplayName: string     │         │ ActorId: string         │           │
│  │ Email: string           │         │ ActorType: string       │           │
│  │ Status: ParticipantStatus│        │ Timestamp: DateTimeOffset│          │
│  │ CreatedAt: DateTimeOffset│        │ OldValues: JsonDocument?│           │
│  │ UpdatedAt: DateTimeOffset│        │ NewValues: JsonDocument?│           │
│  │ DeactivatedAt: DateTimeOffset?│   │ IpAddress: string?      │           │
│  │ SearchVector: tsvector  │         └─────────────────────────┘           │
│  └─────────────────────────┘                                               │
│           │                                                                 │
│           │ UNIQUE(UserId, OrganizationId)                                 │
│           │                                                                 │
└───────────┴─────────────────────────────────────────────────────────────────┘
```

## Entities

### ParticipantIdentity

Represents a user's participant status within an organization. Located in org-specific schema for multi-tenant isolation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique participant identifier |
| UserId | Guid | FK→UserIdentity, NOT NULL | Reference to Tenant Service user |
| OrganizationId | Guid | NOT NULL | Organization this participant belongs to |
| DisplayName | string(256) | NOT NULL | Display name for participant contexts |
| Email | string(256) | NOT NULL | Email (copied from user for search) |
| Status | ParticipantIdentityStatus | NOT NULL | Active, Inactive, Suspended |
| CreatedAt | DateTimeOffset | NOT NULL | Registration timestamp |
| UpdatedAt | DateTimeOffset | NOT NULL | Last modification timestamp |
| DeactivatedAt | DateTimeOffset? | NULL | Deactivation timestamp (soft delete) |
| SearchVector | tsvector | Generated | Full-text search column |

**Indexes**:
- `PK_ParticipantIdentity` on Id
- `UQ_Participant_User_Org` on (UserId, OrganizationId) - one identity per user per org
- `IX_Participant_Org_Status` on (OrganizationId, Status)
- `IX_Participant_Search` GIN on SearchVector

**Constraints**:
- User must exist in Tenant Service
- One participant identity per user per organization

---

### LinkedWalletAddress

Represents a verified link between a participant and a wallet address. Located in public schema for platform-wide uniqueness.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique link identifier |
| ParticipantId | Guid | FK→ParticipantIdentity, NOT NULL | Owning participant |
| OrganizationId | Guid | NOT NULL | Organization (denormalized for queries) |
| WalletAddress | string(256) | NOT NULL | Wallet address (e.g., base58, hex) |
| PublicKey | byte[] | NOT NULL | Public key for signature verification |
| Algorithm | string(50) | NOT NULL | Signing algorithm (ED25519, P-256, RSA-4096) |
| LinkedAt | DateTimeOffset | NOT NULL | Verification timestamp |
| RevokedAt | DateTimeOffset? | NULL | Revocation timestamp |
| Status | WalletLinkStatus | NOT NULL | Active, Revoked |

**Indexes**:
- `PK_LinkedWalletAddress` on Id
- `UQ_Active_WalletAddress` on WalletAddress WHERE Status='Active' (partial unique)
- `IX_WalletLink_Participant` on ParticipantId
- `IX_WalletLink_Address` on WalletAddress

**Constraints**:
- Active wallet address can only be linked to one participant platform-wide
- Revoked links preserved for audit trail
- ParticipantId must reference valid participant

---

### WalletLinkChallenge

Temporary record for wallet verification flow. Located in public schema for cross-org challenge lookup.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Challenge identifier |
| ParticipantId | Guid | FK→ParticipantIdentity, NOT NULL | Participant initiating link |
| WalletAddress | string(256) | NOT NULL | Address being linked |
| Challenge | string(1024) | NOT NULL | Challenge message to sign |
| ExpiresAt | DateTimeOffset | NOT NULL | Challenge expiration (5 min) |
| Status | ChallengeStatus | NOT NULL | Pending, Completed, Expired, Failed |
| CreatedAt | DateTimeOffset | NOT NULL | Challenge creation time |
| CompletedAt | DateTimeOffset? | NULL | Verification completion time |

**Indexes**:
- `PK_WalletLinkChallenge` on Id
- `IX_Challenge_Participant_Status` on (ParticipantId, Status)
- `IX_Challenge_Address_Status` on (WalletAddress, Status)

**Constraints**:
- Challenge expires after 5 minutes
- Only one pending challenge per participant/address combination
- Completed challenges retained for audit

---

### ParticipantAuditEntry

Audit log for participant identity changes. Located in org-specific schema.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Audit entry identifier |
| ParticipantId | Guid | FK→ParticipantIdentity, NOT NULL | Affected participant |
| Action | string(50) | NOT NULL | Action type |
| ActorId | string(256) | NOT NULL | User/service that performed action |
| ActorType | string(20) | NOT NULL | User, Admin, System |
| Timestamp | DateTimeOffset | NOT NULL | Action timestamp |
| OldValues | JsonDocument? | NULL | Previous state (JSON) |
| NewValues | JsonDocument? | NULL | New state (JSON) |
| IpAddress | string(45)? | NULL | Client IP address |

**Indexes**:
- `PK_ParticipantAuditEntry` on Id
- `IX_Audit_Participant_Time` on (ParticipantId, Timestamp DESC)
- `IX_Audit_Actor_Time` on (ActorId, Timestamp DESC)

**Actions**:
- `Created` - Participant registered
- `Updated` - Display name changed
- `WalletLinked` - Wallet address linked
- `WalletRevoked` - Wallet address revoked
- `Activated` - Status changed to Active
- `Deactivated` - Status changed to Inactive
- `Suspended` - Status changed to Suspended

---

## Enumerations

### ParticipantIdentityStatus

```csharp
public enum ParticipantIdentityStatus
{
    Active = 0,      // Can participate in workflows
    Inactive = 1,    // Soft-deleted, preserved for audit
    Suspended = 2    // Temporarily disabled by admin
}
```

### WalletLinkStatus

```csharp
public enum WalletLinkStatus
{
    Active = 0,     // Address actively linked
    Revoked = 1     // Link revoked (soft delete)
}
```

### ChallengeStatus

```csharp
public enum ChallengeStatus
{
    Pending = 0,    // Awaiting signature
    Completed = 1,  // Successfully verified
    Expired = 2,    // Time limit exceeded
    Failed = 3      // Verification failed
}
```

---

## Relationships

### ParticipantIdentity → UserIdentity (Tenant Service)
- **Type**: Many-to-One
- **Constraint**: UserId references UserIdentity in Tenant Service
- **Behavior**: Participant deactivated when user removed from org

### ParticipantIdentity → LinkedWalletAddress
- **Type**: One-to-Many
- **Constraint**: ParticipantId foreign key
- **Behavior**: Cascade delete disabled (preserve audit trail)
- **Limit**: Maximum 10 active addresses per participant

### ParticipantIdentity → ParticipantAuditEntry
- **Type**: One-to-Many
- **Constraint**: ParticipantId foreign key
- **Behavior**: Cascade delete disabled (preserve audit trail)

### ParticipantIdentity → WalletLinkChallenge
- **Type**: One-to-Many
- **Constraint**: ParticipantId foreign key
- **Behavior**: Challenges cleaned up after expiration (background job)

---

## Validation Rules

### ParticipantIdentity

| Field | Rule |
|-------|------|
| DisplayName | Required, 1-256 characters, no leading/trailing whitespace |
| Email | Required, valid email format |
| UserId | Must exist in Tenant Service for the organization |

### LinkedWalletAddress

| Field | Rule |
|-------|------|
| WalletAddress | Required, 1-256 characters, valid address format |
| PublicKey | Required, valid public key for specified algorithm |
| Algorithm | Required, one of: ED25519, P-256, RSA-4096 |

### WalletLinkChallenge

| Field | Rule |
|-------|------|
| Challenge | Required, includes participant ID, address, timestamp, nonce |
| ExpiresAt | Must be within 5 minutes of CreatedAt |

---

## State Transitions

### ParticipantIdentity Status

```
                    ┌─────────┐
        Create ───► │ Active  │ ◄─── Reactivate
                    └────┬────┘
                         │
           ┌─────────────┼─────────────┐
           │             │             │
           ▼             ▼             │
    ┌──────────┐   ┌───────────┐      │
    │ Inactive │   │ Suspended │──────┘
    └──────────┘   └───────────┘
         │               │
         └───────┬───────┘
                 │
          (preserved for
            audit trail)
```

### WalletLinkChallenge Status

```
                    ┌─────────┐
        Create ───► │ Pending │
                    └────┬────┘
                         │
           ┌─────────────┼─────────────┐
           │             │             │
           ▼             ▼             ▼
    ┌───────────┐  ┌─────────┐  ┌────────┐
    │ Completed │  │ Expired │  │ Failed │
    └───────────┘  └─────────┘  └────────┘
         │
         ▼
    LinkedWalletAddress
       created
```

---

## Migration Strategy

1. Add public schema tables first (LinkedWalletAddress, WalletLinkChallenge)
2. Add org schema tables via dynamic migration (ParticipantIdentity, ParticipantAuditEntry)
3. Create indexes
4. Add foreign key constraints
5. Seed any required reference data

**Migration File**: `YYYYMMDDHHMMSS_AddParticipantIdentity.cs`
