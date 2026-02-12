// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Storage;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;

namespace Sorcha.Validator.Service.Managers;

/// <summary>
/// Manages docket operations (block creation, sealing, chain integrity)
/// NOTE: This component runs in the Validator.Service secured environment
/// with access to encryption keys for cryptographic hashing operations (SHA256).
/// </summary>
public class DocketManager
{
    private readonly IRegisterRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public DocketManager(
        IRegisterRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// Creates a new docket from pending transactions
    /// </summary>
    public async Task<Docket> CreateDocketAsync(
        string registerId,
        List<string> transactionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);
        ArgumentNullException.ThrowIfNull(transactionIds);

        if (transactionIds.Count == 0)
        {
            throw new ArgumentException("Cannot create docket with no transactions", nameof(transactionIds));
        }

        // Get register to determine next docket ID
        var register = await _repository.GetRegisterAsync(registerId, cancellationToken);
        if (register == null)
        {
            throw new InvalidOperationException($"Register {registerId} not found");
        }

        // Next docket ID is current height + 1
        var nextDocketId = (ulong)register.Height + 1;

        // Get previous docket hash (if any)
        var previousHash = string.Empty;
        if (register.Height > 0)
        {
            var previousDocket = await _repository.GetDocketAsync(
                registerId,
                (ulong)register.Height,
                cancellationToken);

            if (previousDocket != null)
            {
                previousHash = previousDocket.Hash;
            }
        }

        // Create docket
        var docket = new Docket
        {
            Id = nextDocketId,
            RegisterId = registerId,
            PreviousHash = previousHash,
            TransactionIds = transactionIds,
            TimeStamp = DateTime.UtcNow,
            State = DocketState.Init
        };

        // Calculate docket hash (using SHA256 - requires secure environment)
        docket.Hash = CalculateDocketHash(docket);

        return docket;
    }

    /// <summary>
    /// Proposes a docket for consensus
    /// </summary>
    public async Task<Docket> ProposeDocketAsync(
        Docket docket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docket);

        if (docket.State != DocketState.Init)
        {
            throw new InvalidOperationException($"Docket {docket.Id} is not in Init state");
        }

        docket.State = DocketState.Proposed;
        docket.TimeStamp = DateTime.UtcNow;

        return docket;
    }

    /// <summary>
    /// Seals a docket after consensus approval
    /// </summary>
    public async Task<Docket> SealDocketAsync(
        Docket docket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docket);

        if (docket.State != DocketState.Accepted && docket.State != DocketState.Proposed)
        {
            throw new InvalidOperationException($"Docket {docket.Id} is not in Accepted or Proposed state");
        }

        // Mark docket as sealed
        docket.State = DocketState.Sealed;
        docket.TimeStamp = DateTime.UtcNow;

        // Store docket
        var sealedDocket = await _repository.InsertDocketAsync(docket, cancellationToken);

        // Update register height atomically
        await _repository.UpdateRegisterHeightAsync(
            docket.RegisterId,
            (uint)docket.Id,
            cancellationToken);

        // Update block number for all transactions in this docket
        foreach (var txId in docket.TransactionIds)
        {
            var transaction = await _repository.GetTransactionAsync(
                docket.RegisterId,
                txId,
                cancellationToken);

            if (transaction != null)
            {
                transaction.DocketNumber = docket.Id;
                // Note: In a real implementation, you'd need an UpdateTransaction method
            }
        }

        // Publish docket confirmed event
        await _eventPublisher.PublishAsync(
            "docket:sealed",
            new DocketConfirmedEvent
            {
                RegisterId = sealedDocket.RegisterId,
                DocketId = sealedDocket.Id,
                TransactionIds = sealedDocket.TransactionIds,
                Hash = sealedDocket.Hash,
                TimeStamp = sealedDocket.TimeStamp
            },
            cancellationToken);

        // Publish register height updated event
        var register = await _repository.GetRegisterAsync(docket.RegisterId, cancellationToken);
        if (register != null)
        {
            await _eventPublisher.PublishAsync(
                "register:height-updated",
                new RegisterHeightUpdatedEvent
                {
                    RegisterId = register.Id,
                    OldHeight = register.Height - 1,
                    NewHeight = register.Height,
                    UpdatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        return sealedDocket;
    }

    /// <summary>
    /// Gets a docket by ID
    /// </summary>
    public async Task<Docket?> GetDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);
        return await _repository.GetDocketAsync(registerId, docketId, cancellationToken);
    }

    /// <summary>
    /// Gets all dockets for a register
    /// </summary>
    public async Task<IEnumerable<Docket>> GetDocketsAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);
        return await _repository.GetDocketsAsync(registerId, cancellationToken);
    }

    /// <summary>
    /// Gets dockets in a height range
    /// </summary>
    public async Task<IEnumerable<Docket>> GetDocketRangeAsync(
        string registerId,
        ulong startHeight,
        ulong endHeight,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        var allDockets = await _repository.GetDocketsAsync(registerId, cancellationToken);
        return allDockets
            .Where(d => d.Id >= startHeight && d.Id <= endHeight)
            .OrderBy(d => d.Id);
    }

    /// <summary>
    /// Calculates the hash of a docket using SHA256
    /// SECURITY: This method performs cryptographic operations and must run in a secure environment
    /// </summary>
    private string CalculateDocketHash(Docket docket)
    {
        // Create a deterministic JSON representation
        var hashInput = new
        {
            docket.Id,
            docket.RegisterId,
            docket.PreviousHash,
            TransactionIds = docket.TransactionIds.OrderBy(t => t).ToList(),
            TimeStamp = docket.TimeStamp.ToString("O") // ISO 8601 format
        };

        var json = JsonSerializer.Serialize(hashInput);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies the hash of a docket
    /// SECURITY: This method performs cryptographic operations and must run in a secure environment
    /// </summary>
    public bool VerifyDocketHash(Docket docket)
    {
        ArgumentNullException.ThrowIfNull(docket);

        var calculatedHash = CalculateDocketHash(docket);
        return calculatedHash.Equals(docket.Hash, StringComparison.OrdinalIgnoreCase);
    }
}
