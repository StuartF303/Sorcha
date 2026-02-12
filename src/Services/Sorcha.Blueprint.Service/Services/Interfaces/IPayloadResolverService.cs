// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Engine.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for payload encryption, decryption, and aggregation
/// </summary>
public interface IPayloadResolverService
{
    /// <summary>
    /// Creates encrypted payloads from disclosure results
    /// </summary>
    /// <param name="disclosureResults">The disclosure results containing participant-specific data</param>
    /// <param name="participantWallets">Dictionary mapping participant IDs to wallet addresses</param>
    /// <param name="senderWallet">The sender's wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping wallet addresses to encrypted payloads</returns>
    Task<Dictionary<string, byte[]>> CreateEncryptedPayloadsAsync(
        Dictionary<string, object> disclosureResults,
        Dictionary<string, string> participantWallets,
        string senderWallet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates historical data from previous transactions
    /// </summary>
    /// <param name="registerAddress">The register address</param>
    /// <param name="transactionIds">Previous transaction IDs to retrieve</param>
    /// <param name="wallet">The wallet requesting the data</param>
    /// <param name="disclosureRules">Optional disclosure rules to filter the data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated historical data</returns>
    Task<Dictionary<string, object>> AggregateHistoricalDataAsync(
        string registerAddress,
        IEnumerable<string> transactionIds,
        string wallet,
        IEnumerable<string>? disclosureRules = null,
        CancellationToken cancellationToken = default);
}
