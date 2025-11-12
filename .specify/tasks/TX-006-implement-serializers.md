# Task: Implement Serializers (Binary, JSON, Transport)

**ID:** TX-006
**Status:** Not Started
**Priority:** High
**Estimate:** 12 hours
**Created:** 2025-11-12

## Objective

Implement three serialization formats: Binary (compact for storage), JSON (human-readable for APIs), and Transport (optimized for network transmission).

## Implementation Details

### Interfaces/ITransactionSerializer.cs
```csharp
public interface ITransactionSerializer
{
    byte[] SerializeToBinary(ITransaction transaction);
    ITransaction DeserializeFromBinary(byte[] data);
    string SerializeToJson(ITransaction transaction);
    ITransaction DeserializeFromJson(string json);
    TransportPacket CreateTransportPacket(ITransaction transaction);
}
```

### Binary Serialization (Variable-Length Encoding)

**Format:**
```
[Version: 4 bytes]
[Timestamp: 8 bytes]
[PreviousTxHash: VL-length + data]
[Recipients: VL-count + (VL-length + address) * count]
[Metadata: VL-length + JSON string]
[Signature: VL-length + signature bytes]
[SenderWallet: network byte + VL-length + pubkey]
[PayloadCount: VL-count]
[For each payload:
  - Flags: 2 bytes
  - Options: 8 bytes
  - EncryptedKeys: VL-count + (VL-wallet + VL-key) * count
  - IV: VL-length + IV bytes
  - Hash: 32 bytes
  - Data: VL-length + encrypted bytes
]
```

### JSON Serialization

**Format:**
```json
{
  "txId": "hash...",
  "version": 4,
  "timestamp": "2025-11-12T10:30:00Z",
  "previousTxHash": "hash...",
  "senderWallet": "ws1qyqszqgp...",
  "recipients": ["ws1...", "ws1..."],
  "metadata": {...},
  "signature": "hex...",
  "payloads": [
    {
      "id": 0,
      "type": "Data",
      "size": 1024,
      "isEncrypted": true,
      "isCompressed": true,
      "accessibleBy": ["ws1..."],
      "data": "base64..."
    }
  ]
}
```

### Transport Format

**Optimized for Network:**
```csharp
public class TransportPacket
{
    public string TxId { get; set; }
    public string RegisterId { get; set; }
    public byte[] Data { get; set; }  // Binary serialized transaction
}
```

## Acceptance Criteria

- [ ] Binary serialization working
- [ ] Binary deserialization working
- [ ] JSON serialization working
- [ ] JSON deserialization working
- [ ] Transport packet creation working
- [ ] Round-trip tests passing
- [ ] VarInt encoding working correctly

---

**Dependencies:** TX-001, TX-002, TX-003
