# Research: Hardware Cryptographic Storage and Execution Enclaves

**Feature**: 001-hardware-crypto-enclaves
**Date**: 2025-12-23
**Status**: Completed

This document consolidates research findings for implementing multi-provider cryptographic backend support with HSM integration, environment detection, and automatic backend selection.

---

## 1. Azure Key Vault HSM Integration Patterns

### Decision: Use Azure.Identity DefaultAzureCredential with Key Vault Keys SDK

**Rationale**:
- `DefaultAzureCredential` automatically tries multiple authentication methods in order: environment variables, managed identity, Visual Studio, Azure CLI, etc.
- Eliminates need for explicit credential management
- Works seamlessly in local development (Azure CLI) and production (Managed Identity)
- `Azure.Security.KeyVault.Keys` SDK v4.6.0 has built-in support for HSM-backed keys

**Implementation approach**:
```csharp
var credential = new DefaultAzureCredential();
var client = new KeyClient(new Uri(keyVaultUrl), credential);
var createKeyOptions = new CreateRsaKeyOptions(keyName)
{
    KeySize = 4096,
    HardwareProtected = true  // Enforces HSM storage
};
KeyVaultKey key = await client.CreateRsaKeyAsync(createKeyOptions);
```

**Non-exportable key enforcement**:
- When `HardwareProtected = true`, keys are created in HSM and are non-exportable by design
- All cryptographic operations (`Sign`, `Encrypt`, `Decrypt`) execute within Key Vault HSM boundary
- Use `KeyOperation.Sign` and `KeyOperation.Verify` operations instead of retrieving key material

**Audit logging**:
- Azure Monitor automatically captures all Key Vault operations
- Use `Azure.Monitor.Query` SDK to retrieve audit logs
- Filter for `KeyVault/keys/sign` and `KeyVault/keys/create` operations
- Logs include operation name, identity (Managed Identity object ID), timestamp, and result status

**Fallback behavior during outages**:
- Implement exponential backoff retry policy using `Azure.Core.RetryOptions`
- Default: 3 retries with 0.8s delay, exponential backoff up to 60s
- If fallback is enabled (per-environment config), queue operations and fallback to software signing after retry exhaustion
- Emit high-priority telemetry alerts on fallback activation

**Alternatives considered**:
- Service Principal with Client Secret: Rejected due to credential storage requirements
- Certificate-based authentication: Added complexity without significant benefit when Managed Identity available

---

## 2. AWS KMS Best Practices

### Decision: Use AWSSDK.KeyManagementService with IMDSv2 for IAM Role credentials

**Rationale**:
- IAM Roles for EC2/ECS provide automatic credential rotation (every 5 minutes)
- IMDSv2 (Instance Metadata Service v2) uses session tokens to prevent SSRF attacks
- Customer Managed Keys (CMK) provide full control over key lifecycle, rotation, and audit
- CloudTrail provides comprehensive audit trail without additional configuration

**Implementation approach**:
```csharp
var kmsClient = new AmazonKeyManagementServiceClient(RegionEndpoint.USEast1);
var createKeyRequest = new CreateKeyRequest
{
    KeyUsage = KeyUsageType.SIGN_VERIFY,
    Origin = OriginType.AWS_KMS,
    CustomerMasterKeySpec = CustomerMasterKeySpec.RSA_4096
};
var response = await kmsClient.CreateKeyAsync(createKeyRequest);
```

**IAM Role configuration**:
- Attach IAM policy allowing `kms:CreateKey`, `kms:Sign`, `kms:GetPublicKey`, `kms:Describe*`
- Deny `kms:GetKeyRotationStatus` and `kms:ExportKey` if not needed
- Use tag-based access control for environment separation (e.g., `Environment=Production`)

**Customer-Managed Keys (CMK) vs AWS-Managed**:
- **Decision: Use CMK**
- CMK provides explicit key rotation schedules (365 days recommended)
- CMK allows cross-account access for key migration scenarios
- AWS-managed keys have limited configurability and rotation is opaque

**CloudTrail audit logging**:
- CloudTrail automatically logs all KMS API calls
- Use `LookupEvents` API to query `Sign`, `CreateKey`, `ScheduleKeyDeletion` operations
- Filter by `username` (IAM role session name) and `eventName`

**Multi-region key replication**:
- Use KMS multi-region keys for disaster recovery
- Primary key in us-east-1, replica keys in us-west-2, eu-west-1
- Replication lag typically <1 second
- All replicas share same key material but have independent key policies

**Alternatives considered**:
- AWS Secrets Manager: Designed for secret storage, not cryptographic operations
- AWS CloudHSM: Requires dedicated HSM cluster, higher cost, more operational overhead

---

## 3. GCP Cloud KMS Integration

### Decision: Use Google.Cloud.Kms.V1 with Workload Identity for service account authentication

**Rationale**:
- Workload Identity Federation (for GKE) eliminates need for service account key files
- Cloud KMS HSM keys provide FIPS 140-2 Level 3 certified security
- Cloud Logging provides detailed audit trails without configuration
- Automatic key rotation scheduling built into Cloud KMS

**Implementation approach**:
```csharp
var client = KeyManagementServiceClient.Create();
var keyRingName = new KeyRingName(projectId, locationId, keyRingId);
var createRequest = new CreateCryptoKeyRequest
{
    Parent = keyRingName.ToString(),
    CryptoKeyId = keyId,
    CryptoKey = new CryptoKey
    {
        Purpose = CryptoKey.Types.CryptoKeyPurpose.AsymmetricSign,
        VersionTemplate = new CryptoKeyVersionTemplate
        {
            Algorithm = CryptoKeyVersion.Types.CryptoKeyVersionAlgorithm.RsaSignPkcs12048Sha256,
            ProtectionLevel = ProtectionLevel.Hsm
        }
    }
};
var key = await client.CreateCryptoKeyAsync(createRequest);
```

**Service account authentication**:
- Use Application Default Credentials (ADC) which automatically detect environment
- In GKE: Workload Identity maps Kubernetes service account to GCP service account
- Locally: `gcloud auth application-default login` for development
- Grant `roles/cloudkms.cryptoKeyEncrypterDecrypter` IAM role

**HSM vs software-backed keys**:
- **Decision: HSM for production, software for development**
- HSM: Set `ProtectionLevel = ProtectionLevel.Hsm`
- Software: Set `ProtectionLevel = ProtectionLevel.Software`
- HSM keys cost more ($1/key-version/month) but provide hardware security guarantees

**Cloud Logging audit trail**:
- All KMS operations automatically logged to Cloud Logging
- Use Advanced Log Filters: `resource.type="cloudkms_cryptokey" AND protoPayload.methodName="Sign"`
- Export logs to BigQuery for long-term retention and analysis

**Key rotation automation**:
- Cloud KMS supports automatic key rotation (90 days recommended)
- Set `rotationPeriod = "7776000s"` (90 days) and `nextRotationTime`
- Old key versions remain active for verification
- Use `setIamPolicy` to restrict access to old versions if needed

**Alternatives considered**:
- GCP Confidential Computing: Requires specific VM types, not universally available
- Google Cloud HSM (legacy): Deprecated in favor of Cloud KMS HSM

---

## 4. Kubernetes Secret Encryption

### Decision: Use Kubernetes Secrets with external KMS provider (cloud provider's KMS for envelope encryption)

**Rationale**:
- Kubernetes native encryption-at-rest (etcd) uses envelope encryption with external KMS
- Integrates with Azure Key Vault, AWS KMS, or GCP Cloud KMS as Key Encryption Key (KEK) provider
- Secrets stored in etcd are encrypted using AES-256-GCM with KEK managed by cloud HSM
- RBAC provides fine-grained access control at pod/service account level

**Implementation approach**:
```yaml
# EncryptionConfiguration for kube-apiserver
apiVersion: apiserver.config.k8s.io/v1
kind: EncryptionConfiguration
resources:
  - resources:
      - secrets
    providers:
      - kms:
          name: azure-kms-provider  # or aws-kms-provider, gcp-kms-provider
          endpoint: unix:///var/run/kmsplugin/socket.sock
          cachesize: 1000
          timeout: 3s
      - identity: {}  # Fallback to plaintext if KMS unavailable (development only)
```

**External KMS provider configuration**:
- Azure: Use `kubernetes-kms` plugin with Azure Key Vault integration
- AWS: Use AWS Encryption Provider with KMS CMK
- GCP: Use GCP KMS plugin for envelope encryption
- KEK rotation handled by cloud provider, etcd data automatically re-encrypted

**Sealed Secrets vs native encryption**:
- **Decision: Use native encryption at rest with external KMS**
- Sealed Secrets (Bitnami) encrypts secrets before storing in Git, but requires additional operator
- Native encryption provides transparent encryption without application changes
- Sealed Secrets better for GitOps workflows where secrets stored in version control

**Service account RBAC for secret access**:
```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  namespace: sorcha-production
  name: wallet-key-reader
rules:
  - apiGroups: [""]
    resources: ["secrets"]
    resourceNames: ["wallet-keys"]  # Explicit secret name
    verbs: ["get"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: sorcha-wallet-service-binding
  namespace: sorcha-production
subjects:
  - kind: ServiceAccount
    name: sorcha-wallet-service
    namespace: sorcha-production
roleRef:
  kind: Role
  name: wallet-key-reader
  apiGroup: rbac.authorization.k8s.io
```

**Pod security policies**:
- Use `readOnlyRootFilesystem: true` to prevent secret extraction via filesystem writes
- Set `runAsNonRoot: true` and `allowPrivilegeEscalation: false`
- Mount secrets as read-only volumes: `readOnly: true`
- Use `securityContext.capabilities.drop: ["ALL"]` to minimize attack surface

**Alternatives considered**:
- HashiCorp Vault: Requires separate infrastructure, increased operational complexity
- External Secrets Operator: Adds indirection, but useful for multi-cluster secret sync

---

## 5. OS-Native Secure Storage

### Decision: Use platform-specific secure storage APIs with abstraction layer

**Rationale**:
- OS-native storage provides reasonable security for development environments
- No external dependencies or cloud connectivity required
- Keys encrypted at rest using OS-managed keys
- Automatic integration with OS security features (BitLocker, FileVault, etc.)

**Windows DPAPI (Data Protection API)**:
```csharp
using System.Security.Cryptography;

// User scope: Encrypted per-user, requires same user account to decrypt
// Machine scope: Encrypted per-machine, any user on machine can decrypt
byte[] encryptedData = ProtectedData.Protect(keyData, entropy, DataProtectionScope.CurrentUser);
byte[] decryptedData = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
```
- **Decision: Use `CurrentUser` scope** for development environments
- Keys stored in `%APPDATA%\Sorcha\Keys` with `.encrypted` extension
- DPAPI automatically uses machine's TPM if available
- Entropy (additional secret) stored separately in configuration

**macOS Keychain**:
```csharp
// P/Invoke approach for better control
[DllImport("/System/Library/Frameworks/Security.framework/Security")]
private static extern int SecKeychainAddGenericPassword(/*...*/);

[DllImport("/System/Library/Frameworks/Security.framework/Security")]
private static extern int SecKeychainFindGenericPassword(/*...*/);
```
- **Decision: Use P/Invoke to Security.framework** for programmatic access
- Alternative: `Process.Start("security", "add-generic-password ...")` less efficient but simpler
- Store keys in user's login keychain with `kSecAttrAccessibleWhenUnlocked` attribute
- Keychain automatically integrates with FileVault encryption

**Linux Secret Service**:
```csharp
// Use Tmds.DBus NuGet package for D-Bus communication
using Tmds.DBus;

// Connect to org.freedesktop.secrets service (gnome-keyring, KWallet)
var connection = Connection.Session;
var secretService = connection.CreateProxy<ISecretService>("org.freedesktop.secrets", "/org/freedesktop/secrets");
```
- **Decision: Use D-Bus protocol via Tmds.DBus library**
- Fallback to libsecret if D-Bus not available (headless servers)
- Store in "default" collection with label "Sorcha Wallet Key: {keyId}"
- Secret Service automatically encrypts with user's login password

**Docker volume persistence**:
- Mount host directory for OS-backed key storage: `-v ~/.sorcha/keys:/app/keys:ro`
- Use read-only mounts (`ro`) in container for additional security
- For Windows containers: Use named volumes with DPAPI encryption
- For Linux containers: Bind mount with appropriate file permissions (600)

**Alternatives considered**:
- File-based storage with AES encryption: Requires key management, less secure than OS storage
- In-memory only: Keys lost on restart, not suitable for development persistence

---

## 6. Web Crypto API Limitations

### Decision: Use Web Crypto API with IndexedDB storage for non-extractable keys

**Rationale**:
- Web Crypto API provides hardware-backed crypto in modern browsers (TEE, Secure Enclave)
- Non-extractable keys ensure private keys never leave browser's crypto subsystem
- IndexedDB provides persistent storage across browser sessions
- HTTPS-only requirement enforces secure transport

**Non-extractable key constraints**:
```javascript
const keyPair = await window.crypto.subtle.generateKey(
  {
    name: "ECDSA",
    namedCurve: "P-256"
  },
  false,  // extractable = false (non-exportable)
  ["sign", "verify"]
);

// Store in IndexedDB as CryptoKey object (not raw bytes)
const db = await openDB('sorcha-keys', 1);
await db.put('keys', keyPair.privateKey, 'wallet-key-001');
```
- **Limitation**: Non-extractable keys cannot be backed up or exported
- **Mitigation**: Warn users that browser data deletion loses keys permanently
- **Workaround**: Provide optional "backup mode" with extractable keys wrapped in user password

**IndexedDB encryption patterns**:
- Web Crypto API encrypts keys automatically when `extractable = false`
- IndexedDB storage itself NOT encrypted - relies on OS-level encryption (BitLocker, FileVault)
- **Decision**: Use non-extractable CryptoKey objects, avoid storing raw key material

**HTTPS-only requirements**:
- Web Crypto API operations restricted to secure contexts (HTTPS or localhost)
- HTTP sites: `crypto.subtle` is undefined
- **Decision**: Enforce HTTPS in production, allow HTTP localhost for development

**Browser compatibility matrix**:
| Browser | Version | Web Crypto | IndexedDB | Non-Extractable Keys |
|---------|---------|------------|-----------|----------------------|
| Chrome  | 37+     | ✅         | ✅        | ✅                   |
| Firefox | 34+     | ✅         | ✅        | ✅                   |
| Safari  | 11+     | ✅         | ✅        | ✅                   |
| Edge    | 79+     | ✅         | ✅        | ✅                   |

- **Decision**: Target browsers from last 3 years (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)

**Alternatives considered**:
- LocalStorage: Not suitable for CryptoKey objects, plaintext storage only
- WebAssembly crypto: Adds complexity, keys extractable in WASM memory

---

## 7. Environment Detection Strategies

### Decision: Use hierarchical detection with explicit metadata service queries

**Rationale**:
- Metadata services provide definitive environment identification
- Timeout-based detection (fast fail) prevents startup delays
- Hierarchical fallback ensures correct detection in multi-environment scenarios (e.g., Kubernetes on Azure)

**Detection order** (FR-021 backend priority):
1. Azure Instance Metadata Service (IMDS)
2. AWS Instance Metadata Service v2 (IMDSv2)
3. GCP Metadata Server
4. Kubernetes Service Account detection
5. Environment variables (`ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`)
6. Fallback to OS-native storage

**Azure IMDS**:
```csharp
using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
httpClient.DefaultRequestHeaders.Add("Metadata", "true");
var response = await httpClient.GetAsync("http://169.254.169.254/metadata/instance?api-version=2021-02-01");
if (response.IsSuccessStatusCode)
{
    var metadata = await response.Content.ReadFromJsonAsync<AzureMetadata>();
    // metadata.compute.azEnvironment, metadata.compute.vmId
    return BackendType.Azure;
}
```
- **Endpoint**: `http://169.254.169.254/metadata/instance`
- **Header**: `Metadata: true`
- **Timeout**: 2 seconds (fast fail if not Azure)

**AWS IMDSv2**:
```csharp
// Step 1: Get session token
var tokenRequest = new HttpRequestMessage(HttpMethod.Put, "http://169.254.169.254/latest/api/token");
tokenRequest.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");
var tokenResponse = await httpClient.SendAsync(tokenRequest);
var token = await tokenResponse.Content.ReadAsStringAsync();

// Step 2: Query metadata with token
var metadataRequest = new HttpRequestMessage(HttpMethod.Get, "http://169.254.169.254/latest/meta-data/instance-id");
metadataRequest.Headers.Add("X-aws-ec2-metadata-token", token);
var metadataResponse = await httpClient.SendAsync(metadataRequest);
if (metadataResponse.IsSuccessStatusCode)
{
    return BackendType.Aws;
}
```
- **Endpoint**: `http://169.254.169.254/latest/api/token` (session token)
- **Header**: `X-aws-ec2-metadata-token` (required for IMDSv2)
- **Timeout**: 2 seconds

**GCP Metadata Server**:
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "http://metadata.google.internal/computeMetadata/v1/instance/id");
request.Headers.Add("Metadata-Flavor", "Google");
var response = await httpClient.SendAsync(request);
if (response.IsSuccessStatusCode)
{
    return BackendType.Gcp;
}
```
- **Endpoint**: `http://metadata.google.internal/computeMetadata/v1/`
- **Header**: `Metadata-Flavor: Google`
- **Timeout**: 2 seconds

**Kubernetes detection**:
```csharp
var serviceAccountPath = "/var/run/secrets/kubernetes.io/serviceaccount/token";
if (File.Exists(serviceAccountPath))
{
    // Check if external KMS provider configured (via environment variable or config)
    var kmsProvider = Environment.GetEnvironmentVariable("KUBERNETES_KMS_PROVIDER");
    if (!string.IsNullOrEmpty(kmsProvider))
    {
        // Kubernetes with external KMS (delegates to cloud provider)
        return BackendType.Kubernetes;
    }
    // Otherwise, check for cloud provider metadata (Kubernetes ON cloud)
    // This ensures cloud HSM takes precedence per FR-002
}
```
- **Detection**: Check for service account token file
- **Precedence**: If running in Kubernetes on Azure/AWS/GCP, cloud HSM takes precedence

**Fallback detection order**:
1. Try Azure IMDS → if success, return Azure
2. Try AWS IMDSv2 → if success, return AWS
3. Try GCP Metadata → if success, return GCP
4. Check Kubernetes service account → if exists AND no cloud metadata, return Kubernetes
5. Check OS type (`RuntimeInformation.IsOSPlatform`) → return OS-native (Windows/macOS/Linux)
6. Default: Local development mode with warnings

**Alternatives considered**:
- DNS-based detection: Unreliable, requires network access
- Configuration-only: Error-prone, requires manual setup

---

## 8. Key Rotation Without Downtime

### Decision: Use versioned keys with dual-signing during rotation window

**Rationale**:
- Key versioning allows multiple key versions to coexist
- Dual-signing (new signatures use new key, old signatures verified with old key) ensures zero downtime
- Gradual rollout reduces risk of rotation-related failures
- Automatic rollback if new key version fails validation

**Key versioning scheme**:
```
KeyId: wallet-key-001
Versions:
  - v1: Created 2025-01-01, Active (current signing key)
  - v2: Created 2025-04-01, Pending (rotation in progress)
  - v3: Created 2025-07-01, Active (current signing key after rotation)
  - v1: Deprecated 2025-07-01 (retained for verification only)
```
- **Version format**: `{keyId}/v{versionNumber}`
- **Transition period**: 24 hours (configurable)
- **Retention policy**: Keep deprecated versions for 90 days minimum (for verification)

**Dual-signing during rotation**:
```csharp
public async Task<Signature> SignAsync(byte[] data)
{
    var activeKey = await GetActiveKeyVersionAsync();
    var pendingKey = await GetPendingKeyVersionAsync();

    // Sign with active key (current)
    var signature = await activeKey.SignAsync(data);

    // If rotation in progress, also verify new key can sign
    if (pendingKey != null && IsWithinRotationWindow(pendingKey))
    {
        var testSignature = await pendingKey.SignAsync(data);
        if (testSignature == null)
        {
            // New key failed, abort rotation
            await AbortRotationAsync(pendingKey);
            LogWarning("Key rotation aborted: pending key failed to sign");
        }
    }

    return signature;
}
```
- **Active key**: Current key used for signing
- **Pending key**: New key being validated during rotation window
- **Transition**: After successful validation period, pending becomes active

**Old key retention for verification**:
```csharp
public async Task<bool> VerifyAsync(byte[] data, Signature signature)
{
    // Try active key first (most likely)
    if (await activeKey.VerifyAsync(data, signature))
        return true;

    // Fallback to deprecated keys (for old signatures)
    foreach (var deprecatedKey in deprecatedKeys)
    {
        if (await deprecatedKey.VerifyAsync(data, signature))
        {
            LogInfo($"Signature verified with deprecated key {deprecatedKey.Version}");
            return true;
        }
    }

    return false;
}
```
- **Verification**: Try active key first, fallback to deprecated versions
- **Performance**: Cache verification results to avoid repeated HSM calls

**Rollback mechanisms**:
```csharp
public async Task RollbackRotationAsync(string keyId)
{
    var pendingKey = await GetPendingKeyVersionAsync(keyId);
    var activeKey = await GetActiveKeyVersionAsync(keyId);

    // Delete pending key
    await DeleteKeyVersionAsync(pendingKey);

    // Reset rotation metadata
    await UpdateRotationMetadataAsync(keyId, new RotationMetadata
    {
        Status = RotationStatus.Aborted,
        AbortedAt = DateTime.UtcNow,
        Reason = "Manual rollback or validation failure"
    });

    LogWarning($"Key rotation rolled back for {keyId}");
}
```
- **Rollback triggers**: Manual admin action, validation failure, error rate threshold exceeded
- **Audit trail**: Log all rollback events with reason and timestamp

**Alternatives considered**:
- Immediate key replacement: High risk of downtime if new key fails
- Manual rotation only: Human error risk, delayed security response

---

## 9. HSM-to-HSM Key Migration

### Decision: Use asymmetric key wrapping with RSA-OAEP for HSM-to-HSM transfer

**Rationale**:
- Asymmetric wrapping allows key export from source HSM without exposing plaintext
- RSA-OAEP (Optimal Asymmetric Encryption Padding) provides semantic security
- Wrapped key remains encrypted during transfer
- Destination HSM unwraps key, never exposing plaintext in transit or at endpoints

**Key wrapping technique**:
```
Source HSM (Azure Key Vault):
1. Generate ephemeral RSA key pair in source HSM (wrapping key)
2. Export source wallet key, encrypted with wrapping public key (RSA-OAEP)
3. Wrapped key = RSA_OAEP_Encrypt(walletKey, wrappingPublicKey)

Transfer:
4. Send wrappedKey + wrappingPublicKey to destination

Destination HSM (AWS KMS):
5. Import wrappingPrivateKey into destination HSM (temporary)
6. Unwrap: walletKey = RSA_OAEP_Decrypt(wrappedKey, wrappingPrivateKey)
7. Import walletKey into destination HSM as non-exportable
8. Delete wrappingPrivateKey from destination HSM
```

**Azure Key Vault export (source)**:
```csharp
var sourceClient = new KeyClient(new Uri(azureKeyVaultUrl), credential);

// Create wrapping key in source HSM
var wrappingKey = await sourceClient.CreateRsaKeyAsync(new CreateRsaKeyOptions("migration-wrapping-key")
{
    KeySize = 4096,
    HardwareProtected = true
});

// Export wallet key, encrypted with wrapping key
var exportRequest = new ExportKeyRequest(walletKeyName, wrappingKey.Name);
var exportedKey = await sourceClient.ExportKeyAsync(exportRequest);
// exportedKey.Key is RSA-OAEP encrypted
```

**AWS KMS import (destination)**:
```csharp
var kmsClient = new AmazonKeyManagementServiceClient();

// Import wrapping key (public key only initially)
var importKeyRequest = new ImportKeyMaterialRequest
{
    KeyId = destinationKeyId,
    WrappedKeyMaterial = exportedKey.Key,
    WrappingAlgorithm = WrappingAlgorithmSpec.RSAES_OAEP_SHA_256,
    ExpirationModel = ExpirationModelType.KEY_MATERIAL_EXPIRES,
    ValidTo = DateTime.UtcNow.AddHours(1)  // Temporary import
};
await kmsClient.ImportKeyMaterialAsync(importKeyRequest);
```

**Provider-specific limitations**:
- **Azure**: Requires Premium SKU for HSM key export
- **AWS**: Import requires CMK with `EXTERNAL` origin, not `AWS_KMS` origin
- **GCP**: Cloud KMS doesn't support key export; migration requires alternative approach (e.g., re-key transactions)

**Audit trail requirements**:
```csharp
public async Task<KeyMigrationRecord> MigrateKeyAsync(string keyId, BackendType source, BackendType destination)
{
    var record = new KeyMigrationRecord
    {
        KeyId = keyId,
        SourceProvider = source,
        DestinationProvider = destination,
        InitiatedAt = DateTime.UtcNow,
        InitiatedBy = GetCurrentUserPrincipal(),
        Status = MigrationStatus.InProgress
    };

    // Step 1: Export from source
    var wrappedKey = await ExportKeyAsync(keyId, source);
    record.SourceKeyId = wrappedKey.SourceId;

    // Step 2: Transfer (encrypted)
    record.TransferredAt = DateTime.UtcNow;
    record.IntegrityHash = ComputeSha256Hash(wrappedKey.CipherText);

    // Step 3: Import to destination
    var destinationKeyId = await ImportKeyAsync(wrappedKey, destination);
    record.DestinationKeyId = destinationKeyId;

    // Step 4: Verification
    var testData = GenerateRandomBytes(32);
    var sourceSignature = await SignWithKey(testData, keyId, source);
    var destSignature = await SignWithKey(testData, destinationKeyId, destination);

    if (sourceSignature.SequenceEqual(destSignature))
    {
        record.Status = MigrationStatus.Completed;
        record.CompletedAt = DateTime.UtcNow;
    }
    else
    {
        record.Status = MigrationStatus.Failed;
        record.ErrorMessage = "Signature mismatch between source and destination keys";
        await DeleteKeyAsync(destinationKeyId, destination);  // Cleanup failed import
    }

    await StoreM migrationRecordAsync(record);
    return record;
}
```
- **Cryptographic proof**: Hash of wrapped key ensures integrity
- **Verification**: Test signatures from both keys must match
- **Administrative approval**: Require explicit approval before deletion of source key

**Migration verification procedures**:
1. Export key from source HSM (wrapped)
2. Import to destination HSM
3. Generate random test data
4. Sign test data with source key
5. Sign same test data with destination key
6. Compare signatures (must match)
7. If verification passes, mark migration successful
8. Retain source key for 30 days minimum before deletion (safety buffer)

**Alternatives considered**:
- Symmetric key wrapping (AES-KW): Requires shared secret, less suitable for HSM-to-HSM
- Plaintext export: Violates security requirements, keys exposed during transfer

---

## 10. Managed Identity Authentication

### Decision: Use SDK-provided credential chains with environment-specific defaults

**Rationale**:
- Modern cloud SDKs provide credential chains that automatically detect environment
- Zero configuration required in most cases (automatic in cloud environments)
- Local development works seamlessly with CLI-based authentication
- No credential storage or secret management required

**Azure Managed Identity**:
```csharp
using Azure.Identity;

// DefaultAzureCredential tries in order:
// 1. Environment variables (AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID)
// 2. Managed Identity (Azure VM, App Service, AKS, etc.)
// 3. Visual Studio authentication
// 4. Azure CLI authentication
// 5. Azure PowerShell authentication
var credential = new DefaultAzureCredential();
var client = new KeyClient(new Uri(keyVaultUrl), credential);
```
- **Token acquisition**: Automatic via Azure Instance Metadata Service (IMDS)
- **Token refresh**: SDK handles automatic refresh (every 24 hours)
- **Configuration**: Assign Managed Identity to VM/App Service, grant IAM role to Key Vault

**AWS IAM Roles**:
```csharp
using Amazon.Runtime;

// SDK automatically uses IAM Role credentials from:
// 1. Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
// 2. EC2 instance profile (IMDSv2)
// 3. ECS task role
// 4. AWS CLI credentials (~/.aws/credentials)
var kmsClient = new AmazonKeyManagementServiceClient();  // No explicit credentials
```
- **Instance profile**: Attach IAM role to EC2 instance, ECS task, or Lambda function
- **Token acquisition**: Automatic via IMDSv2 (session token + temporary credentials)
- **Token refresh**: SDK handles automatic refresh (every 1 hour)

**GCP Service Account impersonation**:
```csharp
using Google.Apis.Auth.OAuth2;

// Application Default Credentials (ADC) tries in order:
// 1. GOOGLE_APPLICATION_CREDENTIALS environment variable (JSON key file)
// 2. Workload Identity (GKE service account → GCP service account mapping)
// 3. Compute Engine metadata server
// 4. gcloud CLI credentials
var credential = GoogleCredential.GetApplicationDefault();
var client = KeyManagementServiceClient.Create();
```
- **Workload Identity (GKE)**: Map Kubernetes service account to GCP service account
- **Token acquisition**: Automatic via GCP metadata server
- **Token refresh**: SDK handles automatic refresh

**Kubernetes service account token projection**:
```csharp
using k8s;

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);

// Service account token automatically mounted at:
// /var/run/secrets/kubernetes.io/serviceaccount/token
var token = File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token");

// Use token to authenticate to cloud provider from within pod
var azureCredential = new ClientSecretCredential(tenantId, clientId, token);
```
- **Token projection**: Kubernetes projects service account token into pod filesystem
- **Audience claim**: Configure token audience for Azure/AWS/GCP federation
- **Token refresh**: Kubernetes automatically rotates token (every 1 hour)

**Local development authentication**:
- **Azure**: `az login` → `DefaultAzureCredential` uses Azure CLI tokens
- **AWS**: `aws configure` → SDK uses credentials from `~/.aws/credentials`
- **GCP**: `gcloud auth application-default login` → ADC uses gcloud tokens

**Alternatives considered**:
- Service principals with certificates: More complex, requires certificate management
- Static API keys: Security risk, requires secret rotation

---

## Summary of Key Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| Azure HSM | DefaultAzureCredential + Key Vault Keys SDK | Automatic credential detection, HSM enforcement |
| AWS KMS | IAM Roles with IMDSv2 | Automatic credential rotation, no secret storage |
| GCP Cloud KMS | Workload Identity + ADC | GKE-native authentication, zero configuration |
| Kubernetes | External KMS provider with envelope encryption | Cloud HSM integration, native K8s encryption |
| OS Storage | Platform-specific APIs (DPAPI, Keychain, Secret Service) | No external dependencies, OS-integrated security |
| Web Crypto | Non-extractable keys in IndexedDB | Browser-native security, no server-side storage |
| Environment Detection | Hierarchical metadata service queries | Definitive identification, fast fail timeouts |
| Key Rotation | Versioned keys with dual-signing | Zero downtime, automatic rollback capability |
| Key Migration | Asymmetric key wrapping (RSA-OAEP) | HSM-to-HSM transfer without plaintext exposure |
| Authentication | SDK credential chains | Environment-aware, zero configuration |

---

## Implementation Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Cloud HSM outage | High | Exponential backoff retry, optional software fallback (per-environment config) |
| Key rotation failure | Medium | Automatic rollback, validation period, dual-signing |
| Environment misdetection | Medium | Hierarchical fallback, explicit configuration override |
| Managed identity misconfiguration | High | Startup health checks, fail-fast on production, clear error messages |
| Key migration corruption | High | Cryptographic verification, integrity hashing, source key retention period |
| Browser key loss | Low | Prominent warnings, optional backup mode with user password wrapping |
| Performance degradation | Medium | Connection pooling, SDK-level caching, operation queuing during outages |

---

**Research completed**: 2025-12-23
**Next phase**: Phase 1 - Data Model and Contracts
