// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.ServiceClients.Wallet;

/// <summary>
/// Unified client interface for Wallet Service operations
/// </summary>
/// <remarks>
/// This interface combines all Wallet Service operations needed across all consuming services:
/// - Validator Service: System wallet management, signing, verification
/// - Blueprint Service: Encryption/decryption, transaction signing, wallet queries
/// - CLI: Wallet creation, management, queries
///
/// All methods use gRPC when available, falling back to HTTP REST endpoints.
/// </remarks>
public interface IWalletServiceClient
{
    // =========================================================================
    // System Wallet Operations (Validator Service)
    // =========================================================================

    /// <summary>
    /// Creates or retrieves the system wallet for a validator instance
    /// </summary>
    /// <param name="validatorId">Unique validator identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System wallet ID</returns>
    /// <remarks>
    /// Used by Validator Service to maintain a persistent identity wallet.
    /// Creates a new wallet on first call, returns existing wallet on subsequent calls.
    /// </remarks>
    Task<string> CreateOrRetrieveSystemWalletAsync(
        string validatorId,
        CancellationToken cancellationToken = default);

    // =========================================================================
    // Signing Operations (Validator Service, Blueprint Service)
    // =========================================================================

    /// <summary>
    /// Signs data using a wallet's private key
    /// </summary>
    /// <param name="walletId">Wallet ID or address</param>
    /// <param name="dataToSign">Data to sign (hex string)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signature (base64-encoded)</returns>
    Task<string> SignDataAsync(
        string walletId,
        string dataToSign,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs transaction data with a wallet's private key
    /// </summary>
    /// <param name="walletAddress">Wallet address to use for signing</param>
    /// <param name="transactionData">Transaction data to sign</param>
    /// <param name="derivationPath">Optional key derivation path (BIP44 or Sorcha system path like "sorcha:register-attestation")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Digital signature (base64-encoded string)</returns>
    Task<string> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        string? derivationPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a digital signature
    /// </summary>
    /// <param name="publicKey">Public key or wallet address</param>
    /// <param name="data">Original data that was signed</param>
    /// <param name="signature">Signature to verify (base64-encoded)</param>
    /// <param name="algorithm">Signature algorithm (ED25519, NIST-P256, RSA-4096)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifySignatureAsync(
        string publicKey,
        string data,
        string signature,
        string algorithm,
        CancellationToken cancellationToken = default);

    // =========================================================================
    // Encryption Operations (Blueprint Service)
    // =========================================================================

    /// <summary>
    /// Encrypts a payload for a recipient wallet
    /// </summary>
    /// <param name="recipientWalletAddress">Wallet address that will decrypt the payload</param>
    /// <param name="payload">Data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted payload</returns>
    Task<byte[]> EncryptPayloadAsync(
        string recipientWalletAddress,
        byte[] payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a payload using a wallet's private key
    /// </summary>
    /// <param name="walletAddress">Wallet address to use for decryption</param>
    /// <param name="encryptedPayload">Encrypted payload to decrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted payload</returns>
    Task<byte[]> DecryptPayloadAsync(
        string walletAddress,
        byte[] encryptedPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a payload using delegated access authorization
    /// </summary>
    /// <param name="walletAddress">Wallet address to use for decryption</param>
    /// <param name="encryptedPayload">Encrypted payload to decrypt</param>
    /// <param name="delegationToken">Credential token from STS granting delegated decrypt access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted payload</returns>
    /// <remarks>
    /// Used when a service needs to decrypt on behalf of a user.
    /// The delegation token grants temporary decrypt permission.
    /// </remarks>
    Task<byte[]> DecryptWithDelegationAsync(
        string walletAddress,
        byte[] encryptedPayload,
        string delegationToken,
        CancellationToken cancellationToken = default);

    // =========================================================================
    // Wallet Management (All Services)
    // =========================================================================

    /// <summary>
    /// Gets wallet information by address
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet information, or null if not found</returns>
    Task<WalletInfo?> GetWalletAsync(
        string walletAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new wallet
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <param name="algorithm">Cryptographic algorithm (ED25519, NIST-P256, RSA-4096)</param>
    /// <param name="owner">Owner principal</param>
    /// <param name="tenant">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created wallet information</returns>
    Task<WalletInfo> CreateWalletAsync(
        string name,
        string algorithm,
        string owner,
        string tenant,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wallet information returned by the Wallet Service
/// </summary>
public class WalletInfo
{
    /// <summary>
    /// Unique wallet address (public identifier)
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Human-readable wallet name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Public key (base64-encoded)
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// Cryptographic algorithm (ED25519, NIST-P256, RSA-4096)
    /// </summary>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Wallet status (Active, Revoked, etc.)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Owner principal
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public required string Tenant { get; init; }

    /// <summary>
    /// When wallet was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When wallet was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Extensible metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
