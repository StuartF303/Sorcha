// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class SyncCheckpointTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var cp = new SyncCheckpoint();

        cp.PeerId.Should().BeEmpty();
        cp.RegisterId.Should().BeEmpty();
        cp.CurrentVersion.Should().Be(0);
        cp.LastSyncTime.Should().Be(0);
        cp.TotalItems.Should().Be(0);
        cp.SourcePeerId.Should().BeNull();
    }

    [Fact]
    public void UpdateAfterSync_ShouldUpdateAllFields()
    {
        var cp = new SyncCheckpoint { PeerId = "peer1", RegisterId = "register1" };

        cp.UpdateAfterSync(newVersion: 42, itemCount: 100);

        cp.CurrentVersion.Should().Be(42);
        cp.TotalItems.Should().Be(100);
        cp.LastSyncTime.Should().BeCloseTo(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 1000);
    }

    [Fact]
    public void UpdateAfterSync_ShouldResetNextSyncDue()
    {
        var cp = new SyncCheckpoint
        {
            NextSyncDue = DateTime.UtcNow.AddMinutes(-10) // already past
        };

        cp.UpdateAfterSync(newVersion: 1, itemCount: 1);

        cp.NextSyncDue.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void IsSyncDue_ShouldReturnTrue_WhenPastDueTime()
    {
        var cp = new SyncCheckpoint
        {
            NextSyncDue = DateTime.UtcNow.AddMinutes(-1)
        };

        cp.IsSyncDue().Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_ShouldReturnFalse_WhenBeforeDueTime()
    {
        var cp = new SyncCheckpoint
        {
            NextSyncDue = DateTime.UtcNow.AddMinutes(5)
        };

        cp.IsSyncDue().Should().BeFalse();
    }

    [Fact]
    public void RegisterId_ShouldBeSettable()
    {
        var cp = new SyncCheckpoint
        {
            PeerId = "peer1",
            RegisterId = "my-register"
        };

        cp.RegisterId.Should().Be("my-register");
    }

    [Fact]
    public void SourcePeerId_ShouldBeSettable()
    {
        var cp = new SyncCheckpoint
        {
            SourcePeerId = "source-peer-1"
        };

        cp.SourcePeerId.Should().Be("source-peer-1");
    }
}
