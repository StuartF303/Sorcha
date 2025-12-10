using System.CommandLine;
using FluentAssertions;
using Sorcha.Cli.Commands;

namespace Sorcha.Cli.Tests.Commands;

/// <summary>
/// Unit tests for Peer command structure and options.
/// </summary>
public class PeerCommandsTests
{
    [Fact]
    public void PeerCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerCommand();
        command.Name.Should().Be("peer");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerCommand_ShouldHaveFiveSubcommands()
    {
        var command = new PeerCommand();
        command.Subcommands.Should().HaveCount(5);
        command.Subcommands.Select(c => c.Name).Should().Contain(new[] { "list", "get", "topology", "stats", "health" });
    }

    #region PeerListCommand Tests

    [Fact]
    public void PeerListCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerListCommand();
        command.Name.Should().Be("list");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerListCommand_ShouldHaveOptionalStatusOption()
    {
        var command = new PeerListCommand();
        var statusOption = command.Options.FirstOrDefault(o => o.Name == "status");
        statusOption.Should().NotBeNull();
        statusOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PeerListCommand_ShouldHaveOptionalSortOption()
    {
        var command = new PeerListCommand();
        var sortOption = command.Options.FirstOrDefault(o => o.Name == "sort");
        sortOption.Should().NotBeNull();
        sortOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PeerListCommand_ShouldExecuteSuccessfully_WithNoOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerListCommand());
        var exitCode = await rootCommand.InvokeAsync("list");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PeerListCommand_ShouldExecuteSuccessfully_WithStatusFilter()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerListCommand());
        var exitCode = await rootCommand.InvokeAsync("list --status connected");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerGetCommand Tests

    [Fact]
    public void PeerGetCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerGetCommand();
        command.Name.Should().Be("get");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerGetCommand_ShouldHaveRequiredPeerIdOption()
    {
        var command = new PeerGetCommand();
        var peerIdOption = command.Options.FirstOrDefault(o => o.Name == "peer-id");
        peerIdOption.Should().NotBeNull();
        peerIdOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void PeerGetCommand_ShouldHaveOptionalShowHistoryOption()
    {
        var command = new PeerGetCommand();
        var showHistoryOption = command.Options.FirstOrDefault(o => o.Name == "show-history");
        showHistoryOption.Should().NotBeNull();
        showHistoryOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PeerGetCommand_ShouldHaveOptionalShowMetricsOption()
    {
        var command = new PeerGetCommand();
        var showMetricsOption = command.Options.FirstOrDefault(o => o.Name == "show-metrics");
        showMetricsOption.Should().NotBeNull();
        showMetricsOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PeerGetCommand_ShouldExecuteSuccessfully_WithRequiredPeerId()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerGetCommand());
        var exitCode = await rootCommand.InvokeAsync("get --peer-id peer-123");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PeerGetCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerGetCommand());
        var exitCode = await rootCommand.InvokeAsync("get --peer-id peer-123 --show-history --show-metrics");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerTopologyCommand Tests

    [Fact]
    public void PeerTopologyCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerTopologyCommand();
        command.Name.Should().Be("topology");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerTopologyCommand_ShouldHaveOptionalFormatOption()
    {
        var command = new PeerTopologyCommand();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "format");
        formatOption.Should().NotBeNull();
        formatOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PeerTopologyCommand_ShouldExecuteSuccessfully_WithDefaultFormat()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerTopologyCommand());
        var exitCode = await rootCommand.InvokeAsync("topology");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PeerTopologyCommand_ShouldExecuteSuccessfully_WithTreeFormat()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerTopologyCommand());
        var exitCode = await rootCommand.InvokeAsync("topology --format tree");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerStatsCommand Tests

    [Fact]
    public void PeerStatsCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerStatsCommand();
        command.Name.Should().Be("stats");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerStatsCommand_ShouldHaveOptionalWindowOption()
    {
        var command = new PeerStatsCommand();
        var windowOption = command.Options.FirstOrDefault(o => o.Name == "window");
        windowOption.Should().NotBeNull();
        windowOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PeerStatsCommand_ShouldExecuteSuccessfully_WithDefaultWindow()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerStatsCommand());
        var exitCode = await rootCommand.InvokeAsync("stats");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PeerStatsCommand_ShouldExecuteSuccessfully_WithCustomWindow()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerStatsCommand());
        var exitCode = await rootCommand.InvokeAsync("stats --window 24h");
        exitCode.Should().Be(0);
    }

    #endregion

    #region PeerHealthCommand Tests

    [Fact]
    public void PeerHealthCommand_ShouldHaveCorrectNameAndDescription()
    {
        var command = new PeerHealthCommand();
        command.Name.Should().Be("health");
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeerHealthCommand_ShouldHaveOptionalCheckConnectivityOption()
    {
        var command = new PeerHealthCommand();
        var checkConnectivityOption = command.Options.FirstOrDefault(o => o.Name == "check-connectivity");
        checkConnectivityOption.Should().NotBeNull();
        checkConnectivityOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PeerHealthCommand_ShouldHaveOptionalCheckConsensusOption()
    {
        var command = new PeerHealthCommand();
        var checkConsensusOption = command.Options.FirstOrDefault(o => o.Name == "check-consensus");
        checkConsensusOption.Should().NotBeNull();
        checkConsensusOption!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task PeerHealthCommand_ShouldExecuteSuccessfully_WithNoOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerHealthCommand());
        var exitCode = await rootCommand.InvokeAsync("health");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task PeerHealthCommand_ShouldExecuteSuccessfully_WithAllOptions()
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(new PeerHealthCommand());
        var exitCode = await rootCommand.InvokeAsync("health --check-connectivity --check-consensus");
        exitCode.Should().Be(0);
    }

    #endregion
}
