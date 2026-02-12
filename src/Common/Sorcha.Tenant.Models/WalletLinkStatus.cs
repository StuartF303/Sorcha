// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Status of a wallet address link to a participant identity.
/// </summary>
public enum WalletLinkStatus
{
    /// <summary>
    /// Wallet address is actively linked and can be used for signing.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Wallet link has been revoked (soft delete for audit trail).
    /// </summary>
    Revoked = 1
}
