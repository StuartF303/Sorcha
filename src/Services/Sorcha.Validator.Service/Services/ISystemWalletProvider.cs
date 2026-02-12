// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Provides access to the system wallet for signing genesis dockets
/// </summary>
public interface ISystemWalletProvider
{
    /// <summary>
    /// Gets the system wallet ID/address for signing operations
    /// </summary>
    string? GetSystemWalletId();

    /// <summary>
    /// Sets the system wallet ID/address after creation
    /// </summary>
    void SetSystemWalletId(string walletId);

    /// <summary>
    /// Checks if the system wallet is initialized
    /// </summary>
    bool IsInitialized { get; }
}
