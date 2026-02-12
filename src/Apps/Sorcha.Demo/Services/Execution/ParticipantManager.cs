// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Demo.Models;
using Sorcha.Demo.Services.Api;
using Sorcha.Demo.Services.Storage;

namespace Sorcha.Demo.Services.Execution;

/// <summary>
/// Manages participant wallets for demo execution
/// </summary>
public class ParticipantManager
{
    private readonly WalletApiClient _walletClient;
    private readonly WalletStorage _walletStorage;
    private readonly ILogger<ParticipantManager> _logger;

    public ParticipantManager(
        WalletApiClient walletClient,
        WalletStorage walletStorage,
        ILogger<ParticipantManager> logger)
    {
        _walletClient = walletClient;
        _walletStorage = walletStorage;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all participants have wallets (creates new or loads existing)
    /// </summary>
    public async Task<Dictionary<string, ParticipantContext>> EnsureParticipantWalletsAsync(
        string[] participantIds,
        bool reuseExisting,
        string algorithm = "ED25519",
        CancellationToken ct = default)
    {
        var participants = new Dictionary<string, ParticipantContext>();

        // Try to load existing wallets if reuse requested
        if (reuseExisting && _walletStorage.WalletsExist())
        {
            _logger.LogInformation("Attempting to load existing wallets...");
            var loaded = await _walletStorage.LoadWalletsAsync();

            if (loaded != null)
            {
                // Check if all required participants exist
                var allExist = participantIds.All(id => loaded.ContainsKey(id));

                if (allExist)
                {
                    _logger.LogInformation("All {Count} participant wallets loaded from storage", participantIds.Length);
                    return loaded;
                }
                else
                {
                    _logger.LogWarning("Not all participants found in storage, creating new wallets");
                }
            }
        }

        // Create new wallets
        _logger.LogInformation("Creating new wallets for {Count} participants", participantIds.Length);

        foreach (var participantId in participantIds)
        {
            var participant = await CreateParticipantWalletAsync(participantId, algorithm, ct);
            participants[participantId] = participant;
        }

        // Save wallets to storage
        await _walletStorage.SaveWalletsAsync(participants);

        return participants;
    }

    /// <summary>
    /// Creates a new wallet for a participant
    /// </summary>
    private async Task<ParticipantContext> CreateParticipantWalletAsync(
        string participantId,
        string algorithm,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating wallet for participant: {ParticipantId}", participantId);

        var walletResponse = await _walletClient.CreateWalletAsync(
            name: $"Demo-{participantId}",
            algorithm: algorithm,
            ct: ct);

        if (walletResponse == null)
        {
            throw new InvalidOperationException($"Failed to create wallet for participant: {participantId}");
        }

        var participant = new ParticipantContext
        {
            ParticipantId = participantId,
            Name = participantId,
            WalletAddress = walletResponse.Address,
            Algorithm = walletResponse.Algorithm,
            Mnemonic = walletResponse.Mnemonic, // Store for demo (INSECURE!)
            CreatedAt = walletResponse.CreatedAt,
            ActionsExecuted = 0
        };

        _logger.LogInformation("Created wallet for {ParticipantId}: {Address}",
            participantId,
            walletResponse.Address.Length > 16
                ? walletResponse.Address[..16] + "..."
                : walletResponse.Address);

        return participant;
    }

    /// <summary>
    /// Validates that all required participants have wallets
    /// </summary>
    public bool ValidateParticipants(
        Dictionary<string, ParticipantContext> participants,
        string[] requiredParticipants)
    {
        return requiredParticipants.All(id => participants.ContainsKey(id));
    }

    /// <summary>
    /// Gets participant context by ID
    /// </summary>
    public ParticipantContext? GetParticipant(
        Dictionary<string, ParticipantContext> participants,
        string participantId)
    {
        return participants.TryGetValue(participantId, out var participant)
            ? participant
            : null;
    }
}
