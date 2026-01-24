# YARP Workflows Reference

## Contents
- Adding a New Service Route
- Testing YARP Routes
- Debugging Route Issues
- Production Configuration

---

## Adding a New Service Route

Copy this checklist and track progress:
- [ ] Step 1: Add cluster definition
- [ ] Step 2: Add route definition(s)
- [ ] Step 3: Add health endpoint route
- [ ] Step 4: Update OpenAPI aggregation (if applicable)
- [ ] Step 5: Test through gateway

### Step 1: Add Cluster

In `appsettings.json` under `ReverseProxy.Clusters`:

```json
{
  "newservice-cluster": {
    "Destinations": {
      "destination1": {
        "Address": "http://newservice:8080"
      }
    }
  }
}
```

### Step 2: Add Routes

```json
{
  "newservice-route": {
    "ClusterId": "newservice-cluster",
    "Match": {
      "Path": "/api/newservice/{**catch-all}"
    },
    "Transforms": [
      { "PathPattern": "/api/{**catch-all}" }
    ]
  },
  "newservice-base-route": {
    "ClusterId": "newservice-cluster",
    "Match": {
      "Path": "/api/newservice"
    },
    "Transforms": [
      { "PathPattern": "/api" }
    ]
  }
}
```

### Step 3: Add Health Route

```json
{
  "newservice-status-route": {
    "ClusterId": "newservice-cluster",
    "Match": {
      "Path": "/api/newservice/status"
    },
    "Transforms": [
      { "PathPattern": "/health" }
    ]
  }
}
```

### Step 4: Update Services Configuration

Add to `Services` section for health aggregation:

```json
{
  "Services": {
    "NewService": {
      "Url": "http://newservice:8080"
    }
  }
}
```

---

## Testing YARP Routes

### Integration Test Pattern

```csharp
public class GatewayRoutingTests : GatewayIntegrationTestBase
{
    [Fact]
    public async Task NewServiceRoutes_AreProxiedCorrectly()
    {
        // Request through gateway
        var response = await GatewayClient!.GetAsync("/api/newservice/items");
        
        // Should be proxied to backend service
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task NewServiceStatus_MapsToHealthEndpoint()
    {
        var response = await GatewayClient!.GetAsync("/api/newservice/status");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
```

### Manual Testing with curl

```bash
# Test route through gateway
curl http://localhost:80/api/blueprint/blueprints

# Test health mapping
curl http://localhost:80/api/blueprint/status

# Test with verbose output to see routing
curl -v http://localhost:80/api/wallet/wallets
```

---

## Debugging Route Issues

### Enable YARP Logging

In `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Yarp": "Debug"
    }
  }
}
```

### Common Issues Checklist

1. **404 Not Found**
   - Check route `Path` pattern matches request
   - Verify cluster destination address is correct
   - Ensure service is running and reachable

2. **502 Bad Gateway**
   - Backend service not responding
   - Check Docker network connectivity
   - Verify service health endpoints

3. **Endpoint Returns Gateway Response Instead of Backend**
   - Route may be matching a gateway endpoint
   - Check order of `app.Map*()` calls
   - Ensure `MapReverseProxy()` is last

### Validation Loop

1. Make configuration changes
2. Restart gateway: `docker-compose restart api-gateway`
3. Test with curl: `curl -v http://localhost:80/api/path`
4. Check logs: `docker-compose logs -f api-gateway`
5. If request fails, check YARP debug logs and repeat from step 1

---

## Production Configuration

### Health Checks

```json
{
  "Clusters": {
    "blueprint-cluster": {
      "HealthCheck": {
        "Active": {
          "Enabled": true,
          "Interval": "00:00:30",
          "Timeout": "00:00:10",
          "Path": "/health"
        }
      },
      "Destinations": {
        "destination1": {
          "Address": "http://blueprint-service:8080",
          "Health": "http://blueprint-service:8080/health"
        }
      }
    }
  }
}
```

### Load Balancing

```json
{
  "Clusters": {
    "register-cluster": {
      "LoadBalancingPolicy": "RoundRobin",
      "Destinations": {
        "node1": { "Address": "http://register-1:8080" },
        "node2": { "Address": "http://register-2:8080" }
      }
    }
  }
}
```

Available policies: `RoundRobin`, `Random`, `PowerOfTwoChoices`, `LeastRequests`, `FirstAlphabetical`

### Session Affinity

```json
{
  "Clusters": {
    "stateful-cluster": {
      "SessionAffinity": {
        "Enabled": true,
        "Policy": "Cookie",
        "AffinityKeyName": ".Yarp.Affinity"
      }
    }
  }
}
```

---

## DO/DON'T Quick Reference

| DO | DON'T |
|----|-------|
| Call `MapReverseProxy()` last | Put YARP before custom endpoints |
| Use `{**catch-all}` for path segments | Use `{*any}` inconsistently |
| Add transforms when paths differ | Assume paths pass through unchanged |
| Test routes after configuration changes | Deploy without testing routing |
| Enable YARP debug logging when troubleshooting | Guess at route matching issues |