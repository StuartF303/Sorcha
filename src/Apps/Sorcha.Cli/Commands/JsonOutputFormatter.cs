// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System.Text.Json;

namespace Sorcha.Cli.Commands;

/// <summary>
/// JSON output formatter.
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public string FormatSingle<T>(T data) where T : class
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <inheritdoc/>
    public string FormatCollection<T>(IEnumerable<T> data) where T : class
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <inheritdoc/>
    public string FormatMessage(string message)
    {
        return JsonSerializer.Serialize(new { message }, JsonOptions);
    }
}
