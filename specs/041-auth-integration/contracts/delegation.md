# API Contract: Delegation Token Flow

**Feature**: 041-auth-integration
**Date**: 2026-02-25

## Existing Endpoint (Tenant Service — no changes needed)

### POST /api/service-auth/token/delegated

Issues a delegation token for a service to act on behalf of a user.

**Auth**: Anonymous (uses service credentials + user token in body)

**Request**:
```json
{
  "service_token": "eyJhbGciOiJIUzI1NiI...(service JWT)...",
  "user_access_token": "eyJhbGciOiJIUzI1NiI...(user JWT)...",
  "scopes": ["wallets:sign"]
}
```

**Response** (200 OK):
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiI...",
  "token_type": "Bearer",
  "expires_in": 300,
  "delegated_user_id": "user-guid",
  "delegated_org_id": "org-guid",
  "scope": "wallets:sign"
}
```

**Error** (401 Unauthorized — invalid service credentials):
```json
{
  "error": "invalid_client",
  "error_description": "Invalid service credentials"
}
```

**Error** (401 Unauthorized — revoked user token):
```json
{
  "error": "invalid_grant",
  "error_description": "User access token has been revoked"
}
```

**Error** (403 Forbidden — scope exceeds user's permissions):
```json
{
  "error": "invalid_scope",
  "error_description": "Requested scope exceeds user's granted permissions"
}
```

## New Client Interface: IDelegationTokenClient

```csharp
/// <summary>
/// Acquires delegation tokens for service-on-behalf-of-user operations.
/// </summary>
public interface IDelegationTokenClient
{
    /// <summary>
    /// Obtains a delegation token allowing this service to act on behalf of the specified user.
    /// </summary>
    /// <param name="userAccessToken">The user's current access token (from Authorization header)</param>
    /// <param name="scopes">The specific scopes needed for the downstream operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delegation JWT string, or null if acquisition failed</returns>
    Task<string?> GetDelegationTokenAsync(
        string userAccessToken,
        string[] scopes,
        CancellationToken cancellationToken = default);
}
```

**Implementation**: `DelegationTokenClient`
- Gets service token from `IServiceAuthClient`
- POSTs to `/api/service-auth/token/delegated` with both tokens
- **No caching** — delegation tokens are short-lived (5 min) and user-specific
- Returns null on failure (logs error)

## Delegation Flow Integration Pattern

### Blueprint Service (caller)

```csharp
// In action submission handler
var userToken = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
var delegationToken = await _delegationClient.GetDelegationTokenAsync(
    userToken,
    ["wallets:sign"],
    cancellationToken);

// Call Wallet Service with delegation token
var walletClient = _httpClientFactory.CreateClient("WalletService");
walletClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", delegationToken);
var result = await walletClient.PostAsync("/api/wallets/sign", content);
```

### Wallet Service (receiver)

Validates delegation token using `RequireDelegatedAuthority` policy:
- Checks `token_type=service` (service identity)
- Checks `delegated_user_id` present (user delegation)
- Can extract `delegated_user_id` to verify wallet ownership
- Can extract `scope` to verify operation is permitted

## RequireDelegatedAuthority Policy Contract

Must be added to Blueprint, Wallet, and Register services:

```csharp
options.AddPolicy("RequireDelegatedAuthority", policy =>
{
    policy.RequireClaim("token_type", "service");
    policy.RequireClaim("delegated_user_id");
});
```

### Endpoint Application

| Service | Endpoint | Policy |
|---------|----------|--------|
| Wallet | `POST /api/wallets/{id}/sign` | RequireDelegatedAuthority |
| Wallet | `POST /api/wallets/{id}/encrypt` | RequireDelegatedAuthority |
| Register | `POST /api/registers/{id}/transactions` | CanSubmitTransactions (existing — already accepts service tokens) |

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User token revoked after delegation issued | Delegation token remains valid until its 5-min expiry (per spec edge case) |
| Delegation token used for wrong scope | 403 Forbidden |
| Delegation token expired | 401 Unauthorized |
| Service token expired during delegation request | ServiceAuthClient auto-refreshes, retry once |
| Multiple delegation requests for same user | Each returns independent token (no dedup needed) |
