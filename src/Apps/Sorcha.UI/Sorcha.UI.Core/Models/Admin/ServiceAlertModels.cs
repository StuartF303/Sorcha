// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// Severity level for a service alert.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Represents an active alert generated from service metric evaluation.
/// </summary>
public record ServiceAlert
{
    public string Id { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MetricName { get; init; }
    public double? CurrentValue { get; init; }
    public double? Threshold { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Response containing all active alerts and summary counts.
/// </summary>
public record AlertsResponse
{
    public IReadOnlyList<ServiceAlert> Alerts { get; init; } = [];
    public int InfoCount { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public int CriticalCount { get; init; }
    public int TotalCount { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
