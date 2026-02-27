// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System;

namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Supported symmetric encryption algorithms.
/// </summary>
public enum EncryptionType
{
    /// <summary>
    /// AES-128 in CBC mode. Deprecated: unauthenticated encryption is vulnerable to padding oracle attacks.
    /// </summary>
    [Obsolete("AES-CBC is unauthenticated and vulnerable to padding oracle attacks. Use AES_GCM or CHACHA20_POLY1305.")]
    AES_128,

    /// <summary>
    /// AES-256 in CBC mode. Deprecated: unauthenticated encryption is vulnerable to padding oracle attacks.
    /// </summary>
    [Obsolete("AES-CBC is unauthenticated and vulnerable to padding oracle attacks. Use AES_GCM or CHACHA20_POLY1305.")]
    AES_256,

    /// <summary>
    /// AES-256 in GCM mode (authenticated encryption).
    /// </summary>
    AES_GCM,

    /// <summary>
    /// ChaCha20-Poly1305 (authenticated encryption).
    /// </summary>
    CHACHA20_POLY1305,

    /// <summary>
    /// XChaCha20-Poly1305 (authenticated encryption with extended nonce).
    /// </summary>
    XCHACHA20_POLY1305
}
