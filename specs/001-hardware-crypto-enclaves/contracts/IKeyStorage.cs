using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sorcha.Cryptography.Abstractions;

/// <summary>
/// Provides persistent storage and retrieval of key metadata.
/// Implements FR-008, FR-011, FR-013, FR-017.
/// </summary>
/// <remarks>
/// This interface is responsible for storing key metadata (KeyHandle), NOT the actual key material.
/// Key material remains in the secure enclave (HSM, TPM, OS storage, etc.) and is never persisted
/// by this storage layer.
///
/// Key security properties (exportable/non-exportable, HSM-backed) are stored to enforce
/// production/development environment boundaries (FR-011, FR-014).
/// </remarks>
public interface IKeyStorage
{
    /// <summary>
    /// Stores key metadata in persistent storage.
    /// </summary>
    /// <param name="keyHandle">The key handle containing metadata (NOT the actual key material).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if storage successful, false otherwise.</returns>
    /// <remarks>
    /// Only metadata is stored (KeyId, Version, Algorithm, timestamps, provider URI).
    /// The actual private key material remains in the backend (HSM/OS storage/etc.).
    /// </remarks>
    /// <exception cref="KeyStorageException">If storage operation fails.</exception>
    Task<bool> StoreKeyHandleAsync(KeyHandle keyHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves key metadata by KeyId and Version.
    /// </summary>
    /// <param name="keyId">Unique key identifier.</param>
    /// <param name="version">Key version number (for rotation tracking).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The key handle if found, null otherwise.</returns>
    /// <exception cref="KeyStorageException">If retrieval operation fails.</exception>
    Task<KeyHandle?> GetKeyHandleAsync(string keyId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active (current) version of a key (FR-017).
    /// </summary>
    /// <param name="keyId">Unique key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active key handle, or null if key not found or no active version.</returns>
    /// <remarks>
    /// The active version is the highest version number with Status == KeyStatus.Active.
    /// Used for signing operations - signatures use the active key version.
    /// </remarks>
    /// <exception cref="KeyStorageException">If retrieval operation fails.</exception>
    Task<KeyHandle?> GetActiveKeyHandleAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all versions of a key (for signature verification with deprecated keys).
    /// </summary>
    /// <param name="keyId">Unique key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all key versions, ordered by version descending (newest first).</returns>
    /// <remarks>
    /// Used for signature verification - old signatures may have been created with deprecated
    /// key versions, so we need to try all versions during verification.
    /// </remarks>
    /// <exception cref="KeyStorageException">If retrieval operation fails.</exception>
    Task<IReadOnlyList<KeyHandle>> GetAllKeyVersionsAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates key metadata (typically for rotation status changes).
    /// </summary>
    /// <param name="keyHandle">The updated key handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update successful, false if key not found.</returns>
    /// <remarks>
    /// Used to update key status during rotation:
    /// - Active → Pending (rotation initiated)
    /// - Pending → Active (rotation completed, old key becomes Deprecated)
    /// - Active/Deprecated → Revoked (key compromised)
    /// </remarks>
    /// <exception cref="KeyStorageException">If update operation fails.</exception>
    Task<bool> UpdateKeyHandleAsync(KeyHandle keyHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes key metadata from storage.
    /// </summary>
    /// <param name="keyId">Unique key identifier.</param>
    /// <param name="version">Key version to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion successful, false if key not found.</returns>
    /// <remarks>
    /// This only deletes metadata. The actual key in the backend (HSM/OS storage) must be
    /// deleted separately via ICryptographicBackend.DeleteKeyAsync().
    ///
    /// WARNING: Deleting metadata makes the key unrecoverable even if it still exists in
    /// the backend, as we lose the provider-specific URI needed to reference it.
    /// </remarks>
    /// <exception cref="KeyStorageException">If deletion operation fails.</exception>
    Task<bool> DeleteKeyHandleAsync(string keyId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all keys owned by a specific principal.
    /// </summary>
    /// <param name="ownerPrincipal">User or service principal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of key handles owned by the principal.</returns>
    /// <remarks>
    /// Used for key management dashboards and user-specific key listings.
    /// </remarks>
    /// <exception cref="KeyStorageException">If query operation fails.</exception>
    Task<IReadOnlyList<KeyHandle>> ListKeysByOwnerAsync(string ownerPrincipal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds keys that require rotation based on rotation policy (FR-022).
    /// </summary>
    /// <param name="rotationPolicy">The rotation policy to evaluate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of keys that need rotation (NextScheduledRotationDate &lt;= now).</returns>
    /// <remarks>
    /// Called by automatic rotation service to identify keys that have exceeded their
    /// rotation interval. Only returns keys with Status == Active and AutomaticRotationEnabled.
    /// </remarks>
    /// <exception cref="KeyStorageException">If query operation fails.</exception>
    Task<IReadOnlyList<KeyHandle>> FindKeysRequiringRotationAsync(
        RotationPolicy rotationPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prevents migration of development-mode keys to production (FR-011).
    /// </summary>
    /// <param name="keyId">Key identifier to validate.</param>
    /// <param name="targetEnvironment">Target environment type (Production, Staging, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if migration is allowed, false if prevented by policy.</returns>
    /// <remarks>
    /// Validates that:
    /// - Development keys (SecurityLevel == Software) cannot be migrated to Production
    /// - OS-backed keys (BackendType == OS) cannot be migrated to Production
    /// - Only HSM-backed keys can be migrated to Production environments
    /// </remarks>
    /// <exception cref="KeyStorageException">If validation fails.</exception>
    Task<bool> ValidateKeyMigrationAsync(
        string keyId,
        EnvironmentType targetEnvironment,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown by key storage operations.
/// </summary>
public class KeyStorageException : Exception
{
    public string? KeyId { get; }
    public string OperationName { get; }

    public KeyStorageException(
        string message,
        string operationName,
        string? keyId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        KeyId = keyId;
        OperationName = operationName;
    }
}
