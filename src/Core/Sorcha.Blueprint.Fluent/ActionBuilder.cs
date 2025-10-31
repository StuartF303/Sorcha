// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating Action instances with data schemas, routing, and disclosures
/// </summary>
public class ActionBuilder
{
    private readonly Models.Action _action;
    private readonly Dictionary<string, Participant> _participants;
    private readonly List<Disclosure> _disclosures = new();
    private readonly Dictionary<string, JsonNode> _calculations = new();

    internal ActionBuilder(int actionId, Dictionary<string, Participant> participants)
    {
        _action = new Models.Action
        {
            Id = actionId
        };
        _participants = participants;
    }

    /// <summary>
    /// Sets the action title
    /// </summary>
    public ActionBuilder WithTitle(string title)
    {
        _action.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the action description
    /// </summary>
    public ActionBuilder WithDescription(string description)
    {
        _action.Description = description;
        return this;
    }

    /// <summary>
    /// Specifies the sending participant
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if participant doesn't exist</exception>
    public ActionBuilder SentBy(string participantId)
    {
        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        _action.Sender = participantId;
        return this;
    }

    /// <summary>
    /// Defines the JSON Schema for action data
    /// </summary>
    public ActionBuilder RequiresData(Action<DataSchemaBuilder> configure)
    {
        var builder = new DataSchemaBuilder();
        configure(builder);
        var schemas = new List<JsonDocument>();
        if (_action.DataSchemas != null)
        {
            schemas.AddRange(_action.DataSchemas);
        }
        schemas.Add(builder.Build());
        _action.DataSchemas = schemas;
        return this;
    }

    /// <summary>
    /// Configures selective data disclosure to a participant
    /// </summary>
    public ActionBuilder Disclose(string participantId, Action<DisclosureBuilder> configure)
    {
        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        var builder = new DisclosureBuilder(participantId);
        configure(builder);
        _disclosures.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Routes to a single next participant
    /// </summary>
    public ActionBuilder RouteToNext(string participantId)
    {
        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        // Set up simple routing to the participant
        var condition = new Condition(participantId, true);
        _action.Participants = new List<Condition> { condition };
        return this;
    }

    /// <summary>
    /// Enables conditional routing with JSON Logic
    /// </summary>
    public ActionBuilder RouteConditionally(Action<ConditionBuilder> configure)
    {
        var builder = new ConditionBuilder(_participants);
        configure(builder);
        _action.Condition = builder.Build();
        return this;
    }

    /// <summary>
    /// Adds JSON Logic calculation for a field
    /// </summary>
    public ActionBuilder Calculate(string fieldName, Action<CalculationBuilder> configure)
    {
        var builder = new CalculationBuilder();
        configure(builder);
        _calculations[fieldName] = builder.Build();
        return this;
    }

    /// <summary>
    /// Defines the UI form for the action
    /// </summary>
    public ActionBuilder WithForm(Action<FormBuilder> configure)
    {
        var builder = new FormBuilder();
        configure(builder);
        _action.Form = builder.Build();
        return this;
    }

    internal Models.Action Build()
    {
        // Apply disclosures
        if (_disclosures.Count > 0)
        {
            _action.Disclosures = _disclosures;
        }

        // Apply calculations
        if (_calculations.Count > 0)
        {
            _action.Calculations = _calculations;
        }

        return _action;
    }
}
