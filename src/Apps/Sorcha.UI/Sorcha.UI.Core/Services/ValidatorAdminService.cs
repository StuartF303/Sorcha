// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sorcha.UI.Core.Models.Admin;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Implementation of <see cref="IValidatorAdminService"/> that calls the Validator Service API.
/// </summary>
public class ValidatorAdminService : IValidatorAdminService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ValidatorAdminService> _logger;

    public ValidatorAdminService(HttpClient httpClient, ILogger<ValidatorAdminService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ValidatorStatusViewModel> GetMempoolStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/admin/mempool", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch mempool status: {StatusCode}", response.StatusCode);
                return new ValidatorStatusViewModel { IsLoaded = false };
            }

            var status = await response.Content.ReadFromJsonAsync<ValidatorStatusViewModel>(cancellationToken: cancellationToken);
            return status ?? new ValidatorStatusViewModel { IsLoaded = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching mempool status");
            return new ValidatorStatusViewModel { IsLoaded = false };
        }
    }

    public async Task<RegisterMempoolStat> GetRegisterMempoolAsync(string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stat = await _httpClient.GetFromJsonAsync<RegisterMempoolStat>(
                $"/api/v1/transactions/mempool/{registerId}", cancellationToken);
            return stat ?? new RegisterMempoolStat { RegisterId = registerId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching mempool for register {RegisterId}", registerId);
            return new RegisterMempoolStat { RegisterId = registerId };
        }
    }
}
