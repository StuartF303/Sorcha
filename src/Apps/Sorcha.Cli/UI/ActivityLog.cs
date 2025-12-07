// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Spectre.Console;
using System.Collections.Concurrent;

namespace Sorcha.Cli.UI;

/// <summary>
/// Thread-safe activity log for capturing API calls and events
/// </summary>
public class ActivityLog
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;

    public event Action? OnLogAdded;

    public ActivityLog(int maxEntries = 500)
    {
        _maxEntries = maxEntries;
    }

    public void LogRequest(string method, string url, string? body = null)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Request,
            Method = method,
            Url = url,
            Body = body
        });
    }

    public void LogResponse(int statusCode, string? body = null, long elapsedMs = 0)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Response,
            StatusCode = statusCode,
            Body = body,
            ElapsedMs = elapsedMs
        });
    }

    public void LogInfo(string message)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Info,
            Message = message
        });
    }

    public void LogSuccess(string message)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Success,
            Message = message
        });
    }

    public void LogWarning(string message)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Warning,
            Message = message
        });
    }

    public void LogError(string message, Exception? ex = null)
    {
        AddEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = LogType.Error,
            Message = message,
            Exception = ex
        });
    }

    private void AddEntry(LogEntry entry)
    {
        _entries.Enqueue(entry);

        // Trim old entries
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }

        OnLogAdded?.Invoke();
    }

    public IEnumerable<LogEntry> GetEntries(int count = 50)
    {
        return _entries.TakeLast(count);
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Renders the log panel as a Spectre.Console renderable
    /// </summary>
    public Panel RenderPanel(int height = 15)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Time").Width(12))
            .AddColumn(new TableColumn("Entry"));

        var entries = GetEntries(height - 2).ToList();

        foreach (var entry in entries)
        {
            var (timeMarkup, entryMarkup) = FormatEntry(entry);
            table.AddRow(timeMarkup, entryMarkup);
        }

        // Pad with empty rows if needed
        for (var i = entries.Count; i < height - 2; i++)
        {
            table.AddRow("", "");
        }

        return new Panel(table)
            .Header("[bold blue]Activity Log[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
    }

    private (Markup time, Markup entry) FormatEntry(LogEntry entry)
    {
        var timeStr = entry.Timestamp.ToString("HH:mm:ss.ff");
        var timeMarkup = new Markup($"[grey]{timeStr}[/]");

        var entryMarkup = entry.Type switch
        {
            LogType.Request => new Markup($"[cyan]>> {entry.Method}[/] [white]{TruncateUrl(entry.Url ?? "")}[/]"),
            LogType.Response => FormatResponse(entry),
            LogType.Info => new Markup($"[blue]i[/] {Markup.Escape(entry.Message ?? "")}"),
            LogType.Success => new Markup($"[green]✓[/] {Markup.Escape(entry.Message ?? "")}"),
            LogType.Warning => new Markup($"[yellow]![/] {Markup.Escape(entry.Message ?? "")}"),
            LogType.Error => new Markup($"[red]✗[/] {Markup.Escape(entry.Message ?? "")}"),
            _ => new Markup(Markup.Escape(entry.Message ?? ""))
        };

        return (timeMarkup, entryMarkup);
    }

    private Markup FormatResponse(LogEntry entry)
    {
        var statusColor = entry.StatusCode switch
        {
            >= 200 and < 300 => "green",
            >= 300 and < 400 => "yellow",
            >= 400 and < 500 => "orange1",
            >= 500 => "red",
            _ => "white"
        };

        var bodyPreview = string.IsNullOrEmpty(entry.Body)
            ? ""
            : $" [grey]({entry.Body.Length} bytes)[/]";

        return new Markup($"[{statusColor}]<< {entry.StatusCode}[/] [grey]{entry.ElapsedMs}ms[/]{bodyPreview}");
    }

    private string TruncateUrl(string url, int maxLength = 60)
    {
        if (url.Length <= maxLength) return url;
        return url[..(maxLength - 3)] + "...";
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogType Type { get; set; }
    public string? Method { get; set; }
    public string? Url { get; set; }
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string? Message { get; set; }
    public long ElapsedMs { get; set; }
    public Exception? Exception { get; set; }
}

public enum LogType
{
    Request,
    Response,
    Info,
    Success,
    Warning,
    Error
}
