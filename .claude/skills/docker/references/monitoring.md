# Monitoring Reference

## Contents
- OpenTelemetry Configuration
- Aspire Dashboard
- Health Check Endpoints
- Metrics and Tracing
- Troubleshooting

## OpenTelemetry Configuration

### Docker Compose OTEL Settings

```yaml
x-otel-env: &otel-env
  OTEL_EXPORTER_OTLP_ENDPOINT: http://aspire-dashboard:18889
  OTEL_SERVICE_NAME: ${OTEL_SERVICE_NAME:-sorcha-service}
  OTEL_RESOURCE_ATTRIBUTES: deployment.environment=docker

services:
  blueprint-service:
    environment:
      <<: *otel-env
      OTEL_SERVICE_NAME: blueprint-service  # Override per service
```

### Service Defaults Integration

All Sorcha services use `Sorcha.ServiceDefaults` which configures OpenTelemetry automatically:

```csharp
// Program.cs - adds health checks, OTEL, service discovery
builder.AddServiceDefaults();
```

The `ServiceDefaults` package configures:
- Logging with OpenTelemetry
- Metrics (ASP.NET Core, HTTP client, runtime)
- Distributed tracing
- Custom meters for Peer Service

## Aspire Dashboard

### Access Points

| Endpoint | Port | Purpose |
|----------|------|---------|
| Dashboard UI | 18888 | Web interface for traces/logs/metrics |
| OTLP gRPC | 4317 (internal 18889) | Telemetry ingestion |
| OTLP HTTP | 4318 (internal 18890) | Alternative ingestion |

### Docker Compose Configuration

```yaml
aspire-dashboard:
  image: mcr.microsoft.com/dotnet/aspire-dashboard:9.0
  container_name: sorcha-aspire-dashboard
  ports:
    - "18888:18888"  # Dashboard UI
    - "4317:18889"   # OTLP gRPC
    - "4318:18890"   # OTLP HTTP
  environment:
    - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
    - DASHBOARD__OTLP__AUTHMODE=Unsecured
```

### Viewing Telemetry

1. Open http://localhost:18888
2. Navigate to:
   - **Traces**: Distributed request tracing
   - **Metrics**: Service performance data
   - **Logs**: Structured log output

## Health Check Endpoints

### Standard Endpoints

All Sorcha services expose:

| Endpoint | Purpose |
|----------|---------|
| `/health` | Readiness check (dependencies healthy) |
| `/alive` | Liveness check (process running) |

### Health Check Exclusion from Tracing

```csharp
// ServiceDefaults excludes health checks from traces
tracing.AddAspNetCoreInstrumentation(tracing =>
    tracing.Filter = context =>
        !context.Request.Path.StartsWithSegments("/health")
        && !context.Request.Path.StartsWithSegments("/alive")
);
```

### Validating Health

```bash
# Check individual service health
curl http://localhost:5000/health   # Blueprint
curl http://localhost:5110/health   # Tenant
curl http://localhost:5290/health   # Register

# Via API Gateway
curl http://localhost:80/blueprint/health
```

## Metrics and Tracing

### Custom Metrics (Peer Service)

```csharp
// Sorcha.Peer.Service adds custom metrics
metrics.AddMeter("Sorcha.Peer.Service");
```

### Custom Activity Sources

```csharp
// Add custom tracing sources
tracing.AddSource("Sorcha.Peer.Service");
tracing.AddSource(builder.Environment.ApplicationName);
```

### Viewing in Dashboard

1. **Traces**: See request flow across services
2. **Metrics**: Monitor custom `Sorcha.Peer.Service` metrics
3. **Logs**: Filter by `OTEL_SERVICE_NAME`

## Troubleshooting

### No Telemetry Appearing

**Check 1:** Verify OTEL endpoint is correct
```bash
docker logs sorcha-aspire-dashboard 2>&1 | grep -i "listening"
```

**Check 2:** Verify service can reach dashboard
```bash
docker exec sorcha-blueprint-service wget -qO- http://aspire-dashboard:18889/health
```

**Check 3:** Verify environment variable is set
```bash
docker exec sorcha-blueprint-service printenv | grep OTEL
```

### Service Not Sending Traces

**Check:** Service must have `OTEL_EXPORTER_OTLP_ENDPOINT` set:
```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://aspire-dashboard:18889
```

### WARNING: Missing OTEL Endpoint Silently Disables Telemetry

**The Problem:**
```yaml
# BAD - No OTEL endpoint, telemetry silently disabled
blueprint-service:
  environment:
    ASPNETCORE_ENVIRONMENT: Development
    # Missing OTEL_EXPORTER_OTLP_ENDPOINT
```

**Why This Breaks:**
`ServiceDefaults` checks for `OTEL_EXPORTER_OTLP_ENDPOINT` before enabling exporters:
```csharp
var useOtlpExporter = !string.IsNullOrWhiteSpace(
    builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
```

**The Fix:**
```yaml
# GOOD - Use YAML anchor to ensure all services have OTEL config
environment:
  <<: *otel-env
```

### Debugging Container Logs

```bash
# Real-time logs for single service
docker-compose logs -f blueprint-service

# All services
docker-compose logs -f

# Last 100 lines with timestamps
docker-compose logs --tail=100 -t blueprint-service

# Filter for errors
docker-compose logs blueprint-service 2>&1 | grep -i "error\|exception"
```

### Health Check Iteration Pattern

1. Make configuration change
2. Restart service: `docker-compose restart blueprint-service`
3. Check health: `curl http://localhost:5000/health`
4. If unhealthy, check logs: `docker-compose logs --tail=50 blueprint-service`
5. Repeat until healthy