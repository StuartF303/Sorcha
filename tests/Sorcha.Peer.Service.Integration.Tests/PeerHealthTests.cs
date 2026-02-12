// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Integration.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Sorcha.Peer.Service.Integration.Tests;

/// <summary>
/// Integration tests for peer service health and metrics endpoints
/// </summary>
[Collection("PeerIntegration")]
public class PeerHealthTests : IClassFixture<PeerTestFixture>
{
    private readonly PeerTestFixture _fixture;

    public PeerHealthTests(PeerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Healthy_Status()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        var response = await peer.HttpClient.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("healthy");
        health.Service.Should().Be("peer-service");
        health.Version.Should().NotBeNullOrEmpty();
        health.Uptime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Metrics_Endpoint_Should_Return_Current_Metrics()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        var response = await peer.HttpClient.GetAsync("/api/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metrics = await response.Content.ReadFromJsonAsync<MetricsResponse>();
        metrics.Should().NotBeNull();
        metrics!.UptimeSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Metrics_Via_gRPC_Should_Match_REST_Metrics()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        var grpcMetrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
        var restResponse = await peer.HttpClient.GetAsync("/api/metrics");
        var restMetrics = await restResponse.Content.ReadFromJsonAsync<MetricsResponse>();

        // Assert
        grpcMetrics.Should().NotBeNull();
        restMetrics.Should().NotBeNull();

        // Metrics should be very close (may not be exact due to timing)
        grpcMetrics.TotalTransactions.Should().BeGreaterOrEqualTo(restMetrics!.TotalTransactions - 10);
        grpcMetrics.UptimeSeconds.Should().BeCloseTo(restMetrics.UptimeSeconds, 5);
    }

    [Fact]
    public async Task Uptime_Should_Increase_Over_Time()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        var metrics1 = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
        var uptime1 = metrics1.UptimeSeconds;

        await Task.Delay(2000); // Wait 2 seconds

        var metrics2 = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
        var uptime2 = metrics2.UptimeSeconds;

        // Assert
        uptime2.Should().BeGreaterThan(uptime1);
        (uptime2 - uptime1).Should().BeGreaterOrEqualTo(1); // At least 1 second difference
    }

    [Fact]
    public async Task Health_Check_Should_Include_Metrics()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        var response = await peer.HttpClient.GetAsync("/api/health");
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();

        // Assert
        health.Should().NotBeNull();
        health!.Metrics.Should().NotBeNull();
        health.Metrics!.ActivePeers.Should().BeGreaterOrEqualTo(0);
        health.Metrics.TotalTransactions.Should().BeGreaterOrEqualTo(0);
        health.Metrics.ThroughputPerSecond.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task All_Peers_Should_Report_Healthy()
    {
        // Arrange & Act
        var healthChecks = await Task.WhenAll(_fixture.Peers.Select(async peer =>
        {
            var response = await peer.HttpClient.GetAsync("/api/health");
            var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
            return (peer.PeerId, health);
        }));

        // Assert
        foreach (var (peerId, health) in healthChecks)
        {
            health.Should().NotBeNull($"Health check for {peerId} should return data");
            health!.Status.Should().Be("healthy", $"Peer {peerId} should be healthy");
        }
    }

    // Helper classes for deserialization
    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Uptime { get; set; } = string.Empty;
        public HealthMetrics? Metrics { get; set; }
    }

    private class HealthMetrics
    {
        public int ActivePeers { get; set; }
        public long TotalTransactions { get; set; }
        public double ThroughputPerSecond { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
    }

    private class MetricsResponse
    {
        public int ActivePeers { get; set; }
        public long TotalTransactions { get; set; }
        public double ThroughputPerSecond { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long UptimeSeconds { get; set; }
    }
}
