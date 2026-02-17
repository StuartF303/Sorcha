// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Normalises JSON Schemas from various drafts to draft-2020-12.
/// Extracts metadata (field count, field names, required fields, keywords).
/// </summary>
public static class JsonSchemaNormaliser
{
    private const string Draft202012Uri = "https://json-schema.org/draft/2020-12/schema";
    private const int MaxNormalisationDepth = 64;

    /// <summary>
    /// Normalises a JSON Schema to draft-2020-12.
    /// </summary>
    public static JsonDocument Normalise(JsonDocument schema)
    {
        var node = JsonNode.Parse(schema.RootElement.GetRawText());
        if (node is not JsonObject root)
        {
            return schema;
        }

        var draft = DetectDraft(root);
        NormaliseNode(root, draft);

        // Set $schema to 2020-12
        root["$schema"] = Draft202012Uri;

        return JsonDocument.Parse(root.ToJsonString());
    }

    /// <summary>
    /// Normalises a JSON Schema string to draft-2020-12.
    /// </summary>
    public static JsonDocument Normalise(string schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);
        return Normalise(doc);
    }

    /// <summary>
    /// Extracts metadata from a JSON Schema.
    /// </summary>
    public static SchemaMetadataResult ExtractMetadata(JsonDocument schema)
    {
        var root = schema.RootElement;
        var fieldNames = new List<string>();
        var requiredFields = new List<string>();
        var keywords = new List<string>();

        // Extract properties
        if (root.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in properties.EnumerateObject())
            {
                fieldNames.Add(prop.Name);
            }
        }

        // Extract required fields
        if (root.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in required.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    requiredFields.Add(item.GetString()!);
                }
            }
        }

        // Extract keywords from title and description
        if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
        {
            keywords.AddRange(ExtractKeywords(title.GetString()!));
        }
        if (root.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
        {
            keywords.AddRange(ExtractKeywords(description.GetString()!));
        }

        return new SchemaMetadataResult
        {
            FieldCount = fieldNames.Count,
            FieldNames = fieldNames.ToArray(),
            RequiredFields = requiredFields.ToArray(),
            Keywords = keywords.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    /// <summary>
    /// Computes a SHA-256 content hash for change detection.
    /// </summary>
    public static string ComputeContentHash(JsonDocument schema)
    {
        var json = schema.RootElement.GetRawText();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>
    /// Detects the JSON Schema draft version.
    /// </summary>
    internal static string DetectDraft(JsonObject root)
    {
        if (root.TryGetPropertyValue("$schema", out var schemaNode) && schemaNode is JsonValue schemaValue)
        {
            var schemaUri = schemaValue.ToString();
            if (schemaUri.Contains("draft-04")) return "draft-04";
            if (schemaUri.Contains("draft-06")) return "draft-06";
            if (schemaUri.Contains("draft-07")) return "draft-07";
            if (schemaUri.Contains("2019-09")) return "2019-09";
            if (schemaUri.Contains("2020-12")) return "2020-12";
        }

        // Try to infer from keywords
        if (root.ContainsKey("id") && !root.ContainsKey("$id")) return "draft-04";
        if (root.ContainsKey("definitions") && !root.ContainsKey("$defs")) return "draft-07";

        return "unknown";
    }

    private static void NormaliseNode(JsonObject node, string draft, int depth = 0)
    {
        if (depth > MaxNormalisationDepth) return;

        switch (draft)
        {
            case "draft-04":
                NormaliseDraft04(node, depth);
                break;
            case "draft-06":
                NormaliseDraft06(node, depth);
                break;
            case "draft-07":
                NormaliseDraft07(node, depth);
                break;
        }
    }

    /// <summary>
    /// draft-04 → 2020-12: id → $id, definitions → $defs, exclusiveMinimum/Maximum boolean → number
    /// </summary>
    private static void NormaliseDraft04(JsonObject node, int depth)
    {
        // id → $id
        if (node.ContainsKey("id") && !node.ContainsKey("$id"))
        {
            var id = node["id"];
            node.Remove("id");
            node["$id"] = id?.DeepClone();
        }

        // definitions → $defs
        RenameDefinitions(node);

        // exclusiveMinimum: true + minimum: N → exclusiveMinimum: N
        NormaliseExclusiveMinMax(node);

        // Recursively process nested schemas
        NormaliseChildren(node, "draft-04", depth);
    }

    /// <summary>
    /// draft-06 → 2020-12: exclusiveMinimum/Maximum boolean→number, definitions → $defs
    /// </summary>
    private static void NormaliseDraft06(JsonObject node, int depth)
    {
        RenameDefinitions(node);
        NormaliseExclusiveMinMax(node);
        NormaliseChildren(node, "draft-06", depth);
    }

    /// <summary>
    /// draft-07 → 2020-12: definitions → $defs
    /// </summary>
    private static void NormaliseDraft07(JsonObject node, int depth)
    {
        RenameDefinitions(node);
        NormaliseChildren(node, "draft-07", depth);
    }

    private static void RenameDefinitions(JsonObject node)
    {
        if (node.ContainsKey("definitions") && !node.ContainsKey("$defs"))
        {
            var defs = node["definitions"];
            node.Remove("definitions");
            node["$defs"] = defs?.DeepClone();
        }
    }

    private static void NormaliseExclusiveMinMax(JsonObject node)
    {
        // exclusiveMinimum: true + minimum: N → exclusiveMinimum: N (remove minimum)
        if (node.TryGetPropertyValue("exclusiveMinimum", out var exMinNode) &&
            exMinNode is JsonValue exMinValue)
        {
            try
            {
                var boolVal = exMinValue.GetValue<bool>();
                if (boolVal && node.TryGetPropertyValue("minimum", out var minNode) &&
                    minNode is JsonValue minValue)
                {
                    var minNum = minValue.GetValue<double>();
                    node.Remove("exclusiveMinimum");
                    node.Remove("minimum");
                    node["exclusiveMinimum"] = minNum;
                }
                else if (!boolVal)
                {
                    // exclusiveMinimum: false → just remove the key
                    node.Remove("exclusiveMinimum");
                }
            }
            catch (InvalidOperationException)
            {
                // Already a number — no conversion needed
            }
        }

        // Same for exclusiveMaximum
        if (node.TryGetPropertyValue("exclusiveMaximum", out var exMaxNode) &&
            exMaxNode is JsonValue exMaxValue)
        {
            try
            {
                var boolVal = exMaxValue.GetValue<bool>();
                if (boolVal && node.TryGetPropertyValue("maximum", out var maxNode) &&
                    maxNode is JsonValue maxValue)
                {
                    var maxNum = maxValue.GetValue<double>();
                    node.Remove("exclusiveMaximum");
                    node.Remove("maximum");
                    node["exclusiveMaximum"] = maxNum;
                }
                else if (!boolVal)
                {
                    node.Remove("exclusiveMaximum");
                }
            }
            catch (InvalidOperationException)
            {
                // Already a number — no conversion needed
            }
        }
    }

    private static void NormaliseChildren(JsonObject node, string draft, int depth)
    {
        var childKeys = new[] { "properties", "$defs", "definitions", "patternProperties" };
        foreach (var key in childKeys)
        {
            if (node.TryGetPropertyValue(key, out var child) && child is JsonObject childObj)
            {
                foreach (var (_, value) in childObj)
                {
                    if (value is JsonObject propSchema)
                    {
                        NormaliseNode(propSchema, draft, depth + 1);
                    }
                }
            }
        }

        // Array items
        if (node.TryGetPropertyValue("items", out var items))
        {
            if (items is JsonObject itemsObj)
            {
                NormaliseNode(itemsObj, draft, depth + 1);
            }
        }

        // allOf, anyOf, oneOf
        var compositionKeys = new[] { "allOf", "anyOf", "oneOf" };
        foreach (var key in compositionKeys)
        {
            if (node.TryGetPropertyValue(key, out var arr) && arr is JsonArray jsonArr)
            {
                foreach (var item in jsonArr)
                {
                    if (item is JsonObject itemObj)
                    {
                        NormaliseNode(itemObj, draft, depth + 1);
                    }
                }
            }
        }
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var words = text.Split([' ', ',', '.', '-', '_', '/', '(', ')', '[', ']'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var word in words)
        {
            if (word.Length >= 3)
            {
                yield return word.ToLowerInvariant();
            }
        }
    }
}

/// <summary>
/// Metadata extracted from a JSON Schema.
/// </summary>
public sealed class SchemaMetadataResult
{
    public int FieldCount { get; init; }
    public string[] FieldNames { get; init; } = [];
    public string[] RequiredFields { get; init; } = [];
    public string[] Keywords { get; init; } = [];
}
