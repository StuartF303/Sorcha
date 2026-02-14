// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Storage.Redis;
using Xunit;

namespace Sorcha.Register.Storage.Redis.Tests;

public class RedisEventStreamConfigurationTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new RedisEventStreamConfiguration();

        config.StreamPrefix.Should().Be("sorcha:events:");
        config.ConsumerGroup.Should().Be("register-service");
        config.MaxStreamLength.Should().Be(10000);
        config.ReadBlockMilliseconds.Should().Be(5000);
        config.PendingIdleTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.BatchSize.Should().Be(10);
    }

    [Fact]
    public void Properties_CanBeCustomized()
    {
        var config = new RedisEventStreamConfiguration
        {
            StreamPrefix = "custom:",
            ConsumerGroup = "validator-service",
            MaxStreamLength = 5000,
            ReadBlockMilliseconds = 1000,
            PendingIdleTimeout = TimeSpan.FromMinutes(1),
            BatchSize = 50
        };

        config.StreamPrefix.Should().Be("custom:");
        config.ConsumerGroup.Should().Be("validator-service");
        config.MaxStreamLength.Should().Be(5000);
        config.ReadBlockMilliseconds.Should().Be(1000);
        config.PendingIdleTimeout.Should().Be(TimeSpan.FromMinutes(1));
        config.BatchSize.Should().Be(50);
    }
}
