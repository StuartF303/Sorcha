using System.CommandLine;
using FluentAssertions;
using Moq;
using Sorcha.Cli.Commands;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;
using Xunit;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Register command structure and options.
/// </summary>
public class RegisterCommandsTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly HttpClientFactory _clientFactory;

    public RegisterCommandsTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockConfigService = new Mock<IConfigurationService>();

        // Setup default mock behavior
        _mockConfigService.Setup(x => x.GetActiveProfileAsync())
            .ReturnsAsync(new Profile { Name = "test" });
        _mockAuthService.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("test-token");

        _clientFactory = new HttpClientFactory(_mockConfigService.Object);
    }

    private IAuthenticationService AuthService => _mockAuthService.Object;
    private IConfigurationService ConfigService => _mockConfigService.Object;

    [Fact]
    public void RegisterCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("register");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterCommand_ShouldHaveFourSubcommands()
    {
        var command = new RegisterCommand(_clientFactory, AuthService, ConfigService);
        command.Subcommands.Should().HaveCount(4);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "create", "delete" });
    }

    #region RegisterListCommand Tests

    [Fact]
    public void RegisterListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterListCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterListCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterListCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("list").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterGetCommand Tests

    [Fact]
    public void RegisterGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterGetCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterGetCommand_ShouldHaveRequiredIdOption()
    {
        var command = new RegisterGetCommand(_clientFactory, AuthService, ConfigService);
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.Required.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterGetCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterGetCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("get --id test-register-123").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterCreateCommand Tests

    [Fact]
    public void RegisterCreateCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterCreateCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("create");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveRequiredNameOption()
    {
        var command = new RegisterCreateCommand(_clientFactory, AuthService, ConfigService);
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        nameOption.Should().NotBeNull();
        nameOption!.Required.Should().BeTrue();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveRequiredOrgIdOption()
    {
        var command = new RegisterCreateCommand(_clientFactory, AuthService, ConfigService);
        var orgIdOption = command.Options.FirstOrDefault(o => o.Name == "org-id");
        orgIdOption.Should().NotBeNull();
        orgIdOption!.Required.Should().BeTrue();
    }

    [Fact]
    public void RegisterCreateCommand_ShouldHaveOptionalDescriptionOption()
    {
        var command = new RegisterCreateCommand(_clientFactory, AuthService, ConfigService);
        var descOption = command.Options.FirstOrDefault(o => o.Name == "description");
        descOption.Should().NotBeNull();
        descOption!.Required.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCreateCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterCreateCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("create --name TestReg --org-id org-123 --description \"Test register\"").InvokeAsync();
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RegisterCreateCommand_ShouldExecuteSuccessfully_WithoutOptionalDescription()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterCreateCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("create --name TestReg --org-id org-123").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion

    #region RegisterDeleteCommand Tests

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new RegisterDeleteCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("delete");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveRequiredIdOption()
    {
        var command = new RegisterDeleteCommand(_clientFactory, AuthService, ConfigService);
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.Required.Should().BeTrue();
    }

    [Fact]
    public void RegisterDeleteCommand_ShouldHaveOptionalYesOption()
    {
        var command = new RegisterDeleteCommand(_clientFactory, AuthService, ConfigService);
        var yesOption = command.Options.FirstOrDefault(o => o.Name == "yes");
        yesOption.Should().NotBeNull();
        yesOption!.Required.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterDeleteCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterDeleteCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("delete --id test-register-123").InvokeAsync();
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RegisterDeleteCommand_ShouldExecuteSuccessfully_WithYesFlag()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new RegisterDeleteCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("delete --id test-register-123 --yes").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion
}
