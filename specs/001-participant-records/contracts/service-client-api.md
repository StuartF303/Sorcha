# API Contract: Service Client Extensions

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## IRegisterServiceClient — New Methods

**File**: `src/Common/Sorcha.ServiceClients/Register/IRegisterServiceClient.cs`

### Participant Query Methods

```
GetPublishedParticipantsAsync(registerId, page, pageSize, status?) → ParticipantPage
GetPublishedParticipantByAddressAsync(registerId, walletAddress) → PublishedParticipantRecord?
GetPublishedParticipantByIdAsync(registerId, participantId, includeHistory?) → PublishedParticipantRecord?
ResolvePublicKeyAsync(registerId, walletAddress, algorithm?) → PublicKeyResolution?
```

### Response Models

**PublishedParticipantRecord**:
| Field | Type | Notes |
|-------|------|-------|
| ParticipantId | string (UUID) | Immutable identity anchor |
| OrganizationName | string | From latest version |
| ParticipantName | string | From latest version |
| Status | string | active, deprecated, revoked |
| Version | int | Highest version number |
| LatestTxId | string | Transaction ID of latest version |
| Addresses | List<ParticipantAddressInfo> | All addresses from latest version |
| Metadata | JsonElement? | Opaque metadata |
| PublishedAt | DateTimeOffset | Timestamp of latest version |
| History | List<ParticipantVersionSummary>? | Only when includeHistory=true |

**ParticipantAddressInfo**:
| Field | Type | Notes |
|-------|------|-------|
| WalletAddress | string | Address string |
| PublicKey | string | Base64-encoded |
| Algorithm | string | ED25519, P-256, RSA-4096 |
| Primary | bool | Default address flag |

**ParticipantPage**:
| Field | Type | Notes |
|-------|------|-------|
| Page | int | Current page |
| PageSize | int | Items per page |
| Total | int | Total matching participants |
| Participants | List<PublishedParticipantRecord> | Results |

**PublicKeyResolution**:
| Field | Type | Notes |
|-------|------|-------|
| ParticipantId | string | Owning participant |
| ParticipantName | string | Display name |
| WalletAddress | string | Resolved address |
| PublicKey | string | Base64-encoded key |
| Algorithm | string | Key algorithm |
| Status | string | Participant status |

---

## IValidatorServiceClient — TransactionSubmission Update

**File**: `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs`

### TransactionSubmission Record Changes

| Field | Before | After |
|-------|--------|-------|
| BlueprintId | `required string` | `string?` |
| ActionId | `required string` | `string?` |

For Participant transactions:
- `BlueprintId` = null
- `ActionId` = "participant-publish" (or "participant-update" / "participant-revoke")
- `Metadata["Type"]` = "Participant"
- `Metadata["participantId"]` = UUID string

---

## Tenant Service Internal — Participant Publishing Service

**New interface**: `IParticipantPublishingService`

```
PublishParticipantAsync(request) → ParticipantPublishResult
UpdateParticipantAsync(participantId, request) → ParticipantPublishResult
RevokeParticipantAsync(participantId, registerId, signerWallet) → ParticipantPublishResult
```

**Dependencies** (injected):
- `IRegisterServiceClient` — query register for existing participants and chain tip
- `IValidatorServiceClient` — submit signed transaction
- `IParticipantService` — validate user authorization
- `IWalletServiceClient` — sign transactions with user's wallet
- `IHashProvider` — compute payload hashes and deterministic TxIds
