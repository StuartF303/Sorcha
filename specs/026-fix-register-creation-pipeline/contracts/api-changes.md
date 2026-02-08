# API Contract Changes: 026-fix-register-creation-pipeline

**Date**: 2026-02-08

## New Endpoints

### Peer Service — Register Advertisement

**Endpoint**: `POST /api/registers/{registerId}/advertise`

**Purpose**: Notifies the Peer Service to begin or stop advertising a register to the peer network.

**Request**:
```json
{
  "isPublic": true
}
```

**Responses**:
- `200 OK` — Advertisement updated successfully
- `404 Not Found` — Register not found in local advertisement list (when setting isPublic=false for unknown register)

**Behavior**:
- When `isPublic=true`: Calls `RegisterAdvertisementService.AdvertiseRegister()` with default sync state and version 0
- When `isPublic=false`: Calls `RegisterAdvertisementService.RemoveAdvertisement()`
- This endpoint is called fire-and-forget by the Register Service; failures are logged but not propagated

### Validator Service — Monitoring Query

**Endpoint**: `GET /api/admin/validators/monitoring`

**Purpose**: Returns the list of register IDs currently being monitored by the Validator Service for docket building.

**Response**:
```json
{
  "registerIds": ["abc123def456...", "789012fed345..."],
  "count": 2
}
```

**Responses**:
- `200 OK` — Always succeeds, returns empty list if no registers monitored

## Modified Endpoints

### Register Service — Initiate Creation

**Endpoint**: `POST /api/registers/create/initiate`

**Updated Request Model** (`InitiateRegisterCreationRequest`):
```json
{
  "name": "my-register",
  "description": "Optional description",
  "tenantId": "tenant-guid",
  "owners": [{ "subject": "owner-subject", "publicKey": "base64-key" }],
  "additionalAdmins": [],
  "metadata": {},
  "advertise": true
}
```

**New Field**: `advertise` (bool, default: false) — Controls whether the register is advertised to the peer network after creation.

### Register Service — Update Register

**Endpoint**: `PUT /api/registers/{id}`

**Updated Behavior**: When the `advertise` field changes, the Register Service now sends a fire-and-forget notification to the Peer Service via `IPeerServiceClient.AdvertiseRegisterAsync()`. Notification failure is logged but does not affect the response.

## Modified Internal Contracts

### IPeerServiceClient — New Method

**Interface**: `Sorcha.ServiceClients.Peer.IPeerServiceClient`

**New Method**:
```csharp
Task AdvertiseRegisterAsync(string registerId, bool isPublic, CancellationToken cancellationToken = default);
```

**Purpose**: HTTP client method to call the Peer Service advertisement endpoint.

### DocketBuildTriggerService — Transaction Mapping Fix

**Affected Method**: `WriteDocketAndTransactionsAsync()`

**Before** (broken):
```csharp
Payloads = [],
PayloadCount = 0,
SenderWallet = "system",
Signature = string.Empty
```

**After** (correct):
```csharp
Payloads = new[] { new PayloadModel { Data = Base64(tx.Payload.GetRawText()), Hash = tx.PayloadHash, ... } },
PayloadCount = 1,
SenderWallet = Base64(tx.Signatures[0].PublicKey) ?? "system",
Signature = Base64(tx.Signatures[0].SignatureValue)
```

### DocketBuildTriggerService — Genesis Retry Logic

**Affected Behavior**: Genesis docket write completion tracking

**Before**: `_genesisWritten[registerId] = true` set unconditionally after write attempt (even on failure)
**After**: Flag set only on successful write. New `_genesisRetryCount` dictionary tracks failures. After 3 failures, register is unmonitored and warning logged.

### GenesisManager — Error Propagation

**Affected Method**: `NeedsGenesisDocketAsync()`

**Before**: Catches all exceptions, returns `false` (silently skipping genesis)
**After**: Removes catch-all; exceptions propagate to `DocketBuilder.BuildDocketAsync()` which already handles them

### Genesis Constants

**New File**: `src/Common/Sorcha.Register.Models/Constants/GenesisConstants.cs`

```csharp
public static class GenesisConstants
{
    public const string BlueprintId = "genesis";
    public const string ActionId = "register-creation";
}
```

Replaces magic string `"genesis"` in `ValidationEndpoints.cs:317`.
