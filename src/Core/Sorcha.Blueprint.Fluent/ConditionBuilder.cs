// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating conditional routing logic using JSON Logic
/// </summary>
public class ConditionBuilder
{
    private readonly Dictionary<string, Participant> _participants;
    private readonly List<ConditionalRoute> _routes = new();
    private string? _elseParticipantId;
    private JsonNode? _currentCondition;

    internal ConditionBuilder(Dictionary<string, Participant> participants)
    {
        _participants = participants;
    }

    /// <summary>
    /// Defines a JSON Logic condition
    /// </summary>
    public ConditionBuilder When(Func<JsonLogicBuilder, JsonNode> conditionBuilder)
    {
        var builder = new JsonLogicBuilder();
        _currentCondition = conditionBuilder(builder);
        return this;
    }

    /// <summary>
    /// Routes to a participant if the current condition is true
    /// </summary>
    public ConditionBuilder ThenRoute(string participantId)
    {
        if (_currentCondition == null)
            throw new InvalidOperationException("Must call When() before ThenRoute()");

        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        _routes.Add(new ConditionalRoute
        {
            Condition = _currentCondition,
            ParticipantId = participantId
        });

        _currentCondition = null;
        return this;
    }

    /// <summary>
    /// Specifies the default route when no conditions match
    /// </summary>
    public ConditionBuilder ElseRoute(string participantId)
    {
        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        _elseParticipantId = participantId;
        return this;
    }

    internal JsonNode Build()
    {
        // Build conditional routing logic
        // If multiple routes, use if-then-else chain
        if (_routes.Count == 0)
        {
            return JsonNode.Parse("{\"==\":[0,0]}")!; // Default: always true
        }

        if (_routes.Count == 1 && _elseParticipantId == null)
        {
            return _routes[0].Condition;
        }

        // Build nested if-then-else structure
        // {"if": [condition1, result1, condition2, result2, ..., elseResult]}
        var ifArray = new JsonArray();
        foreach (var route in _routes)
        {
            ifArray.Add(route.Condition?.DeepClone());
            ifArray.Add(JsonValue.Create(route.ParticipantId));
        }

        if (_elseParticipantId != null)
        {
            ifArray.Add(JsonValue.Create(_elseParticipantId));
        }

        return new JsonObject { ["if"] = ifArray };
    }

    private class ConditionalRoute
    {
        public JsonNode? Condition { get; set; }
        public string ParticipantId { get; set; } = string.Empty;
    }
}

/// <summary>
/// Helper for building JSON Logic expressions
/// </summary>
public class JsonLogicBuilder
{
    /// <summary>
    /// Greater than comparison: variable > value
    /// </summary>
    public JsonNode GreaterThan(string variable, object value)
    {
        return new JsonObject
        {
            [">"] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Greater than or equal: variable >= value
    /// </summary>
    public JsonNode GreaterThanOrEqual(string variable, object value)
    {
        return new JsonObject
        {
            [">="] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Less than comparison: variable &lt; value
    /// </summary>
    public JsonNode LessThan(string variable, object value)
    {
        return new JsonObject
        {
            ["<"] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Less than or equal: variable &lt;= value
    /// </summary>
    public JsonNode LessThanOrEqual(string variable, object value)
    {
        return new JsonObject
        {
            ["<="] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Equality comparison: variable == value
    /// </summary>
    public JsonNode Equals(string variable, object value)
    {
        return new JsonObject
        {
            ["=="] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Inequality comparison: variable != value
    /// </summary>
    public JsonNode NotEquals(string variable, object value)
    {
        return new JsonObject
        {
            ["!="] = new JsonArray(Variable(variable), JsonValue.Create(value))
        };
    }

    /// <summary>
    /// Logical AND: all conditions must be true
    /// </summary>
    public JsonNode And(params JsonNode[] conditions)
    {
        var array = new JsonArray();
        foreach (var condition in conditions)
        {
            array.Add(condition?.DeepClone());
        }
        return new JsonObject { ["and"] = array };
    }

    /// <summary>
    /// Logical OR: at least one condition must be true
    /// </summary>
    public JsonNode Or(params JsonNode[] conditions)
    {
        var array = new JsonArray();
        foreach (var condition in conditions)
        {
            array.Add(condition?.DeepClone());
        }
        return new JsonObject { ["or"] = array };
    }

    /// <summary>
    /// Logical NOT: negates a condition
    /// </summary>
    public JsonNode Not(JsonNode condition)
    {
        return new JsonObject
        {
            ["!"] = condition?.DeepClone()
        };
    }

    /// <summary>
    /// References a data variable: {"var": "variableName"}
    /// </summary>
    public JsonNode Variable(string name)
    {
        return new JsonObject { ["var"] = name };
    }
}
