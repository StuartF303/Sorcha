// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// Definition of a tool that can be used by the AI.
/// </summary>
public record ToolDefinition
{
    /// <summary>
    /// Unique tool name (e.g., create_blueprint, add_participant).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// JSON Schema for the tool's parameters.
    /// </summary>
    public required JsonDocument InputSchema { get; init; }

    /// <summary>
    /// Creates a tool definition with a typed input schema.
    /// </summary>
    public static ToolDefinition Create(string name, string description, object inputSchema)
    {
        var json = JsonSerializer.Serialize(inputSchema);
        return new ToolDefinition
        {
            Name = name,
            Description = description,
            InputSchema = JsonDocument.Parse(json)
        };
    }
}
