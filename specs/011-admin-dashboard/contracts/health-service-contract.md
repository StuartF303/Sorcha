# Health Check Service Contract

**Feature Branch**: `011-admin-dashboard`
**Date**: 2026-01-19

## Overview

The Health Check Service is a client-side Blazor service that polls multiple backend service health endpoints and aggregates the results for display on the Admin Dashboard.

## Service Interface

```csharp
// Location: Sorcha.UI.Core/Services/IHealthCheckService.cs

public interface IHealthCheckService : IAsyncDisposable
{
    /// <summary>
    /// Current health status of all monitored services.
    /// </summary>
    IReadOnlyList<ServiceHealthStatus> ServiceStatuses { get; }

    /// <summary>
    /// Event raised when any service health status changes.
    /// </summary>
    event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Start polling health endpoints at the configured interval.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop polling and clean up resources.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Force an immediate health check of all services.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health status for a specific service.
    /// </summary>
    ServiceHealthStatus? GetServiceStatus(string serviceKey);
}
```

## Data Models

```csharp
// Location: Sorcha.UI.Core/Models/ServiceHealthStatus.cs

public record ServiceHealthStatus
{
    public required string ServiceName { get; init; }
    public required string ServiceKey { get; init; }
    public required string Endpoint { get; init; }
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    public DateTimeOffset? LastCheckTime { get; set; }
    public TimeSpan? LastCheckDuration { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum HealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}

public class HealthStatusChangedEventArgs : EventArgs
{
    public required string ServiceKey { get; init; }
    public required HealthStatus OldStatus { get; init; }
    public required HealthStatus NewStatus { get; init; }
}
```

## Configuration

```csharp
// Location: Sorcha.UI.Core/Models/HealthCheckOptions.cs

public class HealthCheckOptions
{
    /// <summary>
    /// Polling interval in milliseconds. Default: 30000 (30 seconds)
    /// </summary>
    public int PollingIntervalMs { get; set; } = 30_000;

    /// <summary>
    /// Timeout for individual health checks in milliseconds. Default: 5000 (5 seconds)
    /// </summary>
    public int TimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// Services to monitor.
    /// </summary>
    public List<ServiceEndpointConfig> Services { get; set; } = [];
}

public record ServiceEndpointConfig
{
    public required string ServiceName { get; init; }
    public required string ServiceKey { get; init; }
    public required string HealthEndpoint { get; init; }
}
```

## Default Service Configuration

| Service | Key | Endpoint |
|---------|-----|----------|
| Blueprint Service | blueprint | `{API_GATEWAY}/blueprint/health` |
| Register Service | register | `{API_GATEWAY}/register/health` |
| Wallet Service | wallet | `{API_GATEWAY}/wallet/health` |
| Tenant Service | tenant | `{API_GATEWAY}/tenant/health` |
| Validator Service | validator | `{API_GATEWAY}/validator/health` |
| Peer Service | peer | `{API_GATEWAY}/peer/health` |
| API Gateway | gateway | `{API_GATEWAY}/health` |

## Health Check Response Format

Standard ASP.NET Core Health Check response:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "self": {
      "data": {},
      "duration": "00:00:00.0012345",
      "status": "Healthy",
      "tags": ["live"]
    }
  }
}
```

## Status Mapping

| HTTP Status | Response Status | Mapped HealthStatus |
|-------------|-----------------|---------------------|
| 200 | "Healthy" | Healthy |
| 200 | "Degraded" | Degraded |
| 503 | "Unhealthy" | Unhealthy |
| Timeout | - | Unknown |
| Network Error | - | Unknown |

## DI Registration

```csharp
// Location: Sorcha.UI.Web.Client/Program.cs

builder.Services.Configure<HealthCheckOptions>(options =>
{
    options.PollingIntervalMs = 30_000;
    options.Services =
    [
        new ServiceEndpointConfig
        {
            ServiceName = "Blueprint Service",
            ServiceKey = "blueprint",
            HealthEndpoint = "/blueprint/health"
        },
        // ... other services
    ];
});

builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
```

## Usage in Components

```razor
@inject IHealthCheckService HealthService
@implements IDisposable

<MudGrid>
    @foreach (var service in HealthService.ServiceStatuses)
    {
        <MudItem xs="12" sm="6" md="4">
            <ServiceHealthCard Status="@service" />
        </MudItem>
    }
</MudGrid>

@code {
    protected override async Task OnInitializedAsync()
    {
        HealthService.HealthStatusChanged += OnHealthChanged;
        await HealthService.StartAsync();
    }

    private void OnHealthChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        HealthService.HealthStatusChanged -= OnHealthChanged;
    }
}
```
