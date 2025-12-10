using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;
using Xunit;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for organization commands.
/// </summary>
public class OrganizationCommandsTests
{
    [Fact]
    public void OrganizationCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new OrganizationCommand();

        // Assert
        command.Name.Should().Be("org");
        command.Description.Should().Be("Manage organizations");
    }

    [Fact]
    public void OrganizationCommand_ShouldHaveAllSubcommands()
    {
        // Arrange & Act
        var command = new OrganizationCommand();

        // Assert
        command.Subcommands.Should().HaveCount(5);
        command.Subcommands.Should().Contain(c => c.Name == "list");
        command.Subcommands.Should().Contain(c => c.Name == "get");
        command.Subcommands.Should().Contain(c => c.Name == "create");
        command.Subcommands.Should().Contain(c => c.Name == "update");
        command.Subcommands.Should().Contain(c => c.Name == "delete");
    }

    [Fact]
    public void OrgListCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new OrgListCommand();

        // Assert
        command.Name.Should().Be("list");
        command.Description.Should().Be("List all organizations");
    }

    [Fact]
    public void OrgGetCommand_ShouldHaveRequiredIdOption()
    {
        // Arrange & Act
        var command = new OrgGetCommand();

        // Assert
        command.Name.Should().Be("get");
        command.Description.Should().Be("Get an organization by ID");

        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void OrgCreateCommand_ShouldHaveRequiredNameOption()
    {
        // Arrange & Act
        var command = new OrgCreateCommand();

        // Assert
        command.Name.Should().Be("create");
        command.Description.Should().Be("Create a new organization");

        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void OrgCreateCommand_ShouldHaveOptionalSubdomainAndDescriptionOptions()
    {
        // Arrange & Act
        var command = new OrgCreateCommand();

        // Assert
        var subdomainOption = command.Options.FirstOrDefault(o => o.Name == "subdomain");
        subdomainOption.Should().NotBeNull();
        subdomainOption!.IsRequired.Should().BeFalse();

        var descriptionOption = command.Options.FirstOrDefault(o => o.Name == "description");
        descriptionOption.Should().NotBeNull();
        descriptionOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void OrgUpdateCommand_ShouldHaveRequiredIdOption()
    {
        // Arrange & Act
        var command = new OrgUpdateCommand();

        // Assert
        command.Name.Should().Be("update");
        command.Description.Should().Be("Update an organization");

        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void OrgUpdateCommand_ShouldHaveOptionalNameAndDescriptionOptions()
    {
        // Arrange & Act
        var command = new OrgUpdateCommand();

        // Assert
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeFalse();

        var descriptionOption = command.Options.FirstOrDefault(o => o.Name == "description");
        descriptionOption.Should().NotBeNull();
        descriptionOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void OrgDeleteCommand_ShouldHaveRequiredIdOption()
    {
        // Arrange & Act
        var command = new OrgDeleteCommand();

        // Assert
        command.Name.Should().Be("delete");
        command.Description.Should().Be("Delete an organization");

        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void OrgDeleteCommand_ShouldHaveOptionalYesOption()
    {
        // Arrange & Act
        var command = new OrgDeleteCommand();

        // Assert
        var yesOption = command.Options.FirstOrDefault(o => o.Name == "yes");
        yesOption.Should().NotBeNull();
        yesOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task OrgListCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new OrgListCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("list");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task OrgGetCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new OrgGetCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("get --id test-org-123");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task OrgCreateCommand_ShouldExecuteSuccessfully_WithRequiredName()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new OrgCreateCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("create --name \"Test Org\"");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task OrgCreateCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new OrgCreateCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("create --name \"Test Org\" --subdomain testorg --description \"Test organization\"");

        // Assert
        exitCode.Should().Be(0);
    }
}
