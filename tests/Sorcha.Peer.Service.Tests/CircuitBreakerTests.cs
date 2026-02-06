// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Communication;

namespace Sorcha.Peer.Service.Tests;

public class CircuitBreakerTests
{
    private readonly Mock<ILogger<CircuitBreaker>> _loggerMock = new();

    private CircuitBreaker CreateCircuitBreaker(
        int failureThreshold = 5,
        TimeSpan? resetTimeout = null)
    {
        return new CircuitBreaker(
            _loggerMock.Object,
            "test-breaker",
            failureThreshold,
            resetTimeout);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CircuitBreaker(null!, "test");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CircuitBreaker(_loggerMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void State_InitiallyClosed()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_StateStaysClosed()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        await breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_FailedOperation_ThrowsCircuitBreakerException()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        var act = () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("test error"));

        // Assert
        await act.Should().ThrowAsync<CircuitBreakerException>();
    }

    [Fact]
    public async Task ExecuteAsync_FailedOperation_InnerExceptionPreserved()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();
        var innerException = new InvalidOperationException("test error");

        // Act
        var act = () => breaker.ExecuteAsync<int>(() => throw innerException);

        // Assert
        (await act.Should().ThrowAsync<CircuitBreakerException>())
            .WithInnerException<InvalidOperationException>()
            .WithMessage("test error");
    }

    [Fact]
    public async Task FailureCount_IncrementsOnEachFailure()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 10);

        // Act - cause 3 failures
        for (int i = 0; i < 3; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }

        // Assert
        var stats = breaker.GetStats();
        stats.FailureCount.Should().Be(3);
    }

    [Fact]
    public async Task State_TransitionsToOpen_AfterThresholdFailures()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 3);

        // Act - cause 3 failures (at threshold)
        for (int i = 0; i < 3; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task State_RemainsClosedBeforeThreshold()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 5);

        // Act - cause 4 failures (below threshold)
        for (int i = 0; i < 4; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpen_ThrowsCircuitBreakerOpenException()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 1);
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }

        // Act
        var act = () => breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        await act.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithFallback_WhenOpen_UsesFallback()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 1);
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }

        // Act
        var result = await breaker.ExecuteAsync(
            () => Task.FromResult(42),
            () => Task.FromResult(-1));

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public async Task ExecuteAsync_WithFallback_OnOperationFailure_UsesFallback()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        var result = await breaker.ExecuteAsync<int>(
            () => throw new InvalidOperationException("fail"),
            () => Task.FromResult(-1));

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public async Task ExecuteAsync_WithFallback_OnSuccess_ReturnsPrimaryResult()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        var result = await breaker.ExecuteAsync(
            () => Task.FromResult(42),
            () => Task.FromResult(-1));

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Reset_SetsStateToClosed()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        breaker.Reset();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task Reset_ZeroesFailureCount()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 10);
        for (int i = 0; i < 3; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }

        // Act
        breaker.Reset();

        // Assert
        var stats = breaker.GetStats();
        stats.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task Reset_AfterOpen_AllowsExecutionAgain()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 1);
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }
        breaker.State.Should().Be(CircuitState.Open);

        // Act
        breaker.Reset();
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetStats_ReturnsCorrectValues()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 7);

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.Name.Should().Be("test-breaker");
        stats.State.Should().Be(CircuitState.Closed);
        stats.FailureCount.Should().Be(0);
        stats.FailureThreshold.Should().Be(7);
        stats.LastFailureTime.Should().Be(DateTimeOffset.MinValue);
        stats.OpenedAt.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task GetStats_AfterFailures_ReflectsState()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 2);
        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.State.Should().Be(CircuitState.Open);
        stats.FailureCount.Should().Be(2);
        stats.LastFailureTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        stats.OpenedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CustomThreshold_TriggersAtCorrectCount()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 3);

        // Act - 2 failures (below threshold)
        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }
        breaker.State.Should().Be(CircuitState.Closed);

        // Act - 3rd failure (at threshold)
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task SuccessInClosedState_ResetsFailureCount()
    {
        // Arrange
        var breaker = CreateCircuitBreaker(failureThreshold: 5);

        // Cause 3 failures
        for (int i = 0; i < 3; i++)
        {
            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
            catch (CircuitBreakerException) { }
        }
        breaker.GetStats().FailureCount.Should().Be(3);

        // Act - success should reset
        await breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        breaker.GetStats().FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task HalfOpen_SuccessTransitionsToClosed()
    {
        // Arrange - open the breaker with a very short timeout
        var breaker = CreateCircuitBreaker(failureThreshold: 1, resetTimeout: TimeSpan.FromMilliseconds(50));
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }
        breaker.State.Should().Be(CircuitState.Open);

        // Wait for timeout to transition to HalfOpen
        await Task.Delay(100);
        breaker.State.Should().Be(CircuitState.HalfOpen);

        // Act - success in HalfOpen should close
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task HalfOpen_FailureTransitionsToOpen()
    {
        // Arrange - open the breaker with a very short timeout
        var breaker = CreateCircuitBreaker(failureThreshold: 1, resetTimeout: TimeSpan.FromMilliseconds(50));
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); }
        catch (CircuitBreakerException) { }

        // Wait for timeout to transition to HalfOpen
        await Task.Delay(100);
        breaker.State.Should().Be(CircuitState.HalfOpen);

        // Act - failure in HalfOpen should re-open
        try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail again")); }
        catch (CircuitBreakerException) { }

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void GetStats_ResetTimeout_ReflectsConfiguration()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);
        var breaker = CreateCircuitBreaker(resetTimeout: timeout);

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.ResetTimeout.Should().Be(timeout);
    }

    [Fact]
    public void Constructor_DefaultResetTimeout_IsOneMinute()
    {
        // Arrange
        var breaker = CreateCircuitBreaker();

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.ResetTimeout.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Constructor_DefaultThreshold_IsFive()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, "test");

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.FailureThreshold.Should().Be(5);
    }
}
