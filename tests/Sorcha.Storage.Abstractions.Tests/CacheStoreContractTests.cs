// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.Abstractions;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Contract tests for ICacheStore. All implementations must satisfy these tests.
/// Derive from this class and implement CreateCacheStore() to test a specific implementation.
/// </summary>
public abstract class CacheStoreContractTests
{
    protected abstract ICacheStore CreateCacheStore();

    private ICacheStore Sut => CreateCacheStore();

    // ===========================
    // GetAsync
    // ===========================

    [Fact]
    public async Task GetAsync_NonexistentKey_ReturnsNull()
    {
        var result = await Sut.GetAsync<string>("contract-nonexistent");

        result.Should().BeNull();
    }

    // ===========================
    // SetAsync + GetAsync
    // ===========================

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        var sut = Sut;
        await sut.SetAsync("contract-set-1", "hello");

        var result = await sut.GetAsync<string>("contract-set-1");

        result.Should().Be("hello");
    }

    [Fact]
    public async Task SetAsync_ComplexObject_RoundTripsCorrectly()
    {
        var sut = Sut;
        var value = new CacheTestObject { Id = 42, Name = "Test" };
        await sut.SetAsync("contract-complex-1", value);

        var result = await sut.GetAsync<CacheTestObject>("contract-complex-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        var sut = Sut;
        await sut.SetAsync("contract-overwrite", "first");
        await sut.SetAsync("contract-overwrite", "second");

        var result = await sut.GetAsync<string>("contract-overwrite");

        result.Should().Be("second");
    }

    // ===========================
    // RemoveAsync
    // ===========================

    [Fact]
    public async Task RemoveAsync_ExistingKey_ReturnsTrueAndRemoves()
    {
        var sut = Sut;
        await sut.SetAsync("contract-remove-1", "value");

        var removed = await sut.RemoveAsync("contract-remove-1");
        var exists = await sut.ExistsAsync("contract-remove-1");

        removed.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_NonexistentKey_ReturnsFalse()
    {
        var result = await Sut.RemoveAsync("contract-remove-missing");

        result.Should().BeFalse();
    }

    // ===========================
    // ExistsAsync
    // ===========================

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        var sut = Sut;
        await sut.SetAsync("contract-exists-1", "value");

        var result = await sut.ExistsAsync("contract-exists-1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentKey_ReturnsFalse()
    {
        var result = await Sut.ExistsAsync("contract-exists-missing");

        result.Should().BeFalse();
    }

    // ===========================
    // GetOrSetAsync
    // ===========================

    [Fact]
    public async Task GetOrSetAsync_KeyMissing_CallsFactoryAndStores()
    {
        var sut = Sut;
        var factoryCalled = false;

        var result = await sut.GetOrSetAsync("contract-getorset-1", async _ =>
        {
            factoryCalled = true;
            return "factory-value";
        });

        var cached = await sut.GetAsync<string>("contract-getorset-1");

        result.Should().Be("factory-value");
        factoryCalled.Should().BeTrue();
        cached.Should().Be("factory-value");
    }

    [Fact]
    public async Task GetOrSetAsync_KeyExists_DoesNotCallFactory()
    {
        var sut = Sut;
        await sut.SetAsync("contract-getorset-2", "existing");
        var factoryCalled = false;

        var result = await sut.GetOrSetAsync("contract-getorset-2", async _ =>
        {
            factoryCalled = true;
            return "new-value";
        });

        result.Should().Be("existing");
        factoryCalled.Should().BeFalse();
    }

    // ===========================
    // RemoveByPatternAsync
    // ===========================

    [Fact]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys()
    {
        var sut = Sut;
        await sut.SetAsync("contract:pattern:1", "v1");
        await sut.SetAsync("contract:pattern:2", "v2");
        await sut.SetAsync("contract:other:1", "v3");

        var removed = await sut.RemoveByPatternAsync("contract:pattern:*");

        removed.Should().Be(2);
        (await sut.ExistsAsync("contract:pattern:1")).Should().BeFalse();
        (await sut.ExistsAsync("contract:pattern:2")).Should().BeFalse();
        (await sut.ExistsAsync("contract:other:1")).Should().BeTrue();
    }

    // ===========================
    // IncrementAsync
    // ===========================

    [Fact]
    public async Task IncrementAsync_NewKey_CreatesWithDelta()
    {
        var result = await Sut.IncrementAsync("contract-incr-new", 5);

        result.Should().Be(5);
    }

    [Fact]
    public async Task IncrementAsync_ExistingKey_IncrementsByDelta()
    {
        var sut = Sut;
        await sut.IncrementAsync("contract-incr-existing", 10);

        var result = await sut.IncrementAsync("contract-incr-existing", 3);

        result.Should().Be(13);
    }

    // ===========================
    // GetStatisticsAsync
    // ===========================

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        var sut = Sut;
        await sut.SetAsync("contract-stats-key", "value");
        await sut.GetAsync<string>("contract-stats-key");    // hit
        await sut.GetAsync<string>("contract-stats-missing"); // miss

        var stats = await sut.GetStatisticsAsync();

        stats.Should().NotBeNull();
        stats.TotalRequests.Should().BeGreaterThanOrEqualTo(2);
    }

    // ===========================
    // Test Helpers
    // ===========================

    protected class CacheTestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
