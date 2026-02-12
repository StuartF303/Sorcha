// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using ValidatorStatus = Sorcha.Validator.Service.Services.Interfaces.ValidatorStatus;

namespace Sorcha.Validator.Service.Tests.Services;

/// <summary>
/// Unit tests for ControlDocketProcessor (VAL-9.41)
/// Tests cover control transaction extraction, validation, and processing.
/// </summary>
public class ControlDocketProcessorTests
{
    private readonly Mock<IGenesisConfigService> _mockGenesisConfigService;
    private readonly Mock<IControlBlueprintVersionResolver> _mockVersionResolver;
    private readonly Mock<IValidatorRegistry> _mockValidatorRegistry;
    private readonly Mock<ILogger<ControlDocketProcessor>> _mockLogger;
    private readonly ControlDocketProcessor _processor;

    private const string TestRegisterId = "test-register-001";
    private const string TestValidatorId = "validator-001";

    public ControlDocketProcessorTests()
    {
        _mockGenesisConfigService = new Mock<IGenesisConfigService>();
        _mockVersionResolver = new Mock<IControlBlueprintVersionResolver>();
        _mockValidatorRegistry = new Mock<IValidatorRegistry>();
        _mockLogger = new Mock<ILogger<ControlDocketProcessor>>();

        _processor = new ControlDocketProcessor(
            _mockGenesisConfigService.Object,
            _mockVersionResolver.Object,
            _mockValidatorRegistry.Object,
            _mockLogger.Object);

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var defaultConfig = CreateDefaultGenesisConfiguration();
        _mockGenesisConfigService
            .Setup(s => s.GetFullConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultConfig);

        _mockValidatorRegistry
            .Setup(r => r.GetActiveCountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
    }

    private static GenesisConfiguration CreateDefaultGenesisConfiguration()
    {
        return new GenesisConfiguration
        {
            RegisterId = TestRegisterId,
            GenesisTransactionId = "genesis-tx-001",
            ControlBlueprintVersionId = "control-v1",
            Consensus = new ConsensusConfig
            {
                SignatureThresholdMin = 2,
                SignatureThresholdMax = 10,
                DocketTimeout = TimeSpan.FromSeconds(30),
                MaxSignaturesPerDocket = 10,
                MaxTransactionsPerDocket = 100,
                DocketBuildInterval = TimeSpan.FromSeconds(10)
            },
            Validators = new ValidatorConfig
            {
                RegistrationMode = "public",
                MinValidators = 2,
                MaxValidators = 10,
                RequireStake = false
            },
            LeaderElection = new LeaderElectionConfig
            {
                Mechanism = "rotating",
                HeartbeatInterval = TimeSpan.FromSeconds(5),
                LeaderTimeout = TimeSpan.FromSeconds(15)
            },
            LoadedAt = DateTimeOffset.UtcNow,
            CacheTtl = TimeSpan.FromMinutes(30)
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullGenesisConfigService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ControlDocketProcessor(
            null!,
            _mockVersionResolver.Object,
            _mockValidatorRegistry.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullVersionResolver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ControlDocketProcessor(
            _mockGenesisConfigService.Object,
            null!,
            _mockValidatorRegistry.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullValidatorRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ControlDocketProcessor(
            _mockGenesisConfigService.Object,
            _mockVersionResolver.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ControlDocketProcessor(
            _mockGenesisConfigService.Object,
            _mockVersionResolver.Object,
            _mockValidatorRegistry.Object,
            null!));
    }

    #endregion

    #region ExtractControlTransactions Tests

    [Fact]
    public void ExtractControlTransactions_WithNullDocket_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.ExtractControlTransactions(null!));
    }

    [Fact]
    public void ExtractControlTransactions_WithEmptyDocket_ReturnsEmptyList()
    {
        // Arrange
        var docket = CreateDocket([]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractControlTransactions_WithNoControlTransactions_ReturnsEmptyList()
    {
        // Arrange
        var regularTx = CreateTransaction("tx-001", "regular.action", new { data = "test" });
        var docket = CreateDocket([regularTx]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractControlTransactions_WithValidatorRegisterAction_ExtractsTransaction()
    {
        // Arrange
        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var controlTx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([controlTx]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().HaveCount(1);
        result[0].ActionType.Should().Be(ControlActionType.ValidatorRegister);
        result[0].ActionId.Should().Be("control.validator.register");
    }

    [Fact]
    public void ExtractControlTransactions_WithConfigUpdateAction_ExtractsTransaction()
    {
        // Arrange
        var payload = new
        {
            path = "consensus.signatureThreshold.min",
            newValue = 3,
            reason = "Increase minimum signatures"
        };
        var controlTx = CreateTransaction("tx-001", "control.config.update", payload);
        var docket = CreateDocket([controlTx]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().HaveCount(1);
        result[0].ActionType.Should().Be(ControlActionType.ConfigUpdate);
    }

    [Fact]
    public void ExtractControlTransactions_WithMixedTransactions_ExtractsOnlyControlTransactions()
    {
        // Arrange
        var regularTx = CreateTransaction("tx-001", "workflow.submit", new { data = "test" });
        var controlTx1 = CreateTransaction("tx-002", "control.validator.register", new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        });
        var controlTx2 = CreateTransaction("tx-003", "control.config.update", new
        {
            path = "consensus.docketTimeout",
            newValue = "PT60S"
        });
        var docket = CreateDocket([regularTx, controlTx1, controlTx2]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Transaction.TransactionId).Should().Contain(["tx-002", "tx-003"]);
    }

    [Theory]
    [InlineData("control.validator.register", ControlActionType.ValidatorRegister)]
    [InlineData("control.validator.approve", ControlActionType.ValidatorApprove)]
    [InlineData("control.validator.suspend", ControlActionType.ValidatorSuspend)]
    [InlineData("control.validator.remove", ControlActionType.ValidatorRemove)]
    [InlineData("control.config.update", ControlActionType.ConfigUpdate)]
    [InlineData("control.blueprint.publish", ControlActionType.BlueprintPublish)]
    [InlineData("control.register.updateMetadata", ControlActionType.RegisterUpdateMetadata)]
    public void ExtractControlTransactions_WithActionId_ReturnsCorrectActionType(
        string actionId,
        ControlActionType expectedType)
    {
        // Arrange
        var payload = CreateValidPayloadForActionType(expectedType);
        var controlTx = CreateTransaction("tx-001", actionId, payload);
        var docket = CreateDocket([controlTx]);

        // Act
        var result = _processor.ExtractControlTransactions(docket);

        // Assert
        result.Should().HaveCount(1);
        result[0].ActionType.Should().Be(expectedType);
    }

    #endregion

    #region IsControlDocket Tests

    [Fact]
    public void IsControlDocket_WithNullDocket_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.IsControlDocket(null!));
    }

    [Fact]
    public void IsControlDocket_WithNoControlTransactions_ReturnsFalse()
    {
        // Arrange
        var regularTx = CreateTransaction("tx-001", "workflow.submit", new { data = "test" });
        var docket = CreateDocket([regularTx]);

        // Act
        var result = _processor.IsControlDocket(docket);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsControlDocket_WithControlTransactions_ReturnsTrue()
    {
        // Arrange
        var controlTx = CreateTransaction("tx-001", "control.config.update", new
        {
            path = "consensus.docketTimeout",
            newValue = "PT60S"
        });
        var docket = CreateDocket([controlTx]);

        // Act
        var result = _processor.IsControlDocket(docket);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsControlDocket_WithEmptyDocket_ReturnsFalse()
    {
        // Arrange
        var docket = CreateDocket([]);

        // Act
        var result = _processor.IsControlDocket(docket);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateControlTransactionsAsync Tests

    [Fact]
    public async Task ValidateControlTransactionsAsync_WithEmptyList_ReturnsSuccess()
    {
        // Arrange
        var controlTransactions = new List<ControlTransaction>();

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_WithValidValidatorRegistration_ReturnsSuccess()
    {
        // Arrange
        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidTransactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_WithInvalidEndpoint_ReturnsError()
    {
        // Arrange
        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "not-a-valid-uri"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("tx-001");
        result.Errors["tx-001"].Should().Contain(e => e.Contains("valid URI"));
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_WithMissingValidatorId_ReturnsError()
    {
        // Arrange
        var payload = new
        {
            validatorId = "",
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors["tx-001"].Should().Contain(e => e.Contains("ValidatorId"));
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_ValidatorApprovalForNonexistentValidator_ReturnsError()
    {
        // Arrange
        _mockValidatorRegistry
            .Setup(r => r.GetValidatorAsync(TestRegisterId, TestValidatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidatorInfo?)null);

        var payload = new
        {
            validatorId = TestValidatorId,
            approvedBy = "admin-001"
        };
        var tx = CreateTransaction("tx-001", "control.validator.approve", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors["tx-001"].Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_ValidatorRemovalBelowMinimum_ReturnsError()
    {
        // Arrange
        var validatorInfo = new ValidatorInfo
        {
            ValidatorId = TestValidatorId,
            PublicKey = "pubkey-001",
            GrpcEndpoint = "https://validator1.example.com",
            Status = ValidatorStatus.Active,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        _mockValidatorRegistry
            .Setup(r => r.GetValidatorAsync(TestRegisterId, TestValidatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validatorInfo);
        _mockValidatorRegistry
            .Setup(r => r.GetActiveCountAsync(TestRegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Equals minimum (2)

        var payload = new
        {
            validatorId = TestValidatorId,
            removedBy = "admin-001",
            reason = "No longer needed"
        };
        var tx = CreateTransaction("tx-001", "control.validator.remove", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors["tx-001"].Should().Contain(e => e.Contains("below minimum"));
    }

    [Fact]
    public async Task ValidateControlTransactionsAsync_ConfigUpdateWithInvalidPath_ReturnsError()
    {
        // Arrange
        var payload = new
        {
            path = "invalid.config.path",
            newValue = "something",
            reason = "Testing"
        };
        var tx = CreateTransaction("tx-001", "control.config.update", payload);
        var docket = CreateDocket([tx]);
        var controlTransactions = _processor.ExtractControlTransactions(docket);

        // Act
        var result = await _processor.ValidateControlTransactionsAsync(
            TestRegisterId, controlTransactions, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors["tx-001"].Should().Contain(e => e.Contains("Unknown configuration path"));
    }

    #endregion

    #region ProcessCommittedDocketAsync Tests

    [Fact]
    public async Task ProcessCommittedDocketAsync_WithNoControlTransactions_ReturnsSuccessWithZeroActions()
    {
        // Arrange
        var regularTx = CreateTransaction("tx-001", "workflow.submit", new { data = "test" });
        var docket = CreateDocket([regularTx]);

        // Act
        var result = await _processor.ProcessCommittedDocketAsync(TestRegisterId, docket);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsApplied.Should().Be(0);
        result.ConfigurationUpdated.Should().BeFalse();
        result.ValidatorsModified.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessCommittedDocketAsync_WithValidatorRegistration_RefreshesValidatorRegistry()
    {
        // Arrange
        _mockValidatorRegistry
            .Setup(r => r.RegisterAsync(
                TestRegisterId,
                It.IsAny<ValidatorRegistration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidatorRegistrationResult.Succeeded("tx-001", 0));

        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);

        // Act
        var result = await _processor.ProcessCommittedDocketAsync(TestRegisterId, docket);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsApplied.Should().Be(1);
        result.ValidatorsModified.Should().BeTrue();
        _mockValidatorRegistry.Verify(
            r => r.RefreshAsync(TestRegisterId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommittedDocketAsync_WithConfigUpdate_RefreshesGenesisConfig()
    {
        // Arrange
        var payload = new
        {
            path = "consensus.signatureThreshold.min",
            newValue = 3,
            reason = "Increase minimum"
        };
        var tx = CreateTransaction("tx-001", "control.config.update", payload);
        var docket = CreateDocket([tx]);

        // Act
        var result = await _processor.ProcessCommittedDocketAsync(TestRegisterId, docket);

        // Assert
        result.Success.Should().BeTrue();
        result.ConfigurationUpdated.Should().BeTrue();
        _mockGenesisConfigService.Verify(
            s => s.RefreshConfigAsync(TestRegisterId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockVersionResolver.Verify(
            v => v.InvalidateCache(TestRegisterId),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommittedDocketAsync_RaisesControlActionAppliedEvent()
    {
        // Arrange
        ControlActionAppliedEventArgs? capturedArgs = null;
        _processor.ControlActionApplied += (sender, args) => capturedArgs = args;

        _mockValidatorRegistry
            .Setup(r => r.RegisterAsync(
                TestRegisterId,
                It.IsAny<ValidatorRegistration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidatorRegistrationResult.Succeeded("tx-001", 0));

        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);

        // Act
        await _processor.ProcessCommittedDocketAsync(TestRegisterId, docket);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.RegisterId.Should().Be(TestRegisterId);
        capturedArgs.TransactionId.Should().Be("tx-001");
        capturedArgs.ActionType.Should().Be(ControlActionType.ValidatorRegister);
    }

    #endregion

    #region ApplyControlActionAsync Tests

    [Fact]
    public async Task ApplyControlActionAsync_ValidatorRegister_CallsValidatorRegistry()
    {
        // Arrange
        _mockValidatorRegistry
            .Setup(r => r.RegisterAsync(
                TestRegisterId,
                It.IsAny<ValidatorRegistration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidatorRegistrationResult.Succeeded("tx-001", 5));

        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);
        var controlTx = _processor.ExtractControlTransactions(docket)[0];

        // Act
        var result = await _processor.ApplyControlActionAsync(TestRegisterId, controlTx);

        // Assert
        result.Success.Should().BeTrue();
        result.ChangeDescription.Should().Contain("registered");
        result.ChangeDescription.Should().Contain("order: 5");
    }

    [Fact]
    public async Task ApplyControlActionAsync_ValidatorRegisterFails_ReturnsFailureResult()
    {
        // Arrange
        _mockValidatorRegistry
            .Setup(r => r.RegisterAsync(
                TestRegisterId,
                It.IsAny<ValidatorRegistration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidatorRegistrationResult.Failed("Max validators reached"));

        var payload = new
        {
            validatorId = TestValidatorId,
            publicKey = "pubkey-001",
            endpoint = "https://validator1.example.com"
        };
        var tx = CreateTransaction("tx-001", "control.validator.register", payload);
        var docket = CreateDocket([tx]);
        var controlTx = _processor.ExtractControlTransactions(docket)[0];

        // Act
        var result = await _processor.ApplyControlActionAsync(TestRegisterId, controlTx);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Max validators reached");
    }

    [Fact]
    public async Task ApplyControlActionAsync_BlueprintPublish_ReturnsSuccessWithDescription()
    {
        // Arrange
        var payload = new
        {
            blueprintId = "bp-001",
            blueprintJson = "{\"title\":\"Test Blueprint\"}",
            publishedBy = "admin-001"
        };
        var tx = CreateTransaction("tx-001", "control.blueprint.publish", payload);
        var docket = CreateDocket([tx]);
        var controlTx = _processor.ExtractControlTransactions(docket)[0];

        // Act
        var result = await _processor.ApplyControlActionAsync(TestRegisterId, controlTx);

        // Assert
        result.Success.Should().BeTrue();
        result.ChangeDescription.Should().Contain("Blueprint bp-001 published");
    }

    [Fact]
    public async Task ApplyControlActionAsync_MetadataUpdate_ReturnsSuccessWithDescription()
    {
        // Arrange
        var payload = new
        {
            field = "description",
            newValue = "Updated register description"
        };
        var tx = CreateTransaction("tx-001", "control.register.updateMetadata", payload);
        var docket = CreateDocket([tx]);
        var controlTx = _processor.ExtractControlTransactions(docket)[0];

        // Act
        var result = await _processor.ApplyControlActionAsync(TestRegisterId, controlTx);

        // Assert
        result.Success.Should().BeTrue();
        result.ChangeDescription.Should().Contain("description updated");
    }

    #endregion

    #region Helper Methods

    private static Transaction CreateTransaction(string txId, string actionId, object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadElement = JsonDocument.Parse(payloadJson).RootElement.Clone();

        return new Transaction
        {
            TransactionId = txId,
            RegisterId = TestRegisterId,
            BlueprintId = "control-blueprint",
            ActionId = actionId,
            Payload = payloadElement,
            PayloadHash = $"hash-{txId}",
            CreatedAt = DateTimeOffset.UtcNow,
            Signatures =
            [
                new Signature
                {
                    PublicKey = System.Text.Encoding.UTF8.GetBytes("test-pubkey"),
                    SignatureValue = System.Text.Encoding.UTF8.GetBytes("test-signature"),
                    Algorithm = "ED25519",
                    SignedAt = DateTimeOffset.UtcNow
                }
            ]
        };
    }

    private static Docket CreateDocket(List<Transaction> transactions)
    {
        return new Docket
        {
            DocketId = $"docket-{Guid.NewGuid():N}",
            RegisterId = TestRegisterId,
            DocketNumber = 1,
            DocketHash = "test-docket-hash",
            CreatedAt = DateTimeOffset.UtcNow,
            Transactions = transactions,
            ProposerValidatorId = "validator-001",
            ProposerSignature = new Signature
            {
                PublicKey = System.Text.Encoding.UTF8.GetBytes("proposer-pubkey"),
                SignatureValue = System.Text.Encoding.UTF8.GetBytes("proposer-signature"),
                Algorithm = "ED25519",
                SignedAt = DateTimeOffset.UtcNow
            },
            MerkleRoot = "test-merkle-root"
        };
    }

    private static object CreateValidPayloadForActionType(ControlActionType actionType)
    {
        return actionType switch
        {
            ControlActionType.ValidatorRegister => new
            {
                validatorId = TestValidatorId,
                publicKey = "pubkey-001",
                endpoint = "https://validator1.example.com"
            },
            ControlActionType.ValidatorApprove => new
            {
                validatorId = TestValidatorId,
                approvedBy = "admin-001"
            },
            ControlActionType.ValidatorSuspend => new
            {
                validatorId = TestValidatorId,
                suspendedBy = "admin-001",
                reason = "Maintenance"
            },
            ControlActionType.ValidatorRemove => new
            {
                validatorId = TestValidatorId,
                removedBy = "admin-001",
                reason = "Decommissioned"
            },
            ControlActionType.ConfigUpdate => new
            {
                path = "consensus.signatureThreshold.min",
                newValue = 3,
                reason = "Increase threshold"
            },
            ControlActionType.BlueprintPublish => new
            {
                blueprintId = "bp-001",
                blueprintJson = "{\"title\":\"Test\"}",
                publishedBy = "admin-001"
            },
            ControlActionType.RegisterUpdateMetadata => new
            {
                field = "name",
                newValue = "Updated Name"
            },
            _ => new { }
        };
    }

    #endregion
}
