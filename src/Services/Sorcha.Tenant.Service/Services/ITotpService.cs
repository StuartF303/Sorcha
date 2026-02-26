// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Services;

/// <summary>
/// Result of TOTP setup containing the secret, QR URI, and backup codes.
/// </summary>
public record TotpSetupResult
{
    /// <summary>
    /// Base32-encoded shared secret for manual entry in authenticator apps.
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// otpauth:// URI for QR code generation.
    /// Format: otpauth://totp/Sorcha:{email}?secret={base32}&amp;issuer=Sorcha
    /// </summary>
    public required string QrUri { get; init; }

    /// <summary>
    /// One-time backup codes (8-char alphanumeric) for account recovery.
    /// Shown once during setup — user must store securely.
    /// </summary>
    public required string[] BackupCodes { get; init; }
}

/// <summary>
/// Result of TOTP status check.
/// </summary>
public record TotpStatusResult
{
    /// <summary>
    /// Whether TOTP 2FA is fully enabled (setup completed and verified).
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// When TOTP was verified and activated. Null if not enabled.
    /// </summary>
    public DateTime? VerifiedAt { get; init; }
}

/// <summary>
/// Service interface for TOTP two-factor authentication operations.
/// Manages setup, verification, validation, and backup code lifecycle.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Initiates TOTP setup for a user. Generates a new shared secret,
    /// QR URI, and 10 backup codes. Replaces any existing pending setup.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Setup result with secret, QR URI, and backup codes.</returns>
    Task<TotpSetupResult> SetupAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the initial TOTP code after setup, enabling 2FA for the user.
    /// Must be called with a valid code from the authenticator app to complete enrollment.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="code">Six-digit TOTP code from authenticator app.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if code is valid and TOTP is now enabled.</returns>
    Task<bool> VerifyAndEnableAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a TOTP code during login (after password verification).
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="code">Six-digit TOTP code from authenticator app.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if code is valid.</returns>
    Task<bool> ValidateCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and consumes a backup code. Each backup code can only be used once.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="code">Eight-character alphanumeric backup code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backup code is valid and was consumed.</returns>
    Task<bool> ValidateBackupCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables TOTP 2FA for a user, removing all configuration.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisableAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the TOTP status for a user (enabled/disabled).
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status result.</returns>
    Task<TotpStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a short-lived login token for the two-factor authentication step.
    /// Issued after successful password verification when TOTP is enabled.
    /// Valid for 5 minutes.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Short-lived login token string.</returns>
    Task<string> GenerateLoginTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a login token issued during the two-factor authentication flow.
    /// </summary>
    /// <param name="loginToken">The login token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID if valid, null otherwise.</returns>
    Task<Guid?> ValidateLoginTokenAsync(string loginToken, CancellationToken cancellationToken = default);
}
