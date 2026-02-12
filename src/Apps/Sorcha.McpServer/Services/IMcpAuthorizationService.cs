// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.McpServer.Services;

/// <summary>
/// Handles role-based access control for MCP tools.
/// </summary>
public interface IMcpAuthorizationService
{
    /// <summary>
    /// Checks if the current session can invoke the specified tool.
    /// </summary>
    /// <param name="toolName">The name of the tool to check.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    bool CanInvokeTool(string toolName);

    /// <summary>
    /// Gets the list of tools available to the current session.
    /// </summary>
    /// <returns>List of authorized tool names.</returns>
    IReadOnlyList<string> GetAuthorizedTools();
}
