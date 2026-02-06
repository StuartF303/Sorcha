// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class RetryBackoffValidatorTests
{
    [Fact]
    public void CalculateBackoff_AttemptZero_ReturnsZero()
    {
        // Act
        var result = RetryBackoffValidator.CalculateBackoff(0);

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CalculateBackoff_NegativeAttempt_ReturnsZero()
    {
        // Act
        var result = RetryBackoffValidator.CalculateBackoff(-1);

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    [InlineData(5, 16)]
    [InlineData(6, 32)]
    public void CalculateBackoff_ExponentialGrowth_ReturnsCorrectDelay(int attempt, int expectedSeconds)
    {
        // Act
        var result = RetryBackoffValidator.CalculateBackoff(attempt);

        // Assert
        result.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(100)]
    public void CalculateBackoff_AboveCap_Returns60Seconds(int attempt)
    {
        // Act
        var result = RetryBackoffValidator.CalculateBackoff(attempt);

        // Assert
        result.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void GetExpectedBackoffSequence_Returns10Elements()
    {
        // Act
        var sequence = RetryBackoffValidator.GetExpectedBackoffSequence();

        // Assert
        sequence.Should().HaveCount(10);
    }

    [Fact]
    public void GetExpectedBackoffSequence_MatchesExpectedValues()
    {
        // Arrange
        var expected = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(16),
            TimeSpan.FromSeconds(32),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60)
        };

        // Act
        var sequence = RetryBackoffValidator.GetExpectedBackoffSequence();

        // Assert
        sequence.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(-1, false)]
    public void IsValidAttemptNumber_ReturnsCorrectResult(int attemptNumber, bool expected)
    {
        // Act
        var result = RetryBackoffValidator.IsValidAttemptNumber(attemptNumber);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Constants_MatchPeerServiceConstants()
    {
        // Assert
        RetryBackoffValidator.InitialDelaySeconds.Should().Be(PeerServiceConstants.RetryInitialDelaySeconds);
        RetryBackoffValidator.MaxDelaySeconds.Should().Be(PeerServiceConstants.RetryMaxDelaySeconds);
        RetryBackoffValidator.Multiplier.Should().Be(PeerServiceConstants.RetryMultiplier);
        RetryBackoffValidator.MaxRetryAttempts.Should().Be(PeerServiceConstants.MaxRetryAttempts);
    }
}
