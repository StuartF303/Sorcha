// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Options;
using Sorcha.Storage.Abstractions;
using Sorcha.Storage.Abstractions.Caching;
using Sorcha.Storage.InMemory;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Integration tests for VerifiedCache using actual InMemory implementations.
/// </summary>
public class VerifiedCacheIntegrationTests
{
    private readonly InMemoryCacheStore _cacheStore;
    private readonly InMemoryWormStore<RegisterDocket, ulong> _wormStore;
    private readonly IOptions<VerifiedCacheConfiguration> _options;
    private readonly VerifiedCache<RegisterDocket, ulong> _sut;

    public VerifiedCacheIntegrationTests()
    {
        _cacheStore = new InMemoryCacheStore();
        _wormStore = new InMemoryWormStore<RegisterDocket, ulong>(d => d.Height);
        _options = Options.Create(new VerifiedCacheConfiguration
        {
            KeyPrefix = "register:docket:",
            CacheTtlSeconds = 3600,
            EnableHashVerification = true,
            WarmingBatchSize = 100,
            StartupStrategy = CacheStartupStrategy.Progressive,
            BlockingThreshold = 1000
        });

        _sut = new VerifiedCache<RegisterDocket, ulong>(
            _cacheStore,
            _wormStore,
            d => d.Height,
            _options,
            d => d.Hash);
    }

    [Fact]
    public async Task FullWorkflow_AppendAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var docket = new RegisterDocket
        {
            Height = 1,
            Hash = "abc123",
            Payload = "test payload",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act - Append through verified cache
        var appended = await _sut.AppendAsync(docket);

        // Assert - Should be retrievable from cache
        var retrieved = await _sut.GetAsync(1UL);
        retrieved.Should().NotBeNull();
        retrieved!.Height.Should().Be(1);
        retrieved.Hash.Should().Be("abc123");

        // Verify statistics
        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().Be(1); // GetAsync should hit cache
        stats.CacheMisses.Should().Be(0);
    }

    [Fact]
    public async Task CacheMiss_FetchesFromWormStore()
    {
        // Arrange - Add directly to WORM store (bypassing cache)
        var docket = new RegisterDocket
        {
            Height = 2,
            Hash = "def456",
            Payload = "direct worm insert",
            Timestamp = DateTimeOffset.UtcNow
        };
        await _wormStore.AppendAsync(docket);

        // Act - Retrieve through verified cache
        var retrieved = await _sut.GetAsync(2UL);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Hash.Should().Be("def456");

        // Should have been a cache miss initially
        var stats = await _sut.GetStatisticsAsync();
        stats.CacheMisses.Should().Be(1);
        stats.WormFetches.Should().Be(1);

        // Second retrieval should hit cache
        var retrieved2 = await _sut.GetAsync(2UL);
        retrieved2.Should().NotBeNull();

        var stats2 = await _sut.GetStatisticsAsync();
        stats2.CacheHits.Should().Be(1);
    }

    [Fact]
    public async Task GetManyAsync_MixedCacheHitsAndMisses()
    {
        // Arrange
        var dockets = Enumerable.Range(1, 5).Select(i => new RegisterDocket
        {
            Height = (ulong)i,
            Hash = $"hash{i}",
            Payload = $"payload{i}",
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        // Add some through cache, some directly to WORM
        await _sut.AppendAsync(dockets[0]); // In cache
        await _sut.AppendAsync(dockets[1]); // In cache
        await _wormStore.AppendAsync(dockets[2]); // WORM only
        await _wormStore.AppendAsync(dockets[3]); // WORM only
        await _sut.AppendAsync(dockets[4]); // In cache

        // Act
        var result = await _sut.GetManyAsync(new ulong[] { 1, 2, 3, 4, 5 });

        // Assert
        result.Should().HaveCount(5);

        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().Be(3); // 1, 2, 5 in cache
        stats.CacheMisses.Should().Be(2); // 3, 4 not in cache
        stats.WormFetches.Should().Be(2); // Fetched 3, 4 from WORM
    }

    [Fact]
    public async Task GetRangeAsync_RetrievesRangeFromWorm()
    {
        // Arrange
        var dockets = Enumerable.Range(1, 10).Select(i => new RegisterDocket
        {
            Height = (ulong)i,
            Hash = $"hash{i}",
            Payload = $"payload{i}",
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        foreach (var d in dockets)
        {
            await _wormStore.AppendAsync(d);
        }

        // Act
        var result = await _sut.GetRangeAsync(3UL, 7UL);

        // Assert
        result.Should().HaveCount(5);
        result.Select(d => d.Height).Should().BeEquivalentTo(new ulong[] { 3, 4, 5, 6, 7 });

        // Range queries should populate cache
        var cached = await _sut.GetAsync(5UL);
        cached.Should().NotBeNull();

        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Invalidate_RemovesFromCache()
    {
        // Arrange
        var docket = new RegisterDocket
        {
            Height = 100,
            Hash = "invalidate_test",
            Payload = "will be invalidated",
            Timestamp = DateTimeOffset.UtcNow
        };
        await _sut.AppendAsync(docket);

        // Verify in cache
        var initial = await _sut.GetAsync(100UL);
        initial.Should().NotBeNull();
        var statsInitial = await _sut.GetStatisticsAsync();
        statsInitial.CacheHits.Should().Be(1);

        // Act
        await _sut.InvalidateAsync(100UL);

        // Assert - Next retrieval should be a cache miss (fetches from WORM)
        var afterInvalidate = await _sut.GetAsync(100UL);
        afterInvalidate.Should().NotBeNull();

        var statsAfter = await _sut.GetStatisticsAsync();
        statsAfter.CacheMisses.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentSequenceAsync_ReturnsWormStoreSequence()
    {
        // Arrange
        var dockets = Enumerable.Range(1, 5).Select(i => new RegisterDocket
        {
            Height = (ulong)i,
            Hash = $"hash{i}",
            Payload = $"payload{i}",
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        foreach (var d in dockets)
        {
            await _sut.AppendAsync(d);
        }

        // Act
        var sequence = await _sut.GetCurrentSequenceAsync();

        // Assert
        sequence.Should().Be(5UL);
    }

    [Fact]
    public async Task WarmCacheAsync_PopulatesCache()
    {
        // Arrange - Add documents directly to WORM
        var dockets = Enumerable.Range(1, 20).Select(i => new RegisterDocket
        {
            Height = (ulong)i,
            Hash = $"hash{i}",
            Payload = $"payload{i}",
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        foreach (var d in dockets)
        {
            await _wormStore.AppendAsync(d);
        }

        // Verify cache is empty
        var prewarmStats = await _sut.GetStatisticsAsync();
        prewarmStats.CacheHits.Should().Be(0);

        // Act - Warm the cache
        var progress = new Progress<CacheWarmingProgress>();
        await _sut.WarmCacheAsync(20UL, progress);

        // Assert - All documents should now be in cache
        for (ulong i = 1; i <= 20; i++)
        {
            var doc = await _sut.GetAsync(i);
            doc.Should().NotBeNull();
        }

        var postwarmStats = await _sut.GetStatisticsAsync();
        postwarmStats.CacheHits.Should().Be(20); // All from cache
    }

    [Fact]
    public async Task VerifyIntegrityAsync_ReturnsValidForConsistentData()
    {
        // Arrange
        var dockets = Enumerable.Range(1, 5).Select(i => new RegisterDocket
        {
            Height = (ulong)i,
            Hash = $"hash{i}",
            Payload = $"payload{i}",
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        foreach (var d in dockets)
        {
            await _sut.AppendAsync(d);
        }

        // Act
        var result = await _sut.VerifyIntegrityAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.DocumentsVerified.Should().Be(5);
        result.MismatchCount.Should().Be(0);
    }

    [Fact]
    public async Task Statistics_TrackLatency()
    {
        // Arrange & Act
        for (int i = 0; i < 10; i++)
        {
            var docket = new RegisterDocket
            {
                Height = (ulong)(i + 1),
                Hash = $"hash{i}",
                Payload = $"payload{i}",
                Timestamp = DateTimeOffset.UtcNow
            };
            await _sut.AppendAsync(docket);
            await _sut.GetAsync((ulong)(i + 1));
        }

        // Assert
        var stats = await _sut.GetStatisticsAsync();
        stats.AverageLatencyMs.Should().BeGreaterThan(0);
        stats.HitRate.Should().BeGreaterThan(0);
    }
}

/// <summary>
/// Test document representing a register docket entry.
/// </summary>
public class RegisterDocket
{
    public ulong Height { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
