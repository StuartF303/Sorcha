// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Sorcha.UI.Core.Models.Registers;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Manages SignalR connection to the Register Hub for real-time updates.
/// </summary>
public class RegisterHubConnection : IAsyncDisposable
{
    private readonly ILogger<RegisterHubConnection> _logger;
    private readonly string _hubUrl;
    private HubConnection? _hubConnection;
    private ConnectionState _connectionState = new();
    private readonly HashSet<string> _subscribedRegisters = [];
    private string? _subscribedTenant;

    /// <summary>
    /// Event raised when a new transaction is confirmed.
    /// </summary>
    public event Func<string, string, Task>? OnTransactionConfirmed;

    /// <summary>
    /// Event raised when a register is created.
    /// </summary>
    public event Func<string, string, Task>? OnRegisterCreated;

    /// <summary>
    /// Event raised when a register is deleted.
    /// </summary>
    public event Func<string, Task>? OnRegisterDeleted;

    /// <summary>
    /// Event raised when a docket is sealed (new block).
    /// </summary>
    public event Func<string, ulong, string, Task>? OnDocketSealed;

    /// <summary>
    /// Event raised when register height is updated.
    /// </summary>
    public event Func<string, uint, Task>? OnRegisterHeightUpdated;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    public event Action<ConnectionState>? OnConnectionStateChanged;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState ConnectionState => _connectionState;

    public RegisterHubConnection(string baseUrl, ILogger<RegisterHubConnection> logger)
    {
        _hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/register";
        _logger = logger;
    }

    /// <summary>
    /// Starts the SignalR connection.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null)
        {
            return;
        }

        UpdateConnectionState(ConnectionStatus.Connecting);

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            // Register event handlers
            _hubConnection.On<string, string>("TransactionConfirmed", async (registerId, txId) =>
            {
                _logger.LogDebug("Transaction confirmed: {TxId} in register {RegisterId}", txId, registerId);
                if (OnTransactionConfirmed != null)
                {
                    await OnTransactionConfirmed(registerId, txId);
                }
            });

            _hubConnection.On<string, string>("RegisterCreated", async (registerId, name) =>
            {
                _logger.LogDebug("Register created: {RegisterId} ({Name})", registerId, name);
                if (OnRegisterCreated != null)
                {
                    await OnRegisterCreated(registerId, name);
                }
            });

            _hubConnection.On<string>("RegisterDeleted", async (registerId) =>
            {
                _logger.LogDebug("Register deleted: {RegisterId}", registerId);
                if (OnRegisterDeleted != null)
                {
                    await OnRegisterDeleted(registerId);
                }
            });

            _hubConnection.On<string, ulong, string>("DocketSealed", async (registerId, docketId, hash) =>
            {
                _logger.LogDebug("Docket sealed: {DocketId} in register {RegisterId}", docketId, registerId);
                if (OnDocketSealed != null)
                {
                    await OnDocketSealed(registerId, docketId, hash);
                }
            });

            _hubConnection.On<string, uint>("RegisterHeightUpdated", async (registerId, newHeight) =>
            {
                _logger.LogDebug("Register height updated: {RegisterId} -> {Height}", registerId, newHeight);
                if (OnRegisterHeightUpdated != null)
                {
                    await OnRegisterHeightUpdated(registerId, newHeight);
                }
            });

            // Handle connection lifecycle
            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("SignalR reconnecting: {Error}", error?.Message);
                UpdateConnectionState(ConnectionStatus.Reconnecting, error?.Message);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async connectionId =>
            {
                _logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
                UpdateConnectionState(ConnectionStatus.Connected);

                // Re-subscribe to registers and tenant
                await ResubscribeAsync();
            };

            _hubConnection.Closed += error =>
            {
                _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
                UpdateConnectionState(ConnectionStatus.Disconnected, error?.Message);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync(cancellationToken);

            UpdateConnectionState(ConnectionStatus.Connected);
            _logger.LogInformation("SignalR connected to {HubUrl}", _hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub at {HubUrl}", _hubUrl);
            UpdateConnectionState(ConnectionStatus.Disconnected, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Stops the SignalR connection.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            return;
        }

        try
        {
            await _hubConnection.StopAsync(cancellationToken);
            UpdateConnectionState(ConnectionStatus.Disconnected);
            _logger.LogInformation("SignalR disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping SignalR connection");
        }
    }

    /// <summary>
    /// Subscribes to updates for a specific register.
    /// </summary>
    public async Task SubscribeToRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe to register {RegisterId}: not connected", registerId);
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("SubscribeToRegister", registerId, cancellationToken);
            _subscribedRegisters.Add(registerId);
            _logger.LogDebug("Subscribed to register {RegisterId}", registerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Unsubscribes from updates for a specific register.
    /// </summary>
    public async Task UnsubscribeFromRegisterAsync(string registerId, CancellationToken cancellationToken = default)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromRegister", registerId, cancellationToken);
            _subscribedRegisters.Remove(registerId);
            _logger.LogDebug("Unsubscribed from register {RegisterId}", registerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Subscribes to tenant-wide events.
    /// </summary>
    public async Task SubscribeToTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe to tenant {TenantId}: not connected", tenantId);
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("SubscribeToTenant", tenantId, cancellationToken);
            _subscribedTenant = tenantId;
            _logger.LogDebug("Subscribed to tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Unsubscribes from tenant-wide events.
    /// </summary>
    public async Task UnsubscribeFromTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromTenant", tenantId, cancellationToken);
            _subscribedTenant = null;
            _logger.LogDebug("Unsubscribed from tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from tenant {TenantId}", tenantId);
        }
    }

    private async Task ResubscribeAsync()
    {
        // Re-subscribe to all previously subscribed registers
        foreach (var registerId in _subscribedRegisters.ToList())
        {
            try
            {
                await _hubConnection!.InvokeAsync("SubscribeToRegister", registerId);
                _logger.LogDebug("Re-subscribed to register {RegisterId}", registerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to re-subscribe to register {RegisterId}", registerId);
            }
        }

        // Re-subscribe to tenant if applicable
        if (!string.IsNullOrEmpty(_subscribedTenant))
        {
            try
            {
                await _hubConnection!.InvokeAsync("SubscribeToTenant", _subscribedTenant);
                _logger.LogDebug("Re-subscribed to tenant {TenantId}", _subscribedTenant);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to re-subscribe to tenant {TenantId}", _subscribedTenant);
            }
        }
    }

    private void UpdateConnectionState(ConnectionStatus status, string? errorMessage = null)
    {
        var newState = status switch
        {
            ConnectionStatus.Connected => new ConnectionState
            {
                Status = status,
                LastConnected = DateTime.UtcNow,
                ReconnectAttempts = 0
            },
            ConnectionStatus.Reconnecting => _connectionState with
            {
                Status = status,
                ReconnectAttempts = _connectionState.ReconnectAttempts + 1,
                ErrorMessage = errorMessage
            },
            _ => new ConnectionState
            {
                Status = status,
                LastConnected = _connectionState.LastConnected,
                ReconnectAttempts = _connectionState.ReconnectAttempts,
                ErrorMessage = errorMessage
            }
        };

        _connectionState = newState;
        OnConnectionStateChanged?.Invoke(newState);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        GC.SuppressFinalize(this);
    }
}
