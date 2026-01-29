// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Models.JsonLd;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating Blueprint instances with validation
/// </summary>
public class BlueprintBuilder
{
    private readonly Models.Blueprint _blueprint;
    private readonly Dictionary<string, Participant> _participants = new();
    private readonly Dictionary<int, Models.Action> _actions = new();

    private BlueprintBuilder()
    {
        _blueprint = new Models.Blueprint
        {
            Id = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Creates a new BlueprintBuilder instance
    /// </summary>
    public static BlueprintBuilder Create() => new();

    /// <summary>
    /// Sets the blueprint ID (optional - auto-generated if not specified)
    /// </summary>
    public BlueprintBuilder WithId(string id)
    {
        _blueprint.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the blueprint title (required, min 3 characters)
    /// </summary>
    public BlueprintBuilder WithTitle(string title)
    {
        _blueprint.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the blueprint description (required, min 5 characters)
    /// </summary>
    public BlueprintBuilder WithDescription(string description)
    {
        _blueprint.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the version number (default: 1)
    /// </summary>
    public BlueprintBuilder WithVersion(int version)
    {
        _blueprint.Version = version;
        return this;
    }

    /// <summary>
    /// Adds metadata key-value pair
    /// </summary>
    public BlueprintBuilder WithMetadata(string key, string value)
    {
        _blueprint.Metadata ??= new Dictionary<string, string>();
        _blueprint.Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Enables JSON-LD with default context
    /// </summary>
    public BlueprintBuilder WithJsonLd()
    {
        _blueprint.JsonLdContext = JsonLdContext.DefaultContext.DeepClone();
        _blueprint.JsonLdType = JsonLdTypes.Blueprint;
        return this;
    }

    /// <summary>
    /// Enables JSON-LD with context based on blueprint category
    /// </summary>
    public BlueprintBuilder WithJsonLd(string category)
    {
        _blueprint.JsonLdContext = JsonLdContext.GetContextByCategory(category);
        _blueprint.JsonLdType = JsonLdTypes.Blueprint;

        // Also set the category in metadata if not already set
        if (_blueprint.Metadata?.ContainsKey("category") != true)
        {
            WithMetadata("category", category);
        }

        return this;
    }

    /// <summary>
    /// Sets custom JSON-LD context
    /// </summary>
    public BlueprintBuilder WithJsonLdContext(JsonNode context)
    {
        _blueprint.JsonLdContext = context.DeepClone();
        _blueprint.JsonLdType = JsonLdTypes.Blueprint;
        return this;
    }

    /// <summary>
    /// Sets custom JSON-LD type
    /// </summary>
    public BlueprintBuilder WithJsonLdType(string type)
    {
        _blueprint.JsonLdType = type;
        return this;
    }

    /// <summary>
    /// Merges custom context with default context
    /// </summary>
    public BlueprintBuilder WithAdditionalJsonLdContext(JsonNode customContext)
    {
        if (_blueprint.JsonLdContext == null)
        {
            WithJsonLd();
        }

        _blueprint.JsonLdContext = JsonLdContext.MergeContexts(customContext);
        return this;
    }

    /// <summary>
    /// Adds a participant to the blueprint
    /// </summary>
    /// <param name="participantId">Unique identifier for the participant</param>
    /// <param name="configure">Configuration delegate for the participant</param>
    public BlueprintBuilder AddParticipant(string participantId, Action<ParticipantBuilder> configure)
    {
        var builder = new ParticipantBuilder(participantId);
        configure(builder);
        var participant = builder.Build();

        _participants[participantId] = participant;
        _blueprint.Participants.Add(participant);

        return this;
    }

    /// <summary>
    /// Adds an action to the blueprint
    /// </summary>
    /// <param name="actionId">Unique identifier for the action (0-based sequence)</param>
    /// <param name="configure">Configuration delegate for the action</param>
    public BlueprintBuilder AddAction(int actionId, Action<ActionBuilder> configure)
    {
        var builder = new ActionBuilder(actionId, _participants);
        configure(builder);
        var action = builder.Build();

        _actions[actionId] = action;
        _blueprint.Actions.Add(action);

        return this;
    }

    /// <summary>
    /// Builds the blueprint (validates requirements)
    /// </summary>
    /// <returns>The completed Blueprint instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public Models.Blueprint Build()
    {
        // Validate minimum requirements
        if (string.IsNullOrWhiteSpace(_blueprint.Title) || _blueprint.Title.Length < 3)
            throw new InvalidOperationException("Blueprint title must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(_blueprint.Description) || _blueprint.Description.Length < 5)
            throw new InvalidOperationException("Blueprint description must be at least 5 characters");

        if (_blueprint.Participants.Count < 2)
            throw new InvalidOperationException("Blueprint must have at least 2 participants");

        if (_blueprint.Actions.Count < 1)
            throw new InvalidOperationException("Blueprint must have at least 1 action");

        // Auto-set JSON-LD types only when JSON-LD mode is enabled
        if (_blueprint.JsonLdContext != null)
        {
            foreach (var participant in _blueprint.Participants)
            {
                if (string.IsNullOrEmpty(participant.JsonLdType))
                {
                    participant.JsonLdType = JsonLdTypeHelper.GetParticipantType(participant.Organisation);
                }
            }

            foreach (var action in _blueprint.Actions)
            {
                if (string.IsNullOrEmpty(action.JsonLdType) && !string.IsNullOrEmpty(action.Title))
                {
                    action.JsonLdType = JsonLdTypeHelper.GetActionType(action.Title);
                }
            }
        }

        _blueprint.UpdatedAt = DateTimeOffset.UtcNow;

        return _blueprint;
    }

    /// <summary>
    /// Builds the blueprint without validation (for drafts)
    /// </summary>
    public Models.Blueprint BuildDraft()
    {
        _blueprint.UpdatedAt = DateTimeOffset.UtcNow;
        return _blueprint;
    }
}
