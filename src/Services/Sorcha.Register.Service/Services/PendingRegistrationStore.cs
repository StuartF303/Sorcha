// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using Sorcha.Register.Models;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// In-memory implementation of pending registration storage
/// </summary>
/// <remarks>
/// Uses ConcurrentDictionary for thread-safe access.
/// Registered as singleton to maintain state across requests.
/// TODO: Replace with Redis-backed storage for production multi-instance deployments
/// </remarks>
public class PendingRegistrationStore : IPendingRegistrationStore
{
    private readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();
    private readonly ILogger<PendingRegistrationStore> _logger;

    public PendingRegistrationStore(ILogger<PendingRegistrationStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Add(string registerId, PendingRegistration registration)
    {
        if (string.IsNullOrEmpty(registerId))
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));

        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

        if (!_pendingRegistrations.TryAdd(registerId, registration))
        {
            _logger.LogWarning("Failed to add pending registration for ID {RegisterId} - already exists", registerId);
        }
        else
        {
            _logger.LogDebug("Added pending registration for ID {RegisterId}", registerId);
        }
    }

    /// <inheritdoc />
    public bool TryRemove(string registerId, out PendingRegistration? registration)
    {
        if (string.IsNullOrEmpty(registerId))
        {
            registration = null;
            return false;
        }

        var success = _pendingRegistrations.TryRemove(registerId, out registration);

        if (success)
        {
            _logger.LogDebug("Removed pending registration for ID {RegisterId}", registerId);
        }

        return success;
    }

    /// <inheritdoc />
    public bool Exists(string registerId)
    {
        if (string.IsNullOrEmpty(registerId))
            return false;

        return _pendingRegistrations.ContainsKey(registerId);
    }

    /// <inheritdoc />
    public void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _pendingRegistrations
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            if (_pendingRegistrations.TryRemove(key, out _))
            {
                _logger.LogInformation("Removed expired pending registration {RegisterId}", key);
            }
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired pending registrations", expiredKeys.Count);
        }
    }
}
