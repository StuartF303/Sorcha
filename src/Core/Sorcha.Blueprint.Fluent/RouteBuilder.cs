// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating Route instances with conditions and parallel branch support
/// </summary>
public class RouteBuilder
{
    private readonly Route _route;

    internal RouteBuilder(string routeId)
    {
        _route = new Route { Id = routeId };
    }

    /// <summary>
    /// Sets the target action IDs for this route (required).
    /// Multiple IDs create parallel branches.
    /// </summary>
    public RouteBuilder ToActions(params int[] nextActionIds)
    {
        _route.NextActionIds = nextActionIds.ToList();
        return this;
    }

    /// <summary>
    /// Sets a JSON Logic condition for this route.
    /// The route matches when this condition evaluates to true.
    /// </summary>
    public RouteBuilder When(Func<JsonLogicBuilder, JsonNode> condition)
    {
        var builder = new JsonLogicBuilder();
        _route.Condition = condition(builder);
        return this;
    }

    /// <summary>
    /// Marks this route as the default (used when no other routes match).
    /// </summary>
    public RouteBuilder AsDefault()
    {
        _route.IsDefault = true;
        return this;
    }

    /// <summary>
    /// Sets a description for this route.
    /// </summary>
    public RouteBuilder WithDescription(string description)
    {
        _route.Description = description;
        return this;
    }

    /// <summary>
    /// Sets a deadline for parallel branches (ISO 8601 duration).
    /// </summary>
    public RouteBuilder WithBranchDeadline(string isoDuration)
    {
        _route.BranchDeadline = isoDuration;
        return this;
    }

    internal Route Build()
    {
        if (_route.NextActionIds == null || !_route.NextActionIds.Any())
        {
            throw new InvalidOperationException("Route must have at least one target action. Call ToActions() first.");
        }

        return _route;
    }
}
