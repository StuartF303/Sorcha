using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Transaction management commands.
/// </summary>
public class TransactionCommand : Command
{
    public TransactionCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("tx", "Manage transactions in registers")
    {
        AddCommand(new TxListCommand(clientFactory, authService, configService));
        AddCommand(new TxGetCommand(clientFactory, authService, configService));
        AddCommand(new TxSubmitCommand(clientFactory, authService, configService));
        AddCommand(new TxStatusCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists transactions in a register.
/// </summary>
public class TxListCommand : Command
{
    public TxListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List transactions in a register")
    {
        var registerIdOption = new Option<string>(
            aliases: new[] { "--register-id", "-r" },
            description: "Register ID")
        {
            IsRequired = true
        };

        var skipOption = new Option<int?>(
            aliases: new[] { "--skip", "-s" },
            description: "Number of transactions to skip (for pagination)");

        var takeOption = new Option<int?>(
            aliases: new[] { "--take", "-t" },
            description: "Number of transactions to retrieve (default: 100)");

        AddOption(registerIdOption);
        AddOption(skipOption);
        AddOption(takeOption);

        this.SetHandler(async (registerId, skip, take) =>
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
                    ConsoleHelper.WriteError("You must be authenticated to list transactions.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var transactions = await client.ListTransactionsAsync(registerId, skip, take, $"Bearer {token}");

                // Display results
                if (transactions == null || transactions.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No transactions found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {transactions.Count} transaction(s) in register '{registerId}':");
                Console.WriteLine();

                // Display as table
                Console.WriteLine($"{"TX ID",-38} {"Type",-20} {"Sender",-40} {"Status",-12} {"Timestamp"}");
                Console.WriteLine(new string('-', 140));

                foreach (var tx in transactions)
                {
                    var timestamp = tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"{tx.Id,-38} {tx.TxType,-20} {tx.SenderWallet,-40} {tx.Status,-12} {timestamp}");
                }

                // Show pagination info
                if (skip.HasValue || take.HasValue)
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo($"Showing {transactions.Count} transaction(s) (skip: {skip ?? 0}, take: {take ?? 100})");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view transactions in this register.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list transactions: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, registerIdOption, skipOption, takeOption);
    }
}

/// <summary>
/// Gets a transaction by ID.
/// </summary>
public class TxGetCommand : Command
{
    public TxGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a transaction by ID")
    {
        var registerIdOption = new Option<string>(
            aliases: new[] { "--register-id", "-r" },
            description: "Register ID")
        {
            IsRequired = true
        };

        var txIdOption = new Option<string>(
            aliases: new[] { "--tx-id", "-t" },
            description: "Transaction ID")
        {
            IsRequired = true
        };

        AddOption(registerIdOption);
        AddOption(txIdOption);

        this.SetHandler(async (registerId, txId) =>
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
                    ConsoleHelper.WriteError("You must be authenticated to get a transaction.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var tx = await client.GetTransactionAsync(registerId, txId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Transaction details:");
                Console.WriteLine();
                Console.WriteLine($"  Transaction ID:  {tx.Id}");
                Console.WriteLine($"  Register ID:     {tx.RegisterId}");
                Console.WriteLine($"  Type:            {tx.TxType}");
                Console.WriteLine($"  Sender Wallet:   {tx.SenderWallet}");
                Console.WriteLine($"  Status:          {tx.Status}");
                Console.WriteLine($"  Timestamp:       {tx.Timestamp:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(tx.PreviousTxId))
                {
                    Console.WriteLine($"  Previous TX:     {tx.PreviousTxId}");
                }

                Console.WriteLine();
                Console.WriteLine("  Payload:");
                Console.WriteLine($"  {tx.Payload}");

                Console.WriteLine();
                Console.WriteLine("  Signature:");
                Console.WriteLine($"  {tx.Signature}");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Transaction '{txId}' not found in register '{registerId}'.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view this transaction.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get transaction: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, registerIdOption, txIdOption);
    }
}

/// <summary>
/// Submits a new transaction.
/// </summary>
public class TxSubmitCommand : Command
{
    public TxSubmitCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("submit", "Submit a new transaction to a register")
    {
        var registerIdOption = new Option<string>(
            aliases: new[] { "--register-id", "-r" },
            description: "Register ID")
        {
            IsRequired = true
        };

        var txTypeOption = new Option<string>(
            aliases: new[] { "--type", "-t" },
            description: "Transaction type")
        {
            IsRequired = true
        };

        var walletOption = new Option<string>(
            aliases: new[] { "--wallet", "-w" },
            description: "Sender wallet address")
        {
            IsRequired = true
        };

        var payloadOption = new Option<string>(
            aliases: new[] { "--payload", "-p" },
            description: "Transaction payload (JSON)")
        {
            IsRequired = true
        };

        var signatureOption = new Option<string>(
            aliases: new[] { "--signature", "-s" },
            description: "Transaction signature")
        {
            IsRequired = true
        };

        var previousTxOption = new Option<string?>(
            aliases: new[] { "--previous-tx", "-x" },
            description: "Previous transaction ID (for chaining)");

        AddOption(registerIdOption);
        AddOption(txTypeOption);
        AddOption(walletOption);
        AddOption(payloadOption);
        AddOption(signatureOption);
        AddOption(previousTxOption);

        this.SetHandler(async (registerId, txType, wallet, payload, signature, previousTx) =>
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
                    ConsoleHelper.WriteError("You must be authenticated to submit a transaction.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Build request
                var request = new SubmitTransactionRequest
                {
                    RegisterId = registerId,
                    TxType = txType,
                    SenderWallet = wallet,
                    Payload = payload,
                    Signature = signature,
                    PreviousTxId = previousTx
                };

                // Call API
                var response = await client.SubmitTransactionAsync(registerId, request, $"Bearer {token}");

                // Display results
                if (response.Status == "Pending" || response.Status == "Confirmed")
                {
                    ConsoleHelper.WriteSuccess($"Transaction submitted successfully!");
                    Console.WriteLine();
                    Console.WriteLine($"  Transaction ID:  {response.TransactionId}");
                    Console.WriteLine($"  Status:          {response.Status}");
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo($"Use 'sorcha tx status --register-id {registerId} --tx-id {response.TransactionId}' to check status.");
                }
                else
                {
                    ConsoleHelper.WriteError($"Transaction submission failed.");
                    Console.WriteLine($"  Transaction ID:  {response.TransactionId}");
                    Console.WriteLine($"  Status:          {response.Status}");
                    if (!string.IsNullOrEmpty(response.Error))
                    {
                        Console.WriteLine($"  Error:           {response.Error}");
                    }
                    Environment.ExitCode = ExitCodes.GeneralError;
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Invalid transaction. Please check your input.");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to submit transactions to this register.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to submit transaction: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, registerIdOption, txTypeOption, walletOption, payloadOption, signatureOption, previousTxOption);
    }
}

/// <summary>
/// Gets the status of a transaction.
/// </summary>
public class TxStatusCommand : Command
{
    public TxStatusCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("status", "Get the status of a transaction")
    {
        var registerIdOption = new Option<string>(
            aliases: new[] { "--register-id", "-r" },
            description: "Register ID")
        {
            IsRequired = true
        };

        var txIdOption = new Option<string>(
            aliases: new[] { "--tx-id", "-t" },
            description: "Transaction ID")
        {
            IsRequired = true
        };

        AddOption(registerIdOption);
        AddOption(txIdOption);

        this.SetHandler(async (registerId, txId) =>
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
                    ConsoleHelper.WriteError("You must be authenticated to check transaction status.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var response = await client.GetTransactionStatusAsync(registerId, txId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Transaction status:");
                Console.WriteLine();
                Console.WriteLine($"  Transaction ID:  {response.TransactionId}");
                Console.WriteLine($"  Status:          {response.Status}");

                if (!string.IsNullOrEmpty(response.Error))
                {
                    Console.WriteLine($"  Error:           {response.Error}");
                }

                // Provide status explanation
                Console.WriteLine();
                switch (response.Status.ToLowerInvariant())
                {
                    case "pending":
                        ConsoleHelper.WriteInfo("The transaction is waiting to be processed.");
                        break;
                    case "confirmed":
                        ConsoleHelper.WriteSuccess("The transaction has been confirmed and added to the register.");
                        break;
                    case "failed":
                        ConsoleHelper.WriteError("The transaction failed validation or processing.");
                        break;
                    case "rejected":
                        ConsoleHelper.WriteError("The transaction was rejected by the register.");
                        break;
                    default:
                        ConsoleHelper.WriteWarning($"Unknown status: {response.Status}");
                        break;
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Transaction '{txId}' not found in register '{registerId}'.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view transaction status.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get transaction status: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, registerIdOption, txIdOption);
    }
}
