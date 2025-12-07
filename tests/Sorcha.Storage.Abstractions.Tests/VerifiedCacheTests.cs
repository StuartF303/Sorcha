// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Storage.Abstractions;
using Sorcha.Storage.Abstractions.Caching;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

public class VerifiedCacheTests
{
    private readonly Mock<ICacheStore> _mockCacheStore;
    private readonly Mock<IWormStore<TestDocket, ulong>> _mockWormStore;
    private readonly IOptions<VerifiedCacheConfiguration> _options;
    private readonly VerifiedCache<TestDocket, ulong> _sut;

    public VerifiedCacheTests()
    {
        _mockCacheStore = new Mock<ICacheStore>();
        _mockWormStore = new Mock<IWormStore<TestDocket, ulong>>();
        _options = Options.Create(new VerifiedCacheConfiguration
        {
            KeyPrefix = "test:",
            CacheTtlSeconds = 3600,
            EnableHashVerification = false
        });

        _sut = new VerifiedCache<TestDocket, ulong>(
            _mockCacheStore.Object,
            _mockWormStore.Object,
            d => d.Id,
            _options);
    }

    [Fact]
    public async Task GetAsync_WhenCacheHit_ReturnsCachedValue()
    {
        // Arrange
        var docket = new TestDocket { Id = 1, Hash = "abc123" };
        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(docket);

        // Act
        var result = await _sut.GetAsync(1UL);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Hash.Should().Be("abc123");
        _mockWormStore.Verify(w => w.GetAsync(It.IsAny<ulong>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenCacheMiss_FetchesFromWormAndPopulatesCache()
    {
        // Arrange
        var docket = new TestDocket { Id = 1, Hash = "abc123" };
        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);
        _mockWormStore
            .Setup(w => w.GetAsync(1UL, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docket);

        // Act
        var result = await _sut.GetAsync(1UL);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        _mockCacheStore.Verify(
            c => c.SetAsync("test:1", docket, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);
        _mockWormStore
            .Setup(w => w.GetAsync(999UL, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);

        // Act
        var result = await _sut.GetAsync(999UL);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AppendAsync_AppendsToWormAndPopulatesCache()
    {
        // Arrange
        var docket = new TestDocket { Id = 5, Hash = "newHash" };
        _mockWormStore
            .Setup(w => w.AppendAsync(docket, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docket);

        // Act
        var result = await _sut.AppendAsync(docket);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(5);
        _mockWormStore.Verify(w => w.AppendAsync(docket, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheStore.Verify(
            c => c.SetAsync("test:5", docket, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentSequenceAsync_DelegatesToWormStore()
    {
        // Arrange
        _mockWormStore
            .Setup(w => w.GetCurrentSequenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100UL);

        // Act
        var result = await _sut.GetCurrentSequenceAsync();

        // Assert
        result.Should().Be(100UL);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesFromCache()
    {
        // Arrange
        _mockCacheStore
            .Setup(c => c.RemoveAsync("test:10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.InvalidateAsync(10UL);

        // Assert
        _mockCacheStore.Verify(c => c.RemoveAsync("test:10", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateByPatternAsync_RemovesByPattern()
    {
        // Arrange
        _mockCacheStore
            .Setup(c => c.RemoveByPatternAsync("test:user:*", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5L);

        // Act
        var result = await _sut.InvalidateByPatternAsync("user:*");

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetRangeAsync_FetchesFromWormAndPopulatesCache()
    {
        // Arrange
        var dockets = new[]
        {
            new TestDocket { Id = 1, Hash = "hash1" },
            new TestDocket { Id = 2, Hash = "hash2" },
            new TestDocket { Id = 3, Hash = "hash3" }
        };
        _mockWormStore
            .Setup(w => w.GetRangeAsync(1UL, 3UL, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dockets);

        // Act
        var result = await _sut.GetRangeAsync(1UL, 3UL);

        // Assert
        result.Should().HaveCount(3);
        _mockCacheStore.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<TestDocket>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Act
        var stats = await _sut.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.CacheHits.Should().Be(0);
        stats.CacheMisses.Should().Be(0);
    }

    [Fact]
    public async Task VerifyIntegrityAsync_DelegatesToWormStore()
    {
        // Arrange
        var wormResult = new IntegrityCheckResult(
            IsValid: true,
            DocumentsChecked: 100,
            CorruptedDocuments: 0,
            Violations: Array.Empty<IntegrityViolation>());
        _mockWormStore
            .Setup(w => w.VerifyIntegrityAsync(It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wormResult);

        // Act
        var result = await _sut.VerifyIntegrityAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.DocumentsVerified.Should().Be(100);
    }

    [Fact]
    public async Task GetManyAsync_ReturnsCachedAndFetchesMissing()
    {
        // Arrange
        var cached1 = new TestDocket { Id = 1, Hash = "hash1" };
        var fromWorm = new TestDocket { Id = 3, Hash = "hash3" };

        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached1);
        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);
        _mockCacheStore
            .Setup(c => c.GetAsync<TestDocket>("test:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);

        _mockWormStore
            .Setup(w => w.GetAsync(2UL, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDocket?)null);
        _mockWormStore
            .Setup(w => w.GetAsync(3UL, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromWorm);

        // Act
        var result = await _sut.GetManyAsync(new[] { 1UL, 2UL, 3UL });

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Id == 1);
        result.Should().Contain(d => d.Id == 3);
    }

}

/// <summary>
/// Test document for VerifiedCache tests.
/// Must be public for Moq to create proxies.
/// </summary>
public class TestDocket
{
    public ulong Id { get; set; }
    public string Hash { get; set; } = string.Empty;
}
