# Task: Implement Dapr Event Publisher/Subscriber

**ID:** REG-012
**Status:** Not Started
**Priority:** High
**Estimate:** 8 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Implement Dapr-based event publishing and subscription to enable integration with existing SICCAR infrastructure.

## Tasks

### Project Setup
- [ ] Create `Siccar.RegisterService.Events.Dapr` project
- [ ] Add NuGet package `Dapr.Client` (1.14.0 or later)
- [ ] Add NuGet package `Microsoft.Extensions.Logging`
- [ ] Add NuGet package `Microsoft.Extensions.Options`
- [ ] Reference `Siccar.RegisterService` core library
- [ ] Reference `Siccar.Common` for Topics constants

### Configuration
- [ ] Create `DaprEventBusOptions.cs`
- [ ] Define `PubSubName` property (default: "pubsub")
- [ ] Define retry policy settings
- [ ] Define timeout settings
- [ ] Define circuit breaker settings

### Publisher Implementation
- [ ] Create `DaprEventPublisher.cs` class
- [ ] Implement `IEventPublisher` interface
- [ ] Add constructor with:
  - `DaprClient` daprClient
  - `IOptions<DaprEventBusOptions>` options
  - `ILogger<DaprEventPublisher>` logger
- [ ] Implement `PublishAsync<TEvent>(string topic, TEvent eventData)`
- [ ] Add retry logic with exponential backoff
- [ ] Add correlation ID to Dapr metadata
- [ ] Log all publish operations

### Subscriber Implementation
- [ ] Create `DaprEventSubscriber.cs` class
- [ ] Implement `IEventSubscriber` interface
- [ ] Use Dapr subscription API for dynamic subscriptions
- [ ] Handle subscription lifecycle
- [ ] Implement error handling and retry
- [ ] Add dead-letter queue support

### Event Serialization
- [ ] Use System.Text.Json for serialization
- [ ] Configure JSON options (camelCase, null handling)
- [ ] Support event versioning
- [ ] Handle deserialization errors gracefully

### Retry and Circuit Breaker
- [ ] Implement Polly retry policy
- [ ] Configure exponential backoff (2s, 4s, 8s, 16s)
- [ ] Implement circuit breaker
- [ ] Add timeout policy (30s default)
- [ ] Log retry attempts

### Error Handling
- [ ] Handle Dapr connectivity errors
- [ ] Handle serialization errors
- [ ] Handle subscription errors
- [ ] Implement dead-letter queue for failed messages
- [ ] Log all errors with context

### Integration with Dapr Topics
- [ ] Map to existing topic names from `Topics` class:
  - `Topics.TransactionValidationCompletedTopicName`
  - `Topics.TransactionSubmittedTopicName`
  - `Topics.DocketConfirmedTopicName`
  - `Topics.WalletAddressCreationTopicName`
  - `Topics.RegisterCreatedTopicName`
  - `Topics.RegisterDeletedTopicName`
  - `Topics.TransactionConfirmedTopicName`

## Implementation Example

```csharp
public class DaprEventPublisher : IEventPublisher
{
    private readonly DaprClient _daprClient;
    private readonly DaprEventBusOptions _options;
    private readonly ILogger<DaprEventPublisher> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public DaprEventPublisher(
        DaprClient daprClient,
        IOptions<DaprEventBusOptions> options,
        ILogger<DaprEventPublisher> logger)
    {
        _daprClient = daprClient;
        _options = options.Value;
        _logger = logger;

        // Configure retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} for publishing event after {Delay}s",
                        retryCount, timeSpan.TotalSeconds);
                });
    }

    public async Task PublishAsync<TEvent>(
        string topic,
        TEvent eventData,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await PublishAsync(topic, eventData, new EventMetadata(), cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        string topic,
        TEvent eventData,
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var daprMetadata = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(metadata.CorrelationId))
                {
                    daprMetadata["correlationId"] = metadata.CorrelationId;
                }
                if (!string.IsNullOrEmpty(metadata.CausationId))
                {
                    daprMetadata["causationId"] = metadata.CausationId;
                }

                _logger.LogDebug(
                    "Publishing event to topic {Topic} via Dapr pubsub {PubSub}",
                    topic, _options.PubSubName);

                await _daprClient.PublishEventAsync(
                    _options.PubSubName,
                    topic,
                    eventData,
                    daprMetadata,
                    cancellationToken);

                _logger.LogInformation(
                    "Event published to topic {Topic} successfully", topic);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event to topic {Topic} after retries", topic);
            throw;
        }
    }
}

public class DaprEventSubscriber : IEventSubscriber
{
    private readonly DaprClient _daprClient;
    private readonly DaprEventBusOptions _options;
    private readonly ILogger<DaprEventSubscriber> _logger;
    private readonly Dictionary<string, List<Delegate>> _handlers;

    public DaprEventSubscriber(
        DaprClient daprClient,
        IOptions<DaprEventBusOptions> options,
        ILogger<DaprEventSubscriber> logger)
    {
        _daprClient = daprClient;
        _options = options.Value;
        _logger = logger;
        _handlers = new Dictionary<string, List<Delegate>>();
    }

    public async Task SubscribeAsync<TEvent>(
        string topic,
        Func<TEvent, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (!_handlers.ContainsKey(topic))
        {
            _handlers[topic] = new List<Delegate>();
        }

        _handlers[topic].Add(handler);

        _logger.LogInformation(
            "Subscribed to topic {Topic} with handler for {EventType}",
            topic, typeof(TEvent).Name);

        await Task.CompletedTask;
    }

    public async Task UnsubscribeAsync(string topic)
    {
        _handlers.Remove(topic);
        _logger.LogInformation("Unsubscribed from topic {Topic}", topic);
        await Task.CompletedTask;
    }

    // This would be called by Dapr when an event is received
    // Usually configured via [Topic] attribute in ASP.NET Core controllers
    public async Task HandleEventAsync<TEvent>(
        string topic,
        TEvent eventData,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (!_handlers.TryGetValue(topic, out var handlerList))
        {
            _logger.LogWarning(
                "Received event for topic {Topic} but no handlers registered", topic);
            return;
        }

        foreach (var handler in handlerList.OfType<Func<TEvent, Task>>())
        {
            try
            {
                await handler(eventData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error handling event from topic {Topic}", topic);
                // Continue processing other handlers
            }
        }
    }
}

public class DaprEventBusOptions
{
    public string PubSubName { get; set; } = "pubsub";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public int CircuitBreakerThreshold { get; set; } = 5;
}
```

## Acceptance Criteria

- [ ] Publisher implements IEventPublisher
- [ ] Subscriber implements IEventSubscriber
- [ ] Retry logic with exponential backoff
- [ ] Proper error handling
- [ ] Correlation ID support
- [ ] Comprehensive logging
- [ ] Integration tests with Dapr sidecar

## Definition of Done

- All methods implemented
- Integration tests with Dapr passing
- Retry logic verified
- Error scenarios tested
- Code review approved
- XML documentation complete
- README with Dapr setup instructions

---

**Dependencies:** REG-001, REG-011
**Blocks:** REG-016, REG-026
