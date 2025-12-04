// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for reconstructing accumulated state from prior workflow transactions.
/// Fetches and decrypts transaction payloads to build the context needed for action execution.
/// </summary>
public interface IStateReconstructionService
{
    /// <summary>
    /// Reconstructs the accumulated state from prior transactions in a workflow instance.
    /// Only fetches transactions needed for the current action (blueprint-defined scope).
    /// </summary>
    /// <param name="blueprint">The blueprint definition</param>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="currentActionId">The action being executed</param>
    /// <param name="registerId">The register where transactions are stored</param>
    /// <param name="delegationToken">The credential token for delegated decrypt access</param>
    /// <param name="participantWallets">Mapping of participant IDs to wallet addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The accumulated state from prior actions</returns>
    Task<AccumulatedState> ReconstructAsync(
        Sorcha.Blueprint.Models.Blueprint blueprint,
        string instanceId,
        int currentActionId,
        string registerId,
        string delegationToken,
        Dictionary<string, string> participantWallets,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstructs state for a specific branch in a parallel workflow.
    /// </summary>
    /// <param name="blueprint">The blueprint definition</param>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="currentActionId">The action being executed</param>
    /// <param name="branchId">The branch ID</param>
    /// <param name="registerId">The register where transactions are stored</param>
    /// <param name="delegationToken">The credential token for delegated decrypt access</param>
    /// <param name="participantWallets">Mapping of participant IDs to wallet addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The accumulated state for the branch</returns>
    Task<AccumulatedState> ReconstructForBranchAsync(
        Sorcha.Blueprint.Models.Blueprint blueprint,
        string instanceId,
        int currentActionId,
        string branchId,
        string registerId,
        string delegationToken,
        Dictionary<string, string> participantWallets,
        CancellationToken cancellationToken = default);
}
