# Task: Implement Transaction Versioning Support

**ID:** TX-007
**Status:** Not Started
**Priority:** High
**Estimate:** 10 hours
**Created:** 2025-11-12

## Objective

Implement version detection, backward compatibility for v1-v4 transactions, and version routing.

## Implementation Details

### Versioning/VersionDetector.cs
```csharp
public class VersionDetector : IVersionDetector
{
    public TransactionVersion DetectVersion(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("Insufficient data");

        // Read version from first 4 bytes (little-endian)
        uint version = BitConverter.ToUInt32(data, 0);

        return version switch
        {
            1 => TransactionVersion.V1,
            2 => TransactionVersion.V2,
            3 => TransactionVersion.V3,
            4 => TransactionVersion.V4,
            _ => throw new NotSupportedException($"Version {version} not supported")
        };
    }

    public TransactionVersion DetectVersion(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetUInt32();

        return version switch
        {
            1 => TransactionVersion.V1,
            2 => TransactionVersion.V2,
            3 => TransactionVersion.V3,
            4 => TransactionVersion.V4,
            _ => throw new NotSupportedException($"Version {version} not supported")
        };
    }
}
```

### Versioning/TransactionFactory.cs
```csharp
public class TransactionFactory : ITransactionFactory
{
    public ITransaction Create(TransactionVersion version)
    {
        return version switch
        {
            TransactionVersion.V4 => new TransactionV4(),
            TransactionVersion.V3 => new TransactionV3Adapter(),
            TransactionVersion.V2 => new TransactionV2Adapter(),
            TransactionVersion.V1 => new TransactionV1Adapter(),
            _ => throw new NotSupportedException($"Version {version} not supported")
        };
    }

    public ITransaction Deserialize(byte[] data)
    {
        var version = versionDetector.DetectVersion(data);
        var transaction = Create(version);

        // Use version-specific deserializer
        var serializer = GetSerializer(version);
        return serializer.DeserializeFromBinary(data);
    }
}
```

### Backward Compatibility Adapters

**V1-V3 Adapters:**
- Read old format
- Convert to V4 internal representation
- Maintain signature compatibility
- Support old payload formats

## Testing Requirements

- [ ] Detect version from V1 binary
- [ ] Detect version from V2 binary
- [ ] Detect version from V3 binary
- [ ] Detect version from V4 binary
- [ ] Deserialize V1 transaction
- [ ] Deserialize V2 transaction
- [ ] Deserialize V3 transaction
- [ ] Verify old transaction signatures
- [ ] Decrypt old payloads

## Acceptance Criteria

- [ ] Version detection working for v1-v4
- [ ] Backward compatibility adapters implemented
- [ ] All old transactions can be read
- [ ] Old signatures can be verified
- [ ] Old payloads can be decrypted
- [ ] All backward compatibility tests passing

---

**Dependencies:** TX-001 through TX-006
