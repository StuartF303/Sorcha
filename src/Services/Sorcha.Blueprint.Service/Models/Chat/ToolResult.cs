// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// The outcome of executing a tool.
/// </summary>
public record ToolResult
{
    /// <summary>
    /// References the ToolCall.Id this is a result for.
    /// </summary>
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Whether execution succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Tool output data (if success).
    /// </summary>
    public JsonDocument? Result { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether the blueprint was modified by this tool execution.
    /// </summary>
    public required bool BlueprintChanged { get; init; }

    /// <summary>
    /// Creates a successful tool result.
    /// </summary>
    public static ToolResult Succeeded(string toolCallId, object result, bool blueprintChanged = false)
    {
        var json = JsonSerializer.Serialize(result);
        return new ToolResult
        {
            ToolCallId = toolCallId,
            Success = true,
            Result = JsonDocument.Parse(json),
            BlueprintChanged = blueprintChanged
        };
    }

    /// <summary>
    /// Creates a failed tool result.
    /// </summary>
    public static ToolResult Failed(string toolCallId, string error)
    {
        return new ToolResult
        {
            ToolCallId = toolCallId,
            Success = false,
            Error = error,
            BlueprintChanged = false
        };
    }
}
