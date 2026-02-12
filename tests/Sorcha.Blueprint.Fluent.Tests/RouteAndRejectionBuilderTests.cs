// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent.Tests;

public class RouteAndRejectionBuilderTests
{
    #region RouteBuilder Tests

    [Fact]
    public void AddRoute_WithDefaultRoute_SetsRouteOnAction()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AddRoute("route-1", r => r
                    .ToActions(2)
                    .AsDefault()))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2"))
            .Build();

        var action = blueprint.Actions[0];
        action.Routes.Should().HaveCount(1);
        action.Routes!.First().Id.Should().Be("route-1");
        action.Routes!.First().IsDefault.Should().BeTrue();
        action.Routes!.First().NextActionIds.Should().Contain(2);
    }

    [Fact]
    public void AddRoute_WithCondition_SetsCondition()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AddRoute("high-value", r => r
                    .ToActions(2)
                    .When(j => j.GreaterThan("amount", 10000))))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2"))
            .Build();

        var route = blueprint.Actions[0].Routes!.First();
        route.Condition.Should().NotBeNull();
        route.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void AddRoute_WithParallelActions_SetsMultipleNextActionIds()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddParticipant("p3", p => p.Named("P3"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AddRoute("parallel", r => r
                    .ToActions(2, 3)
                    .AsDefault()
                    .WithDescription("Parallel review")))
            .AddAction(2, a => a
                .WithTitle("Legal Review")
                .SentBy("p2"))
            .AddAction(3, a => a
                .WithTitle("Finance Review")
                .SentBy("p3"))
            .Build();

        var route = blueprint.Actions[0].Routes!.First();
        route.NextActionIds.Should().HaveCount(2);
        route.NextActionIds.Should().Contain(2);
        route.NextActionIds.Should().Contain(3);
        route.Description.Should().Be("Parallel review");
    }

    [Fact]
    public void AddRoute_WithBranchDeadline_SetsDeadline()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AddRoute("timed", r => r
                    .ToActions(2)
                    .AsDefault()
                    .WithBranchDeadline("P7D")))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2"))
            .Build();

        var route = blueprint.Actions[0].Routes!.First();
        route.BranchDeadline.Should().Be("P7D");
    }

    [Fact]
    public void AddRoute_WithMultipleRoutes_PreservesOrder()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddParticipant("p3", p => p.Named("P3"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AddRoute("high-value", r => r
                    .ToActions(3)
                    .When(j => j.GreaterThan("amount", 10000)))
                .AddRoute("default", r => r
                    .ToActions(2)
                    .AsDefault()))
            .AddAction(2, a => a.WithTitle("Standard").SentBy("p2"))
            .AddAction(3, a => a.WithTitle("Executive").SentBy("p3"))
            .Build();

        var routes = blueprint.Actions[0].Routes!.ToList();
        routes.Should().HaveCount(2);
        routes[0].Id.Should().Be("high-value");
        routes[1].Id.Should().Be("default");
    }

    [Fact]
    public void WithDefaultRoute_CreatesDefaultRouteShorthand()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .WithDefaultRoute(2))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2"))
            .Build();

        var routes = blueprint.Actions[0].Routes!.ToList();
        routes.Should().HaveCount(1);
        routes[0].IsDefault.Should().BeTrue();
        routes[0].NextActionIds.Should().Contain(2);
    }

    #endregion

    #region RejectionConfigBuilder Tests

    [Fact]
    public void OnRejection_SetsRejectionConfig()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1"))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2")
                .OnRejection(r => r
                    .RouteToAction(1)
                    .RequireReason()))
            .Build();

        var action = blueprint.Actions[1];
        action.RejectionConfig.Should().NotBeNull();
        action.RejectionConfig!.TargetActionId.Should().Be(1);
        action.RejectionConfig.RequireReason.Should().BeTrue();
    }

    [Fact]
    public void OnRejection_WithTargetParticipant_SetsParticipant()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a.WithTitle("Submit").SentBy("p1"))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2")
                .OnRejection(r => r
                    .RouteToAction(1)
                    .WithTargetParticipant("p1")))
            .Build();

        var config = blueprint.Actions[1].RejectionConfig!;
        config.TargetParticipantId.Should().Be("p1");
    }

    [Fact]
    public void OnRejection_AsTerminal_SetsTerminalFlag()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a.WithTitle("Submit").SentBy("p1"))
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2")
                .OnRejection(r => r
                    .RouteToAction(1)
                    .AsTerminal()))
            .Build();

        var config = blueprint.Actions[1].RejectionConfig!;
        config.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region AsStartingAction and RequiresPriorActions Tests

    [Fact]
    public void AsStartingAction_SetsFlag()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .AsStartingAction())
            .Build();

        blueprint.Actions[0].IsStartingAction.Should().BeTrue();
    }

    [Fact]
    public void RequiresPriorActions_SetsActionIds()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test workflow description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(1, a => a.WithTitle("Submit").SentBy("p1").AsStartingAction())
            .AddAction(2, a => a
                .WithTitle("Review")
                .SentBy("p2")
                .RequiresPriorActions(1))
            .Build();

        blueprint.Actions[1].RequiredPriorActions.Should().Contain(1);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteWorkflow_WithRoutesAndRejection_Builds()
    {
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Expense Approval")
            .WithDescription("Multi-level expense approval workflow")
            .AddParticipant("employee", p => p.Named("Employee"))
            .AddParticipant("manager", p => p.Named("Manager"))
            .AddParticipant("finance", p => p.Named("Finance"))
            .AddAction(1, a => a
                .WithTitle("Submit Expense")
                .SentBy("employee")
                .AsStartingAction()
                .AddRoute("high-value", r => r
                    .ToActions(3)
                    .When(j => j.GreaterThan("amount", 5000))
                    .WithDescription("High value expenses go to finance"))
                .AddRoute("standard", r => r
                    .ToActions(2)
                    .AsDefault()
                    .WithDescription("Standard expenses go to manager")))
            .AddAction(2, a => a
                .WithTitle("Manager Approval")
                .SentBy("manager")
                .RequiresPriorActions(1)
                .OnRejection(r => r
                    .RouteToAction(1)
                    .RequireReason()
                    .WithTargetParticipant("employee")))
            .AddAction(3, a => a
                .WithTitle("Finance Approval")
                .SentBy("finance")
                .RequiresPriorActions(1)
                .OnRejection(r => r
                    .RouteToAction(1)
                    .RequireReason()))
            .Build();

        // Verify structure
        blueprint.Actions.Should().HaveCount(3);

        // Verify routes
        var submitAction = blueprint.Actions[0];
        submitAction.IsStartingAction.Should().BeTrue();
        submitAction.Routes.Should().HaveCount(2);

        // Verify rejection configs
        blueprint.Actions[1].RejectionConfig.Should().NotBeNull();
        blueprint.Actions[1].RejectionConfig!.TargetActionId.Should().Be(1);
        blueprint.Actions[2].RejectionConfig.Should().NotBeNull();
    }

    #endregion
}
