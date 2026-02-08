// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.Core.Models.Common;
using Sorcha.UI.Core.Models.Workflows;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Service for managing workflow instances and user actions.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Gets workflow instances for the current user.
    /// </summary>
    Task<PaginatedList<WorkflowInstanceViewModel>> GetMyWorkflowsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific workflow instance.
    /// </summary>
    Task<WorkflowInstanceViewModel?> GetWorkflowAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending actions for the current user.
    /// </summary>
    Task<List<PendingActionViewModel>> GetPendingActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits action data for a workflow step.
    /// </summary>
    Task<bool> SubmitActionAsync(ActionSubmissionViewModel submission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an action with a reason.
    /// </summary>
    Task<bool> RejectActionAsync(string instanceId, string actionId, string reason, CancellationToken cancellationToken = default);
}
