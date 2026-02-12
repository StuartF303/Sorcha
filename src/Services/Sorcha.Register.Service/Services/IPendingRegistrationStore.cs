// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Register.Models;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// Manages storage of pending register creations
/// </summary>
/// <remarks>
/// Backed by Redis for production multi-instance deployments.
/// Registered as singleton to maintain consistent access across requests.
/// </remarks>
public interface IPendingRegistrationStore
{
    /// <summary>
    /// Adds a pending registration
    /// </summary>
    void Add(string registerId, PendingRegistration registration);

    /// <summary>
    /// Tries to retrieve and remove a pending registration
    /// </summary>
    bool TryRemove(string registerId, out PendingRegistration? registration);

    /// <summary>
    /// Checks if a pending registration exists
    /// </summary>
    bool Exists(string registerId);

    /// <summary>
    /// Removes expired pending registrations
    /// </summary>
    void CleanupExpired();
}
