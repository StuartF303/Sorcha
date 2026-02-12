// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;

namespace Sorcha.Demo.Services.Api;

/// <summary>
/// Client for Register Service API
/// </summary>
public class RegisterApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public RegisterApiClient(HttpClient httpClient, ILogger<RegisterApiClient> logger, string baseUrl)
        : base(httpClient, logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Creates a new register
    /// </summary>
    public async Task<RegisterResponse?> CreateRegisterAsync(
        string name,
        string tenantId = "demo-tenant",
        CancellationToken ct = default)
    {
        var request = new
        {
            name,
            tenantId
        };

        return await PostAsync<object, RegisterResponse>($"{_baseUrl}/registers", request, ct);
    }

    /// <summary>
    /// Gets a register by ID
    /// </summary>
    public async Task<RegisterResponse?> GetRegisterAsync(string registerId, CancellationToken ct = default)
    {
        return await GetAsync<RegisterResponse>($"{_baseUrl}/registers/{registerId}", ct);
    }

    /// <summary>
    /// Lists all registers
    /// </summary>
    public async Task<List<RegisterResponse>?> ListRegistersAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<RegisterResponse>>($"{_baseUrl}/registers", ct);
    }

    /// <summary>
    /// Gets a transaction by hash
    /// </summary>
    public async Task<TransactionResponse?> GetTransactionAsync(
        string registerId,
        string transactionHash,
        CancellationToken ct = default)
    {
        return await GetAsync<TransactionResponse>(
            $"{_baseUrl}/registers/{registerId}/transactions/{transactionHash}",
            ct);
    }

    /// <summary>
    /// Queries transactions in a register
    /// </summary>
    public async Task<TransactionQueryResponse?> QueryTransactionsAsync(
        string registerId,
        string? senderWallet = null,
        int? limit = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (senderWallet != null) queryParams.Add($"senderWallet={senderWallet}");
        if (limit != null) queryParams.Add($"limit={limit}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        var url = $"{_baseUrl}/registers/{registerId}/transactions{queryString}";

        return await GetAsync<TransactionQueryResponse>(url, ct);
    }

    /// <summary>
    /// Checks Register Service health
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        return await base.CheckHealthAsync(_baseUrl, ct);
    }
}

/// <summary>
/// Response from register creation/retrieval
/// </summary>
public class RegisterResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public uint Height { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response from transaction query
/// </summary>
public class TransactionResponse
{
    public string TxId { get; set; } = string.Empty;
    public string RegisterId { get; set; } = string.Empty;
    public string SenderWallet { get; set; } = string.Empty;
    public string? RecipientWallet { get; set; }
    public string? PreviousTxId { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
    public string Signature { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response from transaction query (paginated)
/// </summary>
public class TransactionQueryResponse
{
    public List<TransactionResponse> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
