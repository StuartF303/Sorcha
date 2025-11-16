// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.SignalR;

namespace Sorcha.Blueprint.Service.Hubs;

/// <summary>
/// SignalR hub for real-time action notifications.
/// </summary>
/// <remarks>
/// This hub enables real-time communication between the Blueprint Service and clients
/// for action availability, confirmation, and rejection notifications.
///
/// Connection URL: /actionshub
/// Authentication: JWT token via query parameter ?access_token={jwt}
///
/// Client Methods (called by server):
/// - ActionAvailable(notification): Notifies client when a new action is available
/// - ActionConfirmed(notification): Notifies client when an action is confirmed
/// - ActionRejected(notification): Notifies client when an action is rejected
///
/// Server Methods (called by clients):
/// - SubscribeToWallet(walletAddress): Subscribe to notifications for a wallet
/// - UnsubscribeFromWallet(walletAddress): Unsubscribe from wallet notifications
/// </remarks>
public class ActionsHub : Hub
{
    private readonly ILogger<ActionsHub> _logger;

    public ActionsHub(ILogger<ActionsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userIdentifier = Context.UserIdentifier;

        _logger.LogInformation(
            "Client connected to ActionsHub. ConnectionId: {ConnectionId}, User: {User}",
            connectionId,
            userIdentifier ?? "anonymous");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userIdentifier = Context.UserIdentifier;

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected from ActionsHub with error. ConnectionId: {ConnectionId}, User: {User}",
                connectionId,
                userIdentifier ?? "anonymous");
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected from ActionsHub. ConnectionId: {ConnectionId}, User: {User}",
                connectionId,
                userIdentifier ?? "anonymous");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to notifications for a specific wallet address.
    /// </summary>
    /// <param name="walletAddress">The wallet address to subscribe to</param>
    public async Task SubscribeToWallet(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new HubException("Wallet address cannot be empty");
        }

        var groupName = GetWalletGroupName(walletAddress);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to wallet notifications. ConnectionId: {ConnectionId}, Wallet: {Wallet}",
            Context.ConnectionId,
            walletAddress);
    }

    /// <summary>
    /// Unsubscribe from notifications for a specific wallet address.
    /// </summary>
    /// <param name="walletAddress">The wallet address to unsubscribe from</param>
    public async Task UnsubscribeFromWallet(string walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new HubException("Wallet address cannot be empty");
        }

        var groupName = GetWalletGroupName(walletAddress);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from wallet notifications. ConnectionId: {ConnectionId}, Wallet: {Wallet}",
            Context.ConnectionId,
            walletAddress);
    }

    /// <summary>
    /// Get the SignalR group name for a wallet address.
    /// </summary>
    private static string GetWalletGroupName(string walletAddress)
    {
        return $"wallet:{walletAddress}";
    }
}

/// <summary>
/// Action notification sent to clients
/// </summary>
public record ActionNotification
{
    public required string TransactionHash { get; init; }
    public required string WalletAddress { get; init; }
    public required string RegisterAddress { get; init; }
    public string? BlueprintId { get; init; }
    public string? ActionId { get; init; }
    public string? InstanceId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? Message { get; init; }
}
