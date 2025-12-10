using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Transaction command structure and options.
/// </summary>
public class TransactionCommandsTests
{
    [Fact]
    public void TransactionCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new TransactionCommand();
        command.Name.Should().Be("tx");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TransactionCommand_ShouldHaveFourSubcommands()
    {
        var command = new TransactionCommand();
        command.Subcommands.Should().HaveCount(4);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "submit", "status" });
    }

    #region TxListCommand Tests

    [Fact]
    public void TxListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new TxListCommand();
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TxListCommand_ShouldHaveRequiredRegisterIdOption()
    {
        var command = new TxListCommand();
        var registerIdOption = command.Options.FirstOrDefault(o => o.Name == "register-id");
        registerIdOption.Should().NotBeNull();
        registerIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxListCommand_ShouldHaveOptionalSkipOption()
    {
        var command = new TxListCommand();
        var skipOption = command.Options.FirstOrDefault(o => o.Name == "skip");
        skipOption.Should().NotBeNull();
        skipOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void TxListCommand_ShouldHaveOptionalTakeOption()
    {
        var command = new TxListCommand();
        var takeOption = command.Options.FirstOrDefault(o => o.Name == "take");
        takeOption.Should().NotBeNull();
        takeOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task TxListCommand_ShouldExecuteSuccessfully_WithRequiredRegisterId()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxListCommand());
        var exitCode = await rootCommand.InvokeAsync("list --register-id reg-123");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task TxListCommand_ShouldExecuteSuccessfully_WithPaginationOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxListCommand());
        var exitCode = await rootCommand.InvokeAsync("list --register-id reg-123 --skip 10 --take 50");
        exitCode.Should().Be(0);
    }

    #endregion

    #region TxGetCommand Tests

    [Fact]
    public void TxGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new TxGetCommand();
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TxGetCommand_ShouldHaveRequiredRegisterIdOption()
    {
        var command = new TxGetCommand();
        var registerIdOption = command.Options.FirstOrDefault(o => o.Name == "register-id");
        registerIdOption.Should().NotBeNull();
        registerIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxGetCommand_ShouldHaveRequiredTxIdOption()
    {
        var command = new TxGetCommand();
        var txIdOption = command.Options.FirstOrDefault(o => o.Name == "tx-id");
        txIdOption.Should().NotBeNull();
        txIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task TxGetCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxGetCommand());
        var exitCode = await rootCommand.InvokeAsync("get --register-id reg-123 --tx-id tx-456");
        exitCode.Should().Be(0);
    }

    #endregion

    #region TxSubmitCommand Tests

    [Fact]
    public void TxSubmitCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new TxSubmitCommand();
        command.Name.Should().Be("submit");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveRequiredRegisterIdOption()
    {
        var command = new TxSubmitCommand();
        var registerIdOption = command.Options.FirstOrDefault(o => o.Name == "register-id");
        registerIdOption.Should().NotBeNull();
        registerIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveRequiredTypeOption()
    {
        var command = new TxSubmitCommand();
        var typeOption = command.Options.FirstOrDefault(o => o.Name == "type");
        typeOption.Should().NotBeNull();
        typeOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveRequiredWalletOption()
    {
        var command = new TxSubmitCommand();
        var walletOption = command.Options.FirstOrDefault(o => o.Name == "wallet");
        walletOption.Should().NotBeNull();
        walletOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveRequiredPayloadOption()
    {
        var command = new TxSubmitCommand();
        var payloadOption = command.Options.FirstOrDefault(o => o.Name == "payload");
        payloadOption.Should().NotBeNull();
        payloadOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveOptionalPreviousTxOption()
    {
        var command = new TxSubmitCommand();
        var previousTxOption = command.Options.FirstOrDefault(o => o.Name == "previous-tx");
        previousTxOption.Should().NotBeNull();
        previousTxOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void TxSubmitCommand_ShouldHaveOptionalSignOption()
    {
        var command = new TxSubmitCommand();
        var signOption = command.Options.FirstOrDefault(o => o.Name == "sign");
        signOption.Should().NotBeNull();
        signOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task TxSubmitCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxSubmitCommand());
        var exitCode = await rootCommand.InvokeAsync("submit --register-id reg-123 --type data --wallet wallet-456 --payload '{\"test\":\"data\"}'");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task TxSubmitCommand_ShouldExecuteSuccessfully_WithPreviousTx()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxSubmitCommand());
        var exitCode = await rootCommand.InvokeAsync("submit --register-id reg-123 --type data --wallet wallet-456 --payload '{\"test\":\"data\"}' --previous-tx tx-000");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task TxSubmitCommand_ShouldExecuteSuccessfully_WithAutoSign()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxSubmitCommand());
        var exitCode = await rootCommand.InvokeAsync("submit --register-id reg-123 --type data --wallet wallet-456 --payload '{\"test\":\"data\"}' --sign");
        exitCode.Should().Be(0);
    }

    #endregion

    #region TxStatusCommand Tests

    [Fact]
    public void TxStatusCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new TxStatusCommand();
        command.Name.Should().Be("status");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TxStatusCommand_ShouldHaveRequiredRegisterIdOption()
    {
        var command = new TxStatusCommand();
        var registerIdOption = command.Options.FirstOrDefault(o => o.Name == "register-id");
        registerIdOption.Should().NotBeNull();
        registerIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TxStatusCommand_ShouldHaveRequiredTxIdOption()
    {
        var command = new TxStatusCommand();
        var txIdOption = command.Options.FirstOrDefault(o => o.Name == "tx-id");
        txIdOption.Should().NotBeNull();
        txIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task TxStatusCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new TxStatusCommand());
        var exitCode = await rootCommand.InvokeAsync("status --register-id reg-123 --tx-id tx-456");
        exitCode.Should().Be(0);
    }

    #endregion
}
