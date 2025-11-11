// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Blueprint - Defines a workflow with participants, actions, and data flow
/// </summary>
public class Blueprint : IEquatable<Blueprint>
{
    /// <summary>
    /// JSON-LD context for semantic web integration
    /// </summary>
    [JsonPropertyName("@context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? JsonLdContext { get; set; }

    /// <summary>
    /// JSON-LD type for semantic classification
    /// </summary>
    [JsonPropertyName("@type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JsonLdType { get; set; }

    /// <summary>
    /// Unique identifier for the Blueprint
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MaxLength(64)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The Blueprint title
    /// </summary>
    [DataAnnotations.Required(AllowEmptyStrings = false, ErrorMessage = "Blueprint title must be populated.")]
    [DataAnnotations.MinLength(3)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the Blueprint's purpose
    /// </summary>
    [DataAnnotations.Required(AllowEmptyStrings = false, ErrorMessage = "Blueprint description must be populated.")]
    [DataAnnotations.MinLength(5)]
    [DataAnnotations.MaxLength(2000)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Version number
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Embedded reference data schema documents
    /// </summary>
    [JsonPropertyName("dataSchemas")]
    public List<JsonDocument>? DataSchemas { get; set; }

    /// <summary>
    /// List of participants (minimum 2 required)
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(2)]
    [Json.Schema.Generation.MinItems(2)]
    [JsonPropertyName("participants")]
    public List<Participant> Participants { get; set; } = [];

    /// <summary>
    /// Actions defining the workflow steps (minimum 1 required)
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [Json.Schema.Generation.MinItems(1)]
    [JsonPropertyName("actions")]
    public List<Models.Action> Actions { get; set; } = [];

    /// <summary>
    /// Metadata for the blueprint
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When the blueprint was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the blueprint was last updated
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) => Equals(obj as Blueprint);

    public bool Equals(Blueprint? other)
    {
        return other != null &&
               Id == other.Id &&
               Title == other.Title &&
               Description == other.Description &&
               Version == other.Version &&
               (Participants == other.Participants ||
                Participants.SequenceEqual(other.Participants)) &&
               (Actions == other.Actions ||
                Actions.SequenceEqual(other.Actions));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Title, Description, Version, Participants, Actions);
    }
}
