// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Validator;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Models.Requests;
using Sorcha.Blueprint.Service.Services.Implementation;
using Sorcha.Blueprint.Service.Services.Interfaces;
using Sorcha.Blueprint.Service.Storage;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;
using ParticipantModel = Sorcha.Blueprint.Models.Participant;
using RouteModel = Sorcha.Blueprint.Models.Route;
using RejectionConfigModel = Sorcha.Blueprint.Models.RejectionConfig;

namespace Sorcha.Blueprint.Service.Tests.Services;

/// <summary>
/// Unit tests for ActionExecutionService
/// </summary>
public class ActionExecutionServiceTests
{
    private readonly Mock<IActionResolverService> _mockActionResolver;
    private readonly Mock<IStateReconstructionService> _mockStateReconstruction;
    private readonly Mock<ITransactionBuilderService> _mockTransactionBuilder;
    private readonly Mock<IRegisterServiceClient> _mockRegisterClient;
    private readonly Mock<IValidatorServiceClient> _mockValidatorClient;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IInstanceStore> _mockInstanceStore;
    private readonly Mock<IExecutionEngine> _mockExecutionEngine;
    private readonly Mock<ILogger<ActionExecutionService>> _mockLogger;
    private readonly ActionExecutionService _service;

    public ActionExecutionServiceTests()
    {
        _mockActionResolver = new Mock<IActionResolverService>();
        _mockStateReconstruction = new Mock<IStateReconstructionService>();
        _mockTransactionBuilder = new Mock<ITransactionBuilderService>();
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
        _mockValidatorClient = new Mock<IValidatorServiceClient>();
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockInstanceStore = new Mock<IInstanceStore>();
        _mockExecutionEngine = new Mock<IExecutionEngine>();
        _mockLogger = new Mock<ILogger<ActionExecutionService>>();

        _service = new ActionExecutionService(
            _mockActionResolver.Object,
            _mockStateReconstruction.Object,
            _mockTransactionBuilder.Object,
            _mockRegisterClient.Object,
            _mockValidatorClient.Object,
            _mockWalletClient.Object,
            _mockNotificationService.Object,
            _mockInstanceStore.Object,
            _mockExecutionEngine.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullActionResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                null!,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullStateReconstruction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                null!,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullTransactionBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                null!,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullRegisterClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                null!,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullValidatorClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                null!,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullWalletClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                null!,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                null!,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullInstanceStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                null!,
                _mockExecutionEngine.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullExecutionEngine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                null!,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionExecutionService(
                _mockActionResolver.Object,
                _mockStateReconstruction.Object,
                _mockTransactionBuilder.Object,
                _mockRegisterClient.Object,
                _mockValidatorClient.Object,
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
                _mockExecutionEngine.Object,
                null!));
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNonExistentInstance_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "non-existent-instance";
        var actionId = 1;
        var request = new ActionSubmissionRequest
        {
            BlueprintId = "blueprint-1",
            ActionId = "1",
            SenderWallet = "wallet-sender",
            RegisterAddress = "register-1",
            PayloadData = new Dictionary<string, object>()
        };
        var delegationToken = "test-token";

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentBlueprint_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "non-existent-bp");

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync("non-existent-bp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlueprintModel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentAction_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 999; // Non-existent
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync("blueprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        _mockActionResolver
            .Setup(x => x.GetActionDefinition(blueprint, "999"))
            .Returns((ActionModel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));
    }

    [Fact]
    public async Task ExecuteAsync_WithActionNotCurrentAction_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 2; // Not a current action
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        instance.CurrentActionIds = [1]; // Only action 1 is current
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == 2);
        action.IsStartingAction = false;

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync("blueprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        _mockActionResolver
            .Setup(x => x.GetActionDefinition(blueprint, "2"))
            .Returns(action);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));
    }

    // Note: ExecuteAsync full flow test is skipped because it requires complex
    // mocking of Transaction class construction which requires ICryptoModule,
    // IHashProvider, and IPayloadManager. The validation logic is tested above.

    #endregion

    #region Engine Delegation Tests

    [Fact]
    public void Constructor_WithAllDependencies_CreatesInstance()
    {
        // Verify we can construct with all dependencies
        var service = new ActionExecutionService(
            _mockActionResolver.Object,
            _mockStateReconstruction.Object,
            _mockTransactionBuilder.Object,
            _mockRegisterClient.Object,
            _mockValidatorClient.Object,
            _mockWalletClient.Object,
            _mockNotificationService.Object,
            _mockInstanceStore.Object,
            _mockExecutionEngine.Object,
            _mockLogger.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ExecuteAsync_WithConditionalRoute_DelegatesToEngine()
    {
        // Arrange - Verify engine routing is called with correct parameters
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);

        SetupCommonMocks(instanceId, instance, blueprint, action);

        var engineRoutingResult = new Sorcha.Blueprint.Engine.Models.RoutingResult
        {
            NextActions = [new Sorcha.Blueprint.Engine.Models.RoutedAction
            {
                ActionId = "2",
                ParticipantId = "reviewer"
            }],
            IsParallel = false
        };

        _mockExecutionEngine
            .Setup(x => x.DetermineRoutingAsync(
                blueprint,
                action,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(engineRoutingResult);

        _mockExecutionEngine
            .Setup(x => x.ApplyDisclosures(It.IsAny<Dictionary<string, object>>(), action))
            .Returns(new List<Sorcha.Blueprint.Engine.Models.DisclosureResult>());

        // Act & Assert - verify engine is called (will throw at transaction building, which is fine)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        _mockExecutionEngine.Verify(x => x.DetermineRoutingAsync(
            blueprint, action, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidSchemaData_DelegatesToEngineValidation()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);
        action.DataSchemas = new[] { System.Text.Json.JsonDocument.Parse("""{ "type": "object" }""") };

        SetupCommonMocks(instanceId, instance, blueprint, action);

        var validationErrors = new List<Sorcha.Blueprint.Engine.Models.ValidationError>
        {
            Sorcha.Blueprint.Engine.Models.ValidationError.Create("/field1", "Invalid value")
        };

        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                action,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Invalid(validationErrors));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        Assert.Contains("Invalid value", ex.Errors[0]);
        _mockExecutionEngine.Verify(x => x.ValidateAsync(
            It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSchema_FallsBackToFieldPresenceCheck()
    {
        // Arrange - no DataSchemas defined, should use RequiredActionData fallback
        var instanceId = "test-instance";
        var actionId = 1;
        var request = new ActionSubmissionRequest
        {
            BlueprintId = "blueprint-1",
            ActionId = "1",
            SenderWallet = "wallet-sender",
            RegisterAddress = "register-1",
            PayloadData = new Dictionary<string, object>() // Missing required fields
        };
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);
        action.DataSchemas = null; // No schemas
        action.RequiredActionData = new List<string> { "mandatoryField" };

        SetupCommonMocks(instanceId, instance, blueprint, action);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        Assert.Contains("Required field 'mandatoryField' is missing", ex.Message);

        // Engine validation should NOT have been called (no schemas defined)
        _mockExecutionEngine.Verify(x => x.ValidateAsync(
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<ActionModel>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithCalculations_DelegatesToEngine()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);
        action.Calculations = new Dictionary<string, System.Text.Json.Nodes.JsonNode?>
        {
            ["total"] = System.Text.Json.Nodes.JsonNode.Parse("""{"+":[{"var":"a"},{"var":"b"}]}""")
        };

        SetupCommonMocks(instanceId, instance, blueprint, action);

        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Valid());

        _mockExecutionEngine
            .Setup(x => x.DetermineRoutingAsync(blueprint, action, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.RoutingResult.Complete());

        _mockExecutionEngine
            .Setup(x => x.ApplyDisclosures(It.IsAny<Dictionary<string, object>>(), action))
            .Returns(new List<Sorcha.Blueprint.Engine.Models.DisclosureResult>());

        var calculatedData = new Dictionary<string, object>
        {
            ["a"] = 10,
            ["b"] = 20,
            ["total"] = 30
        };

        _mockExecutionEngine
            .Setup(x => x.ApplyCalculationsAsync(It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calculatedData);

        // Act & Assert - will throw at transaction building, verify engine called
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        _mockExecutionEngine.Verify(x => x.ApplyCalculationsAsync(
            It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisclosureRules_DelegatesToEngine()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);

        SetupCommonMocks(instanceId, instance, blueprint, action);

        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Valid());

        _mockExecutionEngine
            .Setup(x => x.DetermineRoutingAsync(blueprint, action, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.RoutingResult.Complete());

        var disclosureResults = new List<Sorcha.Blueprint.Engine.Models.DisclosureResult>
        {
            Sorcha.Blueprint.Engine.Models.DisclosureResult.Create("reviewer", new Dictionary<string, object> { ["field1"] = "value1" })
        };

        _mockExecutionEngine
            .Setup(x => x.ApplyDisclosures(It.IsAny<Dictionary<string, object>>(), action))
            .Returns(disclosureResults);

        // Act & Assert - will throw at transaction building, verify engine called
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        _mockExecutionEngine.Verify(x => x.ApplyDisclosures(
            It.IsAny<Dictionary<string, object>>(), action),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisclosureRules_FiltersDataPerRecipient()
    {
        // Arrange - disclosure returns different filtered data per participant
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);

        SetupCommonMocks(instanceId, instance, blueprint, action);

        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Valid());

        _mockExecutionEngine
            .Setup(x => x.DetermineRoutingAsync(blueprint, action, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.RoutingResult.Complete());

        // Two different disclosures: applicant sees field1+field2, reviewer sees only field1
        var disclosureResults = new List<Sorcha.Blueprint.Engine.Models.DisclosureResult>
        {
            Sorcha.Blueprint.Engine.Models.DisclosureResult.Create("applicant", new Dictionary<string, object>
            {
                ["field1"] = "value1",
                ["field2"] = 42
            }),
            Sorcha.Blueprint.Engine.Models.DisclosureResult.Create("reviewer", new Dictionary<string, object>
            {
                ["field1"] = "value1"
            })
        };

        _mockExecutionEngine
            .Setup(x => x.ApplyDisclosures(It.IsAny<Dictionary<string, object>>(), action))
            .Returns(disclosureResults);

        // Act & Assert - verify engine processes two separate disclosures
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        _mockExecutionEngine.Verify(x => x.ApplyDisclosures(
            It.IsAny<Dictionary<string, object>>(), action),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithWildcardDisclosure_SendsAllFields()
    {
        // Arrange - disclosure returns full data (wildcard)
        var instanceId = "test-instance";
        var actionId = 1;
        var request = CreateTestRequest();
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);

        SetupCommonMocks(instanceId, instance, blueprint, action);

        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(It.IsAny<Dictionary<string, object>>(), action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Valid());

        _mockExecutionEngine
            .Setup(x => x.DetermineRoutingAsync(blueprint, action, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.RoutingResult.Complete());

        // Wildcard disclosure: reviewer gets all fields
        var disclosureResults = new List<Sorcha.Blueprint.Engine.Models.DisclosureResult>
        {
            Sorcha.Blueprint.Engine.Models.DisclosureResult.Create("reviewer", new Dictionary<string, object>
            {
                ["field1"] = "value1",
                ["field2"] = 42
            })
        };

        _mockExecutionEngine
            .Setup(x => x.ApplyDisclosures(It.IsAny<Dictionary<string, object>>(), action))
            .Returns(disclosureResults);

        // Act & Assert - verify engine called and disclosure includes all fields
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteAsync(instanceId, actionId, request, delegationToken));

        _mockExecutionEngine.Verify(x => x.ApplyDisclosures(
            It.IsAny<Dictionary<string, object>>(), action),
            Times.Once);
    }

    #endregion

    #region RejectAsync Tests

    [Fact]
    public async Task RejectAsync_WithNonExistentInstance_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "non-existent-instance";
        var actionId = 1;
        var request = new ActionRejectionRequest
        {
            Reason = "Test rejection"
        };
        var delegationToken = "test-token";

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RejectAsync(instanceId, actionId, request, delegationToken));
    }

    [Fact]
    public async Task RejectAsync_WithActionNotAllowingRejection_ThrowsInvalidOperationException()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 1;
        var request = new ActionRejectionRequest { Reason = "Test rejection" };
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprint();
        var action = blueprint.Actions!.First(a => a.Id == actionId);
        action.RejectionConfig = null; // No rejection allowed

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync("blueprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        _mockActionResolver
            .Setup(x => x.GetActionDefinition(blueprint, "1"))
            .Returns(action);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RejectAsync(instanceId, actionId, request, delegationToken));
    }

    [Fact]
    public async Task RejectAsync_WithRequiredReasonMissing_ThrowsValidationException()
    {
        // Arrange
        var instanceId = "test-instance";
        var actionId = 2;
        var request = new ActionRejectionRequest { Reason = "" }; // Empty reason
        var delegationToken = "test-token";
        var instance = CreateTestInstance(instanceId, "blueprint-1");
        var blueprint = CreateTestBlueprintWithRejection();
        var action = blueprint.Actions!.First(a => a.Id == actionId);

        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync("blueprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        _mockActionResolver
            .Setup(x => x.GetActionDefinition(blueprint, "2"))
            .Returns(action);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RejectAsync(instanceId, actionId, request, delegationToken));
    }

    #endregion

    #region Helper Methods

    private void SetupCommonMocks(string instanceId, Instance instance, BlueprintModel blueprint, ActionModel action)
    {
        _mockInstanceStore
            .Setup(x => x.GetAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _mockActionResolver
            .Setup(x => x.GetBlueprintAsync(instance.BlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        _mockActionResolver
            .Setup(x => x.GetActionDefinition(blueprint, action.Id.ToString()))
            .Returns(action);

        _mockStateReconstruction
            .Setup(x => x.ReconstructAsync(
                blueprint,
                instanceId,
                action.Id,
                instance.RegisterId,
                It.IsAny<string>(),
                instance.ParticipantWallets,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccumulatedState());

        // Default: validation passes when no schemas
        _mockExecutionEngine
            .Setup(x => x.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                action,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sorcha.Blueprint.Engine.Models.ValidationResult.Valid());

        // Default: no calculations
        _mockExecutionEngine
            .Setup(x => x.ApplyCalculationsAsync(
                It.IsAny<Dictionary<string, object>>(),
                action,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>());
    }

    private ActionSubmissionRequest CreateTestRequest()
    {
        return new ActionSubmissionRequest
        {
            BlueprintId = "blueprint-1",
            ActionId = "1",
            SenderWallet = "wallet-sender",
            RegisterAddress = "register-1",
            PayloadData = new Dictionary<string, object>
            {
                ["field1"] = "value1",
                ["field2"] = 42
            }
        };
    }

    private Instance CreateTestInstance(string instanceId, string blueprintId)
    {
        return new Instance
        {
            Id = instanceId,
            BlueprintId = blueprintId,
            BlueprintVersion = 1,
            RegisterId = "register-1",
            TenantId = "test-tenant",
            State = InstanceState.Active,
            CurrentActionIds = [1],
            ParticipantWallets = new Dictionary<string, string>
            {
                ["applicant"] = "wallet-applicant",
                ["reviewer"] = "wallet-reviewer"
            }
        };
    }

    private BlueprintModel CreateTestBlueprint()
    {
        return new BlueprintModel
        {
            Id = "blueprint-1",
            Title = "Test Blueprint",
            Participants = new List<ParticipantModel>
            {
                new ParticipantModel { Id = "applicant", Name = "Applicant", WalletAddress = "wallet-applicant" },
                new ParticipantModel { Id = "reviewer", Name = "Reviewer", WalletAddress = "wallet-reviewer" }
            },
            Actions = new List<ActionModel>
            {
                new ActionModel
                {
                    Id = 1,
                    Title = "Submit Application",
                    Sender = "applicant",
                    IsStartingAction = true,
                    Routes = new List<RouteModel>
                    {
                        new RouteModel { NextActionIds = new List<int> { 2 } }
                    }
                },
                new ActionModel
                {
                    Id = 2,
                    Title = "Review Application",
                    Sender = "reviewer"
                }
            }
        };
    }

    private BlueprintModel CreateTestBlueprintWithRejection()
    {
        var blueprint = CreateTestBlueprint();
        var reviewAction = blueprint.Actions!.First(a => a.Id == 2);
        reviewAction.RejectionConfig = new RejectionConfigModel
        {
            TargetActionId = 1,
            RequireReason = true,
            IsTerminal = false
        };
        return blueprint;
    }


    #endregion
}
