// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Distribution;

namespace Sorcha.Peer.Service.Tests;

public class GossipProtocolEngineTests : IDisposable
{
    private readonly Mock<ILogger<GossipProtocolEngine>> _loggerMock = new();
    private readonly List<PeerListManager> _managers = new();

    private GossipProtocolEngine CreateEngine(
        int fanoutFactor = 3,
        int gossipRounds = 3)
    {
        var config = new PeerServiceConfiguration
        {
            TransactionDistribution = new TransactionDistributionConfiguration
            {
                FanoutFactor = fanoutFactor,
                GossipRounds = gossipRounds
            }
        };

        var configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        configMock.Setup(x => x.Value).Returns(config);

        var peerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object,
            configMock.Object);
        _managers.Add(peerListManager);

        return new GossipProtocolEngine(
            _loggerMock.Object,
            configMock.Object,
            peerListManager);
    }

    public void Dispose()
    {
        foreach (var manager in _managers)
            manager.Dispose();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        configMock.Setup(x => x.Value).Returns(new PeerServiceConfiguration());
        var peerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object, configMock.Object);
        _managers.Add(peerListManager);

        // Act
        var act = () => new GossipProtocolEngine(null!, configMock.Object, peerListManager);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        configMock.Setup(x => x.Value).Returns(new PeerServiceConfiguration());
        var peerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object, configMock.Object);
        _managers.Add(peerListManager);

        // Act
        var act = () => new GossipProtocolEngine(_loggerMock.Object, null!, peerListManager);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullPeerListManager_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        configMock.Setup(x => x.Value).Returns(new PeerServiceConfiguration());

        // Act
        var act = () => new GossipProtocolEngine(_loggerMock.Object, configMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("peerListManager");
    }

    #region ShouldGossip

    [Fact]
    public void ShouldGossip_UnseenTransaction_WithValidTTL_ReturnsTrue()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-new",
            TTL = 3600,
            GossipRound = 0
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldGossip_AlreadySeenTransaction_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-seen");
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-seen",
            TTL = 3600,
            GossipRound = 0
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldGossip_ZeroTTL_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-expired",
            TTL = 0,
            GossipRound = 0
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldGossip_NegativeTTL_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-neg-ttl",
            TTL = -1,
            GossipRound = 0
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldGossip_AtMaxGossipRounds_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine(gossipRounds: 3);
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-max-rounds",
            TTL = 3600,
            GossipRound = 3 // At max
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldGossip_AboveMaxGossipRounds_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine(gossipRounds: 3);
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-over-rounds",
            TTL = 3600,
            GossipRound = 5
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldGossip_BelowMaxGossipRounds_WithValidTTL_ReturnsTrue()
    {
        // Arrange
        var engine = CreateEngine(gossipRounds: 3);
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-ok-rounds",
            TTL = 3600,
            GossipRound = 2
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region RecordSeen

    [Fact]
    public void RecordSeen_MarksTransactionAsSeen()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        engine.RecordSeen("tx-123");

        // Assert
        var state = engine.GetGossipState("tx-123");
        state.Should().NotBeNull();
        state!.TransactionId.Should().Be("tx-123");
    }

    [Fact]
    public void RecordSeen_SubsequentShouldGossip_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-456");

        var transaction = new TransactionNotification
        {
            TransactionId = "tx-456",
            TTL = 3600,
            GossipRound = 0
        };

        // Act
        var result = engine.ShouldGossip(transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RecordSeen_SetsStartedAt()
    {
        // Arrange
        var engine = CreateEngine();
        var before = DateTimeOffset.UtcNow;

        // Act
        engine.RecordSeen("tx-789");

        // Assert
        var state = engine.GetGossipState("tx-789");
        state!.StartedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordSeen_SetsCurrentRoundToZero()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        engine.RecordSeen("tx-round");

        // Assert
        var state = engine.GetGossipState("tx-round");
        state!.CurrentRound.Should().Be(0);
    }

    #endregion

    #region PrepareForNextRound

    [Fact]
    public void PrepareForNextRound_IncrementsGossipRound()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification { TransactionId = "tx-1", GossipRound = 1 };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.GossipRound.Should().Be(2);
    }

    [Fact]
    public void PrepareForNextRound_IncrementsHopCount()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification { TransactionId = "tx-1", HopCount = 3 };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.HopCount.Should().Be(4);
    }

    [Fact]
    public void PrepareForNextRound_DecrementsTTL()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification { TransactionId = "tx-1", TTL = 100 };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.TTL.Should().Be(99);
    }

    [Fact]
    public void PrepareForNextRound_PreservesTransactionId()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification { TransactionId = "tx-preserve" };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.TransactionId.Should().Be("tx-preserve");
    }

    [Fact]
    public void PrepareForNextRound_PreservesOriginPeerId()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-1",
            OriginPeerId = "peer-origin"
        };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.OriginPeerId.Should().Be("peer-origin");
    }

    [Fact]
    public void PrepareForNextRound_PreservesTransactionData()
    {
        // Arrange
        var engine = CreateEngine();
        var data = new byte[] { 1, 2, 3, 4 };
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-1",
            HasFullData = true,
            TransactionData = data
        };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.HasFullData.Should().BeTrue();
        next.TransactionData.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void PrepareForNextRound_PreservesTimestamp()
    {
        // Arrange
        var engine = CreateEngine();
        var timestamp = DateTimeOffset.UtcNow.AddHours(-1);
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-1",
            Timestamp = timestamp
        };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void PrepareForNextRound_PreservesDataSizeAndHash()
    {
        // Arrange
        var engine = CreateEngine();
        var transaction = new TransactionNotification
        {
            TransactionId = "tx-1",
            DataSize = 1024,
            DataHash = "abc123hash"
        };

        // Act
        var next = engine.PrepareForNextRound(transaction);

        // Assert
        next.DataSize.Should().Be(1024);
        next.DataHash.Should().Be("abc123hash");
    }

    #endregion

    #region GetGossipState

    [Fact]
    public void GetGossipState_UnknownTransaction_ReturnsNull()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var state = engine.GetGossipState("tx-unknown");

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetGossipState_AfterRecordSeen_ReturnsState()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-known");

        // Act
        var state = engine.GetGossipState("tx-known");

        // Assert
        state.Should().NotBeNull();
        state!.TransactionId.Should().Be("tx-known");
    }

    #endregion

    #region CleanupOldState

    [Fact]
    public void CleanupOldState_RemovesOldEntries()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-old");

        // Act - cleanup with zero maxAge removes everything
        var removed = engine.CleanupOldState(TimeSpan.Zero);

        // Assert
        removed.Should().Be(1);
        engine.GetGossipState("tx-old").Should().BeNull();
    }

    [Fact]
    public void CleanupOldState_PreservesRecentEntries()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-recent");

        // Act - cleanup with large maxAge preserves everything
        var removed = engine.CleanupOldState(TimeSpan.FromHours(1));

        // Assert
        removed.Should().Be(0);
        engine.GetGossipState("tx-recent").Should().NotBeNull();
    }

    [Fact]
    public void CleanupOldState_ReturnsCountOfRemovedEntries()
    {
        // Arrange
        var engine = CreateEngine();
        engine.RecordSeen("tx-1");
        engine.RecordSeen("tx-2");
        engine.RecordSeen("tx-3");

        // Act
        var removed = engine.CleanupOldState(TimeSpan.Zero);

        // Assert
        removed.Should().Be(3);
    }

    [Fact]
    public void CleanupOldState_EmptyState_ReturnsZero()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var removed = engine.CleanupOldState(TimeSpan.Zero);

        // Assert
        removed.Should().Be(0);
    }

    #endregion

    #region BloomFilter

    [Fact]
    public void AddToBloomFilter_ThenCheckBloomFilter_ReturnsTrue()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        engine.AddToBloomFilter("peer-1", "tx-hash-1");

        // Assert
        engine.CheckBloomFilter("peer-1", "tx-hash-1").Should().BeTrue();
    }

    [Fact]
    public void CheckBloomFilter_UnknownPeer_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var result = engine.CheckBloomFilter("peer-unknown", "tx-hash-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CheckBloomFilter_UnknownTransaction_ReturnsFalse()
    {
        // Arrange
        var engine = CreateEngine();
        engine.AddToBloomFilter("peer-1", "tx-hash-known");

        // Act
        var result = engine.CheckBloomFilter("peer-1", "tx-hash-unknown");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void BloomFilter_DifferentPeers_HaveSeparateFilters()
    {
        // Arrange
        var engine = CreateEngine();
        engine.AddToBloomFilter("peer-1", "tx-hash-1");

        // Act & Assert
        engine.CheckBloomFilter("peer-1", "tx-hash-1").Should().BeTrue();
        engine.CheckBloomFilter("peer-2", "tx-hash-1").Should().BeFalse();
    }

    [Fact]
    public void BloomFilter_MultipleTransactions_AllFound()
    {
        // Arrange
        var engine = CreateEngine();
        engine.AddToBloomFilter("peer-1", "tx-hash-1");
        engine.AddToBloomFilter("peer-1", "tx-hash-2");
        engine.AddToBloomFilter("peer-1", "tx-hash-3");

        // Assert
        engine.CheckBloomFilter("peer-1", "tx-hash-1").Should().BeTrue();
        engine.CheckBloomFilter("peer-1", "tx-hash-2").Should().BeTrue();
        engine.CheckBloomFilter("peer-1", "tx-hash-3").Should().BeTrue();
    }

    #endregion
}
