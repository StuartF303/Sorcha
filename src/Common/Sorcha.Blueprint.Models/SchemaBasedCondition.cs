// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// A schema-based condition that evaluates form data against a JSON Schema fragment.
/// Follows the JSON Forms condition specification.
/// </summary>
public class SchemaBasedCondition
{
    /// <summary>
    /// JSON Pointer to the data field being evaluated
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// JSON Schema that the scoped data must match for the condition to be true
    /// </summary>
    [JsonPropertyName("schema")]
    public JsonNode? Schema { get; set; }
}
