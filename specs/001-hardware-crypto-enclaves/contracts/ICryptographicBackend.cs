using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sorcha.Cryptography.Abstractions;

/// <summary>
/// Represents a cryptographic backend provider (Azure Key Vault, AWS KMS, GCP Cloud KMS, etc.).
/// Implements FR-001, FR-002, FR-003, FR-004, FR-008.
/// </summary>
/// <remarks>
/// This is the primary abstraction for all cryptographic operations. Each backend implementation
/// must ensure that:
/// - Keys are generated within secure enclaves (FR-003)
/// - Signing operations execute within the enclave boundary (FR-004)
/// - No private key material is exposed to application memory
/// - All operations are audit logged (FR-010)
/// </remarks>
public interface ICryptographicBackend : IDisposable
{
    /// <summary>
    /// Unique identifier for this backend instance.
    /// </summary>
    string BackendId { get; }

    /// <summary>
    /// The provider type (Azure, AWS, GCP, Kubernetes, OS, Browser).
    /// </summary>
    BackendType ProviderType { get; }

    /// <summary>
    /// Security level provided by this backend (HSM, TPM, Software).
    /// </summary>
    SecurityLevel SecurityLevel { get; }

    /// <summary>
    /// Current availability status of the backend.
    /// </summary>
    AvailabilityStatus Status { get; }

    /// <summary>
    /// Initializes the backend with provider-specific configuration.
    /// </summary>
    /// <param name="configuration">Provider-specific configuration parameters (e.g., Key Vault URL, region).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if initialization successful, false otherwise.</returns>
    /// <exception cref="CryptographicBackendException">If initialization fails.</exception>
    Task<bool> InitializeAsync(IDictionary<string, string> configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new cryptographic key within the secure enclave (FR-003).
    /// </summary>
    /// <param name="algorithm">The cryptographic algorithm (RSA_4096, ECDSA_P256, etc.).</param>
    /// <param name="keyId">Unique identifier for the key.</param>
    /// <param name="securityProperties">Security properties (exportable, HSM-backed, operations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="KeyHandle"/> referencing the generated key.</returns>
    /// <remarks>
    /// For HSM-backed implementations, keys MUST be marked as non-exportable (FR-003).
    /// The key generation operation MUST occur entirely within the HSM boundary.
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If key generation fails.</exception>
    Task<KeyHandle> GenerateKeyAsync(
        CryptoAlgorithm algorithm,
        string keyId,
        KeySecurityProperties securityProperties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs data using a key stored in the secure enclave (FR-004).
    /// </summary>
    /// <param name="keyHandle">Reference to the key to use for signing.</param>
    /// <param name="data">Data to sign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cryptographic signature.</returns>
    /// <remarks>
    /// For HSM-backed implementations, the signing operation MUST execute entirely within
    /// the HSM boundary. Private key material MUST NOT be exposed to application memory (FR-004).
    /// All signing operations MUST be audit logged (FR-010).
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If signing operation fails.</exception>
    Task<byte[]> SignAsync(
        KeyHandle keyHandle,
        byte[] data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a signature using a key stored in the secure enclave.
    /// </summary>
    /// <param name="keyHandle">Reference to the key to use for verification.</param>
    /// <param name="data">Original data that was signed.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    /// <exception cref="CryptographicBackendException">If verification operation fails.</exception>
    Task<bool> VerifyAsync(
        KeyHandle keyHandle,
        byte[] data,
        byte[] signature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the public key portion of a key pair (safe to expose).
    /// </summary>
    /// <param name="keyHandle">Reference to the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public key bytes in DER-encoded format.</returns>
    /// <remarks>
    /// This operation retrieves only the public key component. Private key material
    /// MUST NEVER be retrieved (FR-003, FR-004).
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If retrieval fails.</exception>
    Task<byte[]> GetPublicKeyAsync(
        KeyHandle keyHandle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key from the secure storage.
    /// </summary>
    /// <param name="keyHandle">Reference to the key to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion successful, false if key not found.</returns>
    /// <remarks>
    /// This operation is irreversible. For production keys, consider using soft-delete
    /// or retention policies instead of immediate deletion.
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If deletion fails.</exception>
    Task<bool> DeleteKeyAsync(
        KeyHandle keyHandle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a key in encrypted/wrapped form for migration (FR-012).
    /// </summary>
    /// <param name="keyHandle">Reference to the key to export.</param>
    /// <param name="wrappingKeyHandle">Key to use for wrapping the exported key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wrapped (encrypted) key material.</returns>
    /// <remarks>
    /// For HSM-backed keys, this operation uses asymmetric key wrapping (RSA-OAEP).
    /// The key is exported in encrypted form using the provided wrapping key.
    /// The plaintext key material MUST NEVER leave the HSM boundary (FR-012).
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If export fails or key is non-exportable.</exception>
    Task<byte[]> ExportKeyAsync(
        KeyHandle keyHandle,
        KeyHandle wrappingKeyHandle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a wrapped key from another backend (FR-012).
    /// </summary>
    /// <param name="wrappedKeyData">The encrypted key material.</param>
    /// <param name="unwrappingKeyHandle">Key to use for unwrapping.</param>
    /// <param name="targetKeyId">Identifier for the imported key.</param>
    /// <param name="algorithm">Algorithm of the imported key.</param>
    /// <param name="securityProperties">Security properties for the imported key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="KeyHandle"/> referencing the imported key.</returns>
    /// <remarks>
    /// The unwrapping operation MUST occur within the HSM boundary.
    /// The plaintext key material MUST NOT be exposed during import (FR-012).
    /// </remarks>
    /// <exception cref="CryptographicBackendException">If import fails.</exception>
    Task<KeyHandle> ImportKeyAsync(
        byte[] wrappedKeyData,
        KeyHandle unwrappingKeyHandle,
        string targetKeyId,
        CryptoAlgorithm algorithm,
        KeySecurityProperties securityProperties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the backend (FR-016).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="HealthCheckResult"/> indicating backend availability.</returns>
    /// <remarks>
    /// Health checks should complete quickly (&lt;2 seconds) to avoid blocking startup.
    /// Implementations should test connectivity to the backend service and validate credentials.
    /// </remarks>
    Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown by cryptographic backend operations.
/// </summary>
public class CryptographicBackendException : Exception
{
    public BackendType BackendType { get; }
    public string OperationName { get; }
    public string? KeyId { get; }

    public CryptographicBackendException(
        string message,
        BackendType backendType,
        string operationName,
        string? keyId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        BackendType = backendType;
        OperationName = operationName;
        KeyId = keyId;
    }
}

/// <summary>
/// Result of a backend health check operation (FR-016).
/// </summary>
public record HealthCheckResult(
    bool IsHealthy,
    string? Message = null,
    TimeSpan? ResponseTime = null,
    DateTime CheckedAt = default);
