# API Contract: Activity Events API

**Service**: Blueprint Service | **Base Path**: `/api/events`

## Authentication

All endpoints require JWT Bearer authentication. Admin endpoints require `Administrator` or `SystemAdmin` role.

## Endpoints

### GET /api/events

List activity events for the authenticated user.

**Query Parameters**:

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number (1-based) |
| pageSize | int | No | 50 | Items per page (max 100) |
| unreadOnly | bool | No | false | Filter to unread events only |
| severity | string | No | — | Filter by severity: Info, Success, Warning, Error |
| since | DateTimeOffset | No | — | Events created after this timestamp |

**Response** `200 OK`:
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "eventType": "WalletCreated",
      "severity": "Success",
      "title": "Wallet Created",
      "message": "Wallet 'My Primary Wallet' created with ED25519 algorithm",
      "sourceService": "Wallet",
      "entityId": "did:sorcha:w:abc123",
      "entityType": "Wallet",
      "isRead": false,
      "createdAt": "2026-02-26T10:30:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 50
}
```

**Response** `401 Unauthorized`: Missing or invalid JWT.

---

### GET /api/events/unread-count

Get the count of unread events for the authenticated user.

**Response** `200 OK`:
```json
{
  "count": 5
}
```

---

### POST /api/events/mark-read

Mark events as read.

**Request Body**:
```json
{
  "eventIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

Pass empty `eventIds` array to mark ALL as read.

**Response** `200 OK`:
```json
{
  "markedCount": 5
}
```

---

### POST /api/events

Create an activity event (internal service-to-service use).

**Authorization**: Service token required.

**Request Body**:
```json
{
  "organizationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "TransactionSubmitted",
  "severity": "Info",
  "title": "Transaction Submitted",
  "message": "Action 'Review Document' submitted on register 'Legal Docs'",
  "sourceService": "Blueprint",
  "entityId": "tx-abc123",
  "entityType": "Transaction"
}
```

**Response** `201 Created`: Returns the created event with `id`, `createdAt`, and `expiresAt`.

---

### GET /api/events/admin

List events for all users in the admin's organisation (admin only).

**Authorization**: `Administrator` or `SystemAdmin` role required.

**Query Parameters**: Same as GET /api/events, plus:

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| userId | Guid | No | — | Filter to specific user's events |

**Response** `200 OK`: Same shape as GET /api/events, with additional `userDisplayName` field per event.

**Response** `403 Forbidden`: User lacks admin role.

---

### DELETE /api/events/{id}

Delete a specific event (user can only delete their own).

**Response** `204 No Content`: Event deleted.

**Response** `404 Not Found`: Event not found or belongs to another user.

---

## SignalR Hub: /hubs/events

### Connection

```
wss://{host}/hubs/events?access_token={jwt}
```

### Server Methods (Client → Server)

| Method | Parameters | Description |
|--------|------------|-------------|
| `Subscribe` | — | Subscribe to personal events (auto-joined to `user:{userId}` group) |
| `SubscribeOrg` | — | Subscribe to org-wide events (admin only, joins `org:{orgId}` group) |
| `Unsubscribe` | — | Leave personal event group |
| `UnsubscribeOrg` | — | Leave org event group |

### Client Methods (Server → Client)

| Method | Parameters | Description |
|--------|------------|-------------|
| `EventReceived` | `ActivityEventDto event` | New event pushed in real-time |
| `UnreadCountUpdated` | `int count` | Updated unread count after batch operations |

### Event Types

| EventType | Severity | Source | Trigger |
|-----------|----------|--------|---------|
| WalletCreated | Success | Wallet | Wallet creation |
| WalletDeleted | Warning | Wallet | Wallet deletion |
| TransactionSubmitted | Info | Blueprint | Action submission |
| TransactionConfirmed | Success | Register | Ledger confirmation |
| ActionAssigned | Info | Blueprint | New pending action |
| ActionConfirmed | Success | Blueprint | Action completed |
| ActionRejected | Warning | Blueprint | Action rejected |
| BlueprintPublished | Success | Blueprint | Blueprint publish |
| BlueprintCreated | Info | Blueprint | Blueprint creation |
| LoginSuccess | Info | Tenant | Successful login |
| LoginFailed | Warning | Tenant | Failed login attempt |
| TwoFactorEnabled | Info | Tenant | 2FA setup completed |
| PreferencesUpdated | Info | Tenant | Settings changed |
