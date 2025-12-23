# Backend Selection API

**Feature**: 001-hardware-crypto-enclaves
**Implements**: FR-002, FR-008, FR-014, FR-021
**Date**: 2025-12-23

This document defines the API for automatic backend selection based on environment detection and configuration priority.

---

## Overview

The Backend Selection API provides automatic detection of deployment environments and selects the most appropriate cryptographic backend based on:

1. Environment detection (Azure, AWS, GCP, Kubernetes, OS, Browser)
2. Configured precedence order (FR-021)
3. Backend availability status (health checks)
4. Environment-specific security requirements (FR-014)

---

## IBackendSelector Interface

```csharp
namespace Sorcha.Cryptography.Abstractions;

/// <summary>
/// Selects the appropriate cryptographic backend based on environment and configuration.
/// Implements FR-002, FR-008, FR-021.
/// </summary>
public interface IBackendSelector
{
    /// <summary>
    /// Selects the best available backend for the current environment.
    /// </summary>
    /// <param name="configuration">Environment configuration with backend precedence order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected cryptographic backend.</returns>
    /// <remarks>
    /// Selection algorithm (FR-021):
    /// 1. Get backends in precedence order from configuration
    /// 2. Filter by environment-allowed backends
    /// 3. For each backend in order:
    ///    a. Check availability (health check)
    ///    b. If available and meets security requirements, return
    /// 4. If no backend available, throw exception (or fallback if enabled)
    ///
    /// Default precedence order (configurable per FR-021):
    /// 1. Cloud HSM (Azure/AWS/GCP)
    /// 2. Kubernetes Secrets with external KMS
    /// 3. OS-native secure storage
    /// 4. Software-only (development mode with warnings)
    /// </remarks>
    /// <exception cref="BackendSelectionException">If no suitable backend found.</exception>
    Task<ICryptographicBackend> SelectBackendAsync(
        EnvironmentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available backends in precedence order.
    /// </summary>
    /// <param name="configuration">Environment configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available backends, ordered by precedence.</returns>
    /// <remarks>
    /// Useful for diagnostic purposes or allowing users to manually select a backend.
    /// </remarks>
    Task<IReadOnlyList<ICryptographicBackend>> GetAvailableBackendsAsync(
        EnvironmentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a backend meets security requirements for the environment.
    /// </summary>
    /// <param name="backend">The backend to validate.</param>
    /// <param name="configuration">Environment configuration with security requirements.</param>
    /// <returns>True if backend meets requirements, false otherwise.</returns>
    /// <remarks>
    /// Validation rules (FR-014):
    /// - Production environment: backend.SecurityLevel MUST be HSM
    /// - Production environment: backend.ProviderType MUST be Azure/AWS/GCP (not OS, not Software)
    /// - Staging environment: backend.SecurityLevel SHOULD be HSM (TPM acceptable)
    /// - Development environment: Any security level allowed with warnings
    /// </remarks>
    bool ValidateBackendSecurity(
        ICryptographicBackend backend,
        EnvironmentConfiguration configuration);
}

public class BackendSelectionException : Exception
{
    public EnvironmentType EnvironmentType { get; }
    public IReadOnlyList<BackendType> AttemptedBackends { get; }

    public BackendSelectionException(
        string message,
        EnvironmentType environmentType,
        IReadOnlyList<BackendType> attemptedBackends,
        Exception? innerException = null)
        : base(message, innerException)
    {
        EnvironmentType = environmentType;
        AttemptedBackends = attemptedBackends;
    }
}
```

---

## IEnvironmentDetector Interface

```csharp
namespace Sorcha.Cryptography.Abstractions;

/// <summary>
/// Detects the deployment environment by querying cloud metadata services and Kubernetes API.
/// Implements FR-002.
/// </summary>
public interface IEnvironmentDetector
{
    /// <summary>
    /// Detects the current deployment environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detected backend type, or BackendType.Os if no cloud environment detected.</returns>
    /// <remarks>
    /// Detection order (with 2-second timeout per check):
    /// 1. Azure Instance Metadata Service (IMDS) - http://169.254.169.254/metadata/instance
    /// 2. AWS IMDSv2 - http://169.254.169.254/latest/api/token
    /// 3. GCP Metadata Server - http://metadata.google.internal/computeMetadata/v1/
    /// 4. Kubernetes Service Account - /var/run/secrets/kubernetes.io/serviceaccount/token
    /// 5. Fallback to OS detection (Windows/macOS/Linux)
    ///
    /// FR-002: "Cloud HSM taking precedence over Kubernetes Secrets when both are available"
    /// - If running in Kubernetes on Azure/AWS/GCP, cloud provider detection takes precedence
    /// - Example: AKS on Azure returns BackendType.Azure, not BackendType.Kubernetes
    /// </remarks>
    Task<BackendType> DetectEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific cloud provider metadata service is reachable.
    /// </summary>
    /// <param name="providerType">The cloud provider to check (Azure/AWS/GCP).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if metadata service reachable, false otherwise.</returns>
    /// <remarks>
    /// Used for health checks and diagnostics. Timeout is 2 seconds (fast fail).
    /// </remarks>
    Task<bool> IsCloudProviderAvailableAsync(
        BackendType providerType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if running in a Kubernetes environment.
    /// </summary>
    /// <returns>True if Kubernetes service account token file exists, false otherwise.</returns>
    /// <remarks>
    /// Kubernetes detection is file-based: checks for /var/run/secrets/kubernetes.io/serviceaccount/token
    /// No network call required.
    /// </remarks>
    bool IsKubernetesEnvironment();

    /// <summary>
    /// Gets the detected OS platform (Windows, macOS, Linux).
    /// </summary>
    /// <returns>The OS platform.</returns>
    /// <remarks>
    /// Uses RuntimeInformation.IsOSPlatform() for platform detection.
    /// </remarks>
    OSPlatform GetOSPlatform();
}
```

---

## Backend Precedence Algorithm (FR-021)

```
Input: EnvironmentConfiguration config
Output: ICryptographicBackend selected

1. backends = config.BackendPrecedenceOrder  # Default: [CloudHSM, Kubernetes, OS, Software]
2. availableBackends = []

3. FOR EACH backendType IN backends:
4.   IF backendType NOT IN config.AllowedBackendTypes:
5.     CONTINUE  # Skip disallowed backends
6.
7.   backend = CreateBackend(backendType)
8.   healthCheck = AWAIT backend.HealthCheckAsync()
9.
10.  IF healthCheck.IsHealthy:
11.    IF ValidateBackendSecurity(backend, config):
12.      RETURN backend  # First available backend that passes validation
13.    END IF
14.  END IF
15. END FOR

16. IF config.FallbackPolicy.Enabled:
17.   # Fallback to software signing with warnings
18.   LogWarning("All HSM backends unavailable, falling back to software signing")
19.   RETURN SoftwareBackend()
20. ELSE:
21.   THROW BackendSelectionException("No suitable backend available")
22. END IF
```

### Example Precedence Order

**Production Environment** (config.EnvironmentType == Production):
```
BackendPrecedenceOrder: [Azure, Aws, Gcp, Kubernetes]
AllowedBackendTypes: [Azure, Aws, Gcp, Kubernetes]  # No OS, no Software
RequiredSecurityLevel: HSM
FallbackPolicy.Enabled: false  # Fail-closed
```

**Staging Environment** (config.EnvironmentType == Staging):
```
BackendPrecedenceOrder: [Azure, Aws, Gcp, Kubernetes, Os]
AllowedBackendTypes: [Azure, Aws, Gcp, Kubernetes, Os]
RequiredSecurityLevel: HSM (TPM acceptable)
FallbackPolicy.Enabled: true  # Allow fallback for resilience testing
```

**Development Environment** (config.EnvironmentType == Development):
```
BackendPrecedenceOrder: [Azure, Os, Software]  # Prefer cloud if available, fallback to local
AllowedBackendTypes: [Azure, Aws, Gcp, Os, Software]  # Allow all
RequiredSecurityLevel: Software (any level acceptable)
FallbackPolicy.Enabled: true
```

---

## Configuration Example

```json
{
  "Cryptography": {
    "Environment": {
      "Type": "Production",
      "RequiredSecurityLevel": "HSM",
      "AllowedBackends": ["Azure", "Aws", "Gcp", "Kubernetes"],
      "BackendPrecedence": ["Azure", "Aws", "Gcp", "Kubernetes"],
      "KeyRotation": {
        "AutomaticEnabled": true,
        "IntervalDays": 90
      },
      "FallbackPolicy": {
        "Enabled": false,
        "WarningLevel": "Error"
      },
      "Audit": {
        "Enabled": true,
        "RetentionDays": 365,
        "RedactSensitiveData": true
      }
    },
    "Backends": {
      "Azure": {
        "KeyVaultUrl": "https://sorcha-prod-kv.vault.azure.net/",
        "UseManagedIdentity": true
      },
      "Aws": {
        "Region": "us-east-1",
        "UseIamRole": true
      },
      "Gcp": {
        "ProjectId": "sorcha-prod",
        "LocationId": "global",
        "KeyRingId": "sorcha-keys",
        "UseWorkloadIdentity": true
      }
    }
  }
}
```

---

## Environment Detection Flow

```
┌──────────────────────────────┐
│  Start Environment Detection │
└──────────┬───────────────────┘
           │
           ▼
    ┌─────────────┐
    │ Query Azure │ (2s timeout)
    │    IMDS     │
    └──────┬──────┘
           │
      ┌────▼────┐
      │ Success?│──Yes──▶ Return BackendType.Azure
      └────┬────┘
           │ No
           ▼
    ┌─────────────┐
    │  Query AWS  │ (2s timeout)
    │   IMDSv2    │
    └──────┬──────┘
           │
      ┌────▼────┐
      │ Success?│──Yes──▶ Return BackendType.Aws
      └────┬────┘
           │ No
           ▼
    ┌─────────────┐
    │  Query GCP  │ (2s timeout)
    │  Metadata   │
    └──────┬──────┘
           │
      ┌────▼────┐
      │ Success?│──Yes──▶ Return BackendType.Gcp
      └────┬────┘
           │ No
           ▼
  ┌──────────────────┐
  │ Check Kubernetes │ (file exists check)
  │ Service Account  │
  └──────┬───────────┘
         │
    ┌────▼────┐
    │ Exists? │──Yes──▶ Return BackendType.Kubernetes
    └────┬────┘
         │ No
         ▼
  ┌──────────────┐
  │  Detect OS   │ (RuntimeInformation)
  │   Platform   │
  └──────┬───────┘
         │
         ▼
  Return BackendType.Os (Windows/macOS/Linux)
```

**Note**: Detection is cached for 5 minutes to avoid repeated metadata service calls.

---

## Security Validation Matrix

| Environment Type | Required Security Level | Allowed Backend Types | HSM Fallback Enabled |
|------------------|-------------------------|----------------------|----------------------|
| Production       | HSM                     | Azure, AWS, GCP, K8s | No (fail-closed)     |
| Staging          | HSM (TPM acceptable)    | Azure, AWS, GCP, K8s, OS | Yes (with warnings) |
| Development      | Software acceptable     | All                  | Yes                  |
| Local            | Software acceptable     | OS, Software         | Yes                  |

---

## Health Check Requirements (FR-016)

Each backend implementation MUST provide a health check that:

1. **Tests connectivity** to the backend service (Key Vault, KMS, etc.)
2. **Validates authentication** (managed identity token, IAM role, service account)
3. **Completes within 2 seconds** (timeout for fast-fail)
4. **Returns structured result** with status, message, and response time

**Health Check Example**:
```csharp
public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        // Test connectivity and authentication
        var testKeyName = "__health-check-key__";
        var exists = await CheckKeyExistsAsync(testKeyName, ct);

        stopwatch.Stop();
        return new HealthCheckResult(
            IsHealthy: true,
            Message: "Backend is healthy",
            ResponseTime: stopwatch.Elapsed,
            CheckedAt: DateTime.UtcNow);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        return new HealthCheckResult(
            IsHealthy: false,
            Message: $"Backend unhealthy: {ex.Message}",
            ResponseTime: stopwatch.Elapsed,
            CheckedAt: DateTime.UtcNow);
    }
}
```

---

## Startup Requirements (FR-016)

**Production environments MUST fail-fast during startup** if required cryptographic backend is unavailable:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register cryptography services
        services.AddCryptographicBackends(Configuration);
    }

    public async Task Configure(IApplicationBuilder app, IBackendSelector selector, EnvironmentConfiguration config)
    {
        // FR-016: Fail startup if production backend unavailable
        if (config.EnvironmentType == EnvironmentType.Production)
        {
            try
            {
                var backend = await selector.SelectBackendAsync(config);
                var health = await backend.HealthCheckAsync();

                if (!health.IsHealthy)
                {
                    throw new InvalidOperationException(
                        $"Production cryptographic backend unhealthy: {health.Message}. " +
                        "Application cannot start without HSM availability.");
                }
            }
            catch (BackendSelectionException ex)
            {
                throw new InvalidOperationException(
                    "Production cryptographic backend not available. " +
                    "Application cannot start without HSM availability.", ex);
            }
        }
    }
}
```

---

**Contract Version**: 1.0
**Last Updated**: 2025-12-23
**Implementation**: Phase 2 (Tasks)
