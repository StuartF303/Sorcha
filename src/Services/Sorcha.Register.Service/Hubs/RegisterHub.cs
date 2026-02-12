// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.AspNetCore.SignalR;

namespace Sorcha.Register.Service.Hubs;

/// <summary>
/// SignalR hub for real-time register notifications
/// </summary>
public class RegisterHub : Hub<IRegisterHubClient>
{
    /// <summary>
    /// Subscribe to register updates
    /// </summary>
    public async Task SubscribeToRegister(string registerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"register:{registerId}");
    }

    /// <summary>
    /// Unsubscribe from register updates
    /// </summary>
    public async Task UnsubscribeFromRegister(string registerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"register:{registerId}");
    }

    /// <summary>
    /// Subscribe to all register events for a tenant
    /// </summary>
    public async Task SubscribeToTenant(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }

    /// <summary>
    /// Unsubscribe from tenant events
    /// </summary>
    public async Task UnsubscribeFromTenant(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Client methods that can be invoked by the hub
/// </summary>
public interface IRegisterHubClient
{
    Task RegisterCreated(string registerId, string name);
    Task RegisterDeleted(string registerId);
    Task RegisterStatusChanged(string registerId, string status);
    Task TransactionConfirmed(string registerId, string transactionId);
    Task DocketSealed(string registerId, ulong docketId, string hash);
    Task RegisterHeightUpdated(string registerId, uint newHeight);
}
