# Data Model: Registers and Transactions UI

**Date**: 2026-01-20
**Feature**: 012-registers-transactions-ui

## Overview

This document defines the UI-side data models (DTOs and ViewModels) for the Registers and Transactions feature. These models wrap or extend the existing backend models from `Sorcha.Register.Models` for UI-specific needs.

## Entities

### RegisterViewModel

Wraps `Register` with UI-specific computed properties.

```
RegisterViewModel
├── Id: string (32-char GUID, from backend)
├── Name: string (1-38 chars, from backend)
├── Height: uint (block count, from backend)
├── Status: RegisterStatus (enum, from backend)
├── Advertise: bool (public visibility, from backend)
├── TenantId: string (organization ID, from backend)
├── CreatedAt: DateTime (from backend)
├── UpdatedAt: DateTime (from backend)
├── IsOnline: bool (computed: Status == Online)
├── StatusColor: Color (computed: based on Status)
├── StatusIcon: string (computed: based on Status)
├── LastUpdateFormatted: string (computed: relative time)
└── TransactionCountFormatted: string (computed: "1.2K" format)
```

**Validation Rules**:
- Id: Required, exactly 32 characters
- Name: Required, 1-38 characters
- TenantId: Required for non-public registers

**Status → Color Mapping**:
| Status | Color | Icon |
|--------|-------|------|
| Online | Success (green) | CheckCircle |
| Offline | Default (gray) | Cancel |
| Checking | Warning (orange) | Sync |
| Recovery | Error (red) | Warning |

### TransactionViewModel

Wraps `TransactionModel` with UI-specific formatting.

```
TransactionViewModel
├── TxId: string (64-char hex, from backend)
├── RegisterId: string (from backend)
├── SenderWallet: string (Base58, from backend)
├── RecipientsWallets: string[] (from backend)
├── TimeStamp: DateTime (from backend)
├── BlockNumber: ulong? (from backend)
├── PayloadCount: ulong (from backend)
├── Signature: string (from backend)
├── MetaData: TransactionMetaData? (from backend)
├── TxIdTruncated: string (computed: first 8 chars + "...")
├── SenderTruncated: string (computed: first 8 + "..." + last 4)
├── TimeStampFormatted: string (computed: relative or absolute)
├── IsRecent: bool (computed: within last 5 seconds)
└── TransactionType: string (computed: from MetaData or "Transfer")
```

**Validation Rules**:
- TxId: Required, exactly 64 characters
- RegisterId: Required
- SenderWallet: Required, valid Base58
- Signature: Required

### TransactionListResponse

Paginated response from API.

```
TransactionListResponse
├── Page: int (current page number)
├── PageSize: int (items per page)
├── Total: int (total count)
├── Transactions: TransactionViewModel[]
├── HasMore: bool (computed: Page * PageSize < Total)
└── TotalPages: int (computed: ceil(Total / PageSize))
```

### RegisterCreationRequest

Request model for creating a new register.

```
RegisterCreationRequest
├── Name: string (required, 1-38 chars)
├── TenantId: string (required)
├── Advertise: bool (default: false)
└── IsFullReplica: bool (default: true)
```

**Validation Rules**:
- Name: Required, 1-38 characters, alphanumeric with spaces/hyphens
- TenantId: Required, must match user's organization

### RegisterCreationState

Wizard state tracking.

```
RegisterCreationState
├── CurrentStep: int (1-3)
├── Request: RegisterCreationRequest
├── InitiateResponse: InitiateRegisterCreationResponse?
├── IsProcessing: bool
├── ErrorMessage: string?
└── IsComplete: bool
```

### ConnectionState

SignalR connection state for UI display.

```
ConnectionState
├── Status: ConnectionStatus (Connected/Connecting/Disconnected/Reconnecting)
├── LastConnected: DateTime?
├── ReconnectAttempts: int
├── ErrorMessage: string?
└── IsHealthy: bool (computed: Status == Connected)
```

**ConnectionStatus Enum**:
```
ConnectionStatus
├── Disconnected
├── Connecting
├── Connected
└── Reconnecting
```

## Relationships

```
Organization (TenantId)
    │
    ├──< Register (many registers per organization)
    │       │
    │       └──< Transaction (many transactions per register)
    │
    └──< User (many users per organization)
            │
            └── Role (Participant or Administrator)
```

## State Transitions

### Register Status
```
[Created] ──► Online ◄──► Offline
                │
                ▼
            Checking ──► Recovery ──► Online
```

### Connection Status
```
[Initial] ──► Connecting ──► Connected
                  │              │
                  │              ▼
                  │         Disconnected
                  │              │
                  ▼              ▼
              (Error)      Reconnecting ──► Connected
```

## UI Component Data Flow

```
┌─────────────────┐
│  RegisterService │ ◄── HTTP/REST ── API Gateway
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ RegistersPage   │ ◄── Registers list
└────────┬────────┘
         │ (select)
         ▼
┌─────────────────┐     ┌────────────────────┐
│ RegisterDetail  │ ◄───│ RegisterHubConnection│ ◄── SignalR
└────────┬────────┘     └────────────────────┘
         │
         ├── TransactionList (virtualized)
         │       │
         │       ▼
         │   TransactionRow (repeated)
         │
         └── TransactionDetail (selected)
```
