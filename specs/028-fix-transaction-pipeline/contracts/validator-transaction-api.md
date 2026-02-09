# API Contract: Validator Transaction Submission

**Existing Endpoint** (no changes needed to the Validator Service endpoint itself)

## POST /api/v1/transactions/validate

Submit an action transaction for validation and mempool inclusion.

### Request

```json
{
  "transactionId": "a1b2c3d4...64-char-hex-hash",
  "registerId": "register-id-string",
  "blueprintId": "blueprint-id-string",
  "actionId": "1",
  "payload": { /* JsonElement: the action's disclosed payload data */ },
  "payloadHash": "a1b2c3d4...64-char-hex-hash (same as transactionId)",
  "signatures": [
    {
      "publicKey": "base64-encoded-public-key-bytes",
      "signatureValue": "base64-encoded-signature-bytes",
      "algorithm": "ED25519"
    }
  ],
  "createdAt": "2026-02-09T12:00:00Z",
  "expiresAt": null,
  "priority": 1,
  "metadata": {
    "instanceId": "instance-id-string",
    "Type": "Action"
  }
}
```

### Response: 200 OK (Success)

```json
{
  "isValid": true,
  "added": true,
  "transactionId": "a1b2c3d4...",
  "registerId": "register-id",
  "addedAt": "2026-02-09T12:00:01Z"
}
```

### Response: 400 Bad Request (Validation Failed)

```json
{
  "isValid": false,
  "errors": [
    {
      "code": "INVALID_SIGNATURE",
      "message": "Signature verification failed",
      "field": "Signatures[0]"
    }
  ]
}
```

### Response: 409 Conflict (Mempool Full or Duplicate)

```json
{
  "isValid": true,
  "added": false,
  "message": "Failed to add transaction to memory pool (pool full or duplicate)"
}
```

---

## New Client Method: IValidatorServiceClient.SubmitTransactionAsync

### Interface Addition

```csharp
Task<TransactionSubmissionResult> SubmitTransactionAsync(
    ActionTransactionSubmission request,
    CancellationToken cancellationToken = default);
```

### Request Model

```csharp
public record ActionTransactionSubmission
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required JsonElement Payload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<SignatureInfo> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public record SignatureInfo
{
    public required string PublicKey { get; init; }
    public required string SignatureValue { get; init; }
    public required string Algorithm { get; init; }
}
```

### Response Model

```csharp
public record TransactionSubmissionResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string RegisterId { get; init; } = string.Empty;
    public DateTimeOffset? AddedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}
```
