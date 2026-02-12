// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Storage;

/// <summary>
/// In-memory implementation of instance storage.
/// Suitable for development and testing. Replace with persistent storage for production.
/// </summary>
public class InMemoryInstanceStore : IInstanceStore
{
    private readonly ConcurrentDictionary<string, Instance> _instances = new();

    /// <inheritdoc/>
    public Task<Instance> CreateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(instance.Id))
        {
            throw new ArgumentException("Instance ID is required", nameof(instance));
        }

        if (!_instances.TryAdd(instance.Id, instance))
        {
            throw new InvalidOperationException($"Instance {instance.Id} already exists");
        }

        return Task.FromResult(instance);
    }

    /// <inheritdoc/>
    public Task<Instance?> GetAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        _instances.TryGetValue(instanceId, out var instance);
        return Task.FromResult(instance);
    }

    /// <inheritdoc/>
    public Task<Instance> UpdateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(instance.Id))
        {
            throw new ArgumentException("Instance ID is required", nameof(instance));
        }

        instance.UpdatedAt = DateTimeOffset.UtcNow;

        if (!_instances.TryGetValue(instance.Id, out _))
        {
            throw new InvalidOperationException($"Instance {instance.Id} not found");
        }

        _instances[instance.Id] = instance;
        return Task.FromResult(instance);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Instance>> GetByBlueprintAsync(
        string blueprintId,
        InstanceState? state = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _instances.Values
            .Where(i => i.BlueprintId == blueprintId);

        if (state.HasValue)
        {
            query = query.Where(i => i.State == state.Value);
        }

        var result = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Instance>> GetByRegisterAsync(
        string registerId,
        InstanceState? state = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _instances.Values
            .Where(i => i.RegisterId == registerId);

        if (state.HasValue)
        {
            query = query.Where(i => i.State == state.Value);
        }

        var result = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Instance>> GetByParticipantWalletAsync(
        string walletAddress,
        InstanceState? state = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _instances.Values
            .Where(i => i.ParticipantWallets.Values.Contains(walletAddress));

        if (state.HasValue)
        {
            query = query.Where(i => i.State == state.Value);
        }

        var result = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_instances.TryRemove(instanceId, out _));
    }
}
