# Sorcha Performance & Load Tests

NBomber-based performance and load testing for the API Gateway and backend services.

## Overview

These tests measure:
- **Throughput**: Requests per second
- **Latency**: Response times (min, mean, max, percentiles)
- **Error Rate**: Failed requests under load
- **Stability**: Performance degradation over time

## Running Tests

### Prerequisites
- Services must be running (via AppHost or docker-compose)
- Gateway accessible at https://localhost:7082 (or custom URL)

### Run All Performance Tests
```bash
cd tests/Sorcha.Performance.Tests
dotnet run
```

### Target Custom URL
```bash
dotnet run https://your-gateway-url
```

### View Reports
Reports are generated in `performance-reports/` folder:
- **HTML Report**: Open `performance-reports/report.html` in browser
- **Markdown Report**: View `performance-reports/report.md`

## Test Scenarios

### 1. Health Check Load Test
- **Rate**: 100 requests/second
- **Duration**: 30 seconds
- **Total**: ~3,000 requests
- **Endpoint**: `/api/health`

Tests basic endpoint availability and gateway responsiveness.

### 2. Blueprint API Load Test
- **Rate**: 50 requests/second
- **Duration**: 30 seconds
- **Total**: ~1,500 requests
- **Endpoint**: `/api/blueprint/blueprints`

Tests Blueprint service throughput via gateway proxy.

### 3. Peer API Load Test
- **Rate**: 50 requests/second
- **Duration**: 30 seconds
- **Total**: ~1,500 requests
- **Endpoint**: `/api/peer/peers`

Tests Peer service throughput via gateway proxy.

### 4. Mixed Gateway Load Test
- **Ramp Up**: 10-100 requests/second over 20 seconds
- **Sustained**: 100 requests/second for 30 seconds
- **Ramp Down**: 100-0 requests/second over 10 seconds
- **Endpoints**: Random mix of health, stats, and service endpoints

Tests gateway under realistic mixed load with ramp-up/down.

## Interpreting Results

### Key Metrics

**RPS (Requests Per Second)**
- Actual throughput achieved
- Compare to target rate
- Lower than target = performance bottleneck

**Latency Percentiles**
- **P50 (Median)**: Typical response time
- **P75**: 75% of requests faster than this
- **P95**: 95% of requests faster than this
- **P99**: 99% of requests faster than this

**OK vs Fail**
- **OK**: Successful responses (200-299 status codes)
- **Fail**: Failed responses (4xx, 5xx, timeouts)
- Target: >99% OK rate

### Example Output
```
scenario: 'Health Check Load Test', duration: '00:00:30', ok count: 2995, fail count: 5, rps: 99.8

step: 'health_check'
  request count:      all = 3000 | ok = 2995 | fail = 5
  latency (ms):       min = 5 | mean = 12 | max = 87 | 50% = 10 | 75% = 15 | 95% = 25 | 99% = 45
  throughput (rps):   all = 100 | ok = 99.8 | fail = 0.2
```

### Performance Targets

**Good Performance:**
- P95 latency < 100ms
- P99 latency < 200ms
- >99.5% success rate
- Achieved target RPS

**Needs Optimization:**
- P95 latency > 200ms
- P99 latency > 500ms
- <99% success rate
- RPS significantly below target

## Customizing Tests

### Adjust Load Levels

Edit `Program.cs` scenarios:

```csharp
// Increase rate
Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))

// Longer duration
.WithLoadSimulations(
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
)
```

### Add New Scenarios

```csharp
static ScenarioProps CreateMyScenario(string baseUrl)
{
    var http = new HttpClient();

    var step = Step.Create("my_step", async context =>
    {
        var response = await http.GetAsync($"{baseUrl}/api/my-endpoint");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });

    return ScenarioBuilder
        .CreateScenario("My Test", step)
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
}
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Performance Tests
  run: |
    cd tests/Sorcha.Performance.Tests
    dotnet run https://staging.example.com

- name: Upload Performance Reports
  uses: actions/upload-artifact@v3
  with:
    name: performance-reports
    path: tests/Sorcha.Performance.Tests/performance-reports/
```

## Troubleshooting

### High Latency
- Check Docker resources (CPU/RAM)
- Check Redis performance
- Profile backend services
- Verify network isn't bottleneck

### Low RPS
- Increase `--rate` parameter
- Check if services throttling
- Verify gateway not rate limiting

### High Error Rate
- Check service logs for errors
- Verify endpoints are correct
- Check for timeouts (increase HttpClient timeout if needed)

### Tests Timeout
```csharp
// Increase HttpClient timeout
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
```

## Load Testing Patterns

### Spike Test
```csharp
Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
```

### Stress Test
```csharp
Simulation.RampingInject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
```

### Soak Test (Endurance)
```csharp
Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromHours(1))
```

## Resources

- [NBomber Documentation](https://nbomber.com/)
- [Load Testing Best Practices](https://nbomber.com/docs/loadtesting-basics)
- [Performance Testing Guide](https://learn.microsoft.com/aspnet/core/performance/)
