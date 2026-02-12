// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Tests.Replication;

public class RegisterCacheTests
{
    private readonly RegisterCache _cache;

    public RegisterCacheTests()
    {
        _cache = new RegisterCache(new Mock<ILogger<RegisterCache>>().Object);
    }

    [Fact]
    public void GetOrCreate_NewRegister_ReturnsNewEntry()
    {
        var entry = _cache.GetOrCreate("reg-1");

        entry.Should().NotBeNull();
        entry.RegisterId.Should().Be("reg-1");
    }

    [Fact]
    public void GetOrCreate_SameRegister_ReturnsSameEntry()
    {
        var entry1 = _cache.GetOrCreate("reg-1");
        var entry2 = _cache.GetOrCreate("reg-1");

        entry1.Should().BeSameAs(entry2);
    }

    [Fact]
    public void Get_NonExistentRegister_ReturnsNull()
    {
        _cache.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Get_ExistingRegister_ReturnsEntry()
    {
        _cache.GetOrCreate("reg-1");

        _cache.Get("reg-1").Should().NotBeNull();
    }

    [Fact]
    public void Remove_ExistingRegister_ReturnsTrue()
    {
        _cache.GetOrCreate("reg-1");

        _cache.Remove("reg-1").Should().BeTrue();
        _cache.Get("reg-1").Should().BeNull();
    }

    [Fact]
    public void Remove_NonExistentRegister_ReturnsFalse()
    {
        _cache.Remove("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void GetCachedRegisterIds_ReturnsAllCachedIds()
    {
        _cache.GetOrCreate("reg-1");
        _cache.GetOrCreate("reg-2");
        _cache.GetOrCreate("reg-3");

        _cache.GetCachedRegisterIds().Should().BeEquivalentTo(["reg-1", "reg-2", "reg-3"]);
    }

    [Fact]
    public void GetAllStatistics_ReturnsStatsForAllRegisters()
    {
        _cache.GetOrCreate("reg-1");
        _cache.GetOrCreate("reg-2");

        var stats = _cache.GetAllStatistics();

        stats.Should().HaveCount(2);
        stats.Should().ContainKey("reg-1");
        stats.Should().ContainKey("reg-2");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new RegisterCache(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

public class RegisterCacheEntryTests
{
    private readonly RegisterCacheEntry _entry;

    public RegisterCacheEntryTests()
    {
        _entry = new RegisterCacheEntry("reg-1");
    }

    [Fact]
    public void AddOrUpdateTransaction_StoresTransaction()
    {
        var tx = CreateTransaction("tx-1", 1);

        _entry.AddOrUpdateTransaction(tx);

        _entry.GetTransaction("tx-1").Should().BeSameAs(tx);
    }

    [Fact]
    public void AddOrUpdateTransaction_UpdatesExisting()
    {
        var tx1 = CreateTransaction("tx-1", 1);
        var tx2 = CreateTransaction("tx-1", 2);

        _entry.AddOrUpdateTransaction(tx1);
        _entry.AddOrUpdateTransaction(tx2);

        _entry.GetTransaction("tx-1").Should().BeSameAs(tx2);
    }

    [Fact]
    public void AddOrUpdateTransaction_TracksLatestVersion()
    {
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-1", 5));
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-2", 3));
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-3", 10));

        _entry.GetLatestTransactionVersion().Should().Be(10);
    }

    [Fact]
    public void AddOrUpdateDocket_StoresDocket()
    {
        var docket = CreateDocket(1);

        _entry.AddOrUpdateDocket(docket);

        _entry.GetDocket(1).Should().BeSameAs(docket);
    }

    [Fact]
    public void AddOrUpdateDocket_TracksLatestVersion()
    {
        _entry.AddOrUpdateDocket(CreateDocket(3));
        _entry.AddOrUpdateDocket(CreateDocket(1));
        _entry.AddOrUpdateDocket(CreateDocket(7));

        _entry.GetLatestDocketVersion().Should().Be(7);
    }

    [Fact]
    public void GetTransaction_NonExistent_ReturnsNull()
    {
        _entry.GetTransaction("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetDocket_NonExistent_ReturnsNull()
    {
        _entry.GetDocket(999).Should().BeNull();
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectCounts()
    {
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-1", 1));
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-2", 2));
        _entry.AddOrUpdateDocket(CreateDocket(1));

        var stats = _entry.GetStatistics();

        stats.RegisterId.Should().Be("reg-1");
        stats.TransactionCount.Should().Be(2);
        stats.DocketCount.Should().Be(1);
        stats.LatestTransactionVersion.Should().Be(2);
        stats.LatestDocketVersion.Should().Be(1);
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        _entry.AddOrUpdateTransaction(CreateTransaction("tx-1", 1));
        _entry.AddOrUpdateDocket(CreateDocket(1));

        _entry.Clear();

        _entry.GetTransaction("tx-1").Should().BeNull();
        _entry.GetDocket(1).Should().BeNull();
        _entry.GetLatestTransactionVersion().Should().Be(0);
        _entry.GetLatestDocketVersion().Should().Be(0);
    }

    [Fact]
    public void InitialState_HasZeroVersions()
    {
        _entry.GetLatestTransactionVersion().Should().Be(0);
        _entry.GetLatestDocketVersion().Should().Be(0);
    }

    private static CachedTransaction CreateTransaction(string id, long version) => new()
    {
        TransactionId = id,
        RegisterId = "reg-1",
        Version = version,
        Data = [0x01, 0x02]
    };

    private static CachedDocket CreateDocket(long version) => new()
    {
        RegisterId = "reg-1",
        Version = version,
        Data = [0x01, 0x02],
        DocketHash = $"hash-{version}",
        TransactionIds = [$"tx-{version}"]
    };
}

public class RegisterCacheEvictionTests
{
    [Fact]
    public void AddOrUpdateTransaction_ExceedsLimit_EvictsOldest()
    {
        var entry = new RegisterCacheEntry("reg-1", maxTransactions: 5, maxDockets: 100);

        // Add 7 transactions (versions 1-7)
        for (var i = 1; i <= 7; i++)
        {
            entry.AddOrUpdateTransaction(new CachedTransaction
            {
                TransactionId = $"tx-{i}",
                RegisterId = "reg-1",
                Version = i,
                Data = [0x01]
            });
        }

        var stats = entry.GetStatistics();
        stats.TransactionCount.Should().BeLessThanOrEqualTo(5);

        // Oldest versions should be evicted, newest retained
        entry.GetTransaction("tx-7").Should().NotBeNull();
        entry.GetTransaction("tx-6").Should().NotBeNull();
    }

    [Fact]
    public void AddOrUpdateDocket_ExceedsLimit_EvictsOldest()
    {
        var entry = new RegisterCacheEntry("reg-1", maxTransactions: 100, maxDockets: 3);

        // Add 5 dockets (versions 1-5)
        for (var i = 1; i <= 5; i++)
        {
            entry.AddOrUpdateDocket(new CachedDocket
            {
                RegisterId = "reg-1",
                Version = i,
                Data = [0x01],
                DocketHash = $"hash-{i}",
                TransactionIds = [$"tx-{i}"]
            });
        }

        var stats = entry.GetStatistics();
        stats.DocketCount.Should().BeLessThanOrEqualTo(3);

        // Newest dockets should be retained
        entry.GetDocket(5).Should().NotBeNull();
        entry.GetDocket(4).Should().NotBeNull();

        // Oldest should be evicted
        entry.GetDocket(1).Should().BeNull();
    }

    [Fact]
    public void AddOrUpdateTransaction_UnderLimit_NoEviction()
    {
        var entry = new RegisterCacheEntry("reg-1", maxTransactions: 10, maxDockets: 10);

        for (var i = 1; i <= 5; i++)
        {
            entry.AddOrUpdateTransaction(new CachedTransaction
            {
                TransactionId = $"tx-{i}",
                RegisterId = "reg-1",
                Version = i,
                Data = [0x01]
            });
        }

        entry.GetStatistics().TransactionCount.Should().Be(5);

        // All should still be present
        for (var i = 1; i <= 5; i++)
        {
            entry.GetTransaction($"tx-{i}").Should().NotBeNull();
        }
    }

    [Fact]
    public void AddOrUpdateDocket_UnderLimit_NoEviction()
    {
        var entry = new RegisterCacheEntry("reg-1", maxTransactions: 10, maxDockets: 10);

        for (var i = 1; i <= 5; i++)
        {
            entry.AddOrUpdateDocket(new CachedDocket
            {
                RegisterId = "reg-1",
                Version = i,
                Data = [0x01],
                DocketHash = $"hash-{i}",
                TransactionIds = [$"tx-{i}"]
            });
        }

        entry.GetStatistics().DocketCount.Should().Be(5);
    }
}
