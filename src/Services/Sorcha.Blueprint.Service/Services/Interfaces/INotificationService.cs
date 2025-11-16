// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Service.Hubs;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for broadcasting real-time notifications via SignalR.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notify a wallet that a new action is available.
    /// </summary>
    /// <param name="notification">The action notification</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyActionAvailableAsync(ActionNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Notify a wallet that an action has been confirmed.
    /// </summary>
    /// <param name="notification">The action notification</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyActionConfirmedAsync(ActionNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Notify a wallet that an action has been rejected.
    /// </summary>
    /// <param name="notification">The action notification</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyActionRejectedAsync(ActionNotification notification, CancellationToken ct = default);
}
