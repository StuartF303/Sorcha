// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// PassKey-authenticated user without organizational affiliation.
/// Uses FIDO2/WebAuthn for passwordless authentication.
/// </summary>
public class PublicIdentity
{
    /// <summary>
    /// Unique public user identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FIDO2 credential ID (globally unique).
    /// Used to look up the authenticator during login.
    /// </summary>
    public byte[] PassKeyCredentialId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// COSE-encoded public key for signature verification.
    /// </summary>
    public byte[] PublicKeyCose { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Signature counter for cloned authenticator detection.
    /// Increments on each authentication. If counter doesn't increase, authenticator may be cloned.
    /// </summary>
    public int SignatureCounter { get; set; } = 0;

    /// <summary>
    /// Authenticator device type (e.g., "YubiKey 5 NFC", "Windows Hello", "TouchID").
    /// Extracted from authenticator data if available.
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// PassKey registration timestamp (UTC).
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last successful authentication timestamp (UTC). Null if never used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
