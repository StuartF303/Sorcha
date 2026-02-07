// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Data;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Background service that manages per-register replication lifecycle.
/// Orchestrates sync state transitions for each subscribed register:
/// - ForwardOnly: Subscribing → Active
/// - FullReplica: Subscribing → Syncing → FullyReplicated
/// </summary>
public class RegisterSyncBackgroundService : BackgroundService
{
    private readonly ILogger<RegisterSyncBackgroundService> _logger;
    private readonly RegisterReplicationService _replicationService;
    private readonly IDbContextFactory<PeerDbContext>? _dbContextFactory;
    private readonly RegisterSyncConfiguration _syncConfig;
    private readonly Dictionary<string, RegisterSubscription> _subscriptions = new();

    public RegisterSyncBackgroundService(
        ILogger<RegisterSyncBackgroundService> logger,
        RegisterReplicationService replicationService,
        IOptions<PeerServiceConfiguration> configuration,
        IDbContextFactory<PeerDbContext>? dbContextFactory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _replicationService = replicationService ?? throw new ArgumentNullException(nameof(replicationService));
        _dbContextFactory = dbContextFactory;
        _syncConfig = configuration?.Value?.RegisterSync ?? new RegisterSyncConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RegisterSyncBackgroundService starting");

        // Load existing subscriptions from database
        await LoadSubscriptionsAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_syncConfig.PeriodicSyncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RegisterSyncBackgroundService loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("RegisterSyncBackgroundService stopped");
    }

    /// <summary>
    /// Adds a new register subscription.
    /// </summary>
    public async Task<RegisterSubscription> SubscribeToRegisterAsync(
        string registerId,
        ReplicationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (_subscriptions.ContainsKey(registerId))
        {
            _logger.LogWarning("Already subscribed to register {RegisterId}", registerId);
            return _subscriptions[registerId];
        }

        var subscription = new RegisterSubscription
        {
            RegisterId = registerId,
            Mode = mode,
            SyncState = RegisterSyncState.Subscribing
        };

        _subscriptions[registerId] = subscription;
        await PersistSubscriptionAsync(subscription, cancellationToken);

        _logger.LogInformation(
            "Subscribed to register {RegisterId} with mode {Mode}",
            registerId, mode);

        return subscription;
    }

    /// <summary>
    /// Removes a register subscription.
    /// </summary>
    public async Task UnsubscribeFromRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.Remove(registerId))
        {
            await DeleteSubscriptionAsync(registerId, cancellationToken);
            _logger.LogInformation("Unsubscribed from register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Gets all current subscriptions.
    /// </summary>
    public IReadOnlyCollection<RegisterSubscription> GetSubscriptions()
    {
        return _subscriptions.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a specific subscription by register ID.
    /// </summary>
    public RegisterSubscription? GetSubscription(string registerId)
    {
        _subscriptions.TryGetValue(registerId, out var sub);
        return sub;
    }

    private async Task ProcessSubscriptionsAsync(CancellationToken cancellationToken)
    {
        foreach (var (registerId, subscription) in _subscriptions)
        {
            try
            {
                await ProcessSubscriptionAsync(subscription, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing subscription for register {RegisterId}",
                    registerId);
                subscription.RecordSyncFailure(ex.Message);
            }
        }
    }

    private async Task ProcessSubscriptionAsync(
        RegisterSubscription subscription,
        CancellationToken cancellationToken)
    {
        switch (subscription.SyncState)
        {
            case RegisterSyncState.Subscribing:
                // Transition based on mode
                subscription.TransitionToNextState();
                await PersistSubscriptionAsync(subscription, cancellationToken);
                break;

            case RegisterSyncState.Syncing:
                // Full replica mode: pull docket chain
                var result = await _replicationService.PullFullReplicaAsync(subscription, cancellationToken);
                if (result.Success)
                {
                    subscription.TransitionToNextState();
                    _logger.LogInformation(
                        "Register {RegisterId} fully replicated ({Dockets} dockets, {Txs} transactions)",
                        subscription.RegisterId, result.DocketsSynced, result.TransactionsSynced);
                }
                await PersistSubscriptionAsync(subscription, cancellationToken);
                break;

            case RegisterSyncState.FullyReplicated:
            case RegisterSyncState.Active:
                // Subscribe to live transactions (no-op if already streaming)
                // The live subscription runs as a long-lived stream
                break;

            case RegisterSyncState.Error:
                // Retry after cooldown
                if (subscription.ConsecutiveFailures < _syncConfig.MaxRetryAttempts)
                {
                    _logger.LogInformation(
                        "Retrying register {RegisterId} after {Failures} failures",
                        subscription.RegisterId, subscription.ConsecutiveFailures);
                    subscription.SyncState = RegisterSyncState.Subscribing;
                    await PersistSubscriptionAsync(subscription, cancellationToken);
                }
                break;
        }
    }

    private async Task LoadSubscriptionsAsync(CancellationToken cancellationToken)
    {
        if (_dbContextFactory == null) return;

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entities = await context.RegisterSubscriptions.ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var sub = entity.ToDomain();
                _subscriptions[sub.RegisterId] = sub;
            }

            _logger.LogInformation("Loaded {Count} register subscriptions from database",
                entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading register subscriptions from database");
        }
    }

    private async Task PersistSubscriptionAsync(
        RegisterSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (_dbContextFactory == null) return;

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = RegisterSubscriptionEntity.FromDomain(subscription);

            var existing = await context.RegisterSubscriptions
                .FirstOrDefaultAsync(s => s.RegisterId == subscription.RegisterId, cancellationToken);

            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                context.RegisterSubscriptions.Add(entity);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting subscription for register {RegisterId}",
                subscription.RegisterId);
        }
    }

    private async Task DeleteSubscriptionAsync(string registerId, CancellationToken cancellationToken)
    {
        if (_dbContextFactory == null) return;

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await context.RegisterSubscriptions
                .FirstOrDefaultAsync(s => s.RegisterId == registerId, cancellationToken);

            if (entity != null)
            {
                context.RegisterSubscriptions.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription for register {RegisterId}", registerId);
        }
    }
}
