// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Manages memory pools for pending transactions with FIFO + priority queuing
/// </summary>
public class MemPoolManager : IMemPoolManager
{
    private readonly MemPoolConfiguration _config;
    private readonly ILogger<MemPoolManager> _logger;

    // Per-register memory pools
    private readonly ConcurrentDictionary<string, RegisterMemoryPool> _memoryPools = new();

    public MemPoolManager(
        IOptions<MemPoolConfiguration> config,
        ILogger<MemPoolManager> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a transaction to the memory pool
    /// </summary>
    public Task<bool> AddTransactionAsync(string registerId, Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
            throw new ArgumentException("Register ID is required", nameof(registerId));

        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        var pool = GetOrCreatePool(registerId);

        // Check if transaction already exists
        if (pool.ContainsTransaction(transaction.TransactionId))
        {
            _logger.LogWarning("Transaction {TransactionId} already exists in memory pool for register {RegisterId}",
                transaction.TransactionId, registerId);
            return Task.FromResult(false);
        }

        // Check capacity
        if (pool.IsFull && transaction.Priority != TransactionPriority.High)
        {
            // Evict oldest low/normal priority transaction
            if (!pool.EvictOldest())
            {
                _logger.LogWarning("Memory pool for register {RegisterId} is full and cannot evict", registerId);
                return Task.FromResult(false);
            }

            pool.IncrementEvictions();
        }

        // Check high-priority quota
        if (transaction.Priority == TransactionPriority.High)
        {
            var highPriorityQuota = (int)(_config.MaxSize * _config.HighPriorityQuota);
            if (pool.HighPriorityCount >= highPriorityQuota)
            {
                _logger.LogWarning("High-priority quota exceeded for register {RegisterId}. Downgrading to normal priority.",
                    registerId);
                transaction.Priority = TransactionPriority.Normal;
            }
        }

        // Set added timestamp
        transaction.AddedToPoolAt = DateTimeOffset.UtcNow;

        // Add to appropriate queue
        pool.AddTransaction(transaction);

        _logger.LogInformation("Added transaction {TransactionId} to memory pool for register {RegisterId} with priority {Priority}",
            transaction.TransactionId, registerId, transaction.Priority);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Removes a transaction from the memory pool
    /// </summary>
    public Task<bool> RemoveTransactionAsync(string registerId, string transactionId, CancellationToken cancellationToken = default)
    {
        if (!_memoryPools.TryGetValue(registerId, out var pool))
            return Task.FromResult(false);

        var removed = pool.RemoveTransaction(transactionId);

        if (removed)
        {
            _logger.LogInformation("Removed transaction {TransactionId} from memory pool for register {RegisterId}",
                transactionId, registerId);
        }

        return Task.FromResult(removed);
    }

    /// <summary>
    /// Gets pending transactions for docket building (ordered by priority then FIFO)
    /// </summary>
    public Task<List<Transaction>> GetPendingTransactionsAsync(string registerId, int maxCount, CancellationToken cancellationToken = default)
    {
        if (!_memoryPools.TryGetValue(registerId, out var pool))
            return Task.FromResult(new List<Transaction>());

        var transactions = pool.GetTransactions(maxCount);

        _logger.LogDebug("Retrieved {Count} transactions from memory pool for register {RegisterId}",
            transactions.Count, registerId);

        return Task.FromResult(transactions);
    }

    /// <summary>
    /// Gets the transaction count for a register
    /// </summary>
    public Task<int> GetTransactionCountAsync(string registerId, CancellationToken cancellationToken = default)
    {
        if (!_memoryPools.TryGetValue(registerId, out var pool))
            return Task.FromResult(0);

        return Task.FromResult(pool.TotalCount);
    }

    /// <summary>
    /// Gets memory pool statistics
    /// </summary>
    public Task<MemPoolStats> GetStatsAsync(string registerId, CancellationToken cancellationToken = default)
    {
        if (!_memoryPools.TryGetValue(registerId, out var pool))
        {
            return Task.FromResult(new MemPoolStats
            {
                RegisterId = registerId,
                MaxSize = _config.MaxSize
            });
        }

        var stats = new MemPoolStats
        {
            RegisterId = registerId,
            TotalTransactions = pool.TotalCount,
            HighPriorityCount = pool.HighPriorityCount,
            NormalPriorityCount = pool.NormalPriorityCount,
            LowPriorityCount = pool.LowPriorityCount,
            MaxSize = _config.MaxSize,
            TotalEvictions = pool.TotalEvictions,
            TotalExpired = pool.TotalExpired,
            OldestTransactionTime = pool.OldestTransactionTime
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Cleans up expired transactions from all memory pools
    /// </summary>
    public Task CleanupExpiredTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var totalExpired = 0;

        foreach (var (registerId, pool) in _memoryPools)
        {
            var expired = pool.RemoveExpiredTransactions(now);
            totalExpired += expired;

            if (expired > 0)
            {
                _logger.LogInformation("Removed {Count} expired transactions from memory pool for register {RegisterId}",
                    expired, registerId);
            }
        }

        if (totalExpired > 0)
        {
            _logger.LogInformation("Cleanup removed {TotalExpired} expired transactions across all memory pools", totalExpired);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns transactions to the memory pool (after failed consensus)
    /// </summary>
    public async Task ReturnTransactionsAsync(string registerId, List<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        if (transactions == null || transactions.Count == 0)
            return;

        _logger.LogInformation("Returning {Count} transactions to memory pool for register {RegisterId}",
            transactions.Count, registerId);

        foreach (var transaction in transactions)
        {
            // Preserve original timestamp and priority
            await AddTransactionAsync(registerId, transaction, cancellationToken);
        }
    }

    private RegisterMemoryPool GetOrCreatePool(string registerId)
    {
        return _memoryPools.GetOrAdd(registerId, _ => new RegisterMemoryPool(_config.MaxSize, _logger));
    }

    /// <summary>
    /// Internal class managing a single register's memory pool with priority queues
    /// </summary>
    private class RegisterMemoryPool
    {
        private readonly int _maxSize;
        private readonly ILogger _logger;

        // Priority queues
        private readonly ConcurrentQueue<Transaction> _highPriorityQueue = new();
        private readonly ConcurrentQueue<Transaction> _normalPriorityQueue = new();
        private readonly ConcurrentQueue<Transaction> _lowPriorityQueue = new();

        // Transaction lookup for quick existence checks
        private readonly ConcurrentDictionary<string, Transaction> _transactionLookup = new();

        // Statistics
        private int _totalEvictions;
        private int _totalExpired;

        public RegisterMemoryPool(int maxSize, ILogger logger)
        {
            _maxSize = maxSize;
            _logger = logger;
        }

        public int TotalCount => _transactionLookup.Count;
        public int HighPriorityCount => _highPriorityQueue.Count;
        public int NormalPriorityCount => _normalPriorityQueue.Count;
        public int LowPriorityCount => _lowPriorityQueue.Count;
        public bool IsFull => TotalCount >= _maxSize;
        public int TotalEvictions => _totalEvictions;
        public int TotalExpired => _totalExpired;

        public DateTimeOffset? OldestTransactionTime
        {
            get
            {
                if (_transactionLookup.Values.Any())
                {
                    return _transactionLookup.Values.Min(t => t.AddedToPoolAt);
                }
                return null;
            }
        }

        public bool ContainsTransaction(string transactionId) => _transactionLookup.ContainsKey(transactionId);

        public void AddTransaction(Transaction transaction)
        {
            _transactionLookup[transaction.TransactionId] = transaction;

            switch (transaction.Priority)
            {
                case TransactionPriority.High:
                    _highPriorityQueue.Enqueue(transaction);
                    break;
                case TransactionPriority.Normal:
                    _normalPriorityQueue.Enqueue(transaction);
                    break;
                case TransactionPriority.Low:
                    _lowPriorityQueue.Enqueue(transaction);
                    break;
            }
        }

        public bool RemoveTransaction(string transactionId)
        {
            return _transactionLookup.TryRemove(transactionId, out _);
        }

        public List<Transaction> GetTransactions(int maxCount)
        {
            var transactions = new List<Transaction>();

            // Get high-priority first
            while (transactions.Count < maxCount && _highPriorityQueue.TryPeek(out var tx))
            {
                if (_transactionLookup.ContainsKey(tx.TransactionId))
                {
                    transactions.Add(tx);
                }
                _highPriorityQueue.TryDequeue(out _);
            }

            // Then normal priority
            while (transactions.Count < maxCount && _normalPriorityQueue.TryPeek(out var tx))
            {
                if (_transactionLookup.ContainsKey(tx.TransactionId))
                {
                    transactions.Add(tx);
                }
                _normalPriorityQueue.TryDequeue(out _);
            }

            // Then low priority
            while (transactions.Count < maxCount && _lowPriorityQueue.TryPeek(out var tx))
            {
                if (_transactionLookup.ContainsKey(tx.TransactionId))
                {
                    transactions.Add(tx);
                }
                _lowPriorityQueue.TryDequeue(out _);
            }

            return transactions;
        }

        public bool EvictOldest()
        {
            // Try to evict from low priority first, then normal
            if (_lowPriorityQueue.TryDequeue(out var lowTx))
            {
                _transactionLookup.TryRemove(lowTx.TransactionId, out _);
                return true;
            }

            if (_normalPriorityQueue.TryDequeue(out var normalTx))
            {
                _transactionLookup.TryRemove(normalTx.TransactionId, out _);
                return true;
            }

            return false;
        }

        public void IncrementEvictions() => Interlocked.Increment(ref _totalEvictions);

        public int RemoveExpiredTransactions(DateTimeOffset now)
        {
            var expiredIds = _transactionLookup.Values
                .Where(t => t.ExpiresAt.HasValue && t.ExpiresAt.Value < now)
                .Select(t => t.TransactionId)
                .ToList();

            foreach (var id in expiredIds)
            {
                if (_transactionLookup.TryRemove(id, out _))
                {
                    Interlocked.Increment(ref _totalExpired);
                }
            }

            return expiredIds.Count;
        }
    }
}
