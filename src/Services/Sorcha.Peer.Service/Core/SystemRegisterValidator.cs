// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validator for system register ID
/// </summary>
public static class SystemRegisterValidator
{
    /// <summary>
    /// Well-known system register ID (00000000-0000-0000-0000-000000000000)
    /// </summary>
    public static readonly Guid SystemRegisterId = PeerServiceConstants.SystemRegisterId;

    /// <summary>
    /// Checks if a register ID is the system register
    /// </summary>
    /// <param name="registerId">Register ID to check</param>
    /// <returns>True if this is the system register ID</returns>
    public static bool IsSystemRegister(Guid registerId)
    {
        return registerId == SystemRegisterId;
    }

    /// <summary>
    /// Validates that a register ID is the system register ID
    /// </summary>
    /// <param name="registerId">Register ID to validate</param>
    /// <exception cref="ArgumentException">Thrown when register ID is not the system register ID</exception>
    public static void ValidateSystemRegisterId(Guid registerId)
    {
        if (registerId != SystemRegisterId)
        {
            throw new ArgumentException(
                $"Invalid system register ID: {registerId}. Must be {SystemRegisterId} (Guid.Empty)",
                nameof(registerId));
        }
    }
}
