// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Implementation;

/// <summary>
/// Selective disclosure processor that filters data based on JSON Pointer rules.
/// </summary>
/// <remarks>
/// Implements privacy-preserving selective data disclosure by filtering
/// action data according to JSON Pointer specifications.
/// 
/// Thread-safe and can be used concurrently.
/// </remarks>
public class DisclosureProcessor : IDisclosureProcessor
{
    /// <summary>
    /// Apply a single disclosure rule to filter data for one participant.
    /// </summary>
    public Dictionary<string, object> ApplyDisclosure(
        Dictionary<string, object> data,
        Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(disclosure);

        var result = new Dictionary<string, object>();

        // Process each JSON Pointer in the disclosure
        foreach (var pointer in disclosure.DataPointers)
        {
            if (string.IsNullOrWhiteSpace(pointer))
                continue;

            // Handle wildcard: /* means all fields
            if (pointer == "/*" || pointer == "#/*")
            {
                // Return all top-level fields
                foreach (var kvp in data)
                {
                    result[kvp.Key] = kvp.Value;
                }
                continue;
            }

            // Process JSON Pointer
            var fields = ExtractFieldsFromPointer(pointer, data);
            foreach (var field in fields)
            {
                result[field.Key] = field.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Create disclosure results for all participants in an action.
    /// </summary>
    public List<DisclosureResult> CreateDisclosures(
        Dictionary<string, object> data,
        IEnumerable<Disclosure> disclosures)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(disclosures);

        var results = new List<DisclosureResult>();

        foreach (var disclosure in disclosures)
        {
            var disclosedData = ApplyDisclosure(data, disclosure);

            results.Add(DisclosureResult.Create(
                disclosure.ParticipantAddress,
                disclosedData
            ));
        }

        return results;
    }

    /// <summary>
    /// Extracts fields from data using a JSON Pointer.
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - Root level fields: "/fieldName"
    /// - Nested fields: "/user/name"
    /// - Array indices: "/items/0"
    /// - Hash prefix: "#/fieldName" (same as "/fieldName")
    /// </remarks>
    private static Dictionary<string, object> ExtractFieldsFromPointer(
        string pointer,
        Dictionary<string, object> data)
    {
        var result = new Dictionary<string, object>();

        // Remove leading '#' if present
        if (pointer.StartsWith("#/"))
        {
            pointer = pointer.Substring(1);
        }

        // Remove leading '/' if present
        if (pointer.StartsWith("/"))
        {
            pointer = pointer.Substring(1);
        }

        // Empty pointer means root (all data)
        if (string.IsNullOrEmpty(pointer))
        {
            foreach (var kvp in data)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        // Split pointer into segments
        var segments = pointer.Split('/');

        // Navigate through the data structure
        var value = NavigateToValue(data, segments);

        if (value != null)
        {
            // Use the first segment as the key in the result
            result[segments[0]] = value;
        }

        return result;
    }

    /// <summary>
    /// Navigates through a data structure using JSON Pointer segments.
    /// </summary>
    private static object? NavigateToValue(object current, string[] segments)
    {
        foreach (var segment in segments)
        {
            if (current == null)
                return null;

            // Decode JSON Pointer escapes
            var decodedSegment = DecodePointerSegment(segment);

            // Handle dictionary/object
            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(decodedSegment, out var value))
                    return null;

                current = value;
                continue;
            }

            // Handle list/array
            if (current is System.Collections.IList list)
            {
                if (int.TryParse(decodedSegment, out var index))
                {
                    if (index < 0 || index >= list.Count)
                        return null;

                    current = list[index]!;
                    continue;
                }
                return null;
            }

            // Try to handle as JSON element
            if (current is JsonElement element)
            {
                current = NavigateJsonElement(element, decodedSegment);
                if (current == null)
                    return null;
                continue;
            }

            // If we can't navigate further, return null
            return null;
        }

        return current;
    }

    /// <summary>
    /// Navigates through a JsonElement.
    /// </summary>
    private static object? NavigateJsonElement(JsonElement element, string segment)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty(segment, out var property))
                {
                    return ConvertJsonElement(property);
                }
                return null;

            case JsonValueKind.Array:
                if (int.TryParse(segment, out var index))
                {
                    var array = element.EnumerateArray().ToList();
                    if (index >= 0 && index < array.Count)
                    {
                        return ConvertJsonElement(array[index]);
                    }
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Converts a JsonElement to a .NET object.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.Deserialize<Dictionary<string, object>>(),
            JsonValueKind.Array => element.Deserialize<List<object>>(),
            _ => null
        };
    }

    /// <summary>
    /// Decodes JSON Pointer escape sequences.
    /// </summary>
    /// <remarks>
    /// JSON Pointer escaping (RFC 6901):
    /// - "~0" represents "~"
    /// - "~1" represents "/"
    /// </remarks>
    private static string DecodePointerSegment(string segment)
    {
        return segment
            .Replace("~1", "/")
            .Replace("~0", "~");
    }
}
