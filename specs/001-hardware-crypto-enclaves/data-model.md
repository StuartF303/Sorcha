# Data Model: Hardware Cryptographic Storage and Execution Enclaves

**Feature**: 001-hardware-crypto-enclaves
**Date**: 2025-12-23
**Status**: Design Phase

This document defines the domain models, value objects, and data structures for the cryptographic backend abstraction layer.

---

## Entity Relationship Diagram

```
┌─────────────────────────────┐
│  EnvironmentConfiguration   │
│  (Aggregate Root)            │
├─────────────────────────────┤
│ + EnvironmentType           │
│ + RequiredSecurityLevel     │
│ + BackendPrecedence[]       │
│ + RotationInterval          │
│ + FallbackPolicy            │
└──────────┬──────────────────┘
           │ 1
           │ configures
           │ *
┌──────────▼──────────────────┐
│  CryptographicBackend       │
│  (Entity)                    │
├─────────────────────────────┤
│ + BackendId                 │
│ + ProviderType              │
│ + SecurityLevel             │
│ + AvailabilityStatus        │
│ + Configuration             │
└──────────┬──────────────────┘
           │ 1
           │ manages
           │ *
┌──────────▼──────────────────┐
│  KeyHandle                   │
│  (Entity, Aggregate Root)    │
├─────────────────────────────┤
│ + KeyId                     │
│ + Version                   │
│ + Algorithm                 │
│ + CreatedAt                 │
│ + LastRotation At            │
│ + NextRotationDate          │
│ + ProviderUri               │
│ + SecurityProperties        │
└──────────┬──────────────────┘
           │ 1
           │ tracks
           │ *
┌──────────▼──────────────────┐
│  CryptographicOperation     │
│  (Value Object)              │
├─────────────────────────────┤
│ + OperationId               │
│ + OperationType             │
│ + KeyHandleReference        │
│ + Timestamp                 │
│ + RequestingPrincipal       │
│ + Status                    │
│ + AuditTrailReference       │
└─────────────────────────────┘

┌─────────────────────────────┐
│  KeyMigrationRecord         │
│  (Entity)                    │
├─────────────────────────────┤
│ + MigrationId               │
│ + SourceProvider            │
│ + DestinationProvider       │
│ + MigrationTimestamp        │
│ + SourceKeyId               │
│ + DestinationKeyId          │
│ + IntegrityHash             │
│ + AdministrativeApproval    │
│ + Status                    │
└─────────────────────────────┘
```

---

## 1. EnvironmentConfiguration (Aggregate Root)

Defines the cryptographic requirements and policies for different deployment environments.

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| EnvironmentType | `EnvironmentType` (enum) | Yes | production, staging, development, local | Must be one of defined values |
| RequiredSecurityLevel | `SecurityLevel` (enum) | Yes | HSM, TPM, software | production MUST be HSM |
| AllowedBackendTypes | `List<BackendType>` | Yes | Permitted backend providers | Not empty, valid BackendType values |
| BackendPrecedenceOrder | `List<BackendType>` | Yes | Priority order for backend selection (FR-021) | Default: [CloudHSM, Kubernetes, OS, Software] |
| KeyRotationInterval | `TimeSpan` | No | Automatic rotation interval (FR-022) | Min: 1 day, Max: 365 days. Default: 90 days |
| FallbackPolicy | `FallbackPolicyConfiguration` | Yes | HSM fallback behavior (FR-024) | production default: disabled |
| AuditRequirements | `AuditConfiguration` | Yes | Logging and audit settings | - |
| CreatedAt | `DateTime` | Yes | Configuration creation timestamp | UTC |
| UpdatedAt | `DateTime` | Yes | Last modification timestamp | UTC |
| CreatedBy | `string` | Yes | User/service principal that created config | Not null or empty |

### Enum: EnvironmentType

```csharp
public enum EnvironmentType
{
    Production,   // Requires HSM, no fallback
    Staging,      // HSM recommended, fallback allowed
    Development,  // OS-native storage allowed
    Local         // Software-only with warnings
}
```

### Nested Type: FallbackPolicyConfiguration

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Enabled | `bool` | Yes | Whether HSM fallback to software signing is allowed |
| WarningLevel | `LogLevel` | Yes | Log level for fallback activation (Warning/Error) |
| MaxFallbackDuration | `TimeSpan?` | No | Maximum duration fallback can be active. Null = unlimited |

### Nested Type: AuditConfiguration

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| EnableAuditLogging | `bool` | Yes | Enable audit trail for all operations (FR-010) |
| RetentionPeriod | `TimeSpan` | Yes | Audit log retention period. Min: 90 days |
| RedactSensitiveData | `bool` | Yes | Automatically redact key material from logs (FR-013) |

### Business Rules

1. **Production HSM requirement** (FR-014): If `EnvironmentType == Production`, then `RequiredSecurityLevel` MUST be `HSM`
2. **Fallback policy** (FR-024): If `EnvironmentType == Production`, then `FallbackPolicy.Enabled` SHOULD default to `false`
3. **Backend precedence** (FR-021): Default order is [Azure/AWS/GCP HSM, Kubernetes, OS, Software], configurable per environment
4. **Rotation interval**: Must be between 1 day and 365 days. Recommended: 90 days for production, 365 days for development

### Invariants

- `BackendPrecedenceOrder` must contain at least one `BackendType`
- `KeyRotationInterval` must not be negative
- `AuditRequirements.RetentionPeriod` >= 90 days for production environments

---

## 2. CryptographicBackend (Entity)

Represents the storage and execution environment for cryptographic operations.

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| BackendId | `string` | Yes | Unique identifier (GUID or provider-specific ID) | Not null or empty, valid GUID format |
| ProviderType | `BackendType` (enum) | Yes | Azure, AWS, GCP, Kubernetes, OS, Browser | Must be one of defined values |
| SecurityLevel | `SecurityLevel` (enum) | Yes | HSM, TPM, software | - |
| AvailabilityStatus | `AvailabilityStatus` (enum) | Yes | Available, Degraded, Unavailable | - |
| Configuration | `Dictionary<string, string>` | No | Provider-specific configuration (e.g., Key Vault URL) | - |
| LastHealthCheck | `DateTime` | No | Last health check timestamp | UTC |
| HealthCheckStatus | `HealthCheckResult` | No | Result of last health check | - |
| ConnectionString | `string?` | No | Provider connection details (e.g., Key Vault URI) | Valid URI for cloud providers |
| AuthenticationMethod | `AuthenticationType` (enum) | Yes | ManagedIdentity, ServiceAccount, ProcessIdentity | Per FR-023 |

### Enum: BackendType

```csharp
public enum BackendType
{
    Azure,       // Azure Key Vault HSM
    Aws,         // AWS KMS
    Gcp,         // GCP Cloud KMS
    Kubernetes,  // K8s Secrets with external KMS
    Os,          // Windows DPAPI, macOS Keychain, Linux Secret Service
    Browser,     // Web Crypto API
    Software     // Software-only (development fallback)
}
```

### Enum: SecurityLevel

```csharp
public enum SecurityLevel
{
    HSM,      // Hardware Security Module (FIPS 140-2 Level 3+)
    TPM,      // Trusted Platform Module
    Software  // Software-based encryption (development only)
}
```

### Enum: AvailabilityStatus

```csharp
public enum AvailabilityStatus
{
    Available,     // Backend is healthy and operational
    Degraded,      // Backend is operational but experiencing issues
    Unavailable    // Backend is not reachable or failed health check
}
```

### Enum: AuthenticationType

```csharp
public enum AuthenticationType
{
    ManagedIdentity,    // Azure Managed Identity, AWS IAM Role
    ServiceAccount,     // Kubernetes service account
    ProcessIdentity,    // OS process identity (local development)
    Certificate,        // Certificate-based authentication
    ApiKey              // API key (discouraged)
}
```

### Business Rules

1. **Security level enforcement** (FR-001, FR-003): If `ProviderType == Azure | AWS | GCP`, then `SecurityLevel` MUST be `HSM`
2. **Availability monitoring** (FR-016): `AvailabilityStatus` MUST be updated by health check service every 60 seconds
3. **Authentication method** (FR-023): Cloud providers (Azure/AWS/GCP) MUST use `ManagedIdentity`, Kubernetes MUST use `ServiceAccount`

### Invariants

- `BackendId` must be unique across all backends
- If `SecurityLevel == HSM`, then `ProviderType` must be `Azure | AWS | GCP | Kubernetes` (with external KMS)
- `LastHealthCheck` must not be null if `AvailabilityStatus != Available`

---

## 3. KeyHandle (Entity, Aggregate Root)

Abstract reference to a cryptographic key stored in a secure enclave. This is the central entity for key lifecycle management.

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| KeyId | `string` | Yes | Unique key identifier (GUID) | Not null or empty, valid GUID format |
| Version | `int` | Yes | Key version number (for rotation) | >= 1, incremented on rotation |
| Algorithm | `CryptoAlgorithm` (enum) | Yes | RSA_4096, ECDSA_P256, ED25519 | Must be supported by backend |
| CreationTimestamp | `DateTime` | Yes | When the key was created | UTC, immutable |
| LastRotationTimestamp | `DateTime?` | No | Last rotation date | UTC, null if never rotated |
| NextScheduledRotationDate | `DateTime?` | No | Next scheduled automatic rotation (FR-022) | UTC, computed from rotation policy |
| ProviderSpecificUri | `string` | Yes | Backend-specific key URI (e.g., Azure Key Vault key ID) | Not null or empty, valid URI |
| SecurityProperties | `KeySecurityProperties` | Yes | Exportable/non-exportable, HSM-backed | - |
| BackendType | `BackendType` (enum) | Yes | Which backend stores this key | - |
| Status | `KeyStatus` (enum) | Yes | Active, Pending (rotation), Deprecated, Revoked | Default: Active |
| OwnerPrincipal | `string` | Yes | User/service that owns the key | Not null or empty |

### Enum: CryptoAlgorithm

```csharp
public enum CryptoAlgorithm
{
    RSA_4096,      // RSA with 4096-bit key
    RSA_2048,      // RSA with 2048-bit key
    ECDSA_P256,    // NIST P-256 curve
    ECDSA_P384,    // NIST P-384 curve
    ED25519        // EdDSA with Curve25519
}
```

### Enum: KeyStatus

```csharp
public enum KeyStatus
{
    Active,        // Currently used for signing operations
    Pending,       // Rotation in progress, not yet active
    Deprecated,    // Old version, retained for verification only
    Revoked        // Key compromised or explicitly revoked
}
```

### Nested Type: KeySecurityProperties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| IsExportable | `bool` | Yes | Whether key can be exported from backend |
| IsHsmBacked | `bool` | Yes | Whether key is stored in HSM (FR-003) |
| AllowedOperations | `List<KeyOperation>` (enum) | Yes | Sign, Verify, Encrypt, Decrypt, Wrap, Unwrap |
| MinimumRotationInterval | `TimeSpan` | No | Minimum time before rotation allowed |

### Enum: KeyOperation

```csharp
public enum KeyOperation
{
    Sign,
    Verify,
    Encrypt,
    Decrypt,
    WrapKey,    // For key migration (FR-012)
    UnwrapKey
}
```

### Business Rules

1. **HSM non-exportable** (FR-003, FR-004): If `SecurityProperties.IsHsmBacked == true`, then `SecurityProperties.IsExportable` MUST be `false`
2. **Rotation scheduling** (FR-022): `NextScheduledRotationDate` = `LastRotationTimestamp` + `EnvironmentConfiguration.KeyRotationInterval` (if automatic rotation enabled)
3. **Version increment** (FR-017): On rotation, create new KeyHandle with `Version = previousVersion + 1`, set previous key `Status = Deprecated`
4. **Key lifecycle**: Active → (rotation) → Pending → (validation) → Active (old becomes Deprecated)

### Invariants

- `Version` must be monotonically increasing for same `KeyId`
- If `Status == Deprecated`, then `LastRotationTimestamp` must not be null
- `ProviderSpecificUri` format must match `BackendType` (e.g., Azure URI format for Azure backend)

---

## 4. CryptographicOperation (Value Object)

Represents a signing, encryption, or key generation operation. Used for audit logging and telemetry.

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| OperationId | `Guid` | Yes | Unique operation identifier | Generated on creation |
| OperationType | `OperationType` (enum) | Yes | KeyGeneration, Sign, Verify, Encrypt, Decrypt | - |
| KeyHandleReference | `string` | Yes | Reference to KeyHandle.KeyId | Not null or empty |
| Timestamp | `DateTime` | Yes | Operation execution time | UTC |
| RequestingPrincipal | `string` | Yes | User/service that initiated operation | Not null or empty |
| Status | `OperationStatus` (enum) | Yes | Success, Failed, Pending | - |
| ErrorMessage | `string?` | No | Error details if status == Failed | - |
| DurationMs | `long` | No | Operation duration in milliseconds | >= 0 |
| AuditTrailReference | `string?` | No | Provider-specific audit log reference (e.g., Azure Monitor log ID) | - |
| BackendType | `BackendType` (enum) | Yes | Backend where operation executed | - |

### Enum: OperationType

```csharp
public enum OperationType
{
    KeyGeneration,  // Creating new key
    Sign,           // Signing operation
    Verify,         // Signature verification
    Encrypt,        // Encryption operation
    Decrypt,        // Decryption operation
    Rotate,         // Key rotation
    Migrate,        // Key migration between backends
    HealthCheck     // Backend availability check
}
```

### Enum: OperationStatus

```csharp
public enum OperationStatus
{
    Success,    // Operation completed successfully
    Failed,     // Operation failed with error
    Pending     // Operation in progress (async)
}
```

### Business Rules

1. **Audit logging** (FR-010): All operations with `OperationType != HealthCheck` MUST have `AuditTrailReference` populated
2. **Performance monitoring** (NFR-007): If `OperationType == Sign` and `DurationMs > 500`, emit warning telemetry
3. **Sensitive data redaction** (FR-013): `ErrorMessage` must never contain key material, only key identifiers

### Invariants

- `Timestamp` must be <= current time (cannot log future operations)
- If `Status == Failed`, then `ErrorMessage` must not be null
- `DurationMs` must be >= 0

---

## 5. KeyMigrationRecord (Entity)

Tracks secure key transfer between providers (FR-012). Used for audit trail and verification.

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| MigrationId | `Guid` | Yes | Unique migration identifier | Generated on creation |
| SourceProvider | `BackendType` (enum) | Yes | Provider where key originated | - |
| DestinationProvider | `BackendType` (enum) | Yes | Provider where key will be imported | - |
| MigrationTimestamp | `DateTime` | Yes | When migration was initiated | UTC |
| CompletedTimestamp | `DateTime?` | No | When migration completed | UTC, null if in progress |
| SourceKeyId | `string` | Yes | Original KeyHandle.KeyId | Not null or empty |
| DestinationKeyId | `string?` | No | New KeyHandle.KeyId in destination | Populated on completion |
| IntegrityHash | `string` | Yes | SHA-256 hash of wrapped key for integrity verification | 64 hex characters |
| CryptographicProof | `byte[]?` | No | Test signature for verification | - |
| AdministrativeApproval | `ApprovalRecord` | Yes | Who approved the migration | - |
| Status | `MigrationStatus` (enum) | Yes | InProgress, Completed, Failed, RolledBack | Default: InProgress |
| ErrorMessage | `string?` | No | Error details if status == Failed | - |

### Enum: MigrationStatus

```csharp
public enum MigrationStatus
{
    InProgress,   // Migration initiated, not yet complete
    Completed,    // Migration successful, keys verified
    Failed,       // Migration failed, source key retained
    RolledBack    // Migration rolled back, destination key deleted
}
```

### Nested Type: ApprovalRecord

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| ApprovedBy | `string` | Yes | User principal who approved migration |
| ApprovedAt | `DateTime` | Yes | Approval timestamp (UTC) |
| ApprovalJustification | `string?` | No | Reason for migration |

### Business Rules

1. **Cryptographic verification** (FR-012): Before setting `Status = Completed`, system MUST verify `CryptographicProof` (test signatures from both keys must match)
2. **Integrity hash**: `IntegrityHash` = SHA256(wrapped_key_bytes). Must match after transfer to ensure no corruption
3. **Source key retention**: Original key in `SourceProvider` must be retained for 30 days minimum after `CompletedTimestamp` before deletion
4. **Audit trail** (FR-012): All migration operations logged with full details (source, destination, approval, outcome)

### Invariants

- `SourceProvider` != `DestinationProvider` (cannot migrate to same provider)
- If `Status == Completed`, then `DestinationKeyId` and `CompletedTimestamp` must not be null
- If `Status == Failed`, then `ErrorMessage` must not be null
- `CompletedTimestamp` must be >= `MigrationTimestamp`

---

## 6. RotationPolicy (Value Object)

Configuration for automatic and manual key rotation (FR-017, FR-022).

### Properties

| Property | Type | Required | Description | Validation Rules |
|----------|------|----------|-------------|------------------|
| AutomaticRotationEnabled | `bool` | Yes | Enable time-based automatic rotation | - |
| RotationInterval | `TimeSpan` | No | Time between rotations (if automatic enabled) | Min: 1 day, Max: 365 days |
| RetainDeprecatedVersions | `bool` | Yes | Keep old key versions for verification | Recommended: true |
| DeprecatedVersionRetentionPeriod | `TimeSpan` | No | How long to keep deprecated keys | Min: 90 days (for audit) |
| ManualRotationAllowed | `bool` | Yes | Allow immediate admin-triggered rotation (FR-017) | Recommended: true |
| ValidationPeriod | `TimeSpan` | No | Dual-signing period during rotation | Default: 24 hours |

### Business Rules

1. **Automatic rotation** (FR-022): If `AutomaticRotationEnabled == true`, system monitors key age and initiates rotation when `KeyHandle.CreationTimestamp + RotationInterval <= DateTime.UtcNow`
2. **Manual override** (FR-017): If `ManualRotationAllowed == true`, administrators can trigger immediate rotation via API endpoint
3. **Retention policy**: Deprecated keys retained for `DeprecatedVersionRetentionPeriod` minimum, then eligible for deletion

### Invariants

- If `AutomaticRotationEnabled == true`, then `RotationInterval` must not be null
- `RotationInterval` must be between 1 day and 365 days
- `DeprecatedVersionRetentionPeriod` >= 90 days (for audit compliance)

---

## Domain Services

### BackendSelectorService

Implements FR-021 backend selection priority logic.

**Responsibilities**:
- Evaluate available backends based on `EnvironmentConfiguration.BackendPrecedenceOrder`
- Check backend availability via health checks
- Return highest-priority available backend

**Key Method**:
```csharp
Task<CryptographicBackend> SelectBackendAsync(EnvironmentConfiguration config, CancellationToken ct);
```

### KeyRotationService

Implements FR-017, FR-022 automatic and manual key rotation.

**Responsibilities**:
- Monitor key age and trigger automatic rotation based on `RotationPolicy`
- Handle manual rotation requests from administrators
- Manage key versioning and dual-signing during rotation window
- Rollback on validation failure

**Key Methods**:
```csharp
Task<KeyHandle> InitiateAutomaticRotationAsync(string keyId, CancellationToken ct);
Task<KeyHandle> InitiateManualRotationAsync(string keyId, string requestingPrincipal, CancellationToken ct);
Task RollbackRotationAsync(string keyId, string reason, CancellationToken ct);
```

### KeyMigrationService

Implements FR-012 HSM-to-HSM key migration with asymmetric key wrapping.

**Responsibilities**:
- Export key from source HSM (wrapped with ephemeral RSA key)
- Transfer wrapped key securely
- Import key to destination HSM
- Verify cryptographic integrity (test signatures)
- Maintain audit trail

**Key Methods**:
```csharp
Task<KeyMigrationRecord> MigrateKeyAsync(string keyId, BackendType source, BackendType destination, ApprovalRecord approval, CancellationToken ct);
Task<bool> VerifyMigrationIntegrityAsync(KeyMigrationRecord migration, CancellationToken ct);
```

### EnvironmentDetectorService

Implements FR-002 automatic environment detection.

**Responsibilities**:
- Query cloud metadata services (Azure IMDS, AWS IMDSv2, GCP metadata server)
- Detect Kubernetes environment via service account presence
- Fallback to OS detection
- Cache detection result for performance

**Key Method**:
```csharp
Task<BackendType> DetectEnvironmentAsync(CancellationToken ct);
```

---

## Data Validation Summary

| Entity | Primary Key | Unique Constraints | Required Fields | Immutable Fields |
|--------|-------------|-------------------|-----------------|------------------|
| EnvironmentConfiguration | EnvironmentType | EnvironmentType | All except KeyRotationInterval | CreatedAt, CreatedBy |
| CryptographicBackend | BackendId | BackendId | All except optional Configuration | BackendId, ProviderType |
| KeyHandle | KeyId + Version | (KeyId, Version) | All except rotation timestamps | KeyId, Version, CreationTimestamp, Algorithm |
| CryptographicOperation | OperationId | OperationId | All except optional fields | OperationId, Timestamp, OperationType, KeyHandleReference |
| KeyMigrationRecord | MigrationId | MigrationId | All except completion fields | MigrationId, SourceProvider, DestinationProvider, SourceKeyId, MigrationTimestamp |

---

**Data Model Version**: 1.0
**Last Updated**: 2025-12-23
**Next Phase**: API Contracts Generation
