// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Blazored.LocalStorage;
using Sorcha.UI.Core.Models.Wallet;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Manages the user's default wallet preference using browser local storage.
/// </summary>
public class WalletPreferenceService : IWalletPreferenceService
{
    private const string StorageKey = "sorcha:preferences:defaultWallet";
    private readonly ILocalStorageService _localStorage;

    public WalletPreferenceService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<string?> GetDefaultWalletAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync(StorageKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetDefaultWalletAsync(string walletAddress)
    {
        await _localStorage.SetItemAsStringAsync(StorageKey, walletAddress);
    }

    public async Task ClearDefaultWalletAsync()
    {
        await _localStorage.RemoveItemAsync(StorageKey);
    }

    public async Task<string?> GetSmartDefaultAsync(List<WalletDto> wallets)
    {
        if (wallets is not { Count: > 0 })
            return null;

        // Single wallet: auto-select
        if (wallets.Count == 1)
            return wallets[0].Address;

        // Multiple wallets: check stored preference
        var storedDefault = await GetDefaultWalletAsync();
        if (!string.IsNullOrEmpty(storedDefault))
        {
            // Verify the stored default is still in the wallet list
            if (wallets.Any(w => w.Address == storedDefault))
                return storedDefault;

            // Stored default no longer valid — clear it
            await ClearDefaultWalletAsync();
        }

        // Fallback: first wallet
        return wallets[0].Address;
    }
}
