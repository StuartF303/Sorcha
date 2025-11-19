using Sorcha.Wallet.Core.Services.Interfaces;
using Sorcha.Cli.Demo.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace Sorcha.Cli.Demo.Services;

/// <summary>
/// Manages wallet creation and storage for demo participants
/// </summary>
public class WalletDemoService
{
    private readonly IWalletService _walletService;
    private readonly LocalStorageManager _storageManager;
    private readonly ILogger<WalletDemoService> _logger;

    public WalletDemoService(
        IWalletService walletService,
        LocalStorageManager storageManager,
        ILogger<WalletDemoService> logger)
    {
        _walletService = walletService;
        _storageManager = storageManager;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all participants have wallets created and stored
    /// </summary>
    public async Task EnsureParticipantWalletsAsync(string[] participants, DemoContext context)
    {
        // Load existing wallets from storage
        var storage = await _storageManager.LoadWalletsAsync();
        var walletsCreated = 0;
        var walletsLoaded = 0;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Initializing participant wallets...", ctx =>
            {
                foreach (var participant in participants)
                {
                    ctx.Status($"Processing wallet for [cyan]{participant}[/]...");

                    // Check if wallet already exists in storage
                    if (storage.Wallets.TryGetValue(participant, out var storedWallet))
                    {
                        // Load existing wallet
                        context.ParticipantWallets[participant] = storedWallet.WalletAddress;
                        context.ParticipantMnemonics[participant] = storedWallet.Mnemonic;
                        walletsLoaded++;

                        _logger.LogInformation("Loaded existing wallet for {Participant}: {Address}",
                            participant, storedWallet.WalletAddress);
                    }
                    else
                    {
                        // Create new wallet
                        ctx.Status($"Creating new wallet for [cyan]{participant}[/]...");

                        var (wallet, mnemonic) = _walletService.CreateWalletAsync(
                            name: $"{participant} Demo Wallet",
                            algorithm: "ED25519",
                            owner: $"demo-{participant.ToLowerInvariant()}",
                            tenant: "sorcha-demo",
                            wordCount: 12,
                            passphrase: null,
                            cancellationToken: default
                        ).GetAwaiter().GetResult(); // Blocking for Spectre.Console status

                        // Store in context
                        context.ParticipantWallets[participant] = wallet.Address;
                        context.ParticipantMnemonics[participant] = mnemonic.ToString();

                        // Store in persistent storage
                        storage.Wallets[participant] = new StoredWallet
                        {
                            ParticipantName = participant,
                            WalletAddress = wallet.Address,
                            Mnemonic = mnemonic.ToString(),
                            Algorithm = wallet.Algorithm,
                            CreatedAt = DateTime.UtcNow
                        };

                        walletsCreated++;

                        _logger.LogInformation("Created new wallet for {Participant}: {Address}",
                            participant, wallet.Address);
                    }
                }

                // Save updated storage
                if (walletsCreated > 0)
                {
                    ctx.Status("Saving wallet data...");
                    storage.LastModified = DateTime.UtcNow;
                    _storageManager.SaveWalletsAsync(storage).GetAwaiter().GetResult();
                }
            });

        // Display summary
        var summary = new Panel(
            $"[green]✓[/] Wallets initialized\n" +
            $"  • [cyan]{walletsLoaded}[/] loaded from storage\n" +
            $"  • [yellow]{walletsCreated}[/] newly created\n" +
            $"  • [dim]Storage: {_storageManager.WalletsFilePath}[/]")
        {
            Header = new PanelHeader("[bold]Wallet Summary[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (walletsCreated > 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️  WARNING:[/] Mnemonics stored in plain text at:");
            AnsiConsole.MarkupLine($"   [dim]{_storageManager.WalletsFilePath}[/]");
            AnsiConsole.MarkupLine("[dim]   This is INSECURE and for demo purposes only![/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Clears all stored wallet data
    /// </summary>
    public async Task ClearAllWalletsAsync()
    {
        await _storageManager.ClearWalletsAsync();
        _logger.LogWarning("All wallet data cleared from storage");
    }

    /// <summary>
    /// Gets the mnemonic for a participant (INSECURE - demo only!)
    /// </summary>
    public string? GetParticipantMnemonic(string participant, DemoContext context)
    {
        return context.ParticipantMnemonics.TryGetValue(participant, out var mnemonic)
            ? mnemonic
            : null;
    }

    /// <summary>
    /// Gets the wallet address for a participant
    /// </summary>
    public string? GetParticipantWalletAddress(string participant, DemoContext context)
    {
        return context.ParticipantWallets.TryGetValue(participant, out var address)
            ? address
            : null;
    }
}
