// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Replication;
using StackExchange.Redis;

namespace Sorcha.Peer.Service.Tests.Replication;

public class RedisAdvertisementStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly Mock<IServer> _mockServer;
    private readonly RedisAdvertisementStore _store;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisAdvertisementStoreTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockServer = new Mock<IServer>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDb.Object);
        _mockRedis.Setup(r => r.GetServers())
            .Returns([_mockServer.Object]);

        _store = new RedisAdvertisementStore(
            _mockRedis.Object,
            new Mock<ILogger<RedisAdvertisementStore>>().Object);
    }

    [Fact]
    public void Constructor_NullRedis_ThrowsArgumentNullException()
    {
        Action act = () => new RedisAdvertisementStore(null!, new Mock<ILogger<RedisAdvertisementStore>>().Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new RedisAdvertisementStore(_mockRedis.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #region SetLocalAsync

    [Fact]
    public async Task SetLocalAsync_SerializesAndSetsWithTtl()
    {
        var ad = new LocalRegisterAdvertisement
        {
            RegisterId = "reg-1",
            SyncState = RegisterSyncState.Active,
            LatestVersion = 42,
            LatestDocketVersion = 7,
            IsPublic = true
        };

        await _store.SetLocalAsync(ad);

        _mockDb.Verify(db => db.StringSetAsync(
            (RedisKey)"peer:advert:local:reg-1",
            It.Is<RedisValue>(v => v.ToString().Contains("reg-1")),
            TimeSpan.FromMinutes(5),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetLocalAsync_RedisUnavailable_LogsWarningAndDoesNotThrow()
    {
        _mockDb.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        var ad = new LocalRegisterAdvertisement
        {
            RegisterId = "reg-1",
            SyncState = RegisterSyncState.Active
        };

        var act = () => _store.SetLocalAsync(ad);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SetRemoteAsync

    [Fact]
    public async Task SetRemoteAsync_UsesCompositeKey()
    {
        var info = new PeerRegisterInfo
        {
            RegisterId = "reg-1",
            SyncState = RegisterSyncState.FullyReplicated,
            LatestVersion = 100,
            IsPublic = true
        };

        await _store.SetRemoteAsync("peer-42", info);

        _mockDb.Verify(db => db.StringSetAsync(
            (RedisKey)"peer:advert:remote:peer-42:reg-1",
            It.IsAny<RedisValue>(),
            TimeSpan.FromMinutes(5),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region RemoveLocalAsync

    [Fact]
    public async Task RemoveLocalAsync_DeletesKey()
    {
        await _store.RemoveLocalAsync("reg-1");

        _mockDb.Verify(db => db.KeyDeleteAsync(
            (RedisKey)"peer:advert:local:reg-1",
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLocalAsync_RedisUnavailable_DoesNotThrow()
    {
        _mockDb.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var act = () => _store.RemoveLocalAsync("reg-1");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetAllLocalAsync

    [Fact]
    public async Task GetAllLocalAsync_DeserializesAllEntries()
    {
        var ad1 = new LocalRegisterAdvertisement { RegisterId = "reg-1", SyncState = RegisterSyncState.Active, IsPublic = true };
        var ad2 = new LocalRegisterAdvertisement { RegisterId = "reg-2", SyncState = RegisterSyncState.FullyReplicated, IsPublic = true };

        var keys = new RedisKey[] { "peer:advert:local:reg-1", "peer:advert:local:reg-2" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        _mockDb.Setup(db => db.StringGetAsync((RedisKey)"peer:advert:local:reg-1", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(ad1, JsonOptions));
        _mockDb.Setup(db => db.StringGetAsync((RedisKey)"peer:advert:local:reg-2", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(ad2, JsonOptions));

        var results = await _store.GetAllLocalAsync();

        results.Should().HaveCount(2);
        results.Select(r => r.RegisterId).Should().BeEquivalentTo(["reg-1", "reg-2"]);
    }

    [Fact]
    public async Task GetAllLocalAsync_RedisUnavailable_ReturnsEmpty()
    {
        _mockRedis.Setup(r => r.GetServers()).Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var results = await _store.GetAllLocalAsync();
        results.Should().BeEmpty();
    }

    #endregion

    #region GetAllRemoteAsync

    [Fact]
    public async Task GetAllRemoteAsync_GroupsByPeerId()
    {
        var info1 = new PeerRegisterInfo { RegisterId = "reg-1", SyncState = RegisterSyncState.Active, IsPublic = true };
        var info2 = new PeerRegisterInfo { RegisterId = "reg-2", SyncState = RegisterSyncState.FullyReplicated, IsPublic = true };

        var keys = new RedisKey[] { "peer:advert:remote:peer-A:reg-1", "peer:advert:remote:peer-A:reg-2" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        _mockDb.Setup(db => db.StringGetAsync((RedisKey)"peer:advert:remote:peer-A:reg-1", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(info1, JsonOptions));
        _mockDb.Setup(db => db.StringGetAsync((RedisKey)"peer:advert:remote:peer-A:reg-2", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(info2, JsonOptions));

        var results = await _store.GetAllRemoteAsync();

        results.Should().ContainKey("peer-A");
        results["peer-A"].Should().HaveCount(2);
    }

    #endregion

    #region RemoveLocalExceptAsync

    [Fact]
    public async Task RemoveLocalExceptAsync_RemovesKeysNotInSet()
    {
        var keys = new RedisKey[] { "peer:advert:local:reg-1", "peer:advert:local:reg-2", "peer:advert:local:reg-3" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var keepSet = new HashSet<string> { "reg-1" };
        var removed = await _store.RemoveLocalExceptAsync(keepSet);

        removed.Should().Be(2);
        _mockDb.Verify(db => db.KeyDeleteAsync((RedisKey)"peer:advert:local:reg-2", It.IsAny<CommandFlags>()), Times.Once);
        _mockDb.Verify(db => db.KeyDeleteAsync((RedisKey)"peer:advert:local:reg-3", It.IsAny<CommandFlags>()), Times.Once);
        _mockDb.Verify(db => db.KeyDeleteAsync((RedisKey)"peer:advert:local:reg-1", It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLocalExceptAsync_AllInKeepSet_RemovesNothing()
    {
        var keys = new RedisKey[] { "peer:advert:local:reg-1" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var removed = await _store.RemoveLocalExceptAsync(new HashSet<string> { "reg-1" });

        removed.Should().Be(0);
    }

    #endregion

    #region RemoveRemoteByPeerAsync

    [Fact]
    public async Task RemoveRemoteByPeerAsync_DeletesAllKeysForPeer()
    {
        var keys = new RedisKey[] { "peer:advert:remote:peer-A:reg-1", "peer:advert:remote:peer-A:reg-2" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        await _store.RemoveRemoteByPeerAsync("peer-A");

        _mockDb.Verify(db => db.KeyDeleteAsync(
            It.Is<RedisKey[]>(k => k.Length == 2),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion
}
