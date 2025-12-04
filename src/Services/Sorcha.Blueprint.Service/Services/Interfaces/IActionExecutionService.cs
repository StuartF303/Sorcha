// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Service.Models.Requests;
using Sorcha.Blueprint.Service.Models.Responses;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Orchestrates the execution of workflow actions.
/// Coordinates state reconstruction, validation, routing, transaction building, and notifications.
/// </summary>
public interface IActionExecutionService
{
    /// <summary>
    /// Executes a workflow action with full orchestration:
    /// 1. Fetch prior transactions from Register
    /// 2. Decrypt payloads using delegated access
    /// 3. Reconstruct accumulated state
    /// 4. Validate input data against schema
    /// 5. Evaluate routing conditions (JSON Logic)
    /// 6. Apply calculations
    /// 7. Apply disclosure rules
    /// 8. Build transaction with encrypted payloads
    /// 9. Sign transaction
    /// 10. Submit to Register
    /// 11. Notify participants via SignalR
    /// </summary>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="actionId">The action ID being executed</param>
    /// <param name="request">The action submission request</param>
    /// <param name="delegationToken">The credential token for delegated operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The action submission response</returns>
    Task<ActionSubmissionResponse> ExecuteAsync(
        string instanceId,
        int actionId,
        ActionSubmissionRequest request,
        string delegationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a workflow action, routing to the configured rejection target.
    /// </summary>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="actionId">The action ID being rejected</param>
    /// <param name="request">The rejection request</param>
    /// <param name="delegationToken">The credential token for delegated operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rejection response</returns>
    Task<ActionRejectionResponse> RejectAsync(
        string instanceId,
        int actionId,
        ActionRejectionRequest request,
        string delegationToken,
        CancellationToken cancellationToken = default);
}
