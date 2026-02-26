# API Contract: User Preferences API

**Service**: Tenant Service | **Base Path**: `/api/preferences`

## Authentication

All endpoints require JWT Bearer authentication. Users can only access their own preferences.

## Endpoints

### GET /api/preferences

Get the authenticated user's preferences. Creates default preferences if none exist (lazy initialization).

**Response** `200 OK`:
```json
{
  "theme": "System",
  "language": "en",
  "timeFormat": "Local",
  "defaultWalletAddress": "did:sorcha:w:abc123",
  "notificationsEnabled": false,
  "twoFactorEnabled": false
}
```

**Default Values** (when first created):

| Field | Default | Logic |
|-------|---------|-------|
| theme | "System" | Follow OS preference |
| language | "en" | English (client sends detected browser language on first request) |
| timeFormat | "Local" | User's local timezone |
| defaultWalletAddress | null | No default until set |
| notificationsEnabled | false | Opt-in |
| twoFactorEnabled | false | Managed by TOTP flow |

**Response** `401 Unauthorized`: Missing or invalid JWT.

---

### PUT /api/preferences

Update the authenticated user's preferences. Partial updates supported — omitted fields are not changed.

**Request Body**:
```json
{
  "theme": "Dark",
  "language": "fr",
  "timeFormat": "UTC",
  "defaultWalletAddress": "did:sorcha:w:xyz789",
  "notificationsEnabled": true
}
```

**Validation Rules**:

| Field | Validation |
|-------|-----------|
| theme | Must be "Light", "Dark", or "System" |
| language | Must be "en", "fr", "de", or "es" |
| timeFormat | Must be "UTC" or "Local" |
| defaultWalletAddress | If provided, must be non-empty string (max 200 chars). Wallet existence not validated server-side — client is responsible. |
| notificationsEnabled | Boolean |
| twoFactorEnabled | **Read-only** — cannot be set via this endpoint. Managed by TOTP API. |

**Response** `200 OK`: Returns the full updated preferences object.

**Response** `400 Bad Request`: Validation failure.
```json
{
  "errors": {
    "theme": ["Must be 'Light', 'Dark', or 'System'"],
    "language": ["Must be 'en', 'fr', 'de', or 'es'"]
  }
}
```

**Response** `401 Unauthorized`: Missing or invalid JWT.

---

### GET /api/preferences/default-wallet

Get just the default wallet address (lightweight endpoint for signing flows).

**Response** `200 OK`:
```json
{
  "defaultWalletAddress": "did:sorcha:w:abc123"
}
```

**Response** `200 OK` (no default set):
```json
{
  "defaultWalletAddress": null
}
```

---

### PUT /api/preferences/default-wallet

Set the default wallet address.

**Request Body**:
```json
{
  "walletAddress": "did:sorcha:w:abc123"
}
```

**Response** `200 OK`: Returns updated default wallet.

**Response** `400 Bad Request`: Empty or missing walletAddress.

---

### DELETE /api/preferences/default-wallet

Clear the default wallet selection.

**Response** `204 No Content**: Default wallet cleared.

---

## Integration Notes

### Migration from WalletPreferenceService

The existing `WalletPreferenceService` in `Sorcha.UI.Core` uses `Blazored.LocalStorage` with key `sorcha:preferences:defaultWallet`. This must be migrated:

1. On first login with new code, check localStorage for existing preference
2. If found, write to server-side preferences via PUT /api/preferences/default-wallet
3. Clear localStorage key after successful migration
4. All subsequent reads go through server-side API

### Preference Load on App Startup

1. App starts → GET /api/preferences
2. Apply theme immediately (ThemeService)
3. Load language file (LocalizationService)
4. Set time format (global setting)
5. Cache in memory for session duration
6. Write-through on any change (PUT /api/preferences)
