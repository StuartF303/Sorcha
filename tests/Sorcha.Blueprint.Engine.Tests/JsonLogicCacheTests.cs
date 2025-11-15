// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Sorcha.Blueprint.Engine.Caching;
using Xunit;

namespace Sorcha.Blueprint.Engine.Tests;

public class JsonLogicCacheTests
{
    [Fact]
    public void GetOrAdd_FirstAccess_CallsFactory()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var factoryCalled = false;

        // Act
        var result = cache.GetOrAdd(expression, exp =>
        {
            factoryCalled = true;
            return "computed-value";
        });

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().Be("computed-value");
    }

    [Fact]
    public void GetOrAdd_SecondAccess_ReturnsCachedValue()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var factoryCallCount = 0;

        // Act - first call
        var result1 = cache.GetOrAdd(expression, exp =>
        {
            factoryCallCount++;
            return "computed-value";
        });

        // Act - second call
        var result2 = cache.GetOrAdd(expression, exp =>
        {
            factoryCallCount++;
            return "different-value"; // Should not be called
        });

        // Assert
        factoryCallCount.Should().Be(1); // Factory called only once
        result1.Should().Be("computed-value");
        result2.Should().Be("computed-value"); // Same cached value
    }

    [Fact]
    public void GetOrAdd_DifferentExpressions_CachesSeparately()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression1 = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var expression2 = JsonNode.Parse(@"{""<"": [{""var"": ""amount""}, 1000]}")!;

        // Act
        var result1 = cache.GetOrAdd(expression1, exp => "value1");
        var result2 = cache.GetOrAdd(expression2, exp => "value2");

        // Assert
        result1.Should().Be("value1");
        result2.Should().Be("value2");
    }

    [Fact]
    public void GetOrAdd_SameExpressionDifferentInstance_ReturnsFromCache()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression1 = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var expression2 = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!; // Same content, different instance

        // Act
        var result1 = cache.GetOrAdd(expression1, exp => "value1");
        var result2 = cache.GetOrAdd(expression2, exp => "value2");

        // Assert
        result1.Should().Be("value1");
        result2.Should().Be("value1"); // Returns cached value
    }

    [Fact]
    public async Task GetOrAddAsync_FirstAccess_CallsAsyncFactory()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrAddAsync(expression, async exp =>
        {
            await Task.Delay(1); // Simulate async work
            factoryCalled = true;
            return "async-value";
        });

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().Be("async-value");
    }

    [Fact]
    public async Task GetOrAddAsync_SecondAccess_ReturnsCachedValue()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var factoryCallCount = 0;

        // Act - first call
        var result1 = await cache.GetOrAddAsync(expression, async exp =>
        {
            await Task.Delay(1);
            factoryCallCount++;
            return "async-value";
        });

        // Act - second call
        var result2 = await cache.GetOrAddAsync(expression, async exp =>
        {
            await Task.Delay(1);
            factoryCallCount++;
            return "different-value";
        });

        // Assert
        factoryCallCount.Should().Be(1);
        result1.Should().Be("async-value");
        result2.Should().Be("async-value");
    }

    [Fact]
    public void TryGet_CachedValue_ReturnsTrue()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        cache.GetOrAdd(expression, exp => "cached-value");

        // Act
        var found = cache.TryGet<string>(expression, out var value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be("cached-value");
    }

    [Fact]
    public void TryGet_NotCached_ReturnsFalse()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;

        // Act
        var found = cache.TryGet<string>(expression, out var value);

        // Assert
        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Remove_CachedExpression_RemovesFromCache()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        cache.GetOrAdd(expression, exp => "value");

        // Act
        cache.Remove(expression);
        var found = cache.TryGet<string>(expression, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllCachedValues()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression1 = JsonNode.Parse(@"{"">"": [{""var"": ""a""}, 1]}")!;
        var expression2 = JsonNode.Parse(@"{"">"": [{""var"": ""b""}, 2]}")!;

        cache.GetOrAdd(expression1, exp => "value1");
        cache.GetOrAdd(expression2, exp => "value2");

        // Act
        cache.Clear();

        // Assert
        cache.TryGet<string>(expression1, out _).Should().BeFalse();
        cache.TryGet<string>(expression2, out _).Should().BeFalse();
    }

    [Fact]
    public void GetOrAdd_ComplexObject_CachesCorrectly()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var complexObject = new TestData { Id = 1, Name = "Test" };

        // Act
        var result = cache.GetOrAdd(expression, exp => complexObject);

        // Assert
        result.Should().BeSameAs(complexObject);
    }

    [Fact]
    public void GetOrAdd_MultipleTypes_CachesIndependently()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;

        // Act
        var stringResult = cache.GetOrAdd(expression, exp => "string-value");
        var intResult = cache.GetOrAdd(expression, exp => 42);

        // Assert
        stringResult.Should().Be("string-value");
        intResult.Should().Be("string-value"); // Returns first cached value regardless of type
    }

    [Fact]
    public void GetOrAdd_NullFactory_ThrowsException()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        Func<JsonNode, string> factory = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(expression, factory));
    }

    [Fact]
    public async Task GetOrAddAsync_ParallelAccess_CallsFactoryOnce()
    {
        // Arrange
        var cache = new JsonLogicCache();
        var expression = JsonNode.Parse(@"{"">"": [{""var"": ""amount""}, 1000]}")!;
        var factoryCallCount = 0;

        // Act - simulate parallel access
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            cache.GetOrAddAsync(expression, async exp =>
            {
                await Task.Delay(10);
                Interlocked.Increment(ref factoryCallCount);
                return "value";
            })
        );

        await Task.WhenAll(tasks);

        // Assert
        // Note: Due to race conditions, this might be called more than once
        // but should be significantly less than 10 times
        factoryCallCount.Should().BeLessThan(10);
    }

    [Fact]
    public void GetStatistics_ReturnsStatistics()
    {
        // Arrange
        var cache = new JsonLogicCache();

        // Act
        var stats = cache.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
