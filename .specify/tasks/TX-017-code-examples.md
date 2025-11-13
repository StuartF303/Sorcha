# Task: Create Code Examples

**ID:** TX-017
**Status:** Not Started
**Priority:** Medium
**Estimate:** 6 hours
**Created:** 2025-11-12

## Objective

Create comprehensive code examples demonstrating transaction and payload management.

## Example Categories

### 1. Basic Transaction (examples/01-BasicTransaction/)
```csharp
// Create, sign, and verify a simple transaction
var result = await new TransactionBuilder()
    .Create()
    .WithRecipients("ws1qyqszqgp...")
    .WithMetadata("{\"type\": \"transfer\"}")
    .SignAsync(senderWifKey)
    .Build();

if (result.IsSuccess)
{
    var tx = result.Value;
    var isValid = await tx.VerifyAsync();
    Console.WriteLine($"Transaction {tx.TxId} is valid: {isValid}");
}
```

### 2. Multi-Recipient Payload (examples/02-MultiRecipient/)
```csharp
// Encrypt payload for multiple recipients
var transaction = await new TransactionBuilder()
    .Create()
    .WithRecipients("wallet1", "wallet2", "wallet3")
    .AddPayload(
        data: documentBytes,
        recipientWallets: new[] { "wallet1", "wallet2" },
        options: new PayloadOptions
        {
            EncryptionType = EncryptionType.XCHACHA20_POLY1305,
            CompressionType = CompressionType.Balanced
        })
    .SignAsync(senderWifKey)
    .Build();

// Wallet1 can decrypt
var payload1 = await transaction.Value.PayloadManager
    .GetPayloadDataAsync(0, wallet1WifKey);

// Wallet3 cannot decrypt (not in recipient list)
var payload3 = await transaction.Value.PayloadManager
    .GetPayloadDataAsync(0, wallet3WifKey);  // Returns AccessDenied
```

### 3. Payload Access Control (examples/03-AccessControl/)
```csharp
// Grant access to additional recipient
await payloadManager.GrantAccessAsync(
    payloadId: 0,
    recipientWallet: "wallet3",
    ownerWifKey: wallet1WifKey);

// Now wallet3 can decrypt
var payload = await payloadManager.GetPayloadDataAsync(0, wallet3WifKey);
```

### 4. Transaction Serialization (examples/04-Serialization/)
```csharp
// Serialize to binary for storage
var binary = transaction.SerializeToBinary();
File.WriteAllBytes("transaction.bin", binary);

// Serialize to JSON for API
var json = transaction.SerializeToJson();
Console.WriteLine(json);

// Create transport packet for network
var packet = transaction.GetTransportPacket();
await SendToNetworkAsync(packet);
```

### 5. Backward Compatibility (examples/05-BackwardCompatibility/)
```csharp
// Read old v1 transaction
var v1Data = File.ReadAllBytes("old_transaction_v1.bin");
var factory = new TransactionFactory();
var transaction = factory.Deserialize(v1Data);

Console.WriteLine($"Version: {transaction.Version}");
Console.WriteLine($"TxId: {transaction.TxId}");

// Verify old signature
var isValid = await transaction.VerifyAsync();
Console.WriteLine($"Old signature valid: {isValid}");
```

### 6. Complete Workflow (examples/06-CompleteWorkflow/)
Complete document sharing scenario with multiple parties.

## Acceptance Criteria

- [ ] All example categories created
- [ ] Examples compile and run
- [ ] Examples demonstrate best practices
- [ ] README references examples

---

**Dependencies:** TX-001 through TX-014
