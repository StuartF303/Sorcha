// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;

namespace Sorcha.McpServer.Infrastructure;

/// <summary>
/// Handles error formatting for MCP tool responses.
/// </summary>
public interface IMcpErrorHandler
{
    /// <summary>
    /// Creates an MCP-compatible error response.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <param name="message">User-facing error message.</param>
    /// <param name="exception">Optional exception for logging (not exposed to user).</param>
    /// <returns>An MCP error response object.</returns>
    McpErrorResponse CreateError(McpErrorCategory category, string message, Exception? exception = null);
}

/// <summary>
/// Categories of errors that can occur during MCP tool execution.
/// </summary>
public enum McpErrorCategory
{
    /// <summary>
    /// Authentication failed (invalid or missing JWT token).
    /// </summary>
    Authentication,

    /// <summary>
    /// Authorization failed (user lacks required role).
    /// </summary>
    Authorization,

    /// <summary>
    /// Input validation failed.
    /// </summary>
    Validation,

    /// <summary>
    /// Backend service is unavailable.
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// Rate limit exceeded.
    /// </summary>
    RateLimited,

    /// <summary>
    /// Resource not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Conflict (e.g., concurrent modification).
    /// </summary>
    Conflict,

    /// <summary>
    /// Internal server error.
    /// </summary>
    Internal
}

/// <summary>
/// MCP-compatible error response.
/// </summary>
public sealed record McpErrorResponse
{
    /// <summary>
    /// Indicates this is an error response.
    /// </summary>
    public bool IsError { get; init; } = true;

    /// <summary>
    /// Error category for client handling.
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// User-facing error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Additional details for actionable errors.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Retry-after time in seconds (for rate limiting).
    /// </summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// Suggested action for the user.
    /// </summary>
    public string? SuggestedAction { get; init; }
}

/// <summary>
/// Handles error formatting for MCP tool responses.
/// </summary>
public sealed class McpErrorHandler : IMcpErrorHandler
{
    private readonly ILogger<McpErrorHandler> _logger;

    public McpErrorHandler(ILogger<McpErrorHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public McpErrorResponse CreateError(McpErrorCategory category, string message, Exception? exception = null)
    {
        // Log the full exception internally
        if (exception != null)
        {
            _logger.LogError(exception, "MCP error occurred: {Category} - {Message}", category, message);
        }
        else
        {
            _logger.LogWarning("MCP error: {Category} - {Message}", category, message);
        }

        return category switch
        {
            McpErrorCategory.Authentication => new McpErrorResponse
            {
                ErrorCode = "AUTHENTICATION_REQUIRED",
                Message = message,
                SuggestedAction = "Please provide a valid JWT token via --jwt-token argument or SORCHA_JWT_TOKEN environment variable."
            },

            McpErrorCategory.Authorization => new McpErrorResponse
            {
                ErrorCode = "ACCESS_DENIED",
                Message = message,
                SuggestedAction = "Contact your administrator to request the required permissions."
            },

            McpErrorCategory.Validation => new McpErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = message,
                Details = exception?.Message,
                SuggestedAction = "Check the input parameters and try again."
            },

            McpErrorCategory.ServiceUnavailable => new McpErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = message,
                SuggestedAction = "The backend service is temporarily unavailable. Please try again later."
            },

            McpErrorCategory.RateLimited => new McpErrorResponse
            {
                ErrorCode = "RATE_LIMITED",
                Message = message,
                RetryAfterSeconds = 60,
                SuggestedAction = "You have exceeded the rate limit. Please wait before making more requests."
            },

            McpErrorCategory.NotFound => new McpErrorResponse
            {
                ErrorCode = "NOT_FOUND",
                Message = message,
                SuggestedAction = "The requested resource does not exist or you do not have permission to access it."
            },

            McpErrorCategory.Conflict => new McpErrorResponse
            {
                ErrorCode = "CONFLICT",
                Message = message,
                SuggestedAction = "The resource has been modified by another user. Please refresh and try again."
            },

            McpErrorCategory.Internal => new McpErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An internal error occurred while processing your request.",
                SuggestedAction = "Please try again. If the problem persists, contact support."
            },

            _ => new McpErrorResponse
            {
                ErrorCode = "UNKNOWN_ERROR",
                Message = message,
                SuggestedAction = "An unexpected error occurred."
            }
        };
    }
}

/// <summary>
/// Extension methods for MCP error handling.
/// </summary>
public static class McpErrorExtensions
{
    /// <summary>
    /// Creates an authentication error response.
    /// </summary>
    public static McpErrorResponse AuthenticationError(this IMcpErrorHandler handler, string message = "Authentication required")
        => handler.CreateError(McpErrorCategory.Authentication, message);

    /// <summary>
    /// Creates an authorization error response.
    /// </summary>
    public static McpErrorResponse AuthorizationError(this IMcpErrorHandler handler, string toolName)
        => handler.CreateError(McpErrorCategory.Authorization, $"Access denied. You do not have permission to use the '{toolName}' tool.");

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    public static McpErrorResponse ValidationError(this IMcpErrorHandler handler, string message, Exception? exception = null)
        => handler.CreateError(McpErrorCategory.Validation, message, exception);

    /// <summary>
    /// Creates a service unavailable error response.
    /// </summary>
    public static McpErrorResponse ServiceUnavailableError(this IMcpErrorHandler handler, string serviceName)
        => handler.CreateError(McpErrorCategory.ServiceUnavailable, $"The {serviceName} service is currently unavailable.");

    /// <summary>
    /// Creates a rate limited error response.
    /// </summary>
    public static McpErrorResponse RateLimitedError(this IMcpErrorHandler handler, string reason)
        => handler.CreateError(McpErrorCategory.RateLimited, reason);

    /// <summary>
    /// Creates a not found error response.
    /// </summary>
    public static McpErrorResponse NotFoundError(this IMcpErrorHandler handler, string resourceType, string identifier)
        => handler.CreateError(McpErrorCategory.NotFound, $"{resourceType} '{identifier}' not found.");

    /// <summary>
    /// Creates a conflict error response.
    /// </summary>
    public static McpErrorResponse ConflictError(this IMcpErrorHandler handler, string message)
        => handler.CreateError(McpErrorCategory.Conflict, message);

    /// <summary>
    /// Creates an internal error response.
    /// </summary>
    public static McpErrorResponse InternalError(this IMcpErrorHandler handler, Exception exception)
        => handler.CreateError(McpErrorCategory.Internal, "An internal error occurred", exception);
}
