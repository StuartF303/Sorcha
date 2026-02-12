// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using Xunit;

namespace Sorcha.Validator.Service.Tests.Services;

public class RotatingLeaderElectionServiceTests : IDisposable
{
    private readonly Mock<IValidatorRegistry> _validatorRegistryMock;
    private readonly Mock<IGenesisConfigService> _genesisConfigServiceMock;
    private readonly Mock<ILogger<RotatingLeaderElectionService>> _loggerMock;
    private readonly ValidatorConfiguration _validatorConfig;
    private readonly LeaderElectionConfiguration _electionConfig;
    private readonly RotatingLeaderElectionService _service;

    private const string TestRegisterId = "test-register";
    private const string ValidatorId1 = "validator-1";
    private const string ValidatorId2 = "validator-2";
    private const string ValidatorId3 = "validator-3";

    public RotatingLeaderElectionServiceTests()
    {
        _validatorConfig = new ValidatorConfiguration
        {
            ValidatorId = ValidatorId1,
            SystemWalletAddress = "test-wallet"
        };

        _electionConfig = new LeaderElectionConfiguration
        {
            DefaultHeartbeatInterval = TimeSpan.FromMilliseconds(100),
            DefaultLeaderTimeout = TimeSpan.FromMilliseconds(300),
            DefaultTermDuration = TimeSpan.FromSeconds(10),
            StartupGracePeriod = TimeSpan.FromMilliseconds(50),
            MissedHeartbeatsThreshold = 3,
            HeartbeatJitterMs = 10
        };

        _validatorRegistryMock = new Mock<IValidatorRegistry>();
        _genesisConfigServiceMock = new Mock<IGenesisConfigService>();
        _loggerMock = new Mock<ILogger<RotatingLeaderElectionService>>();

        SetupDefaultMocks();

        _service = new RotatingLeaderElectionService(
            Options.Create(_validatorConfig),
            Options.Create(_electionConfig),
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupDefaultMocks()
    {
        // Default validator order
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { ValidatorId1, ValidatorId2, ValidatorId3 });

        // Default genesis config
        _genesisConfigServiceMock
            .Setup(g => g.GetLeaderElectionConfigAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaderElectionConfig
            {
                Mechanism = "rotating",
                HeartbeatInterval = TimeSpan.FromMilliseconds(100),
                LeaderTimeout = TimeSpan.FromMilliseconds(300),
                TermDuration = TimeSpan.FromSeconds(10)
            });
    }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullValidatorConfig_ThrowsArgumentNullException()
    {
        var act = () => new RotatingLeaderElectionService(
            null!,
            Options.Create(_electionConfig),
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validatorConfig");
    }

    [Fact]
    public void Constructor_WithNullElectionConfig_ThrowsArgumentNullException()
    {
        var act = () => new RotatingLeaderElectionService(
            Options.Create(_validatorConfig),
            null!,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("electionConfig");
    }

    [Fact]
    public void Constructor_WithNullValidatorRegistry_ThrowsArgumentNullException()
    {
        var act = () => new RotatingLeaderElectionService(
            Options.Create(_validatorConfig),
            Options.Create(_electionConfig),
            null!,
            _genesisConfigServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validatorRegistry");
    }

    [Fact]
    public void Constructor_WithNullGenesisConfigService_ThrowsArgumentNullException()
    {
        var act = () => new RotatingLeaderElectionService(
            Options.Create(_validatorConfig),
            Options.Create(_electionConfig),
            _validatorRegistryMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("genesisConfigService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RotatingLeaderElectionService(
            Options.Create(_validatorConfig),
            Options.Create(_electionConfig),
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void InitialState_HasNoLeader()
    {
        _service.CurrentLeaderId.Should().BeNull();
        _service.IsLeader.Should().BeFalse();
        _service.CurrentTerm.Should().Be(0);
        _service.LastHeartbeatReceived.Should().BeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        var act = async () => await _service.StartAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartAsync_WithEmptyRegisterId_ThrowsArgumentException()
    {
        var act = async () => await _service.StartAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartAsync_LoadsConfiguration()
    {
        await _service.StartAsync(TestRegisterId);

        _genesisConfigServiceMock.Verify(
            g => g.GetLeaderElectionConfigAsync(TestRegisterId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_OnlyStartsOnce()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.StartAsync(TestRegisterId);

        // Should only load config once
        _genesisConfigServiceMock.Verify(
            g => g.GetLeaderElectionConfigAsync(TestRegisterId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TriggerElectionAsync Tests

    [Fact]
    public async Task TriggerElectionAsync_IncrementsTermNumber()
    {
        await _service.StartAsync(TestRegisterId);
        var initialTerm = _service.CurrentTerm;

        await _service.TriggerElectionAsync();

        _service.CurrentTerm.Should().Be(initialTerm + 1);
    }

    [Fact]
    public async Task TriggerElectionAsync_SelectsLeaderBasedOnTerm()
    {
        await _service.StartAsync(TestRegisterId);

        // Term 1 should select validator at index 1 % 3 = 1
        await _service.TriggerElectionAsync();
        _service.CurrentTerm.Should().Be(1);
        _service.CurrentLeaderId.Should().Be(ValidatorId2);

        // Term 2 should select validator at index 2 % 3 = 2
        await _service.TriggerElectionAsync();
        _service.CurrentTerm.Should().Be(2);
        _service.CurrentLeaderId.Should().Be(ValidatorId3);

        // Term 3 should select validator at index 3 % 3 = 0
        await _service.TriggerElectionAsync();
        _service.CurrentTerm.Should().Be(3);
        _service.CurrentLeaderId.Should().Be(ValidatorId1);
    }

    [Fact]
    public async Task TriggerElectionAsync_RaisesLeaderChangedEvent()
    {
        await _service.StartAsync(TestRegisterId);

        LeaderChangedEventArgs? receivedArgs = null;
        _service.LeaderChanged += (_, args) => receivedArgs = args;

        await _service.TriggerElectionAsync();

        receivedArgs.Should().NotBeNull();
        receivedArgs!.NewLeaderId.Should().NotBeNull();
        receivedArgs.Term.Should().Be(1);
    }

    [Fact]
    public async Task TriggerElectionAsync_WithNoValidators_DoesNotSelectLeader()
    {
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync();

        _service.CurrentLeaderId.Should().BeNull();
    }

    [Fact]
    public async Task TriggerElectionAsync_WhenThisValidatorIsLeader_SetsIsLeaderTrue()
    {
        // Arrange - Set validator order so ValidatorId1 (this validator) is selected
        // Term 3 % 3 = 0, so first validator is selected
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { ValidatorId1, ValidatorId2, ValidatorId3 });

        await _service.StartAsync(TestRegisterId);

        // Trigger election 3 times to get term 3 (3 % 3 = 0)
        await _service.TriggerElectionAsync(); // Term 1
        await _service.TriggerElectionAsync(); // Term 2
        await _service.TriggerElectionAsync(); // Term 3

        _service.CurrentLeaderId.Should().Be(ValidatorId1);
        _service.IsLeader.Should().BeTrue();
    }

    #endregion

    #region ProcessHeartbeatAsync Tests

    [Fact]
    public async Task ProcessHeartbeatAsync_WithHigherTerm_AcceptsNewLeader()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync(); // Term 1

        await _service.ProcessHeartbeatAsync(ValidatorId3, 5, 100);

        _service.CurrentTerm.Should().Be(5);
        _service.CurrentLeaderId.Should().Be(ValidatorId3);
        _service.LastHeartbeatReceived.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithSameTerm_UpdatesLastHeartbeat()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync(); // Term 1, leader = ValidatorId2

        var beforeHeartbeat = _service.LastHeartbeatReceived;
        await Task.Delay(10); // Ensure time passes

        await _service.ProcessHeartbeatAsync(ValidatorId2, 1, 100);

        _service.LastHeartbeatReceived.Should().NotBeNull();
        _service.LastHeartbeatReceived.Should().BeAfter(beforeHeartbeat ?? DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithLowerTerm_IgnoresHeartbeat()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.ProcessHeartbeatAsync(ValidatorId3, 10, 100); // Set term to 10

        var currentLeader = _service.CurrentLeaderId;
        await _service.ProcessHeartbeatAsync(ValidatorId2, 5, 100); // Lower term

        _service.CurrentTerm.Should().Be(10);
        _service.CurrentLeaderId.Should().Be(currentLeader);
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithNullLeaderId_ThrowsArgumentException()
    {
        await _service.StartAsync(TestRegisterId);

        var act = async () => await _service.ProcessHeartbeatAsync(null!, 1, 100);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_RaisesLeaderChangedEventOnNewLeader()
    {
        await _service.StartAsync(TestRegisterId);

        LeaderChangedEventArgs? receivedArgs = null;
        _service.LeaderChanged += (_, args) => receivedArgs = args;

        await _service.ProcessHeartbeatAsync(ValidatorId3, 5, 100);

        receivedArgs.Should().NotBeNull();
        receivedArgs!.NewLeaderId.Should().Be(ValidatorId3);
        receivedArgs.Reason.Should().Be(LeaderChangeReason.HigherTermReceived);
    }

    #endregion

    #region GetNextLeaderAsync Tests

    [Fact]
    public async Task GetNextLeaderAsync_WithNoCurrentLeader_ReturnsFirstValidator()
    {
        await _service.StartAsync(TestRegisterId);

        var nextLeader = await _service.GetNextLeaderAsync(null);

        nextLeader.Should().Be(ValidatorId1);
    }

    [Fact]
    public async Task GetNextLeaderAsync_WithCurrentLeader_ReturnsNextInOrder()
    {
        await _service.StartAsync(TestRegisterId);

        var nextLeader = await _service.GetNextLeaderAsync(ValidatorId1);

        nextLeader.Should().Be(ValidatorId2);
    }

    [Fact]
    public async Task GetNextLeaderAsync_WithLastValidator_WrapsToFirst()
    {
        await _service.StartAsync(TestRegisterId);

        var nextLeader = await _service.GetNextLeaderAsync(ValidatorId3);

        nextLeader.Should().Be(ValidatorId1);
    }

    [Fact]
    public async Task GetNextLeaderAsync_WithUnknownValidator_ReturnsFirstValidator()
    {
        await _service.StartAsync(TestRegisterId);

        var nextLeader = await _service.GetNextLeaderAsync("unknown-validator");

        nextLeader.Should().Be(ValidatorId1);
    }

    [Fact]
    public async Task GetNextLeaderAsync_WithNoValidators_ReturnsNull()
    {
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        await _service.StartAsync(TestRegisterId);

        var nextLeader = await _service.GetNextLeaderAsync(ValidatorId1);

        nextLeader.Should().BeNull();
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenRunning_StopsService()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync();

        await _service.StopAsync();

        // After stop, no events should be raised for new elections
        LeaderChangedEventArgs? receivedArgs = null;
        _service.LeaderChanged += (_, args) => receivedArgs = args;

        await _service.TriggerElectionAsync();

        receivedArgs.Should().BeNull();
    }

    [Fact]
    public async Task StopAsync_WhenLeader_RaisesLeaderResignedEvent()
    {
        // Set up so this validator becomes leader
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { ValidatorId1 });

        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync();

        _service.IsLeader.Should().BeTrue();

        LeaderChangedEventArgs? receivedArgs = null;
        _service.LeaderChanged += (_, args) => receivedArgs = args;

        await _service.StopAsync();

        receivedArgs.Should().NotBeNull();
        receivedArgs!.Reason.Should().Be(LeaderChangeReason.LeaderResigned);
        receivedArgs.NewLeaderId.Should().BeNull();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNothing()
    {
        // Don't start the service
        await _service.StopAsync(); // Should not throw
    }

    #endregion

    #region SendHeartbeatAsync Tests

    [Fact]
    public async Task SendHeartbeatAsync_WhenNotLeader_DoesNothing()
    {
        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync(); // ValidatorId2 is leader

        _service.IsLeader.Should().BeFalse();

        // Should not throw
        await _service.SendHeartbeatAsync();
    }

    [Fact]
    public async Task SendHeartbeatAsync_WhenLeader_SendsHeartbeat()
    {
        // Set up so this validator becomes leader
        _validatorRegistryMock
            .Setup(v => v.GetValidatorOrderAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { ValidatorId1 });

        await _service.StartAsync(TestRegisterId);
        await _service.TriggerElectionAsync();

        _service.IsLeader.Should().BeTrue();

        // Should not throw
        await _service.SendHeartbeatAsync();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _service.Dispose();
        _service.Dispose(); // Should not throw
    }

    #endregion
}
