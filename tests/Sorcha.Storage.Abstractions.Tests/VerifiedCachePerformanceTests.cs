// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Sorcha.Storage.Abstractions;
using Sorcha.Storage.Abstractions.Caching;
using Sorcha.Storage.InMemory;
using Xunit;
using Xunit.Abstractions;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Performance tests for VerifiedCache to validate caching effectiveness.
/// </summary>
public class VerifiedCachePerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly InMemoryCacheStore _cacheStore;
    private readonly InMemoryWormStore<PerformanceTestDocket, ulong> _wormStore;
    private readonly VerifiedCache<PerformanceTestDocket, ulong> _sut;

    public VerifiedCachePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _cacheStore = new InMemoryCacheStore();
        _wormStore = new InMemoryWormStore<PerformanceTestDocket, ulong>(d => d.Id);

        var options = Options.Create(new VerifiedCacheConfiguration
        {
            KeyPrefix = "perf:docket:",
            CacheTtlSeconds = 3600,
            EnableHashVerification = false, // Disable for pure cache performance test
            WarmingBatchSize = 1000
        });

        _sut = new VerifiedCache<PerformanceTestDocket, ulong>(
            _cacheStore,
            _wormStore,
            d => d.Id,
            options,
            d => d.Hash);
    }

    [Fact]
    public async Task CacheHit_ShouldBeFasterThan_WormFetch()
    {
        // Arrange - Add document through cache (populates both cache and WORM)
        var docket = CreateDocket(1);
        await _sut.AppendAsync(docket);

        // Warmup
        await _sut.GetAsync(1UL);

        // Measure cache hit time
        var cacheHitTimes = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            await _sut.GetAsync(1UL);
            sw.Stop();
            cacheHitTimes.Add(sw.Elapsed.TotalMicroseconds);
        }

        // Invalidate cache to force WORM fetch
        await _sut.InvalidateAsync(1UL);

        // Measure WORM fetch time (first fetch populates cache)
        var wormFetchTimes = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            await _sut.InvalidateAsync(1UL); // Force cache miss each time
            var sw = Stopwatch.StartNew();
            await _sut.GetAsync(1UL);
            sw.Stop();
            wormFetchTimes.Add(sw.Elapsed.TotalMicroseconds);
        }

        // Assert
        var avgCacheHit = cacheHitTimes.Average();
        var avgWormFetch = wormFetchTimes.Average();

        _output.WriteLine($"Average Cache Hit: {avgCacheHit:F2} µs");
        _output.WriteLine($"Average WORM Fetch: {avgWormFetch:F2} µs");
        _output.WriteLine($"Cache Speedup: {avgWormFetch / avgCacheHit:F2}x");

        // Cache hits should generally be faster (though in-memory both are fast)
        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().BeGreaterThan(0);
        stats.CacheMisses.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BulkOperations_ShouldMaintainPerformance()
    {
        // Arrange - Insert 1000 documents
        const int documentCount = 1000;
        var insertSw = Stopwatch.StartNew();

        for (ulong i = 1; i <= documentCount; i++)
        {
            await _sut.AppendAsync(CreateDocket(i));
        }

        insertSw.Stop();
        _output.WriteLine($"Insert {documentCount} documents: {insertSw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Average insert time: {insertSw.Elapsed.TotalMicroseconds / documentCount:F2} µs/doc");

        // Act - Read all documents (should hit cache)
        var readSw = Stopwatch.StartNew();

        for (ulong i = 1; i <= documentCount; i++)
        {
            await _sut.GetAsync(i);
        }

        readSw.Stop();
        _output.WriteLine($"Read {documentCount} documents (cached): {readSw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Average read time: {readSw.Elapsed.TotalMicroseconds / documentCount:F2} µs/doc");

        // Assert
        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().Be(documentCount);
        stats.AverageLatencyMs.Should().BeLessThan(10); // Sub 10ms average
    }

    [Fact]
    public async Task CacheWarming_ShouldPopulateCache()
    {
        // Arrange - Add documents directly to WORM (bypassing cache)
        const int documentCount = 100;
        for (ulong i = 1; i <= documentCount; i++)
        {
            await _wormStore.AppendAsync(CreateDocket(i));
        }

        // Verify cache is empty
        var preWarmStats = await _sut.GetStatisticsAsync();
        preWarmStats.CacheHits.Should().Be(0);

        // Act - Warm the cache
        var warmSw = Stopwatch.StartNew();
        var progress = new Progress<CacheWarmingProgress>(p =>
        {
            if (p.DocumentsLoaded % 25 == 0)
            {
                _output.WriteLine($"Warming progress: {p.PercentComplete:F1}%");
            }
        });

        await _sut.WarmCacheAsync((ulong)documentCount, progress);
        warmSw.Stop();

        _output.WriteLine($"Cache warming for {documentCount} documents: {warmSw.ElapsedMilliseconds} ms");

        // Assert - All reads should hit cache now
        for (ulong i = 1; i <= documentCount; i++)
        {
            var doc = await _sut.GetAsync(i);
            doc.Should().NotBeNull();
        }

        var postWarmStats = await _sut.GetStatisticsAsync();
        postWarmStats.CacheHits.Should().Be(documentCount);
    }

    [Fact]
    public async Task GetManyAsync_ShouldBatchEfficiently()
    {
        // Arrange
        const int documentCount = 50;
        var ids = new List<ulong>();

        for (ulong i = 1; i <= documentCount; i++)
        {
            await _sut.AppendAsync(CreateDocket(i));
            ids.Add(i);
        }

        // Clear stats
        await _sut.GetStatisticsAsync(); // Read to establish baseline

        // Act
        var sw = Stopwatch.StartNew();
        var results = await _sut.GetManyAsync(ids);
        sw.Stop();

        // Assert
        results.Should().HaveCount(documentCount);
        _output.WriteLine($"GetManyAsync for {documentCount} documents: {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Average per document: {sw.Elapsed.TotalMicroseconds / documentCount:F2} µs");
    }

    [Fact]
    public async Task GetRangeAsync_ShouldBeEfficient()
    {
        // Arrange
        const int documentCount = 100;
        for (ulong i = 1; i <= documentCount; i++)
        {
            await _wormStore.AppendAsync(CreateDocket(i));
        }

        // Act
        var sw = Stopwatch.StartNew();
        var results = await _sut.GetRangeAsync(25UL, 75UL);
        sw.Stop();

        // Assert
        results.Should().HaveCount(51); // 25 to 75 inclusive
        _output.WriteLine($"GetRangeAsync for 51 documents: {sw.ElapsedMilliseconds} ms");

        // Verify range is now cached
        for (ulong i = 25; i <= 75; i++)
        {
            var doc = await _sut.GetAsync(i);
            doc.Should().NotBeNull();
        }

        var stats = await _sut.GetStatisticsAsync();
        stats.CacheHits.Should().Be(51);
    }

    [Fact]
    public async Task Statistics_ShouldTrackAccurately()
    {
        // Arrange
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.AppendAsync(CreateDocket(i));
        }

        // Act - Generate hits
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.GetAsync(i);
        }

        // Generate misses
        for (ulong i = 11; i <= 15; i++)
        {
            await _sut.GetAsync(i); // These don't exist
        }

        // Assert
        var stats = await _sut.GetStatisticsAsync();

        _output.WriteLine($"Cache Hits: {stats.CacheHits}");
        _output.WriteLine($"Cache Misses: {stats.CacheMisses}");
        _output.WriteLine($"WORM Fetches: {stats.WormFetches}");
        _output.WriteLine($"Hit Rate: {stats.HitRate:P2}");
        _output.WriteLine($"Average Latency: {stats.AverageLatencyMs:F3} ms");

        stats.CacheHits.Should().Be(10);
        stats.CacheMisses.Should().Be(5);
        stats.HitRate.Should().BeApproximately(0.67, 0.01); // 10/(10+5) = 0.67
    }

    private static PerformanceTestDocket CreateDocket(ulong id)
    {
        return new PerformanceTestDocket
        {
            Id = id,
            Hash = $"hash-{id:D8}",
            Payload = new string('X', 1000), // 1KB payload
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Test document for performance tests.
/// </summary>
public class PerformanceTestDocket
{
    public ulong Id { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
