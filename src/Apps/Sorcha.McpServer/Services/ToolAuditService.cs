// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;

namespace Sorcha.McpServer.Services;

/// <summary>
/// Records audit logs for tool invocations using structured logging.
/// </summary>
public sealed class ToolAuditService : IToolAuditService
{
    private readonly ILogger<ToolAuditService> _logger;

    public ToolAuditService(ILogger<ToolAuditService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordInvocation(ToolInvocationRecord record)
    {
        if (record.Success)
        {
            _logger.LogInformation(
                "Tool invocation: {InvocationId} | User: {UserId} | Tool: {ToolName} | Duration: {DurationMs}ms | Success",
                record.InvocationId,
                record.UserId,
                record.ToolName,
                record.Duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Tool invocation failed: {InvocationId} | User: {UserId} | Tool: {ToolName} | Duration: {DurationMs}ms | Error: {ErrorMessage}",
                record.InvocationId,
                record.UserId,
                record.ToolName,
                record.Duration.TotalMilliseconds,
                record.ErrorMessage);
        }
    }
}
