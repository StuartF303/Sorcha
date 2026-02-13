// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models;
using Sorcha.ServiceClients.Register;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;
using ParticipantModel = Sorcha.Blueprint.Models.Participant;

namespace Sorcha.Blueprint.Service.Tests.Services;

/// <summary>
/// Tests for PublishService: validation-only endpoint, publish with/without registerId.
/// </summary>
public class PublishServiceTests
{
    private readonly Mock<IBlueprintStore> _mockBlueprintStore;
    private readonly Mock<IPublishedBlueprintStore> _mockPublishedStore;
    private readonly Mock<IRegisterServiceClient> _mockRegisterClient;

    public PublishServiceTests()
    {
        _mockBlueprintStore = new Mock<IBlueprintStore>();
        _mockPublishedStore = new Mock<IPublishedBlueprintStore>();
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
    }

    private PublishService CreateService(IRegisterServiceClient? registerClient = null)
    {
        return new PublishService(
            _mockBlueprintStore.Object,
            _mockPublishedStore.Object,
            registerClient);
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_BlueprintNotFound_ReturnsInvalid()
    {
        _mockBlueprintStore.Setup(s => s.GetAsync("missing")).ReturnsAsync((BlueprintModel?)null);
        var service = CreateService();

        var result = await service.ValidateAsync("missing");

        result.IsValid.Should().BeFalse();
        result.BlueprintId.Should().Be("missing");
        result.ValidationResults.Should().ContainSingle(i => i.Message == "Blueprint not found");
    }

    [Fact]
    public async Task ValidateAsync_ValidBlueprint_ReturnsValid()
    {
        var blueprint = CreateValidBlueprint();
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-1")).ReturnsAsync(blueprint);
        var service = CreateService();

        var result = await service.ValidateAsync("bp-1");

        result.IsValid.Should().BeTrue();
        result.BlueprintId.Should().Be("bp-1");
        result.Title.Should().Be("Test Blueprint");
        result.ValidationResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_InvalidBlueprint_ReturnsErrors()
    {
        var blueprint = new BlueprintModel
        {
            Id = "bp-2",
            Title = "Incomplete",
            Participants = [new ParticipantModel { Id = "p1", Name = "Alice" }],
            Actions = []
        };
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-2")).ReturnsAsync(blueprint);
        var service = CreateService();

        var result = await service.ValidateAsync("bp-2");

        result.IsValid.Should().BeFalse();
        result.ValidationResults.Should().Contain(i => i.Message.Contains("at least 2 participants"));
        result.ValidationResults.Should().Contain(i => i.Message.Contains("at least 1 action"));
    }

    [Fact]
    public async Task ValidateAsync_BlueprintWithCycles_ReturnsValidWithWarnings()
    {
        var blueprint = CreateBlueprintWithCycle();
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-cycle")).ReturnsAsync(blueprint);
        var service = CreateService();

        var result = await service.ValidateAsync("bp-cycle");

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().NotBeEmpty();
    }

    #endregion

    #region PublishAsync — Without RegisterId (Legacy Behavior)

    [Fact]
    public async Task PublishAsync_NoRegisterId_PublishesLocally()
    {
        var blueprint = CreateValidBlueprint();
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-1")).ReturnsAsync(blueprint);
        var service = CreateService();

        var result = await service.PublishAsync("bp-1");

        result.IsSuccess.Should().BeTrue();
        result.PublishedBlueprint.Should().NotBeNull();
        _mockPublishedStore.Verify(s => s.AddAsync(It.IsAny<PublishedBlueprint>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_NullRegisterId_DoesNotCallRegisterClient()
    {
        var blueprint = CreateValidBlueprint();
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-1")).ReturnsAsync(blueprint);
        var service = CreateService(_mockRegisterClient.Object);

        var result = await service.PublishAsync("bp-1", null);

        result.IsSuccess.Should().BeTrue();
        _mockRegisterClient.Verify(
            c => c.PublishBlueprintToRegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    #endregion

    #region PublishAsync — With RegisterId

    [Fact]
    public async Task PublishAsync_WithRegisterId_CallsRegisterClient()
    {
        var blueprint = CreateValidBlueprint();
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-1")).ReturnsAsync(blueprint);
        _mockRegisterClient
            .Setup(c => c.PublishBlueprintToRegisterAsync(
                "reg-1", "bp-1", It.IsAny<string>(), "system", default))
            .ReturnsAsync(true);
        var service = CreateService(_mockRegisterClient.Object);

        var result = await service.PublishAsync("bp-1", "reg-1");

        result.IsSuccess.Should().BeTrue();
        _mockPublishedStore.Verify(s => s.AddAsync(It.IsAny<PublishedBlueprint>()), Times.Once);
        _mockRegisterClient.Verify(
            c => c.PublishBlueprintToRegisterAsync(
                "reg-1", "bp-1", It.IsAny<string>(), "system", default),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_InvalidBlueprint_DoesNotCallRegisterClient()
    {
        var blueprint = new BlueprintModel
        {
            Id = "bp-invalid",
            Title = "Invalid",
            Participants = [],
            Actions = []
        };
        _mockBlueprintStore.Setup(s => s.GetAsync("bp-invalid")).ReturnsAsync(blueprint);
        var service = CreateService(_mockRegisterClient.Object);

        var result = await service.PublishAsync("bp-invalid", "reg-1");

        result.IsSuccess.Should().BeFalse();
        _mockRegisterClient.Verify(
            c => c.PublishBlueprintToRegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    #endregion

    #region Helpers

    private static BlueprintModel CreateValidBlueprint() => new()
    {
        Id = "bp-1",
        Title = "Test Blueprint",
        Participants =
        [
            new ParticipantModel { Id = "p1", Name = "Alice" },
            new ParticipantModel { Id = "p2", Name = "Bob" }
        ],
        Actions =
        [
            new ActionModel
            {
                Id = 1,
                Title = "Start",
                Sender = "p1",
                IsStartingAction = true
            }
        ]
    };

    private static BlueprintModel CreateBlueprintWithCycle() => new()
    {
        Id = "bp-cycle",
        Title = "Cyclic Blueprint",
        Participants =
        [
            new ParticipantModel { Id = "p1", Name = "Alice" },
            new ParticipantModel { Id = "p2", Name = "Bob" }
        ],
        Actions =
        [
            new ActionModel
            {
                Id = 1,
                Title = "Step A",
                Sender = "p1",
                IsStartingAction = true,
                Routes = [new Route { NextActionIds = [2], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2,
                Title = "Step B",
                Sender = "p2",
                Routes = [new Route { NextActionIds = [1], IsDefault = true }]
            }
        ]
    };

    #endregion
}
