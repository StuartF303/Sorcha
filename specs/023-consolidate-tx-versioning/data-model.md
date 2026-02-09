# Data Model: Consolidate Transaction Versioning

**Feature**: 023-consolidate-tx-versioning
**Date**: 2026-02-07

## Overview

This feature involves no new entities and no structural data model changes. The only model change is the `TransactionVersion` enum, which is reduced from four values to one.

## Entity Changes

### TransactionVersion (Enum)

**Before**:
| Value | Integer | Description |
|-------|---------|-------------|
| V1    | 1       | Legacy — basic fields only |
| V2    | 2       | Added sender wallet |
| V3    | 3       | Added recipients, metadata |
| V4    | 4       | Current — full field set |

**After**:
| Value | Integer | Description |
|-------|---------|-------------|
| V1    | 1       | Full field set (all capabilities) |

### Transaction (Unchanged Structure)

The `Transaction` entity retains all fields. The only change is the default version parameter shifts from V4 to V1.

| Field          | Type             | Present in new V1 |
|----------------|------------------|--------------------|
| TxId           | string?          | Yes                |
| Version        | TransactionVersion | Yes (always V1)  |
| PreviousTxHash | string?          | Yes                |
| SenderWallet   | string?          | Yes                |
| Recipients     | string[]?        | Yes                |
| Metadata       | string?          | Yes                |
| Timestamp      | DateTime?        | Yes                |
| Signature      | byte[]?          | Yes                |
| PayloadManager | IPayloadManager  | Yes                |
| RegisterId     | string?          | Yes                |
| DocketNumber    | ulong?           | Yes                |

### TransactionModel (No Change)

The `TransactionModel` in Register Models already defaults `Version = 1`. No modification required.

## Wire Format

### Binary Format (Unchanged)

```
[4 bytes: version uint32 LE]  ← value changes from 4 to 1
[1 byte: hasTimestamp bool]
[8 bytes: timestamp int64, if hasTimestamp]
[VarInt + UTF-8: previousTxHash]
[VarInt + UTF-8: senderWallet]
[VarInt: recipientCount]
  [VarInt + UTF-8: recipient] × recipientCount
[VarInt + UTF-8: metadata]
[1 byte: hasSignature bool]
  [VarInt + bytes: signature, if hasSignature]
[VarInt: payloadCount]
  [payload structure] × payloadCount
```

### JSON Format (Unchanged)

```json
{
  "txId": "...",
  "version": 1,
  "timestamp": "...",
  "previousTxHash": "...",
  "senderWallet": "...",
  "recipients": ["..."],
  "metadata": { ... },
  "signature": "...",
  "payloads": [{ ... }]
}
```

Note: `"version"` value changes from `4` to `1`. All other fields and structure identical.
