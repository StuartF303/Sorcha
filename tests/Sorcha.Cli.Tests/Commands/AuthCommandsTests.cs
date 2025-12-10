using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;
using Xunit;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for authentication commands.
/// </summary>
public class AuthCommandsTests
{
    [Fact]
    public void AuthCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new AuthCommand();

        // Assert
        command.Name.Should().Be("auth");
        command.Description.Should().Be("Manage authentication and login sessions");
    }

    [Fact]
    public void AuthCommand_ShouldHaveAllSubcommands()
    {
        // Arrange & Act
        var command = new AuthCommand();

        // Assert
        command.Subcommands.Should().HaveCount(3);
        command.Subcommands.Should().Contain(c => c.Name == "login");
        command.Subcommands.Should().Contain(c => c.Name == "logout");
        command.Subcommands.Should().Contain(c => c.Name == "status");
    }

    [Fact]
    public void AuthLoginCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new AuthLoginCommand();

        // Assert
        command.Name.Should().Be("login");
        command.Description.Should().Be("Authenticate as a user or service principal");
    }

    [Fact]
    public void AuthLoginCommand_ShouldHaveOptionalUsernameAndPasswordOptions()
    {
        // Arrange & Act
        var command = new AuthLoginCommand();

        // Assert
        var usernameOption = command.Options.FirstOrDefault(o => o.Name == "username");
        usernameOption.Should().NotBeNull();
        usernameOption!.IsRequired.Should().BeFalse();

        var passwordOption = command.Options.FirstOrDefault(o => o.Name == "password");
        passwordOption.Should().NotBeNull();
        passwordOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void AuthLoginCommand_ShouldHaveOptionalClientIdAndSecretOptions()
    {
        // Arrange & Act
        var command = new AuthLoginCommand();

        // Assert
        var clientIdOption = command.Options.FirstOrDefault(o => o.Name == "client-id");
        clientIdOption.Should().NotBeNull();
        clientIdOption!.IsRequired.Should().BeFalse();

        var clientSecretOption = command.Options.FirstOrDefault(o => o.Name == "client-secret");
        clientSecretOption.Should().NotBeNull();
        clientSecretOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void AuthLoginCommand_ShouldHaveOptionalInteractiveOption()
    {
        // Arrange & Act
        var command = new AuthLoginCommand();

        // Assert
        var interactiveOption = command.Options.FirstOrDefault(o => o.Name == "interactive");
        interactiveOption.Should().NotBeNull();
        interactiveOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void AuthLogoutCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new AuthLogoutCommand();

        // Assert
        command.Name.Should().Be("logout");
        command.Description.Should().Be("Clear cached authentication tokens");
    }

    [Fact]
    public void AuthLogoutCommand_ShouldHaveOptionalAllOption()
    {
        // Arrange & Act
        var command = new AuthLogoutCommand();

        // Assert
        var allOption = command.Options.FirstOrDefault(o => o.Name == "all");
        allOption.Should().NotBeNull();
        allOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void AuthStatusCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = new AuthStatusCommand();

        // Assert
        command.Name.Should().Be("status");
        command.Description.Should().Be("Check authentication status for the current profile");
    }

    [Fact]
    public async Task AuthLoginCommand_ShouldExecuteSuccessfully_WithNoOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLoginCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("login");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthLoginCommand_ShouldExecuteSuccessfully_WithInteractiveOption()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLoginCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("login --interactive");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthLoginCommand_ShouldExecuteSuccessfully_WithUsernameAndPassword()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLoginCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("login --username testuser --password testpass");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthLoginCommand_ShouldExecuteSuccessfully_WithClientCredentials()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLoginCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("login --client-id test-client --client-secret test-secret");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthLogoutCommand_ShouldExecuteSuccessfully_WithNoOptions()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLogoutCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("logout");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthLogoutCommand_ShouldExecuteSuccessfully_WithAllOption()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthLogoutCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("logout --all");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task AuthStatusCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new AuthStatusCommand());

        // Act
        var exitCode = await rootCommand.InvokeAsync("status");

        // Assert
        exitCode.Should().Be(0);
    }
}
