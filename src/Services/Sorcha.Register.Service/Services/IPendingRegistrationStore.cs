// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Models;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// Manages in-memory storage of pending register creations
/// </summary>
/// <remarks>
/// This service is registered as a singleton to maintain state across requests.
/// TODO: Replace with Redis-backed storage for production multi-instance deployments
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
