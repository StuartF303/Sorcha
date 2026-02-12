// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Condition"/> class with empty criteria.
    /// </summary>
    public Condition()
    {
        Criteria = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Condition"/> class with a default boolean state.
    /// Creates a JSON Logic expression that evaluates to the specified boolean value.
    /// </summary>
    /// <param name="defaultState">The default boolean state (true or false).</param>
    public Condition(bool defaultState)
    {
        // NOT FALSE = TRUE, NOT TRUE = FALSE
        Criteria = [defaultState ? "{\"!\": [false]}" : "{\"!\": [true]}"];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Condition"/> class with a principal and default state.
    /// </summary>
    /// <param name="principal">The principal/subject affected by this condition.</param>
    /// <param name="defaultState">The default boolean state for the condition.</param>
    public Condition(string principal, bool defaultState) : this(defaultState)
    {
        Principal = principal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Condition"/> class with a principal and JSON Logic criteria.
    /// </summary>
    /// <param name="principal">The principal/subject affected by this condition.</param>
    /// <param name="criteria">List of JSON Logic expressions to be evaluated.</param>
    public Condition(string principal, List<string> criteria)
    {
        Principal = principal;
        Criteria = criteria;
    }
}
