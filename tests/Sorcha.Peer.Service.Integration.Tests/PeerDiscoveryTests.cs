// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Integration.Tests.Infrastructure;
using Sorcha.Peer.Service.Core;
using System.Net;
using System.Net.Http.Json;

namespace Sorcha.Peer.Service.Integration.Tests;

/// <summary>
/// Integration tests for peer discovery functionality
/// Tests REST and gRPC endpoints for peer registration and discovery
/// </summary>
[Collection("PeerIntegration")]
public class PeerDiscoveryTests : IClassFixture<PeerTestFixture>
{
    private readonly PeerTestFixture _fixture;

    public PeerDiscoveryTests(PeerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterPeer_Via_REST_Should_Return_Success()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var newPeer = new PeerNode
        {
            PeerId = "test-peer-rest",
            Endpoint = "http://localhost:5001",
            Metadata = new Dictionary<string, string>
            {
                ["region"] = "us-west",
                ["version"] = "1.0.0"
            }
        };

        // Act
        var response = await peer.HttpClient.PostAsJsonAsync("/api/peers", newPeer);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/peers/test-peer-rest");

        var registered = await response.Content.ReadFromJsonAsync<PeerNode>();
        registered.Should().NotBeNull();
        registered!.PeerId.Should().Be("test-peer-rest");
        registered.Endpoint.Should().Be("http://localhost:5001");
        registered.Status.Should().Be("active");
        registered.Metadata.Should().ContainKey("region");
    }

    [Fact]
    public async Task RegisterPeer_Via_gRPC_Should_Return_Success()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var request = new RegisterPeerRequest
        {
            PeerId = "test-peer-grpc",
            Endpoint = "http://localhost:5002",
            Metadata = { { "region", "eu-west" } }
        };

        // Act
        var response = await peer.GrpcClient.RegisterPeerAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.AssignedPeerId.Should().Be("test-peer-grpc");
        response.Message.Should().Be("Peer registered successfully");
    }

    [Fact]
    public async Task RegisterPeer_Without_PeerId_Should_Generate_Id()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var request = new RegisterPeerRequest
        {
            PeerId = "", // Empty ID should trigger auto-generation
            Endpoint = "http://localhost:5003"
        };

        // Act
        var response = await peer.GrpcClient.RegisterPeerAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.AssignedPeerId.Should().NotBeNullOrEmpty();
        Guid.TryParse(response.AssignedPeerId, out _).Should().BeTrue("Generated ID should be a valid GUID");
    }

    [Fact]
    public async Task GetAllPeers_Should_Return_Registered_Peers()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Register multiple peers
        var peerIds = new[] { "discovery-1", "discovery-2", "discovery-3" };
        foreach (var peerId in peerIds)
        {
            await peer.GrpcClient.RegisterPeerAsync(new RegisterPeerRequest
            {
                PeerId = peerId,
                Endpoint = $"http://localhost:{Random.Shared.Next(5000, 6000)}"
            });
        }

        // Act
        var response = await peer.HttpClient.GetAsync("/api/peers");
        var peers = await response.Content.ReadFromJsonAsync<List<PeerNode>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        peers.Should().NotBeNull();
        peers!.Should().HaveCountGreaterOrEqualTo(3);
        peers.Should().Contain(p => peerIds.Contains(p.PeerId));
    }

    [Fact]
    public async Task GetPeerInfo_Via_REST_Should_Return_Peer_Details()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var peerId = "rest-lookup-test";

        await peer.HttpClient.PostAsJsonAsync("/api/peers", new PeerNode
        {
            PeerId = peerId,
            Endpoint = "http://localhost:5010",
            Metadata = new Dictionary<string, string> { ["test"] = "value" }
        });

        // Act
        var response = await peer.HttpClient.GetAsync($"/api/peers/{peerId}");
        var foundPeer = await response.Content.ReadFromJsonAsync<PeerNode>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        foundPeer.Should().NotBeNull();
        foundPeer!.PeerId.Should().Be(peerId);
        foundPeer.Metadata.Should().ContainKey("test");
    }

    [Fact]
    public async Task GetPeerInfo_Via_gRPC_Should_Return_Peer_Details()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var peerId = "grpc-lookup-test";

        await peer.GrpcClient.RegisterPeerAsync(new RegisterPeerRequest
        {
            PeerId = peerId,
            Endpoint = "http://localhost:5011",
            Metadata = { { "protocol", "grpc" } }
        });

        // Act
        var response = await peer.GrpcClient.GetPeerInfoAsync(new PeerInfoRequest
        {
            PeerId = peerId
        });

        // Assert
        response.Should().NotBeNull();
        response.PeerId.Should().Be(peerId);
        response.Endpoint.Should().Be("http://localhost:5011");
        response.Status.Should().Be("active");
        response.Metadata.Should().ContainKey("protocol");
    }

    [Fact]
    public async Task GetPeerInfo_For_Nonexistent_Peer_Should_Return_NotFound()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var nonexistentId = "does-not-exist";

        // Act
        var response = await peer.HttpClient.GetAsync($"/api/peers/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnregisterPeer_Should_Remove_From_Registry()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var peerId = "to-be-removed";

        await peer.HttpClient.PostAsJsonAsync("/api/peers", new PeerNode
        {
            PeerId = peerId,
            Endpoint = "http://localhost:5020"
        });

        // Act - Delete the peer
        var deleteResponse = await peer.HttpClient.DeleteAsync($"/api/peers/{peerId}");

        // Assert - Verify deletion
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify peer is gone
        var getResponse = await peer.HttpClient.GetAsync($"/api/peers/{peerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Multiple_Peers_Can_Discover_Each_Other()
    {
        // Arrange
        var peer1 = _fixture.Peers[0];
        var peer2 = _fixture.Peers[1];

        // Register peer2 with peer1's registry
        await peer1.GrpcClient.RegisterPeerAsync(new RegisterPeerRequest
        {
            PeerId = peer2.PeerId,
            Endpoint = peer2.BaseAddress,
            Metadata = { { "source", "peer2" } }
        });

        // Register peer1 with peer2's registry
        await peer2.GrpcClient.RegisterPeerAsync(new RegisterPeerRequest
        {
            PeerId = peer1.PeerId,
            Endpoint = peer1.BaseAddress,
            Metadata = { { "source", "peer1" } }
        });

        // Act - Each peer queries for the other
        var peer1FindsPeer2 = await peer1.GrpcClient.GetPeerInfoAsync(new PeerInfoRequest
        {
            PeerId = peer2.PeerId
        });

        var peer2FindsPeer1 = await peer2.GrpcClient.GetPeerInfoAsync(new PeerInfoRequest
        {
            PeerId = peer1.PeerId
        });

        // Assert
        peer1FindsPeer2.Should().NotBeNull();
        peer1FindsPeer2.PeerId.Should().Be(peer2.PeerId);

        peer2FindsPeer1.Should().NotBeNull();
        peer2FindsPeer1.PeerId.Should().Be(peer1.PeerId);
    }

    [Fact]
    public async Task Peer_Registration_Should_Include_Timestamp()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var beforeRegistration = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        await Task.Delay(100); // Small delay to ensure timestamp difference
        var response = await peer.GrpcClient.RegisterPeerAsync(new RegisterPeerRequest
        {
            PeerId = "timestamp-test",
            Endpoint = "http://localhost:5030"
        });

        var peerInfo = await peer.GrpcClient.GetPeerInfoAsync(new PeerInfoRequest
        {
            PeerId = "timestamp-test"
        });

        var afterRegistration = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        peerInfo.RegisteredAt.Should().BeGreaterThan(beforeRegistration);
        peerInfo.RegisteredAt.Should().BeLessThanOrEqualTo(afterRegistration);
    }
}
