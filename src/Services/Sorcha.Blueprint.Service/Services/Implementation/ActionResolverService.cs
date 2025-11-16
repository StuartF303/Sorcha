// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Sorcha.Blueprint.Service.Services.Interfaces;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;

namespace Sorcha.Blueprint.Service.Services.Implementation;

/// <summary>
/// Service for resolving blueprints and actions
/// </summary>
public class ActionResolverService : IActionResolverService
{
    private readonly IBlueprintStore _blueprintStore;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ActionResolverService> _logger;
    private const int CacheTtlMinutes = 10;

    public ActionResolverService(
        IBlueprintStore blueprintStore,
        IDistributedCache cache,
        ILogger<ActionResolverService> logger)
    {
        _blueprintStore = blueprintStore ?? throw new ArgumentNullException(nameof(blueprintStore));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<BlueprintModel?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blueprintId))
        {
            throw new ArgumentException("Blueprint ID cannot be null or empty", nameof(blueprintId));
        }

        // Try to get from cache first
        var cacheKey = $"blueprint:{blueprintId}";
        var cachedBlueprint = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedBlueprint))
        {
            _logger.LogDebug("Blueprint {BlueprintId} retrieved from cache", blueprintId);
            return JsonSerializer.Deserialize<BlueprintModel>(cachedBlueprint);
        }

        // Get from store
        var blueprint = await _blueprintStore.GetAsync(blueprintId);
        if (blueprint == null)
        {
            _logger.LogWarning("Blueprint {BlueprintId} not found", blueprintId);
            return null;
        }

        // Cache for future requests
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheTtlMinutes)
        };
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(blueprint),
            cacheOptions,
            cancellationToken);

        _logger.LogDebug("Blueprint {BlueprintId} cached for {Minutes} minutes", blueprintId, CacheTtlMinutes);

        return blueprint;
    }

    /// <inheritdoc/>
    public ActionModel? GetActionDefinition(BlueprintModel blueprint, string actionId)
    {
        if (blueprint == null)
        {
            throw new ArgumentNullException(nameof(blueprint));
        }

        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException("Action ID cannot be null or empty", nameof(actionId));
        }

        var action = blueprint.Actions?.FirstOrDefault(a => a.Id == actionId);
        if (action == null)
        {
            _logger.LogWarning("Action {ActionId} not found in blueprint {BlueprintId}", actionId, blueprint.Id);
        }

        return action;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> ResolveParticipantWalletsAsync(
        BlueprintModel blueprint,
        IEnumerable<string> participantIds,
        CancellationToken cancellationToken = default)
    {
        if (blueprint == null)
        {
            throw new ArgumentNullException(nameof(blueprint));
        }

        if (participantIds == null)
        {
            throw new ArgumentNullException(nameof(participantIds));
        }

        var walletMap = new Dictionary<string, string>();
        var participantIdList = participantIds.ToList();

        foreach (var participantId in participantIdList)
        {
            var participant = blueprint.Participants?.FirstOrDefault(p => p.Id == participantId);
            if (participant == null)
            {
                _logger.LogWarning("Participant {ParticipantId} not found in blueprint {BlueprintId}", participantId, blueprint.Id);
                continue;
            }

            // For now, we'll use a placeholder wallet resolution
            // In the future, this would call a Participant/Wallet service to resolve actual wallet addresses
            // For MVP, participants are expected to have wallet addresses in metadata or properties
            var walletAddress = ResolveWalletFromParticipant(participant);
            if (!string.IsNullOrEmpty(walletAddress))
            {
                walletMap[participantId] = walletAddress;
            }
            else
            {
                _logger.LogWarning("Could not resolve wallet for participant {ParticipantId}", participantId);
            }
        }

        await Task.CompletedTask; // For async signature compatibility
        return walletMap;
    }

    private string? ResolveWalletFromParticipant(Sorcha.Blueprint.Models.Participant participant)
    {
        // TODO: This is a placeholder implementation
        // In a full implementation, this would:
        // 1. Check if participant has a wallet address in metadata
        // 2. Query a Participant service to get the wallet
        // 3. Support different participant types (role-based, user-based, etc.)

        // For now, return a placeholder wallet based on participant name
        // This will be replaced when Wallet Service integration is complete
        return $"wallet_{participant.Name?.ToLowerInvariant().Replace(" ", "_") ?? participant.Id}";
    }
}
