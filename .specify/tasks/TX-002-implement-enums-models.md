# Task: Implement Enums and Data Models

**ID:** TX-002
**Status:** Not Started
**Priority:** Critical
**Estimate:** 6 hours
**Created:** 2025-11-12

## Objective

Define foundational enums and data models for transaction and payload management.

## Files to Create

### Enums/TransactionVersion.cs
```csharp
public enum TransactionVersion : uint
{
    V1 = 1,
    V2 = 2,
    V3 = 3,
    V4 = 4  // Current version
}
```

### Enums/TransactionStatus.cs
```csharp
public enum TransactionStatus
{
    Success = 0,
    InvalidSignature = 1,
    InvalidPayload = 2,
    InvalidMetadata = 3,
    InvalidRecipients = 4,
    NotSigned = 5,
    SerializationFailed = 6,
    VersionNotSupported = 7
}
```

### Enums/PayloadType.cs
```csharp
public enum PayloadType : ushort
{
    Data = 0,           // Generic data
    Document = 1,       // Document/file
    Message = 2,        // Text message
    Metadata = 3,       // JSON metadata
    Custom = 999        // User-defined
}
```

### Models/TransactionResult.cs
```csharp
public class TransactionResult<T>
{
    public TransactionStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSuccess => Status == TransactionStatus.Success;

    public static TransactionResult<T> Success(T value) => new()
    {
        Status = TransactionStatus.Success,
        Value = value
    };

    public static TransactionResult<T> Failure(
        TransactionStatus status,
        string? error = null) => new()
    {
        Status = status,
        ErrorMessage = error
    };
}
```

### Models/TransactionMetadata.cs
```csharp
public sealed class TransactionMetadata
{
    public string? PreviousTxHash { get; set; }
    public string[]? Recipients { get; set; }
    public string? JsonMetadata { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? SenderWallet { get; set; }
    public byte[]? Signature { get; set; }
}
```

### Models/PayloadInfo.cs
```csharp
public sealed class PayloadInfo
{
    public uint Id { get; init; }
    public PayloadType Type { get; init; }
    public long OriginalSize { get; init; }
    public long CompressedSize { get; init; }
    public bool IsCompressed { get; init; }
    public bool IsEncrypted { get; init; }
    public EncryptionType EncryptionType { get; init; }
    public HashType HashType { get; init; }
    public string[] AccessibleBy { get; init; } = Array.Empty<string>();
}
```

## Acceptance Criteria

- [ ] All enum types defined with XML docs
- [ ] All model classes defined with XML docs
- [ ] TransactionResult generic type implemented
- [ ] All types compile without warnings
- [ ] No nullable warnings

---

**Dependencies:** TX-001
