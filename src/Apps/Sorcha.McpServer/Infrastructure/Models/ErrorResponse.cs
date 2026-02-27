// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.McpServer.Infrastructure.Models;

/// <summary>
/// Shared error response model used across all MCP tool implementations
/// for deserializing error payloads from downstream service HTTP responses.
/// </summary>
public sealed class ErrorResponse
{
    public string? Error { get; set; }
}
