// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Engine.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Tests for RoutingResult, including parallel branch support via RoutedAction and Parallel factory.
/// </summary>
public class RoutingResultTests
{
    #region Complete Factory

    [Fact]
    public void Complete_ReturnsWorkflowComplete()
    {
        var result = RoutingResult.Complete();

        result.IsWorkflowComplete.Should().BeTrue();
        result.NextActionId.Should().BeNull();
        result.NextParticipantId.Should().BeNull();
        result.NextActions.Should().BeEmpty();
        result.IsParallel.Should().BeFalse();
        result.RejectedToParticipantId.Should().BeNull();
    }

    #endregion

    #region Next Factory

    [Fact]
    public void Next_PopulatesSingularProperties()
    {
        var result = RoutingResult.Next("action-2", "participant-b", "amount > 1000");

        result.NextActionId.Should().Be("action-2");
        result.NextParticipantId.Should().Be("participant-b");
        result.MatchedCondition.Should().Be("amount > 1000");
        result.IsWorkflowComplete.Should().BeFalse();
        result.IsParallel.Should().BeFalse();
    }

    [Fact]
    public void Next_PopulatesNextActionsListWithSingleEntry()
    {
        var result = RoutingResult.Next("action-2", "participant-b");

        result.NextActions.Should().HaveCount(1);
        result.NextActions[0].ActionId.Should().Be("action-2");
        result.NextActions[0].ParticipantId.Should().Be("participant-b");
    }

    [Fact]
    public void Next_WithoutMatchedCondition_SetsConditionNull()
    {
        var result = RoutingResult.Next("action-3", "participant-c");

        result.MatchedCondition.Should().BeNull();
    }

    [Fact]
    public void Next_BackwardCompatibility_SingularAndListMatch()
    {
        var result = RoutingResult.Next("action-5", "participant-e", "status == approved");

        result.NextActionId.Should().Be(result.NextActions[0].ActionId);
        result.NextParticipantId.Should().Be(result.NextActions[0].ParticipantId);
    }

    #endregion

    #region Parallel Factory

    [Fact]
    public void Parallel_WithMultipleActions_SetsIsParallelTrue()
    {
        var actions = new List<RoutedAction>
        {
            new() { ActionId = "action-2", ParticipantId = "participant-a", BranchId = "branch-1", MatchedRouteId = "route-1" },
            new() { ActionId = "action-3", ParticipantId = "participant-b", BranchId = "branch-2", MatchedRouteId = "route-1" }
        };

        var result = RoutingResult.Parallel(actions);

        result.IsParallel.Should().BeTrue();
        result.NextActions.Should().HaveCount(2);
        result.IsWorkflowComplete.Should().BeFalse();
    }

    [Fact]
    public void Parallel_WithSingleAction_SetsIsParallelFalse()
    {
        var actions = new List<RoutedAction>
        {
            new() { ActionId = "action-2", ParticipantId = "participant-a" }
        };

        var result = RoutingResult.Parallel(actions);

        result.IsParallel.Should().BeFalse();
        result.NextActions.Should().HaveCount(1);
    }

    [Fact]
    public void Parallel_SetsFirstActionAsSingularForBackwardCompatibility()
    {
        var actions = new List<RoutedAction>
        {
            new() { ActionId = "action-2", ParticipantId = "participant-a" },
            new() { ActionId = "action-3", ParticipantId = "participant-b" }
        };

        var result = RoutingResult.Parallel(actions, "route condition");

        result.NextActionId.Should().Be("action-2");
        result.NextParticipantId.Should().Be("participant-a");
        result.MatchedCondition.Should().Be("route condition");
    }

    [Fact]
    public void Parallel_PreservesBranchAndRouteIds()
    {
        var actions = new List<RoutedAction>
        {
            new() { ActionId = "action-2", BranchId = "branch-A", MatchedRouteId = "approval-route" },
            new() { ActionId = "action-3", BranchId = "branch-B", MatchedRouteId = "approval-route" }
        };

        var result = RoutingResult.Parallel(actions);

        result.NextActions[0].BranchId.Should().Be("branch-A");
        result.NextActions[0].MatchedRouteId.Should().Be("approval-route");
        result.NextActions[1].BranchId.Should().Be("branch-B");
        result.NextActions[1].MatchedRouteId.Should().Be("approval-route");
    }

    [Fact]
    public void Parallel_WithEmptyList_IsNotParallel()
    {
        var result = RoutingResult.Parallel([]);

        result.IsParallel.Should().BeFalse();
        result.NextActions.Should().BeEmpty();
        result.NextActionId.Should().BeNull();
    }

    #endregion

    #region Reject Factory

    [Fact]
    public void Reject_SetsRejectedParticipant()
    {
        var result = RoutingResult.Reject("participant-a");

        result.RejectedToParticipantId.Should().Be("participant-a");
        result.NextActionId.Should().BeNull();
        result.IsWorkflowComplete.Should().BeFalse();
        result.NextActions.Should().BeEmpty();
        result.IsParallel.Should().BeFalse();
    }

    #endregion

    #region RoutedAction Record

    [Fact]
    public void RoutedAction_DefaultValues()
    {
        var action = new RoutedAction();

        action.ActionId.Should().Be("");
        action.ParticipantId.Should().BeNull();
        action.BranchId.Should().BeNull();
        action.MatchedRouteId.Should().BeNull();
    }

    [Fact]
    public void RoutedAction_WithInitValues()
    {
        var action = new RoutedAction
        {
            ActionId = "action-5",
            ParticipantId = "participant-x",
            BranchId = "branch-42",
            MatchedRouteId = "route-approval"
        };

        action.ActionId.Should().Be("action-5");
        action.ParticipantId.Should().Be("participant-x");
        action.BranchId.Should().Be("branch-42");
        action.MatchedRouteId.Should().Be("route-approval");
    }

    [Fact]
    public void RoutedAction_RecordEquality()
    {
        var a = new RoutedAction { ActionId = "1", ParticipantId = "p1" };
        var b = new RoutedAction { ActionId = "1", ParticipantId = "p1" };

        a.Should().Be(b);
    }

    #endregion
}
