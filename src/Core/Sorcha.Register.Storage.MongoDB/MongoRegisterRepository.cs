// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Sorcha.Register.Core.Storage;
using Sorcha.Register.Models;

namespace Sorcha.Register.Storage.MongoDB;

/// <summary>
/// MongoDB implementation of IRegisterRepository.
/// Provides document storage for registers, transactions, and dockets.
/// </summary>
public class MongoRegisterRepository : IRegisterRepository
{
    private readonly IMongoCollection<Models.Register> _registers;
    private readonly IMongoCollection<TransactionModel> _transactions;
    private readonly IMongoCollection<Docket> _dockets;
    private readonly ILogger<MongoRegisterRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the MongoRegisterRepository.
    /// </summary>
    public MongoRegisterRepository(
        IOptions<MongoRegisterStorageConfiguration> options,
        ILogger<MongoRegisterRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options?.Value);

        var config = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var client = new MongoClient(config.ConnectionString);
        var database = client.GetDatabase(config.DatabaseName);

        _registers = database.GetCollection<Models.Register>(config.RegisterCollectionName);
        _transactions = database.GetCollection<TransactionModel>(config.TransactionCollectionName);
        _dockets = database.GetCollection<Docket>(config.DocketCollectionName);

        if (config.CreateIndexesOnStartup)
        {
            CreateIndexesAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Initializes a new instance for testing with explicit database.
    /// </summary>
    public MongoRegisterRepository(
        IMongoDatabase database,
        string registerCollection,
        string transactionCollection,
        string docketCollection,
        ILogger<MongoRegisterRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(database);

        _registers = database.GetCollection<Models.Register>(registerCollection);
        _transactions = database.GetCollection<TransactionModel>(transactionCollection);
        _dockets = database.GetCollection<Docket>(docketCollection);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates indexes for optimal query performance.
    /// </summary>
    private async Task CreateIndexesAsync()
    {
        _logger.LogInformation("Creating MongoDB indexes for Register storage");

        // Register indexes
        var registerIndexes = new List<CreateIndexModel<Models.Register>>
        {
            new(Builders<Models.Register>.IndexKeys.Ascending(r => r.TenantId)),
            new(Builders<Models.Register>.IndexKeys.Ascending(r => r.Status)),
            new(Builders<Models.Register>.IndexKeys.Ascending(r => r.Name))
        };
        await _registers.Indexes.CreateManyAsync(registerIndexes);

        // Transaction indexes
        var transactionIndexes = new List<CreateIndexModel<TransactionModel>>
        {
            // Composite index for register + txId lookups
            new(Builders<TransactionModel>.IndexKeys
                .Ascending(t => t.RegisterId)
                .Ascending(t => t.TxId),
                new CreateIndexOptions { Unique = true }),

            // Index for sender address queries
            new(Builders<TransactionModel>.IndexKeys
                .Ascending(t => t.RegisterId)
                .Ascending(t => t.SenderWallet)),

            // Index for timestamp-based queries
            new(Builders<TransactionModel>.IndexKeys
                .Ascending(t => t.RegisterId)
                .Descending(t => t.TimeStamp)),

            // Index for block number queries
            new(Builders<TransactionModel>.IndexKeys
                .Ascending(t => t.RegisterId)
                .Ascending(t => t.BlockNumber)),

            // Index for blueprint queries
            new(Builders<TransactionModel>.IndexKeys
                .Ascending("MetaData.BlueprintId")
                .Ascending("MetaData.InstanceId"))
        };
        await _transactions.Indexes.CreateManyAsync(transactionIndexes);

        // Docket indexes
        var docketIndexes = new List<CreateIndexModel<Docket>>
        {
            // Composite index for register + docket ID lookups
            new(Builders<Docket>.IndexKeys
                .Ascending(d => d.RegisterId)
                .Ascending(d => d.Id),
                new CreateIndexOptions { Unique = true }),

            // Index for hash lookups
            new(Builders<Docket>.IndexKeys.Ascending(d => d.Hash)),

            // Index for state queries
            new(Builders<Docket>.IndexKeys
                .Ascending(d => d.RegisterId)
                .Ascending(d => d.State))
        };
        await _dockets.Indexes.CreateManyAsync(docketIndexes);

        _logger.LogInformation("MongoDB indexes created successfully");
    }

    // ===========================
    // Register Operations
    // ===========================

    /// <inheritdoc/>
    public async Task<bool> IsLocalRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Models.Register>.Filter.Eq(r => r.Id, registerId);
        var count = await _registers.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Models.Register>> GetRegistersAsync(CancellationToken cancellationToken = default)
    {
        return await _registers.Find(FilterDefinition<Models.Register>.Empty)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Models.Register>> QueryRegistersAsync(
        Func<Models.Register, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        // For simple predicates, we need to fetch and filter in memory
        // For production, consider using expression-based predicates
        var all = await GetRegistersAsync(cancellationToken);
        return all.Where(predicate);
    }

    /// <inheritdoc/>
    public async Task<Models.Register?> GetRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Models.Register>.Filter.Eq(r => r.Id, registerId);
        return await _registers.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Models.Register> InsertRegisterAsync(Models.Register newRegister, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newRegister);

        await _registers.InsertOneAsync(newRegister, new InsertOneOptions(), cancellationToken);
        _logger.LogDebug("Inserted register {RegisterId}", newRegister.Id);
        return newRegister;
    }

    /// <inheritdoc/>
    public async Task<Models.Register> UpdateRegisterAsync(Models.Register register, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(register);

        register.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Models.Register>.Filter.Eq(r => r.Id, register.Id);
        await _registers.ReplaceOneAsync(filter, register, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        _logger.LogDebug("Updated register {RegisterId}", register.Id);
        return register;
    }

    /// <inheritdoc/>
    public async Task DeleteRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        // Delete all associated transactions first
        var txFilter = Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId);
        await _transactions.DeleteManyAsync(txFilter, cancellationToken);

        // Delete all associated dockets
        var docketFilter = Builders<Docket>.Filter.Eq(d => d.RegisterId, registerId);
        await _dockets.DeleteManyAsync(docketFilter, cancellationToken);

        // Delete the register
        var registerFilter = Builders<Models.Register>.Filter.Eq(r => r.Id, registerId);
        await _registers.DeleteOneAsync(registerFilter, cancellationToken);

        _logger.LogInformation("Deleted register {RegisterId} with all associated data", registerId);
    }

    /// <inheritdoc/>
    public async Task<int> CountRegistersAsync(CancellationToken cancellationToken = default)
    {
        var count = await _registers.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
        return (int)count;
    }

    // ===========================
    // Docket Operations
    // ===========================

    /// <inheritdoc/>
    public async Task<IEnumerable<Docket>> GetDocketsAsync(string registerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Docket>.Filter.Eq(d => d.RegisterId, registerId);
        return await _dockets.Find(filter)
            .SortBy(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Docket?> GetDocketAsync(string registerId, ulong docketId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Docket>.Filter.And(
            Builders<Docket>.Filter.Eq(d => d.RegisterId, registerId),
            Builders<Docket>.Filter.Eq(d => d.Id, docketId));

        return await _dockets.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Docket> InsertDocketAsync(Docket docket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docket);

        await _dockets.InsertOneAsync(docket, new InsertOneOptions(), cancellationToken);
        _logger.LogDebug("Inserted docket {DocketId} for register {RegisterId}", docket.Id, docket.RegisterId);
        return docket;
    }

    /// <inheritdoc/>
    public async Task UpdateRegisterHeightAsync(string registerId, uint newHeight, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Models.Register>.Filter.Eq(r => r.Id, registerId);
        var update = Builders<Models.Register>.Update
            .Set(r => r.Height, newHeight)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        await _registers.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        _logger.LogDebug("Updated register {RegisterId} height to {Height}", registerId, newHeight);
    }

    // ===========================
    // Transaction Operations
    // ===========================

    /// <inheritdoc/>
    public async Task<IQueryable<TransactionModel>> GetTransactionsAsync(string registerId, CancellationToken cancellationToken = default)
    {
        // Return MongoDB LINQ queryable for deferred execution
        var filter = Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId);
        var transactions = await _transactions.Find(filter).ToListAsync(cancellationToken);
        return transactions.AsQueryable();
    }

    /// <inheritdoc/>
    public async Task<TransactionModel?> GetTransactionAsync(string registerId, string transactionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TransactionModel>.Filter.And(
            Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId),
            Builders<TransactionModel>.Filter.Eq(t => t.TxId, transactionId));

        return await _transactions.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TransactionModel> InsertTransactionAsync(TransactionModel transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        // Generate DID URI if not set
        if (string.IsNullOrEmpty(transaction.Id))
        {
            transaction.Id = transaction.GenerateDidUri();
        }

        await _transactions.InsertOneAsync(transaction, new InsertOneOptions(), cancellationToken);
        _logger.LogDebug("Inserted transaction {TxId} in register {RegisterId}", transaction.TxId, transaction.RegisterId);
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TransactionModel>> QueryTransactionsAsync(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var registerFilter = Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId);
        var predicateFilter = Builders<TransactionModel>.Filter.Where(predicate);
        var combinedFilter = Builders<TransactionModel>.Filter.And(registerFilter, predicateFilter);

        return await _transactions.Find(combinedFilter).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TransactionModel>> GetTransactionsByDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TransactionModel>.Filter.And(
            Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId),
            Builders<TransactionModel>.Filter.Eq(t => t.BlockNumber, docketId));

        return await _transactions.Find(filter)
            .SortBy(t => t.TimeStamp)
            .ToListAsync(cancellationToken);
    }

    // ===========================
    // Advanced Queries
    // ===========================

    /// <inheritdoc/>
    public async Task<IEnumerable<TransactionModel>> GetAllTransactionsByRecipientAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TransactionModel>.Filter.And(
            Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId),
            Builders<TransactionModel>.Filter.AnyEq(t => t.RecipientsWallets, address));

        return await _transactions.Find(filter)
            .SortByDescending(t => t.TimeStamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TransactionModel>> GetAllTransactionsBySenderAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TransactionModel>.Filter.And(
            Builders<TransactionModel>.Filter.Eq(t => t.RegisterId, registerId),
            Builders<TransactionModel>.Filter.Eq(t => t.SenderWallet, address));

        return await _transactions.Find(filter)
            .SortByDescending(t => t.TimeStamp)
            .ToListAsync(cancellationToken);
    }
}
