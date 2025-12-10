using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Wallet command structure and options.
/// </summary>
public class WalletCommandsTests
{
    [Fact]
    public void WalletCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletCommand();
        command.Name.Should().Be("wallet");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletCommand_ShouldHaveSixSubcommands()
    {
        var command = new WalletCommand();
        command.Subcommands.Should().HaveCount(6);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "create", "recover", "delete", "sign" });
    }

    #region WalletListCommand Tests

    [Fact]
    public void WalletListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletListCommand();
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WalletListCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletListCommand());
        var exitCode = await rootCommand.InvokeAsync("list");
        exitCode.Should().Be(0);
    }

    #endregion

    #region WalletGetCommand Tests

    [Fact]
    public void WalletGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletGetCommand();
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletGetCommand_ShouldHaveRequiredAddressOption()
    {
        var command = new WalletGetCommand();
        var addressOption = command.Options.FirstOrDefault(o => o.Name == "address");
        addressOption.Should().NotBeNull();
        addressOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task WalletGetCommand_ShouldExecuteSuccessfully_WithRequiredAddress()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletGetCommand());
        var exitCode = await rootCommand.InvokeAsync("get --address wallet-123");
        exitCode.Should().Be(0);
    }

    #endregion

    #region WalletCreateCommand Tests

    [Fact]
    public void WalletCreateCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletCreateCommand();
        command.Name.Should().Be("create");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletCreateCommand_ShouldHaveRequiredNameOption()
    {
        var command = new WalletCreateCommand();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void WalletCreateCommand_ShouldHaveOptionalAlgorithmOption()
    {
        var command = new WalletCreateCommand();
        var algorithmOption = command.Options.FirstOrDefault(o => o.Name == "algorithm");
        algorithmOption.Should().NotBeNull();
        algorithmOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void WalletCreateCommand_ShouldHaveOptionalWordCountOption()
    {
        var command = new WalletCreateCommand();
        var wordCountOption = command.Options.FirstOrDefault(o => o.Name == "word-count");
        wordCountOption.Should().NotBeNull();
        wordCountOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void WalletCreateCommand_ShouldHaveOptionalPassphraseOption()
    {
        var command = new WalletCreateCommand();
        var passphraseOption = command.Options.FirstOrDefault(o => o.Name == "passphrase");
        passphraseOption.Should().NotBeNull();
        passphraseOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task WalletCreateCommand_ShouldExecuteSuccessfully_WithRequiredName()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletCreateCommand());
        var exitCode = await rootCommand.InvokeAsync("create --name \"My Wallet\"");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task WalletCreateCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletCreateCommand());
        var exitCode = await rootCommand.InvokeAsync("create --name \"My Wallet\" --algorithm ED25519 --word-count 12 --passphrase secret");
        exitCode.Should().Be(0);
    }

    #endregion

    #region WalletRecoverCommand Tests

    [Fact]
    public void WalletRecoverCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletRecoverCommand();
        command.Name.Should().Be("recover");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletRecoverCommand_ShouldHaveRequiredNameOption()
    {
        var command = new WalletRecoverCommand();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void WalletRecoverCommand_ShouldHaveRequiredMnemonicOption()
    {
        var command = new WalletRecoverCommand();
        var mnemonicOption = command.Options.FirstOrDefault(o => o.Name == "mnemonic");
        mnemonicOption.Should().NotBeNull();
        mnemonicOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void WalletRecoverCommand_ShouldHaveOptionalAlgorithmOption()
    {
        var command = new WalletRecoverCommand();
        var algorithmOption = command.Options.FirstOrDefault(o => o.Name == "algorithm");
        algorithmOption.Should().NotBeNull();
        algorithmOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void WalletRecoverCommand_ShouldHaveOptionalPassphraseOption()
    {
        var command = new WalletRecoverCommand();
        var passphraseOption = command.Options.FirstOrDefault(o => o.Name == "passphrase");
        passphraseOption.Should().NotBeNull();
        passphraseOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task WalletRecoverCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletRecoverCommand());
        var exitCode = await rootCommand.InvokeAsync("recover --name \"Recovered Wallet\" --mnemonic \"word1 word2 word3 word4 word5 word6 word7 word8 word9 word10 word11 word12\"");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task WalletRecoverCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletRecoverCommand());
        var exitCode = await rootCommand.InvokeAsync("recover --name \"Recovered Wallet\" --algorithm ED25519 --mnemonic \"word1 word2 word3 word4 word5 word6 word7 word8 word9 word10 word11 word12\" --passphrase secret");
        exitCode.Should().Be(0);
    }

    #endregion

    #region WalletDeleteCommand Tests

    [Fact]
    public void WalletDeleteCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletDeleteCommand();
        command.Name.Should().Be("delete");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletDeleteCommand_ShouldHaveRequiredAddressOption()
    {
        var command = new WalletDeleteCommand();
        var addressOption = command.Options.FirstOrDefault(o => o.Name == "address");
        addressOption.Should().NotBeNull();
        addressOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void WalletDeleteCommand_ShouldHaveOptionalYesOption()
    {
        var command = new WalletDeleteCommand();
        var yesOption = command.Options.FirstOrDefault(o => o.Name == "yes");
        yesOption.Should().NotBeNull();
        yesOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task WalletDeleteCommand_ShouldExecuteSuccessfully_WithRequiredAddress()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletDeleteCommand());
        var exitCode = await rootCommand.InvokeAsync("delete --address wallet-123");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task WalletDeleteCommand_ShouldExecuteSuccessfully_WithYesFlag()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletDeleteCommand());
        var exitCode = await rootCommand.InvokeAsync("delete --address wallet-123 --yes");
        exitCode.Should().Be(0);
    }

    #endregion

    #region WalletSignCommand Tests

    [Fact]
    public void WalletSignCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new WalletSignCommand();
        command.Name.Should().Be("sign");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WalletSignCommand_ShouldHaveRequiredAddressOption()
    {
        var command = new WalletSignCommand();
        var addressOption = command.Options.FirstOrDefault(o => o.Name == "address");
        addressOption.Should().NotBeNull();
        addressOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void WalletSignCommand_ShouldHaveRequiredDataOption()
    {
        var command = new WalletSignCommand();
        var dataOption = command.Options.FirstOrDefault(o => o.Name == "data");
        dataOption.Should().NotBeNull();
        dataOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task WalletSignCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new WalletSignCommand());
        var exitCode = await rootCommand.InvokeAsync("sign --address wallet-123 --data dGVzdCBkYXRh");
        exitCode.Should().Be(0);
    }

    #endregion
}
