// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Register.Storage.Redis;

/// <summary>
/// Configuration for Redis Streams event infrastructure
/// </summary>
public class RedisEventStreamConfiguration
{
    /// <summary>
    /// Redis key prefix for event streams (e.g. "sorcha:events:")
    /// </summary>
    public string StreamPrefix { get; set; } = "sorcha:events:";

    /// <summary>
    /// Consumer group name identifying this service instance
    /// </summary>
    public string ConsumerGroup { get; set; } = "register-service";

    /// <summary>
    /// Approximate maximum stream length for XADD MAXLEN ~ trimming
    /// </summary>
    public int MaxStreamLength { get; set; } = 10000;

    /// <summary>
    /// Block timeout in milliseconds for XREADGROUP
    /// </summary>
    public int ReadBlockMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Reclaim pending messages after this idle time
    /// </summary>
    public TimeSpan PendingIdleTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Messages per XREADGROUP call
    /// </summary>
    public int BatchSize { get; set; } = 10;
}
