// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class RegisterSyncStateTests
{
    [Theory]
    [InlineData(RegisterSyncState.Subscribing, 0)]
    [InlineData(RegisterSyncState.Syncing, 1)]
    [InlineData(RegisterSyncState.FullyReplicated, 2)]
    [InlineData(RegisterSyncState.Active, 3)]
    [InlineData(RegisterSyncState.Error, 4)]
    public void Values_ShouldMatchExpectedIntegers(RegisterSyncState state, int expected)
    {
        ((int)state).Should().Be(expected);
    }

    [Fact]
    public void Enum_ShouldHaveExactly5Values()
    {
        Enum.GetValues<RegisterSyncState>().Should().HaveCount(5);
    }
}
