// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Core.Events;

/// <summary>
/// Event publisher abstraction for register events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to a topic
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="topic">Topic name</param>
    /// <param name="eventData">Event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class;
}
