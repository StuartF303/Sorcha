# API Contract: TOTP Two-Factor Authentication API

**Service**: Tenant Service | **Base Path**: `/api/totp`

## Authentication

All endpoints require JWT Bearer authentication except the login verification step.

## Endpoints

### POST /api/totp/setup

Initiate 2FA setup. Generates a TOTP secret and backup codes.

**Precondition**: User must NOT already have 2FA enabled. If already enabled, returns 409 Conflict.

**Response** `200 OK`:
```json
{
  "secretKey": "JBSWY3DPEHPK3PXP",
  "qrCodeUri": "otpauth://totp/Sorcha:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Sorcha&algorithm=SHA1&digits=6&period=30",
  "backupCodes": [
    "A1B2-C3D4",
    "E5F6-G7H8",
    "I9J0-K1L2",
    "M3N4-O5P6",
    "Q7R8-S9T0",
    "U1V2-W3X4",
    "Y5Z6-A7B8",
    "C9D0-E1F2"
  ]
}
```

**Response** `409 Conflict`: 2FA already enabled.
```json
{
  "error": "Two-factor authentication is already enabled. Disable it first to reconfigure."
}
```

**Notes**:
- `secretKey` is Base32-encoded for manual entry in authenticator apps
- `qrCodeUri` is the standard `otpauth://` URI for QR code scanning
- `backupCodes` are shown once and cannot be retrieved again
- The configuration is created in unverified state — user must verify before it activates

---

### POST /api/totp/verify

Verify a TOTP code to complete setup or validate during login.

**Request Body**:
```json
{
  "code": "123456"
}
```

**Behavior**:
- **During setup** (IsVerified=false): Verifies code against secret, sets IsVerified=true, IsEnabled=true
- **During login** (IsVerified=true, IsEnabled=true): Validates code for login approval
- **Backup code**: If `code` matches a backup code format (8 chars with hyphen), validates against backup codes

**Response** `200 OK`:
```json
{
  "verified": true,
  "isSetupComplete": true
}
```

**Response** `400 Bad Request`: Invalid code.
```json
{
  "verified": false,
  "error": "Invalid verification code. Please try again."
}
```

**Response** `404 Not Found`: No TOTP configuration exists.

**TOTP Parameters**:
- Algorithm: SHA1
- Digits: 6
- Period: 30 seconds
- Window: ±1 step (allows for clock skew)

---

### DELETE /api/totp

Disable and remove 2FA configuration.

**Precondition**: Must provide a valid TOTP code or backup code for security.

**Request Body**:
```json
{
  "code": "123456"
}
```

**Response** `204 No Content`: 2FA disabled, configuration deleted.

**Response** `400 Bad Request`: Invalid code — cannot disable without valid verification.

**Response** `404 Not Found`: No TOTP configuration exists.

---

### GET /api/totp/status

Check whether 2FA is configured and enabled.

**Response** `200 OK`:
```json
{
  "isConfigured": true,
  "isEnabled": true,
  "backupCodesRemaining": 6
}
```

**Response** `200 OK` (not configured):
```json
{
  "isConfigured": false,
  "isEnabled": false,
  "backupCodesRemaining": 0
}
```

---

### POST /api/totp/backup-codes/regenerate

Generate new backup codes (invalidates all existing codes).

**Precondition**: Must provide a valid TOTP code.

**Request Body**:
```json
{
  "code": "123456"
}
```

**Response** `200 OK`:
```json
{
  "backupCodes": [
    "A1B2-C3D4",
    "E5F6-G7H8",
    "I9J0-K1L2",
    "M3N4-O5P6",
    "Q7R8-S9T0",
    "U1V2-W3X4",
    "Y5Z6-A7B8",
    "C9D0-E1F2"
  ]
}
```

---

## Login Flow Integration

The existing `/api/auth/login` endpoint must be modified:

### Current Flow
```
POST /api/auth/login { email, password }
  → 200 { accessToken, refreshToken }
```

### Updated Flow (with 2FA)
```
POST /api/auth/login { email, password }
  → 200 { accessToken, refreshToken }              (2FA not enabled)
  → 200 { requiresTwoFactor: true, loginToken }     (2FA enabled, needs code)

POST /api/totp/verify { code }
  Headers: Authorization: Bearer {loginToken}
  → 200 { verified: true }                          (code valid)
  → 400 { verified: false, error: "..." }            (code invalid)

POST /api/auth/login/complete { loginToken }
  → 200 { accessToken, refreshToken }               (full auth granted)
```

### Login Token
- Short-lived JWT (5 minutes) with claim `token_type: "2fa_pending"`
- Only valid for `/api/totp/verify` and `/api/auth/login/complete`
- Cannot access any other API endpoint
- Includes userId and organizationId claims

---

## Security Considerations

- TOTP secrets encrypted at rest with AES-256-GCM (same key management as wallet keys)
- Backup codes BCrypt-hashed (cannot be retrieved, only verified)
- Rate limiting on verify endpoint: 5 attempts per minute per user
- Failed verification attempts logged as security events
- Setup endpoint rate limited: 3 calls per hour per user
