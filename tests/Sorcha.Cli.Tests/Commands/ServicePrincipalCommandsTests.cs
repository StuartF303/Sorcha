using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;
using Xunit;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for service principal commands.
/// </summary>
public class ServicePrincipalCommandsTests
{
    [Fact]
    public void ServicePrincipalCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new ServicePrincipalCommand();

        // Assert
        command.Name.Should().Be("principal");
        command.Description.Should().Be("Manage service principals within organizations");
    }

    [Fact]
    public void ServicePrincipalCommand_ShouldHaveAllSubcommands()
    {
        // Arrange & Act
        var command = new ServicePrincipalCommand();

        // Assert
        command.Subcommands.Should().HaveCount(5);
        command.Subcommands.Should().Contain(c => c.Name == "list");
        command.Subcommands.Should().Contain(c => c.Name == "get");
        command.Subcommands.Should().Contain(c => c.Name == "create");
        command.Subcommands.Should().Contain(c => c.Name == "delete");
        command.Subcommands.Should().Contain(c => c.Name == "rotate-secret");
    }

    [Fact]
    public void PrincipalListCommand_ShouldHaveRequiredOrgIdOption()
    {
        // Arrange & Act
        var command = new PrincipalListCommand();

        // Assert
        command.Name.Should().Be("list");
        command.Description.Should().Be("List all service principals in an organization");

        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PrincipalGetCommand_ShouldHaveRequiredOrgIdAndClientIdOptions()
    {
        // Arrange & Act
        var command = new PrincipalGetCommand();

        // Assert
        command.Name.Should().Be("get");
        command.Description.Should().Be("Get a service principal by client ID");

        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();

        var clientIdOption = command.Options.FirstOrDefault(o => o.Name == "client-id");
        clientIdOption.Should().NotBeNull();
        clientIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PrincipalCreateCommand_ShouldHaveRequiredOptions()
    {
        // Arrange & Act
        var command = new PrincipalCreateCommand();

        // Assert
        command.Name.Should().Be("create");
        command.Description.Should().Be("Create a new service principal in an organization");

        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();

        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PrincipalCreateCommand_ShouldHaveOptionalDescriptionRolesAndExpirationOptions()
    {
        // Arrange & Act
        var command = new PrincipalCreateCommand();

        // Assert
        var descriptionOption = command.Options.FirstOrDefault(o => o.Name == "description");
        descriptionOption.Should().NotBeNull();
        descriptionOption!.IsRequired.Should().BeFalse();

        var rolesOption = command.Options.FirstOrDefault(o => o.Name == "roles");
        rolesOption.Should().NotBeNull();
        rolesOption!.IsRequired.Should().BeFalse();

        var expiresInDaysOption = command.Options.FirstOrDefault(o => o.Name == "expires-in-days");
        expiresInDaysOption.Should().NotBeNull();
        expiresInDaysOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PrincipalDeleteCommand_ShouldHaveRequiredOrgIdAndClientIdOptions()
    {
        // Arrange & Act
        var command = new PrincipalDeleteCommand();

        // Assert
        command.Name.Should().Be("delete");
        command.Description.Should().Be("Delete a service principal");

        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();

        var clientIdOption = command.Options.FirstOrDefault(o => o.Name == "client-id");
        clientIdOption.Should().NotBeNull();
        clientIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PrincipalDeleteCommand_ShouldHaveOptionalYesOption()
    {
        // Arrange & Act
        var command = new PrincipalDeleteCommand();

        // Assert
        var yesOption = command.Options.FirstOrDefault(o => o.Name == "yes");
        yesOption.Should().NotBeNull();
        yesOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PrincipalRotateSecretCommand_ShouldHaveRequiredOrgIdAndClientIdOptions()
    {
        // Arrange & Act
        var command = new PrincipalRotateSecretCommand();

        // Assert
        command.Name.Should().Be("rotate-secret");
        command.Description.Should().Be("Rotate the client secret for a service principal");

        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.IsRequired.Should().BeTrue();

        var clientIdOption = command.Options.FirstOrDefault(o => o.Name == "client-id");
        clientIdOption.Should().NotBeNull();
        clientIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PrincipalRotateSecretCommand_ShouldHaveOptionalExpiresInDaysOption()
    {
        // Arrange & Act
        var command = new PrincipalRotateSecretCommand();

        // Assert
        var expiresInDaysOption = command.Options.FirstOrDefault(o => o.Name == "expires-in-days");
        expiresInDaysOption.Should().NotBeNull();
        expiresInDaysOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PrincipalListCommand_ShouldExecuteSuccessfully_WithRequiredOrgId()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalListCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("list --org-id test-org-123");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PrincipalGetCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalGetCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("get --org-id test-org-123 --client-id client-456");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PrincipalCreateCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalCreateCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("create --org-id test-org-123 --name \"API Service\"");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PrincipalCreateCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalCreateCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("create --org-id test-org-123 --name \"API Service\" --description \"Backend API\" --roles Service --expires-in-days 90");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PrincipalRotateSecretCommand_ShouldExecuteSuccessfully_WithRequiredOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalRotateSecretCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("rotate-secret --org-id test-org-123 --client-id client-456");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PrincipalRotateSecretCommand_ShouldExecuteSuccessfully_WithExpirationOption()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PrincipalRotateSecretCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("rotate-secret --org-id test-org-123 --client-id client-456 --expires-in-days 365");

        // Assert
        exitCode.Should().Be(0);
    }
}
