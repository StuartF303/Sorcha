// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Cli.Configuration;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// Client for the Register Service API
/// </summary>
public class RegisterApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public RegisterApiClient(HttpClient httpClient, ActivityLog activityLog)
        : base(httpClient, activityLog)
    {
        _baseUrl = TestCredentials.RegisterServiceUrl;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        return await CheckHealthAsync(_baseUrl, ct);
    }

    public async Task<RegisterDto?> CreateRegisterAsync(CreateRegisterRequest request, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<CreateRegisterRequest, RegisterDto>(
            $"{_baseUrl}/api/registers", request, ct);
    }

    public async Task<List<RegisterDto>?> ListRegistersAsync(CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<RegisterDto>>($"{_baseUrl}/api/registers", ct);
    }

    public async Task<RegisterDto?> GetRegisterAsync(string registerId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<RegisterDto>($"{_baseUrl}/api/registers/{registerId}", ct);
    }

    public async Task<TransactionDto?> SubmitTransactionAsync(
        string registerId,
        SubmitTransactionRequest request,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<SubmitTransactionRequest, TransactionDto>(
            $"{_baseUrl}/api/registers/{registerId}/transactions", request, ct);
    }

    public async Task<PagedResult<TransactionDto>?> ListTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<PagedResult<TransactionDto>>(
            $"{_baseUrl}/api/registers/{registerId}/transactions?page={page}&pageSize={pageSize}", ct);
    }

    public async Task<TransactionDto?> GetTransactionAsync(
        string registerId,
        string txId,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<TransactionDto>(
            $"{_baseUrl}/api/registers/{registerId}/transactions/{txId}", ct);
    }

    public async Task<QueryStatsDto?> GetQueryStatsAsync(CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<QueryStatsDto>($"{_baseUrl}/api/query/stats", ct);
    }

    public async Task<List<TransactionDto>?> QueryByWalletAsync(
        string walletAddress,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<TransactionDto>>(
            $"{_baseUrl}/api/query/wallets/{walletAddress}/transactions", ct);
    }

    public async Task<HttpResponseMessage> DeleteRegisterAsync(
        string registerId,
        string tenantId,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await DeleteAsync($"{_baseUrl}/api/registers/{registerId}?tenantId={tenantId}", ct);
    }
}

// DTOs for Register Service
public record RegisterDto(
    string Id,
    string Name,
    string? Description,
    string TenantId,
    int Height,
    int Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateRegisterRequest(
    string Name,
    string? Description,
    string TenantId
);

public record TransactionDto(
    string Id,
    string RegisterId,
    string SenderId,
    string? BlueprintId,
    string? InstanceId,
    int? ActionId,
    string Hash,
    int Height,
    string Status,
    string? Payload,
    string? Signature,
    DateTime CreatedAt,
    DateTime? ConfirmedAt
);

public record SubmitTransactionRequest(
    string SenderId,
    string? BlueprintId = null,
    string? InstanceId = null,
    int? ActionId = null,
    string? Payload = null,
    string? Signature = null
);

public record QueryStatsDto(
    int TotalRegisters,
    int TotalTransactions,
    int TotalDockets,
    DateTime? LastTransactionAt
);
