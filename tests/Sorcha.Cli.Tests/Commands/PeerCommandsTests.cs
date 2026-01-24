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
/// Unit tests for Peer command structure and options.
/// </summary>
public class PeerCommandsTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly HttpClientFactory _clientFactory;

    public PeerCommandsTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockConfigService = new Mock<IConfigurationService>();

        // Setup default mock behavior
        _mockConfigService.Setup(x => x.GetActiveProfileAsync())
            .ReturnsAsync(new Profile { Name = "test" });
        _mockAuthService.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("test-token");

        // HttpClientFactory requires a real IConfigurationService, so we use the mock
        _clientFactory = new HttpClientFactory(_mockConfigService.Object);
    }

    private IAuthenticationService AuthService => _mockAuthService.Object;
    private IConfigurationService ConfigService => _mockConfigService.Object;

    [Fact]
    public void PeerCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("peer");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerCommand_ShouldHaveFourSubcommands()
    {
        var command = new PeerCommand(_clientFactory, AuthService, ConfigService);
        command.Subcommands.Should().HaveCount(4);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "stats", "health" });
    }

    #region PeerListCommand Tests

    [Fact]
    public void PeerListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerListCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerListCommand_ShouldParseArguments()
    {
        // This test verifies the command structure parses correctly
        // Actual execution requires integration tests with real services
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new PeerListCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("list").InvokeAsync();
        // Exit code 0 means parsing succeeded; handler errors are caught internally
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerGetCommand Tests

    [Fact]
    public void PeerGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerGetCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerGetCommand_ShouldHaveRequiredIdOption()
    {
        var command = new PeerGetCommand(_clientFactory, AuthService, ConfigService);
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.Required.Should().BeTrue();
    }

    [Fact]
    public async Task PeerGetCommand_ShouldParseArguments_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new PeerGetCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("get --id peer-123").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerStatsCommand Tests

    [Fact]
    public void PeerStatsCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerStatsCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("stats");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerStatsCommand_ShouldParseArguments()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new PeerStatsCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("stats").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerHealthCommand Tests

    [Fact]
    public void PeerHealthCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerHealthCommand(_clientFactory, AuthService, ConfigService);
        command.Name.Should().Be("health");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerHealthCommand_ShouldParseArguments()
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(new PeerHealthCommand(_clientFactory, AuthService, ConfigService));
        var exitCode = await rootCommand.Parse("health").InvokeAsync();
        exitCode.Should().Be(0);
    }

    #endregion
}
