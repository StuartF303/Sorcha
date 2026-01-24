# MongoDB Types Reference

## Contents
- BSON Attributes
- Document Models
- Configuration Types
- Filter and Update Definitions
- Expression-Based Type Safety

---

## BSON Attributes

### Standard Mapping

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class SystemRegisterEntry
{
    [BsonId]  // Maps to MongoDB _id field
    public string BlueprintId { get; set; }

    [BsonElement("registerId")]  // Explicit field name
    public Guid RegisterId { get; set; }

    [BsonElement("document")]
    public BsonDocument Document { get; set; }  // Flexible schema

    [BsonElement("publishedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]  // UTC handling
    public DateTime PublishedAt { get; set; }

    [BsonIgnoreIfNull]  // Omit null values from document
    public string? Checksum { get; set; }
}
```

### WARNING: DateTime Without UTC Specification

**The Problem:**

```csharp
// BAD - Local time stored, UTC retrieved = wrong time
public DateTime CreatedAt { get; set; }
```

**Why This Breaks:**
1. MongoDB stores as UTC internally
2. Local time written, UTC retrieved = hours off
3. Different servers = different results

**The Fix:**

```csharp
// GOOD - Explicit UTC handling
[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
public DateTime CreatedAt { get; set; }

// Or always convert before storage
entity.CreatedAt = DateTime.UtcNow;
```

---

## Document Models

### Register (Mutable Entity)

```csharp
// From Sorcha.Register.Models
public class Register
{
    public string Id { get; set; }              // 32-char GUID (no hyphens)
    public string Name { get; set; }            // Human-readable
    public uint Height { get; set; }            // Block height
    public RegisterStatus Status { get; set; }  // Offline/Online/Archived
    public bool Advertise { get; set; }         // Network visibility
    public string TenantId { get; set; }        // Multi-tenant isolation
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### TransactionModel (Immutable Entity)

```csharp
// JSON-LD compatible transaction
public class TransactionModel
{
    [JsonPropertyName("@context")]
    public string? Context { get; set; }        // JSON-LD context

    [JsonPropertyName("@id")]
    public string? Id { get; set; }             // DID URI

    public string RegisterId { get; set; }      // Parent register
    public string TxId { get; set; }            // 64-char hex hash
    public string PrevTxId { get; set; }        // Chain link
    public ulong? BlockNumber { get; set; }     // Docket ID
    public string SenderWallet { get; set; }
    public IEnumerable<string> RecipientsWallets { get; set; }
    public DateTime TimeStamp { get; set; }
    public PayloadModel[] Payloads { get; set; }
    public string Signature { get; set; }
}
```

---

## Configuration Types

### MongoDB Storage Configuration

```csharp
// From MongoRegisterStorageConfiguration.cs
public class MongoRegisterStorageConfiguration
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "sorcha_register";
    public string RegisterCollectionName { get; set; } = "registers";
    public string TransactionCollectionName { get; set; } = "transactions";
    public string DocketCollectionName { get; set; } = "dockets";
    public int MaxBatchSize { get; set; } = 1000;
    public bool CreateIndexesOnStartup { get; set; } = true;
}
```

### appsettings.json Structure

```json
{
  "RegisterStorage": {
    "Type": "MongoDB",
    "MongoDB": {
      "ConnectionString": "mongodb://sorcha:password@localhost:27017",
      "DatabaseName": "sorcha_register",
      "RegisterCollectionName": "registers",
      "CreateIndexesOnStartup": true
    }
  }
}
```

---

## Filter and Update Definitions

### Filter Definition Types

```csharp
// Type-safe filter building
FilterDefinition<T> filter = Builders<T>.Filter.Eq(x => x.Id, id);

// Combine multiple filters
FilterDefinition<T> combined = Builders<T>.Filter.And(filter1, filter2);

// Empty filter (all documents)
FilterDefinition<T> all = Builders<T>.Filter.Empty;
```

### Update Definition Types

```csharp
// Single field update
UpdateDefinition<T> update = Builders<T>.Update.Set(x => x.Field, value);

// Multiple field update (chained)
UpdateDefinition<T> multi = Builders<T>.Update
    .Set(x => x.Field1, value1)
    .Set(x => x.Field2, value2)
    .Inc(x => x.Counter, 1);
```

---

## Expression-Based Type Safety

### ID Extraction Pattern

The Sorcha codebase requires both delegate and expression forms for maximum flexibility:

```csharp
// From MongoServiceExtensions.cs
public static IServiceCollection AddMongoDocumentStore<TDocument, TId>(
    this IServiceCollection services,
    string collectionName,
    Func<TDocument, TId> idSelector,           // Runtime delegate
    Expression<Func<TDocument, TId>> idExpression  // Compile-time expression
)
```

**Why Both?**
- `Func<>` - Fast runtime extraction for application code
- `Expression<>` - Translated to MongoDB filter queries

### Usage Example

```csharp
builder.Services.AddMongoDocumentStore<Blueprint, string>(
    "blueprints",
    doc => doc.Id,         // Delegate for C# code
    doc => doc.Id          // Expression for MongoDB filters
);
```

### WARNING: Wrong ID Type

**The Problem:**

```csharp
// BAD - Using object as ID type
AddMongoDocumentStore<Entity, object>("entities", e => e.Id, e => e.Id);
```

**Why This Breaks:**
1. No compile-time type checking
2. Boxing/unboxing overhead
3. Filter builders can't optimize

**The Fix:**

```csharp
// GOOD - Strong typing
AddMongoDocumentStore<Entity, Guid>("entities", e => e.Id, e => e.Id);
```