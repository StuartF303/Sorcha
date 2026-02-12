// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.InMemory;
using Xunit;

namespace Sorcha.Storage.InMemory.Tests;

public class InMemoryCacheStoreTests
{
    private readonly InMemoryCacheStore _sut;

    public InMemoryCacheStoreTests()
    {
        _sut = new InMemoryCacheStore();
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetAsync<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _sut.SetAsync(key, value);
        var result = await _sut.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var key = "complex-key";
        var value = new TestObject { Id = 42, Name = "Test" };

        // Act
        await _sut.SetAsync(key, value);
        var result = await _sut.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ExpiresEntry()
    {
        // Arrange
        var key = "expiring-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMilliseconds(50);

        // Act
        await _sut.SetAsync(key, value, expiration);
        var beforeExpiration = await _sut.GetAsync<string>(key);
        await Task.Delay(100);
        var afterExpiration = await _sut.GetAsync<string>(key);

        // Assert
        beforeExpiration.Should().Be(value);
        afterExpiration.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyExists_ReturnsTrueAndRemoves()
    {
        // Arrange
        var key = "remove-key";
        await _sut.SetAsync(key, "value");

        // Act
        var result = await _sut.RemoveAsync(key);
        var exists = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _sut.RemoveAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var key = "exists-key";
        await _sut.SetAsync(key, "value");

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_CallsFactoryAndStores()
    {
        // Arrange
        var key = "factory-key";
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrSetAsync(key, async _ =>
        {
            factoryCalled = true;
            return "factory-value";
        });

        var cachedValue = await _sut.GetAsync<string>(key);

        // Assert
        result.Should().Be("factory-value");
        factoryCalled.Should().BeTrue();
        cachedValue.Should().Be("factory-value");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_DoesNotCallFactory()
    {
        // Arrange
        var key = "existing-key";
        await _sut.SetAsync(key, "existing-value");
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrSetAsync(key, async _ =>
        {
            factoryCalled = true;
            return "new-value";
        });

        // Assert
        result.Should().Be("existing-value");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys()
    {
        // Arrange
        await _sut.SetAsync("user:1", "value1");
        await _sut.SetAsync("user:2", "value2");
        await _sut.SetAsync("other:1", "value3");

        // Act
        var removed = await _sut.RemoveByPatternAsync("user:*");

        // Assert
        removed.Should().Be(2);
        (await _sut.ExistsAsync("user:1")).Should().BeFalse();
        (await _sut.ExistsAsync("user:2")).Should().BeFalse();
        (await _sut.ExistsAsync("other:1")).Should().BeTrue();
    }

    [Fact]
    public async Task IncrementAsync_WhenKeyDoesNotExist_CreatesWithDelta()
    {
        // Arrange
        var key = "counter";

        // Act
        var result = await _sut.IncrementAsync(key, 5);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task IncrementAsync_WhenKeyExists_IncrementsValue()
    {
        // Arrange
        var key = "counter";
        await _sut.IncrementAsync(key, 10);

        // Act
        var result = await _sut.IncrementAsync(key, 5);

        // Assert
        result.Should().Be(15);
    }

    [Fact]
    public async Task GetStatisticsAsync_TracksHitsAndMisses()
    {
        // Arrange
        var key = "stats-key";
        await _sut.SetAsync(key, "value");

        // Act
        await _sut.GetAsync<string>(key); // Hit
        await _sut.GetAsync<string>(key); // Hit
        await _sut.GetAsync<string>("nonexistent"); // Miss

        var stats = await _sut.GetStatisticsAsync();

        // Assert
        stats.Hits.Should().Be(2);
        stats.Misses.Should().Be(1);
        stats.TotalRequests.Should().Be(3);
        stats.HitRate.Should().BeApproximately(0.666, 0.01);
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
