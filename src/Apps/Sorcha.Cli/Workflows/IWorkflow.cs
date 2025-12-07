// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Cli.UI;

namespace Sorcha.Cli.Workflows;

/// <summary>
/// Interface for workflow implementations
/// </summary>
public interface IWorkflow
{
    string Name { get; }
    string Description { get; }
    IEnumerable<string> StepNames { get; }

    Task ExecuteAsync(WorkflowProgress progress, ActivityLog activityLog, CancellationToken ct = default);
}
