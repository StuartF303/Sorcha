// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Cli.Configuration;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// Client for the Wallet Service API
/// </summary>
public class WalletApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public WalletApiClient(HttpClient httpClient, ActivityLog activityLog)
        : base(httpClient, activityLog)
    {
        _baseUrl = TestCredentials.WalletServiceUrl;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        return await CheckHealthAsync(_baseUrl, ct);
    }

    public async Task<WalletDto?> CreateWalletAsync(CreateWalletRequest request, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<CreateWalletRequest, WalletDto>(
            $"{_baseUrl}/api/v1/wallets", request, ct);
    }

    public async Task<List<WalletDto>?> ListWalletsAsync(CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<WalletDto>>($"{_baseUrl}/api/v1/wallets", ct);
    }

    public async Task<WalletDto?> GetWalletAsync(string walletAddress, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<WalletDto>($"{_baseUrl}/api/v1/wallets/{walletAddress}", ct);
    }

    public async Task<SignatureResponse?> SignDataAsync(
        string walletAddress,
        SignRequest request,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<SignRequest, SignatureResponse>(
            $"{_baseUrl}/api/v1/wallets/{walletAddress}/sign", request, ct);
    }

    public async Task<List<AddressDto>?> ListAddressesAsync(
        string walletAddress,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<AddressDto>>($"{_baseUrl}/api/v1/wallets/{walletAddress}/addresses", ct);
    }

    public async Task<AddressDto?> RegisterAddressAsync(
        string walletAddress,
        RegisterAddressRequest request,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<RegisterAddressRequest, AddressDto>(
            $"{_baseUrl}/api/v1/wallets/{walletAddress}/addresses", request, ct);
    }

    public async Task<HttpResponseMessage> DeleteWalletAsync(string walletAddress, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await DeleteAsync($"{_baseUrl}/api/v1/wallets/{walletAddress}", ct);
    }
}

// DTOs for Wallet Service
public record WalletDto(
    string Address,
    string Name,
    string Algorithm,
    string PublicKey,
    DateTime CreatedAt,
    int AddressCount
);

public record CreateWalletRequest(
    string Name,
    string Algorithm = "ED25519",
    int WordCount = 24,
    string? Passphrase = null
);

public record SignRequest(
    string Data // Base64 encoded
);

public record SignatureResponse(
    string Signature, // Base64 encoded
    string PublicKey
);

public record AddressDto(
    Guid Id,
    string Address,
    string Type,
    int AccountIndex,
    int AddressIndex,
    bool IsUsed,
    string? Label,
    DateTime CreatedAt
);

public record RegisterAddressRequest(
    string Address,
    string Type,
    int AccountIndex,
    int AddressIndex,
    string? Label = null
);
