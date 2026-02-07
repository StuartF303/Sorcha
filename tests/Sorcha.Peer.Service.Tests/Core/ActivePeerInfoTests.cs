// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class ActivePeerInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var info = new ActivePeerInfo();

        info.PeerId.Should().BeEmpty();
        info.ConnectedPeerIds.Should().NotBeNull().And.BeEmpty();
        info.RegisterStates.Should().NotBeNull().And.BeEmpty();
        info.Status.Should().Be(PeerConnectionStatus.Disconnected);
        info.HeartbeatSequence.Should().Be(0);
        info.MissedHeartbeats.Should().Be(0);
    }

    [Fact]
    public void RecordHeartbeat_ShouldUpdateState()
    {
        var info = new ActivePeerInfo { MissedHeartbeats = 3, HeartbeatSequence = 5 };
        var beforeTime = DateTime.UtcNow;

        info.RecordHeartbeat();

        info.LastHeartbeat.Should().BeOnOrAfter(beforeTime);
        info.HeartbeatSequence.Should().Be(6);
        info.MissedHeartbeats.Should().Be(0);
    }

    [Fact]
    public void RecordMissedHeartbeat_ShouldIncrementCounter()
    {
        var info = new ActivePeerInfo { Status = PeerConnectionStatus.Connected };

        info.RecordMissedHeartbeat();

        info.MissedHeartbeats.Should().Be(1);
        info.Status.Should().Be(PeerConnectionStatus.Connected);
    }

    [Fact]
    public void RecordMissedHeartbeat_ShouldTransitionToTimeout_AfterMaxMisses()
    {
        var info = new ActivePeerInfo
        {
            Status = PeerConnectionStatus.Connected,
            MissedHeartbeats = PeerServiceConstants.MaxMissedHeartbeats - 1
        };

        info.RecordMissedHeartbeat();

        info.Status.Should().Be(PeerConnectionStatus.HeartbeatTimeout);
    }

    [Fact]
    public void IsHeartbeatTimedOut_ShouldReturnFalse_WhenRecent()
    {
        var info = new ActivePeerInfo { LastHeartbeat = DateTime.UtcNow };

        info.IsHeartbeatTimedOut().Should().BeFalse();
    }

    [Fact]
    public void IsHeartbeatTimedOut_ShouldReturnTrue_WhenStale()
    {
        var info = new ActivePeerInfo
        {
            LastHeartbeat = DateTime.UtcNow.AddSeconds(-(PeerServiceConstants.HeartbeatTimeoutSeconds + 1))
        };

        info.IsHeartbeatTimedOut().Should().BeTrue();
    }

    [Fact]
    public void RegisterStates_ShouldTrackPerRegisterState()
    {
        var info = new ActivePeerInfo();
        info.RegisterStates["register-1"] = RegisterSyncState.FullyReplicated;
        info.RegisterStates["register-2"] = RegisterSyncState.Syncing;

        info.RegisterStates.Should().HaveCount(2);
        info.RegisterStates["register-1"].Should().Be(RegisterSyncState.FullyReplicated);
        info.RegisterStates["register-2"].Should().Be(RegisterSyncState.Syncing);
    }

    [Fact]
    public void ConnectedPeerIds_ShouldTrackMultiplePeers()
    {
        var info = new ActivePeerInfo();
        info.ConnectedPeerIds.Add("peer-1");
        info.ConnectedPeerIds.Add("peer-2");
        info.ConnectedPeerIds.Add("peer-3");

        info.ConnectedPeerIds.Should().HaveCount(3).And.Contain("peer-2");
    }
}
