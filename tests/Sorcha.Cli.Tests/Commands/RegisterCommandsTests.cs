using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Register command structure and options.
/// </summary>
public class RegisterCommandsTests
{
    [Fact]
    public void RegisterCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterCommand();
        command.Name.Should().Be("register");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterCommand_ShouldHaveFourSubcommands()
    {
        var command = new RegisterCommand();
        command.Subcommands.Should().HaveCount(4);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "create", "delete" });
    }

    #region RegisterListCommand Tests

    [Fact]
    public void RegisterListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterListCommand();
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterListCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterListCommand());
        var exitCode = await rootCommand.InvokeAsync("list");
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterGetCommand Tests

    [Fact]
    public void RegisterGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterGetCommand();
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterGetCommand_ShouldHaveRequiredIdOption()
    {
        var command = new RegisterGetCommand();
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterGetCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterGetCommand());
        var exitCode = await rootCommand.InvokeAsync("get --id test-register-123");
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterCreateCommand Tests

    [Fact]
    public void RegisterCreateCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterCreateCommand();
        command.Name.Should().Be("create");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveRequiredNameOption()
    {
        var command = new RegisterCreateCommand();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveRequiredOrgIdOption()
    {
        var command = new RegisterCreateCommand();
        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveOptionalDescriptionOption()
    {
        var command = new RegisterCreateCommand();
        var descOption = command.Options.FirstOrDefault(o => o.Name == "description");
        descOption.Should().NotBeNull();
        descOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCreateCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterCreateCommand());
        var exitCode = await rootCommand.InvokeAsync("create --name TestReg --org-id org-123 --description \"Test register\"");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RegisterCreateCommand_ShouldExecuteSuccessfully_WithoutOptionalDescription()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterCreateCommand());
        var exitCode = await rootCommand.InvokeAsync("create --name TestReg --org-id org-123");
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterDeleteCommand Tests

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterDeleteCommand();
        command.Name.Should().Be("delete");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveRequiredIdOption()
    {
        var command = new RegisterDeleteCommand();
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveOptionalYesOption()
    {
        var command = new RegisterDeleteCommand();
        var yesOption = command.Options.FirstOrDefault(o => o.Name == "yes");
        yesOption.Should().NotBeNull();
        yesOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterDeleteCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterDeleteCommand());
        var exitCode = await rootCommand.InvokeAsync("delete --id test-register-123");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RegisterDeleteCommand_ShouldExecuteSuccessfully_WithYesFlag()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new RegisterDeleteCommand());
        var exitCode = await rootCommand.InvokeAsync("delete --id test-register-123 --yes");
        exitCode.Should().Be(0);
    }

    #endregion
}
