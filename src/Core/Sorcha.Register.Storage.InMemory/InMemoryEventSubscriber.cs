// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Register.Core.Events;

namespace Sorcha.Register.Storage.InMemory;

/// <summary>
/// In-memory implementation of IEventSubscriber for testing.
/// When paired with InMemoryEventPublisher, enables end-to-end event flow in unit tests.
/// </summary>
public class InMemoryEventSubscriber : IEventSubscriber
{
    private readonly Dictionary<string, List<SubscriptionEntry>> _subscriptions = new();
    private readonly object _lock = new();

    public Task SubscribeAsync<TEvent>(string topic, Func<TEvent, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(topic, out var handlers))
            {
                handlers = new List<SubscriptionEntry>();
                _subscriptions[topic] = handlers;
            }

            handlers.Add(new SubscriptionEntry(
                typeof(TEvent),
                async obj => await handler((TEvent)obj)));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches an event to all matching subscribers. Used by InMemoryEventPublisher.
    /// </summary>
    internal async Task DispatchAsync<TEvent>(string topic, TEvent eventData) where TEvent : class
    {
        List<SubscriptionEntry> handlers;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(topic, out var entries))
                return;
            handlers = entries.ToList();
        }

        foreach (var handler in handlers)
        {
            if (handler.EventType.IsAssignableFrom(typeof(TEvent)))
            {
                await handler.Handler(eventData);
            }
        }
    }

    /// <summary>
    /// Gets the number of subscriptions for a topic
    /// </summary>
    public int GetSubscriptionCount(string topic)
    {
        lock (_lock)
        {
            return _subscriptions.TryGetValue(topic, out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Clears all subscriptions
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _subscriptions.Clear();
        }
    }

    internal record SubscriptionEntry(Type EventType, Func<object, Task> Handler);
}
