// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// A condition using JSON Logic rules for workflow routing
/// </summary>
public class Condition
{
    /// <summary>
    /// The principal/subject that the condition affects
    /// </summary>
    [DataAnnotations.MaxLength(2048)]
    [JsonPropertyName("principal")]
    public string Principal { get; set; } = string.Empty;

    /// <summary>
    /// JSON Logic criteria to be evaluated
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [Json.Schema.Generation.MinItems(1)]
    [JsonPropertyName("criteria")]
    public IEnumerable<string> Criteria { get; set; } = [];

    public Condition()
    {
        Criteria = [];
    }

    public Condition(bool defaultState)
    {
        // NOT FALSE = TRUE, NOT TRUE = FALSE
        Criteria = [defaultState ? "{\"!\": [false]}" : "{\"!\": [true]}"];
    }

    public Condition(string principal, bool defaultState) : this(defaultState)
    {
        Principal = principal;
    }

    public Condition(string principal, List<string> criteria)
    {
        Principal = principal;
        Criteria = criteria;
    }
}
