# YARP Patterns Reference

## Contents
- Route Definition Patterns
- Path Transformation Patterns
- Cluster Configuration
- Static Content Routing
- Anti-Patterns

---

## Route Definition Patterns

### Service API Route

Standard pattern for proxying API requests to a backend service:

```json
{
  "blueprint-route": {
    "ClusterId": "blueprint-cluster",
    "Match": {
      "Path": "/api/blueprint/{**catch-all}"
    },
    "Transforms": [
      { "PathPattern": "/api/{**catch-all}" }
    ]
  }
}
```

**Flow:** `/api/blueprint/blueprints/123` → Blueprint Service at `/api/blueprints/123`

### Direct Pass-Through Route

When external and internal paths match:

```json
{
  "organizations-direct-route": {
    "ClusterId": "tenant-cluster",
    "Match": {
      "Path": "/api/organizations/{**catch-all}"
    },
    "Transforms": [
      { "PathPattern": "/api/organizations/{**catch-all}" }
    ]
  }
}
```

### Versioned API Route

When backend uses API versioning internally:

```json
{
  "wallet-route": {
    "ClusterId": "wallet-cluster",
    "Match": {
      "Path": "/api/wallet/{**catch-all}"
    },
    "Transforms": [
      { "PathPattern": "/api/v1/{**catch-all}" }
    ]
  }
}
```

**Flow:** `/api/wallet/wallets` → Wallet Service at `/api/v1/wallets`

---

## Path Transformation Patterns

### Strip Service Prefix

```json
"Transforms": [{ "PathPattern": "/api/{**catch-all}" }]
```

Removes the service identifier (`/api/blueprint/`) keeping only the resource path.

### Add Version Prefix

```json
"Transforms": [{ "PathPattern": "/api/v1/{**catch-all}" }]
```

Adds version prefix for backends with versioned APIs.

### Health Endpoint Mapping

Unify health checks across services:

```json
{
  "wallet-status-route": {
    "ClusterId": "wallet-cluster",
    "Match": { "Path": "/api/wallet/status" },
    "Transforms": [{ "PathPattern": "/health" }]
  }
}
```

### Preserve Forwarded Headers

```json
"Transforms": [
  { "PathPattern": "/api/{**catch-all}" },
  { "X-Forwarded": "Set" }
]
```

Sets `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`.

---

## Cluster Configuration

### Single Destination Cluster

```json
{
  "Clusters": {
    "blueprint-cluster": {
      "Destinations": {
        "destination1": {
          "Address": "http://blueprint-service:8080"
        }
      }
    }
  }
}
```

### Multiple Destinations (Load Balancing)

```json
{
  "Clusters": {
    "register-cluster": {
      "LoadBalancingPolicy": "RoundRobin",
      "Destinations": {
        "primary": { "Address": "http://register-1:8080" },
        "secondary": { "Address": "http://register-2:8080" }
      }
    }
  }
}
```

---

## Static Content Routing

### Blazor WASM Routes

```json
{
  "admin-ui-static-framework": {
    "ClusterId": "admin-cluster",
    "Match": { "Path": "/_framework/{**catch-all}" },
    "Transforms": [
      { "PathPattern": "/_framework/{**catch-all}" },
      { "X-Forwarded": "Set" }
    ]
  },
  "admin-ui-blazor-hub": {
    "ClusterId": "admin-cluster",
    "Match": { "Path": "/_blazor/{**catch-all}" },
    "Transforms": [
      { "PathPattern": "/_blazor/{**catch-all}" },
      { "X-Forwarded": "Set" }
    ]
  }
}
```

See the **blazor** skill for Blazor-specific patterns.

---

## Anti-Patterns

### WARNING: MapReverseProxy Before Custom Endpoints

**The Problem:**

```csharp
// BAD - YARP intercepts everything first
app.MapReverseProxy();
app.MapGet("/api/health", ...);  // Never reached!
```

**Why This Breaks:**
1. YARP catches all requests matching routes
2. Custom gateway endpoints never execute
3. Health aggregation, stats, and other gateway features fail

**The Fix:**

```csharp
// GOOD - Custom endpoints execute first
app.MapGet("/api/health", ...);
app.MapGet("/api/stats", ...);
app.MapReverseProxy();  // MUST BE LAST
```

### WARNING: Route Order Conflicts

**The Problem:**

```json
{
  "catch-all-route": {
    "Match": { "Path": "/{**catch-all}" }
  },
  "specific-route": {
    "Match": { "Path": "/api/specific" }
  }
}
```

**Why This Breaks:** Catch-all may match before specific routes depending on configuration order.

**The Fix:** Define specific routes before general routes, or use route `Order` property:

```json
{
  "specific-route": {
    "Order": 1,
    "Match": { "Path": "/api/specific" }
  },
  "catch-all-route": {
    "Order": 100,
    "Match": { "Path": "/{**catch-all}" }
  }
}
```

### WARNING: Missing Transforms for Path Mismatch

**The Problem:**

```json
{
  "Match": { "Path": "/api/blueprint/{**catch-all}" }
  // No transforms - forwards /api/blueprint/... as-is
}
```

**Why This Breaks:** Backend receives `/api/blueprint/blueprints` but expects `/api/blueprints`.

**The Fix:** Always add PathPattern transform when external/internal paths differ.