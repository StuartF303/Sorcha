// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class PeerRegisterInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var info = new PeerRegisterInfo();

        info.RegisterId.Should().BeEmpty();
        info.SyncState.Should().Be(RegisterSyncState.Subscribing);
        info.LatestVersion.Should().Be(0);
        info.LatestDocketVersion.Should().Be(0);
        info.IsPublic.Should().BeTrue();
        info.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CanServeFullReplica_ShouldBeTrue_WhenFullyReplicated()
    {
        var info = new PeerRegisterInfo { SyncState = RegisterSyncState.FullyReplicated };
        info.CanServeFullReplica.Should().BeTrue();
    }

    [Theory]
    [InlineData(RegisterSyncState.Subscribing)]
    [InlineData(RegisterSyncState.Syncing)]
    [InlineData(RegisterSyncState.Active)]
    [InlineData(RegisterSyncState.Error)]
    public void CanServeFullReplica_ShouldBeFalse_WhenNotFullyReplicated(RegisterSyncState state)
    {
        var info = new PeerRegisterInfo { SyncState = state };
        info.CanServeFullReplica.Should().BeFalse();
    }

    [Fact]
    public void ShouldAllowPropertyConfiguration()
    {
        var info = new PeerRegisterInfo
        {
            RegisterId = "register-abc-123",
            SyncState = RegisterSyncState.FullyReplicated,
            LatestVersion = 42,
            LatestDocketVersion = 10,
            IsPublic = false
        };

        info.RegisterId.Should().Be("register-abc-123");
        info.SyncState.Should().Be(RegisterSyncState.FullyReplicated);
        info.LatestVersion.Should().Be(42);
        info.LatestDocketVersion.Should().Be(10);
        info.IsPublic.Should().BeFalse();
    }
}
