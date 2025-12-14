// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validator for exponential backoff retry logic
/// </summary>
public static class RetryBackoffValidator
{
    /// <summary>
    /// Initial delay in seconds (1 second)
    /// </summary>
    public const int InitialDelaySeconds = PeerServiceConstants.RetryInitialDelaySeconds;

    /// <summary>
    /// Maximum delay in seconds (60 seconds cap)
    /// </summary>
    public const int MaxDelaySeconds = PeerServiceConstants.RetryMaxDelaySeconds;

    /// <summary>
    /// Exponential multiplier (2.0 for doubling)
    /// </summary>
    public const double Multiplier = PeerServiceConstants.RetryMultiplier;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public const int MaxRetryAttempts = PeerServiceConstants.MaxRetryAttempts;

    /// <summary>
    /// Calculates exponential backoff delay for a given attempt number
    /// </summary>
    /// <param name="attemptNumber">Retry attempt number (1-based)</param>
    /// <returns>TimeSpan to wait before next attempt</returns>
    public static TimeSpan CalculateBackoff(int attemptNumber)
    {
        if (attemptNumber <= 0)
            return TimeSpan.Zero;

        // Exponential backoff: delay = min(initial * (2 ^ (attempt - 1)), max)
        var delaySeconds = Math.Min(
            InitialDelaySeconds * Math.Pow(Multiplier, attemptNumber - 1),
            MaxDelaySeconds);

        return TimeSpan.FromSeconds(delaySeconds);
    }

    /// <summary>
    /// Gets the expected backoff sequence for all retry attempts
    /// </summary>
    /// <returns>Array of expected delays: 1s, 2s, 4s, 8s, 16s, 32s, 60s, 60s, 60s, 60s</returns>
    public static TimeSpan[] GetExpectedBackoffSequence()
    {
        return PeerServiceConstants.RetryBackoffSequence;
    }

    /// <summary>
    /// Validates if an attempt number is within allowed range
    /// </summary>
    /// <param name="attemptNumber">Attempt number to validate</param>
    /// <returns>True if attempt number is valid (1 to MaxRetryAttempts)</returns>
    public static bool IsValidAttemptNumber(int attemptNumber)
    {
        return attemptNumber >= 1 && attemptNumber <= MaxRetryAttempts;
    }
}
