// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Blueprint summary for list operations.
/// </summary>
public class BlueprintSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("participantCount")]
    public int ParticipantCount { get; set; }

    [JsonPropertyName("actionCount")]
    public int ActionCount { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Full blueprint detail.
/// </summary>
public class BlueprintDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("participantCount")]
    public int ParticipantCount { get; set; }

    [JsonPropertyName("actionCount")]
    public int ActionCount { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("participants")]
    public List<BlueprintParticipant> Participants { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<BlueprintAction> Actions { get; set; } = new();
}

/// <summary>
/// A participant defined in a blueprint.
/// </summary>
public class BlueprintParticipant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// An action defined in a blueprint.
/// </summary>
public class BlueprintAction
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("participant")]
    public string Participant { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a blueprint.
/// </summary>
public class CreateBlueprintRequest
{
    [JsonPropertyName("blueprintJson")]
    public string BlueprintJson { get; set; } = string.Empty;
}

/// <summary>
/// Request to publish a blueprint to a register.
/// </summary>
public class PublishBlueprintRequest
{
    [JsonPropertyName("blueprintId")]
    public string BlueprintId { get; set; } = string.Empty;

    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;
}

/// <summary>
/// Response after publishing a blueprint.
/// </summary>
public class PublishBlueprintResponse
{
    [JsonPropertyName("blueprintId")]
    public string BlueprintId { get; set; } = string.Empty;

    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Blueprint version information.
/// </summary>
public class BlueprintVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("changeDescription")]
    public string ChangeDescription { get; set; } = string.Empty;
}

/// <summary>
/// Blueprint instance (running workflow).
/// </summary>
public class BlueprintInstance
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = string.Empty;

    [JsonPropertyName("blueprintId")]
    public string BlueprintId { get; set; } = string.Empty;

    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("currentActionId")]
    public int? CurrentActionId { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; set; }
}
