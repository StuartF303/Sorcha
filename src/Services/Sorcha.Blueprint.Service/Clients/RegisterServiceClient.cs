// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Sorcha.Register.Models;

namespace Sorcha.Blueprint.Service.Clients;

/// <summary>
/// HTTP client implementation for interacting with the Register Service
/// </summary>
public class RegisterServiceClient : IRegisterServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RegisterServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegisterServiceClient(
        HttpClient httpClient,
        ILogger<RegisterServiceClient> logger)
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
    public async Task<TransactionModel> SubmitTransactionAsync(
        string registerId,
        TransactionModel transaction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));
        }

        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        try
        {
            _logger.LogDebug("Submitting transaction {TxId} to register {RegisterId}",
                transaction.TxId, registerId);

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/registers/{registerId}/transactions",
                transaction,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TransactionModel>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Register Service returned invalid transaction response");
            }

            _logger.LogInformation("Successfully submitted transaction {TxId} to register {RegisterId}",
                result.TxId, registerId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to submit transaction {TxId} to register {RegisterId}",
                transaction.TxId, registerId);
            throw new InvalidOperationException($"Failed to submit transaction: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
        }

        try
        {
            _logger.LogDebug("Getting transaction {TxId} from register {RegisterId}",
                transactionId, registerId);

            var response = await _httpClient.GetAsync(
                $"/api/registers/{registerId}/transactions/{transactionId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Transaction {TxId} not found in register {RegisterId}",
                    transactionId, registerId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TransactionModel>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Successfully retrieved transaction {TxId} from register {RegisterId}",
                transactionId, registerId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get transaction {TxId} from register {RegisterId}",
                transactionId, registerId);
            throw new InvalidOperationException($"Failed to get transaction: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TransactionPage> GetTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));
        }

        if (page < 1)
        {
            throw new ArgumentException("Page must be greater than 0", nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }

        try
        {
            _logger.LogDebug("Getting transactions from register {RegisterId} (page {Page}, size {PageSize})",
                registerId, page, pageSize);

            var response = await _httpClient.GetAsync(
                $"/api/registers/{registerId}/transactions?page={page}&pageSize={pageSize}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TransactionPage>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Register Service returned invalid transaction page response");
            }

            _logger.LogInformation("Successfully retrieved {Count} transactions from register {RegisterId} (page {Page})",
                result.Transactions.Count, registerId, page);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get transactions from register {RegisterId}",
                registerId);
            throw new InvalidOperationException($"Failed to get transactions: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TransactionPage> GetTransactionsByWalletAsync(
        string registerId,
        string walletAddress,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));
        }

        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(walletAddress));
        }

        if (page < 1)
        {
            throw new ArgumentException("Page must be greater than 0", nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }

        try
        {
            _logger.LogDebug("Getting transactions for wallet {WalletAddress} from register {RegisterId} (page {Page}, size {PageSize})",
                walletAddress, registerId, page, pageSize);

            var response = await _httpClient.GetAsync(
                $"/api/query/wallets/{walletAddress}/transactions?registerId={registerId}&page={page}&pageSize={pageSize}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TransactionPage>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Register Service returned invalid transaction page response");
            }

            _logger.LogInformation("Successfully retrieved {Count} transactions for wallet {WalletAddress} from register {RegisterId} (page {Page})",
                result.Transactions.Count, walletAddress, registerId, page);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get transactions for wallet {WalletAddress} from register {RegisterId}",
                walletAddress, registerId);
            throw new InvalidOperationException($"Failed to get transactions by wallet: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Sorcha.Register.Models.Register?> GetRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("Register ID cannot be null or empty", nameof(registerId));
        }

        try
        {
            _logger.LogDebug("Getting register {RegisterId}", registerId);

            var response = await _httpClient.GetAsync(
                $"/api/registers/{registerId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Register {RegisterId} not found", registerId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Sorcha.Register.Models.Register>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Successfully retrieved register {RegisterId}", registerId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get register {RegisterId}", registerId);
            throw new InvalidOperationException($"Failed to get register: {ex.Message}", ex);
        }
    }
}
