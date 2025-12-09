// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Sorcha.Cli.UI;

/// <summary>
/// Manages detailed payload viewing for requests and responses
/// </summary>
public class PayloadDetail
{
    private readonly ConcurrentQueue<PayloadEntry> _entries = new();
    private readonly int _maxEntries;
    private PayloadEntry? _currentEntry;

    public event Action? OnPayloadAdded;

    public PayloadDetail(int maxEntries = 100)
    {
        _maxEntries = maxEntries;
    }

    public void SetCurrentPayload(string title, string contentType, string content, string? method = null, string? url = null)
    {
        var entry = new PayloadEntry
        {
            Timestamp = DateTime.Now,
            Title = title,
            ContentType = contentType,
            Content = content,
            Method = method,
            Url = url
        };

        _currentEntry = entry;
        _entries.Enqueue(entry);

        // Trim old entries
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }

        OnPayloadAdded?.Invoke();
    }

    public void SetCurrentRequestPayload(string method, string url, string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            SetCurrentPayload($"Request: {method}", "text/plain", "(no body)", method, url);
            return;
        }

        var contentType = IsJson(body) ? "application/json" : "text/plain";
        var formattedContent = contentType == "application/json"
            ? FormatJson(body)
            : body;

        SetCurrentPayload($"Request: {method} {url}", contentType, formattedContent, method, url);
    }

    public void SetCurrentResponsePayload(int statusCode, string? body, long elapsedMs)
    {
        if (string.IsNullOrEmpty(body))
        {
            SetCurrentPayload($"Response: {statusCode} ({elapsedMs}ms)", "text/plain", "(empty response)");
            return;
        }

        var contentType = IsJson(body) ? "application/json" : "text/plain";
        var formattedContent = contentType == "application/json"
            ? FormatJson(body)
            : body;

        SetCurrentPayload($"Response: {statusCode} ({elapsedMs}ms)", contentType, formattedContent);
    }

    public void Clear()
    {
        _currentEntry = null;
        while (_entries.TryDequeue(out _)) { }
        OnPayloadAdded?.Invoke();
    }

    public IEnumerable<PayloadEntry> GetEntries(int count = 10)
    {
        return _entries.TakeLast(count);
    }

    /// <summary>
    /// Renders the payload detail panel as a Spectre.Console renderable
    /// </summary>
    public Panel RenderPanel(int height = 20)
    {
        if (_currentEntry == null)
        {
            var emptyContent = new Markup("[grey]No payload data yet...[/]");
            return new Panel(emptyContent)
                .Header("[bold yellow]Payload Detail[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow);
        }

        // Create scrollable content
        var lines = _currentEntry.Content.Split('\n');
        var maxLines = height - 4; // Account for panel border and header

        // Format header info
        var headerText = new StringBuilder();
        headerText.AppendLine($"[bold]{Markup.Escape(_currentEntry.Title)}[/]");
        headerText.AppendLine($"[grey]{_currentEntry.Timestamp:HH:mm:ss.fff}[/]");

        if (!string.IsNullOrEmpty(_currentEntry.Method))
        {
            headerText.AppendLine($"[cyan]{_currentEntry.Method}[/] [white]{Markup.Escape(_currentEntry.Url ?? "")}[/]");
        }

        headerText.AppendLine($"[grey]Content-Type: {_currentEntry.ContentType}[/]");
        headerText.AppendLine();

        var content = new Rows(
            new Markup(headerText.ToString()),
            new Rule().RuleStyle("grey dim"),
            CreateScrollableContent(lines, maxLines)
        );

        return new Panel(content)
            .Header("[bold yellow]Payload Detail[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
    }

    private IRenderable CreateScrollableContent(string[] lines, int maxLines)
    {
        var displayLines = lines.Take(maxLines).ToList();

        // Add scroll indicator if content is truncated
        if (lines.Length > maxLines)
        {
            displayLines.Add($"[grey]... ({lines.Length - maxLines} more lines - scroll to view)[/]");
        }

        var markup = string.Join("\n", displayLines.Select(line =>
        {
            // Syntax highlight JSON if applicable
            if (_currentEntry?.ContentType == "application/json")
            {
                return HighlightJson(line);
            }
            return Markup.Escape(line);
        }));

        return new Markup(markup);
    }

    private bool IsJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var trimmed = content.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }

    private string HighlightJson(string line)
    {
        // Simple JSON syntax highlighting
        var escaped = Markup.Escape(line);

        // Highlight strings (values in quotes)
        escaped = System.Text.RegularExpressions.Regex.Replace(
            escaped,
            @"""([^""]+)""(?=\s*:)",
            "[cyan]\"$1\"[/]"); // Property names

        escaped = System.Text.RegularExpressions.Regex.Replace(
            escaped,
            @":\s*""([^""]*)""",
            ": [green]\"$1\"[/]"); // String values

        // Highlight numbers
        escaped = System.Text.RegularExpressions.Regex.Replace(
            escaped,
            @":\s*(\d+\.?\d*)",
            ": [yellow]$1[/]");

        // Highlight booleans and null
        escaped = System.Text.RegularExpressions.Regex.Replace(
            escaped,
            @"\b(true|false|null)\b",
            "[magenta]$1[/]");

        return escaped;
    }
}

public class PayloadEntry
{
    public DateTime Timestamp { get; set; }
    public string Title { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Method { get; set; }
    public string? Url { get; set; }
}
