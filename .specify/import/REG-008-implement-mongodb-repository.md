# Task: Implement MongoDB Repository

**ID:** REG-008
**Status:** Not Started
**Priority:** Critical
**Estimate:** 12 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Implement the MongoDB storage provider for the RegisterService, providing persistent storage for registers, transactions, and dockets.

## Tasks

### Project Setup
- [ ] Create `Siccar.RegisterService.Storage.MongoDB` project
- [ ] Add NuGet package `MongoDB.Driver` (latest stable)
- [ ] Add NuGet package `Microsoft.Extensions.Options`
- [ ] Add NuGet package `Microsoft.Extensions.Logging`
- [ ] Reference `Siccar.RegisterService` core library
- [ ] Reference `Siccar.Platform` for models

### Configuration Classes
- [ ] Create `MongoDBRepositoryOptions.cs`
- [ ] Define `ConnectionString` property
- [ ] Define `DatabaseName` property
- [ ] Define `RegistersCollectionName` property (default: "LocalRegisters")
- [ ] Define retry policy settings
- [ ] Define timeout settings

### Repository Implementation
- [ ] Create `MongoDBRegisterRepository.cs` class
- [ ] Implement `IRegisterRepository` interface
- [ ] Add constructor with:
  - `IOptions<MongoDBRepositoryOptions>` options
  - `ILogger<MongoDBRegisterRepository>` logger
- [ ] Initialize MongoDB client with connection pooling
- [ ] Create indexes on startup

### MongoDB Client Setup
- [ ] Configure `MongoClientSettings`
- [ ] Enable connection pooling
- [ ] Configure retry reads and writes
- [ ] Add command logging (debug mode)
- [ ] Set timeout values
- [ ] Configure server selection timeout

### Register Operations
- [ ] Implement `GetRegisterAsync(string registerId)`
- [ ] Implement `GetRegistersAsync()`
- [ ] Implement `QueryRegisters(Func<Register, bool> predicate)`
- [ ] Implement `InsertRegisterAsync(Register newRegister)`
  - Insert into LocalRegisters collection
  - Create dedicated collection for register data
- [ ] Implement `UpdateRegisterAsync(Register register)`
- [ ] Implement `DeleteRegisterAsync(string registerId)`
  - Remove from LocalRegisters
  - Drop dedicated collection
- [ ] Implement `IsLocalRegisterAsync(string registerId)`
  - Use in-memory cache for performance
- [ ] Implement `CountRegisters()`

### Transaction Operations
- [ ] Implement `GetTransactionsAsync(string registerId)`
  - Return IQueryable for OData support
- [ ] Implement `GetTransactionAsync(string registerId, string transactionId)`
- [ ] Implement `InsertTransactionAsync(TransactionModel transaction)`
  - Insert into register-specific collection
  - Handle duplicate key errors gracefully
- [ ] Implement `QueryTransactions(string registerId, Expression<Func<TransactionModel, bool>> predicate)`
- [ ] Implement `QueryTransactionPayload(string registerId, Expression<Func<TransactionModel, bool>> predicate)`

### Docket Operations
- [ ] Implement `GetDocketsAsync(string registerId)`
- [ ] Implement `GetDocketAsync(string registerId, ulong docketId)`
- [ ] Implement `InsertDocketAsync(Docket docket)`
  - Insert into register-specific collection
  - Handle duplicate key errors

### Collection Management
- [ ] Implement dynamic collection naming (per register ID)
- [ ] Create indexes on register creation:
  - Transactions: Index on TxId (unique), PrevTxId, MetaData.BlueprintId
  - Dockets: Index on Id (unique), RegisterId
  - Payloads: Index on Hash
- [ ] Implement collection existence check
- [ ] Implement collection creation with validation rules

### Caching Layer
- [ ] Implement in-memory cache for local register IDs
- [ ] Update cache on insert/delete operations
- [ ] Thread-safe cache operations
- [ ] Cache invalidation strategy

### Error Handling
- [ ] Handle MongoDB connection errors
- [ ] Handle duplicate key exceptions
- [ ] Handle timeout exceptions
- [ ] Wrap MongoDB exceptions with custom exceptions
- [ ] Log all errors with context
- [ ] Implement retry logic for transient failures

### Query Optimization
- [ ] Use projection to limit returned fields
- [ ] Implement efficient paging with skip/take
- [ ] Use find vs aggregate appropriately
- [ ] Optimize LINQ queries
- [ ] Add query hints for complex queries

## Implementation Example

```csharp
public class MongoDBRegisterRepository : IRegisterRepository
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Register> _registersCollection;
    private readonly ILogger<MongoDBRegisterRepository> _logger;
    private readonly List<string> _localRegistersCache;
    private readonly object _cacheLock = new object();

    public MongoDBRegisterRepository(
        IOptions<MongoDBRepositoryOptions> options,
        ILogger<MongoDBRegisterRepository> logger)
    {
        _logger = logger;
        var connectionString = options.Value.ConnectionString;
        var databaseName = options.Value.DatabaseName;

        var mongoUrl = new MongoUrl(connectionString);
        var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

        // Configure for production
        clientSettings.MaxConnectionPoolSize = 100;
        clientSettings.MinConnectionPoolSize = 10;
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        clientSettings.RetryReads = true;
        clientSettings.RetryWrites = true;

        // Debug logging
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            clientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    _logger.LogDebug("MongoDB Command: {Command}",
                        e.Command.ToJson());
                });
            };
        }

        _mongoClient = new MongoClient(clientSettings);
        _database = _mongoClient.GetDatabase(databaseName);
        _registersCollection = _database.GetCollection<Register>(
            options.Value.RegistersCollectionName);

        // Initialize cache
        _localRegistersCache = _registersCollection
            .Find(_ => true)
            .Project(r => r.Id)
            .ToList();

        _logger.LogInformation(
            "MongoDB repository initialized with {Count} local registers",
            _localRegistersCache.Count);
    }

    public async Task<Register> InsertRegisterAsync(
        Register newRegister,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create dedicated collection for this register
            var collectionName = newRegister.Id;
            await _database.CreateCollectionAsync(
                collectionName, cancellationToken: cancellationToken);

            // Create indexes
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var indexKeys = Builders<BsonDocument>.IndexKeys;
            await collection.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<BsonDocument>(
                    indexKeys.Ascending("TxId"),
                    new CreateIndexOptions { Unique = true, Name = "idx_txid" }),
                new CreateIndexModel<BsonDocument>(
                    indexKeys.Ascending("PrevTxId"),
                    new CreateIndexOptions { Name = "idx_prevtxid" }),
                new CreateIndexModel<BsonDocument>(
                    indexKeys.Ascending("MetaData.BlueprintId"),
                    new CreateIndexOptions { Name = "idx_blueprintid" })
            }, cancellationToken);

            // Insert register metadata
            await _registersCollection.InsertOneAsync(
                newRegister, cancellationToken: cancellationToken);

            // Update cache
            lock (_cacheLock)
            {
                if (!_localRegistersCache.Contains(newRegister.Id))
                    _localRegistersCache.Add(newRegister.Id);
            }

            _logger.LogInformation(
                "Register {RegisterId} created in MongoDB", newRegister.Id);

            return newRegister;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RegisterAlreadyExistsException(newRegister.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert register {RegisterId}",
                newRegister.Id);
            throw new RegisterException(
                $"Failed to create register: {ex.Message}", ex);
        }
    }

    public async Task<TransactionModel> InsertTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default)
    {
        var registerId = transaction.MetaData?.RegisterId;
        if (string.IsNullOrEmpty(registerId))
            throw new ArgumentException("Transaction must have RegisterId in metadata");

        if (!await IsLocalRegisterAsync(registerId, cancellationToken))
            throw new RegisterNotFoundException(registerId);

        try
        {
            var collection = _database.GetCollection<TransactionModel>(registerId);
            await collection.InsertOneAsync(transaction, cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Transaction {TxId} inserted into register {RegisterId}",
                transaction.TxId, registerId);

            return transaction;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Transaction already exists - this might be normal in distributed system
            _logger.LogWarning(
                "Transaction {TxId} already exists in register {RegisterId}",
                transaction.TxId, registerId);
            return await GetTransactionAsync(registerId, transaction.TxId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to insert transaction {TxId} into register {RegisterId}",
                transaction.TxId, registerId);
            throw new RegisterException(
                $"Failed to store transaction: {ex.Message}", ex);
        }
    }

    public async Task<bool> IsLocalRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            return _localRegistersCache.Contains(registerId);
        }
    }

    public async Task<IQueryable<TransactionModel>> GetTransactionsAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsLocalRegisterAsync(registerId, cancellationToken))
            throw new RegisterNotFoundException(registerId);

        var collection = _database.GetCollection<TransactionModel>(registerId);
        return collection.AsQueryable();
    }

    // Additional methods...
}
```

## Acceptance Criteria

- [ ] All IRegisterRepository methods implemented
- [ ] MongoDB connection pooling configured
- [ ] Indexes created for optimal performance
- [ ] Error handling comprehensive
- [ ] Caching layer working
- [ ] Retry logic implemented
- [ ] Thread-safe operations
- [ ] Integration tests passing

## Definition of Done

- All methods implemented
- Integration tests with MongoDB passing
- Performance benchmarks meet requirements
- Code review approved
- XML documentation complete
- README with configuration examples

---

**Dependencies:** REG-001, REG-003
**Blocks:** REG-025, REG-028
