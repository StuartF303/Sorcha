// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Register.Models;
using Sorcha.UI.Core.Models.Registers;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// HTTP client implementation for Transaction API operations.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TransactionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TransactionService(HttpClient httpClient, ILogger<TransactionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TransactionListResponse> GetTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/registers/{Uri.EscapeDataString(registerId)}/transactions?page={page}&pageSize={pageSize}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch transactions for register {RegisterId}: {StatusCode}",
                    registerId, response.StatusCode);
                return new TransactionListResponse();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiTransactionListResponse>(
                JsonOptions, cancellationToken);

            if (apiResponse == null)
            {
                return new TransactionListResponse();
            }

            return new TransactionListResponse
            {
                Page = apiResponse.Page,
                PageSize = apiResponse.PageSize,
                Total = apiResponse.Total,
                Transactions = apiResponse.Transactions?.Select(MapToViewModel).ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions for register {RegisterId}", registerId);
            return new TransactionListResponse();
        }
    }

    /// <inheritdoc />
    public async Task<TransactionViewModel?> GetTransactionAsync(
        string registerId,
        string txId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/registers/{Uri.EscapeDataString(registerId)}/transactions/{Uri.EscapeDataString(txId)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to fetch transaction {TxId} in register {RegisterId}: {StatusCode}",
                    txId, registerId, response.StatusCode);
                return null;
            }

            var transaction = await response.Content.ReadFromJsonAsync<TransactionModel>(
                JsonOptions, cancellationToken);

            return transaction != null ? MapToViewModel(transaction) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction {TxId} in register {RegisterId}", txId, registerId);
            return null;
        }
    }

    private static TransactionViewModel MapToViewModel(TransactionModel transaction)
    {
        return new TransactionViewModel
        {
            TxId = transaction.TxId,
            RegisterId = transaction.RegisterId,
            SenderWallet = transaction.SenderWallet,
            RecipientsWallets = transaction.RecipientsWallets?.ToList() ?? [],
            TimeStamp = transaction.TimeStamp,
            BlockNumber = transaction.BlockNumber,
            PayloadCount = transaction.PayloadCount,
            Signature = transaction.Signature,
            PrevTxId = transaction.PrevTxId,
            Version = transaction.Version,
            BlueprintId = transaction.MetaData?.BlueprintId,
            InstanceId = transaction.MetaData?.InstanceId,
            ActionId = transaction.MetaData?.ActionId
        };
    }

    /// <summary>
    /// API response model for transaction list endpoint
    /// </summary>
    private record ApiTransactionListResponse
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public List<TransactionModel>? Transactions { get; init; }
    }
}
