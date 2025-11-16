// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.SignalR;
using Sorcha.Blueprint.Service.Hubs;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services.Implementation;

/// <summary>
/// Service for broadcasting real-time notifications via SignalR.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<ActionsHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<ActionsHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notify a wallet that a new action is available.
    /// </summary>
    public async Task NotifyActionAvailableAsync(ActionNotification notification, CancellationToken ct = default)
    {
        try
        {
            var groupName = GetWalletGroupName(notification.WalletAddress);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ActionAvailable", notification, ct);

            _logger.LogInformation(
                "Sent ActionAvailable notification to wallet {Wallet}. Transaction: {TxHash}",
                notification.WalletAddress,
                notification.TransactionHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send ActionAvailable notification to wallet {Wallet}",
                notification.WalletAddress);
            throw;
        }
    }

    /// <summary>
    /// Notify a wallet that an action has been confirmed.
    /// </summary>
    public async Task NotifyActionConfirmedAsync(ActionNotification notification, CancellationToken ct = default)
    {
        try
        {
            var groupName = GetWalletGroupName(notification.WalletAddress);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ActionConfirmed", notification, ct);

            _logger.LogInformation(
                "Sent ActionConfirmed notification to wallet {Wallet}. Transaction: {TxHash}",
                notification.WalletAddress,
                notification.TransactionHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send ActionConfirmed notification to wallet {Wallet}",
                notification.WalletAddress);
            throw;
        }
    }

    /// <summary>
    /// Notify a wallet that an action has been rejected.
    /// </summary>
    public async Task NotifyActionRejectedAsync(ActionNotification notification, CancellationToken ct = default)
    {
        try
        {
            var groupName = GetWalletGroupName(notification.WalletAddress);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ActionRejected", notification, ct);

            _logger.LogInformation(
                "Sent ActionRejected notification to wallet {Wallet}. Transaction: {TxHash}",
                notification.WalletAddress,
                notification.TransactionHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send ActionRejected notification to wallet {Wallet}",
                notification.WalletAddress);
            throw;
        }
    }

    /// <summary>
    /// Get the SignalR group name for a wallet address.
    /// </summary>
    private static string GetWalletGroupName(string walletAddress)
    {
        return $"wallet:{walletAddress}";
    }
}
