// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// A JSON Forms rule that controls visibility/interactivity of a control
/// based on a schema-based condition evaluated against form data.
/// </summary>
public class FormRule
{
    /// <summary>
    /// The effect to apply when the condition is true
    /// </summary>
    [JsonPropertyName("effect")]
    public RuleEffect Effect { get; set; }

    /// <summary>
    /// The condition that determines when the effect is applied
    /// </summary>
    [JsonPropertyName("condition")]
    public SchemaBasedCondition Condition { get; set; } = new();
}
