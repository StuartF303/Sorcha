// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// A request from the AI to execute a blueprint operation.
/// </summary>
public record ToolCall
{
    /// <summary>
    /// Tool call identifier (from AI provider).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the tool to execute.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Tool parameters as JSON.
    /// </summary>
    public required JsonDocument Arguments { get; init; }
}
