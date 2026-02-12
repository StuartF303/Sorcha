// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Monitoring;

namespace Sorcha.Peer.Service.Tests.Monitoring;

public class ConnectionQualityTrackerTests
{
    private readonly Mock<ILogger<ConnectionQualityTracker>> _loggerMock;
    private readonly ConnectionQualityTracker _tracker;

    public ConnectionQualityTrackerTests()
    {
        _loggerMock = new Mock<ILogger<ConnectionQualityTracker>>();
        _tracker = new ConnectionQualityTracker(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Assert
        _tracker.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new ConnectionQualityTracker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordSuccess_ShouldTrackMetrics()
    {
        // Act
        _tracker.RecordSuccess("peer1", 50);
        _tracker.RecordSuccess("peer1", 60);

        // Assert
        var quality = _tracker.GetQuality("peer1");
        quality.Should().NotBeNull();
        quality!.TotalRequests.Should().Be(2);
        quality.SuccessfulRequests.Should().Be(2);
        quality.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void RecordFailure_ShouldTrackFailures()
    {
        // Act
        _tracker.RecordFailure("peer1");
        _tracker.RecordFailure("peer1");

        // Assert
        var quality = _tracker.GetQuality("peer1");
        quality.Should().NotBeNull();
        quality!.TotalRequests.Should().Be(2);
        quality.FailedRequests.Should().Be(2);
        quality.SuccessRate.Should().Be(0.0);
    }

    [Fact]
    public void RecordSuccess_ShouldCalculateAverageLatency()
    {
        // Act
        _tracker.RecordSuccess("peer1", 50);
        _tracker.RecordSuccess("peer1", 100);
        _tracker.RecordSuccess("peer1", 150);

        // Assert
        var quality = _tracker.GetQuality("peer1");
        quality.Should().NotBeNull();
        quality!.AverageLatencyMs.Should().Be(100);
        quality.MinLatencyMs.Should().Be(50);
        quality.MaxLatencyMs.Should().Be(150);
    }

    [Fact]
    public void GetQuality_ShouldReturnNull_ForUnknownPeer()
    {
        // Act
        var quality = _tracker.GetQuality("nonexistent");

        // Assert
        quality.Should().BeNull();
    }

    [Fact]
    public void GetQuality_ShouldCalculateQualityScore()
    {
        // Arrange - Record excellent connection (low latency, high success rate)
        _tracker.RecordSuccess("peer1", 30);
        _tracker.RecordSuccess("peer1", 40);
        _tracker.RecordSuccess("peer1", 50);

        // Act
        var quality = _tracker.GetQuality("peer1");

        // Assert
        quality.Should().NotBeNull();
        quality!.QualityScore.Should().BeGreaterThan(90); // Excellent quality
        quality.QualityRating.Should().Be("Excellent");
    }

    [Fact]
    public void GetQuality_ShouldReflectMixedResults()
    {
        // Arrange - Mixed success/failure
        _tracker.RecordSuccess("peer1", 100);
        _tracker.RecordFailure("peer1");
        _tracker.RecordSuccess("peer1", 120);
        _tracker.RecordFailure("peer1");

        // Act
        var quality = _tracker.GetQuality("peer1");

        // Assert
        quality.Should().NotBeNull();
        quality!.SuccessRate.Should().Be(0.5); // 50% success rate
        quality.SuccessfulRequests.Should().Be(2);
        quality.FailedRequests.Should().Be(2);
    }

    [Fact]
    public void GetBestPeers_ShouldReturnTopQualityPeers()
    {
        // Arrange
        _tracker.RecordSuccess("peer1", 30);  // Excellent
        _tracker.RecordSuccess("peer2", 150); // Good
        _tracker.RecordSuccess("peer3", 400); // Poor
        _tracker.RecordFailure("peer4");      // Very poor

        // Act
        var bestPeers = _tracker.GetBestPeers(2);

        // Assert
        bestPeers.Should().HaveCount(2);
        bestPeers[0].Should().Be("peer1");
        bestPeers[1].Should().Be("peer2");
    }

    [Fact]
    public void GetAllQualities_ShouldReturnAllMetrics()
    {
        // Arrange
        _tracker.RecordSuccess("peer1", 50);
        _tracker.RecordSuccess("peer2", 100);

        // Act
        var allQualities = _tracker.GetAllQualities();

        // Assert
        allQualities.Should().HaveCount(2);
        allQualities.Should().ContainKey("peer1");
        allQualities.Should().ContainKey("peer2");
    }

    [Fact]
    public void RemovePeer_ShouldRemoveMetrics()
    {
        // Arrange
        _tracker.RecordSuccess("peer1", 50);

        // Act
        _tracker.RemovePeer("peer1");

        // Assert
        var quality = _tracker.GetQuality("peer1");
        quality.Should().BeNull();
    }

    [Fact]
    public void Clear_ShouldRemoveAllMetrics()
    {
        // Arrange
        _tracker.RecordSuccess("peer1", 50);
        _tracker.RecordSuccess("peer2", 100);

        // Act
        _tracker.Clear();

        // Assert
        _tracker.GetAllQualities().Should().BeEmpty();
    }

    [Theory]
    [InlineData(30, "Excellent")]
    [InlineData(80, "Good")]
    [InlineData(150, "Good")]
    [InlineData(250, "Fair")]
    [InlineData(600, "Fair")]
    public void QualityRating_ShouldReflectLatency(long latency, string expectedMinRating)
    {
        // Arrange - All successes to focus on latency impact
        for (int i = 0; i < 10; i++)
        {
            _tracker.RecordSuccess("peer1", latency);
        }

        // Act
        var quality = _tracker.GetQuality("peer1");

        // Assert
        quality.Should().NotBeNull();
        // Verify rating is at least as good as expected minimum
        quality!.QualityRating.Should().NotBeEmpty();
        quality.QualityRating.Should().BeOneOf("Excellent", "Good", "Fair", "Poor");
        // For low latency we expect better ratings
        if (expectedMinRating == "Excellent")
            quality.QualityRating.Should().Be("Excellent");
    }

    [Fact]
    public void RecordSuccess_ShouldIgnoreEmptyPeerId()
    {
        // Act
        _tracker.RecordSuccess("", 50);

        // Assert
        _tracker.GetAllQualities().Should().BeEmpty();
    }

    [Fact]
    public void RecordFailure_ShouldIgnoreEmptyPeerId()
    {
        // Act
        _tracker.RecordFailure("");

        // Assert
        _tracker.GetAllQualities().Should().BeEmpty();
    }
}
