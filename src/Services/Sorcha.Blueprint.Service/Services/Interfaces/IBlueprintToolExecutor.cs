// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Service.Models.Chat;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Executes AI tool calls against a blueprint builder.
/// </summary>
public interface IBlueprintToolExecutor
{
    /// <summary>
    /// Executes a tool and returns the result.
    /// </summary>
    /// <param name="toolName">Name of the tool to execute.</param>
    /// <param name="arguments">Tool arguments as JSON.</param>
    /// <param name="builder">Blueprint builder to modify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the tool execution.</returns>
    Task<ToolResult> ExecuteAsync(
        string toolName,
        JsonDocument arguments,
        BlueprintBuilder builder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tool definitions.
    /// </summary>
    IReadOnlyList<ToolDefinition> GetToolDefinitions();
}
