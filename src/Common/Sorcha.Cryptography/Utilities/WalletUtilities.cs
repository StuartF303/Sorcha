using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Models;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Implements wallet address and key formatting utilities.
/// </summary>
public class WalletUtilities : IWalletUtilities
{
    private const string WalletPrefix = "ws1"; // Sorcha wallet prefix

    /// <summary>
    /// Converts a public key to a wallet address.
    /// </summary>
    public string? PublicKeyToWallet(byte[] publicKey, byte network)
    {
        try
        {
            if (publicKey == null || publicKey.Length == 0)
                return null;

            // Prepend network byte to public key
            byte[] data = new byte[publicKey.Length + 1];
            data[0] = network;
            Array.Copy(publicKey, 0, data, 1, publicKey.Length);

            // Encode with Bech32
            return Bech32.Encode(WalletPrefix, data);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a wallet address to public key and network.
    /// </summary>
    public (byte Network, byte[] PublicKey)? WalletToPublicKey(string walletAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
                return null;

            // Decode Bech32
            var decoded = Bech32.Decode(walletAddress);
            if (decoded == null)
                return null;

            var (hrp, data) = decoded.Value;

            // Verify HRP
            if (hrp != WalletPrefix)
                return null;

            // Extract network byte and public key
            if (data.Length < 2)
                return null;

            byte network = data[0];
            byte[] publicKey = data.Skip(1).ToArray();

            return (network, publicKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates multiple wallet addresses.
    /// </summary>
    public (bool[] Valid, CryptoKey[] ValidWallets) ValidateWallets(string[] walletAddresses)
    {
        if (walletAddresses == null || walletAddresses.Length == 0)
            return (Array.Empty<bool>(), Array.Empty<CryptoKey>());

        var validList = new List<bool>();
        var validWalletsList = new List<CryptoKey>();

        foreach (string address in walletAddresses)
        {
            var result = WalletToPublicKey(address);
            bool isValid = result.HasValue;

            validList.Add(isValid);

            if (isValid)
            {
                var (network, publicKey) = result.Value;
                var cryptoKey = new CryptoKey((WalletNetworks)network, publicKey);
                validWalletsList.Add(cryptoKey);
            }
        }

        return (validList.ToArray(), validWalletsList.ToArray());
    }

    /// <summary>
    /// Converts a private key to WIF (Wallet Import Format).
    /// </summary>
    public string? PrivateKeyToWIF(byte[] privateKey, byte network)
    {
        try
        {
            if (privateKey == null || privateKey.Length == 0)
                return null;

            // Build WIF data: [version byte][private key]
            // Version byte indicates network
            byte[] wifData = new byte[privateKey.Length + 1];
            wifData[0] = network;
            Array.Copy(privateKey, 0, wifData, 1, privateKey.Length);

            // Encode with Base58Check
            return Base58.EncodeCheck(wifData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts WIF to private key and network.
    /// </summary>
    public (byte Network, byte[] PrivateKey)? WIFToPrivateKey(string wif)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(wif))
                return null;

            // Decode Base58Check
            byte[]? decoded = Base58.DecodeCheck(wif);
            if (decoded == null || decoded.Length < 2)
                return null;

            // Extract network byte and private key
            byte network = decoded[0];
            byte[] privateKey = decoded.Skip(1).ToArray();

            return (network, privateKey);
        }
        catch
        {
            return null;
        }
    }
}
