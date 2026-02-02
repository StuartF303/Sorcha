// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
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
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IInstanceStore> _mockInstanceStore;
    private readonly Mock<ILogger<ActionExecutionService>> _mockLogger;
    private readonly ActionExecutionService _service;

    public ActionExecutionServiceTests()
    {
        _mockActionResolver = new Mock<IActionResolverService>();
        _mockStateReconstruction = new Mock<IStateReconstructionService>();
        _mockTransactionBuilder = new Mock<ITransactionBuilderService>();
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockInstanceStore = new Mock<IInstanceStore>();
        _mockLogger = new Mock<ILogger<ActionExecutionService>>();

        _service = new ActionExecutionService(
            _mockActionResolver.Object,
            _mockStateReconstruction.Object,
            _mockTransactionBuilder.Object,
            _mockRegisterClient.Object,
            _mockWalletClient.Object,
            _mockNotificationService.Object,
            _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
                null!,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                null!,
                _mockInstanceStore.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
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
                _mockWalletClient.Object,
                _mockNotificationService.Object,
                _mockInstanceStore.Object,
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
