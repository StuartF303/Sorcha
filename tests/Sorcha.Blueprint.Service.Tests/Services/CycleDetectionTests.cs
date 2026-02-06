// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;
using ParticipantModel = Sorcha.Blueprint.Models.Participant;
using RouteModel = Sorcha.Blueprint.Models.Route;
using RejectionConfigModel = Sorcha.Blueprint.Models.RejectionConfig;

namespace Sorcha.Blueprint.Service.Tests.Services;

/// <summary>
/// Tests for graph cycle detection in blueprint publish validation.
/// Exercises the DetectCycles/DfsCycleDetect methods through PublishService.PublishAsync().
/// </summary>
public class CycleDetectionTests
{
    private readonly Mock<IBlueprintStore> _mockBlueprintStore;
    private readonly Mock<IPublishedBlueprintStore> _mockPublishedStore;
    private readonly PublishService _publishService;

    public CycleDetectionTests()
    {
        _mockBlueprintStore = new Mock<IBlueprintStore>();
        _mockPublishedStore = new Mock<IPublishedBlueprintStore>();
        _publishService = new PublishService(_mockBlueprintStore.Object, _mockPublishedStore.Object);
    }

    [Fact]
    public async Task PublishAsync_LinearGraph_ReturnsSuccess()
    {
        // Arrange: 1 → 2 → 3 (no cycles)
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Start", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Middle", Sender = "p2",
                Routes = [new RouteModel { NextActionIds = [3], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 3, Title = "End", Sender = "p1"
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_SimpleCycle_ReturnsCycleError()
    {
        // Arrange: 1 → 2 → 1 (cycle)
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Start", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Review", Sender = "p2",
                Routes = [new RouteModel { NextActionIds = [1], IsDefault = true }]
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Circular dependency detected"));
        result.Errors.Should().Contain(e => e.Contains("Action 1") && e.Contains("Action 2"));
    }

    [Fact]
    public async Task PublishAsync_SelfReference_ReturnsSelfReferenceError()
    {
        // Arrange: Action 1 routes to itself
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Self-Loop", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [1], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Other", Sender = "p2"
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Self-referencing route detected: Action 1 routes to itself"));
    }

    [Fact]
    public async Task PublishAsync_ComplexCycleWithBranches_DetectsCycle()
    {
        // Arrange: 1 → 2, 1 → 3, 2 → 4, 3 → 4, 4 → 1 (cycle through branches)
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Start", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2, 3], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Branch A", Sender = "p2",
                Routes = [new RouteModel { NextActionIds = [4], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 3, Title = "Branch B", Sender = "p1",
                Routes = [new RouteModel { NextActionIds = [4], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 4, Title = "Converge", Sender = "p2",
                Routes = [new RouteModel { NextActionIds = [1], IsDefault = true }]
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Circular dependency detected"));
    }

    [Fact]
    public async Task PublishAsync_ParallelBranchesNoCycle_ReturnsSuccess()
    {
        // Arrange: 1 → [2, 3] (parallel, no cycles)
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Start", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2, 3], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Parallel A", Sender = "p2"
            },
            new ActionModel
            {
                Id = 3, Title = "Parallel B", Sender = "p1"
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_RejectionConfigCycle_DetectedViaRejectionTarget()
    {
        // Arrange: 1 → 2, but 2 rejects back to 1, and 1 routes to 2 (creates cycle via rejection)
        // Since rejection targets are edges in the graph, 1 → 2 → 1 is a cycle
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Submit", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Review", Sender = "p2",
                RejectionConfig = new RejectionConfigModel
                {
                    TargetActionId = 1,
                    RequireReason = true
                }
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Circular dependency detected"));
    }

    [Fact]
    public async Task PublishAsync_NoRoutes_ReturnsSuccess()
    {
        // Arrange: Simple blueprint with no routes at all
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Action 1", Sender = "p1", IsStartingAction = true
            },
            new ActionModel
            {
                Id = 2, Title = "Action 2", Sender = "p2"
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_DiamondGraph_NoCycle_ReturnsSuccess()
    {
        // Arrange: Diamond shape 1 → [2, 3] → 4 (converge, no back-edges)
        var blueprint = CreateBlueprint(
            new ActionModel
            {
                Id = 1, Title = "Start", Sender = "p1", IsStartingAction = true,
                Routes = [new RouteModel { NextActionIds = [2, 3], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 2, Title = "Left", Sender = "p2",
                Routes = [new RouteModel { NextActionIds = [4], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 3, Title = "Right", Sender = "p1",
                Routes = [new RouteModel { NextActionIds = [4], IsDefault = true }]
            },
            new ActionModel
            {
                Id = 4, Title = "End", Sender = "p2"
            });

        SetupStore(blueprint);

        // Act
        var result = await _publishService.PublishAsync(blueprint.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #region Helpers

    private BlueprintModel CreateBlueprint(params ActionModel[] actions)
    {
        return new BlueprintModel
        {
            Id = "test-blueprint",
            Title = "Test Blueprint",
            Participants =
            [
                new ParticipantModel { Id = "p1", Name = "Participant 1" },
                new ParticipantModel { Id = "p2", Name = "Participant 2" }
            ],
            Actions = actions.ToList()
        };
    }

    private void SetupStore(BlueprintModel blueprint)
    {
        _mockBlueprintStore
            .Setup(x => x.GetAsync(blueprint.Id))
            .ReturnsAsync(blueprint);

        _mockPublishedStore
            .Setup(x => x.AddAsync(It.IsAny<PublishedBlueprint>()))
            .ReturnsAsync((PublishedBlueprint p) => p);
    }

    #endregion
}
