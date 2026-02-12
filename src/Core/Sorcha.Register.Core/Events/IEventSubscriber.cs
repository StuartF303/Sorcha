// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Register.Core.Events;

/// <summary>
/// Event subscriber abstraction for consuming events
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes to events on a topic
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="topic">Topic name</param>
    /// <param name="handler">Event handler function</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SubscribeAsync<TEvent>(string topic, Func<TEvent, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : class;
}
