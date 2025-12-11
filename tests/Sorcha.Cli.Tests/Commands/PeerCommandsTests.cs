using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Services;
using Xunit;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Peer command structure and options.
/// </summary>
public class PeerCommandsTests
{
    // Note: Structure tests use null dependencies since we're only testing command structure, not execution
    private readonly HttpClientFactory _clientFactory = null!;
    private readonly IAuthenticationService _authService = null!;
    private readonly IConfigurationService _configService = null!;

    [Fact]
    public void PeerCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerCommand(_clientFactory, _authService, _configService);
        command.Name.Should().Be("peer");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerCommand_ShouldHaveFourSubcommands()
    {
        var command = new PeerCommand(_clientFactory, _authService, _configService);
        command.Subcommands.Should().HaveCount(4);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "stats", "health" });
    }

    #region PeerListCommand Tests

    [Fact]
    public void PeerListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerListCommand(_clientFactory, _authService, _configService);
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerListCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerListCommand(_clientFactory, _authService, _configService));
        var exitCode = await rootCommand.InvokeAsync("list");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerGetCommand Tests

    [Fact]
    public void PeerGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerGetCommand(_clientFactory, _authService, _configService);
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerGetCommand_ShouldHaveRequiredIdOption()
    {
        var command = new PeerGetCommand(_clientFactory, _authService, _configService);
        var idOption = command.Options.FirstOrDefault(o => o.Name == "id");
        idOption.Should().NotBeNull();
        idOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task PeerGetCommand_ShouldExecuteSuccessfully_WithRequiredId()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerGetCommand(_clientFactory, _authService, _configService));
        var exitCode = await rootCommand.InvokeAsync("get --id peer-123");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerStatsCommand Tests

    [Fact]
    public void PeerStatsCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerStatsCommand(_clientFactory, _authService, _configService);
        command.Name.Should().Be("stats");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerStatsCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerStatsCommand(_clientFactory, _authService, _configService));
        var exitCode = await rootCommand.InvokeAsync("stats");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerHealthCommand Tests

    [Fact]
    public void PeerHealthCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerHealthCommand(_clientFactory, _authService, _configService);
        command.Name.Should().Be("health");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PeerHealthCommand_ShouldExecuteSuccessfully()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerHealthCommand(_clientFactory, _authService, _configService));
        var exitCode = await rootCommand.InvokeAsync("health");
        exitCode.Should().Be(0);
    }

    #endregion
}
