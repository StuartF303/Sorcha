// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Defines data disclosure rules - who can see what data
/// </summary>
public class Disclosure
{
    /// <summary>
    /// Participant address that can access the data
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("participantAddress")]
    public string ParticipantAddress { get; set; } = string.Empty;

    /// <summary>
    /// JSON Pointers to data elements being accessed
    /// Examples: "/fieldName", "/nested/field", "/*" (all fields)
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [Json.Schema.Generation.MinItems(1)]
    [JsonPropertyName("dataPointers")]
    public List<string> DataPointers { get; set; } = [];

    public Disclosure() { }

    public Disclosure(string participantAddress, List<string> dataPointers)
    {
        ParticipantAddress = participantAddress;
        DataPointers = dataPointers;
    }
}
