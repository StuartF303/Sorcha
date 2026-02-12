// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Submits an action transaction to the Validator Service for validation and mempool inclusion
    /// </summary>
    public async Task<TransactionSubmissionResult> SubmitTransactionAsync(
        ActionTransactionSubmission request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Submitting action transaction {TransactionId} for register {RegisterId} to Validator Service",
                request.TransactionId, request.RegisterId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/transactions/validate",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);

                _logger.LogInformation(
                    "Action transaction {TransactionId} validated and added to mempool",
                    request.TransactionId);

                return new TransactionSubmissionResult
                {
                    Success = true,
                    TransactionId = result.GetProperty("transactionId").GetString() ?? request.TransactionId,
                    RegisterId = result.GetProperty("registerId").GetString() ?? request.RegisterId,
                    AddedAt = result.TryGetProperty("addedAt", out var addedAt)
                        ? addedAt.GetDateTimeOffset()
                        : DateTimeOffset.UtcNow
                };
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Action transaction {TransactionId} failed validation: {Content}",
                    request.TransactionId, content);

                return new TransactionSubmissionResult
                {
                    Success = false,
                    TransactionId = request.TransactionId,
                    RegisterId = request.RegisterId,
                    ErrorCode = "VALIDATION_FAILED",
                    ErrorMessage = content
                };
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Action transaction {TransactionId} rejected (conflict): {Content}",
                    request.TransactionId, content);

                return new TransactionSubmissionResult
                {
                    Success = false,
                    TransactionId = request.TransactionId,
                    RegisterId = request.RegisterId,
                    ErrorCode = "MEMPOOL_FULL",
                    ErrorMessage = content
                };
            }

            // Unexpected status code
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Unexpected response {StatusCode} submitting transaction {TransactionId}: {Content}",
                response.StatusCode, request.TransactionId, body);

            return new TransactionSubmissionResult
            {
                Success = false,
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                ErrorCode = "HTTP_ERROR",
                ErrorMessage = $"Unexpected status code: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting action transaction {TransactionId} for register {RegisterId}",
                request.TransactionId, request.RegisterId);

            return new TransactionSubmissionResult
            {
                Success = false,
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                ErrorCode = "HTTP_ERROR",
                ErrorMessage = ex.Message
            };
        }
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
