using System.CommandLine;
using System.CommandLine.Parsing;
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
        Subcommands.Add(new TxListCommand(clientFactory, authService, configService));
        Subcommands.Add(new TxGetCommand(clientFactory, authService, configService));
        Subcommands.Add(new TxSubmitCommand(clientFactory, authService, configService));
        Subcommands.Add(new TxStatusCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists transactions in a register.
/// </summary>
public class TxListCommand : Command
{
    private readonly Option<string> _registerIdOption;
    private readonly Option<int?> _skipOption;
    private readonly Option<int?> _takeOption;

    public TxListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List transactions in a register")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID",
            Required = true
        };

        _skipOption = new Option<int?>("--skip", "-s")
        {
            Description = "Number of transactions to skip (for pagination)"
        };

        _takeOption = new Option<int?>("--take", "-t")
        {
            Description = "Number of transactions to retrieve (default: 100)"
        };

        Options.Add(_registerIdOption);
        Options.Add(_skipOption);
        Options.Add(_takeOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;
            var skip = parseResult.GetValue(_skipOption);
            var take = parseResult.GetValue(_takeOption);

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
                    return ExitCodes.AuthenticationError;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var transactions = await client.ListTransactionsAsync(registerId, skip, take, $"Bearer {token}");

                // Display results
                if (transactions == null || transactions.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No transactions found.");
                    return ExitCodes.Success;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view transactions in this register.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list transactions: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets a transaction by ID.
/// </summary>
public class TxGetCommand : Command
{
    private readonly Option<string> _registerIdOption;
    private readonly Option<string> _txIdOption;

    public TxGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a transaction by ID")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID",
            Required = true
        };

        _txIdOption = new Option<string>("--tx-id", "-t")
        {
            Description = "Transaction ID",
            Required = true
        };

        Options.Add(_registerIdOption);
        Options.Add(_txIdOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;
            var txId = parseResult.GetValue(_txIdOption)!;

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
                    return ExitCodes.AuthenticationError;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Transaction '{txId}' not found in register '{registerId}'.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view this transaction.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get transaction: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Submits a new transaction.
/// </summary>
public class TxSubmitCommand : Command
{
    private readonly Option<string> _registerIdOption;
    private readonly Option<string> _txTypeOption;
    private readonly Option<string> _walletOption;
    private readonly Option<string> _payloadOption;
    private readonly Option<string> _signatureOption;
    private readonly Option<string?> _previousTxOption;

    public TxSubmitCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("submit", "Submit a new transaction to a register")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID",
            Required = true
        };

        _txTypeOption = new Option<string>("--type", "-t")
        {
            Description = "Transaction type",
            Required = true
        };

        _walletOption = new Option<string>("--wallet", "-w")
        {
            Description = "Sender wallet address",
            Required = true
        };

        _payloadOption = new Option<string>("--payload", "-p")
        {
            Description = "Transaction payload (JSON)",
            Required = true
        };

        _signatureOption = new Option<string>("--signature", "-s")
        {
            Description = "Transaction signature",
            Required = true
        };

        _previousTxOption = new Option<string?>("--previous-tx", "-x")
        {
            Description = "Previous transaction ID (for chaining)"
        };

        Options.Add(_registerIdOption);
        Options.Add(_txTypeOption);
        Options.Add(_walletOption);
        Options.Add(_payloadOption);
        Options.Add(_signatureOption);
        Options.Add(_previousTxOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;
            var txType = parseResult.GetValue(_txTypeOption)!;
            var wallet = parseResult.GetValue(_walletOption)!;
            var payload = parseResult.GetValue(_payloadOption)!;
            var signature = parseResult.GetValue(_signatureOption)!;
            var previousTx = parseResult.GetValue(_previousTxOption);

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
                    return ExitCodes.AuthenticationError;
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
                    return ExitCodes.Success;
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
                    return ExitCodes.GeneralError;
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Invalid transaction. Please check your input.");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to submit transactions to this register.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to submit transaction: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets the status of a transaction.
/// </summary>
public class TxStatusCommand : Command
{
    private readonly Option<string> _registerIdOption;
    private readonly Option<string> _txIdOption;

    public TxStatusCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("status", "Get the status of a transaction")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID",
            Required = true
        };

        _txIdOption = new Option<string>("--tx-id", "-t")
        {
            Description = "Transaction ID",
            Required = true
        };

        Options.Add(_registerIdOption);
        Options.Add(_txIdOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;
            var txId = parseResult.GetValue(_txIdOption)!;

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
                    return ExitCodes.AuthenticationError;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Transaction '{txId}' not found in register '{registerId}'.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view transaction status.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get transaction status: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}
