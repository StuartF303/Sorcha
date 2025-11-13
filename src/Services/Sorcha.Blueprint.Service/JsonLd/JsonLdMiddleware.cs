// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Http.Features;
using Sorcha.Blueprint.Models.JsonLd;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

namespace Sorcha.Blueprint.Service.JsonLd;

/// <summary>
/// Middleware for JSON-LD content negotiation
/// Automatically adds JSON-LD context when Accept header includes application/ld+json
/// </summary>
public class JsonLdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JsonLdMiddleware> _logger;

    public JsonLdMiddleware(RequestDelegate next, ILogger<JsonLdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if client accepts JSON-LD
        var acceptsJsonLd = context.Request.Headers.Accept
            .Any(h => h?.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase) == true);

        if (acceptsJsonLd)
        {
            // Store flag in context items for later use
            context.Items["AcceptsJsonLd"] = true;
            _logger.LogDebug("Client accepts application/ld+json");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for JSON-LD middleware
/// </summary>
public static class JsonLdMiddlewareExtensions
{
    /// <summary>
    /// Adds JSON-LD content negotiation middleware
    /// </summary>
    public static IApplicationBuilder UseJsonLdContentNegotiation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JsonLdMiddleware>();
    }

    /// <summary>
    /// Checks if the request accepts JSON-LD
    /// </summary>
    public static bool AcceptsJsonLd(this HttpContext context)
    {
        return context.Items.ContainsKey("AcceptsJsonLd") &&
               context.Items["AcceptsJsonLd"] is bool acceptsJsonLd &&
               acceptsJsonLd;
    }
}

/// <summary>
/// Helper methods for ensuring JSON-LD context on blueprints
/// </summary>
public static class JsonLdHelper
{
    /// <summary>
    /// Ensures blueprint has JSON-LD context (adds if missing)
    /// </summary>
    public static BlueprintModel EnsureJsonLdContext(BlueprintModel blueprint, string? categoryHint = null)
    {
        if (blueprint.JsonLdContext == null)
        {
            // Determine category from metadata
            var category = blueprint.Metadata?.GetValueOrDefault("category") ?? categoryHint;
            blueprint.JsonLdContext = JsonLdContext.GetContextByCategory(category);
            blueprint.JsonLdType = JsonLdTypes.Blueprint;
        }

        // Ensure participants have types
        foreach (var participant in blueprint.Participants)
        {
            if (string.IsNullOrEmpty(participant.JsonLdType))
            {
                participant.JsonLdType = JsonLdTypeHelper.GetParticipantType(participant.Organisation);
            }
        }

        // Ensure actions have types
        foreach (var action in blueprint.Actions)
        {
            if (string.IsNullOrEmpty(action.JsonLdType))
            {
                action.JsonLdType = JsonLdTypeHelper.GetActionType(action.Title);
            }
        }

        return blueprint;
    }

    /// <summary>
    /// Ensures collection of blueprints have JSON-LD context
    /// </summary>
    public static IEnumerable<BlueprintModel> EnsureJsonLdContext(
        IEnumerable<BlueprintModel> blueprints,
        string? categoryHint = null)
    {
        return blueprints.Select(b => EnsureJsonLdContext(b, categoryHint));
    }
}

/// <summary>
/// Result helpers for JSON-LD responses
/// </summary>
public static class JsonLdResults
{
    /// <summary>
    /// Returns a blueprint with JSON-LD context if requested
    /// </summary>
    public static IResult Ok(HttpContext context, BlueprintModel blueprint)
    {
        if (context.AcceptsJsonLd())
        {
            blueprint = JsonLdHelper.EnsureJsonLdContext(blueprint);
            return Results.Ok(blueprint);
        }

        return Results.Ok(blueprint);
    }

    /// <summary>
    /// Returns a collection of blueprints with JSON-LD context if requested
    /// </summary>
    public static IResult Ok(HttpContext context, IEnumerable<BlueprintModel> blueprints)
    {
        if (context.AcceptsJsonLd())
        {
            blueprints = JsonLdHelper.EnsureJsonLdContext(blueprints);
        }

        return Results.Ok(blueprints);
    }

    /// <summary>
    /// Returns a created blueprint with JSON-LD context if requested
    /// </summary>
    public static IResult Created(HttpContext context, string location, BlueprintModel blueprint)
    {
        if (context.AcceptsJsonLd())
        {
            blueprint = JsonLdHelper.EnsureJsonLdContext(blueprint);
        }

        return Results.Created(location, blueprint);
    }
}
