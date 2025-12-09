// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Sorcha.Demo.Services.Api;

/// <summary>
/// Client for Wallet Service API
/// </summary>
public class WalletApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public WalletApiClient(HttpClient httpClient, ILogger<WalletApiClient> logger, string baseUrl)
        : base(httpClient, logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Creates a new HD wallet
    /// </summary>
    public async Task<WalletResponse?> CreateWalletAsync(
        string name,
        string algorithm = "ED25519",
        CancellationToken ct = default)
    {
        var request = new
        {
            name,
            algorithm
        };

        return await PostAsync<object, WalletResponse>($"{_baseUrl}/wallets", request, ct);
    }

    /// <summary>
    /// Gets wallet details by address
    /// </summary>
    public async Task<WalletResponse?> GetWalletAsync(string address, CancellationToken ct = default)
    {
        return await GetAsync<WalletResponse>($"{_baseUrl}/wallets/{address}", ct);
    }

    /// <summary>
    /// Lists all wallets
    /// </summary>
    public async Task<List<WalletResponse>?> ListWalletsAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<WalletResponse>>($"{_baseUrl}/wallets", ct);
    }

    /// <summary>
    /// Signs data with a wallet
    /// </summary>
    public async Task<SignatureResponse?> SignAsync(
        string walletAddress,
        string dataToSign,
        CancellationToken ct = default)
    {
        var request = new
        {
            data = dataToSign
        };

        return await PostAsync<object, SignatureResponse>(
            $"{_baseUrl}/wallets/{walletAddress}/sign",
            request,
            ct);
    }

    /// <summary>
    /// Derives a child wallet (for delegation scenarios)
    /// </summary>
    public async Task<WalletResponse?> DeriveChildWalletAsync(
        string parentAddress,
        uint childIndex,
        string? name = null,
        CancellationToken ct = default)
    {
        var request = new
        {
            childIndex,
            name = name ?? $"Child-{childIndex}"
        };

        return await PostAsync<object, WalletResponse>(
            $"{_baseUrl}/wallets/{parentAddress}/derive",
            request,
            ct);
    }

    /// <summary>
    /// Checks Wallet Service health
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        return await base.CheckHealthAsync(_baseUrl, ct);
    }
}

/// <summary>
/// Response from wallet creation/retrieval
/// </summary>
public class WalletResponse
{
    public string Address { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string? PublicKey { get; set; }
    public string? Mnemonic { get; set; } // Only returned on creation
    public int? AccountIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response from signing operation
/// </summary>
public class SignatureResponse
{
    public string Signature { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
}
