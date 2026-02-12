// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Data;
using Sorcha.Peer.Service.Discovery;

namespace Sorcha.Peer.Service.Tests.Data;

public class PeerDbContextTests : IDisposable
{
    private readonly DbContextOptions<PeerDbContext> _options;
    private readonly string _dbName;

    public PeerDbContextTests()
    {
        _dbName = $"PeerTestDb_{Guid.NewGuid()}";
        _options = new DbContextOptionsBuilder<PeerDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
    }

    [Fact]
    public async Task PeerNodeEntity_CanBePersistedAndRetrieved()
    {
        // Arrange
        var entity = new PeerNodeEntity
        {
            PeerId = "test-peer-1",
            Address = "192.168.1.100",
            Port = 5001,
            Protocols = "GrpcStream,TcpDirect",
            IsSeedNode = true,
            FailureCount = 2,
            AverageLatencyMs = 45
        };

        // Act - Save
        await using (var context = new PeerDbContext(_options))
        {
            context.Peers.Add(entity);
            await context.SaveChangesAsync();
        }

        // Assert - Retrieve
        await using (var context = new PeerDbContext(_options))
        {
            var retrieved = await context.Peers.FindAsync("test-peer-1");
            retrieved.Should().NotBeNull();
            retrieved!.Address.Should().Be("192.168.1.100");
            retrieved.Port.Should().Be(5001);
            retrieved.Protocols.Should().Be("GrpcStream,TcpDirect");
            retrieved.IsSeedNode.Should().BeTrue();
            retrieved.FailureCount.Should().Be(2);
            retrieved.AverageLatencyMs.Should().Be(45);
        }
    }

    [Fact]
    public async Task RegisterSubscriptionEntity_CanBePersistedAndRetrieved()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new RegisterSubscriptionEntity
        {
            Id = id,
            RegisterId = "reg-001",
            Mode = ReplicationMode.FullReplica,
            SyncState = RegisterSyncState.Syncing,
            LastSyncedDocketVersion = 42,
            LastSyncedTransactionVersion = 100,
            TotalDocketsInChain = 50,
            SourcePeerIds = "peer1,peer2",
            ConsecutiveFailures = 1,
            ErrorMessage = null
        };

        // Act
        await using (var context = new PeerDbContext(_options))
        {
            context.RegisterSubscriptions.Add(entity);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new PeerDbContext(_options))
        {
            var retrieved = await context.RegisterSubscriptions.FindAsync(id);
            retrieved.Should().NotBeNull();
            retrieved!.RegisterId.Should().Be("reg-001");
            retrieved.Mode.Should().Be(ReplicationMode.FullReplica);
            retrieved.SyncState.Should().Be(RegisterSyncState.Syncing);
            retrieved.LastSyncedDocketVersion.Should().Be(42);
            retrieved.TotalDocketsInChain.Should().Be(50);
            retrieved.SourcePeerIds.Should().Be("peer1,peer2");
        }
    }

    [Fact]
    public async Task SyncCheckpointEntity_CanBePersistedAndRetrieved()
    {
        // Arrange
        var entity = new SyncCheckpointEntity
        {
            PeerId = "peer-1",
            RegisterId = "reg-001",
            CurrentVersion = 99,
            TotalItems = 500,
            SourcePeerId = "peer-2",
            NextSyncDue = DateTime.UtcNow.AddMinutes(5)
        };

        // Act
        await using (var context = new PeerDbContext(_options))
        {
            context.SyncCheckpoints.Add(entity);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new PeerDbContext(_options))
        {
            var retrieved = await context.SyncCheckpoints.FindAsync("peer-1", "reg-001");
            retrieved.Should().NotBeNull();
            retrieved!.CurrentVersion.Should().Be(99);
            retrieved.TotalItems.Should().Be(500);
            retrieved.SourcePeerId.Should().Be("peer-2");
        }
    }

    [Fact]
    public void PeerNodeEntity_ToDomain_MapsAllFields()
    {
        // Arrange
        var entity = new PeerNodeEntity
        {
            PeerId = "peer-1",
            Address = "10.0.0.1",
            Port = 5001,
            Protocols = "GrpcStream,TcpDirect",
            FirstSeen = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            FailureCount = 3,
            IsSeedNode = true,
            AverageLatencyMs = 25
        };

        // Act
        var domain = entity.ToDomain();

        // Assert
        domain.PeerId.Should().Be("peer-1");
        domain.Address.Should().Be("10.0.0.1");
        domain.Port.Should().Be(5001);
        domain.SupportedProtocols.Should().BeEquivalentTo(["GrpcStream", "TcpDirect"]);
        domain.FirstSeen.Should().Be(entity.FirstSeen);
        domain.LastSeen.Should().Be(entity.LastSeen);
        domain.FailureCount.Should().Be(3);
        domain.IsSeedNode.Should().BeTrue();
        domain.AverageLatencyMs.Should().Be(25);
    }

    [Fact]
    public void PeerNodeEntity_ToDomain_HandlesEmptyProtocols()
    {
        // Arrange
        var entity = new PeerNodeEntity
        {
            PeerId = "peer-1",
            Address = "10.0.0.1",
            Port = 5001,
            Protocols = ""
        };

        // Act
        var domain = entity.ToDomain();

        // Assert
        domain.SupportedProtocols.Should().BeEmpty();
    }

    [Fact]
    public void PeerNodeEntity_FromDomain_MapsAllFields()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer-1",
            Address = "10.0.0.1",
            Port = 5001,
            SupportedProtocols = new List<string> { "GrpcStream", "TcpDirect" },
            FirstSeen = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            FailureCount = 3,
            IsSeedNode = true,
            AverageLatencyMs = 25
        };

        // Act
        var entity = PeerNodeEntity.FromDomain(peer);

        // Assert
        entity.PeerId.Should().Be("peer-1");
        entity.Address.Should().Be("10.0.0.1");
        entity.Port.Should().Be(5001);
        entity.Protocols.Should().Be("GrpcStream,TcpDirect");
        entity.FirstSeen.Should().Be(peer.FirstSeen);
        entity.LastSeen.Should().Be(peer.LastSeen);
        entity.FailureCount.Should().Be(3);
        entity.IsSeedNode.Should().BeTrue();
        entity.AverageLatencyMs.Should().Be(25);
    }

    [Fact]
    public void PeerNodeEntity_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new PeerNode
        {
            PeerId = "roundtrip-peer",
            Address = "172.16.0.50",
            Port = 9000,
            SupportedProtocols = new List<string> { "GrpcStream" },
            FailureCount = 1,
            IsSeedNode = false,
            AverageLatencyMs = 150
        };

        // Act
        var entity = PeerNodeEntity.FromDomain(original);
        var restored = entity.ToDomain();

        // Assert
        restored.PeerId.Should().Be(original.PeerId);
        restored.Address.Should().Be(original.Address);
        restored.Port.Should().Be(original.Port);
        restored.SupportedProtocols.Should().BeEquivalentTo(original.SupportedProtocols);
        restored.FailureCount.Should().Be(original.FailureCount);
        restored.IsSeedNode.Should().Be(original.IsSeedNode);
        restored.AverageLatencyMs.Should().Be(original.AverageLatencyMs);
    }

    [Fact]
    public void RegisterSubscriptionEntity_ToDomain_MapsAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new RegisterSubscriptionEntity
        {
            Id = id,
            RegisterId = "reg-001",
            Mode = ReplicationMode.FullReplica,
            SyncState = RegisterSyncState.Syncing,
            LastSyncedDocketVersion = 42,
            LastSyncedTransactionVersion = 100,
            TotalDocketsInChain = 50,
            SourcePeerIds = "peer1,peer2,peer3",
            CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastSyncAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ErrorMessage = "timeout",
            ConsecutiveFailures = 3
        };

        // Act
        var domain = entity.ToDomain();

        // Assert
        domain.Id.Should().Be(id);
        domain.RegisterId.Should().Be("reg-001");
        domain.Mode.Should().Be(ReplicationMode.FullReplica);
        domain.SyncState.Should().Be(RegisterSyncState.Syncing);
        domain.LastSyncedDocketVersion.Should().Be(42);
        domain.LastSyncedTransactionVersion.Should().Be(100);
        domain.TotalDocketsInChain.Should().Be(50);
        domain.SourcePeerIds.Should().BeEquivalentTo(["peer1", "peer2", "peer3"]);
        domain.ErrorMessage.Should().Be("timeout");
        domain.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void RegisterSubscriptionEntity_ToDomain_HandlesEmptySourcePeerIds()
    {
        // Arrange
        var entity = new RegisterSubscriptionEntity
        {
            RegisterId = "reg-001",
            SourcePeerIds = ""
        };

        // Act
        var domain = entity.ToDomain();

        // Assert
        domain.SourcePeerIds.Should().BeEmpty();
    }

    [Fact]
    public void RegisterSubscriptionEntity_FromDomain_MapsAllFields()
    {
        // Arrange
        var sub = new RegisterSubscription
        {
            Id = Guid.NewGuid(),
            RegisterId = "reg-001",
            Mode = ReplicationMode.ForwardOnly,
            SyncState = RegisterSyncState.Active,
            LastSyncedDocketVersion = 10,
            LastSyncedTransactionVersion = 55,
            TotalDocketsInChain = 20,
            SourcePeerIds = new List<string> { "p1", "p2" },
            ConsecutiveFailures = 0
        };

        // Act
        var entity = RegisterSubscriptionEntity.FromDomain(sub);

        // Assert
        entity.Id.Should().Be(sub.Id);
        entity.RegisterId.Should().Be("reg-001");
        entity.Mode.Should().Be(ReplicationMode.ForwardOnly);
        entity.SyncState.Should().Be(RegisterSyncState.Active);
        entity.SourcePeerIds.Should().Be("p1,p2");
    }

    [Fact]
    public void RegisterSubscriptionEntity_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new RegisterSubscription
        {
            Id = Guid.NewGuid(),
            RegisterId = "roundtrip-reg",
            Mode = ReplicationMode.FullReplica,
            SyncState = RegisterSyncState.FullyReplicated,
            LastSyncedDocketVersion = 100,
            LastSyncedTransactionVersion = 500,
            TotalDocketsInChain = 100,
            SourcePeerIds = new List<string> { "src-peer" },
            ConsecutiveFailures = 0
        };

        // Act
        var entity = RegisterSubscriptionEntity.FromDomain(original);
        var restored = entity.ToDomain();

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.RegisterId.Should().Be(original.RegisterId);
        restored.Mode.Should().Be(original.Mode);
        restored.SyncState.Should().Be(original.SyncState);
        restored.LastSyncedDocketVersion.Should().Be(original.LastSyncedDocketVersion);
        restored.LastSyncedTransactionVersion.Should().Be(original.LastSyncedTransactionVersion);
        restored.TotalDocketsInChain.Should().Be(original.TotalDocketsInChain);
        restored.SourcePeerIds.Should().BeEquivalentTo(original.SourcePeerIds);
    }

    public void Dispose()
    {
        // InMemory databases are automatically cleaned up
    }
}

/// <summary>
/// Tests for PeerListManager with EF Core InMemory database persistence
/// </summary>
public class PeerListManagerWithDbTests : IAsyncDisposable
{
    private readonly PeerListManager _manager;
    private readonly IDbContextFactory<PeerDbContext> _dbContextFactory;
    private readonly string _dbName;

    public PeerListManagerWithDbTests()
    {
        _dbName = $"PeerListManagerTestDb_{Guid.NewGuid()}";

        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        serviceCollection.AddDbContextFactory<PeerDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));
        serviceCollection.AddLogging();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PeerDbContext>>();

        var logger = serviceProvider.GetRequiredService<ILogger<PeerListManager>>();
        var config = Options.Create(new PeerServiceConfiguration
        {
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MaxPeersInList = 100,
                MinHealthyPeers = 5,
                RefreshIntervalMinutes = 15
            }
        });

        _manager = new PeerListManager(logger, config, _dbContextFactory);
    }

    [Fact]
    public async Task AddOrUpdatePeerAsync_PersistsToDatabase()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "persist-test",
            Address = "10.0.0.1",
            Port = 5001,
            SupportedProtocols = new List<string> { "GrpcStream" }
        };

        // Act
        await _manager.AddOrUpdatePeerAsync(peer);

        // Assert - Check database directly
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entity = await context.Peers.FindAsync("persist-test");
        entity.Should().NotBeNull();
        entity!.Address.Should().Be("10.0.0.1");
        entity.Port.Should().Be(5001);
        entity.Protocols.Should().Be("GrpcStream");
    }

    [Fact]
    public async Task RemovePeerAsync_DeletesFromDatabase()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "remove-test",
            Address = "10.0.0.1",
            Port = 5001
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        await _manager.RemovePeerAsync("remove-test");

        // Assert
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entity = await context.Peers.FindAsync("remove-test");
        entity.Should().BeNull();
    }

    [Fact]
    public async Task LoadPeersFromDatabaseAsync_RestoresPeersToMemory()
    {
        // Arrange - Insert directly into database
        await using (var context = await _dbContextFactory.CreateDbContextAsync())
        {
            context.Peers.Add(new PeerNodeEntity
            {
                PeerId = "db-peer-1",
                Address = "10.0.0.1",
                Port = 5001,
                Protocols = "GrpcStream",
                IsSeedNode = true
            });
            context.Peers.Add(new PeerNodeEntity
            {
                PeerId = "db-peer-2",
                Address = "10.0.0.2",
                Port = 5002,
                Protocols = "TcpDirect"
            });
            await context.SaveChangesAsync();
        }

        // Act - Create fresh manager and load from DB
        var logger = new Mock<ILogger<PeerListManager>>();
        var config = Options.Create(new PeerServiceConfiguration
        {
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MaxPeersInList = 100,
                MinHealthyPeers = 5,
                RefreshIntervalMinutes = 15
            }
        });
        var freshManager = new PeerListManager(logger.Object, config, _dbContextFactory);
        await freshManager.LoadPeersFromDatabaseAsync();

        // Assert
        var peers = freshManager.GetAllPeers();
        peers.Should().HaveCount(2);
        peers.Select(p => p.PeerId).Should().Contain(["db-peer-1", "db-peer-2"]);

        var seedPeer = freshManager.GetPeer("db-peer-1");
        seedPeer.Should().NotBeNull();
        seedPeer!.IsSeedNode.Should().BeTrue();
        seedPeer.SupportedProtocols.Should().Contain("GrpcStream");

        freshManager.Dispose();
    }

    [Fact]
    public async Task UpdateLastSeenAsync_PersistsUpdatedTimestamp()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "update-test",
            Address = "10.0.0.1",
            Port = 5001,
            LastSeen = DateTimeOffset.UtcNow.AddHours(-1),
            FailureCount = 3
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        await _manager.UpdateLastSeenAsync("update-test");

        // Assert
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entity = await context.Peers.FindAsync("update-test");
        entity.Should().NotBeNull();
        entity!.FailureCount.Should().Be(0);
        entity.LastSeen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    public async ValueTask DisposeAsync()
    {
        _manager?.Dispose();
    }
}
