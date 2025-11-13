using System.Threading.Tasks;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;
using Sorcha.Cryptography.Utilities;

namespace Sorcha.TransactionHandler.Tests;

/// <summary>
/// Test helper methods.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Generates a test wallet for use in tests.
    /// </summary>
    public static async Task<TestWallet> GenerateTestWalletAsync(WalletNetworks network = WalletNetworks.ED25519)
    {
        var cryptoModule = new CryptoModule();
        var walletUtils = new WalletUtilities();
        var keyManager = new KeyManager(cryptoModule);

        var result = await keyManager.CreateMasterKeyRingAsync(network);

        if (!result.IsSuccess || result.Value == null)
            throw new System.InvalidOperationException("Failed to generate test wallet");

        var keyRing = result.Value;

        // Convert private key to Base64 (current Transaction implementation expects Base64)
        var privateKeyBase64 = System.Convert.ToBase64String(keyRing.MasterKeySet.PrivateKey.Key!);

        // Generate wallet address from public key
        var address = walletUtils.PublicKeyToWallet(keyRing.MasterKeySet.PublicKey.Key!, (byte)network);

        return new TestWallet
        {
            KeyRing = keyRing,
            PrivateKeyWif = privateKeyBase64,
            Address = address ?? throw new System.InvalidOperationException("Failed to generate wallet address")
        };
    }
}

/// <summary>
/// Test wallet container.
/// </summary>
public class TestWallet
{
    public required KeyRing KeyRing { get; init; }
    public required string PrivateKeyWif { get; init; }
    public required string Address { get; init; }
}
