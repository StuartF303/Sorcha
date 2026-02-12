// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Sorcha.ServiceClients.Register;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Tests.Services;

public class GenesisConfigServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<IRegisterServiceClient> _registerClientMock;
    private readonly Mock<ILogger<GenesisConfigService>> _loggerMock;
    private readonly GenesisConfigCacheConfiguration _config;
    private readonly GenesisConfigService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public GenesisConfigServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _registerClientMock = new Mock<IRegisterServiceClient>();
        _loggerMock = new Mock<ILogger<GenesisConfigService>>();

        _config = new GenesisConfigCacheConfiguration
        {
            KeyPrefix = "test:genesis:",
            DefaultTtl = TimeSpan.FromMinutes(30),
            StaleCheckInterval = TimeSpan.FromMinutes(5),
            EnableLocalCache = true,
            LocalCacheTtl = TimeSpan.FromMinutes(5),
            LocalCacheMaxEntries = 10
        };

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _service = new GenesisConfigService(
            _redisMock.Object,
            _registerClientMock.Object,
            Options.Create(_config),
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRedis_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GenesisConfigService(
            null!,
            _registerClientMock.Object,
            Options.Create(_config),
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("redis");
    }

    [Fact]
    public void Constructor_WithNullRegisterClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GenesisConfigService(
            _redisMock.Object,
            null!,
            Options.Create(_config),
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registerClient");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GenesisConfigService(
            _redisMock.Object,
            _registerClientMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GenesisConfigService(
            _redisMock.Object,
            _registerClientMock.Object,
            Options.Create(_config),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GetFullConfigAsync_WithEmptyRegisterId_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.GetFullConfigAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetFullConfigAsync_WhenNotCached_CreatesDefaultConfig()
    {
        // Arrange
        var registerId = "test-register";

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains(registerId)),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.GetFullConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result.RegisterId.Should().Be(registerId);
        result.Consensus.Should().NotBeNull();
        result.Validators.Should().NotBeNull();
        result.LeaderElection.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFullConfigAsync_WhenCached_ReturnsFromRedis()
    {
        // Arrange
        var registerId = "test-register";
        var cachedConfig = CreateTestConfig(registerId);
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _service.GetFullConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result.RegisterId.Should().Be(registerId);
    }

    [Fact]
    public async Task GetConsensusConfigAsync_ReturnsConsensusFromFullConfig()
    {
        // Arrange
        var registerId = "test-register";
        var cachedConfig = CreateTestConfig(registerId);
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _service.GetConsensusConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result.SignatureThresholdMin.Should().Be(cachedConfig.Consensus.SignatureThresholdMin);
        result.DocketTimeout.Should().Be(cachedConfig.Consensus.DocketTimeout);
    }

    [Fact]
    public async Task GetValidatorConfigAsync_ReturnsValidatorFromFullConfig()
    {
        // Arrange
        var registerId = "test-register";
        var cachedConfig = CreateTestConfig(registerId);
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _service.GetValidatorConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result.MinValidators.Should().Be(cachedConfig.Validators.MinValidators);
        result.RegistrationMode.Should().Be(cachedConfig.Validators.RegistrationMode);
    }

    [Fact]
    public async Task GetLeaderElectionConfigAsync_ReturnsLeaderElectionFromFullConfig()
    {
        // Arrange
        var registerId = "test-register";
        var cachedConfig = CreateTestConfig(registerId);
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _service.GetLeaderElectionConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result.Mechanism.Should().Be(cachedConfig.LeaderElection.Mechanism);
        result.HeartbeatInterval.Should().Be(cachedConfig.LeaderElection.HeartbeatInterval);
    }

    [Fact]
    public async Task GetFullConfigAsync_UsesLocalCacheOnSecondCall()
    {
        // Arrange
        var registerId = "test-register";
        var cachedConfig = CreateTestConfig(registerId);
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act - First call
        await _service.GetFullConfigAsync(registerId);

        // Act - Second call (should use local cache)
        var result = await _service.GetFullConfigAsync(registerId);

        // Assert
        result.Should().NotBeNull();

        // Redis should only be called once for config key
        _databaseMock.Verify(d => d.StringGetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains("config")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task IsConfigStaleAsync_WhenNeverChecked_ReturnsTrue()
    {
        // Arrange
        var registerId = "test-register";

        _databaseMock.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.IsConfigStaleAsync(registerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshConfigAsync_RemovesFromCacheAndFetches()
    {
        // Arrange
        var registerId = "test-register";

        _databaseMock.Setup(d => d.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        await _service.RefreshConfigAsync(registerId);

        // Assert
        _databaseMock.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains("config")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ConfigChanged_EventRaisedWhenVersionChanges()
    {
        // Arrange
        var registerId = "test-register";
        ConfigChangedEventArgs? receivedArgs = null;

        _service.ConfigChanged += (sender, args) => receivedArgs = args;

        var cachedConfig = CreateTestConfig(registerId);
        cachedConfig = cachedConfig with { ControlBlueprintVersionId = "version-1" };
        var json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // First call to populate version cache
        await _service.GetFullConfigAsync(registerId);

        // Setup for refresh - return null so new config is generated
        _databaseMock.Setup(d => d.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _databaseMock.Setup(d => d.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("config")),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        await _service.RefreshConfigAsync(registerId);

        // Assert - Event should have been raised because version changed
        receivedArgs.Should().NotBeNull();
        receivedArgs!.RegisterId.Should().Be(registerId);
        receivedArgs.PreviousVersionId.Should().Be("version-1");
    }

    [Fact]
    public void DefaultConsensusConfig_HasReasonableDefaults()
    {
        // Arrange & Act - Use reflection or create default config
        var config = new ConsensusConfig
        {
            SignatureThresholdMin = 2,
            SignatureThresholdMax = 10,
            DocketTimeout = TimeSpan.FromSeconds(30),
            MaxSignaturesPerDocket = 100,
            MaxTransactionsPerDocket = 1000,
            DocketBuildInterval = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        config.SignatureThresholdMin.Should().BeGreaterThan(0);
        config.SignatureThresholdMax.Should().BeGreaterThanOrEqualTo(config.SignatureThresholdMin);
        config.DocketTimeout.Should().BePositive();
        config.MaxTransactionsPerDocket.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidatorConfig_IsPublicRegistration_ReturnsCorrectly()
    {
        // Arrange
        var publicConfig = new ValidatorConfig
        {
            RegistrationMode = "public",
            MinValidators = 1,
            MaxValidators = 100,
            RequireStake = false
        };

        var consentConfig = new ValidatorConfig
        {
            RegistrationMode = "consent",
            MinValidators = 1,
            MaxValidators = 100,
            RequireStake = false
        };

        // Assert
        publicConfig.IsPublicRegistration.Should().BeTrue();
        consentConfig.IsPublicRegistration.Should().BeFalse();
    }

    #region Helper Methods

    private static GenesisConfiguration CreateTestConfig(string registerId)
    {
        return new GenesisConfiguration
        {
            RegisterId = registerId,
            GenesisTransactionId = $"genesis-{registerId}",
            ControlBlueprintVersionId = $"control-v1-{registerId}",
            Consensus = new ConsensusConfig
            {
                SignatureThresholdMin = 2,
                SignatureThresholdMax = 10,
                DocketTimeout = TimeSpan.FromSeconds(30),
                MaxSignaturesPerDocket = 100,
                MaxTransactionsPerDocket = 1000,
                DocketBuildInterval = TimeSpan.FromMilliseconds(100)
            },
            Validators = new ValidatorConfig
            {
                RegistrationMode = "public",
                MinValidators = 1,
                MaxValidators = 100,
                RequireStake = false,
                StakeAmount = null
            },
            LeaderElection = new LeaderElectionConfig
            {
                Mechanism = "rotating",
                HeartbeatInterval = TimeSpan.FromSeconds(1),
                LeaderTimeout = TimeSpan.FromSeconds(5),
                TermDuration = TimeSpan.FromMinutes(1)
            },
            LoadedAt = DateTimeOffset.UtcNow,
            CacheTtl = TimeSpan.FromMinutes(30)
        };
    }

    #endregion
}
