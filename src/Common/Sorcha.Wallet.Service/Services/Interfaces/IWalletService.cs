using Sorcha.Wallet.Service.Domain.Entities;
using Sorcha.Wallet.Service.Domain.ValueObjects;

namespace Sorcha.Wallet.Service.Services.Interfaces;

/// <summary>
/// Main facade for wallet operations
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Creates a new wallet with a randomly generated mnemonic
    /// </summary>
    /// <param name="name">Wallet name</param>
    /// <param name="algorithm">Cryptographic algorithm (ED25519, SECP256K1, RSA)</param>
    /// <param name="owner">Owner subject identifier</param>
    /// <param name="tenant">Tenant identifier</param>
    /// <param name="wordCount">Mnemonic word count (12 or 24)</param>
    /// <param name="passphrase">Optional BIP39 passphrase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created wallet and the mnemonic (MUST be saved by caller)</returns>
    Task<(Wallet Wallet, Mnemonic Mnemonic)> CreateWalletAsync(
        string name,
        string algorithm,
        string owner,
        string tenant,
        int wordCount = 12,
        string? passphrase = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recovers a wallet from an existing mnemonic
    /// </summary>
    /// <param name="mnemonic">BIP39 mnemonic phrase</param>
    /// <param name="name">Wallet name</param>
    /// <param name="algorithm">Cryptographic algorithm</param>
    /// <param name="owner">Owner subject identifier</param>
    /// <param name="tenant">Tenant identifier</param>
    /// <param name="passphrase">Optional BIP39 passphrase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovered wallet</returns>
    Task<Wallet> RecoverWalletAsync(
        Mnemonic mnemonic,
        string name,
        string algorithm,
        string owner,
        string tenant,
        string? passphrase = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a wallet by address
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet if found</returns>
    Task<Wallet?> GetWalletAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all wallets for a given owner
    /// </summary>
    /// <param name="owner">Owner subject identifier</param>
    /// <param name="tenant">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of wallets</returns>
    Task<IEnumerable<Wallet>> GetWalletsByOwnerAsync(
        string owner,
        string tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates wallet metadata
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="name">New name</param>
    /// <param name="description">New description</param>
    /// <param name="tags">New tags</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated wallet</returns>
    Task<Wallet> UpdateWalletAsync(
        string address,
        string? name = null,
        string? description = null,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a wallet (soft delete)
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteWalletAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new derived address for the wallet
    /// </summary>
    /// <param name="walletAddress">Parent wallet address</param>
    /// <param name="index">Address index</param>
    /// <param name="isChange">Is this a change address?</param>
    /// <param name="label">Optional label</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated address</returns>
    Task<WalletAddress> GenerateAddressAsync(
        string walletAddress,
        int index,
        bool isChange = false,
        string? label = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a transaction with the wallet's private key
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="transactionData">Transaction data to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signature</returns>
    Task<byte[]> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        CancellationToken cancellationToken = default);
}
