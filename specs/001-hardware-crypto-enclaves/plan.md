# Implementation Plan: Hardware Cryptographic Storage and Execution Enclaves

**Branch**: `001-hardware-crypto-enclaves` | **Date**: 2025-12-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-hardware-crypto-enclaves/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature implements a cryptographic backend abstraction layer that supports multiple secure key storage providers across cloud (Azure Key Vault, AWS KMS, GCP Cloud KMS), Kubernetes (Secrets with external KMS), OS-native (DPAPI, Keychain, Secret Service), and browser (Web Crypto API) environments. The system automatically detects the deployment environment and selects the most secure available backend, with cloud HSM taking precedence. All cryptographic operations (key generation, signing, encryption) execute within secure enclave boundaries, with keys never exposed to application memory in HSM-backed scenarios. The implementation extends the existing Sorcha.Cryptography library with provider-specific adapters and integrates with Sorcha.Wallet.Service for wallet key management.

## Technical Context

**Language/Version**: C# 13, .NET 10.0
**Primary Dependencies**:
- Azure.Security.KeyVault.Keys 4.6.0
- Azure.Identity 1.13.0
- AWSSDK.KeyManagementService 3.7.400
- Google.Cloud.Kms.V1 3.19.0
- KubernetesClient 15.0.1
- System.Security.Cryptography (built-in .NET 10)

**Storage**:
- Cloud HSM: Azure Key Vault, AWS KMS, GCP Cloud KMS
- Kubernetes: etcd-backed Secrets with optional external KMS encryption
- OS-native: Windows DPAPI (ProtectedData), macOS Keychain (Security.framework via P/Invoke), Linux Secret Service (DBus/libsecret)
- Browser: Web Crypto API with IndexedDB

**Testing**: xUnit 2.9.0, FluentAssertions 6.12.2, Moq 4.20.72, Testcontainers 3.10.0, NBomber 6.0.0

**Target Platform**:
- Server-side: Linux x64 (production), Windows x64 (development)
- Container: Docker/Kubernetes
- Browser: Modern browsers supporting Web Crypto API (Chrome, Firefox, Safari, Edge)

**Project Type**: Multi-project .NET solution extending existing Sorcha microservices architecture

**Performance Goals**:
- HSM signing operations: <500ms p95 latency
- Environment detection: <100ms on startup
- Key rotation: <5 seconds per key with zero downtime
- Automatic backend selection: <50ms decision time

**Constraints**:
- Zero private key exposure to application memory in HSM mode
- No stored credentials (use managed identities/service accounts only)
- Backward compatible with existing Sorcha.Cryptography API
- Support for offline development environments
- Atomic key rotation with rollback capability

**Scale/Scope**:
- Support 5 backend types (Azure, AWS, GCP, Kubernetes, OS, Browser)
- Handle 10,000+ concurrent signing operations
- Manage 100,000+ keys per deployment
- Support key rotation intervals from 1 day to 365 days

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Pre-Design)

### I. Microservices-First Architecture
✅ **PASS** - Extends existing Sorcha.Cryptography library (core/domain layer) and integrates with Sorcha.Wallet.Service. No new microservice required. Uses .NET Aspire orchestration for configuration.

### II. Security First
✅ **PASS** - Zero trust model with HSM-backed key storage in production. Keys never exposed to application memory. Managed identities eliminate credential storage. AES-256-GCM encryption for envelope encryption scenarios. Audit logging for all operations.

### III. API Documentation
✅ **PASS** - All new APIs will use .NET 10 built-in OpenAPI with Scalar.AspNetCore. XML documentation required for all public methods. OpenAPI examples for complex key migration scenarios.

### IV. Testing Requirements
✅ **PASS** - Target >85% coverage for new code. xUnit tests for all backend adapters. Integration tests with Testcontainers for cloud provider emulation. NBomber performance tests for HSM latency requirements.

### V. Code Quality
✅ **PASS** - C# 13, .NET 10 target framework. Async/await for all I/O operations. Dependency injection for backend selection. Nullable reference types enabled. No compiler warnings.

### VI. Blueprint Creation Standards
N/A - This feature does not involve blueprint creation.

### VII. Domain-Driven Design
✅ **PASS** - Rich domain models: CryptographicBackend, KeyHandle, EnvironmentConfiguration, CryptographicOperation, KeyMigrationRecord. Ubiquitous language established in spec. Aggregates protect key lifecycle invariants.

### VIII. Observability by Default
✅ **PASS** - OpenTelemetry telemetry for all cryptographic operations. Structured logging with automatic key redaction. Health check endpoints for backend availability. Metrics for operation latency and failure rates.

**Gate Result**: ✅ **ALL GATES PASSED** - No constitutional violations. Proceed to Phase 0.

---

### Post-Design Re-evaluation (After Phase 0 Research + Phase 1 Design)

**Date**: 2025-12-23
**Status**: ✅ **ALL GATES PASSED** (Verified after research.md, data-model.md, contracts/)

After completing detailed design artifacts, re-verified constitutional compliance:

**I. Microservices-First Architecture**
- ✅ Confirmed library-level implementation (`Sorcha.Cryptography` extension)
- ✅ No new microservices introduced
- ✅ Backend adapters follow provider pattern with DI registration
- ✅ Wallet Service integration via existing service reference

**II. Security First**
- ✅ Managed identity authentication confirmed (Azure DefaultAzureCredential, AWS IAM Roles, GCP Workload Identity)
- ✅ Zero credential storage - all SDKs use credential chains
- ✅ Key wrapping for migration uses RSA-OAEP (industry standard)
- ✅ Audit hooks in all `ICryptographicBackend` operations
- ✅ Key redaction service planned for sensitive data logging

**III. API Documentation**
- ✅ All contracts include comprehensive XML documentation (verified in contracts/)
- ✅ OpenAPI annotations will be added if REST endpoints exposed (currently library-only)
- ✅ quickstart.md provides usage examples

**IV. Testing Requirements**
- ✅ Test structure planned for >85% coverage
- ✅ Testcontainers identified for cloud provider integration tests
- ✅ NBomber identified for HSM latency performance tests
- ✅ Unit tests for all backend adapters planned

**V. Code Quality**
- ✅ All contracts use async/await patterns
- ✅ Nullable reference types implied in contract signatures
- ✅ DI patterns confirmed in quickstart.md examples
- ✅ C# 13/.NET 10 target confirmed in Technical Context

**VI. Blueprint Creation Standards**
- ✅ N/A - Not applicable to cryptography library

**VII. Domain-Driven Design**
- ✅ Rich domain models verified in data-model.md:
  - EnvironmentConfiguration (Aggregate Root)
  - CryptographicBackend (Entity)
  - KeyHandle (Entity, Aggregate Root)
  - KeyMigrationRecord (Entity)
  - CryptographicOperation (Value Object)
  - RotationPolicy (Value Object)
- ✅ Ubiquitous language confirmed (Backend, HSM, Enclave, Rotation, Migration)
- ✅ Aggregates protect invariants (e.g., KeyHandle version consistency)

**VIII. Observability by Default**
- ✅ `HealthCheckResult` type defined in contracts
- ✅ All backends implement `HealthCheckAsync()`
- ✅ Audit logging hooks in operation interfaces
- ✅ OpenTelemetry integration confirmed in research.md

**IX. Service Communication Standards** (Constitution Section: gRPC)
- ✅ N/A - This is a library extension, not a service
- ✅ Note: If Wallet Service exposes cryptographic operations via gRPC (future), that service will handle mTLS/protobuf per constitutional standards

**X. Dependency Management**
- ✅ All dependencies verified as official SDKs:
  - `Azure.Security.KeyVault.Keys 4.6.0` (Microsoft)
  - `AWSSDK.KeyManagementService 3.7.400` (Amazon)
  - `Google.Cloud.Kms.V1 3.19.0` (Google)
  - `KubernetesClient 15.0.1` (Kubernetes)
  - Built-in .NET 10 System.Security.Cryptography
- ✅ No unapproved third-party packages

**XI. License and Copyright**
- ✅ Will include MIT license headers per constitutional format in all new files

**Conclusion**: Design fully complies with all constitutional principles. No violations, concerns, or trade-offs requiring justification. Implementation can proceed.

## Project Structure

### Documentation (this feature)

```text
specs/001-hardware-crypto-enclaves/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0 output (to be generated)
├── data-model.md        # Phase 1 output (to be generated)
├── quickstart.md        # Phase 1 output (to be generated)
├── contracts/           # Phase 1 output (to be generated)
│   ├── ICryptographicBackend.cs
│   ├── IKeyStorage.cs
│   ├── IKeyRotationService.cs
│   └── backend-selection-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Common/Sorcha.Cryptography/
├── Abstractions/
│   ├── ICryptographicBackend.cs          # Backend provider interface
│   ├── IKeyStorage.cs                     # Key storage operations
│   ├── IKeyRotationService.cs             # Key rotation management
│   ├── IEnvironmentDetector.cs            # Environment detection
│   └── IBackendSelector.cs                # Backend selection strategy
├── Models/
│   ├── CryptographicBackend.cs            # Backend representation
│   ├── KeyHandle.cs                       # Key reference
│   ├── EnvironmentConfiguration.cs        # Environment settings
│   ├── CryptographicOperation.cs          # Operation tracking
│   ├── KeyMigrationRecord.cs              # Migration tracking
│   ├── BackendType.cs                     # Provider enumeration
│   ├── SecurityLevel.cs                   # HSM/TPM/Software classification
│   └── RotationPolicy.cs                  # Rotation configuration
├── Backends/
│   ├── Azure/
│   │   ├── AzureKeyVaultBackend.cs
│   │   ├── AzureManagedIdentityAuth.cs
│   │   └── AzureKeyVaultKeyStorage.cs
│   ├── Aws/
│   │   ├── AwsKmsBackend.cs
│   │   ├── AwsIamRoleAuth.cs
│   │   └── AwsKmsKeyStorage.cs
│   ├── Gcp/
│   │   ├── GcpKmsBackend.cs
│   │   ├── GcpServiceAccountAuth.cs
│   │   └── GcpKmsKeyStorage.cs
│   ├── Kubernetes/
│   │   ├── KubernetesSecretsBackend.cs
│   │   ├── KubernetesServiceAccountAuth.cs
│   │   └── KubernetesSecretStorage.cs
│   ├── Os/
│   │   ├── WindowsDpapiBackend.cs
│   │   ├── MacOsKeychainBackend.cs
│   │   ├── LinuxSecretServiceBackend.cs
│   │   └── OsBackendFactory.cs
│   └── Browser/
│       ├── WebCryptoBackend.cs            # JavaScript interop
│       └── IndexedDbKeyStorage.cs
├── Services/
│   ├── BackendSelectorService.cs          # Implements backend priority logic (FR-021)
│   ├── EnvironmentDetectorService.cs      # Auto-detects deployment environment (FR-002)
│   ├── KeyRotationService.cs              # Automatic and manual rotation (FR-017, FR-022)
│   ├── KeyMigrationService.cs             # HSM-to-HSM transfer (FR-012)
│   └── KeyRedactionService.cs             # Sensitive data redaction (FR-013)
├── Configuration/
│   ├── CryptographyOptions.cs             # Bind from appsettings
│   ├── BackendConfiguration.cs            # Provider-specific settings
│   └── RotationPolicyConfiguration.cs     # Per-environment rotation settings
├── HealthChecks/
│   └── CryptographicBackendHealthCheck.cs # Backend availability check (FR-016)
└── Extensions/
    └── ServiceCollectionExtensions.cs     # DI registration

src/Services/Sorcha.Wallet.Service/
├── Integration/
│   ├── CryptographicBackendIntegration.cs # Integrates with Sorcha.Cryptography
│   └── WalletKeyManager.cs                # Uses ICryptographicBackend for wallet keys
└── appsettings.json                       # Backend configuration

tests/Sorcha.Cryptography.Tests/
├── Unit/
│   ├── BackendSelectorServiceTests.cs
│   ├── EnvironmentDetectorServiceTests.cs
│   ├── KeyRotationServiceTests.cs
│   ├── Azure/AzureKeyVaultBackendTests.cs
│   ├── Aws/AwsKmsBackendTests.cs
│   ├── Gcp/GcpKmsBackendTests.cs
│   ├── Kubernetes/KubernetesSecretsBackendTests.cs
│   └── Os/OsBackendTests.cs
├── Integration/
│   ├── AzureKeyVaultIntegrationTests.cs   # Uses Testcontainers or Azure emulator
│   ├── AwsKmsIntegrationTests.cs          # Uses LocalStack via Testcontainers
│   ├── KubernetesIntegrationTests.cs      # Uses KinD via Testcontainers
│   └── EndToEndBackendSelectionTests.cs
└── Performance/
    ├── HsmSigningLatencyTests.cs          # NBomber load tests
    └── RotationPerformanceTests.cs

src/Apps/Sorcha.AppHost/
└── Program.cs                             # .NET Aspire orchestration with cryptography configuration
```

**Structure Decision**: Extends existing Sorcha.Cryptography library (single-project within monorepo) with backend-specific adapters following provider/adapter pattern. New code resides in `src/Common/Sorcha.Cryptography/` and integrates with `Sorcha.Wallet.Service`. No new microservice required as this is infrastructure-level functionality.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

N/A - No constitutional violations detected.

## Phase 0: Research & Technology Decisions

*This section documents architectural decisions and technology choices*

### Research Tasks

The following areas require investigation before detailed design:

1. **Azure Key Vault HSM Integration Patterns**
   - How to enforce non-exportable key generation
   - Best practices for managed identity authentication
   - Audit log integration with Azure Monitor
   - Fallback behavior during Key Vault outages

2. **AWS KMS Best Practices**
   - IAM role configuration for EC2/ECS instances
   - Customer-managed keys (CMK) vs AWS-managed keys
   - CloudTrail audit log parsing
   - Multi-region key replication

3. **GCP Cloud KMS Integration**
   - Service account authentication patterns
   - HSM vs software-backed keys
   - Cloud Logging audit trail integration
   - Key rotation automation

4. **Kubernetes Secret Encryption**
   - External KMS provider configuration (Azure/AWS/GCP)
   - Sealed Secrets vs native encryption at rest
   - Service account RBAC for secret access
   - Pod security policies for key protection

5. **OS-Native Secure Storage**
   - Windows DPAPI: User vs Machine scope
   - macOS Keychain: P/Invoke vs Process.Start("security")
   - Linux Secret Service: D-Bus protocol vs libsecret wrapper
   - Docker volume persistence for OS-backed keys

6. **Web Crypto API Limitations**
   - Non-extractable key constraints
   - IndexedDB encryption patterns
   - HTTPS-only requirements
   - Browser compatibility matrix

7. **Environment Detection Strategies**
   - Azure: Instance Metadata Service (IMDS) endpoint
   - AWS: IMDSv2 with session tokens
   - GCP: Metadata server endpoints
   - Kubernetes: Downward API and service account presence
   - Fallback detection order

8. **Key Rotation Without Downtime**
   - Key versioning schemes
   - Dual-signing during rotation window
   - Old key retention for verification
   - Rollback mechanisms

9. **HSM-to-HSM Key Migration**
   - Asymmetric key wrapping techniques
   - Provider-specific export/import limitations
   - Audit trail requirements
   - Migration verification procedures

10. **Managed Identity Authentication**
    - Azure Managed Identity token acquisition
    - AWS IAM Roles for EC2/ECS task roles
    - GCP Service Account impersonation
    - Kubernetes service account token projection

### Research Methodology

For each research task:
1. Review official SDK documentation
2. Examine reference implementations
3. Identify security pitfalls and best practices
4. Document decision rationale in research.md
5. Flag any trade-offs or constraints

