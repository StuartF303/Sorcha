# MongoDB Errors Reference

## Contents
- Connection Errors
- Duplicate Key Errors
- Query Errors
- Serialization Errors
- Timeout and Performance Issues
- WORM Violation Errors

---

## Connection Errors

### MongoConnectionException

**Symptom:**
```
MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server.
```

**Common Causes:**
1. MongoDB not running
2. Wrong connection string
3. Network/firewall blocking port 27017
4. Authentication failed

**Diagnostic Steps:**

```bash
# Check MongoDB is running (Docker)
docker ps | grep mongo

# Test connection
docker exec -it sorcha-mongodb mongosh --eval "db.runCommand({ping:1})"

# Check connection string in appsettings
cat src/Services/Sorcha.Register.Service/appsettings.MongoDB.json
```

**Fix for Docker Setup:**

```yaml
# docker-compose.yml
services:
  mongodb:
    image: mongo:7.0
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: sorcha
      MONGO_INITDB_ROOT_PASSWORD: sorcha_dev_password
```

---

## Duplicate Key Errors

### MongoWriteException (E11000)

**Symptom:**
```
MongoDB.Driver.MongoWriteException: A write operation resulted in an error.
E11000 duplicate key error collection: sorcha_register.transactions index: RegisterId_1_TxId_1
```

**Why It Happens:**
Attempting to insert a document with a key that already exists in a unique index.

**DON'T:**

```csharp
// BAD - No existence check before insert
await _collection.InsertOneAsync(transaction);
```

**DO:**

```csharp
// GOOD - Use upsert or check existence
var filter = Builders<TransactionModel>.Filter.And(
    Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, tx.RegisterId),
    Builders<TransactionModel>.Filter.Eq(t => t.TxId, tx.TxId)
);

var exists = await _collection.CountDocumentsAsync(filter, 
    new CountOptions { Limit = 1 }) > 0;

if (exists)
{
    throw new DuplicateTransactionException(tx.TxId);
}

await _collection.InsertOneAsync(tx);
```

**For WORM Store:**

```csharp
// MongoWormStore already handles this
public async Task AppendAsync(TDocument document, CancellationToken ct)
{
    if (await ExistsAsync(_idSelector(document), ct))
    {
        throw new InvalidOperationException(
            "Document already exists. WORM store is append-only.");
    }
    await _collection.InsertOneAsync(document, cancellationToken: ct);
}
```

---

## Query Errors

### InvalidOperationException: Sequence contains no elements

**Symptom:**
```
System.InvalidOperationException: Sequence contains no elements
```

**Why It Happens:**
Using `.First()` or `.Single()` on empty result set.

**DON'T:**

```csharp
// BAD - Throws if not found
var doc = await _collection.Find(filter).FirstAsync();
```

**DO:**

```csharp
// GOOD - Returns null if not found
var doc = await _collection.Find(filter).FirstOrDefaultAsync();
if (doc is null)
{
    throw new EntityNotFoundException(id);
}
```

### Filter Translation Errors

**Symptom:**
```
MongoDB.Driver.Linq.ExpressionNotSupportedException: Expression not supported
```

**Why It Happens:**
Using C# methods that can't translate to MongoDB query language.

**DON'T:**

```csharp
// BAD - Custom method can't translate
var filter = Builders<T>.Filter.Where(x => MyCustomMethod(x.Field));
```

**DO:**

```csharp
// GOOD - Use supported expressions
var filter = Builders<T>.Filter.Regex(x => x.Field, new BsonRegularExpression(pattern));
```

---

## Serialization Errors

### BsonSerializationException

**Symptom:**
```
MongoDB.Bson.BsonSerializationException: An error occurred while serializing the property
```

**Common Causes:**
1. Null value in non-nullable field
2. Missing parameterless constructor
3. Unsupported type (e.g., `dynamic`)

**DON'T:**

```csharp
// BAD - No parameterless constructor
public class Entity
{
    public Entity(string id) { Id = id; }
    public string Id { get; }
}
```

**DO:**

```csharp
// GOOD - Parameterless constructor for BSON
public class Entity
{
    public Entity() { }
    public string Id { get; set; }
}
```

### DateTime Serialization Mismatch

**Symptom:**
Dates off by several hours when reading back from MongoDB.

**DON'T:**

```csharp
// BAD - Local time stored
entity.CreatedAt = DateTime.Now;
```

**DO:**

```csharp
// GOOD - Always UTC
[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
public DateTime CreatedAt { get; set; }

// And store UTC
entity.CreatedAt = DateTime.UtcNow;
```

---

## Timeout and Performance Issues

### OperationCanceledException / Timeout

**Symptom:**
```
System.OperationCanceledException: The operation was canceled.
MongoDB.Driver.MongoCommandException: Command timed out
```

**Diagnostic Checklist:**

```markdown
Copy this checklist:
- [ ] Check if query uses indexes: `db.collection.find(query).explain()`
- [ ] Verify index exists for query fields
- [ ] Check collection size vs query complexity
- [ ] Review connection pool settings
- [ ] Check for lock contention
```

**Common Fixes:**

```csharp
// 1. Add missing index
var index = Builders<T>.IndexKeys.Ascending(x => x.QueryField);
await _collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(index));

// 2. Use projection to reduce data transfer
var projection = Builders<T>.Projection.Include(x => x.Id).Include(x => x.Name);
await _collection.Find(filter).Project(projection).ToListAsync();

// 3. Use pagination instead of loading all
await _collection.Find(filter).Skip(page * 100).Limit(100).ToListAsync();
```

---

## WORM Violation Errors

### InvalidOperationException: WORM Store Append-Only

**Symptom:**
```
System.InvalidOperationException: Document with ID xyz already exists. WORM store is append-only.
```

**Why It Happens:**
Attempting to re-insert a document that already exists in a WORM (Write-Once-Read-Many) collection.

**This is Expected Behavior:**
The error protects ledger integrity. Don't try to work around it.

**Proper Handling:**

```csharp
try
{
    await _wormStore.AppendAsync(transaction, ct);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("WORM store"))
{
    _logger.LogWarning("Transaction {TxId} already exists, skipping", tx.TxId);
    // This is normal during replay/sync scenarios
}
```

---

## Error Handling Pattern

### Repository-Level Error Wrapping

```csharp
public async Task<Register?> GetRegisterAsync(string registerId, CancellationToken ct)
{
    try
    {
        var filter = Builders<Register>.Filter.Eq(r => r.Id, registerId);
        return await _registers.Find(filter).FirstOrDefaultAsync(ct);
    }
    catch (MongoConnectionException ex)
    {
        _logger.LogError(ex, "MongoDB connection failed while getting register {Id}", registerId);
        throw new StorageUnavailableException("Register storage unavailable", ex);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Operation cancelled while getting register {Id}", registerId);
        throw;
    }
}
```

### Integration Test Error Validation

See the **xunit** skill for test patterns.

```csharp
[Fact]
public async Task InsertTransaction_DuplicateId_ThrowsDuplicateException()
{
    // Arrange
    var tx = CreateTestTransaction();
    await _repository.InsertTransactionAsync(tx, CancellationToken.None);

    // Act & Assert
    var act = () => _repository.InsertTransactionAsync(tx, CancellationToken.None);
    await act.Should().ThrowAsync<MongoWriteException>()
        .Where(ex => ex.WriteError.Category == ServerErrorCategory.DuplicateKey);
}
```