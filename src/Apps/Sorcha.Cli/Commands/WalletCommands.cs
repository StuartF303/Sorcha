using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Wallet management commands.
/// </summary>
public class WalletCommand : Command
{
    public WalletCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("wallet", "Manage cryptographic wallets")
    {
        AddCommand(new WalletListCommand(clientFactory, authService, configService));
        AddCommand(new WalletGetCommand(clientFactory, authService, configService));
        AddCommand(new WalletCreateCommand(clientFactory, authService, configService));
        AddCommand(new WalletRecoverCommand(clientFactory, authService, configService));
        AddCommand(new WalletDeleteCommand(clientFactory, authService, configService));
        AddCommand(new WalletSignCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists all wallets for the current user.
/// </summary>
public class WalletListCommand : Command
{
    public WalletListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List all wallets")
    {
        this.SetHandler(async () =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Call API
                var wallets = await client.ListWalletsAsync($"Bearer {token}");

                // Display results
                if (wallets == null || wallets.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No wallets found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {wallets.Count} wallet(s):");
                Console.WriteLine();
                Console.WriteLine($"{"Address",-45} {"Name",-25} {"Algorithm",-12} {"Status",-10}");
                Console.WriteLine(new string('-', 95));
                foreach (var wallet in wallets)
                {
                    Console.WriteLine($"{wallet.Address,-45} {wallet.Name,-25} {wallet.Algorithm,-12} {wallet.Status,-10}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Token may be expired. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list wallets: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets a wallet by address.
/// </summary>
public class WalletGetCommand : Command
{
    public WalletGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a wallet by address")
    {
        var addressOption = new Option<string>(
            aliases: new[] { "--address", "-a" },
            description: "Wallet address")
        {
            IsRequired = true
        };

        AddOption(addressOption);

        this.SetHandler(async (address) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Call API
                var wallet = await client.GetWalletAsync(address, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Wallet details:");
                Console.WriteLine();
                Console.WriteLine($"  Address:     {wallet.Address}");
                Console.WriteLine($"  Name:        {wallet.Name}");
                Console.WriteLine($"  Algorithm:   {wallet.Algorithm}");
                Console.WriteLine($"  Public Key:  {wallet.PublicKey}");
                Console.WriteLine($"  Status:      {wallet.Status}");
                Console.WriteLine($"  Owner:       {wallet.Owner}");
                Console.WriteLine($"  Tenant:      {wallet.Tenant}");
                Console.WriteLine($"  Created:     {wallet.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Updated:     {wallet.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                if (wallet.Metadata != null && wallet.Metadata.Count > 0)
                {
                    Console.WriteLine("  Metadata:");
                    foreach (var (key, value) in wallet.Metadata)
                    {
                        Console.WriteLine($"    {key}: {value}");
                    }
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Wallet '{address}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get wallet: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, addressOption);
    }
}

/// <summary>
/// Creates a new wallet.
/// </summary>
public class WalletCreateCommand : Command
{
    public WalletCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new wallet")
    {
        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Wallet name")
        {
            IsRequired = true
        };

        var algorithmOption = new Option<string>(
            aliases: new[] { "--algorithm", "-a" },
            getDefaultValue: () => "ED25519",
            description: "Cryptographic algorithm (ED25519, NISTP256, RSA4096)");

        var wordCountOption = new Option<int>(
            aliases: new[] { "--word-count", "-w" },
            getDefaultValue: () => 12,
            description: "Number of words in mnemonic (12, 15, 18, 21, or 24)");

        var passphraseOption = new Option<string?>(
            aliases: new[] { "--passphrase", "-p" },
            description: "Optional passphrase for additional security");

        AddOption(nameOption);
        AddOption(algorithmOption);
        AddOption(wordCountOption);
        AddOption(passphraseOption);

        this.SetHandler(async (name, algorithm, wordCount, passphrase) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Build request
                var request = new CreateWalletRequest
                {
                    Name = name,
                    Algorithm = algorithm,
                    WordCount = wordCount,
                    Passphrase = passphrase
                };

                // Call API
                var response = await client.CreateWalletAsync(request, $"Bearer {token}");

                // Display results with security warning
                ConsoleHelper.WriteSuccess($"Wallet created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Address:     {response.Wallet?.Address}");
                Console.WriteLine($"  Name:        {response.Wallet?.Name}");
                Console.WriteLine($"  Algorithm:   {response.Wallet?.Algorithm}");
                Console.WriteLine($"  Public Key:  {response.Wallet?.PublicKey}");
                Console.WriteLine();
                ConsoleHelper.WriteWarning("⚠️  CRITICAL: Save your mnemonic phrase securely!");
                Console.WriteLine($"  Mnemonic:    {string.Join(" ", response.MnemonicWords)}");
                Console.WriteLine();
                ConsoleHelper.WriteWarning("The mnemonic phrase will NEVER be displayed again.");
                ConsoleHelper.WriteWarning("Write it down on paper and store it in a secure location.");
                ConsoleHelper.WriteWarning("Anyone with this phrase can access your wallet!");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid request: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to create wallet: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, nameOption, algorithmOption, wordCountOption, passphraseOption);
    }
}

/// <summary>
/// Recovers a wallet from mnemonic phrase.
/// </summary>
public class WalletRecoverCommand : Command
{
    public WalletRecoverCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("recover", "Recover a wallet from mnemonic phrase")
    {
        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Wallet name")
        {
            IsRequired = true
        };

        var algorithmOption = new Option<string>(
            aliases: new[] { "--algorithm", "-a" },
            getDefaultValue: () => "ED25519",
            description: "Cryptographic algorithm (ED25519, NISTP256, RSA4096)");

        var mnemonicOption = new Option<string>(
            aliases: new[] { "--mnemonic", "-m" },
            description: "Mnemonic phrase (space-separated words)")
        {
            IsRequired = true
        };

        var passphraseOption = new Option<string?>(
            aliases: new[] { "--passphrase", "-p" },
            description: "Optional passphrase if one was used during creation");

        AddOption(nameOption);
        AddOption(algorithmOption);
        AddOption(mnemonicOption);
        AddOption(passphraseOption);

        this.SetHandler(async (name, algorithm, mnemonic, passphrase) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Build request
                var words = mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var request = new RecoverWalletRequest
                {
                    MnemonicWords = words,
                    Name = name,
                    Algorithm = algorithm,
                    Passphrase = passphrase
                };

                // Call API
                var wallet = await client.RecoverWalletAsync(request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Wallet recovered successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Address:     {wallet.Address}");
                Console.WriteLine($"  Name:        {wallet.Name}");
                Console.WriteLine($"  Algorithm:   {wallet.Algorithm}");
                Console.WriteLine($"  Public Key:  {wallet.PublicKey}");
                Console.WriteLine($"  Status:      {wallet.Status}");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid mnemonic phrase or parameters: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to recover wallet: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, nameOption, algorithmOption, mnemonicOption, passphraseOption);
    }
}

/// <summary>
/// Deletes a wallet.
/// </summary>
public class WalletDeleteCommand : Command
{
    public WalletDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete a wallet")
    {
        var addressOption = new Option<string>(
            aliases: new[] { "--address", "-a" },
            description: "Wallet address")
        {
            IsRequired = true
        };

        var confirmOption = new Option<bool>(
            aliases: new[] { "--yes", "-y" },
            description: "Skip confirmation prompt");

        AddOption(addressOption);
        AddOption(confirmOption);

        this.SetHandler(async (address, confirm) =>
        {
            try
            {
                // Confirm deletion
                if (!confirm)
                {
                    ConsoleHelper.WriteWarning("⚠️  WARNING: This will soft-delete the wallet.");
                    if (!ConsoleHelper.Confirm($"Are you sure you want to delete wallet '{address}'?", defaultYes: false))
                    {
                        ConsoleHelper.WriteInfo("Deletion cancelled.");
                        return;
                    }
                }

                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Call API
                await client.DeleteWalletAsync(address, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Wallet '{address}' deleted successfully.");
                ConsoleHelper.WriteInfo("Note: This is a soft delete. Contact support to recover if needed.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Wallet '{address}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to delete wallet: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, addressOption, confirmOption);
    }
}

/// <summary>
/// Signs data with a wallet's private key.
/// </summary>
public class WalletSignCommand : Command
{
    public WalletSignCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("sign", "Sign data with a wallet's private key")
    {
        var addressOption = new Option<string>(
            aliases: new[] { "--address", "-a" },
            description: "Wallet address")
        {
            IsRequired = true
        };

        var dataOption = new Option<string>(
            aliases: new[] { "--data", "-d" },
            description: "Data to sign (base64 encoded)")
        {
            IsRequired = true
        };

        AddOption(addressOption);
        AddOption(dataOption);

        this.SetHandler(async (address, data) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Wallet Service client
                var client = await clientFactory.CreateWalletServiceClientAsync(profileName);

                // Build request
                var request = new SignTransactionRequest
                {
                    TransactionData = data
                };

                // Call API
                var response = await client.SignTransactionAsync(address, request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Data signed successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Signature:   {response.Signature}");
                Console.WriteLine($"  Signed By:   {response.SignedBy}");
                Console.WriteLine($"  Signed At:   {response.SignedAt:yyyy-MM-dd HH:mm:ss}");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Wallet '{address}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid data or parameters: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to sign data: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, addressOption, dataOption);
    }
}
