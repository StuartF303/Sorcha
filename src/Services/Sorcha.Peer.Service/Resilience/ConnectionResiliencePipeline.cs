// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Sorcha.Peer.Service.Resilience;

/// <summary>
/// Polly resilience pipeline for hub node connection with exponential backoff
/// </summary>
public class ConnectionResiliencePipeline
{
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ConnectionResiliencePipeline> _logger;

    /// <summary>
    /// Creates a new connection resilience pipeline with configured retry and timeout policies
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public ConnectionResiliencePipeline(ILogger<ConnectionResiliencePipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10, // Max 10 retries
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1), // Initial delay: 1 second
                MaxDelay = TimeSpan.FromMinutes(1), // Maximum delay: 60 seconds
                UseJitter = true, // Add randomness to prevent thundering herd
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Connection retry {Attempt}/{MaxAttempts} after {Delay}s: {Exception}",
                        args.AttemptNumber + 1,
                        10,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30), // Connection timeout per attempt: 30 seconds
                OnTimeout = args =>
                {
                    logger.LogWarning("Connection attempt timed out after 30 seconds");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Executes an operation with resilience (retry and timeout)
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await _pipeline.ExecuteAsync(async token => await operation(token), cancellationToken);
    }

    /// <summary>
    /// Executes an operation with resilience (retry and timeout) without return value
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await _pipeline.ExecuteAsync(async token => await operation(token), cancellationToken);
    }
}
