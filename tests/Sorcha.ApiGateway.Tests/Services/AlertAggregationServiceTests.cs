// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.ApiGateway.Models;
using Sorcha.ApiGateway.Services;

namespace Sorcha.ApiGateway.Tests.Services;

public class AlertAggregationServiceTests
{
    [Fact]
    public void EvaluateThreshold_ValueBelowWarning_NoAlerts()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThreshold(
            alerts, "validator", "TotalFailed", 5,
            warningThreshold: 10, criticalThreshold: 50,
            "Validator has {0} failed validations");

        alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateThreshold_ValueAtWarning_WarningAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThreshold(
            alerts, "validator", "TotalFailed", 10,
            warningThreshold: 10, criticalThreshold: 50,
            "Validator has {0} failed validations");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Warning);
        alerts[0].Source.Should().Be("validator");
        alerts[0].MetricName.Should().Be("TotalFailed");
        alerts[0].CurrentValue.Should().Be(10);
        alerts[0].Threshold.Should().Be(10);
    }

    [Fact]
    public void EvaluateThreshold_ValueAboveWarningBelowCritical_WarningAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThreshold(
            alerts, "validator", "TotalFailed", 30,
            warningThreshold: 10, criticalThreshold: 50,
            "Validator has {0} failed validations");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void EvaluateThreshold_ValueAtCritical_CriticalAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThreshold(
            alerts, "validator", "TotalFailed", 50,
            warningThreshold: 10, criticalThreshold: 50,
            "Validator has {0} failed validations");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Critical);
        alerts[0].Threshold.Should().Be(50);
    }

    [Fact]
    public void EvaluateThreshold_ValueAboveCritical_CriticalAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThreshold(
            alerts, "peer", "AverageLatency", 3000,
            warningThreshold: 500, criticalThreshold: 2000,
            "Peer average latency is {0:F0}ms");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Critical);
        alerts[0].Id.Should().Be("peer-AverageLatency-critical");
        alerts[0].Message.Should().Contain("3000");
    }

    [Fact]
    public void EvaluateThresholdInverted_ValueAboveWarning_NoAlerts()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThresholdInverted(
            alerts, "validator", "SuccessRate", 98,
            warningThreshold: 95, criticalThreshold: 80,
            "Validator success rate is {0:F1}%");

        alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateThresholdInverted_ValueAtWarning_WarningAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThresholdInverted(
            alerts, "validator", "SuccessRate", 95,
            warningThreshold: 95, criticalThreshold: 80,
            "Validator success rate is {0:F1}%");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void EvaluateThresholdInverted_ValueBelowCritical_CriticalAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThresholdInverted(
            alerts, "peer", "HealthPercentage", 30,
            warningThreshold: 70, criticalThreshold: 40,
            "Peer network health is {0:F1}%");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Critical);
        alerts[0].Id.Should().Be("peer-HealthPercentage-critical");
    }

    [Fact]
    public void EvaluateThresholdInverted_ValueBetweenThresholds_WarningAlert()
    {
        var alerts = new List<ServiceAlert>();

        AlertAggregationService.EvaluateThresholdInverted(
            alerts, "peer", "HealthPercentage", 55,
            warningThreshold: 70, criticalThreshold: 40,
            "Peer network health is {0:F1}%");

        alerts.Should().HaveCount(1);
        alerts[0].Severity.Should().Be(AlertSeverity.Warning);
    }
}
