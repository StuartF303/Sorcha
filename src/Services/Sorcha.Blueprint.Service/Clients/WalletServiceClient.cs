// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;

namespace Sorcha.Blueprint.Service.Clients;

/// <summary>
/// HTTP client implementation for interacting with the Wallet Service
/// </summary>
public class WalletServiceClient : IWalletServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WalletServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WalletServiceClient(
        HttpClient httpClient,
        ILogger<WalletServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    public async Task<byte[]> EncryptPayloadAsync(
        string recipientWalletAddress,
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientWalletAddress))
        {
            throw new ArgumentException("Recipient wallet address cannot be null or empty", nameof(recipientWalletAddress));
        }

        if (payload == null || payload.Length == 0)
        {
            throw new ArgumentException("Payload cannot be null or empty", nameof(payload));
        }

        try
        {
            var request = new
            {
                Payload = Convert.ToBase64String(payload),
                RecipientAddress = recipientWalletAddress
            };

            _logger.LogDebug("Encrypting payload for wallet {WalletAddress} ({PayloadSize} bytes)",
                recipientWalletAddress, payload.Length);

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{recipientWalletAddress}/encrypt",
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EncryptPayloadResponse>(_jsonOptions, cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.EncryptedPayload))
            {
                throw new InvalidOperationException("Wallet Service returned invalid encryption response");
            }

            var encryptedPayload = Convert.FromBase64String(result.EncryptedPayload);

            _logger.LogInformation("Successfully encrypted payload for wallet {WalletAddress} ({EncryptedSize} bytes)",
                recipientWalletAddress, encryptedPayload.Length);

            return encryptedPayload;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload for wallet {WalletAddress}", recipientWalletAddress);
            throw new InvalidOperationException($"Failed to encrypt payload: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> DecryptPayloadAsync(
        string walletAddress,
        byte[] encryptedPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(walletAddress));
        }

        if (encryptedPayload == null || encryptedPayload.Length == 0)
        {
            throw new ArgumentException("Encrypted payload cannot be null or empty", nameof(encryptedPayload));
        }

        try
        {
            var request = new
            {
                EncryptedPayload = Convert.ToBase64String(encryptedPayload)
            };

            _logger.LogDebug("Decrypting payload with wallet {WalletAddress} ({PayloadSize} bytes)",
                walletAddress, encryptedPayload.Length);

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{walletAddress}/decrypt",
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DecryptPayloadResponse>(_jsonOptions, cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.DecryptedPayload))
            {
                throw new InvalidOperationException("Wallet Service returned invalid decryption response");
            }

            var decryptedPayload = Convert.FromBase64String(result.DecryptedPayload);

            _logger.LogInformation("Successfully decrypted payload with wallet {WalletAddress} ({DecryptedSize} bytes)",
                walletAddress, decryptedPayload.Length);

            return decryptedPayload;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload with wallet {WalletAddress}", walletAddress);
            throw new InvalidOperationException($"Failed to decrypt payload: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> DecryptWithDelegationAsync(
        string walletAddress,
        byte[] encryptedPayload,
        string delegationToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(walletAddress));
        }

        if (encryptedPayload == null || encryptedPayload.Length == 0)
        {
            throw new ArgumentException("Encrypted payload cannot be null or empty", nameof(encryptedPayload));
        }

        if (string.IsNullOrWhiteSpace(delegationToken))
        {
            throw new ArgumentException("Delegation token cannot be null or empty", nameof(delegationToken));
        }

        try
        {
            var request = new
            {
                EncryptedPayload = Convert.ToBase64String(encryptedPayload)
            };

            _logger.LogDebug("Decrypting payload with delegated access for wallet {WalletAddress} ({PayloadSize} bytes)",
                walletAddress, encryptedPayload.Length);

            // Create request with delegation token header
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/wallets/{walletAddress}/decrypt")
            {
                Content = JsonContent.Create(request, options: _jsonOptions)
            };
            httpRequest.Headers.Add("X-Delegation-Token", delegationToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DecryptPayloadResponse>(_jsonOptions, cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.DecryptedPayload))
            {
                throw new InvalidOperationException("Wallet Service returned invalid decryption response");
            }

            var decryptedPayload = Convert.FromBase64String(result.DecryptedPayload);

            _logger.LogInformation("Successfully decrypted payload with delegated access for wallet {WalletAddress} ({DecryptedSize} bytes)",
                walletAddress, decryptedPayload.Length);

            return decryptedPayload;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload with delegated access for wallet {WalletAddress}", walletAddress);
            throw new InvalidOperationException($"Failed to decrypt payload with delegation: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(walletAddress));
        }

        if (transactionData == null || transactionData.Length == 0)
        {
            throw new ArgumentException("Transaction data cannot be null or empty", nameof(transactionData));
        }

        try
        {
            var request = new
            {
                TransactionData = Convert.ToBase64String(transactionData)
            };

            _logger.LogDebug("Signing transaction with wallet {WalletAddress}", walletAddress);

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{walletAddress}/sign",
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SignTransactionResponse>(_jsonOptions, cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.Signature))
            {
                throw new InvalidOperationException("Wallet Service returned invalid signature response");
            }

            var signature = Convert.FromBase64String(result.Signature);

            _logger.LogInformation("Successfully signed transaction with wallet {WalletAddress}", walletAddress);

            return signature;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to sign transaction with wallet {WalletAddress}", walletAddress);
            throw new InvalidOperationException($"Failed to sign transaction: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletInfo?> GetWalletAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(walletAddress));
        }

        try
        {
            _logger.LogDebug("Getting wallet information for {WalletAddress}", walletAddress);

            var response = await _httpClient.GetAsync(
                $"/api/v1/wallets/{walletAddress}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Wallet {WalletAddress} not found", walletAddress);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WalletInfo>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Successfully retrieved wallet information for {WalletAddress}", walletAddress);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get wallet {WalletAddress}", walletAddress);
            throw new InvalidOperationException($"Failed to get wallet: {ex.Message}", ex);
        }
    }

    // Internal DTOs for API communication
    private class EncryptPayloadResponse
    {
        public required string EncryptedPayload { get; set; }
        public required string RecipientAddress { get; set; }
        public DateTime EncryptedAt { get; set; }
    }

    private class DecryptPayloadResponse
    {
        public required string DecryptedPayload { get; set; }
        public required string DecryptedBy { get; set; }
        public DateTime DecryptedAt { get; set; }
    }

    private class SignTransactionResponse
    {
        public required string Signature { get; set; }
        public required string SignedBy { get; set; }
        public DateTime SignedAt { get; set; }
    }
}
