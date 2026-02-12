// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for resolving blueprints and actions
/// </summary>
public interface IActionResolverService
{
    /// <summary>
    /// Retrieves a blueprint from the repository or cache
    /// </summary>
    /// <param name="blueprintId">The blueprint ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blueprint if found, otherwise null</returns>
    Task<Sorcha.Blueprint.Models.Blueprint?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts an action definition from a blueprint
    /// </summary>
    /// <param name="blueprint">The blueprint</param>
    /// <param name="actionId">The action ID</param>
    /// <returns>The action if found, otherwise null</returns>
    Sorcha.Blueprint.Models.Action? GetActionDefinition(Sorcha.Blueprint.Models.Blueprint blueprint, string actionId);

    /// <summary>
    /// Resolves participant IDs to wallet addresses
    /// </summary>
    /// <param name="blueprint">The blueprint</param>
    /// <param name="participantIds">The participant IDs to resolve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping participant IDs to wallet addresses</returns>
    Task<Dictionary<string, string>> ResolveParticipantWalletsAsync(
        Sorcha.Blueprint.Models.Blueprint blueprint,
        IEnumerable<string> participantIds,
        CancellationToken cancellationToken = default);
}
