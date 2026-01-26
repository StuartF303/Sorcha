// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Collects signatures from confirming validators for proposed dockets.
/// Implements parallel signature collection with timeout and threshold checking.
/// </summary>
public class SignatureCollector : ISignatureCollector
{
    private readonly ValidatorConfiguration _validatorConfig;
    private readonly ConsensusConfiguration _consensusConfig;
    private readonly ILeaderElectionService _leaderElection;
    private readonly ILogger<SignatureCollector> _logger;

    // Statistics
    private long _totalCollections;
    private long _successfulCollections;
    private long _failedCollections;

    public SignatureCollector(
        IOptions<ValidatorConfiguration> validatorConfig,
        IOptions<ConsensusConfiguration> consensusConfig,
        ILeaderElectionService leaderElection,
        ILogger<SignatureCollector> logger)
    {
        _validatorConfig = validatorConfig?.Value ?? throw new ArgumentNullException(nameof(validatorConfig));
        _consensusConfig = consensusConfig?.Value ?? throw new ArgumentNullException(nameof(consensusConfig));
        _leaderElection = leaderElection ?? throw new ArgumentNullException(nameof(leaderElection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<SignatureCollectionResult> CollectSignaturesAsync(
        Docket docket,
        ConsensusConfig config,
        IReadOnlyList<ValidatorInfo> validators,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(docket);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(validators);

        Interlocked.Increment(ref _totalCollections);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Collecting signatures for docket {DocketId} from {ValidatorCount} validators (threshold: {Min}-{Max})",
            docket.DocketId, validators.Count, config.SignatureThresholdMin, config.SignatureThresholdMax);

        var signatures = new ConcurrentBag<ValidatorSignature>();
        var rejectionDetails = new ConcurrentDictionary<string, string>();
        var nonResponders = new ConcurrentBag<string>();
        var responses = 0;
        var approvals = 0;
        var rejections = 0;

        // Add initiator's signature first
        if (docket.ProposerSignature != null)
        {
            signatures.Add(new ValidatorSignature
            {
                ValidatorId = docket.ProposerValidatorId,
                Signature = docket.ProposerSignature,
                SignedAt = docket.CreatedAt,
                IsInitiator = true
            });
            Interlocked.Increment(ref approvals);
        }

        // Filter out self from validators to request from
        var othersToRequest = validators
            .Where(v => v.ValidatorId != _validatorConfig.ValidatorId)
            .ToList();

        if (othersToRequest.Count == 0)
        {
            _logger.LogDebug("No other validators to request signatures from");
            return CreateResult(
                signatures.ToList(),
                config,
                validators.Count,
                1, // Just self
                1, // Just self approved
                0,
                false,
                stopwatch.Elapsed,
                [],
                new Dictionary<string, string>());
        }

        // Create timeout cancellation
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(config.DocketTimeout);

        try
        {
            // Request signatures in parallel
            var tasks = othersToRequest.Select(async validator =>
            {
                try
                {
                    var response = await RequestSignatureAsync(
                        validator,
                        docket,
                        _leaderElection.CurrentTerm,
                        timeoutCts.Token);

                    if (response == null)
                    {
                        nonResponders.Add(validator.ValidatorId);
                        return;
                    }

                    Interlocked.Increment(ref responses);

                    if (response.Approved && response.Signature != null)
                    {
                        signatures.Add(new ValidatorSignature
                        {
                            ValidatorId = response.ValidatorId,
                            Signature = response.Signature,
                            SignedAt = DateTimeOffset.UtcNow,
                            IsInitiator = false
                        });
                        Interlocked.Increment(ref approvals);
                    }
                    else
                    {
                        Interlocked.Increment(ref rejections);
                        if (response.RejectionDetails != null)
                        {
                            rejectionDetails[validator.ValidatorId] = response.RejectionDetails;
                        }
                    }

                    // Early exit if we have max signatures
                    if (approvals >= config.MaxSignaturesPerDocket)
                    {
                        _logger.LogDebug("Max signatures reached, stopping collection early");
                        await timeoutCts.CancelAsync();
                    }
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    nonResponders.Add(validator.ValidatorId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error requesting signature from validator {ValidatorId}",
                        validator.ValidatorId);
                    nonResponders.Add(validator.ValidatorId);
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // Timeout occurred, but we may still have enough signatures
            _logger.LogWarning(
                "Signature collection timed out for docket {DocketId} after {Timeout}",
                docket.DocketId, config.DocketTimeout);
        }

        stopwatch.Stop();

        var result = CreateResult(
            signatures.ToList(),
            config,
            validators.Count,
            responses + 1, // +1 for self
            approvals,
            rejections,
            timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested,
            stopwatch.Elapsed,
            nonResponders.ToList(),
            new Dictionary<string, string>(rejectionDetails));

        if (result.ThresholdMet)
        {
            Interlocked.Increment(ref _successfulCollections);
            _logger.LogInformation(
                "Signature collection succeeded for docket {DocketId}: {Approvals}/{Total} ({Percentage:F1}%)",
                docket.DocketId, result.Approvals, result.TotalValidators, result.ApprovalPercentage);
        }
        else
        {
            Interlocked.Increment(ref _failedCollections);
            _logger.LogWarning(
                "Signature collection failed for docket {DocketId}: {Approvals}/{Total} (need {Min})",
                docket.DocketId, result.Approvals, result.TotalValidators, config.SignatureThresholdMin);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ValidatorSignatureResponse?> RequestSignatureAsync(
        ValidatorInfo validator,
        Docket docket,
        long term,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(docket);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                "Requesting signature from validator {ValidatorId} for docket {DocketId}",
                validator.ValidatorId, docket.DocketId);

            // TODO: Integrate with Peer Service to send signature request via gRPC
            // For now, simulate a successful response
            // await _peerClient.RequestDocketSignatureAsync(validator.GrpcEndpoint, docket, term, ct);

            // Simulate network latency for testing
            await Task.Delay(Random.Shared.Next(10, 50), ct);

            stopwatch.Stop();

            // In production, this would be the actual response from the validator
            // For now, return a simulated approval
            return new ValidatorSignatureResponse
            {
                ValidatorId = validator.ValidatorId,
                Approved = true,
                Signature = new Signature
                {
                    PublicKey = System.Text.Encoding.UTF8.GetBytes(validator.PublicKey),
                    SignatureValue = System.Text.Encoding.UTF8.GetBytes($"sig-{validator.ValidatorId}-{docket.DocketId}"),
                    Algorithm = "ED25519",
                    SignedAt = DateTimeOffset.UtcNow,
                    SignedBy = validator.ValidatorId
                },
                Latency = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "Signature request to {ValidatorId} was cancelled",
                validator.ValidatorId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to request signature from validator {ValidatorId}",
                validator.ValidatorId);
            return null;
        }
    }

    private static SignatureCollectionResult CreateResult(
        IReadOnlyList<ValidatorSignature> signatures,
        ConsensusConfig config,
        int totalValidators,
        int responsesReceived,
        int approvals,
        int rejections,
        bool timedOut,
        TimeSpan duration,
        IReadOnlyList<string> nonResponders,
        IReadOnlyDictionary<string, string> rejectionDetails)
    {
        // Check if we met the minimum threshold
        var thresholdMet = approvals >= config.SignatureThresholdMin;

        return new SignatureCollectionResult
        {
            Signatures = signatures,
            ThresholdMet = thresholdMet,
            TimedOut = timedOut,
            TotalValidators = totalValidators,
            ResponsesReceived = responsesReceived,
            Approvals = approvals,
            Rejections = rejections,
            Duration = duration,
            NonResponders = nonResponders,
            RejectionDetails = rejectionDetails
        };
    }

    /// <summary>
    /// Get statistics about signature collection
    /// </summary>
    public SignatureCollectorStats GetStats()
    {
        return new SignatureCollectorStats
        {
            TotalCollections = Interlocked.Read(ref _totalCollections),
            SuccessfulCollections = Interlocked.Read(ref _successfulCollections),
            FailedCollections = Interlocked.Read(ref _failedCollections)
        };
    }
}

/// <summary>
/// Statistics for signature collection
/// </summary>
public record SignatureCollectorStats
{
    /// <summary>Total collection attempts</summary>
    public long TotalCollections { get; init; }

    /// <summary>Successful collections (threshold met)</summary>
    public long SuccessfulCollections { get; init; }

    /// <summary>Failed collections (threshold not met)</summary>
    public long FailedCollections { get; init; }

    /// <summary>Success rate (0-1)</summary>
    public double SuccessRate => TotalCollections > 0
        ? (double)SuccessfulCollections / TotalCollections
        : 0;
}
