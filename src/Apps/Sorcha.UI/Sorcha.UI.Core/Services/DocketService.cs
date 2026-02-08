// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sorcha.UI.Core.Models.Explorer;
using Sorcha.UI.Core.Models.Registers;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Implementation of <see cref="IDocketService"/> calling the Register Service API.
/// </summary>
public class DocketService : IDocketService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocketService> _logger;

    public DocketService(HttpClient httpClient, ILogger<DocketService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DocketViewModel>> GetDocketsAsync(string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dockets = await _httpClient.GetFromJsonAsync<List<DocketViewModel>>(
                $"/api/registers/{registerId}/dockets", cancellationToken);
            return dockets ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dockets for register {RegisterId}", registerId);
            return [];
        }
    }

    public async Task<DocketViewModel?> GetDocketAsync(string registerId, string docketId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DocketViewModel>(
                $"/api/registers/{registerId}/dockets/{docketId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching docket {DocketId}", docketId);
            return null;
        }
    }

    public async Task<List<TransactionViewModel>> GetDocketTransactionsAsync(string registerId, string docketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var txs = await _httpClient.GetFromJsonAsync<List<TransactionViewModel>>(
                $"/api/registers/{registerId}/dockets/{docketId}/transactions", cancellationToken);
            return txs ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions for docket {DocketId}", docketId);
            return [];
        }
    }

    public async Task<DocketViewModel?> GetLatestDocketAsync(string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DocketViewModel>(
                $"/api/registers/{registerId}/dockets/latest", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest docket for register {RegisterId}", registerId);
            return null;
        }
    }
}
