// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Blueprint Template - Defines a reusable blueprint pattern with parameters
/// Uses JSON-e syntax for dynamic template evaluation
/// </summary>
public class BlueprintTemplate : IEquatable<BlueprintTemplate>
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MaxLength(64)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Template title
    /// </summary>
    [DataAnnotations.Required(AllowEmptyStrings = false)]
    [DataAnnotations.MinLength(3)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the template's purpose and use cases
    /// </summary>
    [DataAnnotations.Required(AllowEmptyStrings = false)]
    [DataAnnotations.MinLength(5)]
    [DataAnnotations.MaxLength(2000)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template version number
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Category of the template (e.g., "finance", "supply-chain")
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Tags for template classification and search
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// The JSON-e template definition
    /// This is the template that will be evaluated with context parameters
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("template")]
    public JsonNode Template { get; set; } = JsonNode.Parse("{}")!;

    /// <summary>
    /// JSON Schema defining expected parameters for template evaluation
    /// Validates the context object passed during template rendering
    /// </summary>
    [JsonPropertyName("parameterSchema")]
    public JsonDocument? ParameterSchema { get; set; }

    /// <summary>
    /// Default parameter values
    /// Used when parameters are not provided during evaluation
    /// </summary>
    [JsonPropertyName("defaultParameters")]
    public Dictionary<string, object>? DefaultParameters { get; set; }

    /// <summary>
    /// Example parameter sets demonstrating template usage
    /// </summary>
    [JsonPropertyName("examples")]
    public List<TemplateExample>? Examples { get; set; }

    /// <summary>
    /// Template author or organization
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Additional metadata for the template
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When the template was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the template was last updated
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether this template is published and available for use
    /// </summary>
    [JsonPropertyName("published")]
    public bool Published { get; set; } = false;

    /// <summary>
    /// Determines whether the specified object is equal to this blueprint template.
    /// Compares Id, Title, and Version.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal, otherwise false.</returns>
    public override bool Equals(object? obj) => Equals(obj as BlueprintTemplate);

    /// <summary>
    /// Determines whether the specified blueprint template is equal to this template.
    /// Compares Id, Title, and Version for equality.
    /// </summary>
    /// <param name="other">The blueprint template to compare.</param>
    /// <returns>True if the templates are equal, otherwise false.</returns>
    public bool Equals(BlueprintTemplate? other)
    {
        return other != null &&
               Id == other.Id &&
               Title == other.Title &&
               Version == other.Version;
    }

    /// <summary>
    /// Returns a hash code for this blueprint template based on its Id, Title, and Version.
    /// </summary>
    /// <returns>A hash code for the template.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Title, Version);
    }
}

/// <summary>
/// Example parameter set for demonstrating template usage
/// </summary>
public class TemplateExample
{
    /// <summary>
    /// Example name/title
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Example description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Parameter values for this example
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Expected output (optional, for documentation/testing)
    /// </summary>
    [JsonPropertyName("expectedOutput")]
    public JsonNode? ExpectedOutput { get; set; }
}
