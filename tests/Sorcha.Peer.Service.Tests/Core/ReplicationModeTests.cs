// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class ReplicationModeTests
{
    [Fact]
    public void ForwardOnly_ShouldHaveValue0()
    {
        ((int)ReplicationMode.ForwardOnly).Should().Be(0);
    }

    [Fact]
    public void FullReplica_ShouldHaveValue1()
    {
        ((int)ReplicationMode.FullReplica).Should().Be(1);
    }

    [Fact]
    public void Enum_ShouldHaveExactly2Values()
    {
        Enum.GetValues<ReplicationMode>().Should().HaveCount(2);
    }
}
