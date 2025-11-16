// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Core.Events;

namespace Sorcha.Register.Storage.InMemory;

/// <summary>
/// In-memory implementation of IEventPublisher for testing
/// Events are logged but not actually published
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<PublishedEvent> _publishedEvents = new();

    public Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var publishedEvent = new PublishedEvent
        {
            Topic = topic,
            EventType = typeof(TEvent).Name,
            Event = eventData,
            PublishedAt = DateTime.UtcNow
        };

        _publishedEvents.Add(publishedEvent);

        // In a real implementation, this would publish to a message bus
        // For testing, we just store it in memory
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all published events for testing verification
    /// </summary>
    public IReadOnlyList<PublishedEvent> GetPublishedEvents() => _publishedEvents.AsReadOnly();

    /// <summary>
    /// Gets published events of a specific type
    /// </summary>
    public IEnumerable<TEvent> GetPublishedEvents<TEvent>() where TEvent : class
    {
        return _publishedEvents
            .Where(e => e.Event is TEvent)
            .Select(e => (TEvent)e.Event);
    }

    /// <summary>
    /// Clears all published events
    /// </summary>
    public void Clear()
    {
        _publishedEvents.Clear();
    }
}

/// <summary>
/// Represents a published event for testing
/// </summary>
public class PublishedEvent
{
    public required string Topic { get; set; }
    public required string EventType { get; set; }
    public required object Event { get; set; }
    public DateTime PublishedAt { get; set; }
}
