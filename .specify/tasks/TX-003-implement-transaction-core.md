# Task: Implement Transaction Core

**ID:** TX-003
**Status:** Not Started
**Priority:** Critical
**Estimate:** 12 hours
**Created:** 2025-11-12

## Objective

Implement core ITransaction interface and Transaction class with signing, verification, and state management.

## Implementation Details

### Interfaces/ITransaction.cs
```csharp
public interface ITransaction
{
    // Properties
    string? TxId { get; }
    TransactionVersion Version { get; }
    string? PreviousTxHash { get; }
    string? SenderWallet { get; }
    string[]? Recipients { get; }
    string? Metadata { get; }
    DateTime? Timestamp { get; }
    byte[]? Signature { get; }
    IPayloadManager PayloadManager { get; }

    // Methods
    Task<TransactionStatus> SignAsync(
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    Task<TransactionStatus> VerifyAsync(
        CancellationToken cancellationToken = default);

    byte[] SerializeToBinary();
    string SerializeToJson();
    TransportPacket GetTransportPacket();
}
```

### Core/Transaction.cs

**Key Features:**
1. **Double SHA-256 Signing**
   ```csharp
   private async Task<byte[]> ComputeSigningHashAsync()
   {
       var data = SerializeForSigning();
       var hash1 = hashProvider.ComputeHash(data, HashType.SHA256);
       var hash2 = hashProvider.ComputeHash(hash1, HashType.SHA256);
       return hash2;
   }
   ```

2. **Signature Verification**
   ```csharp
   public async Task<TransactionStatus> VerifyAsync()
   {
       // 1. Verify signature
       var hash = await ComputeSigningHashAsync();
       var status = await cryptoModule.VerifyAsync(
           Signature, hash, (byte)network, senderPublicKey);

       if (status != CryptoStatus.Success)
           return TransactionStatus.InvalidSignature;

       // 2. Verify all payload hashes
       var payloadStatus = await PayloadManager.VerifyAllAsync();
       return payloadStatus ? TransactionStatus.Success
           : TransactionStatus.InvalidPayload;
   }
   ```

3. **State Management**
   - Track if transaction is signed
   - Track if transaction is modified after signing
   - Prevent modifications after signing

## Testing Requirements

- [ ] Create transaction with metadata
- [ ] Add recipients
- [ ] Sign transaction
- [ ] Verify signature
- [ ] Verify prevents modification after signing
- [ ] Null parameter handling
- [ ] Invalid wallet address handling

## Acceptance Criteria

- [ ] ITransaction interface complete
- [ ] Transaction class implemented
- [ ] Signing working (double SHA-256)
- [ ] Verification working
- [ ] State management working
- [ ] All unit tests passing (>90% coverage)

---

**Dependencies:** TX-001, TX-002, Siccar.Cryptography v2.0
