using Sorcha.Cryptography.Models;

namespace Sorcha.Cryptography.Interfaces;

/// <summary>
/// Provides wallet address and key formatting utilities.
/// </summary>
public interface IWalletUtilities
{
    /// <summary>
    /// Converts a public key to a wallet address.
    /// </summary>
    /// <param name="publicKey">The public key bytes.</param>
    /// <param name="network">The network type.</param>
    /// <returns>The wallet address, or null if conversion fails.</returns>
    string? PublicKeyToWallet(byte[] publicKey, byte network);

    /// <summary>
    /// Converts a wallet address to public key and network.
    /// </summary>
    /// <param name="walletAddress">The wallet address.</param>
    /// <returns>A tuple containing the network and public key, or null if invalid.</returns>
    (byte Network, byte[] PublicKey)? WalletToPublicKey(string walletAddress);

    /// <summary>
    /// Validates multiple wallet addresses.
    /// </summary>
    /// <param name="walletAddresses">The wallet addresses to validate.</param>
    /// <returns>A tuple containing validation results and valid wallet keys.</returns>
    (bool[] Valid, CryptoKey[] ValidWallets) ValidateWallets(string[] walletAddresses);

    /// <summary>
    /// Converts a private key to WIF (Wallet Import Format).
    /// </summary>
    /// <param name="privateKey">The private key bytes.</param>
    /// <param name="network">The network type.</param>
    /// <returns>The WIF string, or null if conversion fails.</returns>
    string? PrivateKeyToWIF(byte[] privateKey, byte network);

    /// <summary>
    /// Converts WIF to private key and network.
    /// </summary>
    /// <param name="wif">The WIF string.</param>
    /// <returns>A tuple containing the network and private key, or null if invalid.</returns>
    (byte Network, byte[] PrivateKey)? WIFToPrivateKey(string wif);
}
