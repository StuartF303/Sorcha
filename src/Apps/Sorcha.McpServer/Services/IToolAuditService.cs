// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.McpServer.Services;

/// <summary>
/// Records audit logs for tool invocations.
/// </summary>
public interface IToolAuditService
{
    /// <summary>
    /// Records a tool invocation.
    /// </summary>
    /// <param name="record">The audit record to log.</param>
    void RecordInvocation(ToolInvocationRecord record);
}

/// <summary>
/// Represents an audit record for a tool invocation.
/// </summary>
public sealed record ToolInvocationRecord
{
    /// <summary>
    /// Unique identifier for this invocation.
    /// </summary>
    public required string InvocationId { get; init; }

    /// <summary>
    /// The session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The user who invoked the tool.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The name of the tool invoked.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Hash of the input parameters (for privacy).
    /// </summary>
    public string? InputHash { get; init; }

    /// <summary>
    /// Whether the invocation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the invocation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of the invocation.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// When the invocation started.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
