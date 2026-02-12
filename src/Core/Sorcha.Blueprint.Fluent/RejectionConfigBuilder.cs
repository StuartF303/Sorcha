// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating RejectionConfig instances
/// </summary>
public class RejectionConfigBuilder
{
    private readonly RejectionConfig _config = new();

    /// <summary>
    /// Sets the target action to route to on rejection (required).
    /// </summary>
    public RejectionConfigBuilder RouteToAction(int targetActionId)
    {
        _config.TargetActionId = targetActionId;
        return this;
    }

    /// <summary>
    /// Sets the target participant to handle the rejection.
    /// </summary>
    public RejectionConfigBuilder WithTargetParticipant(string participantId)
    {
        _config.TargetParticipantId = participantId;
        return this;
    }

    /// <summary>
    /// Sets whether a rejection reason is required (default: true).
    /// </summary>
    public RejectionConfigBuilder RequireReason(bool require = true)
    {
        _config.RequireReason = require;
        return this;
    }

    /// <summary>
    /// Sets the JSON Schema for structured rejection data.
    /// </summary>
    public RejectionConfigBuilder WithRejectionSchema(JsonElement schema)
    {
        _config.RejectionSchema = schema;
        return this;
    }

    /// <summary>
    /// Marks this rejection as terminal (terminates the workflow).
    /// </summary>
    public RejectionConfigBuilder AsTerminal()
    {
        _config.IsTerminal = true;
        return this;
    }

    internal RejectionConfig Build()
    {
        return _config;
    }
}
