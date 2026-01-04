// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sorcha.ServiceClients.Validator;

/// <summary>
/// HTTP client for Validator Service operations
/// </summary>
public class ValidatorServiceClient : IValidatorServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ValidatorServiceClient> _logger;
    private readonly string _serviceAddress;

    public ValidatorServiceClient(
        IConfiguration configuration,
        ILogger<ValidatorServiceClient> logger,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _serviceAddress = configuration["ServiceClients:ValidatorService:Address"]
            ?? throw new InvalidOperationException("Validator Service address not configured");

        _httpClient.BaseAddress = new Uri(_serviceAddress);

        _logger.LogInformation(
            "ValidatorServiceClient initialized (Address: {Address})",
            _serviceAddress);
    }

    /// <summary>
    /// Submits a genesis transaction to the Validator Service mempool
    /// </summary>
    public async Task<bool> SubmitGenesisTransactionAsync(
        GenesisTransactionSubmission request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Submitting genesis transaction for register {RegisterId} to Validator Service",
                request.RegisterId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/validator/genesis",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Genesis transaction for register {RegisterId} submitted successfully",
                    request.RegisterId);
                return true;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Failed to submit genesis transaction for register {RegisterId}: {StatusCode} - {Content}",
                request.RegisterId,
                response.StatusCode,
                content);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting genesis transaction for register {RegisterId}",
                request.RegisterId);
            return false;
        }
    }
}
