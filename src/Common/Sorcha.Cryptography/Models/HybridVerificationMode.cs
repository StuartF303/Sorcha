// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Controls how hybrid (classical + PQC) signatures are verified.
/// </summary>
public enum HybridVerificationMode
{
    /// <summary>
    /// Both classical AND PQC components must verify successfully.
    /// Use for production security — compromising one key type is insufficient.
    /// </summary>
    Strict,

    /// <summary>
    /// Either classical OR PQC component verifying is sufficient.
    /// Use during migration when not all participants have PQC keys yet.
    /// </summary>
    Permissive
}
