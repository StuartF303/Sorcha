// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Middleware;

/// <summary>
/// Middleware to extract and validate delegation tokens from request headers.
/// The delegation token grants the Blueprint Service permission to perform
/// cryptographic operations on behalf of the authenticated participant.
/// </summary>
public class DelegationTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DelegationTokenMiddleware> _logger;

    /// <summary>
    /// The header name for the delegation token
    /// </summary>
    public const string DelegationTokenHeader = "X-Delegation-Token";

    /// <summary>
    /// The HttpContext item key for storing the delegation token
    /// </summary>
    public const string DelegationTokenKey = "DelegationToken";

    public DelegationTokenMiddleware(RequestDelegate next, ILogger<DelegationTokenMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract delegation token from header if present
        if (context.Request.Headers.TryGetValue(DelegationTokenHeader, out var tokenValues))
        {
            var token = tokenValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Store the token in HttpContext.Items for later retrieval
                context.Items[DelegationTokenKey] = token;

                _logger.LogDebug("Delegation token extracted from request header");
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for DelegationTokenMiddleware
/// </summary>
public static class DelegationTokenMiddlewareExtensions
{
    /// <summary>
    /// Adds the delegation token middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UseDelegationToken(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DelegationTokenMiddleware>();
    }

    /// <summary>
    /// Gets the delegation token from the HttpContext
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>The delegation token, or null if not present</returns>
    public static string? GetDelegationToken(this HttpContext context)
    {
        if (context.Items.TryGetValue(DelegationTokenMiddleware.DelegationTokenKey, out var token))
        {
            return token as string;
        }
        return null;
    }

    /// <summary>
    /// Gets the delegation token from the HttpContext, throwing if not present
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>The delegation token</returns>
    /// <exception cref="InvalidOperationException">Thrown if no delegation token is present</exception>
    public static string GetRequiredDelegationToken(this HttpContext context)
    {
        var token = context.GetDelegationToken();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException(
                $"Delegation token is required. Include the '{DelegationTokenMiddleware.DelegationTokenHeader}' header in your request.");
        }
        return token;
    }
}
