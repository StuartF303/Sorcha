// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Models.JsonLd;

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
    private readonly List<Route> _routes = new();

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

    /// <summary>
    /// Sets the target participant for this action (ActivityStreams)
    /// </summary>
    public ActionBuilder WithTarget(string participantId)
    {
        if (!_participants.ContainsKey(participantId))
            throw new InvalidOperationException($"Participant '{participantId}' not found in blueprint");

        _action.Target = participantId;
        return this;
    }

    /// <summary>
    /// Sets the JSON-LD type for the action
    /// </summary>
    public ActionBuilder AsJsonLdType(string type)
    {
        _action.JsonLdType = type;
        return this;
    }

    /// <summary>
    /// Automatically determines JSON-LD type based on action title
    /// </summary>
    public ActionBuilder WithAutoJsonLdType()
    {
        _action.JsonLdType = JsonLdTypeHelper.GetActionType(_action.Title);
        return this;
    }

    /// <summary>
    /// Sets action as a Create activity
    /// </summary>
    public ActionBuilder AsCreateAction()
    {
        _action.JsonLdType = JsonLdTypes.CreateAction;
        return this;
    }

    /// <summary>
    /// Sets action as an Accept activity
    /// </summary>
    public ActionBuilder AsAcceptAction()
    {
        _action.JsonLdType = JsonLdTypes.AcceptAction;
        return this;
    }

    /// <summary>
    /// Sets action as a Reject activity
    /// </summary>
    public ActionBuilder AsRejectAction()
    {
        _action.JsonLdType = JsonLdTypes.RejectAction;
        return this;
    }

    /// <summary>
    /// Sets action as an Update activity
    /// </summary>
    public ActionBuilder AsUpdateAction()
    {
        _action.JsonLdType = JsonLdTypes.UpdateAction;
        return this;
    }

    /// <summary>
    /// Sets the published timestamp
    /// </summary>
    public ActionBuilder PublishedAt(DateTimeOffset timestamp)
    {
        _action.Published = timestamp;
        return this;
    }

    /// <summary>
    /// Adds additional JSON-LD properties
    /// </summary>
    public ActionBuilder WithAdditionalProperty(string key, JsonNode value)
    {
        _action.AdditionalProperties ??= new Dictionary<string, JsonNode>();
        _action.AdditionalProperties[key] = value.DeepClone();
        return this;
    }

    /// <summary>
    /// Adds a route with the specified route ID and configuration
    /// </summary>
    public ActionBuilder AddRoute(string routeId, Action<RouteBuilder> configure)
    {
        var builder = new RouteBuilder(routeId);
        configure(builder);
        _routes.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a default route that targets the specified action IDs
    /// </summary>
    public ActionBuilder WithDefaultRoute(params int[] nextActionIds)
    {
        _routes.Add(new Route
        {
            Id = $"default-{_action.Id}",
            NextActionIds = nextActionIds.ToList(),
            IsDefault = true
        });
        return this;
    }

    /// <summary>
    /// Configures rejection handling for this action
    /// </summary>
    public ActionBuilder OnRejection(Action<RejectionConfigBuilder> configure)
    {
        var builder = new RejectionConfigBuilder();
        configure(builder);
        _action.RejectionConfig = builder.Build();
        return this;
    }

    /// <summary>
    /// Marks this action as a starting action in the workflow
    /// </summary>
    public ActionBuilder AsStartingAction()
    {
        _action.IsStartingAction = true;
        return this;
    }

    /// <summary>
    /// Specifies which prior actions must be completed before this action can execute
    /// </summary>
    public ActionBuilder RequiresPriorActions(params int[] actionIds)
    {
        _action.RequiredPriorActions = actionIds.ToList();
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

        // Apply routes
        if (_routes.Count > 0)
        {
            _action.Routes = _routes;
        }

        return _action;
    }
}
