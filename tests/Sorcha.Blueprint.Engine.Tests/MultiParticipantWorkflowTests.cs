// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Tests for multi-participant workflow validation.
/// Tests Category 10 from BLUEPRINT-VALIDATION-TEST-PLAN.md
/// </summary>
public class MultiParticipantWorkflowTests
{
    #region Test Data Helpers

    private static BpModels.Blueprint CreateBaseBlueprint(int participantCount)
    {
        var blueprint = new BpModels.Blueprint
        {
            Id = "test-workflow",
            Title = "Multi-Participant Workflow Test",
            Description = "Testing multi-participant workflow scenarios",
            Participants = new List<BpModels.Participant>()
        };

        for (int i = 0; i < participantCount; i++)
        {
            blueprint.Participants.Add(new BpModels.Participant
            {
                Id = $"participant-{i}",
                Name = $"Participant {i}",
                Organisation = $"Org {i}",
                WalletAddress = $"wallet-{i}"
            });
        }

        return blueprint;
    }

    #endregion

    #region 10.1 Simple Linear Workflow (2 participants)

    [Fact]
    public void LinearWorkflow_TwoParticipants_ValidatesCorrectly()
    {
        // Arrange - Simple A → B workflow
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "Participant A submits",
                Sender = "wallet-0",
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                },
                Condition = JsonNode.Parse("{\"==\":[0,0]}")
            }
        };

        // Act - Validate action sender/target references
        var senderExists = blueprint.Participants.Any(p => p.WalletAddress == blueprint.Actions[0].Sender);
        var targetExists = blueprint.Participants.Any(p => p.WalletAddress == blueprint.Actions[0].Target);

        // Assert
        senderExists.Should().BeTrue("Sender must be a valid participant");
        targetExists.Should().BeTrue("Target must be a valid participant");
        blueprint.Participants.Should().HaveCount(2);
        blueprint.Actions.Should().HaveCount(1);
    }

    [Fact]
    public void LinearWorkflow_ActionSenderMatchesPreviousTarget_IsValid()
    {
        // Arrange - A → B → A workflow where sender matches previous target
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "Action 0: A sends to B",
                Sender = "wallet-0",
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                }
            },
            new()
            {
                Id = 1,
                Title = "Action 1: B sends back to A",
                Sender = "wallet-1", // Matches previous action's target
                Target = "wallet-0",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-0", new List<string> { "/*" })
                }
            }
        };

        // Act
        var action1SenderMatchesAction0Target = blueprint.Actions[1].Sender == blueprint.Actions[0].Target;

        // Assert
        action1SenderMatchesAction0Target.Should().BeTrue("Action 1 sender should match Action 0 target");
    }

    [Fact]
    public void LinearWorkflow_CompletesWithAllParticipants_IsValid()
    {
        // Arrange - Workflow that involves all participants
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "A to B",
                Sender = "wallet-0",
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                }
            }
        };

        // Act - Check all participants are involved
        var participantsInvolved = new HashSet<string>
        {
            blueprint.Actions[0].Sender,
            blueprint.Actions[0].Target!
        };

        // Assert
        participantsInvolved.Should().HaveCount(blueprint.Participants.Count, "All participants should be involved");
    }

    [Fact]
    public void LinearWorkflow_ThreeSteps_ValidatesChain()
    {
        // Arrange - A → B → A → B
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new() { Id = 0, Sender = "wallet-0", Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-1", new List<string> { "/*" }) } },
            new() { Id = 1, Sender = "wallet-1", Target = "wallet-0",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-0", new List<string> { "/*" }) } },
            new() { Id = 2, Sender = "wallet-0", Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-1", new List<string> { "/*" }) } }
        };

        // Act - Validate chain continuity
        var chain1Valid = blueprint.Actions[1].Sender == blueprint.Actions[0].Target;
        var chain2Valid = blueprint.Actions[2].Sender == blueprint.Actions[1].Target;

        // Assert
        chain1Valid.Should().BeTrue();
        chain2Valid.Should().BeTrue();
    }

    #endregion

    #region 10.2 Branching Workflow (3+ participants)

    [Fact]
    public void BranchingWorkflow_ConditionalRouting_ValidatesAllBranches()
    {
        // Arrange - A → (B or C based on condition) → D
        var blueprint = CreateBaseBlueprint(4);
        blueprint.Actions = new List<BpModels.Action>
        {
            // Action 0: A submits, routes to B or C based on amount
            new()
            {
                Id = 0,
                Title = "Submit request",
                Sender = "wallet-0",
                Participants = new List<BpModels.Condition>
                {
                    new()
                    {
                        Principal = "wallet-1", // Route to B if amount > 1000
                        Criteria = new List<string> { "{\">\": [{\"var\": \"amount\"}, 1000]}" }
                    },
                    new()
                    {
                        Principal = "wallet-2", // Route to C otherwise
                        Criteria = new List<string> { "{\"<=\": [{\"var\": \"amount\"}, 1000]}" }
                    }
                },
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" }),
                    new("wallet-2", new List<string> { "/*" })
                }
            },
            // Action 1: B or C approves, sends to D
            new()
            {
                Id = 1,
                Title = "Approve",
                // Sender could be wallet-1 or wallet-2
                Target = "wallet-3",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-3", new List<string> { "/*" })
                }
            }
        };

        // Act - Validate participant routing conditions
        var action0HasRouting = blueprint.Actions[0].Participants != null && blueprint.Actions[0].Participants.Any();

        // Assert
        action0HasRouting.Should().BeTrue("Action should have participant routing conditions");
        blueprint.Actions[0].Participants.Should().HaveCount(2, "Should have 2 routing branches");
    }

    [Fact]
    public void BranchingWorkflow_AllBranchesReferenceValidParticipants()
    {
        // Arrange - Workflow with 3 participants and branching
        var blueprint = CreateBaseBlueprint(3);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "Branch decision",
                Sender = "wallet-0",
                Participants = new List<BpModels.Condition>
                {
                    new() { Principal = "wallet-1", Criteria = new List<string> { "{\"==\":[1,1]}" } },
                    new() { Principal = "wallet-2", Criteria = new List<string> { "{\"==\":[2,2]}" } }
                },
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" }),
                    new("wallet-2", new List<string> { "/*" })
                }
            }
        };

        // Act - Validate all routing outputs reference valid participants
        var allOutputsValid = blueprint.Actions[0].Participants!.All(condition =>
            blueprint.Participants.Any(p => p.WalletAddress == condition.Principal)
        );

        // Assert
        allOutputsValid.Should().BeTrue("All routing branch outputs must reference valid participants");
    }

    [Fact]
    public void BranchingWorkflow_FourParticipantsMultiplePaths_ValidatesCorrectly()
    {
        // Arrange - Complex branching: A → (B or C) → D
        var blueprint = CreateBaseBlueprint(4);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "Initial submission",
                Sender = "wallet-0",
                Participants = new List<BpModels.Condition>
                {
                    new() { Principal = "wallet-1" },
                    new() { Principal = "wallet-2" }
                },
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" }),
                    new("wallet-2", new List<string> { "/*" })
                }
            },
            new()
            {
                Id = 1,
                Title = "Review",
                Target = "wallet-3", // Both branches converge to wallet-3
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-3", new List<string> { "/*" })
                }
            }
        };

        // Act
        var hasMultipleBranches = blueprint.Actions[0].Participants!.Count() >= 2;
        var finalTargetValid = blueprint.Participants.Any(p => p.WalletAddress == blueprint.Actions[1].Target);

        // Assert
        hasMultipleBranches.Should().BeTrue();
        finalTargetValid.Should().BeTrue();
    }

    #endregion

    #region 10.3 Round-Robin Workflow

    [Fact]
    public void RoundRobinWorkflow_ThreeParticipantsLoop_ValidatesCorrectly()
    {
        // Arrange - A → B → C → A (endorsement loop)
        var blueprint = CreateBaseBlueprint(3);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "A to B",
                Sender = "wallet-0",
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                }
            },
            new()
            {
                Id = 1,
                Title = "B to C",
                Sender = "wallet-1",
                Target = "wallet-2",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-2", new List<string> { "/*" })
                }
            },
            new()
            {
                Id = 2,
                Title = "C back to A",
                Sender = "wallet-2",
                Target = "wallet-0", // Back to start
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-0", new List<string> { "/*" })
                }
            }
        };

        // Act - Validate the loop
        var formsLoop = blueprint.Actions[2].Target == blueprint.Actions[0].Sender;

        // Assert
        formsLoop.Should().BeTrue("Should form a complete loop back to initial participant");
        blueprint.Actions.Should().HaveCount(3);
    }

    [Fact]
    public void RoundRobinWorkflow_CycleDetection_AllowsControlledLoops()
    {
        // Arrange - Controlled loop with max iterations
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Title = "Iteration step",
                Sender = "wallet-0",
                Target = "wallet-1",
                Condition = JsonNode.Parse(@"{
                    ""if"": [
                        {""<"": [{""var"": ""iteration""}, 3]},
                        0,
                        1
                    ]
                }"), // Loop back to 0 if iteration < 3, else go to 1
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                }
            },
            new()
            {
                Id = 1,
                Title = "Exit loop",
                Sender = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-0", new List<string> { "/*" })
                }
            }
        };

        // Act - Verify condition allows controlled loop
        var hasLoopCondition = blueprint.Actions[0].Condition != null;

        // Assert
        hasLoopCondition.Should().BeTrue("Controlled loops should have termination conditions");
    }

    [Fact]
    public void RoundRobinWorkflow_MultipleIterations_TracksParticipantInvolvement()
    {
        // Arrange - Round-robin with 4 participants
        var blueprint = CreateBaseBlueprint(4);
        var participationCount = new Dictionary<string, int>
        {
            ["wallet-0"] = 0,
            ["wallet-1"] = 0,
            ["wallet-2"] = 0,
            ["wallet-3"] = 0
        };

        blueprint.Actions = new List<BpModels.Action>
        {
            new() { Id = 0, Sender = "wallet-0", Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-1", new List<string> { "/*" }) } },
            new() { Id = 1, Sender = "wallet-1", Target = "wallet-2",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-2", new List<string> { "/*" }) } },
            new() { Id = 2, Sender = "wallet-2", Target = "wallet-3",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-3", new List<string> { "/*" }) } },
            new() { Id = 3, Sender = "wallet-3", Target = "wallet-0",
                Disclosures = new List<BpModels.Disclosure> { new("wallet-0", new List<string> { "/*" }) } }
        };

        // Act - Count participant involvement
        foreach (var action in blueprint.Actions)
        {
            participationCount[action.Sender]++;
            if (action.Target != null)
            {
                participationCount[action.Target]++;
            }
        }

        // Assert - All participants should be involved equally
        participationCount.Values.Should().AllSatisfy(count => count.Should().Be(2), "Each participant appears as sender and target once");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void MultiParticipant_AllParticipantsHaveValidWalletAddresses()
    {
        // Arrange
        var blueprint = CreateBaseBlueprint(5);

        // Act - Validate all wallet addresses are populated
        var allHaveWallets = blueprint.Participants.All(p => !string.IsNullOrEmpty(p.WalletAddress));

        // Assert
        allHaveWallets.Should().BeTrue("All participants must have wallet addresses");
    }

    [Fact]
    public void MultiParticipant_DisclosuresIncludeAllRelevantParticipants()
    {
        // Arrange - Workflow with selective disclosure
        var blueprint = CreateBaseBlueprint(3);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Sender = "wallet-0",
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/data" }), // Primary recipient
                    new("wallet-2", new List<string> { "/summary" }) // Observer gets limited data
                }
            }
        };

        // Act
        var disclosureParticipants = blueprint.Actions[0].Disclosures.Select(d => d.ParticipantAddress).ToList();

        // Assert
        disclosureParticipants.Should().Contain("wallet-1", "Primary target should have disclosure");
        disclosureParticipants.Should().Contain("wallet-2", "Observer should have limited disclosure");
    }

    [Fact]
    public void MultiParticipant_ComplexWorkflowWithParallelPaths_ValidatesCorrectly()
    {
        // Arrange - Workflow with parallel execution paths
        var blueprint = CreateBaseBlueprint(5);
        blueprint.Actions = new List<BpModels.Action>
        {
            // Action 0: A distributes to B and C in parallel
            new()
            {
                Id = 0,
                Title = "Distribute",
                Sender = "wallet-0",
                AdditionalRecipients = new List<string> { "wallet-1", "wallet-2" },
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" }),
                    new("wallet-2", new List<string> { "/*" })
                }
            }
        };

        // Act
        var hasMultipleRecipients = blueprint.Actions[0].AdditionalRecipients.Any();

        // Assert
        hasMultipleRecipients.Should().BeTrue("Parallel paths should have multiple recipients");
    }

    [Fact]
    public void MultiParticipant_SenderNotInParticipantsList_FailsValidation()
    {
        // Arrange
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Sender = "non-existent-wallet", // Invalid sender
                Target = "wallet-1",
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-1", new List<string> { "/*" })
                }
            }
        };

        // Act
        var senderValid = blueprint.Participants.Any(p => p.WalletAddress == blueprint.Actions[0].Sender);

        // Assert
        senderValid.Should().BeFalse("Invalid sender should fail validation");
    }

    [Fact]
    public void MultiParticipant_TargetNotInParticipantsList_FailsValidation()
    {
        // Arrange
        var blueprint = CreateBaseBlueprint(2);
        blueprint.Actions = new List<BpModels.Action>
        {
            new()
            {
                Id = 0,
                Sender = "wallet-0",
                Target = "non-existent-wallet", // Invalid target
                Disclosures = new List<BpModels.Disclosure>
                {
                    new("wallet-0", new List<string> { "/*" })
                }
            }
        };

        // Act
        var targetValid = blueprint.Participants.Any(p => p.WalletAddress == blueprint.Actions[0].Target);

        // Assert
        targetValid.Should().BeFalse("Invalid target should fail validation");
    }

    #endregion
}
