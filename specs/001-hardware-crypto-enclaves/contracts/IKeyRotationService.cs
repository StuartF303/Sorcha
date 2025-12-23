using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sorcha.Cryptography.Abstractions;

/// <summary>
/// Manages cryptographic key rotation with automatic time-based and manual administrator-initiated rotation.
/// Implements FR-017, FR-022.
/// </summary>
/// <remarks>
/// Key rotation process:
/// 1. Create new key version (Status = Pending)
/// 2. Validation period: Dual-signing with both old (Active) and new (Pending) keys
/// 3. If validation successful: New key becomes Active, old key becomes Deprecated
/// 4. If validation fails: Rollback (delete Pending key, retain Active key)
///
/// Old key versions with Status = Deprecated are retained for signature verification.
/// </remarks>
public interface IKeyRotationService
{
    /// <summary>
    /// Initiates automatic time-based key rotation (FR-022).
    /// </summary>
    /// <param name="keyId">Unique identifier of the key to rotate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="RotationResult"/> containing the new key handle and rotation status.</returns>
    /// <remarks>
    /// This method is called automatically by the rotation scheduler when a key's age exceeds
    /// the configured RotationPolicy.RotationInterval.
    ///
    /// Process:
    /// 1. Get active key handle
    /// 2. Create new key version (version = activeVersion + 1, status = Pending)
    /// 3. Enter validation period (configure ValidationPeriod duration)
    /// 4. Test signing with new key
    /// 5. If successful after validation period: Promote to Active, deprecate old key
    /// 6. If failed: Rollback rotation
    ///
    /// FR-022: "System MUST monitor key age and automatically initiate rotation when the
    /// configured time interval is reached"
    /// </remarks>
    /// <exception cref="KeyRotationException">If rotation fails.</exception>
    Task<RotationResult> InitiateAutomaticRotationAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates manual administrator-initiated key rotation (FR-017).
    /// </summary>
    /// <param name="keyId">Unique identifier of the key to rotate.</param>
    /// <param name="requestingPrincipal">Administrator who initiated the rotation.</param>
    /// <param name="reason">Justification for manual rotation (e.g., "Suspected compromise", "Security incident").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="RotationResult"/> containing the new key handle and rotation status.</returns>
    /// <remarks>
    /// Manual rotation allows administrators to immediately rotate keys in response to:
    /// - Security incidents or suspected compromise
    /// - Personnel changes (employee departure)
    /// - Compliance requirements
    ///
    /// Manual rotation follows the same process as automatic rotation but may have a shorter
    /// or zero validation period depending on urgency.
    ///
    /// FR-017: "System MUST...allow administrators to trigger immediate manual rotation for
    /// security incidents"
    /// </remarks>
    /// <exception cref="KeyRotationException">If rotation fails.</exception>
    Task<RotationResult> InitiateManualRotationAsync(
        string keyId,
        string requestingPrincipal,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a failed or problematic key rotation.
    /// </summary>
    /// <param name="keyId">Unique identifier of the key being rotated.</param>
    /// <param name="reason">Reason for rollback (e.g., "Validation failed", "Error rate threshold exceeded").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rollback successful, false if already rolled back or no rotation in progress.</returns>
    /// <remarks>
    /// Rollback process:
    /// 1. Find Pending key version (if exists)
    /// 2. Delete Pending key from backend (ICryptographicBackend.DeleteKeyAsync)
    /// 3. Delete Pending key metadata (IKeyStorage.DeleteKeyHandleAsync)
    /// 4. Retain Active key version (no changes)
    /// 5. Log rollback event with reason
    ///
    /// Triggers for automatic rollback:
    /// - New key fails validation tests
    /// - Error rate exceeds threshold during validation period
    /// - Backend reports key as unhealthy
    /// </remarks>
    /// <exception cref="KeyRotationException">If rollback operation fails.</exception>
    Task<bool> RollbackRotationAsync(
        string keyId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a rotation that has passed validation period.
    /// </summary>
    /// <param name="keyId">Unique identifier of the key being rotated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly activated key handle.</returns>
    /// <remarks>
    /// Called after validation period completes successfully. Performs the final promotion:
    /// 1. Set Pending key status = Active
    /// 2. Set old Active key status = Deprecated
    /// 3. Update NextScheduledRotationDate for new Active key
    /// 4. Log rotation completion event
    ///
    /// After completion:
    /// - New signatures use the new Active key
    /// - Old signatures can still be verified with Deprecated keys
    /// - Deprecated keys are retained per RotationPolicy.DeprecatedVersionRetentionPeriod
    /// </remarks>
    /// <exception cref="KeyRotationException">If completion fails.</exception>
    Task<KeyHandle> CompleteRotationAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rotation status for a key.
    /// </summary>
    /// <param name="keyId">Unique identifier of the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current rotation status, or null if key not found.</returns>
    /// <remarks>
    /// Rotation states:
    /// - None: No rotation in progress, single Active version
    /// - InProgress: Pending version exists, validation period active
    /// - Completed: Rotation finished, new key Active, old key Deprecated
    /// - RolledBack: Rotation aborted, Pending key deleted, Active key retained
    /// </remarks>
    Task<RotationStatus?> GetRotationStatusAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a Pending key can successfully perform cryptographic operations.
    /// </summary>
    /// <param name="keyHandle">The Pending key handle to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// Validation steps:
    /// 1. Generate random test data
    /// 2. Sign data with Pending key
    /// 3. Verify signature with Pending key's public key
    /// 4. Compare result with Active key (both should produce valid signatures)
    /// 5. Check backend health status for Pending key
    ///
    /// If any step fails, rotation should be rolled back.
    /// </remarks>
    Task<ValidationResult> ValidatePendingKeyAsync(
        KeyHandle keyHandle,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a key rotation operation.
/// </summary>
public record RotationResult(
    bool Success,
    KeyHandle? NewKeyHandle = null,
    KeyHandle? OldKeyHandle = null,
    RotationStatus Status = RotationStatus.None,
    string? ErrorMessage = null,
    DateTime? RotationCompletedAt = null);

/// <summary>
/// Status of key rotation process.
/// </summary>
public enum RotationStatus
{
    /// <summary>
    /// No rotation in progress.
    /// </summary>
    None,

    /// <summary>
    /// Rotation initiated, Pending key created, validation period active.
    /// </summary>
    InProgress,

    /// <summary>
    /// Rotation completed successfully, new key Active, old key Deprecated.
    /// </summary>
    Completed,

    /// <summary>
    /// Rotation rolled back due to validation failure or error.
    /// </summary>
    RolledBack
}

/// <summary>
/// Result of Pending key validation.
/// </summary>
public record ValidationResult(
    bool IsValid,
    string? FailureReason = null,
    TimeSpan? TestDuration = null);

/// <summary>
/// Exception thrown by key rotation operations.
/// </summary>
public class KeyRotationException : Exception
{
    public string KeyId { get; }
    public RotationStatus CurrentStatus { get; }

    public KeyRotationException(
        string message,
        string keyId,
        RotationStatus currentStatus,
        Exception? innerException = null)
        : base(message, innerException)
    {
        KeyId = keyId;
        CurrentStatus = currentStatus;
    }
}
