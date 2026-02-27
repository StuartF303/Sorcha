// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System.Reflection;
using Spectre.Console;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Table output formatter using Spectre.Console.
/// </summary>
public class TableOutputFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string FormatSingle<T>(T data) where T : class
    {
        if (data == null)
        {
            return string.Empty;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        foreach (var prop in properties)
        {
            var value = prop.GetValue(data);
            table.AddRow(prop.Name, value?.ToString() ?? "(null)");
        }

        using var writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });

        AnsiConsole.Write(table);

        return writer.ToString();
    }

    /// <inheritdoc/>
    public string FormatCollection<T>(IEnumerable<T> data) where T : class
    {
        var items = data.ToList();
        if (!items.Any())
        {
            return "No items found.";
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var table = new Table();
        table.Border(TableBorder.Rounded);

        // Add columns
        foreach (var prop in properties)
        {
            table.AddColumn(prop.Name);
        }

        // Add rows
        foreach (var item in items)
        {
            var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "(null)").ToArray();
            table.AddRow(values);
        }

        using var writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });

        AnsiConsole.Write(table);

        return writer.ToString();
    }

    /// <inheritdoc/>
    public string FormatMessage(string message)
    {
        return message;
    }
}
