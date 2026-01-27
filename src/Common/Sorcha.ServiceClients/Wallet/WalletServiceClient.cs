// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Auth;

namespace Sorcha.ServiceClients.Wallet;

/// <summary>
/// HTTP client for Wallet Service operations
/// </summary>
public class WalletServiceClient : IWalletServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceAuthClient _serviceAuth;
    private readonly ILogger<WalletServiceClient> _logger;
    private readonly string _serviceAddress;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WalletServiceClient(
        HttpClient httpClient,
        IServiceAuthClient serviceAuth,
        IConfiguration configuration,
        ILogger<WalletServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviceAuth = serviceAuth ?? throw new ArgumentNullException(nameof(serviceAuth));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serviceAddress = configuration["ServiceClients:WalletService:Address"]
            ?? configuration["GrpcClients:WalletService:Address"]
            ?? "http://wallet-service";

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_serviceAddress);
        }

        _logger.LogInformation(
            "WalletServiceClient initialized (Address: {Address})", _serviceAddress);
    }

    // =========================================================================
    // System Wallet Operations
    // =========================================================================

    public async Task<string> CreateOrRetrieveSystemWalletAsync(
        string validatorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating or retrieving system wallet for validator {ValidatorId}", validatorId);

            await SetAuthHeaderAsync(cancellationToken);

            var request = new { validatorId };
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/wallets/system", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SystemWalletResponse>(cancellationToken);
            return result?.Address ?? throw new InvalidOperationException("System wallet response missing address");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/retrieve system wallet for validator {ValidatorId}", validatorId);
            throw;
        }
    }

    // =========================================================================
    // Signing Operations
    // =========================================================================

    public async Task<WalletSignResult> SignDataAsync(
        string walletId,
        string dataToSign,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Signing data with wallet {WalletId}", walletId);

            // Convert hex data to bytes and sign as pre-hashed
            var dataBytes = Convert.FromHexString(dataToSign);
            return await SignTransactionAsync(walletId, dataBytes, isPreHashed: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign data with wallet {WalletId}", walletId);
            throw;
        }
    }

    public async Task<WalletSignResult> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        string? derivationPath = null,
        bool isPreHashed = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Signing transaction with wallet {WalletAddress} (preHashed: {IsPreHashed}, path: {DerivationPath})",
                walletAddress, isPreHashed, derivationPath ?? "default");

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new SignRequest
            {
                TransactionData = Convert.ToBase64String(transactionData),
                DerivationPath = derivationPath,
                IsPreHashed = isPreHashed
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{walletAddress}/sign", requestBody, JsonOptions, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Wallet {walletAddress} not found");
            }

            response.EnsureSuccessStatusCode();

            var signResponse = await response.Content.ReadFromJsonAsync<SignResponse>(cancellationToken);
            if (signResponse is null)
            {
                throw new InvalidOperationException("Sign response was null");
            }

            return new WalletSignResult
            {
                Signature = Convert.FromBase64String(signResponse.Signature),
                PublicKey = Convert.FromBase64String(signResponse.PublicKey),
                SignedBy = signResponse.SignedBy,
                Algorithm = signResponse.Algorithm ?? "ED25519"
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex, "Authentication failed when signing with wallet {WalletAddress}", walletAddress);
            throw new InvalidOperationException($"Authentication failed for wallet signing: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to sign transaction with wallet {WalletAddress}", walletAddress);
            throw;
        }
    }

    public async Task<bool> VerifySignatureAsync(
        string publicKey,
        string data,
        string signature,
        string algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying signature with public key");

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new { publicKey, data, signature, algorithm };
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/wallets/verify", requestBody, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken);
            return result?.IsValid ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature");
            return false;
        }
    }

    // =========================================================================
    // Encryption Operations
    // =========================================================================

    public async Task<byte[]> EncryptPayloadAsync(
        string recipientWalletAddress,
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Encrypting payload for wallet {WalletAddress}", recipientWalletAddress);

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new { payload = Convert.ToBase64String(payload) };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{recipientWalletAddress}/encrypt", requestBody, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EncryptResponse>(cancellationToken);
            return Convert.FromBase64String(result?.EncryptedPayload
                ?? throw new InvalidOperationException("Encrypt response missing payload"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload for wallet {WalletAddress}", recipientWalletAddress);
            throw;
        }
    }

    public async Task<byte[]> DecryptPayloadAsync(
        string walletAddress,
        byte[] encryptedPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Decrypting payload with wallet {WalletAddress}", walletAddress);

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new { encryptedPayload = Convert.ToBase64String(encryptedPayload) };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{walletAddress}/decrypt", requestBody, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DecryptResponse>(cancellationToken);
            return Convert.FromBase64String(result?.DecryptedPayload
                ?? throw new InvalidOperationException("Decrypt response missing payload"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload with wallet {WalletAddress}", walletAddress);
            throw;
        }
    }

    public async Task<byte[]> DecryptWithDelegationAsync(
        string walletAddress,
        byte[] encryptedPayload,
        string delegationToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Decrypting payload with delegation for wallet {WalletAddress}", walletAddress);

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new
            {
                encryptedPayload = Convert.ToBase64String(encryptedPayload),
                delegationToken
            };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/wallets/{walletAddress}/decrypt", requestBody, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DecryptResponse>(cancellationToken);
            return Convert.FromBase64String(result?.DecryptedPayload
                ?? throw new InvalidOperationException("Decrypt response missing payload"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload with delegation for wallet {WalletAddress}", walletAddress);
            throw;
        }
    }

    // =========================================================================
    // Wallet Management
    // =========================================================================

    public async Task<WalletInfo?> GetWalletAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting wallet info for {WalletAddress}", walletAddress);

            await SetAuthHeaderAsync(cancellationToken);

            var response = await _httpClient.GetAsync(
                $"/api/v1/wallets/{walletAddress}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WalletInfo>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallet info for {WalletAddress}", walletAddress);
            return null;
        }
    }

    public async Task<WalletInfo> CreateWalletAsync(
        string name,
        string algorithm,
        string owner,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating wallet {Name} with algorithm {Algorithm}", name, algorithm);

            await SetAuthHeaderAsync(cancellationToken);

            var requestBody = new { name, algorithm, owner, tenant };
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/wallets", requestBody, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<WalletInfo>(cancellationToken)
                ?? throw new InvalidOperationException("Create wallet response was null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wallet {Name}", name);
            throw;
        }
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private async Task SetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        var token = await _serviceAuth.GetTokenAsync(cancellationToken);
        if (token is not null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("No auth token available for Wallet Service call");
        }
    }

    // =========================================================================
    // Response DTOs
    // =========================================================================

    private sealed class SystemWalletResponse
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }

    private sealed class SignRequest
    {
        [JsonPropertyName("transactionData")]
        public string TransactionData { get; set; } = string.Empty;

        [JsonPropertyName("derivationPath")]
        public string? DerivationPath { get; set; }

        [JsonPropertyName("isPreHashed")]
        public bool IsPreHashed { get; set; }
    }

    private sealed class SignResponse
    {
        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;

        [JsonPropertyName("signedBy")]
        public string SignedBy { get; set; } = string.Empty;

        [JsonPropertyName("signedAt")]
        public DateTime SignedAt { get; set; }

        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; } = string.Empty;

        [JsonPropertyName("algorithm")]
        public string? Algorithm { get; set; }
    }

    private sealed class VerifyResponse
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }
    }

    private sealed class EncryptResponse
    {
        [JsonPropertyName("encryptedPayload")]
        public string? EncryptedPayload { get; set; }
    }

    private sealed class DecryptResponse
    {
        [JsonPropertyName("decryptedPayload")]
        public string? DecryptedPayload { get; set; }
    }
}
