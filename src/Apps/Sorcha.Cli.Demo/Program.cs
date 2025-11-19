using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.Cli.Demo;
using Sorcha.Cli.Demo.Services;
using Sorcha.Cli.Demo.Utilities;
using Sorcha.Cli.Demo.Examples;
using Sorcha.Blueprint.Engine;
using Sorcha.Wallet.Core.Services.Implementation;
using Sorcha.Wallet.Core.Services.Interfaces;
using Sorcha.Wallet.Core.Repositories.Implementation;
using Sorcha.Wallet.Core.Repositories.Interfaces;
using Sorcha.Wallet.Core.Encryption.Providers;
using Sorcha.Wallet.Core.Encryption.Interfaces;
using Sorcha.Wallet.Core.Events.Publishers;
using Sorcha.Wallet.Core.Events.Interfaces;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Utilities;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Implementation;
using Spectre.Console;

// Build service provider with dependency injection
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise, only show warnings/errors
});

// Register cryptography services
services.AddSingleton<ICryptoModule, CryptoModule>();
services.AddSingleton<IHashProvider, HashProvider>();
services.AddSingleton<IWalletUtilities, WalletUtilities>();

// Register wallet encryption and events
services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Register wallet repository and services
services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();
services.AddSingleton<IKeyManagementService, KeyManagementService>();
services.AddSingleton<ITransactionService, TransactionService>();
services.AddSingleton<IDelegationService, DelegationService>();
services.AddSingleton<IWalletService, WalletManager>();

// Register blueprint execution engine services
services.AddSingleton<IExecutionEngine, ExecutionEngine>();
services.AddSingleton<IActionProcessor, ActionProcessor>();
services.AddSingleton<ISchemaValidator, SchemaValidator>();
services.AddSingleton<IJsonLogicEvaluator, JsonLogicEvaluator>();
services.AddSingleton<IDisclosureProcessor, DisclosureProcessor>();
services.AddSingleton<IRoutingEngine, RoutingEngine>();

// Register demo services
services.AddSingleton<DemoContext>();
services.AddSingleton<WalletDemoService>();
services.AddSingleton<BlueprintExecutor>();
services.AddSingleton<TransactionChainBuilder>();
services.AddSingleton<ConsoleRenderer>();
services.AddSingleton<LocalStorageManager>();

// Register JSON blueprint services
services.AddSingleton<JsonBlueprintLoader>();
services.AddSingleton<JsonETemplateEngine>();

// Register blueprint examples (now loading from JSON files with JSON-e templating)
services.AddSingleton<IBlueprintExample>(sp => new JsonBlueprintExample(
    fileName: "expense-approval.json",
    name: "Expense Approval Workflow",
    description: "Multi-step expense approval with conditional routing based on amount",
    participants: new[] { "Employee", "Manager", "Finance", "CFO" },
    loader: sp.GetRequiredService<JsonBlueprintLoader>(),
    templateEngine: sp.GetRequiredService<JsonETemplateEngine>(),
    logger: sp.GetRequiredService<ILogger<JsonBlueprintExample>>()
));

services.AddSingleton<IBlueprintExample>(sp => new JsonBlueprintExample(
    fileName: "purchase-order.json",
    name: "Purchase Order Processing",
    description: "Purchase order workflow with supplier and shipping coordination",
    participants: new[] { "Buyer", "Supplier", "Shipping", "Finance" },
    loader: sp.GetRequiredService<JsonBlueprintLoader>(),
    templateEngine: sp.GetRequiredService<JsonETemplateEngine>(),
    logger: sp.GetRequiredService<ILogger<JsonBlueprintExample>>()
));

services.AddSingleton<IBlueprintExample>(sp => new JsonBlueprintExample(
    fileName: "loan-application.json",
    name: "Loan Application Process",
    description: "Loan application with credit check and conditional approval routing",
    participants: new[] { "Applicant", "LoanOfficer", "CreditBureau", "Underwriter" },
    loader: sp.GetRequiredService<JsonBlueprintLoader>(),
    templateEngine: sp.GetRequiredService<JsonETemplateEngine>(),
    logger: sp.GetRequiredService<ILogger<JsonBlueprintExample>>()
));

var serviceProvider = services.BuildServiceProvider();

// Display welcome banner
AnsiConsole.Clear();
AnsiConsole.Write(new FigletText("Sorcha Demo")
    .Color(Color.Cyan1));

AnsiConsole.MarkupLine("[dim]Technology Demonstrator - Blueprint Execution with HD Wallets[/]");
AnsiConsole.WriteLine();

// Get demo context and renderer
var context = serviceProvider.GetRequiredService<DemoContext>();
var renderer = serviceProvider.GetRequiredService<ConsoleRenderer>();

// Get all registered blueprint examples
var blueprintExamples = serviceProvider.GetServices<IBlueprintExample>().ToList();

// Main application loop
var running = true;
while (running)
{
    var choice = renderer.ShowMainMenu();

    switch (choice)
    {
        case "expense":
            var expenseExample = blueprintExamples.FirstOrDefault(e => e.FileName == "expense-approval.json");
            if (expenseExample != null)
                await RunBlueprintDemo(expenseExample, context, renderer, serviceProvider);
            break;

        case "purchase":
            var purchaseExample = blueprintExamples.FirstOrDefault(e => e.FileName == "purchase-order.json");
            if (purchaseExample != null)
                await RunBlueprintDemo(purchaseExample, context, renderer, serviceProvider);
            break;

        case "loan":
            var loanExample = blueprintExamples.FirstOrDefault(e => e.FileName == "loan-application.json");
            if (loanExample != null)
                await RunBlueprintDemo(loanExample, context, renderer, serviceProvider);
            break;

        case "settings":
            renderer.ShowSettingsMenu(context);
            break;

        case "exit":
            running = false;
            break;
    }
}

AnsiConsole.MarkupLine("\n[cyan]Thank you for using Sorcha Demo![/]");

/// <summary>
/// Runs a blueprint demonstration with the specified example
/// </summary>
static async Task RunBlueprintDemo(
    IBlueprintExample example,
    DemoContext context,
    ConsoleRenderer renderer,
    IServiceProvider serviceProvider)
{
    try
    {
        AnsiConsole.Clear();

        // Show blueprint overview
        renderer.ShowBlueprintOverview(example);

        // Initialize wallets for participants
        var walletService = serviceProvider.GetRequiredService<WalletDemoService>();
        await walletService.EnsureParticipantWalletsAsync(example.GetParticipants(), context);

        // Show wallet assignments
        renderer.ShowWalletAssignments(example.GetParticipants(), context);

        if (!AnsiConsole.Confirm("Ready to execute blueprint?"))
            return;

        // Execute blueprint
        var executor = serviceProvider.GetRequiredService<BlueprintExecutor>();
        await executor.ExecuteAsync(example, context, renderer);

        // Show transaction chain summary
        renderer.ShowTransactionChainSummary(context);

        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex);
        AnsiConsole.Markup("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}

/// <summary>
/// Interface for blueprint examples loaded from JSON files
/// </summary>
public interface IBlueprintExample
{
    string Name { get; }
    string Description { get; }
    string FileName { get; }
    string[] GetParticipants();
    Task<Sorcha.Blueprint.Models.Blueprint> GetBlueprintAsync(Dictionary<string, string> participantWallets);
}
