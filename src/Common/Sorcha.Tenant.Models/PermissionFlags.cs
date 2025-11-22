// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Permission flags for authorization decisions across Sorcha services.
/// Extracted from JWT token claims or organization configuration.
/// </summary>
[Flags]
public enum PermissionFlags : long
{
    /// <summary>
    /// No permissions granted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Can read blockchain data.
    /// </summary>
    BlockchainRead = 1 << 0,

    /// <summary>
    /// Can write to blockchain (submit transactions).
    /// </summary>
    BlockchainWrite = 1 << 1,

    /// <summary>
    /// Can create new blockchains.
    /// </summary>
    BlockchainCreate = 1 << 2,

    /// <summary>
    /// Can delete blockchains (soft delete).
    /// </summary>
    BlockchainDelete = 1 << 3,

    /// <summary>
    /// Can read blueprint definitions.
    /// </summary>
    BlueprintRead = 1 << 4,

    /// <summary>
    /// Can create blueprint definitions.
    /// </summary>
    BlueprintCreate = 1 << 5,

    /// <summary>
    /// Can publish blueprints to the network.
    /// </summary>
    BlueprintPublish = 1 << 6,

    /// <summary>
    /// Can execute blueprint actions.
    /// </summary>
    BlueprintExecute = 1 << 7,

    /// <summary>
    /// Can read wallet information (public keys, addresses).
    /// </summary>
    WalletRead = 1 << 8,

    /// <summary>
    /// Can create new wallets.
    /// </summary>
    WalletCreate = 1 << 9,

    /// <summary>
    /// Can sign transactions with wallets.
    /// </summary>
    WalletSign = 1 << 10,

    /// <summary>
    /// Can manage organization settings (administrators only).
    /// </summary>
    OrganizationAdmin = 1 << 11,

    /// <summary>
    /// Can view audit logs (auditors and administrators).
    /// </summary>
    AuditLogRead = 1 << 12,

    /// <summary>
    /// Can manage users within organization (administrators only).
    /// </summary>
    UserManagement = 1 << 13,

    /// <summary>
    /// Can configure IDP settings (administrators only).
    /// </summary>
    IdpConfiguration = 1 << 14,

    /// <summary>
    /// Can manage organization permissions (administrators only).
    /// </summary>
    PermissionManagement = 1 << 15,

    /// <summary>
    /// Full administrative access (all permissions).
    /// </summary>
    FullAdministrator = BlockchainRead | BlockchainWrite | BlockchainCreate | BlockchainDelete |
                         BlueprintRead | BlueprintCreate | BlueprintPublish | BlueprintExecute |
                         WalletRead | WalletCreate | WalletSign |
                         OrganizationAdmin | AuditLogRead | UserManagement | IdpConfiguration | PermissionManagement,

    /// <summary>
    /// Standard member permissions (read + execute).
    /// </summary>
    StandardMember = BlockchainRead | BlueprintRead | BlueprintExecute | WalletRead,

    /// <summary>
    /// Auditor permissions (read-only access to logs and resources).
    /// </summary>
    Auditor = BlockchainRead | BlueprintRead | WalletRead | AuditLogRead
}

/// <summary>
/// Extension methods for PermissionFlags.
/// </summary>
public static class PermissionFlagsExtensions
{
    /// <summary>
    /// Check if a permission set contains a specific permission.
    /// </summary>
    public static bool HasPermission(this PermissionFlags permissions, PermissionFlags permission)
    {
        return (permissions & permission) == permission;
    }

    /// <summary>
    /// Check if a permission set contains ANY of the specified permissions.
    /// </summary>
    public static bool HasAnyPermission(this PermissionFlags permissions, params PermissionFlags[] requiredPermissions)
    {
        foreach (var permission in requiredPermissions)
        {
            if ((permissions & permission) == permission)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if a permission set contains ALL of the specified permissions.
    /// </summary>
    public static bool HasAllPermissions(this PermissionFlags permissions, params PermissionFlags[] requiredPermissions)
    {
        foreach (var permission in requiredPermissions)
        {
            if ((permissions & permission) != permission)
                return false;
        }
        return true;
    }
}
