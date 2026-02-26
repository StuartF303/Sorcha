# Phase 1 Data Model: UI & CLI Modernization

**Feature**: 043-ui-cli-modernization | **Date**: 2026-02-26

## Entities

### 1. ActivityEvent (Blueprint Service — PostgreSQL)

Represents a user or system event captured for the activity log.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique event identifier |
| OrganizationId | Guid | Required, indexed | Organization scope |
| UserId | Guid | Required, indexed | User who triggered or is target of event |
| EventType | string | Required, max 100 | Category: `WalletCreated`, `TransactionSubmitted`, `BlueprintPublished`, `ActionAssigned`, `ActionConfirmed`, etc. |
| Severity | EventSeverity | Required | `Info`, `Warning`, `Error`, `Success` |
| Title | string | Required, max 200 | Short human-readable title |
| Message | string | Required, max 2000 | Detailed event description |
| SourceService | string | Required, max 50 | Originating service: `Blueprint`, `Wallet`, `Register`, `Tenant`, `Validator` |
| EntityId | string? | Max 200 | Related entity ID (walletAddress, blueprintId, transactionId) |
| EntityType | string? | Max 50 | Related entity type: `Wallet`, `Blueprint`, `Transaction`, `Register` |
| IsRead | bool | Default: false | Per-user read status |
| CreatedAt | DateTimeOffset | Required, default: UTC now | Event timestamp |
| ExpiresAt | DateTimeOffset | Required | Retention expiry (CreatedAt + 90 days) |

**Indexes**:
- `IX_ActivityEvent_UserId_CreatedAt` (UserId, CreatedAt DESC) — user's events, newest first
- `IX_ActivityEvent_OrgId_CreatedAt` (OrganizationId, CreatedAt DESC) — admin org-wide view
- `IX_ActivityEvent_ExpiresAt` (ExpiresAt) — cleanup query
- `IX_ActivityEvent_UserId_IsRead` (UserId, IsRead) WHERE IsRead = false — unread count

**Enum: EventSeverity**
```
Info = 0
Success = 1
Warning = 2
Error = 3
```

**Lifecycle**:
- Created when events occur (API call or internal service event)
- IsRead toggled when user opens activity log or marks all read
- Deleted when ExpiresAt < now (daily cleanup job)

---

### 2. UserPreferences (Tenant Service — PostgreSQL, per-org schema)

Stores per-user settings for cross-device consistency.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Preferences record identifier |
| UserId | Guid | Required, unique FK → UserIdentity.Id | One-to-one with user |
| Theme | ThemePreference | Required, default: System | Light, Dark, or System |
| Language | string | Required, default: "en", max 5 | ISO 639-1 code: en, fr, de, es |
| TimeFormat | TimeFormatPreference | Required, default: Local | UTC or Local |
| DefaultWalletAddress | string? | Max 200 | Address of user's default wallet |
| NotificationsEnabled | bool | Default: false | Push notification opt-in |
| TwoFactorEnabled | bool | Default: false, read-only via API | Whether 2FA is active (set by TOTP flow) |
| UpdatedAt | DateTimeOffset | Required | Last modification timestamp |

**Indexes**:
- `IX_UserPreferences_UserId` UNIQUE (UserId) — fast lookup by user

**Enum: ThemePreference**
```
Light = 0
Dark = 1
System = 2
```

**Enum: TimeFormatPreference**
```
UTC = 0
Local = 1
```

**Lifecycle**:
- Created lazily on first GET (if not exists, create with defaults)
- Updated via PUT endpoint
- Deleted when UserIdentity is deleted (cascade)

**Relationship**:
```
UserIdentity (1) ──── (0..1) UserPreferences
   via UserPreferences.UserId FK
```

---

### 3. TotpConfiguration (Tenant Service — PostgreSQL, per-org schema)

Stores 2FA TOTP secrets and backup codes per user.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Configuration identifier |
| UserId | Guid | Required, unique FK → UserIdentity.Id | One-to-one with user |
| EncryptedSecret | string | Required, max 500 | AES-256-GCM encrypted TOTP secret |
| BackupCodes | string[] | Required | Array of BCrypt-hashed backup codes |
| BackupCodesUsed | int | Default: 0 | Count of consumed backup codes |
| IsEnabled | bool | Default: false | Whether 2FA is actively enforced |
| IsVerified | bool | Default: false | Whether user has verified setup with first code |
| CreatedAt | DateTimeOffset | Required | Setup initiation timestamp |
| VerifiedAt | DateTimeOffset? | Nullable | When user verified with first code |

**Indexes**:
- `IX_TotpConfiguration_UserId` UNIQUE (UserId) — one config per user

**Lifecycle**:
1. **Setup initiated**: Create record with EncryptedSecret, IsEnabled=false, IsVerified=false
2. **User verifies**: Set IsVerified=true, VerifiedAt=now
3. **User enables**: Set IsEnabled=true (only if IsVerified=true)
4. **Login enforcement**: If IsEnabled=true, login requires TOTP code
5. **Backup code use**: Verify against hashed codes, increment BackupCodesUsed
6. **Disable**: Set IsEnabled=false (keep record for re-enable)
7. **Reset**: Delete record, user starts fresh

**Relationship**:
```
UserIdentity (1) ──── (0..1) TotpConfiguration
   via TotpConfiguration.UserId FK
```

---

## DTO Models (UI.Core)

### ActivityEventDto

```
ActivityEventDto
├── Id: Guid
├── EventType: string
├── Severity: string ("Info", "Success", "Warning", "Error")
├── Title: string
├── Message: string
├── SourceService: string
├── EntityId: string?
├── EntityType: string?
├── IsRead: bool
├── CreatedAt: DateTimeOffset
└── UserDisplayName: string?  (populated for admin org-wide views)
```

### UserPreferencesDto

```
UserPreferencesDto
├── Theme: string ("Light", "Dark", "System")
├── Language: string ("en", "fr", "de", "es")
├── TimeFormat: string ("UTC", "Local")
├── DefaultWalletAddress: string?
├── NotificationsEnabled: bool
└── TwoFactorEnabled: bool (read-only)
```

### UpdateUserPreferencesRequest

```
UpdateUserPreferencesRequest
├── Theme: string?
├── Language: string?
├── TimeFormat: string?
├── DefaultWalletAddress: string?
└── NotificationsEnabled: bool?
```

### TotpSetupResponse

```
TotpSetupResponse
├── SecretKey: string (base32 for manual entry)
├── QrCodeUri: string (otpauth:// URI for QR code)
└── BackupCodes: string[] (plaintext, shown once)
```

### TotpVerifyRequest

```
TotpVerifyRequest
└── Code: string (6-digit TOTP or backup code)
```

## Entity Relationship Diagram

```
Blueprint Service (PostgreSQL)         Tenant Service (per-org PostgreSQL)
┌─────────────────┐                   ┌─────────────────┐
│  ActivityEvent   │                   │  UserIdentity    │
├─────────────────┤                   ├─────────────────┤
│ Id (PK)          │                   │ Id (PK)          │
│ OrganizationId   │                   │ Email            │
│ UserId           │◄─ ─ ─ ─ ─ ─ ─ ─ ─│ DisplayName      │
│ EventType        │  (logical ref,    │ Roles            │
│ Severity         │   no FK)          │ Status           │
│ Title            │                   │ ...              │
│ Message          │                   └──────┬──────────┘
│ SourceService    │                          │ 1
│ EntityId         │                          │
│ EntityType       │                   ┌──────┴──────────┐
│ IsRead           │                   │ UserPreferences  │
│ CreatedAt        │                   ├─────────────────┤
│ ExpiresAt        │                   │ Id (PK)          │
└─────────────────┘                   │ UserId (FK, UQ)  │
                                      │ Theme            │
                                      │ Language         │
                                      │ TimeFormat       │
                                      │ DefaultWallet    │
                                      │ Notifications    │
                                      │ TwoFactorEnabled │
                                      │ UpdatedAt        │
                                      └──────────────────┘
                                             │ 0..1
                                      ┌──────┴──────────┐
                                      │TotpConfiguration │
                                      ├─────────────────┤
                                      │ Id (PK)          │
                                      │ UserId (FK, UQ)  │
                                      │ EncryptedSecret  │
                                      │ BackupCodes      │
                                      │ BackupCodesUsed  │
                                      │ IsEnabled        │
                                      │ IsVerified       │
                                      │ CreatedAt        │
                                      │ VerifiedAt       │
                                      └──────────────────┘
```

**Cross-Service Note**: ActivityEvent.UserId references UserIdentity.Id logically but not via database FK (different databases, different services). The UI joins these by including `UserDisplayName` in the API response for admin views.
