// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.ApiGateway.Models;

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
    public required string Id { get; init; }
    public required AlertSeverity Severity { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }
    public string? MetricName { get; init; }
    public double? CurrentValue { get; init; }
    public double? Threshold { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
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
    public int TotalCount => Alerts.Count;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Configuration for alert threshold values. Bindable from appsettings.json.
/// </summary>
public class AlertThresholdConfig
{
    public double ValidatorFailedWarning { get; set; } = 10;
    public double ValidatorFailedCritical { get; set; } = 50;
    public double ValidatorSuccessRateWarning { get; set; } = 95;
    public double ValidatorSuccessRateCritical { get; set; } = 80;
    public double ConsensusFailuresWarning { get; set; } = 5;
    public double ConsensusFailuresCritical { get; set; } = 20;
    public double DocketsAbandonedWarning { get; set; } = 3;
    public double DocketsAbandonedCritical { get; set; } = 10;
    public double ValidatorExceptionsWarning { get; set; } = 5;
    public double ValidatorExceptionsCritical { get; set; } = 25;
    public double PeerHealthPercentageWarning { get; set; } = 70;
    public double PeerHealthPercentageCritical { get; set; } = 40;
    public double PeerAverageLatencyWarning { get; set; } = 500;
    public double PeerAverageLatencyCritical { get; set; } = 2000;
}
