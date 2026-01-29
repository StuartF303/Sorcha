// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;

namespace Sorcha.McpServer.Services;

/// <summary>
/// Handles role-based access control for MCP tools.
/// </summary>
public sealed class McpAuthorizationService : IMcpAuthorizationService
{
    private readonly IMcpSessionService _sessionService;
    private readonly ILogger<McpAuthorizationService> _logger;

    // Role to tool mappings per spec
    private static readonly Dictionary<string, HashSet<string>> RoleToToolsMap = new()
    {
        ["sorcha:admin"] =
        [
            "sorcha_health_check",
            "sorcha_log_query",
            "sorcha_metrics",
            "sorcha_tenant_list",
            "sorcha_tenant_create",
            "sorcha_tenant_update",
            "sorcha_user_list",
            "sorcha_user_manage",
            "sorcha_peer_status",
            "sorcha_validator_status",
            "sorcha_register_stats",
            "sorcha_audit_query",
            "sorcha_token_revoke"
        ],
        ["sorcha:designer"] =
        [
            "sorcha_blueprint_list",
            "sorcha_blueprint_get",
            "sorcha_blueprint_create",
            "sorcha_blueprint_update",
            "sorcha_blueprint_validate",
            "sorcha_blueprint_simulate",
            "sorcha_disclosure_analyze",
            "sorcha_blueprint_diff",
            "sorcha_blueprint_export",
            "sorcha_schema_validate",
            "sorcha_schema_generate",
            "sorcha_jsonlogic_test",
            "sorcha_workflow_instances"
        ],
        ["sorcha:participant"] =
        [
            "sorcha_inbox_list",
            "sorcha_action_details",
            "sorcha_action_submit",
            "sorcha_action_validate",
            "sorcha_transaction_history",
            "sorcha_workflow_status",
            "sorcha_disclosed_data",
            "sorcha_wallet_info",
            "sorcha_wallet_sign",
            "sorcha_register_query"
        ]
    };

    public McpAuthorizationService(
        IMcpSessionService sessionService,
        ILogger<McpAuthorizationService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanInvokeTool(string toolName)
    {
        var session = _sessionService.CurrentSession;
        if (session is null)
        {
            _logger.LogWarning("Authorization check failed: no active session");
            return false;
        }

        if (_sessionService.IsTokenExpired())
        {
            _logger.LogWarning("Authorization check failed: token expired for user {UserId}", session.UserId);
            return false;
        }

        foreach (var role in session.Roles)
        {
            if (RoleToToolsMap.TryGetValue(role, out var tools) && tools.Contains(toolName))
            {
                _logger.LogDebug("User {UserId} authorized for tool {ToolName} via role {Role}",
                    session.UserId, toolName, role);
                return true;
            }
        }

        _logger.LogWarning("User {UserId} denied access to tool {ToolName} - missing required role",
            session.UserId, toolName);
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAuthorizedTools()
    {
        var session = _sessionService.CurrentSession;
        if (session is null || _sessionService.IsTokenExpired())
        {
            return [];
        }

        var authorizedTools = new HashSet<string>();
        foreach (var role in session.Roles)
        {
            if (RoleToToolsMap.TryGetValue(role, out var tools))
            {
                authorizedTools.UnionWith(tools);
            }
        }

        return authorizedTools.Order().ToList();
    }
}
