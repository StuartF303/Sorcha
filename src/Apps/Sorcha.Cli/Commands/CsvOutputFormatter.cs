using System.Reflection;
using System.Text;

namespace Sorcha.Cli.Commands;

/// <summary>
/// CSV output formatter.
/// </summary>
public class CsvOutputFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string FormatSingle<T>(T data) where T : class
    {
        if (data == null)
        {
            return string.Empty;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

        // Data row
        var values = properties.Select(p => EscapeCsv(p.GetValue(data)?.ToString() ?? string.Empty));
        sb.AppendLine(string.Join(",", values));

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string FormatCollection<T>(IEnumerable<T> data) where T : class
    {
        var items = data.ToList();
        if (!items.Any())
        {
            return string.Empty;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

        // Data rows
        foreach (var item in items)
        {
            var values = properties.Select(p => EscapeCsv(p.GetValue(item)?.ToString() ?? string.Empty));
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string FormatMessage(string message)
    {
        return message;
    }

    /// <summary>
    /// Escapes a value for CSV output.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
