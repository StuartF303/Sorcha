// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Clients;

/// <summary>
/// Client interface for interacting with the Wallet Service
/// </summary>
public interface IWalletServiceClient
{
    /// <summary>
    /// Encrypts a payload for a recipient wallet
    /// </summary>
    /// <param name="recipientWalletAddress">The wallet address that will be able to decrypt the payload</param>
    /// <param name="payload">The data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The encrypted payload</returns>
    Task<byte[]> EncryptPayloadAsync(
        string recipientWalletAddress,
        byte[] payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a payload using a wallet's private key
    /// </summary>
    /// <param name="walletAddress">The wallet address to use for decryption</param>
    /// <param name="encryptedPayload">The encrypted payload to decrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The decrypted payload</returns>
    Task<byte[]> DecryptPayloadAsync(
        string walletAddress,
        byte[] encryptedPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a transaction with a wallet's private key
    /// </summary>
    /// <param name="walletAddress">The wallet address to use for signing</param>
    /// <param name="transactionData">The transaction data to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The digital signature</returns>
    Task<byte[]> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets wallet information by address
    /// </summary>
    /// <param name="walletAddress">The wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet information, or null if not found</returns>
    Task<WalletInfo?> GetWalletAsync(
        string walletAddress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wallet information returned by the Wallet Service
/// </summary>
public class WalletInfo
{
    public required string Address { get; set; }
    public required string Name { get; set; }
    public required string PublicKey { get; set; }
    public required string Algorithm { get; set; }
    public required string Status { get; set; }
    public required string Owner { get; set; }
    public required string Tenant { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
