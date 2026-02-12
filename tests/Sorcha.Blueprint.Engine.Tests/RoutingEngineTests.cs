// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Unit tests for RoutingEngine.
/// </summary>
public class RoutingEngineTests
{
    private readonly IJsonLogicEvaluator _evaluator;
    private readonly IRoutingEngine _routingEngine;

    public RoutingEngineTests()
    {
        _evaluator = new JsonLogicEvaluator();
        _routingEngine = new RoutingEngine(_evaluator);
    }

    #region Simple Routing

    [Fact]
    public async Task DetermineNextAsync_SimpleRouting_ReturnsNextParticipant()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-001",
            Title = "Simple Workflow",
            Description = "A simple two-step workflow",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "requester", Name = "Requester" },
                new() { Id = "approver", Name = "Approver" }
            },
            Actions = new List<BpModels.Action>
            {
                new() // Action 1 - Requester submits
                {
                    Id = 1,
                    Title = "Submit Request",
                    Sender = "requester",
                    Participants = new List<BpModels.Condition>
                    {
                        new("approver", true) // Always route to approver
                    }
                },
                new() // Action 2 - Approver reviews
                {
                    Id = 2,
                    Title = "Approve Request",
                    Sender = "approver"
                }
            }
        };

        var currentAction = blueprint.Actions[0];
        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

        // Assert
        result.IsWorkflowComplete.Should().BeFalse();
        result.NextParticipantId.Should().Be("approver");
        result.NextActionId.Should().Be("2");
    }

    [Fact]
    public async Task DetermineNextAsync_LastAction_ReturnsComplete()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-001",
            Title = "Simple Workflow",
            Description = "A simple workflow",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Final Action",
                    Sender = "user1",
                    Participants = new List<BpModels.Condition>() // No next participants
                }
            }
        };

        var currentAction = blueprint.Actions[0];
        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

        // Assert
        result.IsWorkflowComplete.Should().BeTrue();
        result.NextParticipantId.Should().BeNull();
        result.NextActionId.Should().BeNull();
    }

    #endregion

    #region Conditional Routing

    [Fact]
    public async Task DetermineNextAsync_ConditionalRouting_RoutesBasedOnAmount()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-002",
            Title = "Approval Workflow",
            Description = "Routes based on amount",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "requester", Name = "Requester" },
                new() { Id = "manager", Name = "Manager" },
                new() { Id = "director", Name = "Director" }
            },
            Actions = new List<BpModels.Action>
            {
                new() // Action 1 - Submit request
                {
                    Id = 1,
                    Title = "Submit Request",
                    Sender = "requester",
                    Participants = new List<BpModels.Condition>
                    {
                        // Route to director if amount >= 10000
                        new("director", new List<string> { """{">=": [{"var": "amount"}, 10000]}""" }),
                        // Otherwise route to manager
                        new("manager", true)
                    }
                },
                new() // Action 2 - Manager approval
                {
                    Id = 2,
                    Title = "Manager Approval",
                    Sender = "manager"
                },
                new() // Action 3 - Director approval
                {
                    Id = 3,
                    Title = "Director Approval",
                    Sender = "director"
                }
            }
        };

        var currentAction = blueprint.Actions[0];

        // Test 1: Small amount -> manager
        var smallAmountData = new Dictionary<string, object> { ["amount"] = 5000 };
        var smallResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, smallAmountData);

        // Test 2: Large amount -> director
        var largeAmountData = new Dictionary<string, object> { ["amount"] = 15000 };
        var largeResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, largeAmountData);

        // Assert
        smallResult.NextParticipantId.Should().Be("manager");
        smallResult.NextActionId.Should().Be("2");

        largeResult.NextParticipantId.Should().Be("director");
        largeResult.NextActionId.Should().Be("3");
    }

    [Fact]
    public async Task DetermineNextAsync_ComplexCondition_EvaluatesCorrectly()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-003",
            Title = "Complex Routing",
            Description = "Routes based on multiple conditions",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "admin", Name = "Admin" },
                new() { Id = "superadmin", Name = "Super Admin" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Participants = new List<BpModels.Condition>
                    {
                        // Route to superadmin if amount > 50000 AND urgent == true
                        new("superadmin", new List<string>
                        {
                            """{">": [{"var": "amount"}, 50000]}""",
                            """{"==": [{"var": "urgent"}, true]}"""
                        }),
                        // Route to admin if amount > 10000
                        new("admin", new List<string>
                        {
                            """{">": [{"var": "amount"}, 10000]}"""
                        }),
                        // Default to user
                        new("user", true)
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Process",
                    Sender = "admin"
                },
                new()
                {
                    Id = 3,
                    Title = "Escalate",
                    Sender = "superadmin"
                }
            }
        };

        var currentAction = blueprint.Actions[0];

        // Test 1: High amount + urgent -> superadmin
        var urgentData = new Dictionary<string, object>
        {
            ["amount"] = 60000,
            ["urgent"] = true
        };
        var urgentResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, urgentData);

        // Test 2: High amount but not urgent -> admin
        var nonUrgentData = new Dictionary<string, object>
        {
            ["amount"] = 60000,
            ["urgent"] = false
        };
        var nonUrgentResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, nonUrgentData);

        // Test 3: Medium amount -> admin
        var mediumData = new Dictionary<string, object>
        {
            ["amount"] = 20000,
            ["urgent"] = false
        };
        var mediumResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, mediumData);

        // Assert
        urgentResult.NextParticipantId.Should().Be("superadmin");
        urgentResult.NextActionId.Should().Be("3");

        nonUrgentResult.NextParticipantId.Should().Be("admin");
        nonUrgentResult.NextActionId.Should().Be("2");

        mediumResult.NextParticipantId.Should().Be("admin");
        mediumResult.NextActionId.Should().Be("2");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DetermineNextAsync_NoConditionsMatch_ReturnsComplete()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-004",
            Title = "Conditional Workflow",
            Description = "May or may not have next step",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "admin", Name = "Admin" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Participants = new List<BpModels.Condition>
                    {
                        // Only route to admin if approved == true
                        new("admin", new List<string>
                        {
                            """{"==": [{"var": "approved"}, true]}"""
                        })
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Process",
                    Sender = "admin"
                }
            }
        };

        var currentAction = blueprint.Actions[0];
        var data = new Dictionary<string, object>
        {
            ["approved"] = false // Condition won't match
        };

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

        // Assert
        result.IsWorkflowComplete.Should().BeTrue();
        result.NextParticipantId.Should().BeNull();
        result.NextActionId.Should().BeNull();
    }

    [Fact]
    public async Task DetermineNextAsync_ParticipantMatchButNoAction_ReturnsComplete()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-005",
            Title = "Incomplete Workflow",
            Description = "Participant exists but no action defined",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "phantom", Name = "Phantom User" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Participants = new List<BpModels.Condition>
                    {
                        new("phantom", true) // Routes to phantom, but no action exists
                    }
                }
                // No action for phantom participant
            }
        };

        var currentAction = blueprint.Actions[0];
        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

        // Assert
        result.IsWorkflowComplete.Should().BeTrue();
    }

    [Fact]
    public async Task DetermineNextAsync_EmptyData_Works()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-006",
            Title = "No Data Workflow",
            Description = "Workflow that doesn't depend on data",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user1", Name = "User 1" },
                new() { Id = "user2", Name = "User 2" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Step 1",
                    Sender = "user1",
                    Participants = new List<BpModels.Condition>
                    {
                        new("user2", true) // Unconditional routing
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Step 2",
                    Sender = "user2"
                }
            }
        };

        var currentAction = blueprint.Actions[0];
        var data = new Dictionary<string, object>(); // Empty data

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

        // Assert
        result.NextParticipantId.Should().Be("user2");
        result.NextActionId.Should().Be("2");
    }

    #endregion

    #region Multiple Criteria

    [Fact]
    public async Task DetermineNextAsync_MultipleCriteriaAllMatch_Routes()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-007",
            Title = "Multi-Criteria Workflow",
            Description = "Requires multiple conditions to match",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "specialist", Name = "Specialist" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Participants = new List<BpModels.Condition>
                    {
                        new("specialist", new List<string>
                        {
                            """{">": [{"var": "amount"}, 5000]}""",
                            """{"==": [{"var": "type"}, "special"]}""",
                            """{"==": [{"var": "verified"}, true]}"""
                        })
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Review",
                    Sender = "specialist"
                }
            }
        };

        var currentAction = blueprint.Actions[0];

        // All criteria match
        var matchingData = new Dictionary<string, object>
        {
            ["amount"] = 10000,
            ["type"] = "special",
            ["verified"] = true
        };
        var matchingResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, matchingData);

        // One criterion fails
        var failingData = new Dictionary<string, object>
        {
            ["amount"] = 10000,
            ["type"] = "special",
            ["verified"] = false // This fails
        };
        var failingResult = await _routingEngine.DetermineNextAsync(blueprint, currentAction, failingData);

        // Assert
        matchingResult.NextParticipantId.Should().Be("specialist");
        failingResult.IsWorkflowComplete.Should().BeTrue();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task DetermineNextAsync_NullBlueprint_ThrowsArgumentNullException()
    {
        // Arrange
        var action = new BpModels.Action { Id = 1, Title = "Test" };
        var data = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _routingEngine.DetermineNextAsync(null!, action, data)
        );
    }

    [Fact]
    public async Task DetermineNextAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-001",
            Title = "Test",
            Description = "Test",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>()
        };
        var data = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _routingEngine.DetermineNextAsync(blueprint, null!, data)
        );
    }

    [Fact]
    public async Task DetermineNextAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-001",
            Title = "Test",
            Description = "Test",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>()
        };
        var action = new BpModels.Action { Id = 1, Title = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _routingEngine.DetermineNextAsync(blueprint, action, null!)
        );
    }

    [Fact]
    public void Constructor_NullEvaluator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoutingEngine(null!));
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task DetermineNextAsync_PurchaseApprovalWorkflow_Works()
    {
        // Arrange - A realistic purchase approval workflow
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-PURCHASE",
            Title = "Purchase Approval",
            Description = "Multi-level purchase approval based on amount",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "employee", Name = "Employee" },
                new() { Id = "team-lead", Name = "Team Lead" },
                new() { Id = "manager", Name = "Manager" },
                new() { Id = "finance", Name = "Finance Director" },
                new() { Id = "ceo", Name = "CEO" }
            },
            Actions = new List<BpModels.Action>
            {
                new() // Step 1: Employee submits
                {
                    Id = 1,
                    Title = "Submit Purchase Request",
                    Sender = "employee",
                    Participants = new List<BpModels.Condition>
                    {
                        new("ceo", new List<string> { """{">=": [{"var": "amount"}, 100000]}""" }),
                        new("finance", new List<string> { """{">=": [{"var": "amount"}, 50000]}""" }),
                        new("manager", new List<string> { """{">=": [{"var": "amount"}, 10000]}""" }),
                        new("team-lead", true) // Default
                    }
                },
                new() // Step 2: Team lead approves
                {
                    Id = 2,
                    Title = "Team Lead Approval",
                    Sender = "team-lead"
                },
                new() // Step 3: Manager approves
                {
                    Id = 3,
                    Title = "Manager Approval",
                    Sender = "manager"
                },
                new() // Step 4: Finance approves
                {
                    Id = 4,
                    Title = "Finance Approval",
                    Sender = "finance"
                },
                new() // Step 5: CEO approves
                {
                    Id = 5,
                    Title = "CEO Approval",
                    Sender = "ceo"
                }
            }
        };

        var currentAction = blueprint.Actions[0];

        // Test different amounts
        var tests = new[]
        {
            (amount: 500, expected: "team-lead", actionId: "2"),
            (amount: 15000, expected: "manager", actionId: "3"),
            (amount: 75000, expected: "finance", actionId: "4"),
            (amount: 150000, expected: "ceo", actionId: "5")
        };

        foreach (var test in tests)
        {
            var data = new Dictionary<string, object> { ["amount"] = test.amount };
            var result = await _routingEngine.DetermineNextAsync(blueprint, currentAction, data);

            result.NextParticipantId.Should().Be(test.expected, $"amount {test.amount} should route to {test.expected}");
            result.NextActionId.Should().Be(test.actionId);
        }
    }

    #endregion

    #region Route-Based Routing

    [Fact]
    public async Task DetermineNextAsync_WithRoutes_UsesRoutesOverLegacy()
    {
        // Arrange - action has both Routes and Participants; Routes should take precedence
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-001",
            Title = "Route-Based Workflow",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "approver", Name = "Approver" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new() { Id = "route-1", NextActionIds = new List<int> { 2 }, IsDefault = true }
                    },
                    // Legacy participants should be ignored when Routes are defined
                    Participants = new List<BpModels.Condition>
                    {
                        new("nonexistent", true)
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Approve",
                    Sender = "approver"
                }
            }
        };

        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert - should use Routes, not legacy Participants
        result.NextActionId.Should().Be("2");
        result.NextParticipantId.Should().Be("approver");
        result.IsWorkflowComplete.Should().BeFalse();
    }

    [Fact]
    public async Task DetermineNextAsync_WithConditionalRoutes_FirstMatchWins()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-002",
            Title = "Conditional Routes",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new()
                        {
                            Id = "high-value",
                            NextActionIds = new List<int> { 3 },
                            Condition = System.Text.Json.Nodes.JsonNode.Parse("""{">": [{"var": "amount"}, 10000]}""")
                        },
                        new()
                        {
                            Id = "default",
                            NextActionIds = new List<int> { 2 },
                            IsDefault = true
                        }
                    }
                },
                new() { Id = 2, Title = "Standard Review", Sender = "reviewer" },
                new() { Id = 3, Title = "Executive Review", Sender = "executive" }
            }
        };

        // Act - high value
        var highResult = await _routingEngine.DetermineNextAsync(
            blueprint, blueprint.Actions[0],
            new Dictionary<string, object> { ["amount"] = 50000 });

        // Act - low value (falls to default)
        var lowResult = await _routingEngine.DetermineNextAsync(
            blueprint, blueprint.Actions[0],
            new Dictionary<string, object> { ["amount"] = 500 });

        // Assert
        highResult.NextActionId.Should().Be("3");
        highResult.NextParticipantId.Should().Be("executive");

        lowResult.NextActionId.Should().Be("2");
        lowResult.NextParticipantId.Should().Be("reviewer");
    }

    [Fact]
    public async Task DetermineNextAsync_WithParallelRoutes_ReturnsParallelResult()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-003",
            Title = "Parallel Routing",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new()
                        {
                            Id = "parallel-review",
                            NextActionIds = new List<int> { 2, 3 }, // Parallel branches
                            IsDefault = true
                        }
                    }
                },
                new() { Id = 2, Title = "Legal Review", Sender = "legal" },
                new() { Id = 3, Title = "Finance Review", Sender = "finance" }
            }
        };

        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert
        result.IsParallel.Should().BeTrue();
        result.NextActions.Should().HaveCount(2);
        result.NextActions[0].ActionId.Should().Be("2");
        result.NextActions[0].ParticipantId.Should().Be("legal");
        result.NextActions[0].BranchId.Should().NotBeNullOrEmpty();
        result.NextActions[1].ActionId.Should().Be("3");
        result.NextActions[1].ParticipantId.Should().Be("finance");
        result.NextActions[1].BranchId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DetermineNextAsync_WithRoutesNoMatch_ReturnsComplete()
    {
        // Arrange - only conditional routes, none match, no default
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-004",
            Title = "No Match",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new()
                        {
                            Id = "conditional",
                            NextActionIds = new List<int> { 2 },
                            Condition = System.Text.Json.Nodes.JsonNode.Parse("""{"==": [{"var": "status"}, "approved"]}""")
                        }
                    }
                },
                new() { Id = 2, Title = "Process", Sender = "processor" }
            }
        };

        var data = new Dictionary<string, object> { ["status"] = "rejected" };

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert
        result.IsWorkflowComplete.Should().BeTrue();
    }

    [Fact]
    public async Task DetermineNextAsync_WithEmptyRoutes_FallsBackToLegacy()
    {
        // Arrange - empty Routes list, should fall back to Participants
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-005",
            Title = "Legacy Fallback",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "approver", Name = "Approver" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>(), // Empty list
                    Participants = new List<BpModels.Condition>
                    {
                        new("approver", true)
                    }
                },
                new() { Id = 2, Title = "Approve", Sender = "approver" }
            }
        };

        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert - should use legacy routing
        result.NextActionId.Should().Be("2");
        result.NextParticipantId.Should().Be("approver");
    }

    [Fact]
    public async Task DetermineNextAsync_WithRouteEmptyNextActionIds_ReturnsComplete()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-006",
            Title = "Empty NextActionIds",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new() { Id = "empty", NextActionIds = new List<int>(), IsDefault = true }
                    }
                }
            }
        };

        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert
        result.IsWorkflowComplete.Should().BeTrue();
    }

    [Fact]
    public async Task DetermineNextAsync_WithRoute_PopulatesMatchedRouteId()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-ROUTE-007",
            Title = "Route ID Tracking",
            Participants = new List<BpModels.Participant>(),
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user",
                    Routes = new List<BpModels.Route>
                    {
                        new()
                        {
                            Id = "approval-route",
                            NextActionIds = new List<int> { 2 },
                            IsDefault = true
                        }
                    }
                },
                new() { Id = 2, Title = "Approve", Sender = "approver" }
            }
        };

        var data = new Dictionary<string, object>();

        // Act
        var result = await _routingEngine.DetermineNextAsync(blueprint, blueprint.Actions[0], data);

        // Assert
        result.NextActions.Should().HaveCount(1);
        result.NextActions[0].MatchedRouteId.Should().Be("approval-route");
    }

    #endregion
}
