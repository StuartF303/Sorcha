// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating Disclosure instances with JSON Pointer field references
/// </summary>
public class DisclosureBuilder
{
    private readonly Disclosure _disclosure;

    internal DisclosureBuilder(string participantAddress)
    {
        _disclosure = new Disclosure
        {
            ParticipantAddress = participantAddress
        };
    }

    /// <summary>
    /// Adds multiple fields using JSON Pointer syntax (e.g., "/fieldName" or "#/fieldName")
    /// </summary>
    public DisclosureBuilder Fields(params string[] jsonPointers)
    {
        foreach (var pointer in jsonPointers)
        {
            Field(pointer);
        }
        return this;
    }

    /// <summary>
    /// Adds a single field using JSON Pointer syntax
    /// </summary>
    public DisclosureBuilder Field(string jsonPointer)
    {
        // Ensure proper JSON Pointer format
        if (!jsonPointer.StartsWith("/") && !jsonPointer.StartsWith("#"))
        {
            jsonPointer = "/" + jsonPointer;
        }

        _disclosure.DataPointers.Add(jsonPointer);
        return this;
    }

    /// <summary>
    /// Discloses all fields (uses "/*" wildcard)
    /// </summary>
    public DisclosureBuilder AllFields()
    {
        _disclosure.DataPointers.Clear();
        _disclosure.DataPointers.Add("/*");
        return this;
    }

    internal Disclosure Build() => _disclosure;
}
