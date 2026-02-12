// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System;
using System.Collections.Generic;

namespace Sorcha.Cryptography.SdJwt;

/// <summary>
/// Result of verifying an SD-JWT token or presentation.
/// </summary>
public class SdJwtVerificationResult
{
    /// <summary>
    /// Whether the token/presentation is cryptographically valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Disclosed claims extracted from the verified token.
    /// </summary>
    public Dictionary<string, object> Claims { get; set; } = new();

    /// <summary>
    /// Verification error messages, if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// The issuer identifier from the token payload.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// The subject identifier from the token payload.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Issuance timestamp from the token payload.
    /// </summary>
    public DateTimeOffset? IssuedAt { get; set; }

    /// <summary>
    /// Expiration timestamp from the token payload.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
