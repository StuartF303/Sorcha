# Task: Implement Event Abstractions

**ID:** REG-011
**Status:** Not Started
**Priority:** High
**Estimate:** 6 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Create event publishing and subscription abstractions to decouple the RegisterService from specific message broker implementations (Dapr, RabbitMQ, etc.).

## Tasks

### Event Publisher Interface
- [ ] Create `Events/IEventPublisher.cs` interface
- [ ] Define `PublishAsync<TEvent>(string topic, TEvent eventData)` method
- [ ] Add cancellation token support
- [ ] Add retry policy configuration
- [ ] Define event metadata (correlation ID, timestamp)

### Event Subscriber Interface
- [ ] Create `Events/IEventSubscriber.cs` interface
- [ ] Define `SubscribeAsync<TEvent>(string topic, Func<TEvent, Task> handler)` method
- [ ] Define subscription lifecycle methods
- [ ] Add error handling callbacks
- [ ] Support for dead-letter queue configuration

### Event Models
- [ ] Create `Events/Models/RegisterCreated.cs`
- [ ] Create `Events/Models/RegisterDeleted.cs`
- [ ] Create `Events/Models/RegisterUpdated.cs`
- [ ] Create `Events/Models/RegisterHeightUpdated.cs`
- [ ] Create `Events/Models/TransactionConfirmed.cs`
- [ ] Create `Events/Models/DocketConfirmed.cs`
- [ ] Add common base event properties (EventId, Timestamp, CorrelationId)

### Event Base Class
- [ ] Create `Events/Models/RegisterEvent.cs` base class
- [ ] Add `EventId` (Guid) property
- [ ] Add `Timestamp` (DateTime UTC) property
- [ ] Add `CorrelationId` (string) property
- [ ] Add `EventType` (string) property
- [ ] Add `Source` (string) property

### Event Envelope
- [ ] Create `Events/EventEnvelope<TEvent>` wrapper class
- [ ] Include event metadata
- [ ] Include routing information
- [ ] Include retry count
- [ ] Support for event versioning

### Configuration
- [ ] Create `Events/EventPublisherOptions.cs`
- [ ] Define retry policy settings
- [ ] Define timeout settings
- [ ] Define circuit breaker settings
- [ ] Define topic naming conventions

## Implementation Example

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string topic,
        TEvent eventData,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    Task PublishAsync<TEvent>(
        string topic,
        TEvent eventData,
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
        where TEvent : class;
}

public interface IEventSubscriber
{
    Task SubscribeAsync<TEvent>(
        string topic,
        Func<TEvent, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    Task SubscribeAsync<TEvent>(
        string topic,
        Func<TEvent, EventMetadata, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    Task UnsubscribeAsync(string topic);
}

public abstract class RegisterEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; }
    public string EventType { get; set; }
    public string Source { get; set; } = "RegisterService";
}

public class RegisterCreated : RegisterEvent
{
    public RegisterCreated()
    {
        EventType = "RegisterCreated";
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionConfirmed : RegisterEvent
{
    public TransactionConfirmed()
    {
        EventType = "TransactionConfirmed";
    }

    public string TransactionId { get; set; }
    public List<string> ToWallets { get; set; }
    public string Sender { get; set; }
    public string PreviousTransactionId { get; set; }
    public TransactionMetaData MetaData { get; set; }
}

public class DocketConfirmed : RegisterEvent
{
    public DocketConfirmed()
    {
        EventType = "DocketConfirmed";
    }

    public string RegisterId { get; set; }
    public ulong DocketId { get; set; }
    public List<string> TransactionIds { get; set; }
    public DateTime TimeStamp { get; set; }
}

public class EventMetadata
{
    public string CorrelationId { get; set; }
    public string CausationId { get; set; }
    public string UserId { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; }
}

public class EventEnvelope<TEvent> where TEvent : class
{
    public TEvent Data { get; set; }
    public EventMetadata Metadata { get; set; }
    public int RetryCount { get; set; }
    public string Version { get; set; } = "1.0";
}
```

## Acceptance Criteria

- [ ] Event publisher interface defined
- [ ] Event subscriber interface defined
- [ ] All event models created
- [ ] Base event class with common properties
- [ ] Event metadata support
- [ ] Cancellation token support
- [ ] XML documentation complete

## Definition of Done

- All interfaces and models created
- Code compiles without errors
- XML documentation complete
- Event versioning supported
- Design review approved
- README updated with event list

---

**Dependencies:** REG-001
**Blocks:** REG-004, REG-005, REG-006, REG-012, REG-013
